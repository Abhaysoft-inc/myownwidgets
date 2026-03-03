using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TimetableWidget
{
    public static class TimetableStore
    {
        private static readonly string DataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TimetableWidget", "timetable.json");

        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        public static Dictionary<string, List<Lecture>> Load()
        {
            try
            {
                if (File.Exists(DataPath))
                {
                    var json = File.ReadAllText(DataPath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, List<Lecture>>>(json, JsonOpts);
                    if (data != null) return data;
                }
            }
            catch { }
            return GetDefault();
        }

        public static void Save(Dictionary<string, List<Lecture>> data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath)!);
            File.WriteAllText(DataPath, JsonSerializer.Serialize(data, JsonOpts));
        }

        public static Dictionary<string, List<Lecture>> GetDefault() =>
            new()
            {
                ["MON"] = new()
                {
                    new() { Time = "9:30 – 10:20 AM", Subject = "Maths-IV" },
                    new() { Time = "10:20 – 11:10 AM", Subject = "TC" },
                    new() { Time = "11:10 – 12:00 PM", Subject = "Python" },
                    new() { Time = "12:00 – 12:50 PM", Subject = "Digital" },
                    new() { Time = "2:00 – 3:40 PM",   Subject = "Digital Lab 🧪" }
                },
                ["TUE"] = new()
                {
                    new() { Time = "9:30 – 10:20 AM",  Subject = "Digital" },
                    new() { Time = "10:20 – 11:10 AM", Subject = "Maths-IV" },
                    new() { Time = "11:10 – 12:00 PM", Subject = "Network" },
                    new() { Time = "12:00 – 12:50 PM", Subject = "MPC" }
                },
                ["WED"] = new()
                {
                    new() { Time = "9:30 – 10:20 AM",  Subject = "Maths-IV" },
                    new() { Time = "10:20 – 11:10 AM", Subject = "Python" },
                    new() { Time = "11:10 – 12:50 PM", Subject = "Machine" },
                    new() { Time = "2:00 – 2:50 PM",   Subject = "TC" },
                    new() { Time = "2:50 – 4:30 PM",   Subject = "Machine Lab 🧪" }
                },
                ["THU"] = new()
                {
                    new() { Time = "9:30 – 11:10 AM",  Subject = "Network Lab 🧪" },
                    new() { Time = "11:10 – 12:00 PM", Subject = "Machine" },
                    new() { Time = "12:00 – 12:50 PM", Subject = "Maths-IV" },
                    new() { Time = "3:40 – 4:30 PM",   Subject = "MPC" }
                },
                ["FRI"] = new()
                {
                    new() { Time = "9:30 – 10:20 AM",  Subject = "Digital" },
                    new() { Time = "10:20 – 11:10 AM", Subject = "Network" },
                    new() { Time = "11:10 – 12:00 PM", Subject = "Network" },
                    new() { Time = "12:00 – 12:50 PM", Subject = "TC" }
                },
                ["SAT"] = new()
                {
                    new() { Time = "9:30 – 10:20 AM",  Subject = "Machine" },
                    new() { Time = "10:20 – 11:10 AM", Subject = "Digital" },
                    new() { Time = "11:10 – 12:00 PM", Subject = "MPC" },
                    new() { Time = "12:00 – 12:50 PM", Subject = "MPC" }
                },
                ["SUN"] = new()
            };
    }
}
