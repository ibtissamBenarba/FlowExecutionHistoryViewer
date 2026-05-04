using ExecutionFlowHistoryViewer.Models;
using ExecutionFlowHistoryViewer.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows.Documents;

namespace ExecutionFlowHistoryViewer.Services
{
    public class FlowClient
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

        /// <summary>
        /// Gets flow runs with server-side pagination using $top and $skiptoken
        /// </summary>
        public FlowRunPageResult GetFlowRuns(string flowId, int top = 100, string skipToken = null)
        {
            var result = new FlowRunPageResult
            {
                Runs = new List<FlowRun>(),
                HasMore = false,
                NextSkipToken = null
            };

            // Build URL with $top for page size
            string url = $"{_baseUrl}/providers/Microsoft.ProcessSimple/environments/{_envId}/flows/{flowId}/runs?api-version=2016-11-01&$top={top}";

            // Add $skiptoken for next page (from previous response's nextLink)
            if (!string.IsNullOrEmpty(skipToken))
            {
                url += $"&$skiptoken={Uri.EscapeDataString(skipToken)}";
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                var response = client.GetAsync(url).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
                    // Check if there's a next page — Microsoft returns this as @odata.nextLink
                    if (data["@odata.nextLink"] != null || data.nextLink != null)
                    {
                        string nextLink = (data["@odata.nextLink"] ?? data.nextLink).ToString();
                        result.HasMore = true;

                        // Extract $skiptoken from the nextLink URL query string
                        var uri = new Uri(nextLink);
                        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        result.NextSkipToken = queryParams["$skiptoken"];
                    }
                }
                else
                {
                    var errorBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    throw new Exception($"Power Automate Error: {response.StatusCode} - {errorBody}");
                }
            }
            return result;
        }
    }
}