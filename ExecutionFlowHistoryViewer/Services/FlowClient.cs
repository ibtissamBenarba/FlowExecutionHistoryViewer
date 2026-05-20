// Services/FlowClient.cs
using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.DTO;
using ExecutionFlowHistoryViewer.Helpers;
using ExecutionFlowHistoryViewer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace ExecutionFlowHistoryViewer.Services
{

    public class FlowClient : IFlowClient
    {
        private readonly string _envId;
        private readonly string _token;
        private readonly string _baseUrl;
        private readonly string _resubmitBaseUrl;
        public FlowClient(string envId, string token, string baseUrl, string resubmitBaseUrl = null)
        {
            _envId = envId;
            _token = token;
            _baseUrl = baseUrl;
            _resubmitBaseUrl = resubmitBaseUrl ?? baseUrl;
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

                // --- LECTURE DU NEXT LINK VIA JOBJECT (infaillible) ---
                var jObject = JObject.Parse(json);
                string nextLink = jObject["@odata.nextLink"]?.ToString()
                               ?? jObject["nextLink"]?.ToString();

                if (!string.IsNullOrEmpty(nextLink))
                {
                    result.HasMore = true;
                    var uri = new Uri(nextLink);
                    var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    result.NextSkipToken = queryParams["$skiptoken"];
                }

                // --- LECTURE DES RUNS VIA DTO (typé et propre) ---
                var dto = jObject.ToObject<FlowRunsResponseDto>();

                if (dto?.Value != null)
                {
                    foreach (var item in dto.Value)
                    {
                        if (item == null) continue;

                        result.Runs.Add(new FlowRun
                        {
                            Id = item.Name,
                            Status = item.Properties?.Status ?? "Unknown",
                            StartDate = item.Properties?.StartTime ?? DateTime.MinValue,
                            EndDate = item.Properties?.EndTime ?? DateTime.MinValue,
                            Url = $"https://make.powerautomate.com/environments/{_envId}/flows/{flowId}/runs/{item.Name}"
                        });
                    }
                }
            }

            return result;
        }
        public FlowRunDetailDto GetRunDetails(string flowId, string runId)
        {
            string url = $"{_baseUrl}/providers/Microsoft.ProcessSimple/environments/{_envId}/flows/{flowId}/runs/{runId}?api-version=2016-11-01";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                var response = client.GetAsync(url).GetAwaiter().GetResult();
                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Power Automate Error: {response.StatusCode} - {json}");

                var jObject = Newtonsoft.Json.Linq.JObject.Parse(json);
                var dto = jObject.ToObject<FlowRunDetailDto>();

                // Correlation n'est pas toujours mappé automatiquement
                var correlation = jObject["properties"]?["correlation"];
                if (correlation != null && dto?.Properties != null)
                {
                    dto.Properties.CorrelationClientTrackingId = correlation["clientTrackingId"]?.ToString();
                }

                return dto;
            }
        }

        public FlowActionsResponseDto GetRunActions(string flowId, string runId)
        {
            string url = $"{_baseUrl}/providers/Microsoft.ProcessSimple/environments/{_envId}/flows/{flowId}/runs/{runId}/actions?api-version=2016-11-01";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                var response = client.GetAsync(url).GetAwaiter().GetResult();
                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Power Automate Error: {response.StatusCode} - {json}");

                return JsonConvert.DeserializeObject<FlowActionsResponseDto>(json);
            }
        }

        public string GetContentFromLink(string linkUri)
        {
            if (string.IsNullOrEmpty(linkUri))
                return null;

            using (var client = new HttpClient())
            {
                // Si l'URI contient déjà un token SAS (sig=), ne pas envoyer Bearer
                bool hasSasToken = linkUri.Contains("sig=") || linkUri.Contains("sp=");

                if (!hasSasToken)
                {
                    // Seulement si PAS de SAS dans l'URL
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                }

                var response = client.GetAsync(linkUri).GetAwaiter().GetResult();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: {response.StatusCode}\nURI: {linkUri}\nResponse: {content}";
                }

                // Formater le JSON
                try
                {
                    var obj = JsonConvert.DeserializeObject(content);
                    return JsonConvert.SerializeObject(obj, Formatting.Indented);
                }
                catch
                {
                    return content;
                }
            }
        }

        public bool ResubmitRun(string flowId, string runId)
        {
            // Get trigger name using the READ API (still works on api.flow.microsoft.com)
            string triggerName = GetTriggerName(flowId, runId);
            string encodedTrigger = Uri.EscapeDataString(triggerName);

            // Use the RESUBMIT API (environment-specific)
            string url = $"{_resubmitBaseUrl}/powerautomate/flows/{flowId}/triggers/{encodedTrigger}/histories/{runId}/resubmit?api-version=1";

            System.Diagnostics.Debug.WriteLine($"RESUBMIT URL: {url}");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                var response = client.PostAsync(url, null).GetAwaiter().GetResult();
                var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HTTP {(int)response.StatusCode}: {responseText}");
                }
                return true;
            }
        }

        private string GetTriggerName(string flowId, string runId)
        {
            // Use READ API to get trigger name
            string url = $"{_baseUrl}/providers/Microsoft.ProcessSimple/environments/{_envId}/flows/{flowId}/runs/{runId}?api-version=2016-11-01";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

                var response = client.GetAsync(url).GetAwaiter().GetResult();
                var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Failed to get run details: {response.StatusCode} - {json}");

                var jObject = JObject.Parse(json);

                string triggerName =
                    jObject["properties"]?["trigger"]?["name"]?.ToString()
                    ?? jObject["properties"]?["trigger"]?["type"]?.ToString();

                if (string.IsNullOrEmpty(triggerName))
                    throw new Exception("Could not extract trigger name from run details.");

                return triggerName;
            }
        }
        }
}