using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.DTO
{
    public class FlowRunsResponseDto
    {
        public List<FlowRunDto> Value { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string ODataNextLink { get; set; }

        // Fallback si @odata.nextLink n'est pas mappé
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalData { get; set; }
    }
}
