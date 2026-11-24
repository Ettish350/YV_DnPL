using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlugIn;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Client;

namespace Reservations.EventReservation
{
    class EventReservationService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;


        public EventReservationService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }



        internal void SetEventNum(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            Guid gPreEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_reservationsid", preEntity);
            Guid gPostEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_reservationsid", postEntity);
            int iPreEventType = EntityUtils.GetOptionSetValueFromEntityValue("new_event_type", preEntity);
            int iPostEventType = EntityUtils.GetOptionSetValueFromEntityValue("new_event_type", postEntity);


            string postEventReservationCode = EntityUtils.GetStringFromEntityValue("new_event_code", postEntity);
                        
            if (iPreEventType.CompareTo(iPostEventType) != 0 && iPostEventType != 0)
            {

                if (iPostEventType == 1 || iPostEventType == 3 || iPostEventType == 4 || iPostEventType == 5 || iPostEventType == 8)
                {
                    // 1 השתלמות  
                    // 3 סמינר
                    // הנצחה 4
                    // קורסים והכשרות 5
                    // 8 כנס

                    string key = "new_event_reservations_" + iPostEventType.ToString();
                    string preNum = gf.GetConfigurationSetting(key);
                    string sEventNum = "";
                    if (iPreEventType == 2)
                    {
                        string sNum = GetNextNumerator("new_event_reservations");
                        sEventNum = preNum + sNum;
                    }
                    else
                    {
                        sEventNum = preNum + postEventReservationCode.Substring(postEventReservationCode.Length - 4);//4 because the numerator is length 4
                    }

                    eEntity["new_event_code"] = sEventNum;
                    bIsUpdate = true;
                }
                else if (iPostEventType == 2)
                {
                    SetReservationNumerator(false, preEntity, postEntity, ref bIsUpdate, ref eEntity);
                }
 
            }else if (iPostEventType == 2) {
                Guid gPreBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", preEntity);
                Guid gPostBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", postEntity);
                if (gPreBudgetFramework.CompareTo(gPostBudgetFramework) != 0 && gPostBudgetFramework.CompareTo(Guid.Empty) != 0) {
                    SetReservationNumerator(true, preEntity, postEntity, ref bIsUpdate, ref eEntity);
                }
            }
        }


        internal void SetReservationNumerator(bool isDiffBudgetFramework, Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            Guid gPreBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", preEntity);
            Guid gPostBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", postEntity);


            if (gPostBudgetFramework.CompareTo(Guid.Empty) != 0)
            {
                #region query
                QueryExpression query = new QueryExpression();
                query.EntityName = "new_budget_framework";

                ColumnSet cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_budget_frameworkid", "new_rsv_department" });
                query.ColumnSet = cols;


                FilterExpression fe = new FilterExpression();
                fe.FilterOperator = LogicalOperator.And;
                fe.Conditions.Add(new ConditionExpression("new_budget_frameworkid", ConditionOperator.Equal, gPostBudgetFramework));
                query.Criteria.AddFilter(fe);

                LinkEntity le = new LinkEntity("new_budget_framework", "new_rsv_department", "new_rsv_department", "new_rsv_departmentid", JoinOperator.Inner);
                le.Columns.AddColumn("new_prefix");
                le.EntityAlias = "rsvd";
                query.LinkEntities.Add(le);
                RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                requestMult.Query = query;

                RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;

                #endregion


                if (respMult.EntityCollection.Entities.Count > 0)
                {

                    Entity eItem = respMult.EntityCollection.Entities[0];

                    string sPreFix = EntityUtils.GetStringAliasedFromEntityValue("rsvd.new_prefix", eItem);
                    string sNum = "";

                    if (isDiffBudgetFramework)
                    {
                        sNum = EntityUtils.GetStringFromEntityValue("new_event_code", postEntity);
                    }else {  
                        sNum = GetNextNumerator("new_reservation");
                    }
                   
                    if (sNum.Length > 3)
                    {
                        sNum = sPreFix + sNum.Substring(3); // because of XXX
                        eEntity["new_event_code"] = sNum;
                        bIsUpdate = true;
                    }
                }
            }
        }

        internal string GetNextNumerator(string entityName)
        {
            string sNumerator = "";
            string sAttributeName = "";
            string sPreFix = "";
            int iNextNumber = -1;
            int iNumeratorNumberLength = 0;

            Entity deNumerator = gf.GetNextNumerator(entityName);
            if (deNumerator != null)
            {
                #region Get numerator data 
                sAttributeName = EntityUtils.GetStringFromEntityValue("new_attribute_name", deNumerator);
                sPreFix = EntityUtils.GetStringFromEntityValue("new_pre_fix", deNumerator);
                iNumeratorNumberLength = EntityUtils.GetIntFromEntityValue("new_numerator_number_length", deNumerator);
                iNextNumber = EntityUtils.GetIntFromEntityValue("new_next_number", deNumerator);
                #endregion

                //we have data for numerator
                if (iNextNumber > 0 && sAttributeName.Length > 0)
                {
                    sNumerator = iNextNumber.ToString();

                    while (sNumerator.Length < iNumeratorNumberLength)
                    {
                        sNumerator = "0" + sNumerator;
                    }

                    #region save numerator entity in SharedVariables
                    //context.SharedVariables.Add(entity.LogicalName + "CurrentSequenceNumber", deNumerator);
                    deNumerator["new_next_number"] = iNextNumber + 1;
                    userService.Update(deNumerator);
                    #endregion
                }
            }//if (deNumerator != null)

            return sPreFix + sNumerator;

        }

        internal void UpdateEventTotalParticipants(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            OrganizationServiceContext orgContext = new OrganizationServiceContext(sysService);

            int gPreNumAdditionalParticipants = EntityUtils.GetIntFromEntityValue("new_num_additional_participants", preEntity);
            int gPostNumAdditionalParticipants = EntityUtils.GetIntFromEntityValue("new_num_additional_participants", postEntity);


            if (gPostNumAdditionalParticipants != gPreNumAdditionalParticipants)
            {
                //select all registeration of spcifice event
                var r = from con in orgContext.CreateQuery("new_event_registration")
                        where (((string)con["new_event_reservations"]).Equals(postEntity.Id.ToString().ToUpper()) && (con["statecode"]).Equals(0))
                        select new { nump = (con["new_number_of_participants"]) };

                var sum = r.ToList().Select(x => Convert.ToInt32(x.nump)).Sum();
                sum += gPostNumAdditionalParticipants;
                eEntity["new_total_participants"] = sum;
                bIsUpdate = true;
            }


        }

        ///// <summary>
        ///// count divisions with workshops that the person should to be registered
        //internal int CalculateEventDivisionsWithWorkshops(Guid eventGuid)
        //{

        //    WriteToLog.LogToFile("CalculateEventDivisionsWithWorkshops: " + eventGuid, "");

        //    #region query
        //    QueryExpression query = new QueryExpression();
        //    query.EntityName = "new_learning_division";

        //    ColumnSet columns = new ColumnSet(true);

        //    query.ColumnSet = columns;

        //    query.Criteria = new FilterExpression();
        //    query.Criteria.FilterOperator = LogicalOperator.And;

        //    ConditionExpression condition1 = new ConditionExpression();
        //    condition1.AttributeName = "new_event_reservations";
        //    condition1.Operator = ConditionOperator.Equal;
        //    condition1.Values.Add(eventGuid);

        //    ConditionExpression condition2 = new ConditionExpression();
        //    condition2.AttributeName = "statecode";
        //    condition2.Operator = ConditionOperator.Equal;
        //    condition2.Values.Add(0);

        //    ConditionExpression condition3 = new ConditionExpression();
        //    condition3.AttributeName = "new_for_everyone";
        //    condition3.Operator = ConditionOperator.Equal;
        //    condition3.Values.Add(false);


        //    query.Criteria.Conditions.Add(condition1);
        //    query.Criteria.Conditions.Add(condition2);
        //    query.Criteria.Conditions.Add(condition3);


        //    RetrieveMultipleRequest retrieve = new RetrieveMultipleRequest();
        //    retrieve.Query = query;
        //    RetrieveMultipleResponse retrieved = (RetrieveMultipleResponse)sysService.Execute(retrieve);
        //    #endregion



        //    int result = 0;
        //    foreach (Entity item in retrieved.EntityCollection.Entities)
        //    {
        //        result += 1;
        //    }
        //    WriteToLog.LogToFile("Total: " + result, "");

        //    return result;

        //}

        /// <summary>
        /// count workshops that the person registered
        internal void UpdateRegistrationStatuces(Guid eventGuid, int newDivisionsAmount)
        {


            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_event_registration";

            ColumnSet columns = new ColumnSet(true);

            query.ColumnSet = columns;

            query.Criteria = new FilterExpression();
            query.Criteria.FilterOperator = LogicalOperator.And;

            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = "new_event_reservations";
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(eventGuid);

            ConditionExpression condition2 = new ConditionExpression();
            condition2.AttributeName = "statecode";
            condition2.Operator = ConditionOperator.Equal;
            condition2.Values.Add(0);

            ConditionExpression condition3 = new ConditionExpression();
            condition3.AttributeName = "new_num_of_workshop_registered";
            condition3.Operator = ConditionOperator.GreaterThan ;
            condition3.Values.Add(0);

            ConditionExpression condition4 = new ConditionExpression();
            condition4.AttributeName = "new_num_of_workshop_registered";
            condition4.Operator = ConditionOperator.NotNull;



            query.Criteria.Conditions.Add(condition1);
            query.Criteria.Conditions.Add(condition2);
            query.Criteria.Conditions.Add(condition3);
            query.Criteria.Conditions.Add(condition4);


            RetrieveMultipleRequest retrieve = new RetrieveMultipleRequest();
            retrieve.Query = query;
            RetrieveMultipleResponse retrieved = (RetrieveMultipleResponse)sysService.Execute(retrieve);
            #endregion


            foreach (Entity item in retrieved.EntityCollection.Entities)
            {

                //2 fully registered, 4 partly registered
                int iNumOfWorkshopsRegistered = EntityUtils.GetIntFromEntityValue("new_num_of_workshop_registered", item);
                int iStatusOptionSet = EntityUtils.GetIntFromEntityValue("new_scheduling_status", item);
                if ((iNumOfWorkshopsRegistered >= newDivisionsAmount) && (iStatusOptionSet != 2))
                {
                    item["new_scheduling_status"] = new OptionSetValue(2);
                    sysService.Update(item);
                }
                else
                {
                    if ((iNumOfWorkshopsRegistered < newDivisionsAmount) && (iStatusOptionSet != 4))
                    {
                        item["new_scheduling_status"] = new OptionSetValue(4);
                        sysService.Update(item);
                    }

                }


            }
            



        }
    }
  
}
