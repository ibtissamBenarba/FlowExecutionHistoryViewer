using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ExecutionFlowHistoryViewer.Contracts;
using Newtonsoft.Json;

namespace ExecutionFlowHistoryViewer.Services
{
    public class GeminiChatService : IChatService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiChatService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<string> AskQuestionAsync(string question, string systemContext, List<ChatMessage> history)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "Error: Gemini API Key is not set in Settings.";
            }

            var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            // Prepare history contents for Gemini API format
            var contents = new List<object>();

            // Add previous history
            foreach (var msg in history)
            {
                contents.Add(new
                {
                    role = msg.Role == "model" ? "model" : "user",
                    parts = new[] { new { text = msg.Content } }
                });
            }

            // Append the new question
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = question } }
            });

            var payload = new
            {
                system_instruction = new
                {
                    parts = new
                    {
                        text = "You are an assistant helping a developer analyze a Power Automate flow run history. Answer questions based ONLY on the provided JSON context of the run history. Keep answers concise. Context: " + systemContext
                    }
                },
                contents = contents
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUri, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"API Error: {response.StatusCode} - {responseString}";
            }

            try
            {
                var result = JsonConvert.DeserializeAnonymousType(responseString, new
                {
                    candidates = new[]
                    {
                        new
                        {
                            content = new
                            {
                                parts = new[]
                                {
                                    new { text = "" }
                                }
                            }
                        }
                    }
                });

                return result?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text ?? "No response from Gemini.";
            }
            catch (Exception ex)
            {
                return $"Error parsing response: {ex.Message}";
            }
        }
    }
}
