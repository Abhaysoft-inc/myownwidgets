using System;
using System.Windows;
using System.Windows.Input;

namespace GifWidget
{
    public partial class AppearanceWindow : Window
    {
        private readonly GifSettings _original;
        private GifSettings _working;

        public event Action<GifSettings>? SettingsApplied;

        public AppearanceWindow(GifSettings current)
        {
            InitializeComponent();
            _original = Clone(current);
            _working = Clone(current);

            // Clamp position sliders to actual virtual screen size
            PosXSlider.Maximum = SystemParameters.VirtualScreenWidth;
            PosYSlider.Maximum = SystemParameters.VirtualScreenHeight;

            // Initialise controls without triggering ValueChanged side-effects
            OpacitySlider.Value = _working.Opacity;
            OpacityLabel.Text = $"{(int)(_working.Opacity * 100)}%";

            SizeSlider.Value = _working.WidgetWidth;
            SizeLabel.Text = $"{(int)_working.WidgetWidth}px";

            RadiusSlider.Value = _working.CornerRadius;
            RadiusLabel.Text = $"{(int)_working.CornerRadius}";

            BorderCheck.IsChecked = _working.ShowBorder;

            double x = _working.WindowX < 0 ? 0 : _working.WindowX;
            double y = _working.WindowY < 0 ? 0 : _working.WindowY;
            PosXSlider.Value = x;
            PosXLabel.Text = $"{(int)x}";
            PosYSlider.Value = y;
            PosYLabel.Text = $"{(int)y}";
        }

        // ── Slider handlers ───────────────────────────────────────────────────

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityLabel == null) return;
            _working.Opacity = e.NewValue;
            OpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
            Preview();
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SizeLabel == null) return;
            _working.WidgetWidth = e.NewValue;
            SizeLabel.Text = $"{(int)e.NewValue}px";
            Preview();
        }

        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RadiusLabel == null) return;
            _working.CornerRadius = e.NewValue;
            RadiusLabel.Text = $"{(int)e.NewValue}";
            Preview();
        }

        private void BorderCheck_Changed(object sender, RoutedEventArgs e)
        {
            _working.ShowBorder = BorderCheck.IsChecked == true;
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
            // Revert live preview
            SettingsApplied?.Invoke(_original);
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        // ── Helpers ───────────────────────────────────────────────────────────

        private void Preview() => SettingsApplied?.Invoke(_working);

        private static GifSettings Clone(GifSettings s) => new()
        {
            GifPath = s.GifPath,
            Opacity = s.Opacity,
            WidgetWidth = s.WidgetWidth,
            WindowX = s.WindowX,
            WindowY = s.WindowY,
            ShowBorder = s.ShowBorder,
            CornerRadius = s.CornerRadius
        };
    }
}
