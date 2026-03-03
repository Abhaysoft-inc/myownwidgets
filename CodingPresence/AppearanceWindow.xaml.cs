using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace CodingPresence
{
    public partial class AppearanceWindow : System.Windows.Window
    {
        public event Action<PresenceSettings>? SettingsApplied;
        private PresenceSettings _s;
        private string _selectedTheme;
        private string _gifPath;

        public AppearanceWindow(PresenceSettings settings)
        {
            InitializeComponent();
            _s = settings;
            _selectedTheme = settings.ColorScheme;
            _gifPath = settings.GifPath;

            OpacitySlider.Value = _s.Opacity;
            WidthSlider.Value = _s.WidgetWidth;
            PollSlider.Value = _s.PollIntervalSeconds;
            QuoteSlider.Value = _s.QuoteIntervalSeconds;
            ShowCatCheck.IsChecked = _s.ShowCat;
            ShowQuoteCheck.IsChecked = _s.ShowQuote;
            UseGifCheck.IsChecked = _s.UseGif;
            GifPathLabel.Text = string.IsNullOrEmpty(_gifPath)
                ? "no GIF selected" : System.IO.Path.GetFileName(_gifPath);

            PosXSlider.Maximum = SystemParameters.VirtualScreenWidth;
            PosYSlider.Maximum = SystemParameters.VirtualScreenHeight;
            PosXSlider.Value = Math.Max(0, _s.WindowX);
            PosYSlider.Value = Math.Max(0, _s.WindowY);

            BuildThemeSwatches();
        }

        // ── Theme swatches ────────────────────────────────────────────────────

        private Border? _selectedSwatch;

        private void BuildThemeSwatches()
        {
            foreach (var theme in ColorTheme.All)
            {
                var outer = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    BorderThickness = new Thickness(theme.Name == _selectedTheme ? 2 : 0),
                    BorderBrush = System.Windows.Media.Brushes.White,
                    ToolTip = theme.Name,
                    Background = BrushFrom(theme.Bg)
                };

                // Accent dot
                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = BrushFrom(theme.Accent),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                outer.Child = dot;

                if (theme.Name == _selectedTheme) _selectedSwatch = outer;

                var t = theme; // capture
                outer.MouseLeftButtonDown += (_, _) =>
                {
                    if (_selectedSwatch != null)
                        _selectedSwatch.BorderThickness = new Thickness(0);
                    outer.BorderThickness = new Thickness(2);
                    _selectedSwatch = outer;
                    _selectedTheme = t.Name;
                    SettingsApplied?.Invoke(Build());
                };

                ThemePanel.Children.Add(outer);
            }
        }

        // ── GIF picker ────────────────────────────────────────────────────────

        private void BrowseGif_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a GIF",
                Filter = "GIF files (*.gif)|*.gif|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _gifPath = dlg.FileName;
                GifPathLabel.Text = System.IO.Path.GetFileName(_gifPath);
                UseGifCheck.IsChecked = true;
                SettingsApplied?.Invoke(Build());
            }
        }

        // ── Handlers ─────────────────────────────────────────────────────────

        private PresenceSettings Build() => new PresenceSettings
        {
            Opacity = OpacitySlider.Value,
            WidgetWidth = (int)WidthSlider.Value,
            PollIntervalSeconds = (int)PollSlider.Value,
            QuoteIntervalSeconds = (int)QuoteSlider.Value,
            ShowCat = ShowCatCheck.IsChecked == true,
            ShowQuote = ShowQuoteCheck.IsChecked == true,
            UseGif = UseGifCheck.IsChecked == true,
            GifPath = _gifPath,
            ColorScheme = _selectedTheme,
            WindowX = (int)PosXSlider.Value,
            WindowY = (int)PosYSlider.Value,
            TodaySeconds = _s.TodaySeconds,
            TodayDate = _s.TodayDate,
        };

        private void Slider_Changed(object sender, RoutedEventArgs e)
            => SettingsApplied?.Invoke(Build());
        private void Toggle_Changed(object sender, RoutedEventArgs e)
            => SettingsApplied?.Invoke(Build());

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SettingsApplied?.Invoke(Build());
            Close();
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SolidColorBrush BrushFrom(string hex)
        {
            try
            {
                return new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            }
            catch { return System.Windows.Media.Brushes.Transparent; }
        }
    }
}
