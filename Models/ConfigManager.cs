using System;
using System.IO;
using System.Text.Json;

namespace SqlForgeWpf.Models
{
    
    public class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public static class ConfigManager
    {
        private const string ConfigFile = "config.json";

        public static void Load()
        {
            if (!File.Exists(ConfigFile)) return;

            try
            {
                string json = File.ReadAllText(ConfigFile);
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("DbHost", out var dbHost)) AppSettings.DbHost = dbHost.GetString()!;
                if (root.TryGetProperty("DbPort", out var dbPort)) AppSettings.DbPort = dbPort.GetString()!;
                if (root.TryGetProperty("DbName", out var dbName)) AppSettings.DbName = dbName.GetString()!;
                if (root.TryGetProperty("DbUser", out var dbUser)) AppSettings.DbUser = dbUser.GetString()!;
                if (root.TryGetProperty("DbPassword", out var dbPass)) AppSettings.DbPassword = dbPass.GetString()!;

                if (root.TryGetProperty("LmServerIp", out var lmIp)) AppSettings.LmHost = lmIp.GetString()!;
                if (root.TryGetProperty("LmEndpoint", out var lmEnd)) AppSettings.LmEndpoint = lmEnd.GetString()!;
                if (root.TryGetProperty("ActiveModel", out var am)) AppSettings.ActiveModel = am.GetString()!;
            }
            catch { /* Ignore corrupt config on load */ }
        }

        public static void Save()
        {
            var configData = new
            {
                AppSettings.DbHost,
                AppSettings.DbPort,
                AppSettings.DbName,
                AppSettings.DbUser,
                AppSettings.DbPassword,
                AppSettings.LmHost,
                AppSettings.LmEndpoint,
                AppSettings.ActiveModel
            };

            string json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
    }
}