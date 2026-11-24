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

namespace Reservations.EventItem
{
    class EventItemService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;


        public EventItemService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }
        internal void SetActivityDayForConferenceScheduleItem(Entity preEntity, Entity postEntity, Entity updateEntity,ref bool bIsUpdate)
        {
            try
            {

                var eventType = EntityUtils.GetOptionSetValueFromEntityValue("new_event_type", postEntity);
                Guid activityDayGuid = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", postEntity);
                if (eventType == 9 && activityDayGuid == Guid.Empty)
                { // event_item type is equal to schedule for conference and activity day is not set yet.
                    var eventResrvation = EntityUtils.GetEntityReferenceFromEntity("new_event_reservations", postEntity);
                    if (eventResrvation.Id != Guid.Empty)
                    {
                        bIsUpdate = true;
                        string description = "שיבוץ<br/>כנס: <a href='/events/training/" + eventResrvation.Id + "' target='_blank'>" + eventResrvation.Name + "</a>";
                        var yvProduct = EntityUtils.GetEntityReferenceFromEntity("new_yv_product", postEntity);
                        if (!string.IsNullOrEmpty(yvProduct.Name)) {
                            description += "<br/>מוצר: "+ yvProduct.Name + " <br/> ";
                        }
                        updateEntity["new_description"] = description;
                        //get activity day for event_reservations:
                        #region query
                        QueryExpression query = new QueryExpression();
                        query.EntityName = "new_activity_day";
                        ColumnSet cols = new ColumnSet();
                        cols.AddColumns(new string[] { "new_activity_dayid", "new_event_reservations" , "new_start_datetime" });
                        query.ColumnSet = cols;
                        FilterExpression fe = new FilterExpression();
                        fe.FilterOperator = LogicalOperator.And;
                        fe.Conditions.Add(new ConditionExpression("new_event_reservations", ConditionOperator.Equal, eventResrvation.Id));
                        fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                        query.Criteria.AddFilter(fe);
                        RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                        requestMult.Query = query;
                        RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
                        #endregion
                        foreach (var activityDayEntity in respMult.EntityCollection.Entities)
                        {
                            var activityDayStart = activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime");
                            var eventItemStart = postEntity.GetAttributeValue<DateTime>("new_start_datetime");
                            if (activityDayStart.ToString("dd/MM/yyyy") == eventItemStart.ToString("dd/MM/yyyy"))
                            {
                                updateEntity["new_activity_day"] = new EntityReference("new_activity_day", activityDayEntity.Id);
                            }
                        }

                    }

                }

            }
            catch (Exception ex) {
                WriteToLog.LogToFileError("Set ActivityDay For Conference Schedule Item failed ----" + ex.Message, "\n");

            }

        }
        internal void UpdateActivityDayResourcesAmounts(Entity preEntity, Entity postEntity)
        {
            Guid museumGuid = new Guid(Config.GetConfigByKey("museumGuid", sysService));
            Guid campusGuid = new Guid(Config.GetConfigByKey("campusGuid", sysService));
            Guid bigQuestionsGuid = new Guid(Config.GetConfigByKey("bigQuestionsGuid", sysService));

            Guid gPreResource = EntityUtils.GetEntityReferenceFromEntityValue("new_resource", preEntity);
            Guid gPostResource = EntityUtils.GetEntityReferenceFromEntityValue("new_resource", postEntity);

            Guid gPreActivityDay = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", preEntity);
            Guid gPostActivityDay = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", postEntity);

            if (gPreResource != museumGuid && gPreResource != campusGuid && gPreResource != bigQuestionsGuid
                && gPostResource != museumGuid && gPostResource != campusGuid && gPostResource != bigQuestionsGuid)
            {
                return;
            }

            //get all event items with the resource and same activity day:
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_event_item";

            ColumnSet cols = new ColumnSet();
            cols.AddColumns(new string[] { "new_activity_day", "new_resource" });
            query.ColumnSet = cols;

            FilterExpression fe = new FilterExpression();
            fe.FilterOperator = LogicalOperator.And;
            fe.Conditions.Add(new ConditionExpression("new_resource", ConditionOperator.Equal, gPostResource));
            fe.Conditions.Add(new ConditionExpression("new_activity_day", ConditionOperator.Equal, gPostActivityDay));
            fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            query.Criteria.AddFilter(fe);

            RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
            requestMult.Query = query;

            RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
            #endregion


            try
            {
                Entity ad = new Entity("new_activity_day");
                ad.Id = gPostActivityDay;
                if (gPostResource == museumGuid)
                    ad["new_museum_amount_scheduled"] = respMult.EntityCollection.Entities.Count;
                if (gPostResource == campusGuid)
                    ad["new_campus_amount_scheduled"] = respMult.EntityCollection.Entities.Count;
                if (gPostResource == bigQuestionsGuid)
                    ad["new_big_questions_amount_scheduled"] = respMult.EntityCollection.Entities.Count;

                sysService.Update(ad);

            }
            catch (Exception ex)
            {

                WriteToLog.LogToFile("update activity_day failed in event item ----" + ex.Message, "\n");

            }

            if (((gPreResource.CompareTo(gPostResource) != 0 && gPreResource.CompareTo(Guid.Empty) != 0)) ||
                 ((gPreActivityDay.CompareTo(gPostActivityDay) != 0 && gPreActivityDay.CompareTo(Guid.Empty) != 0)))
            {
                // resource or activity day was changed - need to update pre too:
                #region query
                query = new QueryExpression();
                query.EntityName = "new_event_item";

                cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_activity_day", "new_resource" });
                query.ColumnSet = cols;

                fe = new FilterExpression();
                fe.FilterOperator = LogicalOperator.And;
                fe.Conditions.Add(new ConditionExpression("new_resource", ConditionOperator.Equal, gPreResource));
                fe.Conditions.Add(new ConditionExpression("new_activity_day", ConditionOperator.Equal, gPreActivityDay));
                fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                query.Criteria.AddFilter(fe);

                requestMult = new RetrieveMultipleRequest();
                requestMult.Query = query;

                respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
                #endregion


                try
                {
                    Entity ad = new Entity("new_activity_day");
                    ad.Id = gPostActivityDay;
                    if (gPostResource == museumGuid)
                        ad["new_museum_amount_scheduled"] = respMult.EntityCollection.Entities.Count;
                    if (gPostResource == campusGuid)
                        ad["new_campus_amount_scheduled"] = respMult.EntityCollection.Entities.Count;
                    if (gPostResource == bigQuestionsGuid)
                        ad["new_big_questions_amount_scheduled"] = respMult.EntityCollection.Entities.Count;

                    sysService.Update(ad);

                }
                catch (Exception ex)
                {

                    WriteToLog.LogToFile("update activity_day failed in event item 2----" + ex.Message, "\n");

                }
            }

        }

        internal void UpdateItineraryGroupParticipantAmount(Entity preEntity, Entity postEntity)
        {

            Guid museumGuid = new Guid(Config.GetConfigByKey("museumGuid", sysService));
            Guid gPostResource = EntityUtils.GetEntityReferenceFromEntityValue("new_resource", postEntity);
            int prevAmount = EntityUtils.GetIntFromEntityValue("new_number_of_participants", preEntity);
            int newAmount = EntityUtils.GetIntFromEntityValue("new_number_of_participants", postEntity);
            int newStatus = EntityUtils.GetOptionSetValueFromEntityValue("new_status", postEntity);


            try
            {
                if (museumGuid.CompareTo(gPostResource) == 0 && prevAmount != newAmount && newStatus == 1)  //museum participants amount changed
                {
                    EntityCollection colItineraryItem = GetEntityCollection("new_itinerary_item", "new_scheduler_resource", postEntity.Id.ToString(), new ColumnSet(true));
                    EntityCollection colItineraryItemGroup = GetEntityCollection("new_new_itinerary_item_new_itinerary_group", "new_itinerary_itemid", colItineraryItem[0].Id.ToString(), new ColumnSet(true) );


                    //EntityCollection colItineraryGroup = GetEntityCollectionFromNtoNrelationship("new_itinerary_group", "new_itinerary_groupid", "new_itinerary_item", "new_itinerary_itemid", colItineraryItem[0].Id, "new_new_itinerary_item_new_itinerary_group");
                    Entity eGroup = new Entity("new_itinerary_group");
                    eGroup.Id = (Guid)colItineraryItemGroup[0]["new_itinerary_groupid"];
                    //eGroup.Id = colItineraryGroup[0].Id;
                    eGroup["new_number_of_participants"] = newAmount;
                    sysService.Update(eGroup);
                }
            }
            catch (Exception ex)
            {

                WriteToLog.LogToFile("update Itinerary Group failed ----" + ex.Message, "\n");

            }


        }

        private Entity GetEntityByGuid(string entityName, Guid guid, ColumnSet cols)
        {
            return sysService.Retrieve(entityName, guid, cols);
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