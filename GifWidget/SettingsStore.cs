using System.IO;
using System.Text.Json;

namespace GifWidget
{
    public static class SettingsStore
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GifWidget", "settings.json");

        public static GifSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                    return JsonSerializer.Deserialize<GifSettings>(File.ReadAllText(_path)) ?? new GifSettings();
            }
            catch { }
            return new GifSettings();
        }

        public static void Save(GifSettings s)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
