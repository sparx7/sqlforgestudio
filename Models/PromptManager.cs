using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SqlForgeWpf.Models
{
    public static class PromptManager
    {
        private static readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompts.json");

        public static string GetSystemPrompt(string dbType, string schema)
        {
            Dictionary<string, string> prompts;
            if (File.Exists(_filePath))
            {
                prompts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_filePath));
            }
            else
            {
                // Auto-generate default optimized prompts if file is missing
                prompts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "PostgreSQL", "You are an expert PostgreSQL developer. Output ONLY valid SQL based on this schema. Wrap identifiers in double quotes. Schema: \n{SCHEMA}" },
                    { "MySQL", "You are an expert MySQL developer. Output ONLY valid SQL based on this schema. Use backticks for identifiers. Schema: \n{SCHEMA}" },
                    { "SQLite", "You are an expert SQLite developer. Output ONLY valid SQLite code. Schema: \n{SCHEMA}" },
                    { "Oracle", "You are an expert Oracle SQL developer. Output ONLY valid Oracle SQL. Use double quotes. Schema: \n{SCHEMA}" }
                };
                File.WriteAllText(_filePath, JsonSerializer.Serialize(prompts, new JsonSerializerOptions { WriteIndented = true }));
            }

            string template = prompts.ContainsKey(dbType) ? prompts[dbType] : prompts["PostgreSQL"];
            return template.Replace("{SCHEMA}", schema);
        }
    }
}