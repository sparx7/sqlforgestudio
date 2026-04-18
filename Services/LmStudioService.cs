using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SqlForgeWpf.Services
{
    public class LmStudioService
    {
        // Keeps the safe static HttpClient we added
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        // Reverted to simple hardcoded URL
        private readonly string _baseUrl = "http://localhost:1234/v1";

        public async Task<string> SendChatMessageAsync(string modelName, string systemPrompt, string userMessage, double temperature = 0.0)
        {
            var payload = new
            {
                model = modelName,
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = temperature
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

        public async Task<List<string>> GetCurrentlyLoadedModelsAsync()
        {
            var models = new List<string>();
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/models");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    foreach (var model in doc.RootElement.GetProperty("data").EnumerateArray())
                    {
                        models.Add(model.GetProperty("id").GetString());
                    }
                }
            }
            catch { /* Ignore connection errors */ }
            return models;
        }

        public async Task LoadModelToRamAsync(string modelName)
        {
            var payload = new { model = modelName };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
        }

        public async Task ClearLoadedModelsAsync()
        {
            var loadedModels = await GetCurrentlyLoadedModelsAsync();
            foreach (var model in loadedModels)
            {
                try { await _httpClient.DeleteAsync($"{_baseUrl}/models/{model}"); }
                catch { }
            }
        }
    }
}