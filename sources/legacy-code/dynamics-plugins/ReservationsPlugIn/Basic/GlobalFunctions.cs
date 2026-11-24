using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;

//using System.Configuration;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;

namespace BasicPlugIn
{
    public class GlobalFunctions
    {

        private IOrganizationService crmService;

        #region basic global function
        public GlobalFunctions(IOrganizationService service)
        {
            crmService = service;
        }

        public Entity GetRecord(string entityName, Guid entityId, string[] columns)
        {
            return GetRecord(entityName, entityId, columns, entityName + "id");
        }

        public Entity GetRecord(string entityName, Guid entityId, string[] columns, string entityIdAttName)
        {
            Entity eRet_ = new Entity();
            RetrieveMultipleRequest req = new RetrieveMultipleRequest();


            QueryExpression query = new QueryExpression();
            query.EntityName = entityName;
            query.ColumnSet = new ColumnSet(columns);


            query.Criteria = new FilterExpression();
            query.Criteria.FilterOperator = LogicalOperator.And;

            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = entityIdAttName;
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(entityId.ToString());


            query.Criteria.Conditions.Add(condition1);

            req.Query = query;

            RetrieveMultipleResponse res = (RetrieveMultipleResponse)crmService.Execute(req);

            if (res.EntityCollection.Entities.Count > 0)
            {
                eRet_ = res.EntityCollection.Entities[0];
            }

            return eRet_;
        }//GetRecord


        public List<Entity> RetrieveFullMultipleResponse(QueryExpression queryExpression, IOrganizationService service)
        {
            List<Entity> entityList = new List<Entity>();
            queryExpression.PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 };
            var pageCount = 0;
            var hasMoreRecords = true;

            while (hasMoreRecords)
            {
                pageCount++;
                queryExpression.PageInfo = new PagingInfo { Count = 5000, PageNumber = pageCount };
                var retrieveResponse = service.RetrieveMultiple(queryExpression);
                if (retrieveResponse.Entities.Count > 0)
                {
                    entityList.AddRange(retrieveResponse.Entities);
                }
                hasMoreRecords = retrieveResponse.MoreRecords;
            }
            return entityList;
        }//RetrieveFullMultipleResponse

        #endregion


        public Guid GetCurrentYear()
        {
            Guid gRet_ = Guid.Empty;
            RetrieveMultipleRequest req = new RetrieveMultipleRequest();


            QueryExpression query = new QueryExpression();
            query.EntityName = "new_year";
            query.ColumnSet = new ColumnSet(true);


            query.Criteria = new FilterExpression();
            query.Criteria.FilterOperator = LogicalOperator.And;

            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = "new_name";
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(DateTime.Now.Year.ToString());


            query.Criteria.Conditions.Add(condition1);

            req.Query = query;

            RetrieveMultipleResponse res = (RetrieveMultipleResponse)crmService.Execute(req);

            if (res.EntityCollection.Entities.Count > 0)
            {
                gRet_ = res.EntityCollection.Entities[0].Id;
            }
            return gRet_;
        }//GetCurrentYear

        internal void ChangeStage(Guid entityId, string logicalName, Guid nextEntityId, string NextLogicalName, string currentStage, int code)
        {
            return;

            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_auto_process_stage_flow";

            ColumnSet columns = new ColumnSet(true);

            query.ColumnSet = columns;

            query.Criteria = new FilterExpression();
            query.Criteria.FilterOperator = LogicalOperator.And;

            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = "new_code";
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(code);
            query.Criteria.Conditions.Add(condition1);
            LinkEntity le = new LinkEntity("new_auto_process_stage_flow", "new_auto_process_flow", "new_auto_process_flow", "new_auto_process_flowid", JoinOperator.Inner);
            le.Columns = new ColumnSet(true);
            query.LinkEntities.Add(le);

            RetrieveMultipleRequest retrieve = new RetrieveMultipleRequest();
            retrieve.Query = query;
            RetrieveMultipleResponse retrieved = (RetrieveMultipleResponse)crmService.Execute(retrieve);
            #endregion

            string sProcessid = "";
            string sStageid = "";
            foreach (Entity item in retrieved.EntityCollection.Entities)
            {
                sProcessid = ((AliasedValue)item["new_auto_process_flow1.new_processid"]).Value.ToString();
                sStageid = EntityUtils.GetStringFromEntityValue("new_stageid", item);
            }
            if (sProcessid.Length > 0 && sStageid.Length > 0)
            {
                if (entityId.CompareTo(nextEntityId) == 0)
                {

                    Entity eEntity = new Entity(logicalName);
                    eEntity.Id = entityId;
                    eEntity["processid"] = new Guid(sProcessid);
                    eEntity["stageid"] = new Guid(sStageid);
                    eEntity["traversedpath"] = sStageid.ToLower();
                    crmService.Update(eEntity);
                }
                else {


                    OrganizationRequest or = new OrganizationRequest("NavigateToNextEntity");

                    or.Parameters.Add("ProcessId", new Guid(sProcessid));
                    or.Parameters.Add("NewActiveStageId", new Guid(sStageid));

                    or.Parameters.Add("CurrentEntityLogicalName", logicalName);
                    or.Parameters.Add("CurrentEntityId", entityId);

                    or.Parameters.Add("NextEntityLogicalName", NextLogicalName);
                    or.Parameters.Add("NextEntityId", nextEntityId);

                    or.Parameters.Add("NewTraversedPath", currentStage + "," + sStageid);

                    var response = crmService.Execute(or) as OrganizationResponse;
                }
            }
        }

        public string GetDomainName(Guid gUser)
        {
            Entity eUser = GetRecord("systemuser", gUser, new string[] { "domainname" });
            return EntityUtils.GetStringFromEntityValue("domainname", eUser);
        }
        public Guid GetUserByDomainName(string sDomainName)
        {
            Guid gRet_ = Guid.Empty;
            RetrieveMultipleRequest req = new RetrieveMultipleRequest();


            QueryExpression query = new QueryExpression();
            query.EntityName = "systemuser";
            query.ColumnSet = new ColumnSet(new string[] { "systemuserid", "fullname" });


            query.Criteria = new FilterExpression();
            query.Criteria.FilterOperator = LogicalOperator.And;
            query.Criteria.AddCondition("domainname", ConditionOperator.Equal, sDomainName);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            req.Query = query;

            RetrieveMultipleResponse res = (RetrieveMultipleResponse)crmService.Execute(req);
            if (res.EntityCollection.Entities.Count > 0)
            {
                gRet_ = res.EntityCollection.Entities[0].Id;
            }

            return gRet_;
        }

        public Entity GetNextNumerator(string entityName)
        {
            Entity entity = null;
            //   try
            {
                // Query for next sequence number from enumerators entity
                QueryExpression query = new QueryExpression();
                query.EntityName = "new_numerator";

                ColumnSet cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_entity_name", "new_attribute_name", "new_next_number", "new_pre_fix", "new_numerator_number_length" });
                query.ColumnSet = cols;

                query.Criteria.AddCondition("new_entity_name", ConditionOperator.Equal, entityName);

                EntityCollection ec = crmService.RetrieveMultiple(query);

                if (ec.Entities.Count > 0)
                {
                    entity = ec.Entities[0];
                }

            }

            return entity;
        }
        public string GetConfigurationSetting(string key)
        {
            //new_key
            string value = "";
            QueryExpression query = new QueryExpression("new_configuration_setting");
            ColumnSet cols = new ColumnSet(new string[] { "new_value" });
            query.ColumnSet = cols;
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("new_key", ConditionOperator.Equal, key);
            try
            {
                EntityCollection entities = crmService.RetrieveMultiple(query);
                if (entities.Entities.Count == 1)
                {
                    value = entities.Entities.FirstOrDefault().GetAttributeValue<string>("new_value");
                }
                else
                {
                    WriteToLog.LogToFile(DateTime.Now + "GetConfigurationSetting - error - entities.Entities.Count: " + entities.Entities.Count, "");
                }
            }
            catch (Exception ex)
            {
                string str = "Exception: " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace;
                WriteToLog.LogToFile(DateTime.Now + "GetConfigurationSetting - " + str, "");
            }
            return value;
        }

    }
}
