using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace CodingPresence
{
    public partial class MainWindow : System.Windows.Window
    {
        private WinForms.NotifyIcon _tray = null!;
        private bool _isDragging;
        private System.Windows.Point _dragOrigin;
        private int _winLeft, _winTop;
        private PresenceSettings _settings = null!;

        // Timers
        private DispatcherTimer _pollTimer = null!;
        private DispatcherTimer _clockTimer = null!;
        private DispatcherTimer _quoteTimer = null!;
        private DispatcherTimer _blinkTimer = null!;

        // GIF animation (pre-composited frames)
        private BitmapSource[]? _gifFrames;
        private int[]? _gifDelays;
        private int _gifFrameIndex;
        private DispatcherTimer? _gifTimer;
        private string _currentGifPath = string.Empty;

        // State
        private DateTime _sessionStart = DateTime.Now;
        private bool _isActive = false;          // IDE detected
        private int _quoteIndex = 0;

        // Anime / coder quotes
        private static readonly string[] Quotes = {
            "\"The only way to do great work is to love what you do.\" — Jobs",
            "\"Code is like humour. When you have to explain it, it's bad.\" — Cory",
            "\"First, solve the problem. Then, write the code.\" — Johnson",
            "\"Experience is the name everyone gives to their mistakes.\" — Wilde",
            "\"In the middle of every difficulty lies opportunity.\" — Einstein",
            "\"It works on my machine! ✨\" — Every dev, ever",
            "\"A ninja who cannot code is just a person in pyjamas.\" — Kakashi probably",
            "\"Plus Ultra! ... also, don't forget to push to git.\" — All Might",
            "\"Even I make bugs sometimes.\" — L Lawliet (probably)",
            "\"The true ninja deploys on Friday.\" — Ancient proverb",
            "\"I am the one who codes.\" — Heisenbug",
            "\"If debugging is the process of removing bugs, then programming must be the process of putting them in.\" — Dijkstra",
            "\"Talk is cheap. Show me the code.\" — Linus Torvalds",
            "\"Sometimes it pays to stay in bed on Monday, rather than spending the rest of the week debugging Monday's code.\" — Dan Salomon",
        };

        public MainWindow()
        {
            InitializeComponent();
            _settings = SettingsStore.Load();
            ResetTodayIfNewDay();

            Opacity = _settings.Opacity;
            Width = _settings.WidgetWidth;

            if (_settings.WindowX < 0 || _settings.WindowY < 0)
            {
                Left = SystemParameters.WorkArea.Right - Width - 20;
                Top = 40;
            }
            else
            {
                Left = _settings.WindowX;
                Top = _settings.WindowY;
            }

            InitTray();
            ShowQuote();
        }

        // ── Today time reset ─────────────────────────────────────────────────

        private void ResetTodayIfNewDay()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (_settings.TodayDate != today)
            {
                _settings.TodaySeconds = 0;
                _settings.TodayDate = today;
            }
        }

        // ── Window Loaded → pin + animations ────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PinToDesktop();
            StartAnimations();
            StartTimers();
        }

        // ── Cat animations ───────────────────────────────────────────────────

        private void StartAnimations()
        {
            // Tail wag
            var tailAnim = new DoubleAnimation(-18, 18,
                new Duration(TimeSpan.FromSeconds(0.7)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase()
            };
            TailRotate.BeginAnimation(RotateTransform.AngleProperty, tailAnim);

            // Paw typing (alternating up-down)
            var pawDA = new DoubleAnimation(0, -5,
                new Duration(TimeSpan.FromSeconds(0.25)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            var pawDB = new DoubleAnimation(-5, 0,
                new Duration(TimeSpan.FromSeconds(0.25)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            PawLeftMove.BeginAnimation(TranslateTransform.YProperty, pawDA);
            PawRightMove.BeginAnimation(TranslateTransform.YProperty, pawDB);

            // Eye blink every ~4 seconds
            _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _blinkTimer.Tick += (_, _) => Blink();
            _blinkTimer.Start();
        }

        private void Blink()
        {
            var close = new DoubleAnimation(1, 0,
                new Duration(TimeSpan.FromMilliseconds(60)));
            var open = new DoubleAnimation(0, 1,
                new Duration(TimeSpan.FromMilliseconds(80)));
            open.BeginTime = TimeSpan.FromMilliseconds(80);

            var group = new ParallelTimeline();
            EyeLeftScale.BeginAnimation(ScaleTransform.ScaleYProperty, close);
            EyeRightScale.BeginAnimation(ScaleTransform.ScaleYProperty, close);
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
                t.Tick += (_, _) =>
                {
                    EyeLeftScale.BeginAnimation(ScaleTransform.ScaleYProperty, open);
                    EyeRightScale.BeginAnimation(ScaleTransform.ScaleYProperty, open);
                    t.Stop();
                };
                t.Start();
            }));
        }

        // ── GIF compositor ────────────────────────────────────────────────
        // Pre-renders every frame so artifact-free bitmaps reach the Image.

        private record FrameInfo(BitmapSource Src, int Left, int Top, int DelayMs, int Disposal);

        private void LoadGif(string path)
        {
            _gifTimer?.Stop();
            _gifFrames = null;
            _gifDelays = null;
            _gifFrameIndex = 0;
            _currentGifPath = path;

            try
            {
                var decoder = new GifBitmapDecoder(
                    new Uri(path, UriKind.Absolute),
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                if (decoder.Frames.Count == 0) return;

                int gifW = decoder.Frames[0].PixelWidth;
                int gifH = decoder.Frames[0].PixelHeight;
                if (decoder.Metadata is BitmapMetadata sm)
                {
                    if (sm.GetQuery("/logscrdesc/Width") is ushort sw) gifW = sw;
                    if (sm.GetQuery("/logscrdesc/Height") is ushort sh) gifH = sh;
                }

                var infos = new FrameInfo[decoder.Frames.Count];
                for (int i = 0; i < decoder.Frames.Count; i++)
                {
                    var f = decoder.Frames[i];
                    var fm = f.Metadata as BitmapMetadata;
                    int left = fm?.GetQuery("/imgdesc/Left") is ushort l ? l : 0;
                    int top = fm?.GetQuery("/imgdesc/Top") is ushort t ? t : 0;
                    int delay = fm?.GetQuery("/grctlext/Delay") is ushort d ? d * 10 : 100;
                    int disp = fm?.GetQuery("/grctlext/Disposal") is byte dp ? dp : 0;
                    var src = new FormatConvertedBitmap(f, PixelFormats.Pbgra32, null, 0);
                    src.Freeze();
                    infos[i] = new FrameInfo(src, left, top, delay, disp);
                }

                _gifFrames = new BitmapSource[infos.Length];
                _gifDelays = new int[infos.Length];
                BitmapSource? canvas = null;
                BitmapSource? savedPre = null;

                for (int i = 0; i < infos.Length; i++)
                {
                    var info = infos[i];
                    _gifDelays![i] = info.DelayMs;

                    BitmapSource? bg;
                    if (i == 0) bg = null;
                    else
                    {
                        int prev = infos[i - 1].Disposal;
                        bg = prev == 2 ? null : prev == 3 ? savedPre : canvas;
                    }
                    if (info.Disposal == 3) savedPre = bg;

                    var visual = new System.Windows.Media.DrawingVisual();
                    using (var ctx = visual.RenderOpen())
                    {
                        if (bg != null) ctx.DrawImage(bg, new Rect(0, 0, gifW, gifH));
                        ctx.DrawImage(info.Src,
                            new Rect(info.Left, info.Top,
                                     info.Src.PixelWidth, info.Src.PixelHeight));
                    }
                    var rtb = new RenderTargetBitmap(gifW, gifH, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(visual);
                    rtb.Freeze();
                    canvas = rtb;
                    _gifFrames![i] = rtb;
                }

                GifMascot.Source = _gifFrames[0];

                if (_gifFrames.Length > 1)
                {
                    _gifTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(_gifDelays![0])
                    };
                    _gifTimer.Tick += GifTimer_Tick;
                    _gifTimer.Start();
                }
            }
            catch { }
        }

        private void GifTimer_Tick(object? s, EventArgs e)
        {
            if (_gifFrames is null || _gifDelays is null) return;
            _gifFrameIndex = (_gifFrameIndex + 1) % _gifFrames.Length;
            if (s is DispatcherTimer t)
                t.Interval = TimeSpan.FromMilliseconds(_gifDelays[_gifFrameIndex]);
            GifMascot.Source = _gifFrames[_gifFrameIndex];
        }

        // ── Theme application ─────────────────────────────────────────────────

        private void ApplyTheme(ColorTheme theme)
        {
            RootBorder.Background = BrushFrom(theme.Bg);
            HeaderBorder.Background = BrushFrom(theme.Header);
            SessionLabel.Foreground = BrushFrom(theme.Accent);
            TodayLabel.Foreground = BrushFrom(theme.Muted);
            ProjectLabel.Foreground = BrushFrom(theme.Fg);
            FileLabel.Foreground = BrushFrom(theme.Muted);
            QuoteBorder.Background = BrushFrom(theme.QuoteBg);
            QuoteLabel.Foreground = BrushFrom(theme.Muted);

            // Accent badge tint
            var accentCol = (System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(theme.Accent);
            accentCol.A = 40;
            IdeBadge.Background = new SolidColorBrush(accentCol);

            // Pixel cat recolour
            var catBrush = BrushFrom(theme.CatFill);
            var earBrush = BrushFrom(theme.EarFill);
            CatBody.Fill = catBrush;
            CatHead.Fill = catBrush;
            CatTail.Fill = catBrush;
            EarLeft.Fill = catBrush;
            EarRight.Fill = catBrush;
            PawLeft.Fill = catBrush;
            PawRight.Fill = catBrush;
            InnerEarLeft.Fill = earBrush;
            InnerEarRight.Fill = earBrush;
            EyeLeft.Fill = earBrush;
            EyeRight.Fill = earBrush;
            CatNose.Fill = earBrush;
            MouthPath.Stroke = earBrush;
        }

        private static SolidColorBrush BrushFrom(string hex)
        {
            try
            {
                return new SolidColorBrush(
                    (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
            catch { return System.Windows.Media.Brushes.Transparent; }
        }

        // ── IDE polling ──────────────────────────────────────────────────────

        private record IdeInfo(string Name, string Color, string Project, string File, string Icon);

        private IdeInfo? Detect()
        {
            var procs = Process.GetProcesses();

            // VS Code
            var vsc = procs.FirstOrDefault(p =>
                p.ProcessName.Equals("Code", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.MainWindowTitle));
            if (vsc != null) return ParseVSCode(vsc.MainWindowTitle);

            // Visual Studio
            var vs = procs.FirstOrDefault(p =>
                p.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.MainWindowTitle));
            if (vs != null) return ParseVS(vs.MainWindowTitle);

            // JetBrains Rider
            var rider = procs.FirstOrDefault(p =>
                p.ProcessName.StartsWith("rider", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.MainWindowTitle));
            if (rider != null) return ParseRider(rider.MainWindowTitle);

            return null;
        }

        // "filename.ts — ProjectFolder — Visual Studio Code"
        private static IdeInfo ParseVSCode(string title)
        {
            var parts = title.Split(new[] { " — " }, StringSplitOptions.TrimEntries);
            string file = parts.Length > 0 ? parts[0].TrimStart('●', ' ') : "—";
            string project = parts.Length > 1 ? parts[1] : "—";
            if (project.EndsWith(" - Visual Studio Code")) project = project[..^21];
            return new IdeInfo("VS Code", "#569CD6", project, file, "⌨");
        }

        // "filename.cs - ProjectName - Microsoft Visual Studio 2022"
        private static IdeInfo ParseVS(string title)
        {
            var parts = title.Split(new[] { " - " }, StringSplitOptions.TrimEntries);
            string file = parts.Length > 0 ? parts[0] : "—";
            string project = parts.Length > 1 ? parts[1] : "—";
            return new IdeInfo("Visual Studio", "#C8A0D6", project, file, "🟣");
        }

        // "ProjectName – filename.ext – JetBrains Rider 2024.3"
        private static IdeInfo ParseRider(string title)
        {
            var parts = title.Split(new[] { " – " }, StringSplitOptions.TrimEntries);
            string project = parts.Length > 0 ? parts[0] : "—";
            string file = parts.Length > 1 ? parts[1] : "—";
            return new IdeInfo("Rider", "#EF4C6C", project, file, "🔴");
        }

        // ── Timers ───────────────────────────────────────────────────────────

        private void StartTimers()
        {
            // Poll IDE
            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_settings.PollIntervalSeconds)
            };
            _pollTimer.Tick += (_, _) => PollIde();
            _pollTimer.Start();
            PollIde(); // immediate first hit

            // Clock / session counter
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => UpdateClock();
            _clockTimer.Start();

            // Quote rotation
            _quoteTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_settings.QuoteIntervalSeconds)
            };
            _quoteTimer.Tick += (_, _) => NextQuote();
            _quoteTimer.Start();
        }

        private void PollIde()
        {
            var ide = Detect();
            _isActive = ide != null;

            if (ide != null)
            {
                IdeNameLabel.Text = ide.Name;
                IdeNameLabel.Foreground = new SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ide.Color));
                ProjectLabel.Text = ide.Project;
                FileLabel.Text = ide.File;
                IdeIcon.Text = ide.Icon;
            }
            else
            {
                IdeNameLabel.Text = "not coding";
                IdeNameLabel.Foreground = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(120, 120, 120, 160));
                ProjectLabel.Text = "—";
                FileLabel.Text = "—";
                IdeIcon.Text = "🐱";
            }
        }

        private void UpdateClock()
        {
            if (_isActive) _settings.TodaySeconds++;

            var session = DateTime.Now - _sessionStart;
            var today = TimeSpan.FromSeconds(_settings.TodaySeconds);

            SessionLabel.Text = $"⏱ {session:hh\\:mm\\:ss}";
            TodayLabel.Text = $"today {today:hh\\:mm}";
        }

        private void ShowQuote()
        {
            QuoteLabel.Text = Quotes[_quoteIndex % Quotes.Length];
        }

        private void NextQuote()
        {
            _quoteIndex = (_quoteIndex + 1) % Quotes.Length;
            ShowQuote();
        }

        // ── Tray icon ────────────────────────────────────────────────────────

        private const string AppName = "CodingPresence";

        private void InitTray()
        {
            _tray = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Coding Presence"
            };

            var startupItem = new WinForms.ToolStripMenuItem("Start with Windows")
            {
                Checked = IsStartupEnabled(),
                CheckOnClick = true
            };
            startupItem.CheckedChanged += (_, _) => SetStartup(startupItem.Checked);

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Next Quote", null, (_, _) => NextQuote());
            menu.Items.Add("Appearance…", null, (_, _) => OpenAppearance());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(startupItem);
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => CloseApp());
            _tray.ContextMenuStrip = menu;
        }

        private void OpenAppearance()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero) { GetWindowRect(hwnd, out RECT r); _settings.WindowX = r.Left; _settings.WindowY = r.Top; }
            var win = new AppearanceWindow(_settings);
            win.SettingsApplied += s => { _settings = s; SettingsStore.Save(s); ApplySettings(s); };
            win.Show();
        }

        internal void ApplySettings(PresenceSettings s)
        {
            Opacity = s.Opacity;
            Width = s.WidgetWidth;

            // Theme
            ApplyTheme(ColorTheme.Get(s.ColorScheme));

            // Mascot: pixel cat vs custom GIF
            bool useGif = s.UseGif && !string.IsNullOrEmpty(s.GifPath) && File.Exists(s.GifPath);
            CatCanvas.Visibility = (s.ShowCat && !useGif) ? Visibility.Visible : Visibility.Collapsed;
            GifMascot.Visibility = (s.ShowCat && useGif) ? Visibility.Visible : Visibility.Collapsed;
            CatQuoteRow.Visibility = (s.ShowCat || s.ShowQuote) ? Visibility.Visible : Visibility.Collapsed;
            QuoteLabel.Visibility = s.ShowQuote ? Visibility.Visible : Visibility.Collapsed;

            if (s.ShowCat && useGif && s.GifPath != _currentGifPath)
                LoadGif(s.GifPath);
            else if (!useGif)
            {
                _gifTimer?.Stop();
                _currentGifPath = string.Empty;
            }

            _pollTimer.Interval = TimeSpan.FromSeconds(s.PollIntervalSeconds);
            _quoteTimer.Interval = TimeSpan.FromSeconds(s.QuoteIntervalSeconds);

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero && s.WindowX >= 0)
                SetWindowPos(hwnd, IntPtr.Zero, (int)s.WindowX, (int)s.WindowY, 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private void CloseApp()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero) { GetWindowRect(hwnd, out RECT r); _settings.WindowX = r.Left; _settings.WindowY = r.Top; }
            SettingsStore.Save(_settings);
            _tray.Visible = false; _tray.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private bool IsStartupEnabled()
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return k?.GetValue(AppName) != null;
        }
        private void SetStartup(bool on)
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (on) k?.SetValue(AppName, $"\"{Process.GetCurrentProcess().MainModule!.FileName}\"");
            else k?.DeleteValue(AppName, false);
        }

        // ── Desktop pinning ──────────────────────────────────────────────────

        private void PinToDesktop()
        {
            IntPtr pm = FindWindow("Progman", null);
            SendMessageTimeout(pm, 0x052C, UIntPtr.Zero, IntPtr.Zero, 0, 1000, out _);
            IntPtr ww = IntPtr.Zero;
            EnumWindows((h, _) =>
            {
                if (FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                    ww = FindWindowEx(IntPtr.Zero, h, "WorkerW", null);
                return true;
            }, IntPtr.Zero);
            if (ww != IntPtr.Zero) SetParent(new WindowInteropHelper(this).Handle, ww);
        }

        // ── Manual drag ──────────────────────────────────────────────────────

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragOrigin = PointToScreen(e.GetPosition(this));
            GetWindowRect(new WindowInteropHelper(this).Handle, out RECT r);
            _winLeft = r.Left; _winTop = r.Top;
            CaptureMouse();
        }
        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pt = PointToScreen(e.GetPosition(this));
            SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero,
                _winLeft + (int)(pt.X - _dragOrigin.X),
                _winTop + (int)(pt.Y - _dragOrigin.Y),
                0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        { _isDragging = false; ReleaseMouseCapture(); }

        // ── P/Invoke ─────────────────────────────────────────────────────────

        private delegate bool EnumWndProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string c, string? w);
        [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr p, IntPtr a, string c, string? w);
        [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr h, uint m, UIntPtr wp, IntPtr lp, uint f, uint t, out UIntPtr r);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWndProc f, IntPtr l);
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr c, IntPtr p);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
        private const uint SWP_NOSIZE = 0x01, SWP_NOZORDER = 0x04, SWP_NOACTIVATE = 0x10;
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    }
}
