using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using OpenCvSharp;
using WinForms = System.Windows.Forms;

namespace ScreenWidget
{
    public partial class MainWindow : System.Windows.Window
    {
        private WinForms.NotifyIcon _notifyIcon = null!;
        private bool _isDragging;
        private System.Windows.Point _dragOrigin;
        private int _winLeft, _winTop;
        private CameraSettings _settings = null!;

        // Timers
        private DispatcherTimer _captureTimer = null!;   // auto-capture
        private DispatcherTimer _countdownTimer = null!;  // refresh countdown label
        private DateTime _nextCapture;

        public MainWindow()
        {
            InitializeComponent();
            _settings = SettingsStore.Load();
            ApplySettings(_settings, initialLoad: true);
            InitializeNotifyIcon();

            if (_settings.WindowX < 0 || _settings.WindowY < 0)
            {
                Left = SystemParameters.WorkArea.Right - _settings.WidgetWidth - 20;
                Top = 600;
            }
            else
            {
                Left = _settings.WindowX;
                Top = _settings.WindowY;
            }
        }

        // ── Settings ─────────────────────────────────────────────────────────

        internal void ApplySettings(CameraSettings s, bool initialLoad = false)
        {
            Opacity = s.Opacity;
            CamImage.Width = s.WidgetWidth;
            CamImage.Height = s.WidgetWidth * 0.75; // 4:3 default
            ContentBorder.Margin = new Thickness(s.BorderThickness);
            ContentBorder.CornerRadius = new CornerRadius(
                Math.Max(0, s.CornerRadius - s.BorderThickness));
            AnimBorderRect.RadiusX = s.CornerRadius;
            AnimBorderRect.RadiusY = s.CornerRadius;

            // Restart border animation with new speed
            var anim = new DoubleAnimation(0, 360,
                new Duration(TimeSpan.FromSeconds(s.BorderSpeed)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            BorderRotate.BeginAnimation(RotateTransform.AngleProperty, anim);

            // Move window live
            var hwnd = new WindowInteropHelper(this).Handle;
            if (!initialLoad && hwnd != IntPtr.Zero && s.WindowX >= 0 && s.WindowY >= 0)
                SetWindowPos(hwnd, IntPtr.Zero,
                    (int)s.WindowX, (int)s.WindowY, 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

            // Restart capture timer with new interval
            RestartCaptureTimer(s.CaptureIntervalMinutes);

            // Load last saved image if any
            if (!string.IsNullOrEmpty(s.LastImagePath) && File.Exists(s.LastImagePath))
                DisplayImage(s.LastImagePath);
        }

        // ── Border animation (started on Loaded so HWND is ready) ─────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PinToDesktop();

            var anim = new DoubleAnimation(0, 360,
                new Duration(TimeSpan.FromSeconds(_settings.BorderSpeed)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            BorderRotate.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        // ── Camera capture ────────────────────────────────────────────────────

        internal void CaptureNow()
        {
            try
            {
                using var cap = new VideoCapture(_settings.CameraIndex);
                if (!cap.IsOpened()) return;

                using var frame = new Mat();
                cap.Read(frame);
                if (frame.Empty()) return;

                // Save to pictures folder
                string path = Path.Combine(
                    SettingsStore.CaptureFolder(),
                    $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                OpenCvSharp.Cv2.ImWrite(path, frame);

                _settings.LastImagePath = path;
                SettingsStore.Save(_settings);

                Dispatcher.Invoke(() => DisplayImage(path));
            }
            catch { /* no camera or capture error – fail silently */ }
        }

        private void DisplayImage(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.EndInit();
                bmp.Freeze();

                CamImage.Source = bmp;
                NoImagePanel.Visibility = Visibility.Collapsed;
                CaptureTimeLabel.Text = $"📷 {DateTime.Now:HH:mm:ss}";
            }
            catch { }
        }

        private void RestartCaptureTimer(int minutes)
        {
            _captureTimer?.Stop();
            _countdownTimer?.Stop();

            _nextCapture = DateTime.Now.AddMinutes(minutes);

            _captureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(minutes)
            };
            _captureTimer.Tick += (_, _) =>
            {
                System.Threading.Tasks.Task.Run(CaptureNow);
                _nextCapture = DateTime.Now.AddMinutes(_settings.CaptureIntervalMinutes);
            };
            _captureTimer.Start();

            // Refresh the "next capture" label every second
            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += (_, _) =>
            {
                var remaining = _nextCapture - DateTime.Now;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                NextCaptureLabel.Text = $"⏱ {remaining:mm\\:ss}";
            };
            _countdownTimer.Start();
        }

        // ── Tray icon ─────────────────────────────────────────────────────────

        private const string AppName = "ScreenWidget";

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
                Text = "Camera Widget"
            };

            var startupItem = new WinForms.ToolStripMenuItem("Start with Windows")
            {
                Checked = IsStartupEnabled(),
                CheckOnClick = true
            };
            startupItem.CheckedChanged += (s, e) => SetStartup(startupItem.Checked);

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Capture Now", null, (_, _) =>
                System.Threading.Tasks.Task.Run(CaptureNow));
            menu.Items.Add("Open captures folder", null, (_, _) =>
                System.Diagnostics.Process.Start("explorer.exe", SettingsStore.CaptureFolder()));
            menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(startupItem);
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => CloseApp());

            _notifyIcon.ContextMenuStrip = menu;
        }

        private void OpenSettings()
        {
            // Sync current window pos before opening
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                GetWindowRect(hwnd, out RECT r);
                _settings.WindowX = r.Left;
                _settings.WindowY = r.Top;
            }

            var win = new SettingsWindow(_settings, this);
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
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                GetWindowRect(hwnd, out RECT r);
                _settings.WindowX = r.Left;
                _settings.WindowY = r.Top;
            }
            SettingsStore.Save(_settings);

            _captureTimer?.Stop();
            _countdownTimer?.Stop();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        // ── Desktop pinning ───────────────────────────────────────────────────

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