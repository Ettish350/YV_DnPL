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

namespace Reservations.EventRegistration
{
    class EventRegistrationService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;


        public EventRegistrationService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }



        internal void SetNumerator(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            Guid gPreEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_reservations", preEntity);
            Guid gPostEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_reservations", postEntity);
            if (gPreEventReservations.CompareTo(gPostEventReservations) != 0 && gPostEventReservations.CompareTo(Guid.Empty) != 0)
            {
                //select event budgetframework
                Entity eventr = sysService.Retrieve("new_event_reservations", gPostEventReservations, new ColumnSet(new string[] { "new_budget_framework", "new_event_type" }));
                //get type

                int iPostEventType = EntityUtils.GetOptionSetValueFromEntityValue("new_event_type", eventr);

                var sPreFix = "";
                switch (iPostEventType)
                {
                    case 1://השתלמות
                        sPreFix = "YVT";
                        break;
                    case 2:// יום עיון
                        sPreFix = "YVD";
                        break;
                    case 3://סמינר
                        sPreFix = "YVS";
                        break;
                    case 4://סיור מערכת הזמנות מקוונת
                        sPreFix = "YVR";
                        break;
                    case 5://הכשרה
                        sPreFix = "YVG";
                        break;
                    case 8://כנס
                        sPreFix = "YVC";
                        break;
                    default:
                        sPreFix = "YVV";
                        break;
                }

                /*
                                Guid gBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", eventr);
                                if (gBudgetFramework != Guid.Empty)
                                {
                                    WriteToLog.LogToFile("\nevent reservation change gBudgetFramework" + gBudgetFramework, "");

                                    //select per pix
                                    #region query
                                    QueryExpression query = new QueryExpression();
                                    query.EntityName = "new_budget_framework";

                                    ColumnSet cols = new ColumnSet();
                                    cols.AddColumns(new string[] { "new_budget_frameworkid", "new_rsv_department" });
                                    query.ColumnSet = cols;


                                    FilterExpression fe = new FilterExpression();
                                    fe.FilterOperator = LogicalOperator.And;
                                    fe.Conditions.Add(new ConditionExpression("new_budget_frameworkid", ConditionOperator.Equal, gBudgetFramework));
                                    query.Criteria.AddFilter(fe);

                                    LinkEntity le = new LinkEntity("new_budget_framework", "new_rsv_department", "new_rsv_department", "new_rsv_departmentid", JoinOperator.Inner);
                                    le.Columns.AddColumn("new_prefix");
                                    le.EntityAlias = "rsvd";
                                    query.LinkEntities.Add(le);
                                    RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                                    requestMult.Query = query;

                                    RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;


                                    if (respMult.EntityCollection.Entities.Count > 0)
                                    {
                                        Entity eItem = respMult.EntityCollection.Entities[0];

                                        string sPreFix = EntityUtils.GetStringAliasedFromEntityValue("rsvd.new_prefix", eItem);

                                        WriteToLog.LogToFile("\nsPreFix" + sPreFix, "");
                  */
                string sNum = EntityUtils.GetStringFromEntityValue("new_num", postEntity);
                if (sNum.Length > 3)
                {
                    sNum = sPreFix + sNum.Substring(3);
                    eEntity["new_num"] = sNum;
                    bIsUpdate = true;
                }
            }
        }




        //internal void UpdateEventTotalParticipants(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        //{
        //    //אם הארוע השתנה -תסיר כמות מארוע קודם ותוסיף כמות לארוע חדש
        //    //אם מס' המשתתפים השתנה תשנה את מס' המשתתפים בארוע
        //    WriteToLog.LogToFile("\nfunction UpdateEventTotalParticipants1", "");
        //    Guid gPreEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_registration", preEntity);
        //    Guid gPostEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_registration", postEntity);
        //    int iperNumberOfParticipants = EntityUtils.GetIntFromEntityValue("new_number_of_participants", preEntity);
        //    int ipostNumberOfParticipants = EntityUtils.GetIntFromEntityValue("new_number_of_participants", postEntity);
        //    int iPreState = EntityUtils.GetOptionSetValueFromEntityValue("statecode", preEntity);
        //    int iPostState = EntityUtils.GetOptionSetValueFromEntityValue("statecode", postEntity);

        //    if (gPreEventReservations.CompareTo(gPostEventReservations) != 0)
        //    {
        //        WriteToLog.LogToFile("event reservation change", "");
        //        try
        //        {
        //            if (gPostEventReservations != Guid.Empty)
        //            {
        //                //הוספת מס' משתתפים חדש לארוע חדש
        //                //updata post entity post praticipent
        //                Entity eEventReservations = sysService.Retrieve("new_event_reservations", gPostEventReservations, new ColumnSet(true));
        //                eEventReservations["new_total_participants"] = EntityUtils.GetIntFromEntityValue("new_total_participants", eEventReservations) + ipostNumberOfParticipants;
        //                sysService.Update(eEventReservations);
        //                WriteToLog.LogToFile("\nupdate sucsess post entity", "");
        //            }
        //            //
        //            if (gPreEventReservations != Guid.Empty)
        //            {
        //                //הורדת מס' משתתפים מארוע קודם
        //                Entity epreEventReservations = sysService.Retrieve("new_event_reservations", gPreEventReservations, new ColumnSet(true));
        //                epreEventReservations["new_total_participants"] = EntityUtils.GetIntFromEntityValue("new_total_participants", epreEventReservations) - iperNumberOfParticipants;
        //                sysService.Update(epreEventReservations);
        //                WriteToLog.LogToFile("\nupdate sucsess pre entity", "");
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            WriteToLog.LogToFile("\nueror UpdateEventTotalParticipants   " + ex.Message, "");
        //        }
        //    }
        //    //אם רק כמות המשתתפים השתנתה הוספת ההפרש
        //    else if (iperNumberOfParticipants.CompareTo(ipostNumberOfParticipants) != 0)
        //    {
        //        WriteToLog.LogToFile("NumberOfParticipants change", "");
        //        try
        //        {
        //            Guid gevent = Guid.Empty;

        //            if (gPostEventReservations != Guid.Empty)
        //                gevent = gPostEventReservations;
        //            else if (gPreEventReservations != Guid.Empty)
        //                gevent = gPreEventReservations;
        //            if (gevent != Guid.Empty)
        //            {
        //                Entity eEventReservations = sysService.Retrieve("new_event_reservations", gevent, new ColumnSet(true));

        //                //ההפרש הוא 
        //                int h = ipostNumberOfParticipants - iperNumberOfParticipants;
        //                WriteToLog.LogToFile("\nההפרש הוא " + h, "");
        //                eEventReservations["new_total_participants"] = EntityUtils.GetIntFromEntityValue("new_total_participants", eEventReservations) + h;
        //                sysService.Update(eEventReservations);
        //                WriteToLog.LogToFile("\nupdate sucsess post entity new_total_participants", "");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            WriteToLog.LogToFile("\nupdate 2 faild " + ex.Message, "");
        //        }
        //    }
        //    if (iPreState.CompareTo(iPostState) != 0)
        //    {
        //        //statcode change
        //        WriteToLog.LogToFile("\nstatcode change", "");
        //        if (iPostState == 1)
        //        {
        //            //אם השתנה ללא פעיל
        //            WriteToLog.LogToFile("\nאם השתנה ללא פעיל", "");
        //            try
        //            {
        //                //הורדת מס' משתתפים מארוע קודם
        //                Entity epreEventReservations = sysService.Retrieve("new_event_reservations", gPreEventReservations, new ColumnSet(true));
        //                epreEventReservations["new_total_participants"] = EntityUtils.GetIntFromEntityValue("new_total_participants", epreEventReservations) - iperNumberOfParticipants;
        //                sysService.Update(epreEventReservations);
        //                WriteToLog.LogToFile("\nupdate sucsess pre entity", "");
        //            }
        //            catch (Exception ex)
        //            {
        //                WriteToLog.LogToFile("\nupdate 3 faild " + ex.Message, "");
        //            }
        //        }
        //        else
        //        {
        //            WriteToLog.LogToFile("\nאם השתנה לפעיל", "");
        //            //אם השתנה לפעיל
        //            try
        //            {
        //                //הוספת מס' משתתפים חדש לארוע חדש
        //                //updata post entity post praticipent
        //                Entity eEventReservations = sysService.Retrieve("new_event_reservations", gPostEventReservations, new ColumnSet(true));
        //                eEventReservations["new_total_participants"] = EntityUtils.GetIntFromEntityValue("new_total_participants", eEventReservations) + ipostNumberOfParticipants;
        //                sysService.Update(eEventReservations);
        //                WriteToLog.LogToFile("\nupdate sucsess post entity", "");
        //            }
        //            catch (Exception ex)
        //            {
        //                WriteToLog.LogToFile("\nupdate 4 faild " + ex.Message, "");
        //            }
        //        }
        //    }
        //    //    #region query
        //    //    QueryExpression query = new QueryExpression();
        //    //    query.EntityName = "new_event_registration";

        //    //    ColumnSet cols = new ColumnSet();
        //    //    cols.AddColumns(new string[] { "new_event_reservationsid", "new_event_reservationsname", "new_number_of_participants" });
        //    //    query.ColumnSet = cols;


        //    //    FilterExpression fe = new FilterExpression();
        //    //    fe.FilterOperator = LogicalOperator.And;
        //    //    fe.Conditions.Add(new ConditionExpression("new_event_reservationsid", ConditionOperator.Equal, gPostEventReservations));
        //    //    query.Criteria.AddFilter(fe);


        //    //    RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
        //    //    requestMult.Query = query;

        //    //    RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;


        //    //    #endregion
        //    //    if (respMult.EntityCollection.Entities.Count > 0)
        //    //    {
        //    //        WriteToLog.LogToFile("\nrespMult.EntityCollection.Entities.Count : " + respMult.EntityCollection.Entities.Count.ToString(), "");

        //    //        int totalcount = 0;

        //    //        foreach (Entity e in respMult.EntityCollection.Entities)
        //    //        {
        //    //            totalcount += EntityUtils.GetIntFromEntityValue("new_number_of_participants", e);
        //    //        }
        //    //        WriteToLog.LogToFile("\ntotalcount " + totalcount.ToString() , "");
        //    //        try
        //    //        {
        //    //            Entity eEventReservations = new Entity("new_event_reservations", gPostEventReservations);
        //    //            eEventReservations["new_total_participants"] = totalcount;
        //    //            sysService.Update(eEventReservations);
        //    //            WriteToLog.LogToFile("\nupdate sucsess", "");

        //    //        }
        //    //        catch (Exception ex)
        //    //        {

        //    //            WriteToLog.LogToFile("\nupdate faild " + ex.Message, "");
        //    //        }


        //    //    }
        //    // }
        //    //  }
        //}

        internal void CheckIfEventRegistrationHasBeenDeactivate(Entity preEntity, Entity postEntity)
        {
            OptionSetValue myState = (OptionSetValue)postEntity.Attributes["statecode"];

            if (myState.Value == 1)
            {
                DeactivateEventRegistrationActivityDayOnDeleteEventRegistration(postEntity);
            }

        }
        internal void UpdateEventTotalParticipants(Entity preEntity, Entity postEntity)
        {

            OrganizationServiceContext orgContext = new OrganizationServiceContext(sysService);
            //new_event_reservations -שדה ארוע מערת אחודה
            Guid gPreEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_reservations", preEntity);
            Guid gPostEventReservations = EntityUtils.GetEntityReferenceFromEntityValue("new_event_reservations", postEntity);

            //new_is_not_for_online_registration
            Entity eEventReservations = gf.GetRecord("new_event_reservations", gPostEventReservations, new string[] { "new_is_not_for_online_registration", "new_num_additional_participants" });
            //WriteToLog.LogToFile("eEventReservations guid:" + eEventReservations.Id.ToString(), "\n");
            //WriteToLog.LogToFile("new_is_not_for_online_registration " + EntityUtils.GetBoolFromEntityValue("new_is_not_for_online_registration", eEventReservations), "\n");
            //if (EntityUtils.GetBoolFromEntityValue("new_is_not_for_online_registration", eEventReservations))
            //{
            //  return;
            //}

            //select all registeration of spcifice event
            var r = from con in orgContext.CreateQuery("new_event_registration")
                    where (((string)con["new_event_reservations"]).Equals(gPostEventReservations.ToString().ToUpper()) && (con["statecode"]).Equals(0))
                    select new { nump = (con["new_number_of_participants"]) };

            var sum = r.ToList().Select(x => Convert.ToInt32(x.nump)).Sum();
            int numAdditional = EntityUtils.GetIntFromEntityValue("new_num_additional_participants", eEventReservations);
            sum += numAdditional;
            try
            {
                Entity ad = new Entity("new_event_reservations");
                ad.Id = gPostEventReservations;
                ad["new_total_participants"] = sum;
                sysService.Update(ad);
            }
            catch (Exception ex)
            {

                WriteToLog.LogToFile("update event reservations failed ----" + ex.Message, "\n");

            }

            if (gPreEventReservations != Guid.Empty && gPreEventReservations.CompareTo(gPostEventReservations) != 0)
            {
                WriteToLog.LogToFile("update pre event reservations ", "\n");
                eEventReservations = gf.GetRecord("new_event_reservations", gPreEventReservations, new string[] { "new_num_additional_participants" });

                var r1 = from con in orgContext.CreateQuery("new_event_registration")
                         where (((string)con["new_event_reservations"]).Equals(gPreEventReservations.ToString().ToUpper()) && (con["statecode"]).Equals(0))
                         select new { nump = (con["new_number_of_participants"]) };

                var sum2 = r1.ToList().Select(x => Convert.ToInt32(x.nump)).Sum();
                sum2 += EntityUtils.GetIntFromEntityValue("new_num_additional_participants", eEventReservations);

                try
                {
                    Entity ad = new Entity("new_event_reservations");
                    ad.Id = gPreEventReservations;
                    ad["new_total_participants"] = sum2;
                    sysService.Update(ad);
                }
                catch (Exception ex)
                {

                    WriteToLog.LogToFile("update pre event reservations failde ----" + ex.Message, "\n");

                }

            }


        }

        internal void UpdateAllActivityDayTotalParticipants(Entity preEntity, Entity postEntity)
        {
            int gPreNumberOfParticipants = EntityUtils.GetIntFromEntityValue("new_number_of_participants", preEntity);
            int gPostNumberOfParticipants = EntityUtils.GetIntFromEntityValue("new_number_of_participants", postEntity);
            int gPreActualNumberOfParticipants = EntityUtils.GetIntFromEntityValue("new_actual_number_of_participants", preEntity);
            int gPostActualNumberOfParticipants = EntityUtils.GetIntFromEntityValue("new_actual_number_of_participants", postEntity);
            //if numberParticipants change
            if (gPostNumberOfParticipants.CompareTo(gPreNumberOfParticipants) != 0 ||
                gPostActualNumberOfParticipants.CompareTo(gPreActualNumberOfParticipants) != 0)
            {
                //select all activity day that contain this EventRegistration
                #region query
                QueryExpression query = new QueryExpression();
                query.EntityName = "new_event_registration_activity_day";

                ColumnSet cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_activity_day" });
                query.ColumnSet = cols;


                FilterExpression fe = new FilterExpression();
                fe.FilterOperator = LogicalOperator.And;
                fe.Conditions.Add(new ConditionExpression("new_event_registration", ConditionOperator.Equal, postEntity.Id));
                query.Criteria.AddFilter(fe);


                RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                requestMult.Query = query;

                RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;

                WriteToLog.LogToFile("Hadassah log: I am here out", "");

                #endregion
                if (respMult.EntityCollection.Entities.Count > 0)
                {
                    foreach (Entity e in respMult.EntityCollection.Entities)
                    {
                        //foreach activity day update total participants

                        WriteToLog.LogToFile("Hadassah log: I am here in foreach", "");

                        UpdateActivityDayTotalParticipants(EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", e));
                    }
                }
            }
        }

        internal void DeactivateItem(Entity eEntity)
        {
            SetStateRequest setStateRequest = new SetStateRequest()
            {
                EntityMoniker = new EntityReference
                {
                    Id = eEntity.Id,
                    LogicalName = eEntity.LogicalName,
                },
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            };
            sysService.Execute(setStateRequest);
        }

        internal void DeactivateEventRegistrationActivityDayOnDeleteEventRegistration(Entity postEntity)
        {

            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_event_registration_activity_day";

            ColumnSet cols = new ColumnSet();
            cols.AddColumns(new string[] { "new_event_registration_activity_dayid" });
            query.ColumnSet = cols;


            FilterExpression fe = new FilterExpression();
            fe.FilterOperator = LogicalOperator.And;
            fe.Conditions.Add(new ConditionExpression("new_event_registration", ConditionOperator.Equal, postEntity.Id));
            query.Criteria.AddFilter(fe);


            RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
            requestMult.Query = query;

            RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;


            #endregion
            if (respMult.EntityCollection.Entities.Count > 0)
            {
                foreach (Entity entity in respMult.EntityCollection.Entities)
                {
                    //Set every event registration activity day to be inactive if its event registration has been deleted.
                    DeactivateItem(entity);

                }
            }

        }

        private void UpdateActivityDayTotalParticipants(Guid e)
        {
            WriteToLog.LogToFile("before select update new_event_registration 1", "\n");

            OrganizationServiceContext orgContext = new OrganizationServiceContext(sysService);

            var r = from con in orgContext.CreateQuery("new_event_registration_activity_day")
                    join acd in orgContext.CreateQuery("new_event_registration")
                    on con["new_event_registration"] equals acd["new_event_registrationid"]
                    where (((string)con["new_activity_day"]).Equals(e.ToString().ToUpper()) && (con["statecode"]).Equals(0))
                    select new {
                        nump = acd.GetAttributeValue<int?>("new_number_of_participants"),
                        numap = acd.GetAttributeValue<int?>("new_actual_number_of_participants")
                    };
            var list = r.ToList();

            var sum = list.Sum(x => x.nump.Value);
            var actualSum = list.Sum(x => x.numap.HasValue ? x.numap.Value : x.nump.Value);
            WriteToLog.LogToFile("Hadassah log: I am here actual num" + actualSum, "");

            try
            {
                var activityDayEntity = userService.Retrieve("new_activity_day", e, new ColumnSet(new string[] { "new_num_additional_participants" }));
                var actualNumOfParticipants = actualSum + activityDayEntity.GetAttributeValue<int>("new_num_additional_participants");
                WriteToLog.LogToFile("Hadassah log: I am here before", "");

                Entity ad = new Entity("new_activity_day", e);

                ad["new_number_of_participants"] = sum;
                ad["new_actual_number_of_participants"] = actualNumOfParticipants;
                sysService.Update(ad);
                WriteToLog.LogToFile("Hadassah log: I am here after", "");

            }
            catch (Exception ex)
            {

                WriteToLog.LogToFile("update activity_day failde ----" + ex.Message, "\n");

            }

        }



    }
}

