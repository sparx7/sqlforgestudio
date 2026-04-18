using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SqlForgeWpf.Models
{
    public static class ProfileManager
    {
        private static readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connections.json");

        public static List<ConnectionProfile> LoadProfiles()
        {
            if (!File.Exists(_filePath)) return new List<ConnectionProfile>();
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ConnectionProfile>>(json) ?? new List<ConnectionProfile>();
        }

        public static void SaveProfiles(List<ConnectionProfile> profiles)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}