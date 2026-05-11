using ExecutionFlowHistoryViewer.Contracts;
using ExecutionFlowHistoryViewer.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecutionFlowHistoryViewer.Services
{
    public class DataverseService : IDataverseService
    {
        private readonly IOrganizationService _service;

        public DataverseService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public int GetTotalFlowRunsCount(List<string> flowIds, DateTime from, DateTime to, string status)
        {
            if (flowIds == null || !flowIds.Any()) return 0;

            // Construction des filtres <value> pour le IN
            string flowFilter = string.Join("", flowIds.Select(id => $"<value>{id}</value>"));

            // Filtre de statut
            string statusFilter = status == "All" ? "" : $"<condition attribute='status' operator='eq' value='{status}' />";

            string fetchXml = $@"
        <fetch aggregate='true'>
          <entity name='flowrun'>
            <attribute name='flowrunid' aggregate='count' alias='total' />
            <filter type='and'>
              <condition attribute='workflowid' operator='in'>{flowFilter}</condition>
              <condition attribute='starttime' operator='on-or-after' value='{from:yyyy-MM-dd}' />
              <condition attribute='starttime' operator='on-or-before' value='{to:yyyy-MM-dd}' />
              {statusFilter}
            </filter>
          </entity>
        </fetch>";

            var result = _service.RetrieveMultiple(new FetchExpression(fetchXml));

            if (result.Entities.Count > 0 && result.Entities[0].Contains("total"))
            {
                var aliasedValue = (AliasedValue)result.Entities[0]["total"];
                return (int)aliasedValue.Value;
            }

            return 0;
        }

        public List<SolutionItem> GetSolutions()
        {
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("isvisible", ConditionOperator.Equal, true) }
                },
                Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
            };

            var results = _service.RetrieveMultiple(query);
            return results.Entities.Select(e => new SolutionItem
            {
                Id = e.Id,
                Name = e.GetAttributeValue<string>("friendlyname")
                    ?? e.GetAttributeValue<string>("uniquename")
                    ?? e.Id.ToString()
            }).ToList();
        }

        public List<Models.Flow> GetFlows(Guid? solutionId = null)
        {
            var query = new QueryExpression("workflow")
            {
                // AJOUT : statecode et statuscode
                ColumnSet = new ColumnSet("workflowid", "name", "statecode", "statuscode"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
                new ConditionExpression("category", ConditionOperator.Equal, 5),
                new ConditionExpression("type", ConditionOperator.Equal, 1)
            }
                },
                Orders = { new OrderExpression("name", OrderType.Ascending) }
            };

            if (solutionId.HasValue && solutionId.Value != Guid.Empty)
            {
                query.LinkEntities.Add(
                    new LinkEntity("workflow", "solutioncomponent", "workflowid", "objectid", JoinOperator.Inner)
                    {
                        LinkCriteria = new FilterExpression
                        {
                            Conditions =
                            {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId.Value),
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 29)
                            }
                        }
                    });
            }

            var results = _service.RetrieveMultiple(query);
            return results.Entities.Select(e => new Models.Flow
            {
                Id = e.Id.ToString(),
                DisplayName = e.GetAttributeValue<string>("name"),
                // AJOUT : Récupérer les codes d'état
                StateCode = e.GetAttributeValue<OptionSetValue>("statecode")?.Value ?? 0,
                StatusCode = e.GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? 0
            }).OrderBy(f => f.DisplayName).ToList();
        }
    }
}
