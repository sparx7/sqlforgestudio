using System;
using System.IO;

namespace SqlForgeWpf.Services
{
    public static class FileSystemManager
    {
        private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string ChatDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chats");

        static FileSystemManager()
        {
            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
            if (!Directory.Exists(ChatDir)) Directory.CreateDirectory(ChatDir);
        }

        public static void SaveLog(string level, string message)
        {
            string file = Path.Combine(LogDir, $"SystemLog_{DateTime.Now:yyyy-MM-dd}.txt");
            File.AppendAllText(file, $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}\n");
        }

        public static void SaveChatInteraction(string dbType, string userPrompt, string generatedSql)
        {
            string file = Path.Combine(ChatDir, $"QueryHistory_{DateTime.Now:yyyy-MM-dd}.txt");
            string entry = $"\n--- {DateTime.Now:HH:mm:ss} | {dbType} ---\nUSER: {userPrompt}\n\nSQL:\n{generatedSql}\n";
            File.AppendAllText(file, entry);
        }
    }
}