using System;
using System.IO;
using System.Text.Json;

namespace ScreenWidget
{
    public static class SettingsStore
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenWidget", "settings.json");

        public static CameraSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                    return JsonSerializer.Deserialize<CameraSettings>(File.ReadAllText(_path))
                           ?? new CameraSettings();
            }
            catch { }
            return new CameraSettings();
        }

        public static void Save(CameraSettings s)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path,
                JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>Folder where captured images are stored.</summary>
        public static string CaptureFolder()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "ScreenWidget");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
