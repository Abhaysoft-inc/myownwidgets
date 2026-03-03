using System;
using System.IO;
using System.Text.Json;

namespace TimetableWidget
{
    public static class SettingsStore
    {
        private static readonly string Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimetableWidget", "settings.json");

        private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(Path))
                {
                    var json = File.ReadAllText(Path);
                    return JsonSerializer.Deserialize<AppSettings>(json, Opts) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings s)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, JsonSerializer.Serialize(s, Opts));
        }
    }
}
