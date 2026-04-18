using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SqlForgeWpf.Models
{
    public static class SchemaManager
    {
        private static readonly string CacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema_cache");

        static SchemaManager()
        {
            if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir);
        }

        // Creates an ultra-dense, token-optimized string for the AI
        public static string BuildConciseSchema(Dictionary<string, Dictionary<string, List<(string Name, string Type)>>> tree)
        {
            var sb = new StringBuilder();

            foreach (var schema in tree)
            {
                foreach (var table in schema.Value)
                {
                    // Format: [schema.table_name]
                    // col1 (type), col2 (type)
                    sb.AppendLine($"[{schema.Key}.{table.Key}]");

                    var columns = table.Value.Select(c => $"{c.Name} ({c.Type})");
                    sb.AppendLine(string.Join(", ", columns));
                    sb.AppendLine(); // Blank line between tables
                }
            }

            return sb.ToString().Trim();
        }

        // Saves the condensed schema locally to avoid querying the DB every time
        public static void CacheSchemaLocally(string host, string dbName, string conciseSchema)
        {
            string fileName = GenerateCacheFilename(host, dbName);
            string filePath = Path.Combine(CacheDir, fileName);
            File.WriteAllText(filePath, conciseSchema);
        }

        // Attempts to load a previously saved schema
        public static string GetCachedSchema(string host, string dbName)
        {
            string fileName = GenerateCacheFilename(host, dbName);
            string filePath = Path.Combine(CacheDir, fileName);

            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            return null;
        }

        // Generates a safe filename based on connection details
        private static string GenerateCacheFilename(string host, string dbName)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes($"{host}_{dbName}");
                byte[] hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash)
                    .Replace("/", "_").Replace("+", "-").Substring(0, 15) + ".schema";
            }
        }

        // Clears the cached schema for a specific connection
        public static void ClearCache(string host, string dbName)
        {
            string fileName = GenerateCacheFilename(host, dbName);
            string filePath = Path.Combine(CacheDir, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}