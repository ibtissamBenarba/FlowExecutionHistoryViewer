using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
namespace ExecutionFlowHistoryViewer.Services
{
    public class GeminiService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private const string BaseEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);
        public GeminiService(string apiKey, string model = "gemini-flash-latest")
        {
            _apiKey = apiKey; _model = model;
        }
        public string Ask(string prompt)
        {
            if (!IsConfigured) throw new InvalidOperationException("Gemini API key not configured.");
            string endpoint = string.Format("{0}{1}:generateContent?key={2}", BaseEndpoint, _model, _apiKey);
            using (var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            })
            {
                var payload = new
                {
                    contents = new[] {
                        new { parts = new[] {
                            new {
                                text = prompt
                            }
                        }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        maxOutputTokens = 2048
                    }
                };
                var json = JsonConvert.SerializeObject(payload);
                var response = http.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode) throw new Exception(string.Format("Gemini API error ({0}): {1}", response.StatusCode, body));
                dynamic result = JsonConvert.DeserializeObject(body);
                if (result.candidates != null && result.candidates.Count > 0)
                {
                    var text = result.candidates[0].content?.parts?[0]?.text?.ToString();
                    if (!string.IsNullOrEmpty(text)) return text;
                }
                throw new Exception("Empty response from Gemini API.");
            }
        }
    }
}