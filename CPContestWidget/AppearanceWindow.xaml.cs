using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CPContestWidget;

public partial class AppearanceWindow : Window
{
    private static readonly string[] KnownPlatforms =
    [
        "codeforces", "codechef", "leetcode", "atcoder", "hackerrank", "hackerearth", "geeksforgeeks"
    ];

    private readonly AppSettings _original;
    private AppSettings _working;
    private readonly Dictionary<string, System.Windows.Controls.CheckBox> _platformChecks = new(StringComparer.OrdinalIgnoreCase);
    private readonly MainWindow? _widget;
    private bool _isInitializingPosition;
    private double _originalLeft;
    private double _originalTop;

    public AppearanceWindow()
    {
        InitializeComponent();
        _widget = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
        _original = SettingsStore.Load();
        _working = Clone(_original);

        OpacitySlider.Value = _working.BgOpacity;
        OpacityLabel.Text = $"{(int)(_working.BgOpacity * 100)}%";
        InitializePositionSliders();
        ContestDaysSlider.Value = Math.Clamp(_working.ContestDays, 1, 30);
        ContestDaysLabel.Text = $"{ContestDaysSlider.Value:F0} days";
        BuildPlatformChecks();

        BuildPresetSwatches();
    }

    private void InitializePositionSliders()
    {
        var workArea = SystemParameters.WorkArea;
        XSlider.Maximum = Math.Max(0, workArea.Right - 120);
        YSlider.Maximum = Math.Max(0, workArea.Bottom - 120);

        _originalLeft = _widget?.Left ?? _working.WidgetLeft ?? 0;
        _originalTop = _widget?.Top ?? _working.WidgetTop ?? 40;

        _isInitializingPosition = true;
        XSlider.Value = Math.Clamp(_originalLeft, XSlider.Minimum, XSlider.Maximum);
        YSlider.Value = Math.Clamp(_originalTop, YSlider.Minimum, YSlider.Maximum);
        _isInitializingPosition = false;

        UpdatePositionLabels();
    }

    private void BuildPlatformChecks()
    {
        var selected = (_working.TrackedPlatforms ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToLowerInvariant())
            .ToHashSet();

        var checkAllByDefault = selected.Count == 0;

        foreach (var platform in KnownPlatforms)
        {
            var check = new System.Windows.Controls.CheckBox
            {
                Content = platform,
                Margin = new Thickness(0, 0, 12, 8),
                Foreground = BrushFrom("#E0E0F0"),
                IsChecked = checkAllByDefault || selected.Contains(platform)
            };

            _platformChecks[platform] = check;
            PlatformPanel.Children.Add(check);
        }
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

        outer.MouseLeftButtonDown += (_, _) =>
        {
            var currentLeft = _working.WidgetLeft;
            var currentTop = _working.WidgetTop;
            var currentPlatforms = _working.TrackedPlatforms?.ToList() ?? [];
            var currentContestDays = _working.ContestDays;

            _working = Clone(preset);
            _working.BgOpacity = OpacitySlider.Value;
            _working.WidgetLeft = currentLeft;
            _working.WidgetTop = currentTop;
            _working.TrackedPlatforms = currentPlatforms;
            _working.ContestDays = currentContestDays;
            PreviewTheme(_working);
        };

        return outer;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityLabel == null)
        {
            return;
        }

        _working.BgOpacity = e.NewValue;
        OpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
        PreviewTheme(_working);
    }

    private static void PreviewTheme(AppSettings s) => App.ApplyTheme(s);

    private async void ApplyBtn_Click(object sender, RoutedEventArgs e)
    {
        var left = XSlider.Value;
        var top = YSlider.Value;

        _working.WidgetLeft = left;
        _working.WidgetTop = top;
        _working.ContestDays = (int)ContestDaysSlider.Value;
        _working.TrackedPlatforms = _platformChecks
            .Where(kvp => kvp.Value.IsChecked == true)
            .Select(kvp => kvp.Key)
            .ToList();

        SettingsStore.Save(_working);

        if (_widget != null)
        {
            _widget.Left = left;
            _widget.Top = top;
            await _widget.TriggerRefreshAsync();
        }

        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        App.ApplyTheme(_original);

        if (_widget != null)
        {
            _widget.Left = _originalLeft;
            _widget.Top = _originalTop;
        }

        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void UseCurrentBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_widget == null)
        {
            return;
        }

        _isInitializingPosition = true;
        XSlider.Value = Math.Clamp(_widget.Left, XSlider.Minimum, XSlider.Maximum);
        YSlider.Value = Math.Clamp(_widget.Top, YSlider.Minimum, YSlider.Maximum);
        _isInitializingPosition = false;
        UpdatePositionLabels();
    }

    private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdatePositionLabels();

        if (_isInitializingPosition || _widget == null)
        {
            return;
        }

        _widget.Left = XSlider.Value;
        _widget.Top = YSlider.Value;
    }

    private void UpdatePositionLabels()
    {
        if (XLabel != null)
        {
            XLabel.Text = $"X: {XSlider.Value:F0}";
        }

        if (YLabel != null)
        {
            YLabel.Text = $"Y: {YSlider.Value:F0}";
        }
    }

    private void ContestDaysSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ContestDaysLabel == null)
        {
            return;
        }

        ContestDaysLabel.Text = $"{e.NewValue:F0} days";
        _working.ContestDays = (int)e.NewValue;
    }

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
        InputBg = s.InputBg,
        WidgetLeft = s.WidgetLeft,
        WidgetTop = s.WidgetTop,
        TrackedPlatforms = s.TrackedPlatforms?.ToList() ?? [],
        ContestDays = s.ContestDays
    };

    private static SolidColorBrush BrushFrom(string hex, double opacity = 1.0)
    {
        try
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(c.A * opacity), c.R, c.G, c.B));
        }
        catch
        {
            return new SolidColorBrush(Colors.Transparent);
        }
    }
}
