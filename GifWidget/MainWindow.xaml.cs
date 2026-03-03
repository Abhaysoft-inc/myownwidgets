using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace GifWidget
{
    public partial class MainWindow : Window
    {
        private WinForms.NotifyIcon _notifyIcon = null!;
        private bool _isDragging;
        private System.Windows.Point _dragOrigin;
        private int _winLeft, _winTop;
        private GifSettings _settings = null!;

        // GIF animation – pre-composited frames
        private BitmapSource[]? _composited;
        private int[]? _delays;
        private int _frameIndex;
        private DispatcherTimer? _gifTimer;

        public MainWindow()
        {
            InitializeComponent();
            _settings = SettingsStore.Load();
            ApplySettings(_settings);
            InitializeNotifyIcon();

            // Position: below TimetableWidget by default (first run)
            if (_settings.WindowX < 0 || _settings.WindowY < 0)
            {
                Left = SystemParameters.WorkArea.Right - _settings.WidgetWidth - 20;
                Top = 320; // approx: timetable top (40) + height (~260) + gap
            }
            else
            {
                Left = _settings.WindowX;
                Top = _settings.WindowY;
            }
        }

        // ── Settings ──────────────────────────────────────────────────────────

        internal void ApplySettings(GifSettings s)
        {
            Opacity = s.Opacity;
            GifImage.Width = s.WidgetWidth;
            RootBorder.CornerRadius = new CornerRadius(s.CornerRadius);

            if (s.ShowBorder)
            {
                RootBorder.BorderThickness = new Thickness(1);
                RootBorder.BorderBrush = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(120, 255, 255, 255));
            }
            else
            {
                RootBorder.BorderThickness = new Thickness(0);
                RootBorder.BorderBrush = System.Windows.Media.Brushes.Transparent;
            }

            // Move window live (works for both regular and desktop-pinned modes)
            if (s.WindowX >= 0 && s.WindowY >= 0)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                    SetWindowPos(hwnd, IntPtr.Zero,
                        (int)s.WindowX, (int)s.WindowY, 0, 0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }

            if (!string.IsNullOrEmpty(s.GifPath) && File.Exists(s.GifPath))
                LoadGif(s.GifPath);
        }

        // ── GIF compositor ────────────────────────────────────────────────────
        // Pre-renders every frame as a fully composited image so the Image
        // control only ever receives complete, artifact-free bitmaps.

        private record FrameInfo(
            BitmapSource Src, int Left, int Top, int DelayMs, int Disposal);

        private void LoadGif(string path)
        {
            _gifTimer?.Stop();
            _composited = null;
            _delays = null;
            _frameIndex = 0;

            try
            {
                var decoder = new GifBitmapDecoder(
                    new Uri(path, UriKind.Absolute),
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                if (decoder.Frames.Count == 0) return;

                // Read logical screen size
                int gifW = decoder.Frames[0].PixelWidth;
                int gifH = decoder.Frames[0].PixelHeight;
                if (decoder.Metadata is BitmapMetadata screenMeta)
                {
                    if (screenMeta.GetQuery("/logscrdesc/Width") is ushort sw) gifW = sw;
                    if (screenMeta.GetQuery("/logscrdesc/Height") is ushort sh) gifH = sh;
                }

                double dpi = 96;

                // Parse per-frame metadata
                var infos = new FrameInfo[decoder.Frames.Count];
                for (int i = 0; i < decoder.Frames.Count; i++)
                {
                    var f = decoder.Frames[i];
                    var fm = f.Metadata as BitmapMetadata;

                    int left = fm?.GetQuery("/imgdesc/Left") is ushort l ? l : 0;
                    int top = fm?.GetQuery("/imgdesc/Top") is ushort t ? t : 0;
                    ushort? d = fm?.GetQuery("/grctlext/Delay") as ushort?;
                    int delayMs = d is { } dv && dv > 0 ? dv * 10 : 100;
                    int disposal = fm?.GetQuery("/grctlext/Disposal") is byte dp ? dp : 0;

                    // Convert to Pbgra32 so compositing works
                    var src = new FormatConvertedBitmap(f, PixelFormats.Pbgra32, null, 0);
                    src.Freeze();
                    infos[i] = new FrameInfo(src, left, top, delayMs, disposal);
                }

                // Pre-composite all frames
                _composited = new BitmapSource[infos.Length];
                _delays = new int[infos.Length];
                BitmapSource? canvas = null;   // accumulated canvas
                BitmapSource? savedPre = null;   // for disposal=3

                for (int i = 0; i < infos.Length; i++)
                {
                    var info = infos[i];
                    _delays![i] = info.DelayMs;

                    // Build "background" from previous frame's disposal rule
                    BitmapSource? bg;
                    if (i == 0)
                        bg = null;
                    else
                    {
                        int prevDisposal = infos[i - 1].Disposal;
                        bg = prevDisposal == 2 ? null          // restore to transparent
                           : prevDisposal == 3 ? savedPre      // restore to pre-previous
                           : canvas;                           // keep composite
                    }

                    // If this frame needs disposal=3, save bg before drawing onto it
                    if (info.Disposal == 3)
                        savedPre = bg;

                    // Render: bg + this frame at its offset
                    var visual = new DrawingVisual();
                    using (var ctx = visual.RenderOpen())
                    {
                        if (bg != null)
                            ctx.DrawImage(bg, new Rect(0, 0, gifW, gifH));
                        ctx.DrawImage(info.Src,
                            new Rect(info.Left, info.Top,
                                     info.Src.PixelWidth, info.Src.PixelHeight));
                    }

                    var rtb = new RenderTargetBitmap(gifW, gifH, dpi, dpi, PixelFormats.Pbgra32);
                    rtb.Render(visual);
                    rtb.Freeze();

                    canvas = rtb;
                    _composited[i] = rtb;
                }

                GifImage.Source = _composited[0];

                if (_composited.Length > 1)
                {
                    _gifTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(_delays![0])
                    };
                    _gifTimer.Tick += GifTimer_Tick;
                    _gifTimer.Start();
                }
            }
            catch { /* silently ignore bad files */ }
        }

        private void GifTimer_Tick(object? sender, EventArgs e)
        {
            if (_composited is null || _delays is null) return;

            _frameIndex = (_frameIndex + 1) % _composited.Length;

            if (sender is DispatcherTimer t)
                t.Interval = TimeSpan.FromMilliseconds(_delays[_frameIndex]);

            GifImage.Source = _composited[_frameIndex];
        }

        // ── Tray icon ─────────────────────────────────────────────────────────

        private const string AppName = "GifWidget";

        private bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }

        private void SetStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
                key?.SetValue(AppName,
                    $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName}\"");
            else
                key?.DeleteValue(AppName, false);
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "GIF Widget"
            };

            var startupItem = new WinForms.ToolStripMenuItem("Start with Windows")
            {
                Checked = IsStartupEnabled(),
                CheckOnClick = true
            };
            startupItem.CheckedChanged += (s, e) => SetStartup(startupItem.Checked);

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Load GIF…", null, (s, e) => PickGif());
            menu.Items.Add("Appearance…", null, (s, e) => OpenAppearance());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(startupItem);
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => CloseApp());

            _notifyIcon.ContextMenuStrip = menu;
        }

        private void PickGif()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a GIF",
                Filter = "GIF files (*.gif)|*.gif|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _settings.GifPath = dlg.FileName;
                SettingsStore.Save(_settings);
                LoadGif(_settings.GifPath);
            }
        }

        private void OpenAppearance()
        {
            // Sync actual current screen position before opening the panel
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                GetWindowRect(hwnd, out RECT r);
                _settings.WindowX = r.Left;
                _settings.WindowY = r.Top;
            }

            var win = new AppearanceWindow(_settings);
            win.SettingsApplied += s =>
            {
                _settings = s;
                SettingsStore.Save(s);
                ApplySettings(s);
            };
            win.Show();
        }

        private void CloseApp()
        {
            GetWindowRect(new WindowInteropHelper(this).Handle, out RECT r);
            _settings.WindowX = r.Left;
            _settings.WindowY = r.Top;
            SettingsStore.Save(_settings);

            _gifTimer?.Stop();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        // ── Desktop pinning ───────────────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e) => PinToDesktop();

        private void PinToDesktop()
        {
            IntPtr progman = FindWindow("Progman", null);
            SendMessageTimeout(progman, 0x052C, UIntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

            IntPtr workerW = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                if (FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                    workerW = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                return true;
            }, IntPtr.Zero);

            if (workerW != IntPtr.Zero)
                SetParent(new WindowInteropHelper(this).Handle, workerW);
        }

        // ── Manual drag ───────────────────────────────────────────────────────

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragOrigin = PointToScreen(e.GetPosition(this));
            GetWindowRect(new WindowInteropHelper(this).Handle, out RECT r);
            _winLeft = r.Left;
            _winTop = r.Top;
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pt = PointToScreen(e.GetPosition(this));
            int dx = (int)(pt.X - _dragOrigin.X);
            int dy = (int)(pt.Y - _dragOrigin.Y);
            SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero,
                _winLeft + dx, _winTop + dy, 0, 0,
                SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
        }

        // ── P/Invoke ──────────────────────────────────────────────────────────

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] static extern IntPtr FindWindow(string cls, string? win);
        [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string? win);
        [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam, uint flags, uint timeout, out UIntPtr result);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc fn, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }
    }
}