using Data.Models;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class ClosedDaysService : IClosedDaysService
    {
        private readonly ICRMConnection _crmService;
        public ClosedDaysService(ICRMConnection crmConnection)
        {
            _crmService = crmConnection;
        }

        public List<ClosedDays> GetClosedDays(DateTime fromDate, DateTime toDate, List<ClosedType> closedTypes)
        {
            List<ClosedDays> closedDays = new List<ClosedDays>();

            var fetchXmlDates = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='new_closed_days_and_activities'>
                    <all-attributes />
                    <filter type='and'>
                      <condition attribute='new_from_closing_date' operator='on-or-after' value='{fromDate}' />
                      <condition attribute='new_from_closing_date' operator='on-or-before' value='{toDate}' />
                      <condition attribute='statecode' operator='eq' value='0' />
                      <condition attribute='new_closing_type' operator='in'>";

            //<condition attribute='new_is_closed_full_day' operator='eq' value='1' />

            closedTypes.ForEach(type => fetchXmlDates += $"<value>{(int)type}</value>");

            fetchXmlDates += @"  </condition>
                                </filter>
                              </entity>
                            </fetch>";

            var query_closedDates = new FetchExpression(fetchXmlDates);
            EntityCollection ec_closedDates = _crmService.RetrieveMultiple(query_closedDates);

            if (ec_closedDates.Entities.Count > 0)
            {
                foreach (Entity entity_closedDate in ec_closedDates.Entities)
                {
                    var closingDate = entity_closedDate.GetAttributeValue<DateTime?>("new_from_closing_date");
                    if (closingDate.HasValue)
                    {
                        closedDays.Add(new ClosedDays
                        {
                            Id = entity_closedDate.Id.ToString(),
                            Name = entity_closedDate.GetAttributeValue<string>("new_name"),
                            StartDate = closingDate.Value,
                            //ClosedType = (ClosedType)entity_closedDate.GetAttributeValue<OptionSetValue>("new_closing_type").Value,
                            IsClosed = entity_closedDate.GetAttributeValue<bool>("new_is_closed_full_day"),
                            //ClosingApprovedBy = entity_closedDate.GetAttributeValue<string>("new_closing_approved_by"),
                        });
                    }
                }
            }
            return closedDays;
        }
    }
}
