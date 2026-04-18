using System;
using System.IO;
using System.Text.Json;

namespace SqlForgeWpf.Models
{
    // The data structure for serialization
    public class AppConfigData
    {
        public string LmHost { get; set; } = "127.0.0.1";
        public string LmPort { get; set; } = "1234";
        public string LmEndpoint { get; set; } = "/v1";
        public string ActiveModel { get; set; } = "[None]";

        public string DbType { get; set; } = "";
        public string DbHost { get; set; } = "";
        public string DbPort { get; set; } = "";
        public string DbName { get; set; } = "";
        public string DbUser { get; set; } = "";
        public string DbPassword { get; set; } = "";
    }

    // The static manager used throughout your application
    public static class AppSettings
    {
        private static readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private static AppConfigData _data = new AppConfigData();

        // --- LM Studio Settings ---
        public static string LmHost { get => _data.LmHost; set { _data.LmHost = value; Save(); } }
        public static string LmPort { get => _data.LmPort; set { _data.LmPort = value; Save(); } }
        public static string LmEndpoint { get => _data.LmEndpoint; set { _data.LmEndpoint = value; Save(); } }
        public static string ActiveModel { get => _data.ActiveModel; set { _data.ActiveModel = value; Save(); } }

        // --- Database Settings ---
        public static string DbType { get => _data.DbType; set { _data.DbType = value; Save(); } }
        public static string DbHost { get => _data.DbHost; set { _data.DbHost = value; Save(); } }
        public static string DbPort { get => _data.DbPort; set { _data.DbPort = value; Save(); } }
        public static string DbName { get => _data.DbName; set { _data.DbName = value; Save(); } }
        public static string DbUser { get => _data.DbUser; set { _data.DbUser = value; Save(); } }
        public static string DbPassword { get => _data.DbPassword; set { _data.DbPassword = value; Save(); } }

        // ==========================================
        // CONFIGURATION ENGINE
        // ==========================================
        public static void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _data = JsonSerializer.Deserialize<AppConfigData>(json) ?? new AppConfigData();
                }
                else
                {
                    Save(); // Create default config if it doesn't exist
                }
            }
            catch
            {
                _data = new AppConfigData(); // Fallback to defaults on corruption
            }
        }

        private static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_filePath, JsonSerializer.Serialize(_data, options));
            }
            catch { /* Ignore IO errors during rapid rapid property updates */ }
        }
    }
}