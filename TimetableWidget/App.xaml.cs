using System.Windows;
using System.Windows.Media;

namespace TimetableWidget;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme(SettingsStore.Load());
        new MainWindow().Show();
    }

    public static void ApplyTheme(AppSettings s)
    {
        var res = Current.Resources;
        res["WidgetBg"] = BrushFrom(s.BgColor, s.BgOpacity);
        res["WidgetFg"] = BrushFrom(s.FgColor);
        res["WidgetMuted"] = BrushFrom(s.MutedColor);
        res["WidgetAccent"] = BrushFrom(s.AccentColor);
        res["WidgetTime"] = BrushFrom(s.TimeColor);
        res["WidgetPanel"] = BrushFrom(s.PanelBg);
        res["WidgetInput"] = BrushFrom(s.InputBg);
    }

    public static SolidColorBrush BrushFrom(string hex, double opacity = 1.0)
    {
        try
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                (byte)(255 * opacity), c.R, c.G, c.B));
        }
        catch { return new SolidColorBrush(Colors.Transparent); }
    }
}

