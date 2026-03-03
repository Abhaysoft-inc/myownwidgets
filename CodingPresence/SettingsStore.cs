using System;
using System.IO;
using System.Text.Json;

namespace CodingPresence
{
    public static class SettingsStore
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CodingPresence", "settings.json");

        public static PresenceSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                    return JsonSerializer.Deserialize<PresenceSettings>(
                        File.ReadAllText(_path)) ?? new PresenceSettings();
            }
            catch { }
            return new PresenceSettings();
        }

        public static void Save(PresenceSettings s)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path,
                JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
