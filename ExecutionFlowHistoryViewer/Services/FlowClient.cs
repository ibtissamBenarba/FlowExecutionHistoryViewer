using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Services
{
    public class FlowClient
    {
        private readonly string _envId;
        private readonly string _token;
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public FlowClient(string envId, string token, string regionUrl, HttpClient httpClient = null)
        {
            _envId = envId;
            _token = token;
            _baseUrl = regionUrl;
            _httpClient = httpClient ?? new HttpClient();
        }

        public List<FlowRun> GetFlowRuns(string flowId)
        {
            var runs = new List<FlowRun>();
            string url = $"{_baseUrl}/providers/Microsoft.ProcessSimple/environments/{_envId}/flows/{flowId}/runs?api-version=2016-11-01";

            // Bdlnaha b HttpRequestMessage bach n-testiwha sahel
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                dynamic data = JsonConvert.DeserializeObject(json);

                if (data.value != null)
                {
                    foreach (var item in data.value)
                    {
                        runs.Add(new FlowRun
                        {
                            Id = item.name,
                            Status = item.properties.status,
                            StartDate = item.properties.startTime,
                            EndDate = item.properties.endTime,
                            Url = $"https://make.powerautomate.com/environments/{_envId}/flows/{flowId}/runs/{item.name}"
                        });
                    }
                }
            }
            else
            {
                var errorBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new Exception($"Erreur Power Automate : {response.StatusCode} - {errorBody}");
            }

            return runs;
        }
    }
}
