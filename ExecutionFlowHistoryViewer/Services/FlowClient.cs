// Services/FlowClient.cs
using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;

namespace ExecutionFlowHistoryViewer.Services
{

    public class FlowClient : IFlowClient
    {
        private readonly string _envId;
        private readonly string _token;
        private readonly string _baseUrl;

        public FlowClient(string envId, string token, string regionUrl)
        {
            _envId = envId;
            _token = token;
            _baseUrl = regionUrl;
        }

        public FlowRunPageResult GetFlowRuns(string flowId, int top = 100, string skipToken = null)
        {
            var result = new FlowRunPageResult();

            string url = $"{_baseUrl}/providers/Microsoft.ProcessSimple/environments/{_envId}/flows/{flowId}/runs?api-version=2016-11-01&$top={top}";

            if (!string.IsNullOrEmpty(skipToken))
                url += $"&$skiptoken={Uri.EscapeDataString(skipToken)}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                var response = client.GetAsync(url).GetAwaiter().GetResult();
                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Power Automate Error: {response.StatusCode} - {json}");

                dynamic data = JsonConvert.DeserializeObject(json);

                if (data.value != null)
                {
                    foreach (var item in data.value)
                    {
                        result.Runs.Add(new FlowRun
                        {
                            Id = item.name,
                            Status = item.properties?.status ?? "Unknown",
                            StartDate = item.properties?.startTime ?? DateTime.MinValue,
                            EndDate = item.properties?.endTime ?? DateTime.MinValue,
                            Url = $"https://make.powerautomate.com/environments/{_envId}/flows/{flowId}/runs/{item.name}"
                        });
                    }
                }

                string nextLink = data["@odata.nextLink"]?.ToString() ?? data.nextLink?.ToString();
                if (!string.IsNullOrEmpty(nextLink))
                {
                    result.HasMore = true;
                    var uri = new Uri(nextLink);
                    var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    result.NextSkipToken = queryParams["$skiptoken"];
                }
            }

            return result;
        }
    }
}