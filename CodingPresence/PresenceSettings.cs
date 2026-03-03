namespace CodingPresence
{
    public class PresenceSettings
    {
        public double Opacity { get; set; } = 0.92;
        public double WidgetWidth { get; set; } = 300;
        public double WindowX { get; set; } = -1;
        public double WindowY { get; set; } = -1;
        public bool ShowCat { get; set; } = true;
        public bool ShowQuote { get; set; } = true;
        public bool UseGif { get; set; } = false;
        public string GifPath { get; set; } = string.Empty;
        public string ColorScheme { get; set; } = "Catppuccin";
        /// <summary>Seconds between IDE polls.</summary>
        public int PollIntervalSeconds { get; set; } = 3;
        /// <summary>Seconds between quote rotations.</summary>
        public int QuoteIntervalSeconds { get; set; } = 60;
        /// <summary>Accumulated coding seconds for today (persisted on exit).</summary>
        public long TodaySeconds { get; set; } = 0;
        /// <summary>Date string yyyy-MM-dd the above was last recorded.</summary>
        public string TodayDate { get; set; } = string.Empty;
    }

    public class ColorTheme
    {
        public string Name { get; init; } = "";
        public string Bg { get; init; } = "";  // root border background
        public string Header { get; init; } = "";  // header tint
        public string Accent { get; init; } = "";  // badge / highlight
        public string Fg { get; init; } = "";  // primary text
        public string Muted { get; init; } = "";  // dim text
        public string QuoteBg { get; init; } = "";  // quote panel
        public string CatFill { get; init; } = "";  // pixel cat body
        public string EarFill { get; init; } = "";  // inner ear / eyes

        public static readonly ColorTheme[] All = new[]
        {
            new ColorTheme { Name="Catppuccin", Bg="#EE1E1E2E", Header="#22141428", Accent="#89B4FA", Fg="#CDD6F4", Muted="#A6ADC8", QuoteBg="#18334488", CatFill="#3D3D6E", EarFill="#FF88CC" },
            new ColorTheme { Name="Tokyo Night", Bg="#EE1A1B2E", Header="#221A1A30", Accent="#7AA2F7", Fg="#C0CAF5", Muted="#7982B4", QuoteBg="#18292E42", CatFill="#292E42", EarFill="#BB9AF7" },
            new ColorTheme { Name="Dracula",     Bg="#EE282A36", Header="#22201E30", Accent="#BD93F9", Fg="#F8F8F2", Muted="#6272A4", QuoteBg="#18443355", CatFill="#44475A", EarFill="#FF79C6" },
            new ColorTheme { Name="Sakura",      Bg="#EE1A0E18", Header="#22180C16", Accent="#FF9CAC", Fg="#F5C2E7", Muted="#C9A5C5", QuoteBg="#18441830", CatFill="#3D1A2E", EarFill="#FF9CAC" },
            new ColorTheme { Name="Nord",        Bg="#EE2E3440", Header="#22242930", Accent="#88C0D0", Fg="#ECEFF4", Muted="#4C566A", QuoteBg="#183B4252", CatFill="#3B4252", EarFill="#88C0D0" },
            new ColorTheme { Name="Gruvbox",     Bg="#EE28201A", Header="#22201810", Accent="#FABD2F", Fg="#EBDBB2", Muted="#928374", QuoteBg="#18504020", CatFill="#504038", EarFill="#FABD2F" },
            new ColorTheme { Name="Synthwave",   Bg="#EE12032A", Header="#22100022", Accent="#FF79C6", Fg="#E0C3FC", Muted="#B48EAD", QuoteBg="#18330044", CatFill="#2D0050", EarFill="#FF00FF" },
        };

        public static ColorTheme Get(string name)
        {
            foreach (var t in All)
                if (t.Name == name) return t;
            return All[0];
        }
    }
}

