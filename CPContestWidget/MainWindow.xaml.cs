using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace CPContestWidget;

public partial class MainWindow : Window
{
    private const string AppName = "CPContestWidget";

    private readonly ContestApiClient _contestApiClient = new();
    private WinForms.NotifyIcon _notifyIcon = null!;
    private AppSettings _settings;

    private bool _isDragging;
    private System.Windows.Point _dragOrigin;
    private int _winLeft;
    private int _winTop;

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsStore.Load();
        InitializeNotifyIcon();
        ApplyStartupPosition();

        SetDateRangeLabel(Math.Clamp(_settings.ContestDays, 1, 30));
        _ = RefreshContestsAsync();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PinToDesktop();
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "CP Contest Widget"
        };

        var startupItem = new WinForms.ToolStripMenuItem("Start with Windows")
        {
            Checked = IsStartupEnabled(),
            CheckOnClick = true
        };
        startupItem.CheckedChanged += (_, _) => SetStartup(startupItem.Checked);

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Refresh Now", null, async (_, _) => await RefreshContestsAsync());
        menu.Items.Add("Appearance", null, (_, _) => new AppearanceWindow().Show());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(startupItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => CloseApp());

        _notifyIcon.ContextMenuStrip = menu;
    }

    private async Task RefreshContestsAsync()
    {
        _settings = SettingsStore.Load();
        var dayCount = Math.Clamp(_settings.ContestDays, 1, 30);
        SetDateRangeLabel(dayCount);

        ContestList.ItemsSource = new List<ContestDisplayItem>
        {
            new()
            {
                DayAndTime = DateTime.Now.ToString("ddd HH:mm", CultureInfo.InvariantCulture),
                Platform = "Loading",
                Name = "Fetching contests..."
            }
        };

        try
        {
            var contests = await _contestApiClient.GetNextDaysAsync(dayCount);

            var selectedPlatforms = (_settings.TrackedPlatforms ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim().ToLowerInvariant())
                .ToHashSet();

            if (selectedPlatforms.Count > 0)
            {
                contests = contests
                    .Where(c => selectedPlatforms.Contains(c.Platform.Trim().ToLowerInvariant()))
                    .ToList();
            }

            if (contests.Count == 0)
            {
                ContestList.ItemsSource = new List<ContestDisplayItem>
                {
                    new()
                    {
                        DayAndTime = DateTime.Now.ToString("ddd", CultureInfo.InvariantCulture),
                        Platform = "No contests",
                        Name = $"No contests in the next {dayCount} day(s) for selected platforms."
                    }
                };
                return;
            }

            ContestList.ItemsSource = contests.Select(ToDisplayItem).ToList();
        }
        catch (Exception ex)
        {
            ContestList.ItemsSource = new List<ContestDisplayItem>
            {
                new()
                {
                    DayAndTime = DateTime.Now.ToString("ddd", CultureInfo.InvariantCulture),
                    Platform = "Error",
                    Name = $"Failed to load contests: {ex.Message}"
                }
            };
        }
    }

    private static ContestDisplayItem ToDisplayItem(ContestItem contest)
    {
        var duration = contest.DurationSeconds.GetValueOrDefault() > 0
            ? $" ({TimeSpan.FromSeconds(contest.DurationSeconds!.Value):h\\:mm})"
            : string.Empty;

        return new ContestDisplayItem
        {
            DayAndTime = contest.Start.ToString("ddd, dd MMM HH:mm", CultureInfo.InvariantCulture),
            Platform = contest.Platform,
            Name = $"{contest.Name}{duration}"
        };
    }

    private void SetDateRangeLabel(int dayCount)
    {
        var start = DateTime.Today;
        var end = start.AddDays(dayCount - 1);
        DateRangeText.Text = $"{start:dd MMM} - {end:dd MMM} ({dayCount} day(s) incl. today)";
    }

    public async Task TriggerRefreshAsync()
    {
        await RefreshContestsAsync();
    }

    private void CloseApp()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue(AppName) != null;
    }

    private void SetStartup(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (enable)
        {
            key?.SetValue(AppName, $"\"{Process.GetCurrentProcess().MainModule!.FileName}\"");
        }
        else
        {
            key?.DeleteValue(AppName, false);
        }
    }

    private void PinToDesktop()
    {
        var progman = FindWindow("Progman", null);
        SendMessageTimeout(progman, 0x052C, UIntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

        var workerW = IntPtr.Zero;
        EnumWindows((hWnd, _) =>
        {
            if (FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
            {
                workerW = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
            }

            return true;
        }, IntPtr.Zero);

        if (workerW != IntPtr.Zero)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetParent(hwnd, workerW);
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragOrigin = PointToScreen(e.GetPosition(this));

        var hwnd = new WindowInteropHelper(this).Handle;
        GetWindowRect(hwnd, out var r);
        _winLeft = r.Left;
        _winTop = r.Top;

        CaptureMouse();
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var screenPt = PointToScreen(e.GetPosition(this));
        var dx = (int)(screenPt.X - _dragOrigin.X);
        var dy = (int)(screenPt.Y - _dragOrigin.Y);

        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowPos(hwnd, IntPtr.Zero, _winLeft + dx, _winTop + dy, 0, 0,
            SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();

        _settings.WidgetLeft = Left;
        _settings.WidgetTop = Top;
        SettingsStore.Save(_settings);
    }

    private void ApplyStartupPosition()
    {
        if (_settings.WidgetLeft.HasValue && _settings.WidgetTop.HasValue)
        {
            Left = _settings.WidgetLeft.Value;
            Top = _settings.WidgetTop.Value;
            return;
        }

        Left = SystemParameters.WorkArea.Right - Width - 20;
        Top = 40;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern IntPtr FindWindow(string cls, string? win);
    [DllImport("user32.dll")] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string? win);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam, uint flags, uint timeout, out UIntPtr result);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

public sealed class ContestDisplayItem
{
    public string DayAndTime { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
