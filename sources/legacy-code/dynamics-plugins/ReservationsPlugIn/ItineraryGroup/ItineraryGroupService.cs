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

namespace Reservations.ItineraryGroup
{
    class ItineraryGroupService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;

     
        public ItineraryGroupService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }

        internal void UpdateActivityDayResourcesAmounts(Entity preEntity, Entity postEntity)
        {

            int museumAmountNeeded = 0;
            int campusAmountNeeded = 0;
            int bigQuestionsAmountNeeded = 0;

            Guid gPreActivityDay = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", preEntity);
            Guid gPostActivityDay = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", postEntity);

            //get all groups for activity day:
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_itinerary_group";

            ColumnSet cols = new ColumnSet();
            cols.AddColumns(new string[] { "new_activity_day", "new_is_museum_required", "new_is_campus_required", "new_is_big_questions_required", "new_is_group_arrived" });
            query.ColumnSet = cols;

            FilterExpression fe = new FilterExpression();
            fe.FilterOperator = LogicalOperator.And;
            fe.Conditions.Add(new ConditionExpression("new_activity_day", ConditionOperator.Equal, gPostActivityDay));
            fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            query.Criteria.AddFilter(fe);

            RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
            requestMult.Query = query;

            RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
            #endregion
            bool isAllGroupsArrived = true;
            if (respMult.EntityCollection.Entities.Count > 0)
            {

                foreach (Entity e in respMult.EntityCollection.Entities)
                {
                    if (EntityUtils.GetBoolFromEntityValue("new_is_museum_required", e))
                        museumAmountNeeded += 1;
                    if (EntityUtils.GetBoolFromEntityValue("new_is_campus_required", e))
                        campusAmountNeeded += 1;
                    if (EntityUtils.GetBoolFromEntityValue("new_is_big_questions_required", e))
                        bigQuestionsAmountNeeded += 1;
                    if (!EntityUtils.GetBoolFromEntityValue("new_is_group_arrived", e)) {
                        isAllGroupsArrived = false;
                    }
                    //WriteToLog.LogToFile("bottom loop: museumAmountNeeded: " + museumAmountNeeded + ", campusAmountNeeded: " + campusAmountNeeded + ", bigQuestionsAmountNeed: " + bigQuestionsAmountNeeded, " \n");
                }

                try
                {
                    Entity ad = new Entity("new_activity_day");
                    ad.Id = gPostActivityDay;
                    ad["new_museum_amount_needed"] = museumAmountNeeded;
                    ad["new_campus_amount_needed"] = campusAmountNeeded;
                    ad["new_big_questions_amount_needed"] = bigQuestionsAmountNeeded;
                    ad["new_did_group_arrive"] = isAllGroupsArrived;
                    /*TODO:
                    eEntity["new_museum_amount_scheduled"] = sNum;
                    eEntity["new_campus_amount_scheduled"] = sNum;
                    eEntity["new_big_questions_amount_scheduled"] = sNum;
                    */
                    sysService.Update(ad);

                }
                catch (Exception ex)
                {

                    WriteToLog.LogToFile("UpdateActivityDayResourcesAmounts update activity_day failed ----" + ex.Message, "\n");

                }

            }

            if ((gPreActivityDay.CompareTo(gPostActivityDay) != 0 && gPostActivityDay.CompareTo(Guid.Empty) != 0))
            {
                // activity day was changed - need to update pre too:
                #region query
                query = new QueryExpression();
                query.EntityName = "new_itinerary_group";

                cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_activity_day", "new_is_museum_required", "new_is_campus_required", "new_is_big_questions_required" });
                query.ColumnSet = cols;

                fe = new FilterExpression();
                fe.FilterOperator = LogicalOperator.And;
                fe.Conditions.Add(new ConditionExpression("new_activity_day", ConditionOperator.Equal, gPreActivityDay));
                fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                query.Criteria.AddFilter(fe);

                requestMult = new RetrieveMultipleRequest();
                requestMult.Query = query;

                respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
                #endregion

                if (respMult.EntityCollection.Entities.Count > 0)
                {

                    foreach (Entity e in respMult.EntityCollection.Entities)
                    {
                        if (EntityUtils.GetBoolFromEntityValue("new_is_museum_required", e))
                            museumAmountNeeded += 1;
                        if (EntityUtils.GetBoolFromEntityValue("new_is_campus_required", e))
                            campusAmountNeeded += 1;
                        if (EntityUtils.GetBoolFromEntityValue("new_is_big_questions_required", e))
                            bigQuestionsAmountNeeded += 1;

                        //WriteToLog.LogToFile("bottom loop: museumAmountNeeded: " + museumAmountNeeded + ", campusAmountNeeded: " + campusAmountNeeded + ", bigQuestionsAmountNeed: " + bigQuestionsAmountNeeded, " \n");
                    }

                    try
                    {
                        Entity ad = new Entity("new_activity_day");
                        ad.Id = gPreActivityDay;
                        ad["new_museum_amount_needed"] = museumAmountNeeded;
                        ad["new_campus_amount_needed"] = campusAmountNeeded;
                        ad["new_big_questions_amount_needed"] = bigQuestionsAmountNeeded;
                        /*TODO:
                        eEntity["new_museum_amount_scheduled"] = sNum;
                        eEntity["new_campus_amount_scheduled"] = sNum;
                        eEntity["new_big_questions_amount_scheduled"] = sNum;
                        */
                        sysService.Update(ad);

                    }
                    catch (Exception ex)
                    {

                        WriteToLog.LogToFile("update activity_day failed in itinerary group ----" + ex.Message, "\n");

                    }

                }
            }

        }

        internal void UpdateActivityDayActualParticipants(Entity preEntity, Entity postEntity)
        {

            Guid gPostActivityDay = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", postEntity);
            int preParticipantsAmount = EntityUtils.GetIntFromEntityValue("new_number_of_participants", preEntity);
            int postParticipantsAmount = EntityUtils.GetIntFromEntityValue("new_number_of_participants", postEntity);

            try
            {

                if (preParticipantsAmount != postParticipantsAmount)
                {
                    EntityCollection allDayGroups = GetEntityCollectionActive("new_itinerary_group", "new_activity_day", gPostActivityDay.ToString(), new ColumnSet("new_number_of_participants"));
                    int counter = 0;
                    foreach (Entity eGroup in allDayGroups.Entities)
                    {
                        counter += (int)eGroup["new_number_of_participants"];
                    }


                    Entity eActivityDay = new Entity("new_activity_day");
                    eActivityDay.Id = gPostActivityDay;
                    eActivityDay["new_actual_number_of_participants"] = counter;
                    sysService.Update(eActivityDay);

                    // לעדכן גם את ההזמנה אם יש - בודק בפנים אם זה הזמנה
                    UpdateReservationTotalParticipants(gPostActivityDay, counter);


                }
            }
            catch (Exception ex)
            {

                WriteToLog.LogToFile("UpdateActivityDayActualParticipants operation failed ----" + ex.Message, "\n");

            }


        }

        internal void UpdateReservationTotalParticipants(Guid gPostActivityDay, int counter)
        {
            try { 
            Entity eActivityDayFull = sysService.Retrieve(
                "new_activity_day",
                 gPostActivityDay,
                 new ColumnSet("new_reservation")
                 );

            EntityReference gPostReservation = eActivityDayFull.GetAttributeValue<EntityReference>("new_reservation");

                // עדכן את ההזמנה **רק אם יש Lookup** וזה לא אירוע
                if (gPostReservation != null)
                {
                    // שלוף את כל ימי הפעילות של ההזמנה
                    EntityCollection allActivityDays = GetEntityCollectionActive(
                        "new_activity_day",
                        "new_reservation",
                        gPostReservation.Id.ToString(),
                        new ColumnSet("new_actual_number_of_participants")
                    );

                    Entity eReservationFull = sysService.Retrieve(
                        "new_reservation",
                        gPostReservation.Id,
                        new ColumnSet("new_actual_number_of_participants")
                    );

                    int currentValue = eReservationFull.GetAttributeValue<int?>("new_actual_number_of_participants") ?? 0;

                    int maxParticipants = allActivityDays.Entities
                            .Where(d => d.Contains("new_actual_number_of_participants") &&
                                        d["new_actual_number_of_participants"] != null)
                            .Select(d => (int)d["new_actual_number_of_participants"])
                            .DefaultIfEmpty(0)
                            .Max();

                    if (maxParticipants != currentValue)
                    {
                        Entity eReservation = new Entity("new_reservation");
                        eReservation.Id = gPostReservation.Id;
                        eReservation["new_actual_number_of_participants"] = maxParticipants;
                        sysService.Update(eReservation);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToLog.LogToFile("UpdateReservationActualParticipants operation failed ----" + ex.Message, "\n");
            }
        }

        private Entity GetEntityByGuid(string entityName, Guid guid, ColumnSet cols)
        {
            return sysService.Retrieve(entityName, guid, cols);
        }

        private EntityCollection GetEntityCollectionActive(string entityName, string attributeName, string attributeValue, ColumnSet cols)
        {
            QueryExpression query = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = cols,
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression(attributeName, ConditionOperator.Equal, attributeValue),
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                    }
                }
            };

            return sysService.RetrieveMultiple(query);
        }

        private EntityCollection GetEntityCollection(string entityName, string attributeName, string attributeValue, ColumnSet cols)
        {
            QueryExpression query = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = cols,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = attributeName,
                            Operator = ConditionOperator.Equal,
                            Values = { attributeValue }
                        }
                    }
                }
            };
            return sysService.RetrieveMultiple(query);
        }

        private EntityCollection GetEntityCollectionFromNtoNrelationship(string targetEntityName, string targetEntityAttr, string sourceEntityName, string sourcetEntityAttr, Guid sourceId, string manyToManyRelationshipName)
        {
            QueryExpression query = new QueryExpression(targetEntityName)
            {
                ColumnSet = new ColumnSet(true),
                LinkEntities =
            {
                new LinkEntity
                {
                    LinkFromEntityName = targetEntityName,
                    LinkToEntityName = manyToManyRelationshipName,
                    LinkFromAttributeName = targetEntityAttr,
                    LinkToAttributeName = targetEntityAttr,
                    LinkEntities =
                    {
                        new LinkEntity
                        {
                            LinkFromEntityName = manyToManyRelationshipName,
                            LinkToEntityName = sourceEntityName,
                            LinkFromAttributeName = sourcetEntityAttr,
                            LinkToAttributeName = sourcetEntityAttr,
                            LinkCriteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression(sourcetEntityAttr, ConditionOperator.Equal, sourceId)
                                }
                            }
                        }
                    }
                }
                }
            };

            return sysService.RetrieveMultiple(query);
        }



    }
}
