using System.Windows;
using System.Windows.Media;

namespace CPContestWidget;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme(SettingsStore.Load());
        new MainWindow().Show();
    }

    public static void ApplyTheme(AppSettings settings)
    {
        var res = Current.Resources;
        res["WidgetBg"] = BrushFrom(settings.BgColor, settings.BgOpacity);
        res["WidgetFg"] = BrushFrom(settings.FgColor);
        res["WidgetMuted"] = BrushFrom(settings.MutedColor);
        res["WidgetAccent"] = BrushFrom(settings.AccentColor);
        res["WidgetTime"] = BrushFrom(settings.TimeColor);
        res["WidgetPanel"] = BrushFrom(settings.PanelBg);
        res["WidgetInput"] = BrushFrom(settings.InputBg);
    }

    public static SolidColorBrush BrushFrom(string hex, double opacity = 1.0)
    {
        try
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)(255 * opacity), c.R, c.G, c.B));
        }
        catch
        {
            return new SolidColorBrush(Colors.Transparent);
        }
    }
}
