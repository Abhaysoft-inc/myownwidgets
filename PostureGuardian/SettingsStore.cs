using System;
using System.IO;
using System.Text.Json;

namespace PostureGuardian
{
    public static class SettingsStore
    {
        private static readonly string _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PostureGuardian");

        private static readonly string _path = Path.Combine(_dir, "settings.json");

        /// <summary>Path to the calibration reference image.</summary>
        public static string CalibrationPath => Path.Combine(_dir, "calibration.png");

        public static PostureSettings Load()
        {
            try
            {
                if (File.Exists(_path))
                    return JsonSerializer.Deserialize<PostureSettings>(
                        File.ReadAllText(_path)) ?? new PostureSettings();
            }
            catch { }
            return new PostureSettings();
        }

        public static void Save(PostureSettings s)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path,
                JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
