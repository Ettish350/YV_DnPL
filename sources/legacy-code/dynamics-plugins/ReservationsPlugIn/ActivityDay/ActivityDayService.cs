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

namespace Reservations.ActivityDay
{
    class ActivityDayService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;

     
        public ActivityDayService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }
        internal void SeActualNumberOfVisitorsOnNumAdditionalParticipantsChanged(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity) {

            int gPreNumAdditionalParticipants = EntityUtils.GetIntFromEntityValue("new_num_additional_participants", preEntity);
            int gPostNumAdditionalParticipants = EntityUtils.GetIntFromEntityValue("new_num_additional_participants", postEntity);
            if (gPostNumAdditionalParticipants != gPreNumAdditionalParticipants) {

                int numOfParticipants = EntityUtils.GetIntFromEntityValue("new_number_of_participants", postEntity);
                int actualNumOfParticipants = numOfParticipants + gPostNumAdditionalParticipants;

                eEntity["new_actual_number_of_participants"] = actualNumOfParticipants;
                bIsUpdate = true;
                //update event_reservation new_num_additional_participants feild
                var eventReservation = EntityUtils.GetEntityReferenceFromEntity("new_event_reservations", postEntity);
                if (eventReservation != null && !eventReservation.Id.Equals(Guid.Empty)) {
                    OrganizationServiceContext orgContext = new OrganizationServiceContext(sysService);
                    //select all activity day that refereneced to that EventReservation
                    #region query
                    QueryExpression query = new QueryExpression("new_activity_day");
                    ColumnSet cols = new ColumnSet();
                    cols.AddColumns(new string[] { "new_num_additional_participants" });
                    query.ColumnSet = cols;
                    FilterExpression fe = new FilterExpression();
                    fe.FilterOperator = LogicalOperator.And;
                    fe.Conditions.Add(new ConditionExpression("new_event_reservations", ConditionOperator.Equal, eventReservation.Id));
                    fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                    fe.Conditions.Add(new ConditionExpression("new_activity_dayid", ConditionOperator.NotEqual, postEntity.Id));
                    query.Criteria.AddFilter(fe);
                    RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                    requestMult.Query = query;
                    RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
                    #endregion

                    var sum = respMult.EntityCollection.Entities.ToList().Select(entity => entity.GetAttributeValue<int>("new_num_additional_participants")).Sum();
                    sum += gPostNumAdditionalParticipants;

                    try
                    {
                        //update reservation
                        Entity eventReservationEntity = new Entity("new_event_reservations",eventReservation.Id);
                        eventReservationEntity["new_num_additional_participants"] = sum;
                        sysService.Update(eventReservationEntity);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidPluginExecutionException("Exception in function SeActualNumberOfVisitorsOnNumAdditionalParticipantsChanged:" + ex.Message, ex);

                    }

                }
            }
          

        }

        //update activityDay name by its first date
        internal void SetActivityDayNameByFirstDate(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
 
            Guid gPostEventReserations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_reservations", postEntity);
            string sPostStartDatetime = EntityUtils.GetDateTimeFromEntityValue("new_start_datetime", postEntity);
            string postName = EntityUtils.GetStringFromEntityValue("new_name", postEntity);
            string stringEventName = String.Empty;

            if (gPostEventReserations.CompareTo(Guid.Empty) != 0)
            {
                //name of event
                #region query
                QueryExpression query = new QueryExpression();
                query.EntityName = "new_event_reservations";

                ColumnSet cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_name" });
                query.ColumnSet = cols;


                FilterExpression fe = new FilterExpression();
                fe.FilterOperator = LogicalOperator.And;
                fe.Conditions.Add(new ConditionExpression("new_event_reservationsid", ConditionOperator.Equal, gPostEventReserations));
                fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                query.Criteria.AddFilter(fe);


                RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                requestMult.Query = query;

                RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;


                #endregion

                if (respMult.EntityCollection.Entities.Count > 0)
                {

                    Entity eItem = respMult.EntityCollection.Entities[0];
                    stringEventName = eItem["new_name"].ToString();

                }
            }

            WriteToLog.LogToFile("SetActivityDayNameByFirstDate : old name : " + postName + ", sPostStartDatetime : " + sPostStartDatetime + " event name :" + stringEventName, "");
            DateTime dateValueParsed;
            string actualDateName = "Activity Day -> ";

            if (DateTime.TryParse(sPostStartDatetime, out dateValueParsed))
                actualDateName += dateValueParsed.ToLocalTime().ToString("dd.MM.yyyy hh:mm tt");
            else
                actualDateName += DateTime.Now.ToString("dd.MM.yyyy hh:mm tt");

            actualDateName += stringEventName;

            WriteToLog.LogToFile("SetActivityDayNameByFirstDate : new name : " + actualDateName, "");

            if (String.Compare(actualDateName, postName) != 0)
            {
                eEntity["new_name"] = actualDateName;
                bIsUpdate = true;
            }

        } /*SetActivityDayNameByFirstDate*/



        internal void SetReservationActivityDaFirstDate(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            //עדכון הזמנה-תאריך יופ פעילות מינימלי
            //בדיקה אם ביום הפעילות או ששדה ההזמנה השתנה או ששדה תאריך

            Guid gPreReservation = EntityUtils.GetEntityReferenceFromEntityValue("new_reservation", preEntity);
            Guid gPostReservation = EntityUtils.GetEntityReferenceFromEntityValue("new_reservation", postEntity);
            string gPreStartDatetime = EntityUtils.GetDateTimeFromEntityValue("new_start_datetime", preEntity);
            string gPostStartDatetime = EntityUtils.GetDateTimeFromEntityValue("new_start_datetime", postEntity);

            if ((gPreReservation.CompareTo(gPostReservation) != 0 && gPostReservation.CompareTo(Guid.Empty) != 0)
                ||(gPreStartDatetime.CompareTo(gPostStartDatetime) != 0))
            {

                //שליפת כל ימי העיון שמקושרים להזמנה זו
                #region query
                QueryExpression query = new QueryExpression();
                query.EntityName = "new_activity_day";

                ColumnSet cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_reservation", "new_start_datetime" });
                query.ColumnSet = cols;


                FilterExpression fe = new FilterExpression();
                fe.FilterOperator = LogicalOperator.And;
                fe.Conditions.Add(new ConditionExpression("new_reservation", ConditionOperator.Equal, gPostReservation));
                fe.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                query.Criteria.AddFilter(fe);

                OrderExpression ord = new OrderExpression("new_start_datetime", OrderType.Ascending);
                query.Orders.Add(ord);

                RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                requestMult.Query = query;

                RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;


                #endregion
                
                if (respMult.EntityCollection.Entities.Count > 0)
                {
                    try
                    {
                        //update reservation
                        Entity eItem = respMult.EntityCollection.Entities[0];

                        Entity eres = new Entity("new_reservation");
                        eres.Id = gPostReservation;
                        eres["new_activity_day_first_date"] = EntityUtils.GetDateTimeFromEntity("new_start_datetime", eItem);

                        sysService.Update(eres);
                    }
                    catch (Exception ex)
                    {

                        throw new InvalidPluginExecutionException("Exception in function SetReservationActivityDaFirstDate:" + ex.Message, ex);

                    }


                }
            }
        }

        internal void UpdateIsNeedScheduling(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            bool is_need_scheduling = false;
            //new_is_need_scheduling

            if (EntityUtils.GetIntFromEntityValue("new_museum_amount_scheduled", postEntity) < EntityUtils.GetIntFromEntityValue("new_museum_amount_needed", postEntity))
                is_need_scheduling = true;

            //if (EntityUtils.GetIntFromEntityValue("new_campus_amount_scheduled", postEntity) < EntityUtils.GetIntFromEntityValue("new_campus_amount_needed", postEntity))
              //  is_need_scheduling = true;

            if (EntityUtils.GetIntFromEntityValue("new_big_questions_amount_scheduled", postEntity) < EntityUtils.GetIntFromEntityValue("new_big_questions_amount_needed", postEntity))
                is_need_scheduling = true;

            if (EntityUtils.GetIntFromEntityValue("new_guide_amount_scheduled", postEntity) < EntityUtils.GetIntFromEntityValue("new_guide_amount_needed", postEntity))
                is_need_scheduling = true;

            if (EntityUtils.GetIntFromEntityValue("new_testimony_amount_scheduled", postEntity) < EntityUtils.GetIntFromEntityValue("new_testimony_amount_needed", postEntity))
                is_need_scheduling = true;

            if (EntityUtils.GetIntFromEntityValue("new_cantor_amount_scheduled", postEntity) < EntityUtils.GetIntFromEntityValue("new_cantor_amount_needed", postEntity))
                is_need_scheduling = true;

            if (EntityUtils.GetIntFromEntityValue("new_lecturer_amount_scheduled", postEntity) < EntityUtils.GetIntFromEntityValue("new_lecturer_amount_needed", postEntity))
                is_need_scheduling = true;

            if (is_need_scheduling != EntityUtils.GetBoolFromEntityValue("new_is_need_scheduling", postEntity)) {
                //EntityUtils.GetBoolFromEntityValue("new_is_need_scheduling", postEntity)
                //new_is_need_scheduling
                eEntity["new_is_need_scheduling"] = is_need_scheduling;
                bIsUpdate = true;
            }

        }


        internal void UpdateArrivalTime(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {

            bool gPreDidGroupArrive = EntityUtils.GetBoolFromEntityValue("new_did_group_arrive", preEntity);
            bool gPostDidGroupArrive = EntityUtils.GetBoolFromEntityValue("new_did_group_arrive", postEntity);


            if (gPreDidGroupArrive != gPostDidGroupArrive)
            {
                //if the DidGroupArrive value was changed to true - set today(), else remove the previous value
                if (gPostDidGroupArrive)
                    eEntity["new_arrival_time"] = DateTime.Now;
                else
                    eEntity["new_arrival_time"] = null;

                bIsUpdate = true;
            }


        }

    }
}
