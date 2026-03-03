using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace TimetableWidget
{
    public partial class MainWindow : Window
    {
        private WinForms.NotifyIcon _notifyIcon = null!;
        private bool _isDragging;
        private System.Windows.Point _dragOrigin;
        private int _winLeft, _winTop;

        public MainWindow()
        {
            InitializeComponent();
            LoadTodayLectures();
            InitializeNotifyIcon();
            // Place widget near top-right by default
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = 40;
        }

        private const string AppName = "TimetableWidget";

        private bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }

        private void SetStartup(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
                key?.SetValue(AppName, $"\"{System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName}\"");
            else
                key?.DeleteValue(AppName, false);
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Timetable Widget"
            };

            var startupItem = new WinForms.ToolStripMenuItem("Start with Windows")
            {
                Checked = IsStartupEnabled(),
                CheckOnClick = true
            };
            startupItem.CheckedChanged += (s, e) => SetStartup(startupItem.Checked);

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Edit Timetable", null, (s, e) => OpenEditor());
            menu.Items.Add("Appearance", null, (s, e) => OpenAppearance());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(startupItem);
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => CloseApp());
            _notifyIcon.ContextMenuStrip = menu;
        }

        private void CloseApp()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PinToDesktop();
        }

        // ── Embed window into the desktop WorkerW so Win+D never hides it ──
        private void PinToDesktop()
        {
            IntPtr progman = FindWindow("Progman", null);
            // Tell the shell to spawn a WorkerW behind desktop icons
            SendMessageTimeout(progman, 0x052C, UIntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

            IntPtr workerW = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                if (FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                    workerW = FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                return true;
            }, IntPtr.Zero);

            if (workerW != IntPtr.Zero)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                SetParent(hwnd, workerW);
            }
        }

        // ── Manual drag (DragMove() doesn't work for child windows) ──
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            var screenPt = PointToScreen(e.GetPosition(this));
            _dragOrigin = screenPt;
            var hwnd = new WindowInteropHelper(this).Handle;
            GetWindowRect(hwnd, out RECT r);
            _winLeft = r.Left;
            _winTop = r.Top;
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;
            var screenPt = PointToScreen(e.GetPosition(this));
            int dx = (int)(screenPt.X - _dragOrigin.X);
            int dy = (int)(screenPt.Y - _dragOrigin.Y);
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, IntPtr.Zero, _winLeft + dx, _winTop + dy, 0, 0,
                SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
        }

        // ── P/Invoke ──
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] static extern IntPtr FindWindow(string cls, string? win);
        [DllImport("user32.dll")] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string? win);
        [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam, uint flags, uint timeout, out UIntPtr result);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr newParent);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        public void Reload() => LoadTodayLectures();

        private void OpenEditor()
        {
            var editor = new EditWindow();
            editor.TimetableSaved += Reload;
            editor.Show();
        }

        private void OpenAppearance()
        {
            new AppearanceWindow().Show();
        }

        private void LoadTodayLectures()
        {
            DateTime today = DateTime.Now;
            string day = today.DayOfWeek.ToString().Substring(0, 3).ToUpper();

            DateText.Text = $"📅 {today:dddd, dd MMMM yyyy}";

            var timetable = TimetableStore.Load();

            if (timetable.TryGetValue(day, out var lectures) && lectures.Count > 0)
            {
                LectureList.ItemsSource = lectures;
            }
            else
            {
                LectureList.ItemsSource = new List<Lecture>
                {
                    new Lecture { Time = "-", Subject = "No Lectures Today 🎉" }
                };
            }
        }
    }

    public class Lecture
    {
        public string Time { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }
}