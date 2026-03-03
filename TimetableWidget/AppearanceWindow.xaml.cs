using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimetableWidget
{
    public partial class AppearanceWindow : Window
    {
        private AppSettings _original;
        private AppSettings _working;

        public AppearanceWindow()
        {
            InitializeComponent();
            _original = SettingsStore.Load();
            _working = Clone(_original);

            OpacitySlider.Value = _working.BgOpacity;
            OpacityLabel.Text = $"{(int)(_working.BgOpacity * 100)}%";

            BuildPresetSwatches();
        }

        private void BuildPresetSwatches()
        {
            foreach (var preset in AppSettings.Presets)
            {
                var swatch = BuildSwatch(preset);
                PresetPanel.Children.Add(swatch);
            }
        }

        private StackPanel BuildSwatch(AppSettings preset)
        {
            var outer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Margin = new Thickness(0, 0, 12, 8),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = preset
            };

            var circle = new Ellipse
            {
                Width = 36,
                Height = 36,
                Fill = BrushFrom(preset.BgColor, 1.0),
                Stroke = BrushFrom(preset.AccentColor, 1.0),
                StrokeThickness = 3
            };

            // Inner dot showing accent
            var inner = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = BrushFrom(preset.AccentColor, 1.0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12)
            };

            var grid = new Grid { Width = 36, Height = 36 };
            grid.Children.Add(circle);
            grid.Children.Add(inner);

            var label = new TextBlock
            {
                Text = preset.ThemeName,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 10,
                Foreground = BrushFrom("#88AAAACC"),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            outer.Children.Add(grid);
            outer.Children.Add(label);

            outer.MouseLeftButtonDown += (s, e) =>
            {
                _working = Clone(preset);
                _working.BgOpacity = OpacitySlider.Value;
                PreviewTheme(_working);
                HighlightSelected(outer);
            };

            return outer;
        }

        private Border? _selectedBorder;

        private void HighlightSelected(StackPanel panel)
        {
            // Remove old highlight
            if (_selectedBorder != null)
                _selectedBorder.BorderThickness = new Thickness(0);

            // Wrap with a visible ring
            if (panel.Parent is Border b)
            {
                b.BorderBrush = BrushFrom("#C0D8FF");
                b.BorderThickness = new Thickness(2);
                _selectedBorder = b;
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityLabel == null) return;
            _working.BgOpacity = e.NewValue;
            OpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
            PreviewTheme(_working);
        }

        private static void PreviewTheme(AppSettings s)
            => App.ApplyTheme(s);

        private void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsStore.Save(_working);
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            App.ApplyTheme(_original); // revert live preview
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        // ── Helpers ──────────────────────────────────────────────────────────

        private static AppSettings Clone(AppSettings s) => new()
        {
            ThemeName = s.ThemeName,
            BgColor = s.BgColor,
            BgOpacity = s.BgOpacity,
            FgColor = s.FgColor,
            MutedColor = s.MutedColor,
            AccentColor = s.AccentColor,
            TimeColor = s.TimeColor,
            PanelBg = s.PanelBg,
            InputBg = s.InputBg
        };

        private static SolidColorBrush BrushFrom(string hex, double opacity = 1.0)
        {
            try
            {
                var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                    (byte)(c.A * opacity), c.R, c.G, c.B));
            }
            catch { return new SolidColorBrush(Colors.Transparent); }
        }
    }
}
