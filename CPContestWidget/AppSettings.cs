namespace CPContestWidget;

public class AppSettings
{
    public string ThemeName { get; set; } = "Ocean";
    public string BgColor { get; set; } = "#1E1E2E";
    public double BgOpacity { get; set; } = 0.85;
    public string FgColor { get; set; } = "#E0E0F0";
    public string MutedColor { get; set; } = "#AAAACC";
    public string AccentColor { get; set; } = "#334488";
    public string TimeColor { get; set; } = "#7799BB";
    public string PanelBg { get; set; } = "#22222E";
    public string InputBg { get; set; } = "#2A2A3E";
    public double? WidgetLeft { get; set; }
    public double? WidgetTop { get; set; }
    public List<string> TrackedPlatforms { get; set; } = [];
    public int ContestDays { get; set; } = 3;

    public static AppSettings[] Presets =>
    [
        new AppSettings
        {
            ThemeName = "Ocean",
            BgColor = "#1E1E2E", BgOpacity = 0.85,
            FgColor = "#E0E0F0", MutedColor = "#AAAACC",
            AccentColor = "#334488", TimeColor = "#7799BB",
            PanelBg = "#22222E", InputBg = "#2A2A3E"
        },
        new AppSettings
        {
            ThemeName = "Forest",
            BgColor = "#1A2420", BgOpacity = 0.88,
            FgColor = "#D4ECD4", MutedColor = "#88AA88",
            AccentColor = "#2D5E3E", TimeColor = "#5A8C6A",
            PanelBg = "#1E2E24", InputBg = "#243028"
        },
        new AppSettings
        {
            ThemeName = "Sunset",
            BgColor = "#28180E", BgOpacity = 0.88,
            FgColor = "#F0DDD0", MutedColor = "#BB9988",
            AccentColor = "#884422", TimeColor = "#AA7755",
            PanelBg = "#2E1C10", InputBg = "#382014"
        },
        new AppSettings
        {
            ThemeName = "Paper",
            BgColor = "#F5F5F0", BgOpacity = 0.95,
            FgColor = "#2C2C3A", MutedColor = "#888899",
            AccentColor = "#3355CC", TimeColor = "#5577AA",
            PanelBg = "#EBEBE5", InputBg = "#E0E0DA"
        }
    ];
}
