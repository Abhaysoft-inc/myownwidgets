using System;
using System.Windows;
using System.Windows.Input;

namespace ScreenWidget
{
    public partial class SettingsWindow : Window
    {
        private readonly CameraSettings _original;
        private CameraSettings _working;
        private readonly MainWindow _main;

        public event Action<CameraSettings>? SettingsApplied;

        public SettingsWindow(CameraSettings current, MainWindow main)
        {
            InitializeComponent();
            _main = main;
            _original = Clone(current);
            _working = Clone(current);

            // Clamp position sliders to actual screen \
            PosXSlider.Maximum = SystemParameters.VirtualScreenWidth;
            PosYSlider.Maximum = SystemParameters.VirtualScreenHeight;

            // Init controls (order matters – set values before any ValueChanged fires)
            CameraIndexSlider.Value = _working.CameraIndex;
            CameraIndexLabel.Text = _working.CameraIndex.ToString();

            IntervalSlider.Value = _working.CaptureIntervalMinutes;
            IntervalLabel.Text = $"{_working.CaptureIntervalMinutes} min";

            OpacitySlider.Value = _working.Opacity;
            OpacityLabel.Text = $"{(int)(_working.Opacity * 100)}%";

            WidthSlider.Value = _working.WidgetWidth;
            WidthLabel.Text = $"{(int)_working.WidgetWidth}px";

            RadiusSlider.Value = _working.CornerRadius;
            RadiusLabel.Text = $"{(int)_working.CornerRadius}";

            BorderThicknessSlider.Value = _working.BorderThickness;
            BorderThicknessLabel.Text = $"{(int)_working.BorderThickness}";

            BorderSpeedSlider.Value = _working.BorderSpeed;
            BorderSpeedLabel.Text = $"{_working.BorderSpeed:0.0}s";

            double x = _working.WindowX < 0 ? 0 : _working.WindowX;
            double y = _working.WindowY < 0 ? 0 : _working.WindowY;
            PosXSlider.Value = x;
            PosXLabel.Text = $"{(int)x}";
            PosYSlider.Value = y;
            PosYLabel.Text = $"{(int)y}";
        }

        // ── Capture buttons ───────────────────────────────────────────────────

        private void CaptureNowBtn_Click(object sender, RoutedEventArgs e)
        {
            CaptureNowBtn.IsEnabled = false;
            CaptureNowBtn.Content = "⏳  Capturing…";
            System.Threading.Tasks.Task.Run(() =>
            {
                _main.CaptureNow();
                Dispatcher.Invoke(() =>
                {
                    CaptureNowBtn.IsEnabled = true;
                    CaptureNowBtn.Content = "📷  Capture Now";
                });
            });
        }

        private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
            => System.Diagnostics.Process.Start("explorer.exe", SettingsStore.CaptureFolder());

        // ── Slider handlers ───────────────────────────────────────────────────

        private void CameraIndexSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CameraIndexLabel == null) return;
            _working.CameraIndex = (int)e.NewValue;
            CameraIndexLabel.Text = _working.CameraIndex.ToString();
        }

        private void IntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IntervalLabel == null) return;
            _working.CaptureIntervalMinutes = (int)e.NewValue;
            IntervalLabel.Text = $"{(int)e.NewValue} min";
            Preview();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityLabel == null) return;
            _working.Opacity = e.NewValue;
            OpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
            Preview();
        }

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (WidthLabel == null) return;
            _working.WidgetWidth = e.NewValue;
            WidthLabel.Text = $"{(int)e.NewValue}px";
            Preview();
        }

        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RadiusLabel == null) return;
            _working.CornerRadius = e.NewValue;
            RadiusLabel.Text = $"{(int)e.NewValue}";
            Preview();
        }

        private void BorderThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BorderThicknessLabel == null) return;
            _working.BorderThickness = e.NewValue;
            BorderThicknessLabel.Text = $"{(int)e.NewValue}";
            Preview();
        }

        private void BorderSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BorderSpeedLabel == null) return;
            _working.BorderSpeed = e.NewValue;
            BorderSpeedLabel.Text = $"{e.NewValue:0.0}s";
            Preview();
        }

        private void PosXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PosXLabel == null) return;
            _working.WindowX = e.NewValue;
            PosXLabel.Text = $"{(int)e.NewValue}";
            Preview();
        }

        private void PosYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PosYLabel == null) return;
            _working.WindowY = e.NewValue;
            PosYLabel.Text = $"{(int)e.NewValue}";
            Preview();
        }

        // ── Buttons ───────────────────────────────────────────────────────────

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsApplied?.Invoke(_working);
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsApplied?.Invoke(_original);
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        // ── Helpers ───────────────────────────────────────────────────────────

        private void Preview() => SettingsApplied?.Invoke(_working);

        private static CameraSettings Clone(CameraSettings s) => new()
        {
            CameraIndex = s.CameraIndex,
            CaptureIntervalMinutes = s.CaptureIntervalMinutes,
            Opacity = s.Opacity,
            WidgetWidth = s.WidgetWidth,
            WindowX = s.WindowX,
            WindowY = s.WindowY,
            BorderThickness = s.BorderThickness,
            CornerRadius = s.CornerRadius,
            BorderSpeed = s.BorderSpeed,
            LastImagePath = s.LastImagePath
        };
    }
}
