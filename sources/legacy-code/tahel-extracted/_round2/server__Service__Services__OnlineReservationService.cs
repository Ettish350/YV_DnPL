using Data.Models;
using Data.Repositories.Interfaces;
using Data8.PowerPlatform.Dataverse.Client.Wsdl;
using Microsoft.Extensions.Configuration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Serilog;
using Service.Services.Interfaces;

using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Text;

using System.Data;

using ZXing;
using ZXing.Windows.Compatibility;
using ZXing.QrCode;
using System.Globalization;
using System;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net;
using System.ServiceModel.Channels;
using Microsoft.Xrm.Sdk.Discovery;
using Microsoft.Crm.Sdk.Messages;
using Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mail;
using System.Xml.Linq;
using System.Security.Policy;
using static ZXing.QrCode.Internal.Mode;
using System.IO;
using System.ComponentModel.Design;
using static System.Net.WebRequestMethods;
using ZXing.QrCode.Internal;
using static System.Net.WebRequestMethods;
using Microsoft.Extensions.ObjectPool;
using Data.Models.Data.Models;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZXing.Datamatrix.Internal;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Web;
// Importing the generated client from the WSDL
// Assuming this is the namespace for the web service

//public void SendEmailViaWebService()
//{
//    var client = new sendMailWSSoapClient(); // לדוגמה - שם המחלקה עשוי להשתנות
//    var response = client.SendMail("recipient@example.com", "Subject here", "Body of email");
//    Console.WriteLine("Mail sent successfully.");
//}
namespace Service.Services
{
    public class OnlineReservationService : IOnlineReservationService
    {
        public readonly int TIME_SLOT_MINTUE = 10;

        private readonly ICRMConnection _crmService;
        private readonly ICrmRepository _crmRepository;
        private readonly INewReservationRepository _newReservationRepository;
        private readonly IOnlineReservationRepository _onlineReservationRepository;
        private readonly IConfiguration _config;
        private readonly IHelperService _helperService;
        private readonly INewReservationService _newReservationService;
        private readonly HttpClient _httpClient;
        private readonly HoursRegulations _hoursRegulations;
        public OnlineReservationService(
             ICRMConnection crmConnection,
             ICrmRepository crmRepository,
             INewReservationRepository newReservationRepository,
             IOnlineReservationRepository onlineReservationRepository,
             IConfiguration configuration,
             IHelperService helperService,
             INewReservationService newReservationService,
             HttpClient httpClient,
             HoursRegulations hoursRegulations
             )
        {
            _crmService = crmConnection;
            _crmRepository = crmRepository;
            _newReservationRepository = newReservationRepository;
            _onlineReservationRepository = onlineReservationRepository;
            _config = configuration;
            _helperService = helperService;
            _newReservationService = newReservationService;
            _httpClient = httpClient;
            _hoursRegulations = hoursRegulations;
        }
        public ResponseWithMessage createActivitiesDaysAhead(int numOfMonth)
        {
            var response = new ResponseWithMessage(true, "");

            var conditionlist = new List<Condition>
            {
                new Condition("new_is_need_event", "eq", "1"),
                new Condition("new_show_on_website", "eq", "1"),
                new Condition("statecode", "eq", "0"),
               // new Condition("new_tour_code", "eq", "236")
            };

            var ToursList = GetTourTypesWithEventOnly(conditionlist);

            DateTime startDate = DateTime.Today;
            DateTime endDate = startDate.AddMonths(numOfMonth);

            foreach (var tour in ToursList)
            {
                DateTime? lastCheckedMonth = null;
                List<DateTime> closeDates = new List<DateTime>();

                for (DateTime currentDate = startDate; currentDate <= endDate; currentDate = currentDate.AddDays(1))
                {
                    int year = currentDate.Year;
                    int month = currentDate.Month;
                    int day = currentDate.Day; ;
                    if (lastCheckedMonth == null || lastCheckedMonth.Value.Month != month || lastCheckedMonth.Value.Year != year)
                    {
                        lastCheckedMonth = currentDate;
                        closeDates = GetCloseDates(tour.BasicDetails.TourCode, year, month, new DateTime(year, month, 1));

                    }

                    if (!closeDates.Contains(currentDate.Date))
                    {
                        var date = currentDate.ToString("dd/MM/yyyy");
                        var duration = (decimal)(tour.BasicDetails.DurationActivity != null ? tour.BasicDetails.DurationActivity : 1);
                        var result = getDateTimeSlots(date, tour.BasicDetails.Id, tour.BasicDetails.TourCode, duration);
                        foreach (var item in result)
                        {
                            //Entity tourType = GetTourByCode(tour.BasicDetails.TourCode);
                            TimeSpan timePart = TimeSpan.Parse(item.Key);
                            DateTime startTime = new DateTime(year, month, day, timePart.Hours, timePart.Minutes, 0);
                            var activityDayTodayList = _newReservationService.GetActivityDayListByEventReservation(tour.EventReservationId, startTime);
                            var entity = activityDayTodayList.Entities.ToList().Find(activityDay => activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime() == startTime);
                            var activityDayGuid = Guid.Empty;
                            if (entity == null)
                            //if (!chackIfActivityDayExistInSpecificDayAndTime(tour.BasicDetails.Id, startTime))
                            {
                                // _onlineReservationRepository.CreateActivityDayIfNotExistAhead(tourType, startTime, tour.EventReservationId);
                                OnlineReservation reservation = getNewReservationForCreateAcivityDaysAhead(tour, startTime);
                                var status = 1;
                                Entity tourType = GetTourByCode(tour.BasicDetails.TourCode);
                                //if (tourType.GetAttributeValue<bool>("new_is_guide_mandatory_per_group"))
                                //{
                                //    status = 0;
                                //}
                                Log.Information($"CreateActivityDay start startTime:{startTime},tourCode:{tour.BasicDetails.TourCode} ");
                                _onlineReservationRepository.CreateActivityDay(reservation, tourType, false, status);//לבדוק איך שולחים סטטוס- נותן שרות. 

                            }
                        }
                    }
                }
            }

            return response;
        }
        public OnlineReservation getNewReservationForCreateAcivityDaysAhead(OnlineTourType tour, DateTime startTime)
        {
            OnlineReservation reservation = new OnlineReservation();
            var reqiredProductList = GetRequireProductsList(tour.BasicDetails.TourCode, "product_required", "he");
            if (reqiredProductList.Count > 0)
            {
                reservation.BidItems = new List<BidItem>();

                foreach (var product in reqiredProductList)
                {
                    var name = product.Name ?? product.NameHe ?? product.NameEn ?? string.Empty;

                    var bidItem = new BidItem
                    {
                        Product = new IdValue(product.Id, name),
                        PricingMethod = product.PricingMethod,
                        IsRequiredServiceProvider = product.IsRequiredServiceProvider,
                        Sku = product.Sku,
                        IsReqired = true,
                        Amount = (int)product.Amount,
                        // אפשר להוסיף שדות נוספים כאן לפי הצורך
                    };

                    reservation.BidItems.Add(bidItem);
                }
            }
            reservation.ActivityPackageList = new List<ActivityPackage>();
            reservation.ReservationDate = startTime;
            // startTime, tour.EventReservationId, tourType
            reservation.StartDateTime = startTime;
            reservation.StartTime = new Time() { hours = startTime.Hour, minutes = startTime.Minute }; ;
            var group = new VisitorsGroup();
            group.StartTime = new Time() { hours = startTime.Hour, minutes = startTime.Minute };
            reservation.GroupsList = new List<VisitorsGroup>();
            reservation.GroupsList.Add(group);
            reservation.TourType = tour;
            return reservation;
        }


        //public string getXmlForActivityDay(string tourCode, DateTime dateTime)
        // {
        //    var fetchXml = _newReservationRepository.StartFetchXml("new_activity_day");
        //    fetchXml += @$"<attribute name=""new_name""/>
        //    <order attribute=""createdon"" descending=""true""/>
        //    <filter type=""and"">
        //        <condition attribute=""statecode"" operator=""eq"" value=""0""/>
        //        <condition attribute=""new_start_datetime"" operator=""eq"" value=""{dateTime}""/>
        //        <condition attribute=""new_activity_day_type"" operator=""eq""  uitype=""new_tour_type"" value=""{tourCode}""/>

        //    </filter>";
        //    fetchXml += _newReservationRepository.EndFetchXml();
        //    return fetchXml;
        // }


        //public bool chackIfActivityDayExistInSpecificDayAndTime(string tourCode, DateTime dateTime )
        //{


        //        string fetchXml = getXmlForActivityDay(tourCode, dateTime);
        //    var activityDaysEntities = _onlineReservationRepository.getEntitytItem(fetchXml);
        //    var eventItemListEntity = activityDaysEntities.Entities.FirstOrDefault();
        //    //if (eventRegistarions.Entities.Count > 0)
        //    return true;
        //}
        public List<VisitorsGroup> GetGroupsForEdit(string activityDayGuid)
        {
            try
            {
                Log.Information($"GetGroups start, activityDayGuid: {activityDayGuid}");

                List<VisitorsGroup> groups = new List<VisitorsGroup>();
                var fetchXml = _newReservationRepository.StartFetchXml("new_itinerary_group");
                fetchXml +=
                @"<attribute name='new_number_of_participants' />
                <attribute name='new_name' />
                <attribute name='new_group_number' />
                    <filter type='and'>
                        <condition attribute='statecode' operator='eq' value='0'/>
                    </filter>
                <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ActivityDay'>
                     <attribute name='new_did_group_arrive' />
                    <filter type='and'>
                        <condition attribute='new_activity_dayid' operator='eq' uitype='new_activity_day' value='{" + activityDayGuid + @"}' />
                    </filter>
                </link-entity>
                <link-entity name='new_new_itinerary_item_new_itinerary_group' from='new_itinerary_groupid' to='new_itinerary_groupid'>
                    <link-entity name='new_itinerary_item' from='new_itinerary_itemid' to='new_itinerary_itemid' alias='itineraryItem'>
                        <attribute name='new_itinerary_itemid' />
                        <attribute name='new_end_time' />
                        <attribute name='new_start_time' />
                        <attribute name='new_scheduler_resource' />
                            <filter type='and'>
                                <condition attribute='statecode' operator='eq' value='0'/>
                            </filter>
                      
                    </link-entity>
                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var result = _crmService.RetrieveMultiple(query);
                if (result.Entities.Count() > 0)
                {
                    foreach (var item in result.Entities)
                    {
                        var group = new VisitorsGroup();

                        // schedulingGuid: prefer the itinerary item's id (new_itinerary_itemid) so the client can send it back for updates.
                        string schedulingGuid = string.Empty;
                        //var aliasedItinItemId = item.GetAttributeValue<AliasedValue>("itineraryItem.new_itinerary_itemid");
                        //if (aliasedItinItemId?.Value is Guid itinGuid)
                        //{
                        //    schedulingGuid = itinGuid.ToString();
                        //}
                        //else if (aliasedItinItemId?.Value is EntityReference itinEr)
                        //{
                        //    schedulingGuid = itinEr.Id.ToString();
                        //}
                        //else if (aliasedItinItemId?.Value != null)
                        //{
                        //    schedulingGuid = aliasedItinItemId.Value.ToString();
                        //}

                        //// fallback: if itinerary item id not found, use scheduler resource (event_item)
                        //if (string.IsNullOrWhiteSpace(schedulingGuid))
                        //{
                        var aliasedScheduler = item.GetAttributeValue<AliasedValue>("itineraryItem.new_scheduler_resource");
                        if (aliasedScheduler?.Value is EntityReference er)
                        {
                            schedulingGuid = er.Id.ToString();
                        }
                        else if (aliasedScheduler?.Value is Guid g)
                        {
                            schedulingGuid = g.ToString();
                        }
                        else if (aliasedScheduler?.Value != null)
                        {
                            schedulingGuid = aliasedScheduler.Value.ToString();
                        }
                        //}

                        group.schedulingGuid = schedulingGuid;
                        DateTime? endDateTime = null;
                        DateTime? startDateTime = null;
                        var aliasedStart = item.GetAttributeValue<AliasedValue>("itineraryItem.new_start_time");
                        var aliasedEnd = item.GetAttributeValue<AliasedValue>("itineraryItem.new_end_time");

                        if (aliasedStart?.Value is DateTime dt)
                        {
                            startDateTime = dt.ToLocalTime();
                        }
                        else
                        {

                            startDateTime = null;

                        }

                        if (startDateTime.HasValue)
                        {
                            group.StartTime = new Time { hours = startDateTime.Value.Hour, minutes = startDateTime.Value.Minute };
                        }
                        else
                        {

                            group.StartTime = new Time { hours = 0, minutes = 0 };
                        }
                        if (aliasedEnd?.Value is DateTime dtEnd)
                        {
                            endDateTime = dtEnd.ToLocalTime();
                        }
                        else
                        {
                            endDateTime = null;
                        }


                        group.EndTime = endDateTime;


                        group.GroupName = item.GetAttributeValue<string>("new_name");
                        group.schedulingItem = new OnlineGroup();
                        if (startDateTime.HasValue)
                        {
                            group.schedulingItem.isChecked = true;
                            group.schedulingItem.HourInMuseon = startDateTime.Value.ToString("HH:mm");

                        }
                        group.VisitorsAmount = item.GetAttributeValue<int>("new_number_of_participants");
                        groups.Add(group);
                    }
                }
                else
                    throw new Exception("Error getting groups in reservation");

                Log.Information($"GetGroups end");
                return groups;
            }
            catch (Exception ex)
            {
                Log.Error($"GetGroups error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public List<OnlineTourType> GetPromotionTours(string lang)

        {
            try
            {
                List<Condition> conditionList = new List<Condition>();

                Condition condition = new Condition("new_is_promotion", "eq", "1");
                Condition conditionShowOnWebSite = new Condition("new_show_on_website", "eq", "1");
                conditionList.Add(condition);
                conditionList.Add(conditionShowOnWebSite);
                var res = GetToursBasicDetailsWithCondition(conditionList, lang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetPromotionTours error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public int GetReservationNumberOfTimesUpdatedOnline(Guid ReservationId)
        {
            try
            {
                // FIX: Use string[] instead of ColumnSet for the third argument
                var entity = _crmService.Retrieve("new_reservation", ReservationId, new[] { "new_number_of_times_updated_online" });
                int numberOfTimesUpdatedOnline = entity.GetAttributeValue<int?>("new_number_of_times_updated_online") ?? 0;
                if (!string.IsNullOrEmpty(numberOfTimesUpdatedOnline.ToString()))
                {
                    return numberOfTimesUpdatedOnline;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(DateTime.Now + $" GetReservationNumberOfTimesUpdatedOnline - ReservationId: {ReservationId}, Exception: {ex.Message}");
                return 0;
            }
        }
        public async Task<bool> confirmUpdateReservation(string rsvGuid)
        {
            //try send mail
            try
            {
                OnlineReservation rsv = GetOnlineReservation(Guid.Parse(rsvGuid));



                Log.Error($"Error sending confirmation mail in updateEditedObject: {rsv.ReservationName}");
                return  await confirmation(rsv, Guid.Parse(rsvGuid),"","",true);
              


            }
            catch (Exception ex)
            {
                Log.Error($"Error sending confirmation mail in updateEditedObject: {ex.Message}");
              
                return false;
            }
        }
        public async Task<CrmResultItem> updateEditedObject(OnlineReservation onlineReservation)
        {
            var res = new CrmResultItem();
            res.IsOk = false;
            res.ErrorMessageStatus = ErrorMessageStatus.GENERAL;
            try
            {
                Log.Information($"updateEditedObject start new_tour_code : {onlineReservation?.TourType?.BasicDetails?.TourCode}, ReservationNumber:{onlineReservation?.ReservationNumber}, reservationGuid:{onlineReservation?.Guid}");

                if (onlineReservation == null)
                {
                    res.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                    return res;
                }
                if (onlineReservation.TourType.BasicDetails.IsNeedEvent)
                {
                    var updateEvent= updateEventRegistrationDate(onlineReservation);
                    if (!updateEvent)
                    {

                        Log.Error($"Error update event registration ID: {onlineReservation.Guid}");
                        res.IsOk = false;
                        res.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                        res.Message = $"Error update event registration ID: {onlineReservation.Guid}";
                        return res;
                    }
                    else
                    {
                        //try send mail
                        try
                        {



                            Log.Error($"Error sending confirmation mail in updateEditedObject: {onlineReservation.ReservationName}");
                            res.IsOk = await confirmationEventRegistration(onlineReservation.Guid);
                            res.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_USER;
                            return res;


                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error sending confirmation mail in updateEditedObject: {ex.Message}");
                            res.IsOk = false;
                            res.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                            res.Message = ex.Message;
                            return res;
                        }

                    }

                }
                
                var tourType = GetTourByCode(onlineReservation.TourType.BasicDetails.TourCode);
                List<Entity> entitiesToUpdate = new List<Entity>(); //update only if  IsThereRoomInTheResource for all groups otherise do nothing.
                                                                    //dates
                string format = "dd/MM/yyyy"; // ציון הפורמט המתאים
                onlineReservation.ReservationDate = DateTime.ParseExact(onlineReservation.reservationDateString, format, null);

                Log.Information($"The UpdateTourReservationDate date is: {onlineReservation.ReservationDate}");


                //string startTime = onlineReservation.StartTime?.hours.ToString() + ":" + onlineReservation.StartTime?.minutes.ToString();
                //onlineReservation.StartDateTime = new DateTime(onlineReservation.ReservationDate.Value.Year, onlineReservation.ReservationDate.Value.Month, onlineReservation.ReservationDate.Value.Day, onlineReservation.StartTime.hours, onlineReservation.StartTime.minutes, 0);

                if (onlineReservation.StartTime != null && onlineReservation.ReservationDate != null)
                {
                    string startTime = onlineReservation.StartTime.hours.ToString() + ":" + onlineReservation.StartTime.minutes.ToString();
                    onlineReservation.StartDateTime = new DateTime(onlineReservation.ReservationDate.Value.Year, onlineReservation.ReservationDate.Value.Month, onlineReservation.ReservationDate.Value.Day, onlineReservation.StartTime.hours, onlineReservation.StartTime.minutes, 0);
                }
                if (onlineReservation.StartDateTime < DateTime.Now)
                {
                    throw new InvalidOperationException("לא ניתן לבחור שעה או תאריך לפני הזמן הנוכחי.");
                }
                //// Update reservation first activity date field:
                Entity reservationEntity = new Entity("new_reservation", Guid.Parse(onlineReservation.Guid));
                reservationEntity["new_activity_day_first_date"] = onlineReservation.StartDateTime;
                reservationEntity["new_is_approved_by_coordinator"] = false;
                reservationEntity["new_is_order_confirmed"] = true;
                reservationEntity["new_number_of_times_updated_online"] = (GetReservationNumberOfTimesUpdatedOnline(Guid.Parse(onlineReservation.Guid)) + 1).ToString();
                entitiesToUpdate.Add(reservationEntity);


                var fetchXml = _newReservationRepository.StartFetchXml("new_activity_day");
                fetchXml += @$"
                    <filter type=""and"">
                      <condition attribute=""new_reservation"" operator=""eq"" value=""{Guid.Parse(onlineReservation.Guid)}"" />
                      <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                    </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);

                EntityCollection activityResult = _crmService.RetrieveMultiple(query);
                //// Update activity day start date and end date:
                if (activityResult.Entities.Count != 1)
                {

                    Trace.WriteLine(DateTime.Now + ": UpdateActivityDay - Error: found " + activityResult.Entities.Count + " ActivityDays instad of 1");
                    return new CrmResultItem(false, "UpdateActivityDay - Error: found " + activityResult.Entities.Count + " ActivityDays instad of 1");
                }
                Entity activityDay = activityResult.Entities[0];
                //לא מבינה מה זה עושה בדיוק!
                //TimeSpan ts = TimeSpan.Parse(onlineReservation.reservationDateString);
                // TimeSpan ts_end = TimeSpan.Parse(onlineReservation.new_end_datetime);
                decimal duration = tourType.GetAttributeValue<decimal>("new_duration_of_hours");
                activityDay["new_start_datetime"] = onlineReservation.StartDateTime;
                activityDay["new_name"] = "Activity Day -> " + onlineReservation.StartDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
                //var duration = tourType.GetAttributeValue<decimal>("new_duration_of_hours");
                activityDay["new_end_datetime"] = onlineReservation.StartDateTime?.AddHours(decimal.ToDouble(duration));
                //if (onlineReservation.ActivityPackageList.Count > 0)
                //{
                //    activityDay["new_number_of_groups"] = 1;
                //}
                //else
                //{
                //    activityDay["new_number_of_groups"] = onlineReservation.GroupsList?.Count;
                //}
                entitiesToUpdate.Add(activityDay);

                var resource = tourType.GetAttributeValue<EntityReference>("new_resource");

                if (resource == null)
                {//doesnt need a resource
                    Trace.WriteLine(DateTime.Now + $" No resource was needed.");
                    var bidList = _onlineReservationRepository.GetBidListForReservation(onlineReservation.TourType.BasicDetails.Id);
                    // Ensure we wait for HandleEventItemsSchedule to complete instead of fire-and-forget
                    var handleResult = await HandleEventItemsSchedule(onlineReservation.Guid, "", activityDay.Id.ToString(), onlineReservation.ReservationDate, reservationEntity, false);
                    if (!handleResult.IsOk)
                    {
                        return handleResult;
                    }

                    entitiesToUpdate.ForEach(entity => _crmService.Update(entity));
                }
                else
                {
                    Trace.WriteLine(DateTime.Now + "before GetItiniraryListByActivityDayAndResource ");


                    try
                    {
                        if (onlineReservation.GroupsList != null && onlineReservation.GroupsList.Count > 0 && onlineReservation.ReservationDate.HasValue)
                        {
                            // compute duration once

                            duration = tourType.GetAttributeValue<decimal>("new_duration_of_hours_resource");
                            if (duration == 0)
                                duration = tourType.GetAttributeValue<decimal>("new_duration_of_hours");
                            if (duration == 0)
                                duration = 1.4M;

                            foreach (var group in onlineReservation.GroupsList)
                            {
                                // schedulingGuid expected to be the event_item id (as returned by GetGroupsForEdit)
                                if (string.IsNullOrWhiteSpace(group.schedulingGuid))
                                    return new CrmResultItem(false, "Mismatch between tour time list and event item list");

                                if (!Guid.TryParse(group.schedulingGuid, out Guid eventItemGuid))
                                    return new CrmResultItem(false, "Mismatch between tour time list and event item list");


                                // Determine start datetime for this group
                                DateTime? targetStart = null;
                                if (group.StartTime != null && onlineReservation.ReservationDate.HasValue)
                                {
                                    targetStart = new DateTime(
                                        onlineReservation.ReservationDate.Value.Year,
                                        onlineReservation.ReservationDate.Value.Month,
                                        onlineReservation.ReservationDate.Value.Day,
                                        group.StartTime.hours,
                                        group.StartTime.minutes,
                                        0);
                                }
                                else if (group.schedulingItem != null && !string.IsNullOrWhiteSpace(group.schedulingItem.HourInMuseon) && onlineReservation.ReservationDate.HasValue)
                                {
                                    // HourInMuseon expected like "HH:mm" or "H:mm"
                                    if (TimeSpan.TryParse(group.schedulingItem.HourInMuseon, out TimeSpan ts))
                                    {
                                        targetStart = new DateTime(
                                            onlineReservation.ReservationDate.Value.Year,
                                            onlineReservation.ReservationDate.Value.Month,
                                            onlineReservation.ReservationDate.Value.Day,
                                            ts.Hours,
                                            ts.Minutes,
                                            0);
                                    }
                                }

                                if (!targetStart.HasValue)
                                    continue;
                                //בדיקת מקום
                                //if (group.VisitorsAmount > CheckIfThereIsPlaceInMuseum(onlineReservation.reservationDateString, group.StartTime, onlineReservation.TourType.BasicDetails.TourCode))
                                //{
                                //    res.IsOk= false;
                                //    res.Message= "No room in resource: " + resource.Name + ", for time: " + group.StartTime.ToString() + ", Please choose a different time";
                                //    res.ErrorMessageStatus= ErrorMessageStatus.UNKNOWN;
                                //    return res;

                                //}
                                try
                                {
                                    // Update event_item (schedule) start/end

                                    var eventItemToUpdate = new Entity("new_event_item", eventItemGuid);
                                    eventItemToUpdate["new_start_datetime"] = targetStart.Value;
                                    eventItemToUpdate["new_end_datetime"] = targetStart.Value.AddHours(decimal.ToDouble(duration));
                                    eventItemToUpdate["new_status"] = new OptionSetValue(1);
                                    entitiesToUpdate.Add(eventItemToUpdate);



                                    //---Update schedule rows(event_item / itinerary_item) according to incoming groups ---


                                    // Update linked itinerary_item(s) which reference this event_item via new_scheduler_resource

                                    var qe = new QueryExpression("new_itinerary_item");
                                    qe.ColumnSet = new ColumnSet(true);
                                    qe.Criteria.AddCondition("new_scheduler_resource", ConditionOperator.Equal, eventItemGuid);
                                    var linkedItems = _crmService.RetrieveMultiple(qe);
                                    foreach (var itinItem in linkedItems.Entities)
                                    {
                                        var itinToUpdate = new Entity("new_itinerary_item", itinItem.Id);
                                        itinToUpdate["new_start_time"] = targetStart.Value;
                                        itinToUpdate["new_end_time"] = targetStart.Value.AddHours(decimal.ToDouble(duration));
                                        entitiesToUpdate.Add(itinToUpdate);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Error updating schedule rows in updateEditedObject: {ex.Message}");
                                    res.IsOk = false;
                                    res.Message = " Error updating schedule rows in updateEditedObject";
                                    return res;
                                }
                            }





                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error updating schedule rows in updateEditedObject: {ex.Message}");
                        //}
                    }
                    var result = await HandleEventItemsSchedule(onlineReservation.Guid, resource.Id.ToString(), activityDay.Id.ToString(), onlineReservation.ReservationDate, reservationEntity, false);
                    //if (!result.IsOk)
                    //{
                    //    return result;
                    //}

                    entitiesToUpdate.ForEach(entity => _crmService.Update(entity));

                }


                res.ResponseObject = onlineReservation;
                res.IsOk = true;
                return res;

            }
            catch (Exception ex)
            {
                Trace.WriteLine(DateTime.Now + ": UpdateActivityDay - Exception: " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace);
                res.IsOk = false;
                res.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                res.Message = ex.Message;
                return res;
            }

           
        }

        public List<OnlineTourType> GetAllToursBasicDetails(InstituitionType instituitionType, string lang)
        {
            try
            {
                var conditionlist = new List<Condition>();
                Condition condition = new Condition("", "eq", "1");
                switch (instituitionType)
                {
                    case InstituitionType.TRAVEL_AGENTS:
                        condition.Name = "new_is_for_travel_agents";
                        break;
                    case InstituitionType.EDUCATIONAL_GROUPS:
                        condition.Name = "new_is_for_institutions";
                        break;
                    case InstituitionType.PRIVATE:
                        condition.Name = "new_is_for_private";
                        break;
                }
                Condition conditionShowOnWebSite = new Condition("new_show_on_website", "eq", "1");
                // Condition ss = new Condition("new_is_package", "eq", "1");

                conditionlist.Add(condition);
                conditionlist.Add(conditionShowOnWebSite);

                var res = GetToursBasicDetailsWithCondition(conditionlist, lang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllToursBasicDetails error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<OnlineTourType> GetSomeTourBasicDetails(List<string> toursCode, string lang)
        {
            List<OnlineTourType> res = new List<OnlineTourType>();
            foreach (var tourCode in toursCode)
            {
                res.Add(GetTourBasicDetails(tourCode, lang));
            }
            return res;
        }

        public List<string> GetAllTours()
        {
            List<string> res = new List<string>();
            res = GetAllToursId();
            return res;
        }

        public bool SetEventRegistartionStatus(string eventRegistarionGuid, bool isPackage, int status)

        {
            try
            {
                Log.Information($"SetEventRegistartionToInactive eventRegistarionGuid:{eventRegistarionGuid}, isPackage:{isPackage}");
                if (isPackage)
                {
                    var fetchXml = _newReservationRepository.StartFetchXml("new_event_registration", "eventReservation", false, true);
                    fetchXml += @$"<attribute name=""new_name""/>
                                    <attribute name=""new_event_registrationid""/>
                                    <order attribute=""createdon"" descending=""true""/>
                                    <filter type=""and"">
                                        <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                        <condition attribute=""new_reservation"" operator=""eq"" value=""{eventRegistarionGuid}""/>
                                    </filter>";
                    fetchXml += _newReservationRepository.EndFetchXml();
                    var query = new FetchExpression(fetchXml);
                    var eventRegistarions = _crmService.RetrieveMultiple(query);
                    if (eventRegistarions.Entities.Count > 0)
                    {
                        foreach (var eventRegistarion in eventRegistarions.Entities)
                        {
                            Entity eventRegistration;
                            eventRegistration = new Entity("new_event_registration", eventRegistarion.GetAttributeValue<Guid>("new_event_registrationid"));
                            eventRegistration["statecode"] = new OptionSetValue(status); //inactive   
                            _crmService.Update(eventRegistration);
                        }
                    }
                }
                else
                {
                    SetStateRequest Req = new SetStateRequest
                    {
                        EntityMoniker = new EntityReference("new_event_registration", Guid.Parse(eventRegistarionGuid)),
                        State = new OptionSetValue(status),//inactive,
                        Status = new OptionSetValue(100000000) //לפני תשלום
                    };
                    _crmService.Execute(Req);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"SetEventRegistartionToInactive error: {ex.Message}");
                return false;
            }
        }

        public OnlineTourType GetTourBasicDetails(string tourCode, string lang)
        {
            try
            {
                var conditionlist = new List<Condition>();
                Condition condition = new Condition("new_tour_code", "eq", tourCode);
                conditionlist.Add(condition);
                var res = GetToursBasicDetailsWithCondition(conditionlist, lang);
                if (res.Count > 0)
                {
                    return res.First();
                }
                else
                {
                    throw new Exception("no tour");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"GetTourBasicDetails error: {ex.Message}");
                throw new Exception(ex.Message);
            }

        }

        public AdditionalDetails GetAdditionalTourDetails(string tourCode, string lang)
        {
            try
            {
                var conditionlist = new List<Condition>();
                Condition condition = new Condition("new_tour_code", "eq", tourCode);
                conditionlist.Add(condition);
                var res = GetAdditionalTourDetailsWithCondition(conditionlist, tourCode, lang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAdditionalTourDetails error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private List<OnlineTourType> GetToursBasicDetailsWithCondition(List<Condition> conditionlist, string lang)
        {
            try
            {

                //אין פה פילטור להראה באתר, אם רוצי ם יש להוסיף ברשימת התנאים לפילטור
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType", false, true);
                fetchXml += @$" 
                    <attribute name=""new_name""/>שם בעברית
                    <attribute name=""new_name_en""/>שם באנגלית
                    <attribute name=""new_description_{lang}""/>הסבר אנגלית
                    <attribute name=""new_short_desc_{lang}""/>הסבר מקוצר עברית
                    <attribute name=""new_tour_code""/>קוד סיור  
                    <attribute name=""new_max_reservations_per_day""/>//מקסימום הזמנות ליום
                    <attribute name=""new_age""/>מתאים לגיל
                    <attribute name=""new_tag""/> תגית
                    <attribute name=""new_duration_of_hours""/>משך שעות
                    <attribute name=""new_relevant_day_sunday""/>
                    <attribute name=""new_relevant_day_monday""/>
                    <attribute name=""new_relevant_day_tuesday""/>
                    <attribute name=""new_target_population""/>
                    <attribute name=""new_category""/>
                    <attribute name=""new_grade_1""/>
                    <attribute name=""new_grade_2""/>
                    <attribute name=""new_grade_3""/>
                    <attribute name=""new_grade_4""/>
                    <attribute name=""new_grade_5""/>
                    <attribute name=""new_grade_6""/>
                    <attribute name=""new_grade_7""/>
                    <attribute name=""new_grade_8""/>
                    <attribute name=""new_grade_9""/>
                    <attribute name=""new_grade_10""/>
                    <attribute name=""new_grade_11""/>
                    <attribute name=""new_grade_12""/>
                    <attribute name=""new_relevant_day_wednesday""/>
                    <attribute name=""new_relevant_day_thursday""/>
                    <attribute name=""new_is_need_event""/>
                    <attribute name=""new_relevant_day_friday""/>
                    <attribute name=""new_tab_to_display""/>
                    <attribute name=""new_max_participants""/>
                    <attribute name=""new_max_participants_in_group""/>
                    <attribute name=""new_tour_typeid""/>
                    <attribute name=""new_min_participants""/>
                    <attribute name=""new_difference_between_arrival_to_start""/>
                    <attribute name=""new_sort_weight_{lang}""/>//משקל למיון
                    <attribute name=""new_event_reservations""/>//ארוע-מערכת אחודה
                    <attribute name=""new_discount_type""/>//סוג הנחה
                    <attribute name=""new_discount""/>//סך הנחה
                    <attribute name=""new_is_package""/>//האם חבילה
                    <attribute name=""new_cancel_registration_fees""/>//בטל דמי הרשמה וטיפול
                    <attribute name=""new_ltr_rtl""/>//יישור טקטסטים
                    <attribute name=""new_is_recommended""/>//האם מומלץ
                    <order attribute=""new_sort_weight_{lang}"" descending=""false""/>
                    <order attribute=""new_name"" descending=""false""/>
                    <filter type=""and"">";

                var conditions = "";
                foreach (var condition in conditionlist)
                {
                    conditions += @$"<condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}"" />";
                }

                conditions += $@"<condition attribute=""statecode"" operator=""eq"" value=""0""/>
                               </filter>";
                fetchXml += conditions;
                fetchXml += _newReservationRepository.EndFetchXml();
                var tourTypeList = new List<OnlineTourType>();
                var query = new FetchExpression(fetchXml);
                var tourTypeEntitiesList = _crmService.RetrieveMultiple(query);

                var res = _onlineReservationRepository.GetTourTypeList(fetchXml, lang);
                res.ForEach(tourType =>
                {
                    bool InfoByHe = !IsHebrew(tourType.BasicDetails.Name) && (lang == "he" || lang == "en") ? false : true;
                    tourType.BasicDetails.GuidLanguages = GetLanguagesByTourType(tourType.BasicDetails.TourCode, lang, InfoByHe);
                    tourType.BasicDetails.Tags = GetTagsByTourType(tourType.BasicDetails.TourCode, lang);
                    tourType.TourTypesRequiredList = GetTourTypesByTourType(tourType.BasicDetails.TourCode, lang);
                });
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetToursBasicDetailsWithCondition error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public static bool IsHebrew(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return Regex.IsMatch(text, @"[\u0590-\u05FF]");
        }
        private List<string> GetAllToursId()
        {
            try
            {
                var tourTypeList = new List<string>();

                //אין פה פילטור להראה באתר, אם רוצי ם יש להוסיף ברשימת התנאים לפילטור
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType", false, true);
                fetchXml += @$" 
                    <attribute name=""new_tour_code""/>
                    <order attribute=""new_name"" descending=""false""/>
                    <filter type=""and"">
                        <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                        <condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>
                    </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var tourTypeIdList = _crmService.RetrieveMultiple(query);
                foreach (var tourTypeEntity in tourTypeIdList.Entities)
                {
                    tourTypeList.Add(tourTypeEntity.GetAttributeValue<string>("new_tour_code"));
                }
                return tourTypeList;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllTourId error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public string getXmlForCoupons(string tourCode)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_coupon");
            fetchXml += @$" <attribute name=""new_name""/>
                    <attribute name=""new_coupon_code""/>
                    <attribute name=""new_discount_type""/>
                    <attribute name=""new_coupon_discount""/>
                    <attribute name=""new_couponid""/>
                    <filter type=""and"">
                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                    </filter>";
            fetchXml += $@"
                              <link-entity name=""new_new_coupon_new_tour_type"" from=""new_couponid"" to=""new_couponid"" intersect=""true"">
                                <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_typeid"">
                                    <filter type=""and"">
                                        <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>
                                    </filter>
                                 </link-entity>
                            </link-entity>";
            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }
        private AdditionalDetails GetAdditionalTourDetailsWithCondition(List<Condition> conditionlist, string tourCode, string lang)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType", false, true);
                fetchXml += @$" <attribute name=""new_resource""/>האם למוזיאון
                                <attribute name=""new_is_need_event""/>האם דרו אירוע
                                <attribute name=""new_tour_typeid""/>
                                <attribute name=""new_age_group""/>//שכבת גיל
                                <attribute name=""new_more_questions_about""/>
                                <attribute name=""new_duration_of_hours_resource""/>
                                <attribute name=""new_needs_more_questions""/>
                                <attribute name=""new_difference_between_arrival_to_start""/>
                                <attribute name=""new_is_to_display_age_and_country""/>//האם למוזיאון
                                <attribute name=""new_resource""/>//האם למוזיאון
                                <attribute name=""new_price_desc_{lang}""/>//הסבר מחיר
                                <attribute name=""new_tour_type_tahel""/>//פעילות יומית עבור תהל 
                                <attribute name=""new_is_allow_group_registration""/>//האם לאפשר רישום קבוצות
                                <attribute name=""new_show_comments_on_website""/>// האם להציג הערות בטופס 
                                <attribute name=""new_tour_type_tahel""/>//פעילות יומית עבור תהל   
                                <attribute name=""new_is_package""/>
                                <attribute name=""new_duration_of_hours""/>
                                <attribute name=""new_is_need_guide_hours_regulation""/>
                                <order attribute=""new_name"" descending=""false""/>
                                <filter type=""and"">";
                var conditions = "";
                foreach (var condition in conditionlist)
                {
                    conditions += @$"<condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}"" />";
                }
                conditions += $@"<condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                                 <condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>
                                               </filter>";
                fetchXml += conditions;
                fetchXml += $@"<link-entity name=""new_budget_framework"" from=""new_budget_frameworkid"" to=""new_budget_framework"" alias=""budget_framework"">
                                <attribute name=""new_pricelist_type""/>  
                                <attribute name=""new_rsv_department""/>
                            </link-entity>";

                fetchXml += $@"
                    <link-entity name=""new_yv_product"" from=""new_yv_productid"" to=""new_product_display_tickets_cards"" alias=""ProductDisplayTicketsCards"" link-type=""outer"">
                            <attribute name=""new_yv_productid""/>
                    </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                string fetchXmlCoupons = getXmlForCoupons(tourCode);

                var res = _onlineReservationRepository.GetTourType(fetchXml, lang, fetchXmlCoupons);
                OptionSetValue priceListType = new OptionSetValue();

                res.reqiredProductList = GetRequireProductsList(tourCode, "product_required", lang);



                var query = new FetchExpression(fetchXml);
                if (_crmService.RetrieveMultiple(query).Entities.Count > 0)
                {
                    var tourTypeEntity = _crmService.RetrieveMultiple(query).Entities.First();
                    if (tourTypeEntity.GetAttributeValue<EntityReference>("new_tour_type_tahel") != null)
                    {
                        res.reqiredProductList.AddRange(GetRequireProductOfTourTypeTahel(lang, tourCode));
                        res.reqiredProductList = res.reqiredProductList.GroupBy(x => x.Id).Select(x => x.First()).ToList();
                    }
                    if (tourTypeEntity.GetAttributeValue<AliasedValue>("budget_framework.new_pricelist_type").Value.ToString() != "")
                    {
                        priceListType = (OptionSetValue)tourTypeEntity.GetAttributeValue<AliasedValue>("budget_framework.new_pricelist_type").Value;
                    }
                    if (res?.ProductDisplayTicketsCards != null &&
                        !res.reqiredProductList.Any(p => p.Id == res.ProductDisplayTicketsCards.Id))
                    {
                        if (tourTypeEntity.GetAttributeValue<AliasedValue>("ProductDisplayTicketsCards.new_yv_productid") != null)
                        {
                            Guid guid = (Guid)tourTypeEntity.GetAttributeValue<AliasedValue>("ProductDisplayTicketsCards.new_yv_productid").Value;
                            var xml = getXmlProductById(guid);
                            Product product = _onlineReservationRepository.GetProductsList(xml, lang)[0];
                            product.IsChooseLang = product.Languages?.Count > 0 ? true : false;
                            product.PriceListItemOptions = GetProductPrice(product.Id, lang, 20, new DateTime(2024, 10, 10, 09, 22, 43), 2,
                                priceListType.Value, "2347C5AE-F818-EB11-8106-00505691117E");
                            res.ProductDisplayTicketsCards = product;
                        }

                        //if (product.Id == res?.ProductDisplayTicketsCards.Id)
                        //{
                        //    res.ProductDisplayTicketsCards = product;
                        //}
                    }
                }

                res.guidLanguages = GetLanguagesByTourType(tourCode, lang);

                res.reqiredProductList.ForEach(product =>
                {

                    //product.Languages = GetLanguagesByProduct(product.Id, lang);
                    product.IsChooseLang = product.Languages?.Count > 0 ? true : false;
                    product.PriceListItemOptions = GetProductPrice(product.Id, lang, 20, new DateTime(2024, 10, 10, 09, 22, 43), 2,
                        priceListType.Value, "2347C5AE-F818-EB11-8106-00505691117E");
                    if (res?.ProductDisplayTicketsCards != null)
                    {
                        if (product.Id == res?.ProductDisplayTicketsCards.Id)
                        {
                            res.ProductDisplayTicketsCards = product;
                        }
                    }


                });

                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAdditionalTourDetailsWithCondition error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public string getXmlProductById(Guid guid)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_yv_product");
            fetchXml += $@"
            <attribute name=""new_name""/>
            <attribute name=""createdon""/>
            <attribute name=""new_division_router""/>
            <attribute name=""new_sort_weight""/>
            <attribute name=""new_product_name_he""/>
            <attribute name=""new_name_en""/>
            <attribute name=""new_sku_product""/>
            <attribute name=""new_almog_interface""/>
            <attribute name=""new_show_on_website""/>
            <attribute name=""new_is_required_service_provider""/>
            <attribute name=""new_pricing_method""/>
            <attribute name=""new_number_of_participants_per_product""/>
            <attribute name=""new_yv_productid""/>
            <order attribute=""new_name"" descending=""false""/>
            <filter type=""and"">
            <condition attribute=""statecode"" operator=""eq"" value=""0""/>
            <condition attribute=""new_yv_productid"" operator=""eq"" value=""{guid}""/>
            </filter>";
            fetchXml += _newReservationRepository.EndFetchXml();

            return fetchXml;
        }
        public void UpdateReservationToActive(Guid reservationGuid, bool isPaid, Guid? couponGuid = null, decimal? totalPriceAfterDiscount = null)
        {
            try
            {
                Log.Information($"UpdateReservationToActive in service start, reservationGuid:{reservationGuid}, isPaid:{isPaid}");
                var reservationEntity = _crmService.Retrieve("new_reservation", reservationGuid, new[] { "new_num" });
                var reservationNumber = reservationEntity.GetAttributeValue<string>("new_num");
                _onlineReservationRepository.UpdateReservationToActive(reservationGuid, reservationNumber, isPaid, couponGuid, totalPriceAfterDiscount);
                Log.Information("UpdateReservationToActive in service end");
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateReservationToActive error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public string getEventRegistrationActivityDay(Guid eventRegistrationGuid)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_event_registration_activity_day");
            fetchXml += $@" 
                <attribute name=""new_name""/>
                <attribute name=""createdon""/>
                <attribute name=""new_event_registration""/>
                <attribute name=""new_activity_day""/>
                <attribute name=""new_event_registration_activity_dayid""/>
                <order attribute=""new_name"" descending=""false""/>
                <link-entity name=""new_event_registration"" from=""new_event_registrationid"" to=""new_event_registration"" alias=""EventRegistration"">
                <attribute name=""new_number_of_participants""/>
                <attribute name=""new_num""/>
                <attribute name=""new_contact""/>
                <attribute name=""new_event_reservations""/>
                <attribute name=""new_email_contact""/>
                <filter type=""and"">
                <condition attribute=""new_event_registrationid"" operator=""eq""  uitype=""new_event_registration"" value=""{eventRegistrationGuid}""/>
                </filter>
                </link-entity>
                ";

            fetchXml += _newReservationRepository.EndFetchXml();

            return fetchXml;
        }

        public void UpdateStatusEventRegistration(Guid EventRegistrationGuid, bool payLater, Guid? couponGuid = null, decimal? totalPriceAfterDiscount = null)
        {
            try
            {
                //< link - entity name = "new_event_registration_activity_day" from = "new_event_registration" to = "new_event_registrationid" alias = "ab" />
                Log.Information($"UpdateStatusEventRegistration start, EventRegistrationGuid:{EventRegistrationGuid}, payLater: {payLater}");
                string fetchXml = getEventRegistrationActivityDay(EventRegistrationGuid);
                var eventItemList = _onlineReservationRepository.getEntitytItem(fetchXml);
                var eventItemListEntity = eventItemList.Entities.FirstOrDefault();
                Guid? activityDayGuid = null;
                if (eventItemListEntity != null)
                {
                    activityDayGuid = eventItemListEntity.GetAttributeValue<Guid>("new_event_registration_activity_dayid");
                }

                _onlineReservationRepository.UpdateStatusEventRegistration(EventRegistrationGuid, payLater, activityDayGuid, couponGuid, totalPriceAfterDiscount);
                Log.Information("UpdateStatusEventRegistration in service end");
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateStatusEventRegistration error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public Guid CreatePaymentInCRM(ResultData respons, OnlineReservation reservation, bool isNeedEvent = false, string tourName = "")
        {
            try
            {
                Log.Error($"CreatePaymentInCRM start: respons:{respons}, reservation: {reservation}, isNeedEvent: {isNeedEvent}, tourName: {tourName}");
                //respons.VoucherId = "123";      
                //respons.CreditCardNumber = "4557430402053365";
                //respons.CreditCardExpDate = "0330";
                //respons.DebitTotal = "18000";//סכום
                //respons.DebitApproveNumber = "456";
                respons.DebitCurrency = "1";
                respons.TotalPayments = "1";
                //respons.FirstPaymentTotal = "";
                //respons.FixedPaymentTotal = "";
                respons.AdditionalDetailsParamX = reservation.ReservationNumber;
                Guid guid = _onlineReservationRepository.CreatePaymentInCRM(respons, reservation, isNeedEvent, tourName);
                Log.Error($"CreatePaymentInCRM end");
                return guid;
            }
            catch (Exception ex)
            {
                Log.Error($"CreatePaymentInCRM error: {ex.Message}");

                //send mail that CRM payment failed! important!
                MailInfo mailInfo = new MailInfo();
                mailInfo.Message = $"CreatePaymentInCRM error: {ex.Message}.<br>" +
                                    $"Reservation num: {reservation.ReservationNumber},<br>" +
                                    $"Reservation guid:{reservation.Guid}.<br>" +
                                    $"Payment in pelecard was successfully finished, but there was an error while creating payment in crm!<br><br>" +
                                    $"response from pelecard is: {JsonConvert.SerializeObject(respons)}";
                mailInfo.Subject = $"CreatePaymentInCRM error for reservation: {reservation.ReservationNumber}";
                mailInfo.To = new List<string>() { "yv.dev.logs@yadvashem.org.il" };
                mailInfo.Cc = new List<string>() { "Tamar.Ostry@yadvashem.org.il", "devora.weyl@yadvashem.org.il", "Noa.Zinger@yadvashem.org.il", "chana.kramer@yadvashem.org.il" };

                mailInfo.Bcc = new List<string>();
                mailInfo.From = "no-reply@yadvashem.org";
                mailInfo.Files = new List<string>();
                mailInfo.Direction = Direction.LTR;
                Log.Information($"Before send email error: {@mailInfo}.");
                _helperService.CreateRequest("http://reservations.yadvashem/sendmail/mail", "POST", JsonConvert.SerializeObject(mailInfo));

                Log.Information("After send email error");

                throw new Exception(ex.Message);
            }
        }

        public void UpdateReservationAfterPayment(ReservationNumberGuid reservationNumGuid)
        {
            try
            {
                Log.Information($"UpdateReservationAfterPayment in service start, reservationGuid:{reservationNumGuid.ReservationGuid}, reservationNum: {reservationNumGuid.ReservationNumber} ");
                _onlineReservationRepository.UpdateReservationAfterPayment(reservationNumGuid);
                Log.Information("UpdateReservationAfterPayment in service end");
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateReservationAfterPayment error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public CrmResultItem GetRequiredProductsAvailablePerDate(ProductRequestPerDate productRequestPerDate)
        {
            var res = new CrmResultItem(true, "");

            foreach (var product in productRequestPerDate.RequiredProductsIdWithMaxPerDate)
            {

                Entity crmProduct = new Entity("new_product");
                crmProduct.Id = product.Key;
                if (productRequestPerDate.tourDate != null)
                {
                    if (productRequestPerDate.tourDate != null)
                    {
                        DateTime tourDate = productRequestPerDate.tourDate.Value; // הבטחה שזה לא null

                        if (GetNumberOfReservationsWithProduct(tourDate, crmProduct) >= product.Value)
                        {
                            res.IsOk = false;
                            res.ErrorMessageStatus = ErrorMessageStatus.MAX_PER_DATE_FINISH;
                            return res;
                        }
                    }

                }
                else
                {
                    res.IsOk = false;
                    res.ErrorMessageStatus = ErrorMessageStatus.MAX_PER_DATE_FINISH;
                    return res;

                }


            }
            return res;
        }


        public List<Product> GetAdditionalProductList(FilterForProduct filterForProduct)
        {
            try
            {
                List<Product> additionalProductList = new List<Product>();
                if (filterForProduct.amount == 0)
                {
                    return additionalProductList;
                }
                var fetchXml = $@"<filter type=""and"">
                                    <filter type=""or"">
                                        <condition attribute=""new_min_number_of_participants"" operator=""le"" value=""{filterForProduct.amount}""/>
                                        <condition attribute=""new_min_number_of_participants"" operator=""null""/>
                                    </filter>
                                    <filter type=""or"">
                                        <condition attribute=""new_max_number_of_participants"" operator=""null""/>
                                        <condition attribute=""new_max_number_of_participants"" operator=""ge"" value=""{filterForProduct.amount}""/>
                                    </filter>
                                    <filter type=""and"">
                                        <condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>
                                    </filter>";
                if (filterForProduct.isPackage)
                {
                    fetchXml += $@"<filter type=""and"">
                                        <condition attribute=""new_show_on_package"" operator=""eq"" value=""1""/>
                                    </filter>";
                }
                fetchXml += $@" </filter>";
                var condition = $@"<condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>";
                var allFetch = InitXmlToProduct(filterForProduct.tourCode, "product_optional", filterForProduct.language, filterForProduct.tourDate, condition, fetchXml);
                var additionalProductEntitiesList = _onlineReservationRepository.GetOptionalEntitiesList(allFetch);
                if (additionalProductEntitiesList.Entities.Count > 0)
                {
                    List<Guid> closedProducts = GetClosedProductsForDate(filterForProduct.tourDate);

                    foreach (var product in additionalProductEntitiesList.Entities)
                    {
                        if (filterForProduct.amount > 0)
                        {
                            if (!IsDateGoodForProduct(filterForProduct.tourDate, product))
                                continue;
                        }
                        if (closedProducts.Contains(product.GetAttributeValue<Guid>("new_yv_productid")))
                            if (closedProducts.Contains(product.GetAttributeValue<Guid>("new_yv_productid")))
                            {
                                continue;
                            }
                        if (product.GetAttributeValue<int>("new_max_products_per_day") > 0)
                        {
                            if (GetNumberOfReservationsWithProduct(filterForProduct.tourDate, product) >= product.GetAttributeValue<int>("new_max_products_per_day"))
                            {
                                continue;
                            }
                        }
                        var YadVashemLocationGuid = _config.GetValue<string>("YadVashemLocationGuid")!;
                        var pricelistType = GetPriceListTypeByTourCode(filterForProduct.tourCode);
                        List<PriceListItem> priceListItemsList = GetProductPrice(product.Id.ToString(), filterForProduct.language, filterForProduct.amount, filterForProduct.tourDate, product.GetAttributeValue<OptionSetValue>("new_division_router").Value, pricelistType, YadVashemLocationGuid);
                        additionalProductList.Add(_onlineReservationRepository.GetProduct(product, filterForProduct.language));
                        additionalProductList[additionalProductList.Count - 1].PriceListItemOptions = priceListItemsList;
                    }
                }

                additionalProductList.ForEach(product =>
                {
                    product.Languages = GetLanguagesByProduct(product.Id, filterForProduct.language);
                    product.IsChooseLang = product.Languages?.Count > 0 ? true : false;
                });
                return additionalProductList;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAdditionalProductList error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private bool IsDateGoodForProduct(DateTime tourDate, Entity entity)
        {
            if (!entity.GetAttributeValue<bool>("new_relevant_day_" + tourDate.DayOfWeek.ToString().ToLower()))
            {
                return false;
            }
            if (GetWorkingDays(DateTime.Today, tourDate, new List<DateTime>()) < entity.GetAttributeValue<int>("new_minimum_days"))//number of working days < minimum_days
            {
                return false;
            }
            return true;
        }

        private int GetWorkingDays(DateTime from, DateTime to, List<DateTime> closedTourDates)
        {
            if (to <= from)
                return 0;

            var dayDifference = (int)to.Subtract(from).TotalDays;

            return Enumerable
                .Range(1, dayDifference)
                .Select(x => from.AddDays(x))
                .Where(x => x.DayOfWeek != DayOfWeek.Saturday &&
                            x.DayOfWeek != DayOfWeek.Friday &&
                            !closedTourDates.Contains(x))
                .Count();
        }


        private List<Guid> GetClosedProductsForDate(DateTime tourDate)
        {
            try
            {
                List<Guid> closedProducts = new List<Guid>();
                var fetchXml = _newReservationRepository.StartFetchXml("new_yv_product", "closedDays", false, true);
                fetchXml += $@"<all-attributes />
                               <filter type=""and"">
                                  <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                               </filter>
                                <link-entity name='new_new_closed_days_and_activities_new_yv_p' from='new_yv_productid' to='new_yv_productid' alias='new_yv_product' visible='false' intersect='true'>        
                                        <link-entity name='new_closed_days_and_activities' from='new_closed_days_and_activitiesid' to='new_closed_days_and_activitiesid' alias='new_closed_days'>
                                            <filter type=""and"">
                                                  <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                                  <condition attribute=""new_from_closing_date"" operator=""on-or-after"" value=""{tourDate.Date.ToString("yyyy-MM-dd")}""/>
                                                  <condition attribute=""new_to_closing_date"" operator=""on-or-before"" value=""{tourDate.Date.ToString("yyyy-MM-dd")}""/>
                                                  <condition attribute=""new_closing_type"" operator=""eq"" value=""{(int)ClosedType.ACTIVITES_CLOSED}""/>
                                            </filter>
                                        </link-entity>
                                </link-entity>";

                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var closedProductsEntitiesList = _crmService.RetrieveMultiple(query);
                if (closedProductsEntitiesList.Entities.Count > 0)
                {
                    foreach (Entity closedProductEntity in closedProductsEntitiesList.Entities)
                    {
                        if (!closedProducts.Contains(closedProductEntity.GetAttributeValue<Guid>("new_yv_productid")))
                            closedProducts.Add(closedProductEntity.GetAttributeValue<Guid>("new_yv_productid"));
                    }
                }
                return closedProducts;
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                Log.Information($"GetClosedProductsForDate error: FaultException<OrganizationServiceFault>:" + ex.Detail + " " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace);
                throw new Exception(ex.Message);
            }
            catch (Exception ex)
            {
                Log.Information($"GetClosedProductsForDate error: FaultException<OrganizationServiceFault>:" + ex.InnerException + " " + ex.Message + " " + ex.StackTrace);
                throw new Exception(ex.Message);
            }
        }

        private int GetNumberOfReservationsWithProduct(DateTime tourDate, Entity product)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_bid", "bidItem", false, true);
                fetchXml += @" <attribute name='new_yv_product' />
                 <attribute name='new_amount' />
                 <attribute name='new_bid_item_number' />
                 <attribute name='new_bidid' />
                 <order attribute='new_name' descending='false' />
                 <filter type='and'>
                     <condition attribute='statecode' operator='eq' value='0' />
                     <condition attribute='new_yv_product' operator='eq' value='{" + product.Id + @"}' />
                 </filter>
                 <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ad'>
                     <filter type='and'>
                         <condition attribute=""new_activity_day_status"" operator=""ne"" value=""3""/>
                         <condition attribute='new_start_datetime' operator='on' value='" + tourDate.Date.ToString("yyyy-MM-dd") + @"' />
                     </filter>
                 </link-entity>";

                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var bidItemsEntitiesList = _crmService.RetrieveMultiple(query);
                int amount = 0;
                if (bidItemsEntitiesList.Entities.Count > 0)
                {
                    foreach (Entity bidItemEntity in bidItemsEntitiesList.Entities)
                    {
                        amount += bidItemEntity.GetAttributeValue<int>("new_amount");
                    }
                }
                return amount;
            }
            catch (Exception ex)
            {
                Log.Information($"GetNumberOfReservationsWithProduct error:" + ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private List<PriceListItem> GetProductPrice(string productId, string lang, int amount, DateTime tourDate, int divisionRouter, int pricelistType, string activityDayLocation, int currentTime = 0)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_yv_product", "priceList", false, true);
                fetchXml += $@" 
                                <attribute name=""new_yv_productid""/>
                                <filter type=""and"">
                                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                    <condition attribute=""new_yv_productid"" operator=""eq"" value=""{productId}""/>
                                </filter>
                                <link-entity name=""new_yv_pricelist"" from=""new_yv_pricelist"" to=""new_yv_productid"" alias=""pricelist"">
                                    <attribute name=""createdon""/>
                                    <attribute name=""new_price_end_date""/>
                                    <attribute name=""new_pricelist_type""/>
                                    <attribute name=""new_activity_day_location""/>
                                    <order attribute=""new_activity_day_location"" descending=""true""/>
                                    <order attribute=""new_pricelist_type"" descending=""true""/>
                                    <order attribute=""new_price_end_date"" descending=""true""/>
                                    <filter type=""and"">
                                        <filter type=""or"">
                                            <condition attribute=""new_activity_day_location"" operator=""null""/>
                                            <condition attribute=""new_activity_day_location"" operator=""eq"" value=""{{2347C5AE-F818-EB11-8106-00505691117E}}""/>
                                        </filter>
                                        <filter type=""or"">
                                            <condition attribute=""new_pricelist_type"" operator=""null""/>
                                            <condition attribute=""new_pricelist_type"" operator=""eq"" value='{pricelistType}'/>
                                        </filter>
                                        <filter type=""or"">
                                            <condition attribute=""new_price_end_date"" operator=""null""/>
                                            <condition attribute=""new_price_end_date"" operator=""on-or-after"" value=""{DateTime.Now.ToString("yyyy-MM-dd")}""/>           
                                        </filter>
                                        <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                    </filter>
                                    <link-entity name=""new_yv_pricelist_item"" from=""new_yv_pricelist_item"" to=""new_yv_pricelistid"" alias=""pricelist_item"">
                                        <attribute name=""new_min_quantity_product_units""/>
                                        <attribute name=""new_max_quantity_product_units""/>
                                        <attribute name=""new_from_time""/>
                                        <attribute name=""new_to_time""/>
                                         <attribute name=""new_min_people_units_for_group""/>
                                        <attribute name=""new_discounted_price_for_groups""/>
                                         <attribute name=""new_price""/>
                                        <attribute name=""new_comment""/>
                                        <attribute name=""new_comments_en""/>      
                                        <attribute name=""new_name""/>
                                        <attribute name=""new_name_en""/>
                                        <attribute name=""new_name_he""/>
                                        <attribute name=""new_ticket_type""/>
                                        <attribute name=""new_sku""/>
                                        <link-entity name=""new_ticket_type"" from=""new_ticket_typeid"" to=""new_ticket_type"" alias=""ticket_type"" link-type=""outer"">
                                        <attribute name=""new_name_en""/>
                                        <attribute name=""new_name_he""/>
                                        <attribute name=""new_comment_en""/>
                                        <attribute name=""new_comment""/>
                                        <attribute name=""new_sorting_weight""/>
                                        </link-entity>

                                    </link-entity>
                                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                //אם חוזק בטעות 2 דברים מהרשימה של המחירים הוא מביא את השורות מחיר של שתיהם מה לעשות?
                var res = _onlineReservationRepository.GetPriceListItemList(fetchXml, lang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetProductPrice error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        private int GetPriceListTypeByTourCode(string tourCode)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "tourType", false, true);
                fetchXml += $@" 
                                <filter type=""and"">
                                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                    <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>
                                </filter>
                                <link-entity name=""new_budget_framework"" from=""new_budget_frameworkid"" to=""new_budget_framework"" alias=""budget_framework"">
                                    <attribute name=""new_pricelist_type""/>//סוג מחירון
                                </link-entity>";

                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var PriceListTypeEntity = _crmService.RetrieveMultiple(query);
                OptionSetValue PriceListType = (OptionSetValue)PriceListTypeEntity.Entities[0].GetAttributeValue<AliasedValue>("budget_framework.new_pricelist_type").Value;
                return PriceListType.Value;
            }
            catch (Exception ex)
            {
                Log.Error($"GetPriceListTypeByTourCode error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<KeyValuePair<Guid, string>> GetLanguagesByTourType(string tourCode, string lang, bool? heLang = null)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_language", "language", false, true);
                fetchXml += $@"<attribute name=""new_en_name""/>
                               <attribute name=""new_name""/>
                               <attribute name=""new_languageid""/>
                               <link-entity name='new_new_tour_type_new_language' from='new_languageid' to='new_languageid' visible='false' intersect='true'>         
                                    <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeid' alias='new_tour_type'>
                                    <filter type='and'>
                                        <condition attribute='statecode' operator= 'eq' value = '0'/>
                                        <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}"" />
                                    </filter>
                                    </link-entity>
                                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetLanguagesList(fetchXml, lang, heLang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetLanguagesByTourType error: {ex.Message}");
                throw new Exception(ex.Message);

            }
        }

        public List<KeyValuePair<Guid, string>> GetLanguagesByProduct(string productId, string lang)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_language", "language", false, true);
                fetchXml += $@"<attribute name=""new_en_name""/>
                               <attribute name=""new_name""/>
                               <attribute name=""new_languageid""/>
                               <link-entity name='new_new_yv_product_new_language' from='new_languageid' to='new_languageid' visible='false' intersect='true'>         
                                    <link-entity name='new_yv_product' from='new_yv_productid' to='new_yv_productid' alias='new_tour_type'>
                                    <filter type='and'>
                                        <condition attribute='statecode' operator= 'eq' value = '0'/>
                                        <condition attribute=""new_yv_productid"" operator=""eq"" value=""{productId}"" />
                                    </filter>
                                    </link-entity>
                                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetLanguagesList(fetchXml, lang, null);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetLanguagesByProduct error: {ex.Message}");
                throw new Exception(ex.Message);

            }
        }

        public List<string> GetTagsByTourType(string tourCode, string lang)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tag", "tag", false, true);
                fetchXml += $@"<attribute name=""new_name_{lang}""/>
                               <link-entity name='new_new_tour_type_new_tag' from='new_tagid' to='new_tagid' visible='false' intersect='true'>         
                                    <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeid' alias='new_tour_type'>
                                    <filter type='and'>
                                        <condition attribute='statecode' operator= 'eq' value = '0'/>
                                        <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}"" />
                                    </filter>
                                    </link-entity>
                                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetTabsList(fetchXml, lang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTagsByTourType error: {ex.Message}");
                throw new Exception(ex.Message);

            }
        }

        public List<Product> GetRequireProductsList(string tourCode, string nameProductList, string lang)
        {
            try
            {
                var condition = $@"<condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>";
                var fetchXml = InitXmlToProduct(tourCode, nameProductList, lang, new DateTime(), condition);
                var res = _onlineReservationRepository.GetProductsList(fetchXml, lang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetRequireProductsList error: {ex.Message}");
                throw new Exception(ex.Message);

            }
        }

        private string InitXmlToProduct(string tourCode, string nameProductList, string lang, DateTime tourDate, string condition = "", string filter = "")
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_yv_product", "product", false, true);
                fetchXml += $@"<attribute name=""new_payment_title_{lang}""/>//שם מקוצר
                               <attribute name=""new_name_en""/>//תאור
                               <attribute name=""new_name""/>//תאור
                               <attribute name=""new_is_recommended""/>//האם מומלץ
                               <attribute name=""new_show_on_package""/>//האם להראות בחבילה
                              // <attribute name=""new_description_en""/>//תאור באנגלית
                               //<attribute name=""new_description_he""/>//תאור בעברית
                                 <attribute name=""new_yv_productid""/>/id
                                 <attribute name=""new_max_products_per_day""/>//מקסימום פריטים ליום
                               <attribute name=""new_pricing_method""/>//צורת תמכור
                               <attribute name=""new_duration_of_hours""/>//משך פעילות
                               <attribute name=""new_max_products_per_day""/>//מספר נרשמים
                               <attribute name=""new_relevant_day_{tourDate.DayOfWeek.ToString().ToLower()}""/>//האם היום שבחרו רללונטי
                               <attribute name=""new_minimum_days""/>//משך פעילות
                               <attribute name=""new_division_router""/>//שיוך לאגף
                               <attribute name=""new_number_of_participants_per_product""/>//מספר מששתפים ליחידת מוצר
                               <attribute name=""new_is_required_service_provider""/>//האם נדרש נותן שירות
                               <attribute name=""new_sort_weight""/>//משקל מיון
                               <order attribute=""new_sort_weight"" descending=""false""/>
                               {filter}
                               <filter type='and'>
                                      <condition attribute=""statecode"" operator= ""eq"" value = ""0""/>
                               </filter>
                               <link-entity name='new_new_tour_type_new_yv_{nameProductList}' from='new_yv_productid' to='new_yv_productid' visible='false' intersect='true'>         
                                    <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeid' alias='new_tour_type'>
                                    <filter type='and'>
                                        <condition attribute=""statecode"" operator= ""eq"" value = ""0""/>
                                        <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}"" /> 
                                        {condition}
                                    </filter>
                                    </link-entity>
                                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                return fetchXml;
            }
            catch (Exception ex)
            {
                Log.Error($"InitXmlToProduct error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private List<Product> GetRequireProductOfTourTypeTahel(string lang, string tourCode)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "tour", false, true);
                fetchXml += $@"<attribute name=""new_name""/>
                               <filter type=""and"">
                                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                    <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>
                                    <condition attribute=""new_tour_type_tahel"" operator=""not-null""/>
                                </filter>
                               <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_type_tahel"" alias=""tour_type_tahel"">
                                    <attribute name=""new_tour_code""/>
                               </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var productEntitiesList = _crmService.RetrieveMultiple(query);
                var taheltourCode = "";
                List<Product> products = new List<Product>();
                if (productEntitiesList.Entities.Count > 0)
                {
                    taheltourCode = productEntitiesList.Entities.First().GetAttributeValue<AliasedValue>("tour_type_tahel.new_tour_code") != null ? productEntitiesList.Entities.First().GetAttributeValue<AliasedValue>("tour_type_tahel.new_tour_code").Value.ToString() : "";
                }
                if (taheltourCode != "")
                {
                    var fetchxmltahel = InitXmlToProduct(taheltourCode, "product_required", lang, new DateTime());
                    products = _onlineReservationRepository.GetProductsList(fetchxmltahel, lang);
                }
                return products;
            }
            catch (Exception ex)
            {
                Log.Error($"GetRequireProductOfTourTypeTahel error: {ex.Message}");
                throw new Exception(ex.Message);

            }
        }
        public string getFetchXmlSendPasswordToEmail(string instituitionSymbol)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_rsv_institution", "TourType", false, true);
            fetchXml += @$" <attribute name=""new_name""/>
                                <attribute name=""new_institution_symbol""/>
                                 <attribute name =""new_email""/>
                                <attribute name=""new_company_number""/>
                                <attribute name=""new_rsv_institutionid""/>
                                 <attribute name=""new_password""/>
                                <order attribute=""new_name"" descending=""false""/>";

            fetchXml += @$" <filter type=""and"">
                    <filter type=""or"">
                        <condition attribute=""new_company_number"" operator=""eq"" value=""{instituitionSymbol}""/>
                        <condition attribute=""new_institution_symbol"" operator=""eq"" value=""{instituitionSymbol}""/>
                    </filter>
                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                </filter>";
            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }
        private async Task<CrmResultItem> sendEmailWithPsswordToTravelAgents(Instituition instituition, string language)
        {
            var result = new CrmResultItem();

            var bcc = _config.GetValue<string>("bcc");
            JObject json = GetTranslateJson();
            if (!json.HasValues)
            {
                result.IsOk = false;
                // result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_TO_USER;
                result.ErrorMessageStatus = ErrorMessageStatus.GENERAL;

                return result;
            }

            string subject = json[language]["emailToTravelAgentToGetPassword"]["subject"].ToString()
                .Replace("branchName", instituition.BranchName);
            string message = json[language]["emailToTravelAgentToGetPassword"]["message"].ToString()
                .Replace("Branchpassword", instituition.Password);

            List<string> toToSend = new List<string>() { instituition.Email };
            List<string> bccToSend = new List<string>() { bcc };
            Direction direction = (language == "he") ? Direction.RTL : Direction.LTR;

            result = await sendEmailGenericCancel(toToSend, bccToSend, direction, subject, message);
            if (!result.IsOk)
            {
                result.IsOk = false;
                // result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_TO_USER;
                result.ErrorMessageStatus = ErrorMessageStatus.GENERAL;

                return result;
            }

            result.IsOk = true;
            result.Message = instituition.Email;
            return result;
        }
        private string GenerateRandomPassword()
        {
            Random random = new Random();
            StringBuilder password = new StringBuilder();

            // Generate 3 random lowercase letters
            for (int i = 0; i < 3; i++)
            {
                char letter = (char)random.Next('a', 'z' + 1);
                password.Append(letter);
            }

            // Generate 3 random numbers (0-9)
            for (int i = 0; i < 3; i++)
            {
                int number = random.Next(0, 10);
                password.Append(number);
            }

            return password.ToString();
        }
        private CrmResultItem sendNewPasswordInCrm(string password, Entity instituition)
        {
            var response = new CrmResultItem();
            if (instituition == null || instituition.Id == Guid.Empty)
            {
                response.IsOk = false;
                //response.ErrorMessageStatus = ErrorMessageStatus.INVALID_INSTITUTION_ID;
                response.ErrorMessageStatus = ErrorMessageStatus.GENERAL;
                return response;
            }

            Entity updateInstituition = new Entity("new_rsv_institution");
            updateInstituition.Id = instituition.Id;
            updateInstituition["new_password"] = password;
            try
            {
                _crmService.Update(updateInstituition);

            }
            catch (Exception ex)
            {
                //response.ErrorMessageStatus = ErrorMessageStatus.DIDNT_SAVE_CRM;
                response.ErrorMessageStatus = ErrorMessageStatus.GENERAL;
                return response;
            }
            response.IsOk = true;
            return response;
        }
        public async Task<CrmResultItem> GetPasswordTravelAgentsToEmail(string instituitionSymbol, string language)
        {
            try
            {
                var res = new CrmResultItem();
                var fetchXml = getFetchXmlSendPasswordToEmail(instituitionSymbol);
                EntityCollection instituitionEntities = _onlineReservationRepository.sendMailWithPasswordToTravelAgents(fetchXml);
                if (instituitionEntities.Entities.Count == 0)
                {
                    res.IsOk = false;
                    // res.ErrorMessageStatus = ErrorMessageStatus.INSTITUTION_DIDNT_FOUND;
                    res.ErrorMessageStatus = ErrorMessageStatus.GENERAL;
                    return res;
                }
                if (instituitionEntities.Entities.Count > 1)
                {
                    res.IsOk = false;
                    res.ErrorMessageStatus = ErrorMessageStatus.GENERAL;

                    // res.ErrorMessageStatus = ErrorMessageStatus.PASSWORD_TOO_MANY;
                    return res;
                }
                Entity instituitionEntity = instituitionEntities.Entities.FirstOrDefault();
                string email = instituitionEntity.GetAttributeValue<string>("new_email");
                string password = instituitionEntity.GetAttributeValue<string>("new_password");
                string name = instituitionEntity.GetAttributeValue<string>("new_name");

                if (!string.IsNullOrEmpty(email))  // Ensure email is not null or empty
                {
                    var instituition = new Instituition();
                    instituition.Email = email;
                    instituition.BranchName = name;
                    instituition.Id = instituitionEntity.Id.ToString();
                    if (!string.IsNullOrEmpty(password))
                    {
                        instituition.Password = password;
                    }
                    else
                    {
                        string newPassword = GenerateRandomPassword();
                        instituition.Password = newPassword;
                        res = sendNewPasswordInCrm(newPassword, instituitionEntity);
                        if (!res.IsOk)
                        {
                            return res;
                        }
                        //make new Password, save in crm , 
                    }

                    res = await sendEmailWithPsswordToTravelAgents(instituition, language);
                    return res;

                }
                else
                {
                    res.IsOk = false;
                    // res.ErrorMessageStatus = ErrorMessageStatus.MAIL_NOT_FOUND;
                    res.ErrorMessageStatus = ErrorMessageStatus.GENERAL;

                    return res;
                }


            }
            catch (Exception ex)
            {
                Log.Error($"GetPasswordTravelAgentsToEmail error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<Instituition> GetInstituitionList(string instituitionSymbol, string? password)
        //, bool isWithPassword
        {
            try
            {
                bool isWithPassword = !string.IsNullOrEmpty(password);
                var fetchXml = _newReservationRepository.StartFetchXml("new_rsv_institution", "TourType", false, true);
                fetchXml += @$" <attribute name=""new_name""/>
                                <attribute name=""new_institution_symbol""/>
                                <attribute name=""new_telephone""/>
                                <attribute name=""new_company_number""/>
                                <attribute name=""new_rsv_institutionid""/>
                                <attribute name=""new_address""/>
                                <attribute name=""new_city""/>
                                <attribute name=""new_paying_institution""/>
                                <attribute name=""new_is_paying_institution""/>
                                <attribute name=""new_is_blocked_for_reservation""/>";
                if (isWithPassword)
                {
                    fetchXml += @$"<attribute name=""new_password""/>
                                <attribute name =""new_email""/>";
                    fetchXml += @$"<order attribute=""new_name"" descending=""false""/>";
                    fetchXml += @$"<filter type=""and"">
                    <condition attribute=""new_password"" operator=""eq"" value=""{password}""/>
                    </filter>";
                }
                else
                {
                    fetchXml += @$"<order attribute=""new_name"" descending=""false""/>";
                }

                fetchXml += @$" <filter type=""and"">
                    <filter type=""or"">
                        <condition attribute=""new_company_number"" operator=""eq"" value=""{instituitionSymbol}""/>
                        <condition attribute=""new_institution_symbol"" operator=""eq"" value=""{instituitionSymbol}""/>
                    </filter>
                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                    <condition attribute=""new_almog_code"" operator=""not-null""/>
                </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetInstituitionList(fetchXml);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetInstituitionList error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public Guid createAttachmentInCrm(string PdfLink, Guid Guid, bool isReservation)
        {
            try
            {
                Log.Error($"createAttachmentInCrm start, PdfLink:{PdfLink}, guid: {Guid}, isReservation: {isReservation}");
                var attachmentEntity = new Entity("new_attachment");
                attachmentEntity["new_file_type"] = new OptionSetValue(9);
                attachmentEntity["new_name"] = PdfLink;
                if (isReservation)
                {
                    attachmentEntity["new_attachments"] = new EntityReference("new_reservation", Guid);
                }
                else
                {
                    attachmentEntity["new_event_registration_attachments"] = new EntityReference("new_event_registration", Guid);
                }
                var attachmentGuid = _crmService.Create(attachmentEntity);
                Log.Error($"createAttachmentInCrm end");
                return attachmentGuid;
            }
            catch (Exception ex)
            {
                Log.Error($"createAttachmentInCrm error: {ex.Message}");
                throw new Exception(ex.Message);
            }

        }

        public async Task<ReservationNumberGuid> CreateReservation(OnlineReservation reservation)
        {

            try
            {

                //remove all BidItems with amount 0
                if (reservation.BidItems != null && reservation.BidItems.Count > 0)
                {
                    reservation.BidItems = reservation.BidItems.FindAll(x => x.Amount > 0);
                }
                Log.Information($"\n------------- new reservation in : {DateTime.Now} -------------\n");
                var res = JsonConvert.SerializeObject(reservation);
                Log.Information($"CreateReservation start, reservation: {JsonConvert.SerializeObject(reservation)}");
                string format = "dd/MM/yyyy"; // ציון הפורמט המתאים
                reservation.ReservationDate = DateTime.ParseExact(reservation.reservationDateString, format, null);

                Log.Information($"The reservation date is: {reservation.ReservationDate}");


                string startTime = reservation.StartTime?.hours.ToString() + ":" + reservation.StartTime?.minutes.ToString();
                reservation.StartDateTime = new DateTime(reservation.ReservationDate.Value.Year, reservation.ReservationDate.Value.Month, reservation.ReservationDate.Value.Day, reservation.StartTime.hours, reservation.StartTime.minutes, 0);
                if (reservation.StartDateTime < DateTime.Now)
                {
                    throw new InvalidOperationException("לא ניתן לבחור שעה או תאריך לפני הזמן הנוכחי.");
                }
                if (reservation.StartTime != null && reservation.ReservationDate != null)
                {
                    startTime = reservation.StartTime.hours.ToString() + ":" + reservation.StartTime.minutes.ToString();
                    reservation.StartDateTime = new DateTime(reservation.ReservationDate.Value.Year, reservation.ReservationDate.Value.Month, reservation.ReservationDate.Value.Day, reservation.StartTime.hours, reservation.StartTime.minutes, 0);
                }


                //if (!IsMuseumScheduleActive(reservation.ScheduleEventGuid))
                //{
                //    throw new Exception("NO_ROOM");
                //}
                Entity tourType;
                var isSetInactiveBeforePayment = false;
                if (reservation.TotalPriceAfterDiscount > 0 && !reservation.PayLater)
                {
                    isSetInactiveBeforePayment = true;
                }
                tourType = GetTourByCode(reservation.TourType.BasicDetails.TourCode);


                var contactGuid = _helperService.GetContactByEmailOrMobile(reservation.Email, reservation.Phone);
                var guideLanguage = _config.GetValue<string>($"Lang-{reservation.FormLang}-Guid")!;
                Guid activityDayGuid;


                if (reservation.VisitorType == TypeVisitors.EDUCATIONAL_GROUPS &&

                   tourType.GetAttributeValue<OptionSetValue>("new_target_population") is OptionSetValue targetPopulation)

                {

                    reservation.TargetAudience = new KeyValuePair<int, string>(targetPopulation.Value, "NULL");

                }

                ReservationNumberGuid resrvationToReturn = _onlineReservationRepository.CreateReservation(reservation, tourType, Guid.Parse(guideLanguage), contactGuid, isSetInactiveBeforePayment, out activityDayGuid);
                if (reservation.ActivityPackageList != null)
                {
                    CreateEventRegistrationToPackages(reservation, activityDayGuid, resrvationToReturn.ReservationGuid);
                }
                Log.Information($"CreateReservation end ReservationGuid:{reservation.Guid}, reservationNumber:{reservation.ReservationNumber}.");
                return resrvationToReturn;
            }
            catch (Exception ex)
            {

                Log.Error($"CreateReservation error, Exception:{ex.Message}");
                throw new Exception(ex.Message);
            }

        }
        public int GetReservationsActivitiesPerDayExtends(string tourId, string dateString)
        {

            var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                            <entity name=""new_reservation"">
                                <attribute name=""new_name""/>
                                <attribute name=""createdon""/>
                                <attribute name=""new_reservation_status""/>
                                <attribute name=""new_number_of_groups""/>
                                <attribute name=""new_number_of_participants""/>
                                <attribute name=""new_num""/>
                                <attribute name=""new_rsv_institution""/>
                                <attribute name=""new_is_order_confirmed""/>
                                <attribute name=""new_budget_framework""/>
                                <attribute name=""new_activity_day_first_date""/>
                                <attribute name=""new_sub_source_reservation""/>
                                <attribute name=""new_tour_type_internet""/>
                                <attribute name=""new_payment_status""/>
                                <attribute name=""new_reservationid""/>
                                <order attribute=""createdon"" descending=""true""/>
                                <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                </filter>
                                <link-entity name=""new_activity_day"" from=""new_reservation"" to=""new_reservationid"" alias=""ac"">
                                <filter type=""and"">
                                <condition attribute=""new_activity_day_type"" operator=""eq"" uitype=""new_tour_type"" value=""{tourId}""/>
                                <condition attribute=""new_start_datetime"" operator=""on"" value=""{dateString}""/>
                                </filter>
                                </link-entity>
                            </entity>
                            </fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            return res.Entities.Count;
        }

        public void CreateEventRegistrationToPackages(OnlineReservation reservation, Guid activityDayGuid, string reservationGuid)
        {
            try
            {
                Log.Information($"CreateEventRegistrationToPackages start reservation: {reservation}, activityDayGuid: {activityDayGuid}");
                //לשלוף את הגוידים של הקבוצות
                List<Guid> groupsGuid = GetAllGroupsGuid(activityDayGuid);
                if (groupsGuid.Count == reservation.GroupsList.Count)
                {
                    for (int i = 0; i < reservation.ActivityPackageList.Count; i++)
                    {
                        Entity tourType = GetTourByCode(reservation.ActivityPackageList[i].ActivityTourCode);
                        ReservationNumberGuid reservationNumberGuid = new ReservationNumberGuid();
                        if (reservation.ActivityPackageList[i].ActivityIsNeedEvent == true)
                        {
                            //רישום לארוע ומעדכנת את רישום לארוע אחודה  עם קישור להזמנה
                            // המרת ההזמנה לצורך העתקתו לאוביקט חדש
                            string res = JsonConvert.SerializeObject(reservation);
                            OnlineReservation tempReservation = JsonConvert.DeserializeObject<OnlineReservation>(res);
                            Guid EventReservationId = GetEventReservationIdOfTourType(reservation.ActivityPackageList[i].ActivityTourCode);
                            tempReservation.TourType.BasicDetails.IsNeedEvent = reservation.ActivityPackageList[i].ActivityIsNeedEvent;
                            tempReservation.TourType.BasicDetails.TourCode = reservation.ActivityPackageList[i].ActivityTourCode;
                            tempReservation.StartTime = reservation.ActivityPackageList[i].ActivityStartTime;
                            tempReservation.StartDateTime = new DateTime(reservation.ReservationDate.Value.Year, reservation.ReservationDate.Value.Month, reservation.ReservationDate.Value.Day, tempReservation.StartTime.hours, tempReservation.StartTime.minutes, 0);
                            tempReservation.BidItems = reservation.ActivityPackageList[i].ActivityBidItems;
                            tempReservation.TourType.EventReservationId = EventReservationId.ToString();
                            tempReservation.Guid = "";
                            reservationNumberGuid = CreateEventRegistration(tempReservation, false, tourType, reservationGuid);
                            //ויצירת שורת לוז בשביל הרישום לארוע ומקשרת לארוע עבור כל קבוצה
                            //לשאול את תמר אם בכוונה שלחה סטרינג ריק בשיבוץ!!
                            for (int j = 0; j < reservation.GroupsList.Count; j++)
                            {
                                tempReservation.GroupsList[j].StartTime = null;
                                _onlineReservationRepository.CreateItineraryItem(tempReservation, j, activityDayGuid, groupsGuid[j], tourType, false, false, "", reservationNumberGuid.ReservationGuid.ToString(), null);
                            }
                        }
                        else
                        {
                            if (tourType.GetAttributeValue<EntityReference>("new_resource") != null && tourType.GetAttributeValue<EntityReference>("new_resource").Id == _config.GetValue<Guid>("MuseumResourceId"))
                            {
                                for (int j = 0; j < reservation.GroupsList.Count; j++)
                                {
                                    _onlineReservationRepository.CreateItineraryItem(reservation, j, activityDayGuid, groupsGuid[j], tourType, false, false, reservation.GroupsList[j].schedulingGuid, "", null);
                                }
                            }
                            else
                            {
                                for (int j = 0; j < reservation.GroupsList.Count; j++)
                                {
                                    _onlineReservationRepository.CreateItineraryItem(reservation, j, activityDayGuid, groupsGuid[j], tourType, false, false, "", reservationNumberGuid.ReservationGuid.ToString(), null);
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("not found all the groups guid");
                }
            }
            catch (Exception ex)
            {

                Log.Error($"CreateEventRegistrationToPackages error, Exception:{ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private List<Guid> GetAllGroupsGuid(Guid activityDayGuid)
        {
            List<Guid> groupsGuid = new List<Guid>();
            var fetchXml = $@"  <fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                                    <entity name=""new_activity_day"">          
                                        <attribute name=""new_activity_dayid""/>
                                        <order attribute=""new_name"" descending=""false""/>
                                        <filter type=""and"">
                                            <condition attribute=""new_activity_dayid"" operator=""eq"" value=""{activityDayGuid}""/>
                                        </filter>
                                        <link-entity name=""new_itinerary_group"" from=""new_activity_day"" to=""new_activity_dayid"" alias=""activityDay"">
                                            <attribute name=""new_itinerary_groupid""/>
                                            <filter type=""and"">
                                                
                                            </filter>
                                        </link-entity>
                                    </entity>
                                </fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            if (res.Entities.Count > 0)
            {
                for (int i = 0; i < res.Entities.Count; i++)
                {
                    groupsGuid.Add((Guid)res.Entities[i].GetAttributeValue<AliasedValue>("activityDay.new_itinerary_groupid").Value);
                }
            }
            return groupsGuid;
        }

        private Guid GetEventReservationIdOfTourType(string tourCode)
        {
            var fetchXml = $@"  <fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                <entity name=""new_tour_type"">
                                <attribute name=""new_event_reservations""/>
                                <order attribute=""new_name"" descending=""false""/>
                                    <filter type=""and"">
                                        <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                        <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>
                                    </filter>
                                </entity>
                                </fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            Guid guid = Guid.Empty;
            if (res.Entities.Count > 0)
            {
                guid = res.Entities[0].GetAttributeValue<EntityReference>("new_event_reservations").Id;
            }
            return guid;
        }
        public Entity GetTourByCode(string new_tour_code, bool isForDurationOnly = false)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType");
            if (isForDurationOnly)
            {
                fetchXml += @"<attribute name=""new_duration_of_hours"" />
                              <attribute name=""new_duration_of_hours_resource"" />";
            }
            else
            {
                fetchXml += @$"
                      <attribute name=""new_resource"" />
                      <attribute name=""new_budget_framework"" />
                      <attribute name=""new_duration_of_hours"" />
                      <attribute name=""new_max_participants"" />
                      <attribute name=""new_division_router"" />
                      <attribute name=""new_activity_day_location"" />
                      <attribute name=""new_yv_product"" />
                      <attribute name=""new_tour_typeid"" />
                      <attribute name=""new_rsv_department"" />
                      <attribute name=""new_duration_of_hours_resource"" />
                      <attribute name=""new_max_products_per_day"" />
                      <attribute name=""new_almog_interface_id"" />
                      <attribute name=""new_category_individuals"" />
                      <attribute name=""new_is_need_headset"" />
                      <attribute name=""new_is_guide_mandatory_per_group"" />
                      <link-entity name=""new_budget_framework"" to=""new_budget_framework"" from=""new_budget_frameworkid"" link-type=""outer"" alias=""BudgetFramework"">
                         <attribute name=""new_email"" />
                         <attribute name=""new_fax"" />
                         <attribute name=""new_telephone"" />
                         <link-entity name=""new_almog_interface"" to=""new_almog_interface"" from=""new_almog_interfaceid"" link-type=""outer"" alias=""Almog"">
                            <attribute name=""new_almog_sub_code"" />
                         </link-entity>
                         <link-entity name=""new_rsv_department"" to=""new_rsv_department"" from=""new_rsv_departmentid"" link-type=""outer"" alias=""Department"">
                            <attribute name=""new_name"" />
                            <attribute name=""new_en_name"" />
                            <attribute name=""new_rsv_departmentid"" />
                         </link-entity>
                      </link-entity>";
            }
            fetchXml += @$"<filter type=""and"">
                         <condition attribute=""new_tour_code"" operator=""eq"" value='{new_tour_code}' />
                         <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                      </filter>";
            fetchXml += _newReservationRepository.EndFetchXml();
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            if (res.Entities.Count > 0)
            {
                return res.Entities.First();
            }
            return new Entity();

        }

        public bool IsMuseumScheduleActive(string scheduleEventGuid)
        {
            var eventItemEntity = _crmService.Retrieve("new_event_item", Guid.Parse(scheduleEventGuid), new[] { "statecode" });
            if (eventItemEntity.GetAttributeValue<OptionSetValue>("statecode").Value == 1)//inactive ,more than 10 minutes passed.
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public List<Country> GetCountries()
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_country");
                fetchXml += @$" <attribute name=""new_name""/>
                                <attribute name=""new_name_he""/> 
                                <attribute name=""new_country_code""/>                        
                                <order attribute=""new_name"" descending=""false""/>
                                <filter type=""and"">
                                 <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                               </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetCountriesList(fetchXml);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetCountries error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<DateTime> GetCloseDates(string tourCode, int year, int month, DateTime? startDate = null)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type");
                fetchXml += @$" <attribute name=""new_relevant_day_sunday""/> 
                                <attribute name=""new_relevant_day_monday""/> 
                                <attribute name=""new_relevant_day_tuesday""/> 
                                <attribute name=""new_relevant_day_wednesday""/>                        
                                <attribute name=""new_relevant_day_thursday""/>                        
                                <attribute name=""new_relevant_day_friday""/>
                                <attribute name=""new_tour_code""/>
                                <attribute name=""new_resource""/>
                                <attribute name=""new_opening_hours_by_museum""/>
                                <attribute name=""new_enable_hours_by_default""/>
                                <attribute name=""new_minimum_days""/>
                                <attribute name=""new_activity_day_location""/>
                                <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>     
                                </filter>
                                <link-entity name=""new_spcific_date_and_hour"" from=""new_tour_type"" to=""new_tour_typeid"" alias=""spcific_date"" link-type=""outer"">
                                    <attribute name=""new_start_time""/>
                                </link-entity>
                                ";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetCloseDatesList(fetchXml, year, month, startDate);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetCloseDates error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<DateTime> GetEventDates(string tourCode, int year, int month, DateTime? startDate = null)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type");
                fetchXml += @$" <attribute name=""new_relevant_day_sunday""/> 
                                <attribute name=""new_relevant_day_monday""/> 
                                <attribute name=""new_relevant_day_tuesday""/> 
                                <attribute name=""new_relevant_day_wednesday""/>                        
                                <attribute name=""new_relevant_day_thursday""/>                        
                                <attribute name=""new_relevant_day_friday""/>
                                <attribute name=""new_tour_code""/>
                                <attribute name=""new_resource""/>
                                <attribute name=""new_opening_hours_by_museum""/>
                                <attribute name=""new_enable_hours_by_default""/>
                                <attribute name=""new_minimum_days""/>
                                <attribute name=""new_activity_day_location""/>
                                <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>     
                                </filter>
                                <link-entity name=""new_spcific_date_and_hour"" from=""new_tour_type"" to=""new_tour_typeid"" alias=""spcific_date"" link-type=""outer"">
                                    <attribute name=""new_start_time""/>
                                </link-entity>
                                ";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetCloseDatesList(fetchXml, year, month, startDate);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetCloseDates error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }


        private EntityCollection getCloseHoursInDate(string date, string tourTypeId)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_closed_days_and_activities");
                fetchXml += @$"<attribute name=""new_from_closing_date""/>
                            <attribute name=""new_to_closing_date""/>
                            <attribute name=""new_closing_type""/>
                            <attribute name=""statecode""/>
                            <order attribute=""new_name"" descending=""false""/>
                            <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_is_closed_full_day"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_from_closing_date"" operator=""on"" value=""{date}""/>
                                <filter type=""or"">
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""4""/>//סגירת פעילות
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""2""/>//קמפוס יד ושם סגור	
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""3""/>//סגירת מוזיאון
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""7""/>//פתיחת מוזיאון מוזיאון
  
                                    </filter>
                            </filter>
                               <link-entity name=""new_new_closed_days_and_activities_new_tour"" from=""new_closed_days_and_activitiesid"" to=""new_closed_days_and_activitiesid"" alias=""ClosedDays"" link-type='outer'>
                                  <attribute name=""new_tour_typeid"" />                                  
                                   <filter type=""and"">
                                      <condition attribute=""new_tour_typeid"" operator=""eq"" value=""{tourTypeId}"" />
                                    </filter>
                                </link-entity>";

                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var res = _crmService.RetrieveMultiple(query);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"getCloseHoursInDate error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<StartTimeItem> getOpenTimeList(string date, List<StartTimeItem> startTimeList, string tourId)
        {

            DateTime dateDateTime = DateTime.Parse(date);

            // ממיר למחרוזת בפורמט dd/MM/yyyy
            string formatted = dateDateTime.ToString("dd/MM/yyyy");

            var closeHoursEc = getCloseHoursInDate(date, tourId);

            var (closeHoursList,openHoursList) = GetCloseArrivalHoursByDate(DateTime.ParseExact(date, "yyyy-MM-dd", null), closeHoursEc, tourId, false);
         
            // סינון הרשימה של שעות ההתחלה

            startTimeList = getOpenHoursList(startTimeList, openHoursList,10);
      
            startTimeList = startTimeList
             .Where(startTime =>
                 !isInCloseHours(TimeSpan.Parse(startTime.StartTime), closeHoursList))
             .ToList();
            return startTimeList;
        }
        private List<StartTimeItem> getOpenHoursList(List<StartTimeItem> startTimeList, List<(TimeSpan From, TimeSpan To)> openHoursList, int TimeSlotMinute)
        {

            var seenTimes = new HashSet<TimeSpan>();
            startTimeList.ForEach(startTime =>
            {
                seenTimes.Add(TimeSpan.Parse(startTime.StartTime));
            });

            openHoursList.ForEach(openHour =>
            {
                for (var time = openHour.From; time <= openHour.To; time = time.Add(TimeSpan.FromMinutes(TimeSlotMinute)))
                {

                    if (!seenTimes.Add(time)) // Add מחזיר false אם כבר קיים
                        continue;

                    var stringTime = time.ToString(@"hh\:mm");
                    TimeSpan startTimeSpan = TimeSpan.Parse(stringTime);


                    var startTimeItem = new StartTimeItem
                    ()
                    {
                        StartTime = stringTime,
                        AvailablePlaces = -1,
                        isClosed = false,
                        exprensiveHour = false //לשאול את דבורה!! חשוב!
                    };

                    startTimeList.Add(startTimeItem);
                }
            });
            return startTimeList;
        }
        //public async Task<AvailableTimesResponse> GetAvailableTimesWithHourRegulation(List<StartTimeItem> startTimeItems, Entity tour, string tourDate)
        //{
        //    try
        //    {
        //        AvailableTimesResponse availableTimesResponse = new AvailableTimesResponse();
        //        TimeSpan difference = DateTime.ParseExact(tourDate, "dd/MM/yyyy", null) - DateTime.Now;

        //        int daysDifference = difference.Days;
        //        if (!IsFriday(tourDate))
        //        {
        //            bool isNeedHourRegulation = tour.GetAttributeValue<bool>("new_is_need_hours_regulation");
        //            bool isNeedGuideHourRegulation = tour.GetAttributeValue<bool>("new_is_need_guide_hours_regulation");
        //            if ((isNeedHourRegulation == true || isNeedGuideHourRegulation == true) && daysDifference > 21)
        //            {
        //                EntityReference department = ((EntityReference)tour.GetAttributeValue<AliasedValue>("BudgetFramework.new_rsv_department").Value);
        //                var HourRegulation = GetHourRegulationOfTourType(department.Name, (int)DateTime.ParseExact(tourDate, "dd/MM/yyyy", null).DayOfWeek);
        //                if (HourRegulation.Count == 0)
        //                {
        //                    startTimeItems = isNeedGuideHourRegulation ? SetStartTimeItemsAvailablePlacesToZero(startTimeItems, 0 , RegulationTarget.Guides): SetStartTimeItemsAvailablePlacesToZero(startTimeItems, 0, RegulationTarget.Visitors);
        //                    availableTimesResponse.startTimeItems = startTimeItems;
        //                    return availableTimesResponse;
        //                }
        //                for (int i = 0; i < HourRegulation.Count(); i++)
        //                {
        //                    var amount = HourRegulation[i].Amount;
        //                    if(tour.GetAttributeValue<bool>("new_is_need_hours_regulation") == true)
        //                    {
        //                        if (HourRegulation[i].Target == RegulationTarget.Visitors)
        //                        {
        //                            decimal maxParticipants = _config.GetValue<int>("MuseumOccupancy") * (amount / 100m);
        //                            int count = GetCountOfParticipantsInEventItemsThatHaveThisDepartment(department.Id.ToString(), tourDate);

        //                            decimal maxParticipantsForCard = maxParticipants - count;
        //                            startTimeItems = SetStartTimeItemsAvailablePlacesToZero(startTimeItems, maxParticipantsForCard, RegulationTarget.Visitors);
        //                        }
        //                    }
        //                    if(tour.GetAttributeValue<bool>("new_is_need_guide_hours_regulation") == true)
        //                    {
        //                        if (HourRegulation[i].Target == RegulationTarget.Guides)
        //                        {
        //                            int guideAmountNeeded = GetGuidesCountByDepartment(department.Id.ToString(), tourDate);
        //                            if (guideAmountNeeded < amount)
        //                            {
        //                                availableTimesResponse.guideAmountRemainingForTour = amount - guideAmountNeeded;
        //                            }
        //                            else
        //                            {
        //                                startTimeItems = SetStartTimeItemsAvailablePlacesToZero(startTimeItems, 0 , RegulationTarget.Guides);
        //                                availableTimesResponse.guideAmountRemainingForTour = 0;
        //                            }
        //                        }
        //                    }                
        //                }
        //            }
        //        }
        //        availableTimesResponse.startTimeItems = startTimeItems;
        //        return availableTimesResponse;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("GetAvailableTimesWithHourRegulation Error  ", ex.Message);
        //        throw new Exception(ex.Message);
        //    }
        //}
        public async Task<AvailableTimesResponse> GetAvailableTimesWithHourGuidRegulation(List<StartTimeItem> startTimeItems, Entity tour, string tourDate)
        {
            try
            {
                AvailableTimesResponse availableTimesResponse = new AvailableTimesResponse();
                TimeSpan difference = DateTime.ParseExact(tourDate, "dd/MM/yyyy", null) - DateTime.Now;

                int daysDifference = difference.Days;
                if (!IsFriday(tourDate))
                {
                    bool isNeedHourRegulation = tour.GetAttributeValue<bool>("new_is_need_hours_regulation");
                    bool isNeedGuideHourRegulation = tour.GetAttributeValue<bool>("new_is_need_guide_hours_regulation");
                    if ((isNeedHourRegulation == true || isNeedGuideHourRegulation == true))
                    {
                        EntityReference department = ((EntityReference)tour.GetAttributeValue<AliasedValue>("BudgetFramework.new_rsv_department").Value);
                        var HourRegulation = GetHourRegulationOfTourType(department.Name, (int)DateTime.ParseExact(tourDate, "dd/MM/yyyy", null).DayOfWeek);
                        if (HourRegulation.Count == 0)
                        {
                            startTimeItems = isNeedGuideHourRegulation ? SetStartTimeItemsAvailablePlacesToZero(startTimeItems, 0, RegulationTarget.Guides) : SetStartTimeItemsAvailablePlacesToZero(startTimeItems, 0, RegulationTarget.Visitors);
                            availableTimesResponse.startTimeItems = startTimeItems;
                            return availableTimesResponse;
                        }
                        
                            for (int i = 0; i < HourRegulation.Count(); i++)
                            {
                                var amount = HourRegulation[i].Amount;
                                if (isNeedHourRegulation == true && daysDifference > 21)
                                {
                                    if (HourRegulation[i].Target == RegulationTarget.Visitors)
                                    {
                                        decimal maxParticipants = _config.GetValue<int>("MuseumOccupancy") * (amount / 100m);
                                        int count = GetCountOfParticipantsInEventItemsThatHaveThisDepartment(department.Id.ToString(), tourDate);

                                        decimal maxParticipantsForCard = maxParticipants - count;
                                        startTimeItems = SetStartTimeItemsAvailablePlacesToZero(startTimeItems, maxParticipantsForCard, RegulationTarget.Visitors);
                                    }
                                }
                                if (isNeedGuideHourRegulation==true)
                                {
                                    if (HourRegulation[i].Target == RegulationTarget.Guides)
                                    {
                                        int guideAmountNeeded = GetGuidesCountByDepartment(department.Id.ToString(), tourDate);
                                        if (guideAmountNeeded < amount)
                                        {
                                            availableTimesResponse.guideAmountRemainingForTour = amount - guideAmountNeeded;
                                        }
                                        else
                                        {
                                            startTimeItems = SetStartTimeItemsAvailablePlacesToZero(startTimeItems, 0, RegulationTarget.Guides);
                                            availableTimesResponse.guideAmountRemainingForTour = 0;
                                        }
                                    }
                                }
                            }
                        
                    }

                    
                }
                availableTimesResponse.startTimeItems = startTimeItems;
                return availableTimesResponse;
            }
            catch (Exception ex)
            {
                Log.Error("GetAvailableTimesWithHourRegulation Error  ", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private int GetGuidesCountByDepartment(string department, string date)
        {
            try
            {
                var dateTimeStr = DateTime.ParseExact(date, "dd/MM/yyyy", null);
                var fetchXml = _newReservationRepository.StartFetchXml("new_activity_day");
                fetchXml += $@"<attribute name=""new_guide_amount_needed""/>
                                <filter type=""and"">
                                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                    <condition attribute=""new_start_datetime"" operator=""on"" value=""{dateTimeStr.ToString("yyyy-MM-dd")}""/>
                                </filter>
                                <link-entity name=""new_reservation"" from=""new_reservationid"" to=""new_reservation"" alias=""reservation""  >
                                  <attribute name=""new_responsible_agent""/>
                                  <filter type=""and"">
                                      <condition attribute=""new_responsible_agent"" operator=""eq"" value=""{department}""/>
                                  </filter>
                                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var activityDayList = _crmService.RetrieveMultiple(query);
                var guideAmountNeeded = 0;
                foreach (var activityDay in activityDayList.Entities)
                {
                    guideAmountNeeded += activityDay.GetAttributeValue<int>("new_guide_amount_needed");
                }
                return guideAmountNeeded;
            }
            catch (Exception ex)
            {
                Log.Information($"GetGuidesCountByDepartment error:{ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        //private decimal CalculateVisitorsAvailability(HoursRegulation regulation, EntityReference department, string tourDate)
        //{
        //    int count = GetCountOfParticipantsInEventItemsThatHaveThisDepartment(department.Id.ToString(), tourDate);
        //    if (regulation.Form == RegulationForm.Percentage)
        //    {
        //        decimal maxParticipants = _config.GetValue<int>("MuseumOccupancy") * (regulation.Amount / 100m);
        //        return maxParticipants - count;
        //    }
        //    return regulation.Amount;
        //}

        //private decimal CalculateGuidesAvailability(HoursRegulation regulation, EntityReference department, string tourDate)
        //{
        //    int usedGuides = GetCountOfGuidesInUse(department.Id.ToString(), tourDate);
        //    return regulation.Amount - usedGuides;
        //}

        private List<StartTimeItem> SetStartTimeItemsAvailablePlacesToZero(List<StartTimeItem> startTimeItems, decimal maxParticipantsForCard, RegulationTarget type)
        {
            for (int i = 0; i < startTimeItems.Count; i++)
            {
                if(type == RegulationTarget.Visitors)
                {
                    TimeSpan time = TimeSpan.Parse(startTimeItems[i].StartTime);
                    TimeSpan start = TimeSpan.FromHours(9.5);
                    TimeSpan end = TimeSpan.FromHours(13.5);
                    if (time >= start && time <= end)
                    {
                        if (startTimeItems[i].AvailablePlaces > (int)maxParticipantsForCard)
                        {
                            startTimeItems[i].AvailablePlaces = (int)maxParticipantsForCard;
                        }
                    }
                }
                if(type == RegulationTarget.Guides)
                {
                    startTimeItems[i].AvailablePlaces = (int)maxParticipantsForCard;
                }
            }
            return startTimeItems;
        }
        public EntityCollection GetSpecialDateInTourBydate(string tourTypeId)
        {
            try
            {
                var specialTimeList = new List<StartTimeItem>();
                var fetchXml = @$"<fetch version='1.0' output-format='xml-platform' mapping='logical' >
                          <entity name=""new_spcific_date_and_hour"" >
                            <attribute name=""new_start_time"" />
                            <order attribute=""new_start_time"" descending=""false"" />
                            <filter type=""and"" >
                              <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                              <condition attribute=""new_tour_type"" operator=""eq"" value='{tourTypeId}' />
                            </filter>
                          </entity>
                        </fetch>";
                var query = new FetchExpression(fetchXml);
                var specificDateTimeEntitiesList = _crmService.RetrieveMultiple(query);
                return specificDateTimeEntitiesList;
            }
            catch (Exception ex)
            {
                Log.Error("GetSpecialDateInTourBydate error", ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<StartTimeItem>> GetAvailableTimes(string tourCode, string tourDate, bool isEducationalGroup, Entity tourE)
        {
            //int numOfVisitors,
            try
            {


                Log.Information("Start GetAvailableTimes at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));                List<StartTimeItem> startTimeList = new List<StartTimeItem>();
                var dateTime = DateTime.ParseExact(tourDate, "dd/MM/yyyy", null);
           
                var TimeListOnTour = _onlineReservationRepository.GetTourDateAndTime(dateTime.Month, dateTime.Year, tourE.Id.ToString(),true);

                Log.Information("Calling GetTourHoursByDate at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                startTimeList = GetTourHoursByDate(TimeListOnTour, dateTime);

                Log.Information("Finish GetTourHoursByDate at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                startTimeList.AddRange(_newReservationService.GetSpecialEventTimeListByTourType(tourE.Id.ToString(), dateTime));
                string expensiveHoursConfig = _config.GetValue<string>("exprensiveHours");
                // פיצול המחרוזת כדי לקבל את שעת ההתחלה והסיום של הטווח
                var hoursParts = expensiveHoursConfig.Split('-');
                TimeSpan startExpensiveHour = TimeSpan.Parse(hoursParts[0]);
                TimeSpan endExpensiveHour = TimeSpan.Parse(hoursParts[1]);
                if (startTimeList.Count > 0)
                {
                    if (tourE.GetAttributeValue<bool>("new_is_need_event") == true)
                    {
                        foreach (StartTimeItem startTimeItem in startTimeList)
                        {
                            TimeSpan startTime = TimeSpan.Parse(startTimeItem.StartTime);
                            Log.Information("Calling CheckIfThereIsPlaceInMuseum at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                            startTimeItem.AvailablePlaces = CheckIfThereIsPlaceInMuseum(tourDate, new Time(startTime), tourCode);
                            Log.Information("Finish CheckIfThereIsPlaceInMuseum at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                            // בדיקה האם השעה בטווח
                            startTimeItem.exprensiveHour = (startTime >= startExpensiveHour && startTime <= endExpensiveHour);
                        }
                    }
                    else if (tourE.GetAttributeValue<EntityReference>("new_resource") != null)
                    {
                        var resourceGuid = tourE.GetAttributeValue<EntityReference>("new_resource").Id;
                        bool isNeedMuseum = false;
                        {
                            if (resourceGuid.Equals(_config.GetValue<Guid>("MuseumResourceId")))
                            {
                                isNeedMuseum = true;
                            }
                        }
                        Log.Information("Calling GetFullResource at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                        var resourceEntity = GetFullResource(resourceGuid);
                        Log.Information("Finish GetFullResource at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                        int maxVisitorsPerSlotTime = resourceEntity.GetAttributeValue<int>("new_capacity");
                        Log.Information("Calling GetResourceScheduleEventsByDate at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                        var eventList = _newReservationService.GetResourceScheduleEventsByDate(dateTime, resourceGuid);
                        Log.Information("Finish GetResourceScheduleEventsByDate at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                        var eventListGroupByDate = eventList.Entities.GroupBy(e => e.GetAttributeValue<DateTime>("new_start_datetime")).Select(x => new { Start = x.Key.ToLocalTime(), SumOfVisitors = x.Sum(a => a.GetAttributeValue<int>("new_number_of_participants")) }).ToList();
                        foreach (StartTimeItem startTimeItem in startTimeList)
                        {

                            var groupByDateItem = eventListGroupByDate.FirstOrDefault(item => item.Start.TimeOfDay.ToString() == startTimeItem.StartTime);
                            if (groupByDateItem != null)
                            {
                                startTimeItem.AvailablePlaces = maxVisitorsPerSlotTime - groupByDateItem.SumOfVisitors;
                            }
                            else
                            {
                                startTimeItem.AvailablePlaces = maxVisitorsPerSlotTime;
                            }
                            TimeSpan startTime = TimeSpan.Parse(startTimeItem.StartTime);

                            // בדיקה האם השעה בטווח
                            startTimeItem.exprensiveHour = (startTime >= startExpensiveHour && startTime <= endExpensiveHour);
                        }

                    }
                    else
                    {

                        foreach (StartTimeItem startTimeItem in startTimeList)
                        {
                            startTimeItem.AvailablePlaces = _config.GetValue<int>("generaleMaxVisitors");
                            TimeSpan startTime = TimeSpan.Parse(startTimeItem.StartTime);

                            // בדיקה האם השעה בטווח
                            startTimeItem.exprensiveHour = (startTime >= startExpensiveHour && startTime <= endExpensiveHour);
                        }
                    }
                    //מסנן את השעות שסוגורת בסגירות בפעילות יומית
                    Log.Information("Calling getOpenTimeList at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                    startTimeList = getOpenTimeList(dateTime.ToString("yyyy-MM-dd"), startTimeList, tourE.Id.ToString());
                    Log.Information("Finis getOpenTimeList at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                    return startTimeList;

                }


                //אם אין שעה ותאריך התחלה או שעה, רק אז תביא מהמשאב את שעות ברירת המחדל שלו
                if (startTimeList.Count == 0)
                {
                    var isOpeningHoursByMuseum = tourE.GetAttributeValue<bool>("new_opening_hours_by_museum");
                    var isEnableHoursByDefault = tourE.GetAttributeValue<bool>("new_enable_hours_by_default");
                    if (tourE.GetAttributeValue<EntityReference>("new_resource") != null)
                    {
                        var resourceId = tourE.GetAttributeValue<EntityReference>("new_resource").Id;
                        if (!resourceId.Equals(_config.GetValue<Guid>("MuseumResourceId")) || isOpeningHoursByMuseum)
                        {
                            Log.Information("Calling GetResourcePerSlotTime at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                          
                            var res = _newReservationService.GetResourcePerSlotTime(tourDate, "", tourCode, resourceId);
                            Log.Information("Finish GetResourcePerSlotTime at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                            startTimeList = res.Item2;
                            //מסנן את השעות שסוגורת בסגירות בפעילות יומית
                            Log.Information("Calling getOpenTimeList at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                            startTimeList = getOpenTimeList(dateTime.ToString("yyyy-MM-dd"), startTimeList, tourE.Id.ToString());
                            Log.Information("Finish getOpenTimeList at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                            //אם זה ארוע מביא כמות לפי הארוע
                            if (tourE.GetAttributeValue<bool>("new_is_need_event") == true)
                            {
                                foreach (StartTimeItem startTimeItem in startTimeList)
                                {
                                    TimeSpan startTime = TimeSpan.Parse(startTimeItem.StartTime);
                                    Log.Information("Calling CheckIfThereIsPlaceInMuseum at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                                    startTimeItem.AvailablePlaces = CheckIfThereIsPlaceInMuseum(tourDate, new Time(startTime), tourCode);
                                    Log.Information("Finish CheckIfThereIsPlaceInMuseum at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

                                    // בדיקה האם השעה בטווח
                                    startTimeItem.exprensiveHour = (startTime >= startExpensiveHour && startTime <= endExpensiveHour);
                                }
                            }
                            else if (tourE.GetAttributeValue<EntityReference>("new_resource") != null)
                            {
                                var resourceGuid = tourE.GetAttributeValue<EntityReference>("new_resource").Id;
                                bool isNeedMuseum = false;
                                {
                                    if (resourceGuid.Equals(_config.GetValue<Guid>("MuseumResourceId")))
                                    {
                                        isNeedMuseum = true;
                                    }
                                }
                                var resourceEntity = GetFullResource(resourceGuid);
                                int maxVisitorsPerSlotTime = resourceEntity.GetAttributeValue<int>("new_capacity");
                                var eventList = _newReservationService.GetResourceScheduleEventsByDate(dateTime, resourceGuid);
                                var eventListGroupByDate = eventList.Entities.GroupBy(e => e.GetAttributeValue<DateTime>("new_start_datetime")).Select(x => new { Start = x.Key.ToLocalTime(), SumOfVisitors = x.Sum(a => a.GetAttributeValue<int>("new_number_of_participants")) }).ToList();

                                foreach (StartTimeItem startTimeItem in startTimeList)
                                {

                                    // פרש את הזמן מה-StartTimeItem לפורמט מדויק של hh:mm
                                    var targetTime = TimeSpan.ParseExact(startTimeItem.StartTime, @"hh\:mm", CultureInfo.InvariantCulture);

                                    var groupByDateItem = eventListGroupByDate.FirstOrDefault(item => item.Start.TimeOfDay == targetTime);
                                    if (groupByDateItem != null)
                                    {
                                        startTimeItem.AvailablePlaces = maxVisitorsPerSlotTime - groupByDateItem.SumOfVisitors;
                                    }
                                    else
                                    {
                                        startTimeItem.AvailablePlaces = maxVisitorsPerSlotTime;
                                    }
                                    TimeSpan startTime = TimeSpan.Parse(startTimeItem.StartTime);

                                    // בדיקה האם השעה בטווח
                                    startTimeItem.exprensiveHour = (startTime >= startExpensiveHour && startTime <= endExpensiveHour);
                                }

                                return startTimeList;
                            }
                        }
                    }
                    else if (isEnableHoursByDefault)
                    {

                        var startTime = new TimeSpan();
                        var endTime = new TimeSpan();

                        if (isEducationalGroup)
                        {
                            startTime = TimeSpan.Parse(_config.GetValue<string>("educationalGroupStartTime"));
                            endTime = TimeSpan.Parse(_config.GetValue<string>("educationalGroupEndTime"));



                        }
                        else
                        {
                            startTime = TimeSpan.Parse(_config.GetValue<string>("startTime"));
                            endTime = TimeSpan.Parse(_config.GetValue<string>("endTime"));

                        }


                        for (var time = startTime; time <= endTime; time = time.Add(TimeSpan.FromMinutes(TIME_SLOT_MINTUE)))
                        {
                            var stringTime = time.ToString(@"hh\:mm");
                            TimeSpan startTimeSpan = TimeSpan.Parse(stringTime);


                            var startTimeItem = new StartTimeItem
                            ()
                            {
                                StartTime = stringTime,
                                AvailablePlaces = _config.GetValue<int>("generaleMaxVisitors"),
                                isClosed = false,
                                exprensiveHour = (startTimeSpan >= startExpensiveHour && startTimeSpan <= endExpensiveHour)
                            };

                            startTimeList.Add(startTimeItem);
                        }
                        //מסנן את השעות שסוגורת בסגירות בפעילות יומית
                        startTimeList = getOpenTimeList(dateTime.ToString("yyyy-MM-dd"), startTimeList, tourE.Id.ToString());

                        if (tourE.GetAttributeValue<bool>("new_is_need_event") == true)
                        {
                            foreach (StartTimeItem startTimeItem in startTimeList)
                            {
                                TimeSpan startTimeSpan = TimeSpan.Parse(startTimeItem.StartTime);

                                startTimeItem.AvailablePlaces = CheckIfThereIsPlaceInMuseum(tourDate, new Time(startTimeSpan), tourCode);

                                // בדיקה האם השעה בטווח
                            }
                        }
                        else if (tourE.GetAttributeValue<EntityReference>("new_resource") != null)
                        {
                            var resourceGuid = tourE.GetAttributeValue<EntityReference>("new_resource").Id;
                            bool isNeedMuseum = false;
                            {
                                if (resourceGuid.Equals(_config.GetValue<Guid>("MuseumResourceId")))
                                {
                                    isNeedMuseum = true;
                                }
                            }
                            var resourceEntity = GetFullResource(resourceGuid);
                            int maxVisitorsPerSlotTime = resourceEntity.GetAttributeValue<int>("new_capacity");
                            var eventList = _newReservationService.GetResourceScheduleEventsByDate(dateTime, resourceGuid);
                            var eventListGroupByDate = eventList.Entities.GroupBy(e => e.GetAttributeValue<DateTime>("new_start_datetime")).Select(x => new { Start = x.Key.ToLocalTime(), SumOfVisitors = x.Sum(a => a.GetAttributeValue<int>("new_number_of_participants")) }).ToList();

                            foreach (StartTimeItem startTimeItem in startTimeList)
                            {

                                // פרש את הזמן מה-StartTimeItem לפורמט מדויק של hh:mm
                                var targetTime = TimeSpan.ParseExact(startTimeItem.StartTime, @"hh\:mm", CultureInfo.InvariantCulture);

                                var groupByDateItem = eventListGroupByDate.FirstOrDefault(item => item.Start.TimeOfDay == targetTime);
                                if (groupByDateItem != null)
                                {
                                    startTimeItem.AvailablePlaces = maxVisitorsPerSlotTime - groupByDateItem.SumOfVisitors;
                                }
                                else
                                {
                                    startTimeItem.AvailablePlaces = maxVisitorsPerSlotTime;
                                }
                                startTime = TimeSpan.Parse(startTimeItem.StartTime);

                                // בדיקה האם השעה בטווח
                                startTimeItem.exprensiveHour = (startTime >= startExpensiveHour && startTime <= endExpensiveHour);
                            }

                        }
                        return startTimeList;
                    }



                }
                //מסנן את השעות שסוגורת בסגירות בפעילות יומית
                startTimeList = getOpenTimeList(dateTime.ToString("yyyy-MM-dd"), startTimeList, tourE.Id.ToString());

                return startTimeList;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAvailableTimes error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<HoursRegulation> GetHourRegulationOfTourType(string department, int day)
        {
            var result = _hoursRegulations.HoursRegulationsList.FindAll(Regulation => Regulation.Department == department && Regulation.Day == day);
            return result;
        }

        public int GetCountOfParticipantsInEventItemsThatHaveThisDepartment(string department, string date)
        {
            try
            {
                var dateTimeStr = DateTime.ParseExact(date, "dd/MM/yyyy", null);
                var fetchXml = _newReservationRepository.StartFetchXml("new_event_item");
                fetchXml += $@"<attribute name=""new_number_of_participants""/>
                            <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_rsv_department"" operator=""eq"" value=""{department}""/>
                                <condition attribute=""new_start_datetime"" operator=""on"" value=""{dateTimeStr.ToString("yyyy-MM-dd")}""/>
                                <condition attribute=""new_resource"" operator=""eq"" uiname=""מוזיאון"" uitype=""new_resource"" value=""{{FEC20EA9-3140-EB11-810B-00505691117E}}""/>
                                <condition attribute=""new_status"" operator=""eq"" value=""1""/>
                            </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var EventItemsList = _crmService.RetrieveMultiple(query);
                var count = 0;
                foreach (var eventItem in EventItemsList.Entities)
                {
                    count += eventItem.GetAttributeValue<int>("new_number_of_participants");
                }
                return count;
            }
            catch (Exception ex)
            {
                Log.Information($"GetCountOfParticipantsInEventItemsThatHaveThisDepartment error:{ex.Message}");
                throw new Exception(ex.Message);
            }

        }

        public List<RightToDiscount> GetRightDiscountList()
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_right_to_discount");
                fetchXml += $@"   <attribute name=""new_right_to_discountid""/>
                <attribute name=""new_name""/>
                <attribute name=""createdon""/>
                <attribute name=""new_discount_amount""/>
                <attribute name=""new_discount_type""/>
                <attribute name=""new_product_gives_discount""/>
                <attribute name=""new_product_gets_discount""/>
                <order attribute=""new_name"" descending=""false""/>
                <filter type=""and"">
                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                </filter>
            ";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetRightDiscountList(fetchXml);
                Log.Information($"GetDiscountList end, res count:{res.Count}");
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetRightDiscountList error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public EntityCollection getProduct()
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_yv_product");
                fetchXml += $@" 
                <attribute name=""new_yv_productid""/>
                <attribute name=""new_name""/>
                <attribute name=""createdon""/>
                <order attribute=""new_name"" descending=""false""/>
                <filter type=""and"">
                <condition attribute=""new_name"" operator=""eq"" value=""דמי רישום לסיורים בודדים""/>
                </filter> ";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.getReservationEntityToCancel(fetchXml);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetRightDiscountList error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public EntityCollection GetXmlRervationByReservationDetails(ObjectToEditCancelDetails reservationDetails, bool isForEdit, bool isCancel)
        {
            List<Condition> conditions = new List<Condition>();
            List<Condition> Institutionconditions = new List<Condition>();
            Condition numberCondition = new Condition("new_num", "eq", reservationDetails.ObjectNumber);
            conditions.Add(numberCondition);
            if (!reservationDetails.IsEmailIdentity)
            {
                Condition compantConditionl = new Condition("new_company_number", "eq", reservationDetails.Identity);
                Institutionconditions.Add(compantConditionl);
                Condition symbolCondition = new Condition("new_institution_symbol", "eq", reservationDetails.Identity);
                Institutionconditions.Add(symbolCondition);
            }
            else
            {
                Condition emailCondition = new Condition("new_email_contact", "eq", reservationDetails.Identity);
                conditions.Add(emailCondition);
            }

            if (isForEdit)
            {
                conditions.Add(new Condition("new_activity_day_first_date", "on", reservationDetails.ActivityDate));
            }

            var fetchXml = _newReservationRepository.StartFetchXml("new_reservation");
            fetchXml += $@"
            <attribute name=""new_name""/>
            <attribute name=""new_language""/>
            <attribute name=""new_form_language""/>
            <attribute name=""new_division_router""/>
            <attribute name=""createdon""/>
            <attribute name=""new_email_contact""/>
            <attribute name=""new_mobile_contact""/>
            <attribute name=""new_number_of_participants""/>
            <attribute name=""new_lastname_contact""/>
            <attribute name=""new_payment_status""/>
            <attribute name=""new_firstname_contact""/>
            <attribute name=""new_number_of_groups""/>
            <attribute name=""new_reservation_status""/>
            <attribute name=""new_number_of_groups""/>
            <attribute name=""new_num""/>
            <attribute name=""new_total_price_after_discount""/>
            <attribute name=""new_rsv_institution""/>
            <attribute name=""new_reservation_source""/>
            <attribute name=""new_is_order_confirmed""/>
            <attribute name=""new_budget_framework""/>
            <attribute name=""new_activity_day_first_date""/>
            <attribute name=""new_sub_source_reservation""/>
            <attribute name=""new_tour_type_internet""/>
            <attribute name=""new_reservationid""/>
            <attribute name=""new_internal_comments""/>
            <order attribute=""createdon"" descending=""true""/>
            <filter type='and'>
            <condition attribute=""statecode"" operator= ""eq"" value = ""0""/>
            </filter>";


            if (conditions.Count > 0)
            {
                var conditionsString = "";
                conditionsString += $@"
                <filter type=""and"">";
                foreach (var condition in conditions)
                {
                    if (!string.IsNullOrEmpty(condition.Name) && !string.IsNullOrEmpty(condition.Operator))
                    {
                        if (string.IsNullOrEmpty(condition.Value))
                        {
                            conditionsString += $@"
                            <condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}""/>";
                        }
                        else
                        {
                            conditionsString += $@"
                            <condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}""/>";
                        }
                    }
                }
                conditionsString += $@"
                </filter>";
                fetchXml += conditionsString;
            }
            if (!reservationDetails.IsEmailIdentity)
            {
                fetchXml += $@"
                <link-entity name=""new_rsv_institution"" from=""new_rsv_institutionid"" to=""new_rsv_institution"" alias=""Department"">
                        <attribute name=""new_name""/>
                        <attribute name=""new_rsv_institutionid""/>";

                if (Institutionconditions.Count > 0)
                {
                    fetchXml += @"<filter type=""or"">";
                    foreach (var condition in Institutionconditions)
                    {
                        fetchXml += $@"
                                <condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}""/>";
                    }
                    fetchXml += @"</filter>";
                }

                fetchXml += @"</link-entity>";
            }

            else
            {
                fetchXml += $@"
                <link-entity name=""new_rsv_institution"" from=""new_rsv_institutionid"" to=""new_rsv_institution"" alias=""Department"" link-type=""outer"" >
                <attribute name=""new_name""/>
                  <attribute name=""new_rsv_institutionid""/>
                </link-entity>";
            }
            if (isCancel)
            {
                fetchXml += $@"
                <link-entity name=""new_payment"" from=""new_reservation_payment"" to=""new_reservationid"" alias=""Payment"" link-type=""outer""> 
                        <attribute name=""new_creditcard_4digit""/>
                        <attribute name=""new_receipt_date""/>
                </link-entity>";

                fetchXml += $@"
                 <link-entity name=""new_event_registration"" from=""new_reservation"" to=""new_reservationid"" alias=""EventRegistration"" link-type=""outer"" >
                    <attribute name=""new_event_registrationid""/>
                </link-entity>";
            }
            if (isForEdit)
            {
                string linkEntitiesForEdit = getLinkEntitiesForEdit();
                fetchXml += linkEntitiesForEdit;
            }

            fetchXml += _newReservationRepository.EndFetchXml();
            var res = _onlineReservationRepository.getReservationEntityToCancel(fetchXml);

            return res;

        }
        public EntityCollection GetXmlTourType(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("The name parameter cannot be null or empty.");
            }

            var fetchXml = $@"
            <fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                <entity name=""new_tour_type"">
                    <attribute name=""new_name""/>
                    <attribute name=""new_name_en""/>
                    <attribute name=""createdon""/>
                    <attribute name=""new_tour_code""/>
                    <attribute name=""new_almog_interface_id""/>
                    <attribute name=""new_days_to_cancel""/>
                    <attribute name=""new_division_router""/>
                    <attribute name=""new_duration_of_hours""/>
                    <attribute name=""new_minimum_days""/>
                    <attribute name=""new_show_in_tahel""/>
                    <attribute name=""new_budget_framework""/>
                    <attribute name=""new_name_en""/>
                    <attribute name=""new_tour_type_tahel""/>
                    <attribute name=""new_show_on_website""/>
                    <attribute name=""new_is_promotion""/>
                    <attribute name=""new_is_need_hours_regulation""/>
                    <attribute name=""new_is_need_event""/>
                    <attribute name=""new_tour_typeid""/>
                    <order attribute=""new_name"" descending=""false""/>
                    <filter type=""and"">
                        <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                        <condition attribute=""new_name"" operator=""eq"" value=""{System.Security.SecurityElement.Escape(name)}""/>
                    </filter>
                </entity>
            </fetch>";

            return _onlineReservationRepository.getReservationEntityToCancel(fetchXml);
        }

        public EntityCollection GetXmlEventRegistrationByReservationDetails(ObjectToEditCancelDetails reservationDetails, bool isCancel)
        {
            List<Condition> conditions = new List<Condition>();
            List<Condition> Institutionconditions = new List<Condition>();
            Condition numberCondition = new Condition("new_num", "eq", reservationDetails.ObjectNumber);
            Condition statusCondition = new Condition("statecode", "eq", "0");
            conditions.Add(numberCondition);
            conditions.Add(statusCondition);
            if (!reservationDetails.IsEmailIdentity)
            {
                Condition contantConditionl = new Condition("new_company_number", "eq", reservationDetails.Identity);
                Institutionconditions.Add(contantConditionl);
                Condition symbolCondition = new Condition("new_institution_symbol", "eq", reservationDetails.Identity);
                Institutionconditions.Add(symbolCondition);
            }
            else
            {
                Condition emailCondition = new Condition("new_email_contact", "eq", reservationDetails.Identity);
                conditions.Add(emailCondition);
            }

            //var fetchXml = _newReservationRepository.StartFetchXml("new_event_registration");
            var fetchXml = $@"
            <fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" >
            <entity name=""new_event_registration"">
            <attribute name=""new_name""/>
             <attribute name=""createdon""/>
            <attribute name=""new_status""/>
             <attribute name=""new_email_contact""/>
            <attribute name=""new_firstname_contact""/>
            <attribute name=""new_lastname_contact""/>
            <attribute name=""new_role_in_school""/>
            <attribute name=""new_age_group""/>
            <attribute name=""new_teaching_subject""/>
            <attribute name=""new_number_of_participants""/>
            <attribute name=""new_total_price_after_discount""/>
            <attribute name=""new_school_name""/>
            <attribute name=""new_contact""/>
            <attribute name=""new_event_reservations""/>
            <attribute name=""new_payment_status""/>
             <attribute name=""new_division_router""/>
            <attribute name=""new_num""/>
            <attribute name=""new_reference""/>
            <attribute name=""new_dietary_needs_other""/>
            <attribute name=""new_dietary_needs""/>
            <attribute name=""new_email_contact""/>
            <attribute name=""new_event_registrationid""/>
            <order attribute=""createdon"" descending=""true""/>";


            if (conditions.Count > 0)
            {
                var conditionsString = "";
                conditionsString += $@"
                <filter type=""and"">";
                foreach (var condition in conditions)
                {
                    if (!string.IsNullOrEmpty(condition.Name) && !string.IsNullOrEmpty(condition.Operator))
                    {
                        if (string.IsNullOrEmpty(condition.Value))
                        {
                            conditionsString += $@"
                            <condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}""/>";
                        }
                        else
                        {
                            conditionsString += $@"
                                <condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}""/>";
                        }
                    }
                }
                conditionsString += $@"
                </filter>";
                fetchXml += conditionsString;
            }
            if (!reservationDetails.IsEmailIdentity)
            {

                fetchXml += $@"
               <link-entity name=""new_rsv_institution"" from=""new_rsv_institutionid"" to=""new_rsv_institution"" alias=""Department"">
                  <attribute name=""new_name""/>
                  <attribute name=""new_rsv_institutionid""/>";
                if (Institutionconditions.Count > 0)
                {
                    var conditionsString = "";
                    conditionsString += $@"
                      <filter type=""or"">";
                    foreach (var condition in Institutionconditions)
                    {
                        if (!string.IsNullOrEmpty(condition.Name) && !string.IsNullOrEmpty(condition.Operator))
                        {
                            if (string.IsNullOrEmpty(condition.Value))
                            {
                                conditionsString += $@"
                            <condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}""/>";
                            }
                            else
                            {
                                conditionsString += $@"
                                <condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}""/>";
                            }
                        }
                    }
                    conditionsString += $@"
                </filter>";
                    fetchXml += conditionsString;
                }
                fetchXml += $@"</link-entity>";
            }
            else
            {
                fetchXml += $@"
                <link-entity name=""new_rsv_institution"" from=""new_rsv_institutionid"" to=""new_rsv_institution"" alias=""Department"" link-type=""outer"" >
                    <attribute name=""new_name""/>
                  <attribute name=""new_rsv_institutionid""/>
                 </link-entity>";
            }
            fetchXml += $@"
                         <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" alias=""EventReservations""  link-type=""outer"" >
                                <attribute name=""new_name""/>
                                 <attribute name=""new_event_code""/>
                        </link-entity>";
            if (isCancel)
            {
                fetchXml += $@"
                <link-entity name=""new_payment"" from=""new_event_payment"" to=""new_event_registrationid"" alias=""Payment"" link-type=""outer""> 
                        <attribute name=""new_creditcard_4digit""/>
                        <attribute name=""new_receipt_date""/>
                </link-entity>";
            }

            fetchXml += $@"
            <link-entity name=""new_event_registration_activity_day"" from=""new_event_registration"" to=""new_event_registrationid"" alias=""EventRegistrationActivityDay"" link-type=""outer"">
                <link-entity name=""new_activity_day"" from=""new_activity_dayid"" to=""new_activity_day"" alias=""ActivityDay"" link-type=""outer"">
                    <attribute name=""new_start_datetime""/>
                    <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" alias=""TourType"" link-type=""outer"">
                        <attribute name=""new_tour_code"" />
                        <attribute name=""new_name"" />
                        <attribute name=""new_name_en"" />
                        <attribute name=""new_days_to_cancel"" />
                    </link-entity>
                </link-entity>
            </link-entity>";
            fetchXml += $@"
            </entity>
            </fetch>";

            var res = _onlineReservationRepository.getReservationEntityToCancel(fetchXml);
            return res;

        }

        public async Task<CrmResultItem> CancelEventRegistration(ObjectToEditCancelDetails evenRegistrationDetails)
        {

            var result = new CrmResultItem(false, "");
            var crm_number = evenRegistrationDetails.ObjectNumber;

            var reservationEntityResult = getEventRegistrationEntity(evenRegistrationDetails, true);
            if (!reservationEntityResult.IsOk)
            {
                result.ErrorMessageStatus = reservationEntityResult.ErrorMessageStatus;
                result.Message = reservationEntityResult.Message;
                return result;
            }
            Entity eventEC = reservationEntityResult.ResponseObject as Entity;
            if (eventEC == null)
            {
                Log.Error("even registration return null from  cancel even registration " + crm_number);
                result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                return result;
            }
            result = GetEventRegistrationToEditCancel(evenRegistrationDetails, false, true, eventEC);
            if (!result.IsOk)
            {
                return result;
            }
            OnlineReservation onlineReservationEvent = result.ResponseObject as OnlineReservation;
            var fetchXml = _onlineReservationRepository.getFetchXmlGetBidListForEventRegistration(evenRegistrationDetails.ObjectNumber);
            onlineReservationEvent.BidItems = _onlineReservationRepository.GetBidItemList(fetchXml, "עברית", true);
            result.ResponseObject = onlineReservationEvent;

            if (onlineReservationEvent == null)
            {
                result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                return result;
            }
            var bidList = _onlineReservationRepository.GetBidListForEventRegistration(evenRegistrationDetails.ObjectNumber);
            var reservationRegistrationAndHandlingFeesPrice = GetReservationContainsRegistrationAndHandlingFeesPrice(bidList, true);
            string language = evenRegistrationDetails.Language;
            var paymentStatus = eventEC.GetAttributeValue<OptionSetValue>("new_payment_status");
            if (paymentStatus != null && paymentStatus.Value != (int)PaymentStatus.NotPaid
                && paymentStatus.Value != (int)PaymentStatus.NotNeedPayment &&
                paymentStatus.Value != (int)PaymentStatus.NotRelevant &&
                paymentStatus.Value != (int)PaymentStatus.Placeholder_NotNeedPayment)
            {
                //need to send mail to check if refund is needed
                string textPaymentStatus = eventEC.FormattedValues["new_payment_status"].ToString();
                try
                {
                    string last4Digits = "";
                    string receipt = "";

                    if (eventEC.GetAttributeValue<AliasedValue>("Payment.new_creditcard_4digit") != null)
                    {
                        last4Digits = eventEC.GetAttributeValue<AliasedValue>("Payment.new_creditcard_4digit").Value.ToString();
                    }
                    if (eventEC.GetAttributeValue<AliasedValue>("Payment.new_receipt_date") != null)
                    {
                        receipt = ((DateTime)eventEC.GetAttributeValue<AliasedValue>("Payment.new_receipt_date").Value).ToLocalTime().ToString("dd/MM/yyyy");
                    }
                    var fetchXml2 = _onlineReservationRepository.getFetchXmlGetBidListForEventRegistration(evenRegistrationDetails.ObjectNumber);
                    onlineReservationEvent.BidItems = _onlineReservationRepository.GetBidItemList(fetchXml2, "עברית", true);
                    result = await SendMailToOrderCenter(paymentStatus.Value, textPaymentStatus, onlineReservationEvent, last4Digits, receipt, reservationRegistrationAndHandlingFeesPrice, true);
                    if (!result.IsOk)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_YAD_VASHEM;
                    Log.Error(DateTime.Now + "An error occurred in send email to YadVashem Aboat the cancel reservation" + ex.Message);
                    //return result;
                }
            }
            updateEventregistration(eventEC.Id);
            result.IsOk = true;

            result = await sendEmailCancelToUser(onlineReservationEvent, language, true);
            result.ResponseObject = new
            {
                OnlineReservation = onlineReservationEvent,
            };
            return result;
        }
        public CrmResultItem getEventRegistrationEntity(ObjectToEditCancelDetails evenRegistrationDetails, bool isCancel)
        {
            var result = new CrmResultItem(false, "");
            var ec = GetXmlEventRegistrationByReservationDetails(evenRegistrationDetails, isCancel);
            if (ec.Entities.Count == 0)//if doesnt exist
            {
                result.ErrorMessageStatus = ErrorMessageStatus.DIDNOT_FOUND;
                Log.Error("CancelFreeEventRegistration Cannot find EventRegistration " + evenRegistrationDetails.ObjectNumber);
                return result;
            }
            if (ec.Entities.Count > 1)//too many, something is wrong
            {
                if (isCancel)
                    result.ErrorMessageStatus = ErrorMessageStatus.TOO_MANY;
                Log.Error("CancelEventRegistration - too many EventRegistration " + evenRegistrationDetails.ObjectNumber);
                return result;
            }

            Entity eventEC = ec.Entities.FirstOrDefault();
            result.IsOk = true;
            result.ResponseObject = eventEC;
            ;
            return result;
        }
        private string getLinkEntitiesForEdit()
        {
            string fetchXml = "";
            fetchXml += $@"
            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_type_internet"" alias=""TourType"" link-type=""outer"">
            <attribute name=""new_tour_code""/>
            <attribute name=""new_resource""/>
            </link-entity>
            <link-entity name=""new_language"" from=""new_languageid"" to=""new_form_language"" alias=""Language"" link-type=""outer"">
            <attribute name=""new_language_code""/>
            <attribute name=""new_en_name""/>
            </link-entity>
            <link-entity name=""new_country"" from=""new_countryid"" to=""new_country"" alias=""Country"" link-type=""outer"">
            <attribute name=""new_country_code""/>
            </link-entity>
            <link-entity name=""new_activity_day"" from=""new_reservation"" to=""new_reservationid"" alias=""ActivityDay"" link-type=""inner"">
            <attribute name=""new_activity_dayid""/>
            <attribute name=""new_start_datetime""/>
            <attribute name=""new_end_datetime""/>
            <attribute name=""new_activity_day_status""/>
            <filter type=""and"">
            <condition attribute=""statecode"" operator=""eq"" value=""0""/>  <!-- רק פעיל -->
            </filter>
            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" alias=""TahelTourType"" link-type=""outer"">
            <attribute name=""new_tour_code""/>
            <attribute name=""new_resource""/>
            </link-entity>
     
            </link-entity>";
            return fetchXml;
        }
        public ReservationEditCancelResponse GetReservationToEdit(ObjectToEditCancelDetails reservationDetails)
        {

            var result = new ReservationEditCancelResponse();
            Entity reservationEntity;

            CrmResultItem reservationEntityResult = getReservationEntity(reservationDetails, true, false);//bring the reservation to edit

            if (!reservationEntityResult.IsOk)

            {
                return new ReservationEditCancelResponse(reservationEntityResult);
            }
            reservationEntity = reservationEntityResult.ResponseObject as Entity;
            if (reservationEntity == null)

            {
                Log.Information("Reservation return null from get reseravtion To cancel " + reservationDetails.ObjectNumber);
                result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                return result;
            }
            string resourceGuid = "";
            if (reservationEntity.Contains("TourType.new_resource"))
            {
                resourceGuid = ((EntityReference)((AliasedValue)reservationEntity["TourType.new_resource"]).Value).Id.ToString();
            }
            else if (reservationEntity.Contains("TahelTourType.new_resource"))

            {
                resourceGuid = ((EntityReference)((AliasedValue)reservationEntity["TahelTourType.new_resource"]).Value).Id.ToString();
            }
            var bidList = _onlineReservationRepository.GetBidListForReservation(reservationEntity.Id.ToString());

            var reservationDaysToCancel = getDaysToCancelByReservationId(reservationEntity.Id);

            var tourDate = reservationEntity.GetAttributeValue<DateTime>("new_activity_day_first_date").ToLocalTime();

            CrmResultItem isReservationCanBeCanceled = IsReservationCanBeChanged(tourDate, reservationEntity, resourceGuid, bidList, reservationDaysToCancel);

            Log.Information(DateTime.Now + ":reservationNum:" + reservationDetails.ObjectNumber + ", isReservationCanBeCancceled: " + isReservationCanBeCanceled.IsOk + ", " + isReservationCanBeCanceled.Message);

            if (!isReservationCanBeCanceled.IsOk)
            {
                return new ReservationEditCancelResponse(isReservationCanBeCanceled);
            }
            OnlineReservation onlineReservation = new OnlineReservation();
            result.TourReservation = new TourReservation();
            if (reservationEntity.Contains("Language.new_language_code") && reservationEntity.Contains("Language.new_en_name"))
            {
                var codeValue = (AliasedValue)reservationEntity["Language.new_language_code"];
                var nameValue = (AliasedValue)reservationEntity["Language.new_en_name"];
                result.TourReservation.new_form_language = codeValue.Value.ToString() + ":" + nameValue.Value.ToString();
            }
            onlineReservation.Guid = reservationEntity.Id.ToString();
            result.IsOk = true;
            return result;
        }


        public CrmResultItem GetObjectToEditCancel(ObjectToEditCancelDetails objectToEditCancelDetails, bool isForEdit)
        {
            var res = new CrmResultItem();
            res.IsOk = false;
            res.ErrorMessageStatus = ErrorMessageStatus.GENERAL;
            if (objectToEditCancelDetails.ObjectNumber.Trim().StartsWith(ObjectToCancelType.RESERVATION) || objectToEditCancelDetails.ObjectNumber.StartsWith(ObjectToCancelType.RESERVATION_SCHOOL))
            {

                res = GetReservationToEditCancel(objectToEditCancelDetails, isForEdit, !isForEdit, null);
                return res;
            }

            if (objectToEditCancelDetails.ObjectNumber.Trim().StartsWith(ObjectToCancelType.EVENT_REGISTRATION))
            {
                res = GetEventRegistrationToEditCancel(objectToEditCancelDetails, isForEdit, !isForEdit, null);
                return res;
            }

            // Fallback: If no prefix is matched, try both services
            res = GetReservationToEditCancel(objectToEditCancelDetails, isForEdit, !isForEdit, null);
            if (res.ErrorMessageStatus != null && res.ErrorMessageStatus == ErrorMessageStatus.DIDNOT_FOUND)
            {
                res = GetEventRegistrationToEditCancel(objectToEditCancelDetails, isForEdit, !isForEdit, null);
                return res;
            }
            else
            {
                return res;
            }

        }
        private async Task<CrmResultItem> SendMailToYadVashemCancelReservationOwnServiceProvider(string reservationId, DateTime? newTourDate, string reservationNum,
            Dictionary<Guid, string> serviceProviders, string coordinatorMail, string oldActivityDay, List<Entity> itiniraryGroupList, bool isForCancel)
        {
            CrmResultItem response = new CrmResultItem(false, "");
            var bcc = _config.GetValue<string>("bcc");
            var EducationTrainingDepartmentEmail = _config.GetValue<string>("EducationTrainingDepartmentEmail");
            string languageJson = "he";
            JObject json = GetTranslateJson();
            if (!json.HasValues)
            {
                response.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_YAD_VASHEM;
                return response;
            }
            string subject = json[languageJson]["emailToYadVashemLogsWhenCancelReservation"]["subject"].ToString()
           .Replace("{reservationNum}", reservationNum)
           .Replace("{newTourDate}", newTourDate?.ToString("dd/MM/yyyy"));

            string messageTemplate = json[languageJson]["emailToYadVashemLogsWhenCancelReservation"]["message"].ToString();

            // Determine values for conditionals
            string serviceProvidersSeparator = serviceProviders.Count > 0 ? ":" : ".";
            //string serviceProvidersList = serviceProviders.Count > 0 ? string.Join(", ", serviceProviders) : "";
            List<string> names = serviceProviders.Values.ToList();

            // Group names in sets of three
            List<string> groupedNames = new List<string>();
            for (int i = 0; i < names.Count; i += 3)
            {
                var namesGroup = names.Skip(i).Take(3);
                groupedNames.Add(string.Join(", ", namesGroup));
            }

            // Final formatted string
            string serviceProvidersList = string.Join("\n", groupedNames);
            string serviceProvidersNotification = serviceProviders.Count > 0 ? "יש לעדכן אותם על הביטול<br>" : "";

            // Replace placeholders
            string message = messageTemplate
                .Replace("{reservationNum}", reservationNum)
                .Replace("{newTourDate}", newTourDate?.ToString("dd/MM/yyyy"))
                .Replace("{serviceProvidersCount}", serviceProviders.Count.ToString())
                .Replace("{serviceProvidersSeparator}", serviceProvidersSeparator)
                .Replace("{serviceProvidersList}", serviceProvidersList)
                .Replace("{serviceProvidersNotification}", serviceProvidersNotification);

            List<string> toToSend = new List<string>() { bcc };
            List<string> bccToSend = new List<string>() { bcc };
            List<string> ccToSend = new List<string>() { };
            Direction direction = Direction.RTL;
            if (serviceProviders.Count > 0)
            {
                bccToSend.Add(EducationTrainingDepartmentEmail);
            }
            // Fetch email lists from configuration
            var serviceProvidersEmails = _config.GetSection("serviceProvidersSendEmail").Get<List<string>>();
            var coordinatorExistsEmails = _config.GetSection("itiniraryGroupListSendEmailCoordinatorMailExist").Get<List<string>>();
            var coordinatorNotExistsEmails = _config.GetSection("itiniraryGroupListSendEmaiCoordinatorMailNotExist").Get<List<string>>();
            coordinatorExistsEmails.Add(coordinatorMail);

            if (serviceProviders.Count > 0)
            {

                if (serviceProvidersEmails != null)
                {
                    toToSend.AddRange(serviceProvidersEmails);
                }

                if (!string.IsNullOrEmpty(coordinatorMail))
                {
                    toToSend.Add(coordinatorMail); // Add coordinator email if exists
                }
            }
            else if (itiniraryGroupList.Count > 0) // Only itinerary items, no service providers
            {
                if (!string.IsNullOrEmpty(coordinatorMail))
                {
                    // Add emails when coordinator exists
                    if (coordinatorExistsEmails != null)
                    {
                        toToSend.AddRange(coordinatorExistsEmails);
                    }
                }
                else
                {
                    // Add emails when coordinator does not exist
                    if (coordinatorNotExistsEmails != null)
                    {
                        toToSend.AddRange(coordinatorNotExistsEmails);
                    }
                }
            }

            response = await sendEmailGenericCancel(toToSend, bccToSend, direction, subject, message, ccToSend);
            return response;

        }
        public CrmResultItem GetEventRegistrationToEditCancel(ObjectToEditCancelDetails evenRegistrationDetails, bool isForEdit, bool isCancel, Entity eventEC = null)
        {


            var result = new CrmResultItem(false, "");
            var crm_number = evenRegistrationDetails.ObjectNumber;
            if (eventEC == null)
            {
                result = getEventRegistrationEntity(evenRegistrationDetails, isCancel);
                if (!result.IsOk)
                {
                    return result;
                }
                eventEC = result.ResponseObject as Entity;
                if (eventEC == null)
                {
                    Log.Information("getEventRegistration return null from get getEventRegistration To cancel " + crm_number);
                    result.IsOk = false;
                    result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                    return result;
                }
            }

            OnlineReservation onlineReservationEvent = new OnlineReservation();

            if (eventEC.GetAttributeValue<OptionSetValue>("new_status") != null
               && eventEC.GetAttributeValue<OptionSetValue>("new_status").Value == (int)ReservationStatus.EVENT_REGISTRATION_CANCELED) //cancelled
            {//already מבוטל
                Log.Information("EventRegistration " + crm_number + " has already been cancelled");
                result.ErrorMessageStatus = ErrorMessageStatus.CANCELLED;
                result.IsOk = false;
                return result;
            }
            if (eventEC.GetAttributeValue<OptionSetValue>("statecode") != null &&
            eventEC.GetAttributeValue<OptionSetValue>("statecode").Value == (int)ReservationStatus.INACTICE)
            {
                //result.Message = "reservation  is inactive";
                result.IsOk = false;
                result.ErrorMessageStatus = ErrorMessageStatus.INACTIVE;
                Log.Information("CancelFreeReservation reservation is inactive " + crm_number);
                return result;
            }
            if (eventEC.GetAttributeValue<AliasedValue>("Department.new_name") != null)
            {
                onlineReservationEvent.Institution = new Instituition();
                onlineReservationEvent.Institution.BranchName = ((AliasedValue)eventEC.Attributes["Department.new_name"]).Value.ToString();
                var aliasedInst = eventEC.GetAttributeValue<AliasedValue>("Department.new_rsv_institutionid");
                if (aliasedInst?.Value != null)
                {
                    var val = aliasedInst.Value;
                    if (val is Guid g) onlineReservationEvent.Institution.Id = g.ToString();
                    else if (val is EntityReference er) onlineReservationEvent.Institution.Id = er.Id.ToString();
                    else onlineReservationEvent.Institution.Id = val.ToString();
                }
            }
            //if (eventEC.GetAttributeValue<string>("new_internal_comments") != null)
            //    onlineReservationEvent.InternalComments = eventEC.GetAttributeValue<string>("new_internal_comments");
            var reservationId = eventEC.GetAttributeValue<Guid?>("new_event_registrationid");
            if (reservationId.HasValue)
            {
                onlineReservationEvent.Guid = reservationId.Value.ToString();
            }
            if (eventEC.GetAttributeValue<string>("new_firstname_contact") != null)
                onlineReservationEvent.FirstName = eventEC.GetAttributeValue<string>("new_firstname_contact");
            if (eventEC.GetAttributeValue<string>("new_lastname_contact") != null)
                onlineReservationEvent.LastName = eventEC.GetAttributeValue<string>("new_lastname_contact");
            if (eventEC.GetAttributeValue<string>("new_mobile_contact") != null)
                onlineReservationEvent.Phone = eventEC.GetAttributeValue<string>("new_mobile_contact");
            if (eventEC.GetAttributeValue<string>("new_email_contact") != null)
                onlineReservationEvent.Email = eventEC.GetAttributeValue<string>("new_email_contact");
            var divisionRouter = eventEC.GetAttributeValue<OptionSetValue>("new_division_router");
            if (divisionRouter != null)
                onlineReservationEvent.DivisionRouter = divisionRouter.Value.ToString();
            if (eventEC.GetAttributeValue<Money>("new_total_price_after_discount") != null)
                onlineReservationEvent.TotalPriceAfterDiscount = eventEC.GetAttributeValue<Money>("new_total_price_after_discount").Value;
            onlineReservationEvent.ReservationNumber = evenRegistrationDetails.ObjectNumber.ToString();
            if (eventEC.GetAttributeValue<int>("new_number_of_participants") != null)
                onlineReservationEvent.NumberOfVisitors = eventEC.GetAttributeValue<int>("new_number_of_participants");
            var eventRegistrationDaysToCancel = 0;
            // Try to populate TourType similarly to GetReservationToEditCancel:
            // prefer resolving the tour code via the linked event reservation -> tour_type, then fetch full TourType details
            try
            {
                string tourCode = null;

                // Prefer tour type coming from the ActivityDay -> TourType aliased link (added to the fetch XML).
                if (eventEC.Attributes.ContainsKey("TourType.new_tour_code") && eventEC.GetAttributeValue<AliasedValue>("TourType.new_tour_code")?.Value != null)
                {
                    tourCode = eventEC.GetAttributeValue<AliasedValue>("TourType.new_tour_code").Value.ToString();
                }



                // Fallback: if the event registration has a reference to new_event_reservations, find the tour type that references this event
                if (string.IsNullOrWhiteSpace(tourCode))
                {
                    var eventReservationsRef = eventEC.GetAttributeValue<EntityReference>("new_event_reservations");
                    if (eventReservationsRef != null)
                    {
                        var tourEntity = GetTourByEvent(eventReservationsRef.Id.ToString());
                        if (tourEntity != null && tourEntity.Id != Guid.Empty && tourEntity.Attributes.ContainsKey("new_tour_code"))
                        {
                            tourCode = tourEntity.GetAttributeValue<string>("new_tour_code");
                        }
                    }
                }

                // If we have a tour code, fetch the full basic and additional details
                if (!string.IsNullOrWhiteSpace(tourCode))
                {
                    onlineReservationEvent.TourType = GetTourBasicDetails(tourCode, evenRegistrationDetails.Language);
                    onlineReservationEvent.TourType.AdditionalTourDetails = GetAdditionalTourDetails(tourCode, evenRegistrationDetails.Language);

                    // Try to populate days-to-cancel: first look for aliased value from TourType, then via GetXmlTourType
                    try
                    {
                        if (eventEC.Attributes.ContainsKey("TourType.new_days_to_cancel") && eventEC.GetAttributeValue<AliasedValue>("TourType.new_days_to_cancel")?.Value != null)
                        {
                            eventRegistrationDaysToCancel = Convert.ToInt32(eventEC.GetAttributeValue<AliasedValue>("TourType.new_days_to_cancel").Value);
                        }
                        else
                        {
                            var xmlTour = GetXmlTourType(onlineReservationEvent.TourType.BasicDetails.Name ?? "");
                            if (xmlTour != null && xmlTour.Entities.Count > 0)
                            {
                                var tourTypeEntity = xmlTour.Entities.FirstOrDefault();
                                if (tourTypeEntity.Attributes.ContainsKey("new_days_to_cancel"))
                                {
                                    eventRegistrationDaysToCancel = tourTypeEntity.GetAttributeValue<int>("new_days_to_cancel");
                                }
                            }
                        }
                    }
                    catch { /* ignore days to cancel extraction errors */ }
                }
                else
                {
                    // last fallback: use the aliased name if nothing else
                    if (eventEC.Attributes.ContainsKey("EventReservations.new_name"))
                    {
                        string tourTypeName = eventEC.GetAttributeValue<AliasedValue>("EventReservations.new_name")?.Value.ToString();
                        onlineReservationEvent.TourType = new OnlineTourType();
                        onlineReservationEvent.TourType.BasicDetails = new BasicTourType();
                        onlineReservationEvent.TourType.BasicDetails.Name = tourTypeName;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"GetEventRegistrationToEditCancel: failed to resolve tour type: {ex.Message}");
            }

            if (eventEC.Attributes.ContainsKey("ActivityDay.new_start_datetime"))
            {
                DateTime tourDate = ((DateTime)eventEC.GetAttributeValue<AliasedValue>("ActivityDay.new_start_datetime").Value).ToLocalTime();
                result = IsEventRegistrationCanBeChanged(tourDate, eventRegistrationDaysToCancel);

                Log.Information(DateTime.Now + ":reservationNum:" + crm_number + ", isReservationCanBeCancceled: " + result.IsOk + ", " + result.Message);
                if (!result.IsOk)
                {
                    return result;
                }
                onlineReservationEvent.ReservationDate = tourDate;
                var hours = tourDate.Hour;
                var minutes = tourDate.Minute;

                // Create a Time object and assign it to StartTime
                onlineReservationEvent.StartTime = new Time
                {
                    hours = hours,
                    minutes = minutes
                };
            }
            result.ResponseObject = onlineReservationEvent;
            result.IsOk = true;
            return result;
        }
        public int CheckIfThereIsPlaceInMuseum(string reservationDateString, Time startTime, string tourCode)
        {
            var tour = GetTourByCode(tourCode);
            string format = "dd/MM/yyyy"; // ציון הפורמט המתאים
            var reservationDate = DateTime.ParseExact(reservationDateString, format, null);
            var startDateTime = new DateTime();
            if (startTime != null && reservationDate != null)
            {
                startDateTime = new DateTime(reservationDate.Year, reservationDate.Month, reservationDate.Day, startTime.hours, startTime.minutes, 0);
            }
            //Checks if there is really still free space left
            var eventReservationRef = tour.GetAttributeValue<EntityReference>("new_event_reservations");
            //אמור להגיע לכאן רק במצב של חבילה אם לא כן אמור להיות ראירועים מקושרים
            if (eventReservationRef == null)
            {
                // תוכלי להחזיר שגיאה, לוג, או פשוט להחזיר 0
                return -1;
            }
            var eventReservationId = eventReservationRef.Id;
            var activityDayTodayList = _newReservationService.GetActivityDayListByEventReservation(eventReservationId.ToString(), startDateTime);
            var activityDay = activityDayTodayList.Entities.ToList().Find(activityDay => activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime() == startDateTime);
            if (activityDay != null)
            {
                //if (activityDay.GetAttributeValue<int>("new_actual_number_of_participants") + numOfVisitors > (int)tour.GetAttributeValue<AliasedValue>("eventReservation.new_max_participants_per_event").Value)
                //{
                var num = (int)tour.GetAttributeValue<AliasedValue>("eventReservation.new_max_participants_per_event").Value - activityDay.GetAttributeValue<int>("new_actual_number_of_participants");
                return num < 0 ? 0 : num;
                //}
            }
            return (int)tour.GetAttributeValue<AliasedValue>("eventReservation.new_max_participants_per_event").Value;
        }
        public async Task<CrmResultItem> CancelReservation(ObjectToEditCancelDetails reservationDetails)
        {
            string language = reservationDetails.Language;
            var result = new CrmResultItem(false, "");
            var crm_number = reservationDetails.ObjectNumber;
            result = getReservationEntity(reservationDetails, false, true);
            Entity reservation = new Entity();
            if (!result.IsOk)
            {
                return result;
            }
            var eventRegistrationIds = new List<Guid>();

            //if (result.ResponseObject is IEnumerable<Entity> entityCollection)
            //{
            //    bool allHaveSameValue = entityCollection.All(e => e.GetAttributeValue<string>("new_num") == reservationDetails.ObjectNumber);
            //    reservation = entityCollection.FirstOrDefault();
            //    foreach (var entity in entityCollection)
            //    {
            //        if (entity.Contains("EventRegistration.new_event_registrationid"))
            //        {
            //            var eventRegistrationId = entity.GetAttributeValue<AliasedValue>("EventRegistration.new_event_registrationid")?.Value;

                //            if (eventRegistrationId != null)
                //            {
                //                eventRegistrationIds.Add((Guid)eventRegistrationId);
                //            }
                //        }
                //    }
                //    Console.WriteLine("ResponseObject is an IEnumerable<Entity>");
                //}
            if (result.ResponseObject is Entity singleEntity)
            {
                reservation = result.ResponseObject as Entity;
            }
            if (reservation == null)
            {
                Log.Error("Reservation return null from get reseravtion To cancel " + crm_number);
                result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                return result;
            }
            result = GetReservationToEditCancel(reservationDetails, false, true, reservation);
            if (!result.IsOk)
            {
                return result;
            }
            OnlineReservation onlineReservation = result.ResponseObject as OnlineReservation;
            if (onlineReservation == null)
            {
                result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                return result;
            }
            var bidList = _onlineReservationRepository.GetBidListForReservation(reservation.Id.ToString());
            var paymentStatus = reservation.GetAttributeValue<OptionSetValue>("new_payment_status");
            var reservationRegistrationAndHandlingFeesPrice = GetReservationContainsRegistrationAndHandlingFeesPrice(bidList);
            if (paymentStatus != null && paymentStatus.Value != (int)PaymentStatus.NotPaid
                && paymentStatus.Value != (int)PaymentStatus.NotNeedPayment &&
                paymentStatus.Value != (int)PaymentStatus.NotRelevant &&
                paymentStatus.Value != (int)PaymentStatus.Placeholder_NotNeedPayment)
            {
                //need to send mail to check if refund is needed
                string textPaymentStatus = reservation.FormattedValues["new_payment_status"].ToString();
                try
                {
                    string last4Digits = "";
                    string receipt = "";

                    if (reservation.GetAttributeValue<AliasedValue>("Payment.new_creditcard_4digit") != null)
                    {
                        last4Digits = reservation.GetAttributeValue<AliasedValue>("Payment.new_creditcard_4digit").Value.ToString();
                    }
                    if (reservation.GetAttributeValue<AliasedValue>("Payment.new_receipt_date") != null)
                    {
                        receipt = ((DateTime)reservation.GetAttributeValue<AliasedValue>("Payment.new_receipt_date").Value).ToLocalTime().ToString("dd/MM/yyyy");
                    }

                    var fetchXml = _onlineReservationRepository.getFetchXmlGetBidListForReservation(reservation.Id.ToString());
                    onlineReservation.BidItems = _onlineReservationRepository.GetBidItemList(fetchXml, "עברית");

                    result = await SendMailToOrderCenter(paymentStatus.Value, textPaymentStatus, onlineReservation, last4Digits, receipt, reservationRegistrationAndHandlingFeesPrice);
                }
                catch (Exception ex)
                {
                    result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_YAD_VASHEM;
                    Log.Error(DateTime.Now + ": SendMailCancelReservationWithPayment An error occurred, crm_number: " + crm_number + ", tourDate: " + onlineReservation.ReservationDate?.ToString("dd/MM/yyyy") + ex);
                }
            }

            result = await HandleEventItemsSchedule(reservation.Id.ToString(), "", "", onlineReservation.ReservationDate, reservation, true);
            if (!result.IsOk)
            {
                return result;
            }
            //update reservation status to cancel

            Entity updateReservation = new Entity("new_reservation");
            updateReservation.Id = reservation.Id;
            updateReservation["new_reservation_status"] = new OptionSetValue(3); //בוטלה
            updateReservation["new_cancellation_date"] = DateTime.Now;
            updateReservation["new_cancellation_reason"] = new OptionSetValue(9);//הזמנה מהאינטרנט
            _crmService.Update(updateReservation);
            result.IsOk = true;
            //update aCTIVITY DAYS:
            try
            {
                updateActivityDays(reservation.Id, onlineReservation, language);
            }
            catch (Exception ex)
            {
                Log.Error("Exception at updateActivityDays in cancel reservation" + DateTime.Now + ex);
                result.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                return result;
            }


            //in package- update event registration to be canceled

            //add func to get it
            //public string getFetchXmlEventRegistrationInReservation(ObjectToEditCancelDetails evenRegistrationDetails)
            //{
            //    return "";
            //}
            //public Entity getEventRegistrationInReservation(ObjectToEditCancelDetails evenRegistrationDetails)
            //{
            //    return new Entity();
            //}


            foreach (Guid eventRegistrationId in eventRegistrationIds)
            {
                try
                {
                    updateEventregistration(eventRegistrationId);

                }
                catch (Exception ex)
                {
                    Log.Error("Exception at updateEventregistration in cancel reservation" + DateTime.Now);
                    result.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                    return result;
                }
            }


            //sendEmailToUser
            result = await sendEmailCancelToUser(onlineReservation, language);
            result.ResponseObject = new
            {
                OnlineReservation = onlineReservation,
            };
            return result;
        }
        public CrmResultItem getReservationEntity(ObjectToEditCancelDetails reservationDetails, bool isForEdit, bool isCancel)
        {
            var crm_number = reservationDetails.ObjectNumber;
            var result = new CrmResultItem(false, "");
            EntityCollection ec = GetXmlRervationByReservationDetails(reservationDetails, isForEdit, isCancel); ;
            if (ec.Entities.Count == 0)//if doesnt exist
            {
                result.ErrorMessageStatus = ErrorMessageStatus.DIDNOT_FOUND;
                Log.Error("CancelFreeReservation Cannot find reservation " + crm_number);
                return result;
            }
            if (ec.Entities.Count > 1)//too many, something is wrong
            {
                if (!isCancel)
                {
                    result.ErrorMessageStatus = ErrorMessageStatus.TOO_MANY;
                    Log.Error("CancelFreeReservation - too many reservations " + reservationDetails.ObjectNumber);
                    return result;
                }
            }

            if (isCancel)
            {
                Entity reservation = ec.Entities.FirstOrDefault();
                result.ResponseObject = reservation;
            }
            else
            {
                Entity reservation = ec.Entities.FirstOrDefault();
                result.ResponseObject = reservation;

            }
            result.IsOk = true;
            return result;
        }

        public CrmResultItem GetReservationToEditCancel(ObjectToEditCancelDetails reservationDetails, bool isForEdit, bool isCancel, Entity reservation = null)
        {

            var result = new CrmResultItem(false, "");
            var crm_number = reservationDetails.ObjectNumber;
            if (reservation == null)
            {
                result = getReservationEntity(reservationDetails, isForEdit, isCancel);
                if (!result.IsOk)
                {
                    return result;
                }
                reservation = result.ResponseObject as Entity;
                if (reservation == null)
                {
                    Log.Information("Reservation return null from get reseravtion To cancel " + crm_number);
                    //result.Message = "Reservation return null from get reseravtion To cancel";
                    result.IsOk = false;
                    result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
                    return result;
                }
            }
            OnlineReservation onlineReservation = new OnlineReservation();

            if (reservation.GetAttributeValue<OptionSetValue>("new_reservation_status") != null
                && reservation.GetAttributeValue<OptionSetValue>("new_reservation_status").Value == (int)ReservationStatus.CANCELLED) //cancelled
            {//already מבוטל
                Log.Error("Reservation " + crm_number + " has already been cancelled");
                result.IsOk = false;
                result.ErrorMessageStatus = ErrorMessageStatus.CANCELLED;
                return result;
            }

            if (reservation.GetAttributeValue<OptionSetValue>("statecode") != null &&
                reservation.GetAttributeValue<OptionSetValue>("statecode").Value == (int)ReservationStatus.INACTICE)
            {
                result.ErrorMessageStatus = ErrorMessageStatus.INACTIVE;
                Log.Error("CancelFreeReservation reservation is inactive " + crm_number);
                result.IsOk = false;
                return result;
            }
            if (reservation.Attributes.Contains("Department.new_name") && reservation["Department.new_name"] is AliasedValue aliasedValue)
            {
                onlineReservation.Institution = new Instituition();
                onlineReservation.Institution.BranchName = aliasedValue.Value?.ToString();
                if (reservation.Attributes.Contains("Department.new_rsv_institutionid") && reservation["Department.new_rsv_institutionid"] is AliasedValue institutionid)
                {
                    onlineReservation.Institution.Id = institutionid.Value?.ToString();
                }

            }



            if (reservation.GetAttributeValue<string>("new_firstname_contact") != null)
                onlineReservation.FirstName = reservation.GetAttributeValue<string>("new_firstname_contact");
            var reservationId = reservation.GetAttributeValue<Guid?>("new_reservationid");
            if (reservationId.HasValue)
            {
                onlineReservation.Guid = reservationId.Value.ToString();
            }
            if (reservation.GetAttributeValue<string>("new_internal_comments") != null)
                onlineReservation.InternalComments = reservation.GetAttributeValue<string>("new_internal_comments");
            if (reservation.GetAttributeValue<Money>("new_total_price_after_discount") != null)
                onlineReservation.TotalPriceAfterDiscount = reservation.GetAttributeValue<Money>("new_total_price_after_discount").Value;
            if (reservation.GetAttributeValue<string>("new_lastname_contact") != null)
                onlineReservation.LastName = reservation.GetAttributeValue<string>("new_lastname_contact");
            if (reservation.GetAttributeValue<string>("new_mobile_contact") != null)
                onlineReservation.Phone = reservation.GetAttributeValue<string>("new_mobile_contact");
            if (reservation.GetAttributeValue<string>("new_email_contact") != null)
                onlineReservation.Email = reservation.GetAttributeValue<string>("new_email_contact");
            int numberOfGroups = reservation.GetAttributeValue<int>("new_number_of_groups");

            if (numberOfGroups != null)
            {
                onlineReservation.NumberOfVisitors = numberOfGroups;
                onlineReservation.GroupsList = new List<VisitorsGroup>(numberOfGroups);
                // replace the single-line call that tries to ToString() the AliasedValue with a robust extraction
                // (place this where the original line was: `onlineReservation.GroupsList =GetGroupsForEdit(...)`)

                var aliasedActivityDayId = reservation.GetAttributeValue<AliasedValue>("ActivityDay.new_activity_dayid");
                string activityDayGuidStr = null;

                if (aliasedActivityDayId?.Value is Guid guidValue)
                {
                    activityDayGuidStr = guidValue.ToString();
                }
                else if (aliasedActivityDayId?.Value is EntityReference er)
                {
                    activityDayGuidStr = er.Id.ToString();
                }
                else if (aliasedActivityDayId?.Value != null)
                {
                    // fallback - sometimes the aliased value may be returned as a string
                    activityDayGuidStr = aliasedActivityDayId.Value.ToString();
                }

                if (!string.IsNullOrEmpty(activityDayGuidStr))
                {
                    onlineReservation.GroupsList = GetGroupsForEdit(activityDayGuidStr);
                }
                else
                {
                    // no activity day id available — initialize empty list or handle as needed
                    onlineReservation.GroupsList = new List<VisitorsGroup>();
                }
                // Initialize each element of GroupsList as a new VisitorsGroup

            }


            if (reservation.GetAttributeValue<int>("new_number_of_participants") != null)
                onlineReservation.NumberOfVisitors = reservation.GetAttributeValue<int>("new_number_of_participants");
            var divisionRouter = reservation.GetAttributeValue<OptionSetValue>("new_division_router");
            if (divisionRouter != null)
                onlineReservation.DivisionRouter = divisionRouter.Value.ToString();
            //var formLanguageRef = reservation.GetAttributeValue<EntityReference>("new_form_language");
            //if (formLanguageRef != null)
            //    onlineReservation.FormLang = formLanguageRef.Name; // Or formLanguageRef.Id if you need the GUID
            onlineReservation.ReservationNumber = reservationDetails.ObjectNumber.ToString();
            var tourDate = reservation.GetAttributeValue<DateTime>("new_activity_day_first_date");
            if (reservation.GetAttributeValue<DateTime>("new_activity_day_first_date") != null)
            {
                onlineReservation.ReservationDate = tourDate;

                // Extract the time from the DateTime
                var hours = tourDate.Hour;
                var minutes = tourDate.Minute;

                // Create a Time object and assign it to StartTime
                onlineReservation.StartTime = new Time
                {
                    hours = hours,
                    minutes = minutes
                };
            }

            //string activityname = getActivitynameByReservationIdAndLanguage(reservation.Id, reservationDetails.Language);
            //onlineReservation.TourType = new OnlineTourType();
            //onlineReservation.TourType.BasicDetails = new BasicTourType();
            //onlineReservation.TourType.BasicDetails.Name = activityname;
            //string tourcode = getActivitynameByReservationIdAndLanguage(reservation.Id, reservationDetails.Language);
            // נסה תחילה לקרוא שדה aliased (אם ה‑fetch מחזיר את ה‑TourType/ActivityType)
            string tourcode = null;

            if (reservation.Contains("TourType.new_tour_code") && reservation["TourType.new_tour_code"] is AliasedValue aliasedTour && aliasedTour.Value != null)
            {
                tourcode = aliasedTour.Value.ToString();
            }


            if (string.IsNullOrWhiteSpace(tourcode))
            {
                var tourTypeRef = reservation.GetAttributeValue<EntityReference>("new_tour_type_internet");
                if (tourTypeRef != null)
                {
                    var tourEntity = _crmService.Retrieve("new_tour_type", tourTypeRef.Id, new[] { "new_tour_code" });
                    tourcode = tourEntity.GetAttributeValue<string>("new_tour_code");
                }
            }

            // עכשיו אפשר להשתמש ב־tourcode אם מלא
            if (!string.IsNullOrWhiteSpace(tourcode))
            {
                onlineReservation.TourType = GetTourBasicDetails(tourcode, reservationDetails.Language);
                onlineReservation.TourType.AdditionalTourDetails = GetAdditionalTourDetails(tourcode, reservationDetails.Language);
            }
            if (tourcode != null)
            {
                onlineReservation.TourType = GetTourBasicDetails(tourcode, reservationDetails.Language);
                onlineReservation.TourType.AdditionalTourDetails = GetAdditionalTourDetails(tourcode, reservationDetails.Language);
            }
            var bidList = _onlineReservationRepository.GetBidListForReservation(reservation.Id.ToString());
            var reservationDaysToCancel = getDaysToCancelByReservationId(reservation.Id);
            result = IsReservationCanBeChanged(tourDate, reservation, "", bidList, reservationDaysToCancel,isCancel);
            Log.Information(DateTime.Now + ":reservationNum:" + crm_number + ", isReservationCanBeCancceled: " + result.IsOk + ", " + result.Message);
            if (!result.IsOk)
            {
                return result;
            }
            result.ResponseObject = onlineReservation;
            result.IsOk = true;
            return result;
        }
        public int getDaysToCancelByReservationId(Guid reservationId)
        {
            var days = 0;
            string fetchXml = GetActivityDayFetchXml(reservationId);
            var eventItemList = _onlineReservationRepository.getEntitytItem(fetchXml);
            var eventItemListEntity = eventItemList.Entities.FirstOrDefault();
            Guid activityDayId = eventItemListEntity.GetAttributeValue<Guid>("new_activity_dayid");
            var getTourTypeEntitXmly = GetTourTypeEntityFromActivityDay(activityDayId);
            var EntityTourType = _onlineReservationRepository.getEntitytItem(getTourTypeEntitXmly);
            var entity = EntityTourType.Entities.FirstOrDefault();
            if (entity.Attributes.ContainsKey("TourType.new_days_to_cancel"))
            {
                var daysForCancel = entity.GetAttributeValue<AliasedValue>("TourType.new_days_to_cancel")?.Value;
                if (daysForCancel != null)
                {
                    days = Convert.ToInt32(daysForCancel);
                }

            }
            return days;

        }
        public string getActivitynameByReservationIdAndLanguage(Guid reservationId, string language)
        {
            string fetchXml = GetActivityDayFetchXml(reservationId);
            var eventItemList = _onlineReservationRepository.getEntitytItem(fetchXml);
            var eventItemListEntity = eventItemList.Entities.FirstOrDefault();
            if (eventItemListEntity.Attributes.ContainsKey("TourType.new_tour_code"))
            {
                //if (language == "he")
                //{
                //    var name = eventItemListEntity.GetAttributeValue<AliasedValue>("TourType.new_name").Value.ToString();
                //    return name;
                //}
                //else
                //{
                //    var name = eventItemListEntity.GetAttributeValue<AliasedValue>("TourType.new_name_en").Value.ToString();
                //    return name;
                //}
                var tourcode = eventItemListEntity.GetAttributeValue<AliasedValue>("TourType.new_tour_code").Value.ToString();
                return tourcode;
            }
            return null;

            ;
        }
        public JObject GetTranslateJson()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JsonFiels", "ReservationEmailText.json");

            if (!System.IO.File.Exists(path))
            {
                return new JObject();
            }

            using (StreamReader r = new StreamReader(path))
            {
                string jsonContent = r.ReadToEnd();
                return JObject.Parse(jsonContent);
            }
        }
        //public async Task<CrmResultItem> sendEmailEditReservation(List<string> to, List<string> bcc1, Direction direction, string subject, string message, List<string>? cc = null)
        //{
        //    CrmResultItem result = new CrmResultItem(false, "");
        //    var url = _config.GetValue<string>("sendMailUrl");
        //    var from = _config.GetValue<string>("noReplayYadVashemEmail");

        //    MailInfo mailInfo = new MailInfo();
        //    mailInfo.Message = message;
        //    mailInfo.Subject = subject;
        //    mailInfo.To = to;
        //    if (cc != null)
        //    {
        //        mailInfo.Cc = cc;

        //    }
        //    mailInfo.Bcc = bcc1;
        //    mailInfo.From = from;
        //    mailInfo.Files = new List<string>();
        //    mailInfo.Direction = direction;


        //    var jsonTosend = JsonConvert.SerializeObject(mailInfo);
        //    Log.Information($"SendMailToOrderCenter  send!!!: {JsonConvert.SerializeObject(jsonTosend, Formatting.Indented)}");
        //    var content = new StringContent(jsonTosend, Encoding.UTF8, "application/json");
        //    HttpClient httpClientEmail = new HttpClient();
        //    if (url != null)
        //    {
        //        httpClientEmail.BaseAddress = new Uri(url);
        //    }
        //    var response = await httpClientEmail.PostAsync("", content);
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var jsonResponse = await response.Content.ReadAsStringAsync();
        //        var wasEmailSent = JsonConvert.DeserializeObject<string>(jsonResponse);
        //        Log.Information($"wasEmailSent cancel  {wasEmailSent}");
        //        result.IsOk = true;
        //        return result;
        //    }
        //    else
        //    {
        //        Log.Error($"error sending email cancel");
        //        result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_USER;
        //        return result;
        //    }

        //}
        public async Task<CrmResultItem> sendEmailGenericCancel(List<string> to, List<string> bcc1, Direction direction, string subject, string message, List<string>? cc = null)
        {
            CrmResultItem result = new CrmResultItem(false, "");
            var url = _config.GetValue<string>("sendMailUrl");
            var from = _config.GetValue<string>("noReplayYadVashemEmail");

            MailInfo mailInfo = new MailInfo();
            mailInfo.Message = message;
            mailInfo.Subject = subject;
            mailInfo.To = to;
            if (cc != null)
            {
                mailInfo.Cc = cc;

            }
            mailInfo.Bcc = bcc1;
            mailInfo.From = from;
            mailInfo.Files = new List<string>();
            mailInfo.Direction = direction;


            var jsonTosend = JsonConvert.SerializeObject(mailInfo);
            Log.Information($"SendMailToOrderCenter  send!!!: {JsonConvert.SerializeObject(jsonTosend, Formatting.Indented)}");
            var content = new StringContent(jsonTosend, Encoding.UTF8, "application/json");
            HttpClient httpClientEmail = new HttpClient();
            if (url != null)
            {
                httpClientEmail.BaseAddress = new Uri(url);
            }
            var response = await httpClientEmail.PostAsync("", content);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var wasEmailSent = JsonConvert.DeserializeObject<string>(jsonResponse);
                Log.Information($"wasEmailSent cancel  {wasEmailSent}");
                result.IsOk = true;
                return result;
            }
            else
            {
                Log.Error($"error sending email cancel");
                result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_USER;
                return result;
            }

        }

        public async Task<CrmResultItem> SendMailToOrderCenter(int paymentStatus, string paymentStatusText, OnlineReservation onlineReservation
            , string last4Digits, string receipt, int reservationRegistrationAndHandlingFeesPrice = 0, bool iseventRegistration = false)
        {
            CrmResultItem result = new CrmResultItem(false, "");
            try
            {
                //Log.Information($"SendMailToOrderCenter : {JsonConvert.SerializeObject(onlineReservation, Formatting.Indented)}");

                JObject json = GetTranslateJson();
                if (!json.HasValues)
                {
                    Log.Error($"SendMailToOrderCenter error : {json}");
                    result.IsOk = false;
                    result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_YAD_VASHEM;
                    return result;
                }

                string languageJson = "he";
                string subject = "";
                string message = "";
                var CancelType = iseventRegistration ? "emailToYadVashemWhenCancelEventRegistration" : "emailToYadVashemWhenCancelReservation";
                if (iseventRegistration)
                {
                    subject = json[languageJson][CancelType]["subject"].ToString()
                     .Replace("EventRegistrationNum", onlineReservation.ReservationNumber)
                    .Replace("paymentStatusText", paymentStatusText)
                    .Replace("EventRegistrationtourDate", onlineReservation.ReservationDate.ToString());  // <-- Added closing parenthesis here

                    message = json[languageJson][CancelType]["message"].ToString()
                    .Replace("EventRegistrationNum", $"<a href=\"http://reservations.yadvashem/reservations/{onlineReservation.Guid}\">{onlineReservation.ReservationNumber}</a>")
                   .Replace("tourName", onlineReservation.TourType.BasicDetails.Name)
                   .Replace("EventRegistrationtourDate", onlineReservation.ReservationDate?.ToString("dd/MM/yyyy"))
                    .Replace("canceledFirstName ", onlineReservation.FirstName)
                    .Replace("canceledLastName", onlineReservation.LastName)
                    .Replace("canceledEmail", onlineReservation.Email)
                   .Replace("TotalPriceAfterDiscount", Math.Floor(onlineReservation.TotalPriceAfterDiscount).ToString())
                    .Replace("TotalPriceAfterIncreasingRegistrationAndHandlingFeesPrice", Math.Floor((onlineReservation.TotalPriceAfterDiscount - reservationRegistrationAndHandlingFeesPrice)).ToString())
                   .Replace("paymentStatusText", paymentStatusText);
                }
                else
                {
                    subject = json[languageJson][CancelType]["subject"].ToString()
                   .Replace("paymentStatusText", paymentStatusText)
                   .Replace("tourName", onlineReservation.TourType.BasicDetails.Name)
                   .Replace("tourDate", onlineReservation.ReservationDate?.ToString("dd/MM/yyyy"))  // <-- Added closing parenthesis here
                   .Replace("reservationNum", onlineReservation.ReservationNumber);
                    message = json[languageJson][CancelType]["message"].ToString()
                    .Replace("reservationNum", $"<a href=\"http://reservations.yadvashem/reservations/{onlineReservation.Guid}\">{onlineReservation.ReservationNumber}</a>")
                   .Replace("tourName", onlineReservation.TourType.BasicDetails.Name)
                   .Replace("tourDate", onlineReservation.ReservationDate?.ToString("dd/MM/yyyy"))
                   .Replace("canceledFirstName ", onlineReservation.FirstName)
                    .Replace("canceledLastName", onlineReservation.LastName)
                    .Replace("canceledEmail", onlineReservation.Email)
                   .Replace("TotalPriceAfterDiscount", Math.Floor(onlineReservation.TotalPriceAfterDiscount).ToString())
                   .Replace("TotalPriceAfterIncreasingRegistrationAndHandlingFeesPrice", Math.Floor((onlineReservation.TotalPriceAfterDiscount - reservationRegistrationAndHandlingFeesPrice)).ToString())
                   .Replace("paymentStatusText", paymentStatusText);
                }
                if (onlineReservation.Institution != null)
                {
                    string BranchName = onlineReservation.Institution.BranchName;
                    string jsonBranchName = json[languageJson]["emailToUserCancel"]["BranchName"].ToString()
                        .Replace("BranchName", BranchName);

                    message += jsonBranchName;
                }
                if (onlineReservation.InternalComments != null)
                {
                    string internalComments = json[languageJson][CancelType]["internalComments"].ToString()
                        .Replace("internalComments", onlineReservation.InternalComments);
                    message += internalComments;
                }
                string payment = "";
                if (last4Digits != "")
                {
                    payment += json[languageJson]["emailToYadVashemWhenCancelReservation"]["last4digits"].ToString()
                    .Replace("digits", last4Digits);
                }
                if (receipt != "")
                {
                    payment += json[languageJson]["emailToYadVashemWhenCancelReservation"]["receipt"].ToString()
                    .Replace("receipt", receipt);
                }
                DateTime? firstDate = onlineReservation?.ReservationDate?.ToLocalTime();
                DateTime? secondDate = DateTime.Now.ToLocalTime();

                int? numberOfDays = (firstDate.HasValue)
                    ? Math.Abs((secondDate.Value - firstDate.Value).Days)
                    : (int?)null;



                string NumDaysCanceledBeforeTjeActivity = json[languageJson]["emailToYadVashemWhenCancelReservation"]["NumDaysCanceledBeforeTjeActivity"].ToString()
               .Replace("days", numberOfDays.ToString());
                message += NumDaysCanceledBeforeTjeActivity;
                message += payment;
                message += GetBidItemsMessage(onlineReservation);
                string end = json[languageJson][CancelType]["endMessage"].ToString();
                message += end;

                var bcc = _config.GetValue<string>("bcc");
                var school = _config.GetValue<string>("schoolEmail");
                var visit = _config.GetValue<string>("visitEmail");
                var EducationTrainingDepartmentEmail = _config.GetValue<string>("EducationTrainingDepartmentEmail");
                List<string> toToSend = new List<string>() { bcc };
                List<string> bccToSend = new List<string>() { bcc };
                List<string> ccToSend = new List<string>() { };
                Direction direction = Direction.RTL;
                // Add recipients based on the payment status and language
                if (paymentStatus == (int)PaymentStatus.Paid)
                {
                    toToSend.Add(visit);
                    ccToSend.Add(school);
                    ccToSend.Add(EducationTrainingDepartmentEmail);

                }
                else
                {
                    toToSend.Add(school);
                    ccToSend.Add(EducationTrainingDepartmentEmail);
                }
                result = await sendEmailGenericCancel(toToSend, bccToSend, direction, subject, message, ccToSend);
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_YAD_VASHEM;
                Log.Error($"{DateTime.Now} Cancel reservation Exception read json to notify the people concerned payment, reservationNum: {onlineReservation.ReservationNumber}" + ex.Message);
                return result;
            }


        }
        public string GetBidItemsMessage(OnlineReservation onlineReservation)
        {
            var message = "";
            var productIds = _config.GetSection("listOfProductDidnwtGiveBackMoneyInCancel").Get<List<string>>();

            var tableRows = new StringBuilder();

            tableRows.AppendLine("<table border='1' style='border-collapse: collapse; width: 100%; direction: rtl;'>");
            tableRows.AppendLine("<thead><tr>");
            tableRows.AppendLine("<th>מוצר</th>");
            tableRows.AppendLine("<th>כמות</th>");
            tableRows.AppendLine("<th>מחיר לפני הנחה</th>");
            tableRows.AppendLine("<th>מחיר לאחר הנחה</th>");
            tableRows.AppendLine("<th>זכאי להחזר</th>");
            tableRows.AppendLine("</tr></thead>");
            tableRows.AppendLine("<tbody>");

            foreach (var item in onlineReservation.BidItems) // assuming you have a List<BidItem> bidItems
            {
                var p = productIds;
                var t = item.Product.Id.ToLower();
                var i = productIds.Contains(item.Product.Id.ToUpper());

                tableRows.AppendLine("<tr>");
                tableRows.AppendLine($"<td>{item.Product?.Value}</td>");
                tableRows.AppendLine($"<td>{item.Amount}</td>");
                tableRows.AppendLine($"<td>{item.TotalPriceBeforeDiscount} שח</td>");
                tableRows.AppendLine($"<td>{item.TotalPriceAfterDiscount} שח</td>");
                tableRows.AppendLine($"<td>{(!productIds.Contains(item.Product.Id.ToUpper()) ? "כן" : "לא")}</td>");
                tableRows.AppendLine("</tr>");
            }

            tableRows.AppendLine("</tbody></table>");
            message += "<br><br><b>פרטי הצעת המחיר:</b><br>" + tableRows.ToString();

            return message;
        }
        private async Task<CrmResultItem> sendEmailCancelToUser(OnlineReservation onlineReservation, string language, bool isEventRegistration = false)
        {
            var result = new CrmResultItem(false, "");
            if (onlineReservation != null)
            {

                bool isWithoutRefund = false;
                var bcc = _config.GetValue<string>("bcc");
                JObject json = GetTranslateJson();
                if (!json.HasValues)
                {
                    result.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_YAD_VASHEM;
                    return result;
                }
                string message = "";
                string start = json[language]["emailToUserCancel"]["Start"].ToString()
                        .Replace("FirstName", onlineReservation.FirstName)
                        .Replace("LastName", onlineReservation.LastName);

                // Determine refund policy message from JSON
                message += start;
                string refundPolicyKey = isWithoutRefund ? "WithoutRefund" : "WithRefund";
                var cancelType = isEventRegistration ? "emailToUserWhenCancelEventRegistration" : "emailToUserWhenCancelReservation";
                string refundPolicy = json[language][cancelType]["RefundPolicy"][refundPolicyKey].ToString();

                // Determine which signature to use
                string SignatureKey = "";
                if (onlineReservation.DivisionRouter == ((int)MainDivision.School).ToString())
                    SignatureKey = "School";

                else if (onlineReservation.DivisionRouter == ((int)MainDivision.Archive).ToString())
                    SignatureKey = "Archive";
                else
                    SignatureKey = "ReservationsCenter";

                string Signature = json[language]["emailToUserCancel"]["Signatures"][SignatureKey].ToString();
                string branchName = "";
                if (onlineReservation?.Institution?.BranchName != null)
                {
                    branchName = json[language]["emailToUserCancel"]["BranchName"].ToString()
                        .Replace("BranchName", onlineReservation.Institution.BranchName);
                }

                string subject = "";
                if (isEventRegistration)
                {
                    subject = json[language][cancelType]["subjectToCancel"].ToString()
                   .Replace("EventRegistrationNum", onlineReservation.ReservationNumber);
                    message += json[language][cancelType]["descriptionToCancel"].ToString()
                    .Replace("EventRegistrationtourDate", onlineReservation.ReservationDate?.ToString("dd/MM/yyyy") ?? onlineReservation.ReservationDate?.ToString())
                   .Replace("EventRegistrationNum", onlineReservation.ReservationNumber)
                   .Replace("BranchName", branchName)
                   .Replace("RefundPolicy", refundPolicy)
                   .Replace("Signature", Signature);
                }
                else
                {
                    subject = json[language][cancelType]["subjectToCancel"].ToString()
                   .Replace("reservationNumber", onlineReservation.ReservationNumber);
                    message += json[language][cancelType]["descriptionToCancel"].ToString()
                    .Replace("ReservationDate", onlineReservation.ReservationDate?.ToString("dd/MM/yyyy") ?? onlineReservation.ReservationDate?.ToString())
                   .Replace("ReservationNumber", onlineReservation.ReservationNumber)
                   .Replace("BranchName", onlineReservation.Institution != null ? onlineReservation.Institution.BranchName : "")
                       .Replace("<br>שם המזמין: ", onlineReservation.Institution == null ? "" : "<br>שם המזמין: ")
                   .Replace("RefundPolicy", refundPolicy)
                   .Replace("Signature", Signature);
                }
                string institutionEmail = "";
                try
                {
                    if (onlineReservation.TourType.BasicDetails.IsNeedEvent)
                    {
                        institutionEmail = GetInstitutionEmailFromEventRegistration(onlineReservation.ReservationNumber);
                    }
                    else
                    {
                        institutionEmail = GetInstitutionEmail(onlineReservation.ReservationNumber);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to get institution email: {ex.Message}");
                }
                List<string> toToSend = new List<string>() { onlineReservation.Email };
                if (!string.IsNullOrWhiteSpace(institutionEmail))
                {
                    toToSend.Add(institutionEmail);
                }
                List<string> bccToSend = new List<string>() { bcc };
                List<string> ccToSend = new List<string>() { };
                Direction direction = (language == "he") ? Direction.RTL : Direction.LTR;

                result = await sendEmailGenericCancel(toToSend, bccToSend, direction, subject, message, ccToSend);
                result.Message = message;
                return result;
            }
            result.ErrorMessageStatus = ErrorMessageStatus.GOT_NUULL_CRM;
            return result;

        }
        public void updateEventregistration(Guid eventRegistrationId)
        {
            Entity updateEventRegistration = new Entity("new_event_registration");
            updateEventRegistration.Id = eventRegistrationId;
            updateEventRegistration["new_status"] = new OptionSetValue(10); //בוטלה


            updateEventRegistration["new_cancellation_date"] = DateTime.Now;
            updateEventRegistration["new_cancellation_reason"] = new OptionSetValue(9);//הזמנה מהאינטרנט
            updateEventRegistration["statecode"] = new OptionSetValue(1); //לא פעיל
            _crmService.Update(updateEventRegistration);
        }
        public void updateActivityDays(Guid reservationId, OnlineReservation onlineReservation, string language)
        {

            string fetchXml = GetActivityDayFetchXml(reservationId);
            EntityCollection activityDays = _onlineReservationRepository.getEntitytItem(fetchXml);
            if (activityDays.Entities.Count <= 0)
            {

                Log.Error("CancelFreeReservation activity day with reservation id " + reservationId + " does not exist");
            }
            if (activityDays.Entities.Count > 0)
            {
                foreach (Entity activityDay in activityDays.Entities)
                {

                    activityDay["new_activity_day_status"] = new OptionSetValue(3); //בוטלה
                    Log.Information("new_activity_day_status = cancelled");
                    _crmService.Update(activityDay);
                    Log.Information("new_activity_day_status = cancelled: after Update");
                }
            }
        }

        public async Task<CrmResultItem> CancelObject(ObjectToEditCancelDetails objectToCancel)
        {
            var res = new CrmResultItem();
            res.IsOk = false;
            res.ErrorMessageStatus = ErrorMessageStatus.GENERAL;

            if (objectToCancel.ObjectNumber.Trim().StartsWith(ObjectToCancelType.RESERVATION) || objectToCancel.ObjectNumber.StartsWith(ObjectToCancelType.RESERVATION_SCHOOL))
            {
                res = await CancelReservation(objectToCancel);
                return res;
            }
            if (objectToCancel.ObjectNumber.Trim().StartsWith(ObjectToCancelType.EVENT_REGISTRATION))
            {
                res = await CancelEventRegistration(objectToCancel);
                return res;
            }

            // Fallback: If no prefix is matched, try both services
            res = await CancelReservation(objectToCancel);
            if (res.ErrorMessageStatus != null && res.ErrorMessageStatus == ErrorMessageStatus.DIDNOT_FOUND)
            {
                res = await CancelEventRegistration(objectToCancel);
                return res;
            }
            else
            {
                return res;
            }


        }
        public async Task<CrmResultItem> HandleEventItemsSchedule(string reservationId, string resourceId, string activityDayId, DateTime? newTourDate, Entity reservationEntity, bool isForCancel = false)
        {
            CrmResultItem response = new CrmResultItem(true);
            try
            {

                string fetchXml = getXmlHandleEventItemsSchedule(reservationId, resourceId);
                var eventItemList = _onlineReservationRepository.getEntitytItem(fetchXml);
                Dictionary<Guid, string> serviceProviders = new Dictionary<Guid, string>();
                string coordinatorMail = "";
                string reservationNum = "";
                string oldActivityDay = "";
                foreach (var eventItemEntity in eventItemList.Entities)
                {
                    if (eventItemEntity.Attributes.ContainsKey("new_service_provider"))
                    {
                        var eventItemForUpdate = new Entity("new_event_item", eventItemEntity.Id);
                        eventItemForUpdate["new_is_waiting_to_be_cancelled"] = true;
                        _crmService.Update(eventItemForUpdate);
                        var serviceProvider = eventItemEntity.GetAttributeValue<EntityReference>("new_service_provider");
                        if (serviceProvider != null && !serviceProviders.ContainsKey(serviceProvider.Id))
                        {
                            serviceProviders.Add(serviceProvider.Id, serviceProvider.Name);
                            if (string.IsNullOrEmpty(reservationNum))
                            {
                                reservationNum = eventItemEntity.GetAttributeValue<AliasedValue>("Reservation.new_num").Value.ToString();
                                oldActivityDay = ((DateTime)eventItemEntity.GetAttributeValue<AliasedValue>("Reservation.new_activity_day_first_date").Value).ToLocalTime().ToString("dd/MM/yyyy");
                            }
                            if (string.IsNullOrEmpty(coordinatorMail) && eventItemEntity.Attributes.ContainsKey("Coordinator.new_email"))
                            {
                                coordinatorMail = eventItemEntity.GetAttributeValue<AliasedValue>("Coordinator.new_email").Value.ToString();
                            }
                        }
                    }
                    else
                    {
                        if (eventItemEntity.Attributes.ContainsKey("new_resource"))
                        {
                            var eventItemForUpdate = new Entity("new_event_item", eventItemEntity.Id);
                            eventItemForUpdate["new_status"] = new OptionSetValue(10);//מבוטל
                            _crmService.Update(eventItemForUpdate);
                        }
                    }
                }
                var itiniraryGroupList = new List<Entity>();
                if (serviceProviders.Count == 0 && !isForCancel)
                {
                    var itiniraryListByActivityDayAndResource = GetItiniraryListByActivityDayAndResource(activityDayId, resourceId);
                    EntityCollection itiniraryGroupResult = _onlineReservationRepository.getEntitytItem(itiniraryListByActivityDayAndResource);
                    itiniraryGroupList = itiniraryGroupResult.Entities.ToList().FindAll(entity =>
                    {
                        if (!entity.Attributes.Contains("EventItem.new_resource"))
                        {
                            return true;
                        }
                        return (entity.GetAttributeValue<AliasedValue>("EventItem.new_resource").Value as EntityReference).Id.ToString() != resourceId;
                    });
                    if (itiniraryGroupList.Count > 0)
                    {
                        string fetchXmlCoordinator = GetReservationFetchXml(reservationId);
                        var coordinatorRes = _crmService.RetrieveMultiple(new FetchExpression(fetchXmlCoordinator));
                        if (coordinatorRes.Entities.Count > 0)
                        {
                            var entity = coordinatorRes.Entities.FirstOrDefault();
                            if (entity.Attributes.ContainsKey("Coordinator.new_email"))
                                coordinatorMail = entity.GetAttributeValue<AliasedValue>("Coordinator.new_email").Value.ToString();
                            reservationNum = entity.GetAttributeValue<string>("new_num");
                            oldActivityDay = entity.GetAttributeValue<DateTime>("new_activity_day_first_date").ToLocalTime().ToString("dd/MM/yyyy");
                        }
                    }
                }
                else if (serviceProviders.Count > 0)
                {
                    reservationEntity["new_is_care_of_schedules_required"] = true;
                }
                // Send mail if there is service providers scheduled, Or send mail to coordinator only if there is  optional itinirary items
                if (serviceProviders.Count > 0 || itiniraryGroupList.Count > 0)
                {
                    if (!isForCancel)
                    {
                        reservationEntity["new_reservation_status"] = new OptionSetValue((int)ReservationStatus.WAITINGTOSCHEDULE); //ממתין לשיבוץ
                    }
                    Log.Information(DateTime.Now + $" go SendMailToYadVashemLogs {serviceProviders}");
                    response = await SendMailToYadVashemCancelReservationOwnServiceProvider(reservationId, newTourDate, reservationNum, serviceProviders, coordinatorMail, oldActivityDay, itiniraryGroupList, isForCancel);
                    return response;



                }

            }
            catch (Exception ex)
            {
                Log.Error($"{DateTime.Now} HandleEventItemsSchedule Get scheduled event item from crm exception, reservationId:{reservationId}: " + ex.Message);
                response.IsOk = false;
                response.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                return response;
            }
            return response;
        }
        private async Task<CrmResultItem> SendMailToYadVashemLogs(string reservationId, DateTime newTourDate, string reservationNum,
            Dictionary<Guid, string> serviceProviders, string coordinatorMail, string oldActivityDay, List<Entity> itiniraryGroupList, bool isForCancel)
        {
            CrmResultItem response = new CrmResultItem(true, "");
            var bcc = _config.GetValue<string>("bcc");
            string languageJson = "he";
            JObject json = GetTranslateJson();
            if (!json.HasValues)
            {
                response.IsOk = false;
                response.ErrorMessageStatus = ErrorMessageStatus.SEND_MAIL_YAD_VASHEM;
                return response;
            }
            string subject = json[languageJson]["emailToYadVashemLogsWhenCancelReservation"]["subject"].ToString()
          .Replace("{reservationNum}", reservationNum)
          .Replace("{newTourDate}", newTourDate.ToString("dd/MM/yyyy"));

            string messageTemplate = json[languageJson]["emailToYadVashemLogsWhenCancelReservation"]["message"].ToString();

            // Determine values for conditionals
            string serviceProvidersSeparator = serviceProviders.Count > 0 ? ":" : ".";
            //string serviceProvidersList = serviceProviders.Count > 0 ? string.Join(", ", serviceProviders) : "";
            List<string> names = serviceProviders.Values.ToList();

            // Group names in sets of three
            List<string> groupedNames = new List<string>();
            for (int i = 0; i < names.Count; i += 3)
            {
                var namesGroup = names.Skip(i).Take(3);
                groupedNames.Add(string.Join(", ", namesGroup));
            }

            // Final formatted string
            string serviceProvidersList = string.Join("\n", groupedNames);
            string serviceProvidersNotification = serviceProviders.Count > 0 ? "יש לעדכן אותם על הביטול<br>" : "";

            // Replace placeholders
            string message = messageTemplate
                .Replace("{reservationNum}", reservationNum)
                .Replace("{newTourDate}", newTourDate.ToString("dd/MM/yyyy"))
                .Replace("{serviceProvidersCount}", serviceProviders.Count.ToString())
                .Replace("{serviceProvidersSeparator}", serviceProvidersSeparator)
                .Replace("{serviceProvidersList}", serviceProvidersList)
                .Replace("{serviceProvidersNotification}", serviceProvidersNotification);

            List<string> toToSend = new List<string>() { bcc };
            List<string> bccToSend = new List<string>() { bcc };
            List<string> ccToSend = new List<string>() { };
            Direction direction = Direction.RTL;
            if (serviceProviders.Count > 0)
            {
                // bccToSend.Add(EducationTrainingDepartmentEmail);
            }
            // Fetch email lists from configuration
            var serviceProvidersEmails = _config.GetSection("serviceProvidersSendEmail").Get<List<string>>();
            var coordinatorExistsEmails = _config.GetSection("itiniraryGroupListSendEmailCoordinatorMailExist").Get<List<string>>();
            var coordinatorNotExistsEmails = _config.GetSection("itiniraryGroupListSendEmaiCoordinatorMailNotExist").Get<List<string>>();
            coordinatorExistsEmails.Add(coordinatorMail);

            if (serviceProviders.Count > 0)
            {

                if (serviceProvidersEmails != null)
                {
                    toToSend.AddRange(serviceProvidersEmails);
                }

                if (!string.IsNullOrEmpty(coordinatorMail))
                {
                    toToSend.Add(coordinatorMail); // Add coordinator email if exists
                }
            }
            else if (itiniraryGroupList.Count > 0) // Only itinerary items, no service providers
            {
                if (!string.IsNullOrEmpty(coordinatorMail))
                {
                    // Add emails when coordinator exists
                    if (coordinatorExistsEmails != null)
                    {
                        toToSend.AddRange(coordinatorExistsEmails);
                    }
                }
                else
                {
                    // Add emails when coordinator does not exist
                    if (coordinatorNotExistsEmails != null)
                    {
                        toToSend.AddRange(coordinatorNotExistsEmails);
                    }
                }
            }

            response = await sendEmailGenericCancel(toToSend, bccToSend, direction, subject, message, ccToSend);
            return response;

        }

        private string GetActivityDayFetchXml(Guid reservationId)
        {
            string fetchXml = "";
            fetchXml += _newReservationRepository.StartFetchXml("new_activity_day");
            fetchXml += $@"
            <attribute name='new_activity_dayid' />
            <attribute name='new_activity_day_status' />
            <attribute name='new_reservation' />
            <filter>
            <condition attribute='new_reservation' operator='eq' value='{reservationId}' />
            <condition attribute='statecode' operator='eq' value='0' />
            </filter>
            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" alias=""TourType"">
                <attribute  name=""new_name"" />
                 <attribute  name=""new_name_en"" />
                 <attribute  name=""new_tour_code"" />
            </link-entity>
        ";

            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }

        private string GetTourTypeEntityFromActivityDay(Guid activityDayId)
        {
            string fetchXml = "";
            fetchXml += _newReservationRepository.StartFetchXml("new_activity_day");
            fetchXml += $@"
            <attribute name=""new_name""/>
            <attribute name=""new_activity_day_type""/>
            <attribute name=""createdby""/>
            <attribute name=""createdon""/>
            <attribute name=""new_activity_dayid""/>
            <order attribute=""new_name"" descending=""false""/>
            <filter type=""and"">
                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                 <condition attribute=""new_activity_dayid"" operator=""eq"" value=""{activityDayId}""/>
            </filter>
            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" link-type=""outer""   alias=""TourType"" >
                <attribute name=""new_days_to_cancel""/>    
            </link-entity>

            


        ";

            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }
        private string GetItiniraryListByActivityDayAndResource(string activityDayId, string resourceId)
        {
            string fetchXml = "";
            fetchXml += _newReservationRepository.StartFetchXml("new_itinerary_group");
            fetchXml += $@"
    
                <attribute name='new_group_number' />
                <attribute name='new_number_of_participants' />
                <filter>
                    <condition attribute='new_activity_day' operator='eq' value='{activityDayId}' />
                    <condition attribute='statecode' operator='eq' value='0' />
                </filter>
                <link-entity name='new_new_itinerary_item_new_itinerary_group' from='new_itinerary_groupid' to='new_itinerary_groupid' link-type='inner'>
                    <link-entity name='new_itinerary_item' from='new_itinerary_itemid' to='new_itinerary_itemid' link-type='inner'>
                        <attribute name='new_itinerary_itemid' />
                         <link-entity name=""new_event_item"" from=""new_event_itemid"" to=""new_scheduler_resource"" link-type='outer'>
                            <attribute name='new_event_itemid' />
                            <attribute name='new_start_datetime' />
                            <attribute name='new_resource' />
                            <filter>
                                <condition attribute='statecode' operator='eq' value='0' />
                                {(string.IsNullOrEmpty(resourceId) ? "" : $"<condition attribute='new_resource' operator='eq' value='{resourceId}' />")}
                            </filter>
                        </link-entity>
                    </link-entity>
                </link-entity>";
            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }
        public string GetReservationFetchXml(string reservationId)
        {
            string fetchXml = "";
            fetchXml += _newReservationRepository.StartFetchXml("new_reservation");
            fetchXml += $@"
            <attribute name='new_num' />
            <attribute name='new_activity_day_first_date' />
            <filter>
                <condition attribute='new_reservationid' operator='eq' value='{reservationId}' />
            </filter>
            <link-entity name='new_service_provider' from='new_service_providerid' to='new_yv_coordinator' link-type='outer' alias='Coordinator'>
                <attribute name='new_email' />
            </link-entity>";
            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }

        public string getXmlHandleEventItemsSchedule(string reservationId, string resourceId)
        {

            string fetchXml = "";
            fetchXml += _newReservationRepository.StartFetchXml("new_event_item");
            fetchXml += $@"
              <attribute name='new_resource' />
              <attribute name='new_service_provider' />
              <filter>
                <condition attribute='statecode' operator='eq' value='0' />
                <condition attribute='new_reservation' operator='eq' value='{reservationId}' />
              </filter>
              <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' link-type='inner' alias='Reservation'>
                <attribute name='new_num' />
                <attribute name='new_activity_day_first_date'/>
                <link-entity name='new_service_provider' from='new_service_providerid' to='new_yv_coordinator' link-type='outer' alias='Coordinator'>
                  <attribute name='new_email' />
                </link-entity>
              </link-entity>
            ";

            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }
        public CrmResultItem IsEventRegistrationCanBeChanged(DateTime tourDate, int reservationDaysToCancel)
        {
            if (tourDate <= DateTime.Now.ToLocalTime())
            {
                return new CrmResultItem(false, ErrorMessageStatus.DATE_PASSED);
            }
            ;
            if (reservationDaysToCancel > 0)
            {
                if (!_onlineReservationRepository.IsTourDateoHasEnoughWorkingDays(tourDate, reservationDaysToCancel))
                {
                    return new CrmResultItem(false, ErrorMessageStatus.DATE_CLOSE);//to mutch close to tour date
                }
            }
            else
            {

                if (!_onlineReservationRepository.IsTourDateoHasEnoughWorkingDays(tourDate, 3))
                {
                    return new CrmResultItem(false, ErrorMessageStatus.DATE_CLOSE);//to mutch close to tour date
                }

            }
            return new CrmResultItem(true);
        }
        public CrmResultItem IsReservationCanBeChanged(DateTime tourDate, Entity reservationEntity, string resourceId, EntityCollection bidList, int reservationDaysToCancel, bool isForCancel = false)
        {
            try
            {
                var reservationId = reservationEntity.Id.ToString();

                if (!isForCancel)
                {
                    var reservationStatus = reservationEntity.GetAttributeValue<OptionSetValue>("new_reservation_status");
                    var activityDayStatus = (OptionSetValue)((AliasedValue)reservationEntity["ActivityDay.new_activity_day_status"]).Value;

                    if (reservationStatus.Value == (int)ReservationStatus.CANCELLED || activityDayStatus.Value == (int)ReservationStatus.CANCELLED)
                    {
                        return new CrmResultItem(false, ErrorMessageStatus.CANCELLED);
                    }
                    if (reservationEntity.GetAttributeValue<OptionSetValue>("new_reservation_source").Value != 1)
                    {//if reservation resource is not from the internet not alowed to edit
                        return new CrmResultItem(false, ErrorMessageStatus.CREATED_NOT_ONLINE);

                    }

                }
                var responsibleAgent = reservationEntity.GetAttributeValue<EntityReference>("new_responsible_agent");
                if (responsibleAgent != null && responsibleAgent.Id.ToString()
                == _config.GetValue<string>("OfficialVisitsResponsibleAgentGuid"))
                {

                    return new CrmResultItem(false, ErrorMessageStatus.OFFICIAL_VISIT);
                }
                if (IsReservationCanBeEditUntilTourDate(bidList))
                {
                    if (tourDate <= DateTime.Now.ToLocalTime())
                    {

                        return new CrmResultItem(false, ErrorMessageStatus.DATE_PASSED);
                    }
                    ;
                }
                else
                {
                    if (tourDate <= DateTime.Now.ToLocalTime())
                    {
                        return new CrmResultItem(false, ErrorMessageStatus.DATE_PASSED);
                    }
                    ;
                    if (reservationDaysToCancel > 0)
                    {
                        if (!_onlineReservationRepository.IsTourDateoHasEnoughWorkingDays(tourDate, reservationDaysToCancel))
                        {
                            return new CrmResultItem(false, ErrorMessageStatus.DATE_CLOSE);//to mutch close to tour date
                        }
                    }
                    else
                    {
                        if (IsReservationCanBeEditUntilOneDayBeforeTourDate(bidList))
                        {
                            if (!_onlineReservationRepository.IsTourDateoHasEnoughWorkingDays(tourDate, 1))
                            {
                                return new CrmResultItem(false, ErrorMessageStatus.DATE_CLOSE);//to mutch close to tour date
                            }
                        }
                        else
                        {
                            if (!_onlineReservationRepository.IsTourDateoHasEnoughWorkingDays(tourDate, 2))
                            {
                                return new CrmResultItem(false, ErrorMessageStatus.DATE_CLOSE);///to mutch close to tour date
                            }
                            // var scheduledServiceProviders = GetScheduledServiceProvidersForOptionalItiniraryItems(reservationId, resourceId);
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                string str = "Exception: " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace;
                Log.Error(DateTime.Now + "IsReservationCanBeEdit - " + str);
                return new CrmResultItem(false, ErrorMessageStatus.UNKNOWN);
            }

            return new CrmResultItem(true);
        }


        public bool IsReservationCanBeEditUntilTourDate(EntityCollection bidList)
        {

            var result = bidList.Entities.Count == 0 || (bidList.Entities.Count == 1 && IsReservationContainsRegistrationAndHandlingFees(bidList));
            return bidList.Entities.Count == 0 || (bidList.Entities.Count == 1 && IsReservationContainsRegistrationAndHandlingFees(bidList));
        }
        public bool IsReservationContainsRegistrationAndHandlingFees(EntityCollection bidList)
        {
            var bid = bidList.Entities.ToList().Find(entity =>
            {
                var product = entity.GetAttributeValue<EntityReference>("new_yv_product");
                if (product != null && product.Id.ToString() == _config.GetValue<string>("RegistrationAndHandlingFees").ToLower())

                {
                    return true;
                }
                return false;
            });
            return bid != null;
        }
        public int GetReservationContainsRegistrationAndHandlingFeesPrice(EntityCollection bidList, bool isEventRegistration = false)
        {
            var price = 0;
            var productIds = _config.GetSection("listOfProductDidnwtGiveBackMoneyInCancel").Get<List<string>>();
            foreach (var entity in bidList.Entities)
            {
                if (isEventRegistration)

                {
                    if (entity.GetAttributeValue<AliasedValue>("product.new_yv_productid") != null)
                    {
                        var product = entity.GetAttributeValue<AliasedValue>("product.new_yv_productid")?.Value;

                        // Ensure product ID is checked in uppercase
                        if (product != null && productIds.Contains(product.ToString().ToUpper(), StringComparer.OrdinalIgnoreCase))
                        {

                            var aliasedTotalPrice = entity.GetAttributeValue<AliasedValue>("BidItem.new_total_price_after_discount_base");

                            if (aliasedTotalPrice != null && aliasedTotalPrice.Value is Money moneyValue)
                            {
                                price += (int)moneyValue.Value; // Converts Money value to integer
                            }
                        }
                    }



                }
                else
                {
                    var product = entity.GetAttributeValue<EntityReference>("new_yv_product");
                    if (product != null && productIds != null && productIds.Contains(product.Id.ToString(), StringComparer.OrdinalIgnoreCase))
                    {
                        var totalPrice = entity.GetAttributeValue<Money>("new_total_price_after_discount_base");
                        if (totalPrice != null)
                        {
                            price += (int)totalPrice.Value;
                        }
                    }
                }


            }

            return price;


        }
        public bool IsReservationCanBeEditUntilOneDayBeforeTourDate(EntityCollection bidList)
        {
            var productWithServiceProviderRequired = bidList.Entities.ToList().Find(entity =>
            {
                return (bool)((AliasedValue)entity["product.new_is_required_service_provider"]).Value;
            });

            return productWithServiceProviderRequired == null;
        }
        private Entity GetFullResource(Guid resourceGuid)
        {
            var resourceEntity = _crmService.Retrieve("new_resource", resourceGuid, true);
            return resourceEntity;
        }

        public Entity GetTourByCode(string new_tour_code)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType");

                fetchXml += @$"
              <attribute name=""new_resource"" />
              <attribute name=""new_name"" />
              <attribute name=""new_name_en"" />
              <attribute name=""new_budget_framework"" />
              <attribute name=""new_resource"" />
             <attribute name=""new_duration_of_hours"" />
             <attribute name=""new_enable_hours_by_default"" />
              <attribute name=""new_opening_hours_by_museum"" />
              <attribute name=""new_max_participants"" />
              <attribute name=""new_division_router"" />
              <attribute name=""new_activity_day_location"" />
              <attribute name=""new_yv_product"" />
              <attribute name=""new_yv_product_event"" />
              <attribute name=""new_tour_typeid"" />
              <attribute name=""new_difference_between_arrival_to_start""/>
              <attribute name=""new_rsv_department"" />
              <attribute name=""new_event_reservations"" />
              <attribute name=""new_age_group"" />
              <attribute name=""new_gefen_type"" />
              <attribute name=""new_budget_framework"" />
              <attribute name=""new_additional_characterization"" />
              <attribute name=""new_duration_of_hours_resource"" />
              <attribute name=""new_max_products_per_day"" />
              <attribute name=""new_almog_interface_id"" />
              <attribute name=""new_category_individuals"" />
              <attribute name=""new_tour_type_tahel"" />
              <attribute name=""new_is_need_headset"" />
              <attribute name=""new_is_need_event""/>
              <attribute name=""new_is_guide_mandatory_per_group"" />
              <attribute name=""new_max_participants"" />
              <attribute name=""new_is_need_hours_regulation"" />
              <attribute name=""new_is_need_guide_hours_regulation"" />
              <attribute name=""new_target_population"" />
              <link-entity name=""new_budget_framework"" to=""new_budget_framework"" from=""new_budget_frameworkid"" link-type=""outer"" alias=""BudgetFramework"">
                 <attribute name=""new_email"" />
                 <attribute name=""new_fax"" />
                 <attribute name=""new_telephone"" />
                 <attribute name=""new_name"" />
                 <attribute name=""new_rsv_department"" />
                 <link-entity name=""new_almog_interface"" to=""new_almog_interface"" from=""new_almog_interfaceid"" link-type=""outer"" alias=""Almog"">
                    <attribute name=""new_almog_sub_code"" />
                 </link-entity>
                 <link-entity name=""new_rsv_department"" to=""new_rsv_department"" from=""new_rsv_departmentid"" link-type=""outer"" alias=""Department"">
                    <attribute name=""new_name"" />
                    <attribute name=""new_en_name"" />
                    <attribute name=""new_rsv_departmentid"" />
                 </link-entity>
              </link-entity>
                <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" link-type=""outer"" alias=""eventReservation"">
                     <attribute name=""new_max_participants_per_event"" />
                </link-entity>";

                fetchXml += @$"<filter type=""and"">
                 <condition attribute=""new_tour_code"" operator=""eq"" value='{new_tour_code}' />
                 <condition attribute=""statecode"" operator=""eq"" value=""0"" />
              </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var res = _crmService.RetrieveMultiple(query);
                if (res.Entities.Count > 0)
                {
                    return res.Entities.First();
                }
                return new Entity();
            }
            catch (Exception ex)
            {
                Log.Error($"GetTourByCode error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private OnlineReservation GetOnlineReservation(Guid reservationGuid, bool withPayment = false)
        {
            Log.Information($"GetOnlineReservation start reservationGuid: " + reservationGuid);

            try
            {
                var entity = _onlineReservationRepository.GetOnlineReservationEntity(reservationGuid);
                var onlineReservation = _onlineReservationRepository.GetOnlineReservation(entity);
                var activityDay = GetActivityDate(onlineReservation.Guid);
                var startTime = activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime();
                onlineReservation.EndTime = activityDay.GetAttributeValue<DateTime>("new_end_datetime").ToLocalTime();
                onlineReservation.StartTime = new Time();
                if (startTime != null)
                {
                    onlineReservation.StartTime.hours = startTime.Hour;
                    onlineReservation.StartTime.minutes = startTime.Minute;

                }
                List<Condition> conditionList = new List<Condition>();
                Condition bidItemCondition = new Condition("new_activity_day", "eq", activityDay.Id.ToString());
                conditionList.Add(bidItemCondition);
                var bidItem = GetBidItems(conditionList, onlineReservation.FormLang);
                onlineReservation.BidItems = bidItem;
                if (onlineReservation.TourType.BasicDetails.isPackage && withPayment)//עבור השורות תשלום וממשק לאלמוג
                {
                    var tourCode = onlineReservation.TourType.BasicDetails.TourCode;
                    onlineReservation.TourType.TourTypesRequiredList = GetTourTypesByTourType(tourCode, "he");
                    onlineReservation.TourType.TourTypesRequiredList.ForEach(tour =>
                    {
                        FilterForProduct filter = new FilterForProduct();
                        filter.isPackage = true;
                        filter.tourCode = tour.BasicDetails.TourCode;
                        filter.amount = onlineReservation.NumberOfVisitors;
                        filter.tourDate = onlineReservation.ReservationDate.Value;
                        filter.language = "he";
                        tour.additionalProductList = GetAdditionalProductList(filter);
                        var productList = GetRequireProductsList(tour.BasicDetails.TourCode, "product_required", "he");
                        if (productList?.Count > 0)
                        {
                            tour.AdditionalTourDetails = new();
                            tour.AdditionalTourDetails.reqiredProductList = productList;

                        }
                    });

                }
                return onlineReservation;
            }
            catch (Exception ex)
            {
                Log.Error($"GetOnlineReservation error: {ex.Message}");
                throw new Exception(ex.Message);
            }
            //< link - entity name = ""new_budget_framework"" from = ""new_budget_frameworkid"" to = ""new_budget_framework"" visible = ""false"" link - type = ""outer"" alias = ""a_82d9dff25b51ea1180d3005056911b09"" >
            //     < attribute name = ""new_rsv_department"" />
            //     </ link - entity > ";
        }

        public Entity GetActivityDate(string reservaionGuid, bool isForEdit = false)
        {
            try
            {
                var fetchXml = @$"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                <entity name=""new_activity_day"">
                                <attribute name=""new_name""/>
                                <attribute name=""new_activity_day_type""/>
                                <attribute name=""new_reservation""/>
                                <attribute name=""new_activity_dayid""/>
                                <attribute name=""new_start_datetime""/>
                                <attribute name=""new_end_datetime""/>
                                <order attribute=""new_name"" descending=""false""/>
                                <filter type=""and"">
                                <condition attribute=""new_reservation"" operator=""eq""  value=""{reservaionGuid}""/>
                                </filter>";
                fetchXml += $@"";
                fetchXml += $@" </entity>
                                </fetch>";
                var query = new FetchExpression(fetchXml);
                return _crmService.RetrieveMultiple(query)[0];
            }
            catch (Exception ex)
            {
                Log.Error($"Get Reservation-activity day error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public List<BidItem> GetProductsToShow(List<Condition> conditions, string lang)
        {
            try
            {
                var fetchXmlBidItems = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                    <entity name=""new_bid"">
                                    <attribute name=""new_name""/>
                                    <attribute name=""createdon""/>
                                    <attribute name=""new_language""/>
                                    <attribute name=""new_discount""/>
                                    <attribute name=""new_discount_type""/>
                                    <attribute name=""new_total_price_after_discount""/>
                                    <attribute name=""new_total_price""/>
                                    <attribute name=""new_price""/>
                                    <attribute name=""new_yv_product""/>
                                    <attribute name=""new_amount""/>
                                    <attribute name=""new_bid_item_number""/>
                                    <attribute name=""new_total_price_after_discount_base""/>
                                    <attribute name=""new_price_base""/>
                                    <attribute name=""new_comments""/>
                                    <attribute name=""new_ticket_type""/>
                                    <attribute name=""new_bidid""/>
                                    <attribute name=""new_product_almog_sku""/>
                                    <order attribute=""new_name"" descending=""false""/>
                                    <filter type=""and"">";
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (string.IsNullOrEmpty(conditions[i].Value))
                    {
                        fetchXmlBidItems += $@"<condition attribute=""{conditions[i].Name}"" operator=""{conditions[i].Operator}""/>";

                    }
                    else
                    {
                        fetchXmlBidItems += $@"<condition attribute=""{conditions[i].Name}"" operator=""{conditions[i].Operator}"" value=""{conditions[i].Value}""/>";
                        fetchXmlBidItems += $@"<condition attribute=""statecode"" operator=""eq"" value=""0""/>";

                    }
                }

                fetchXmlBidItems += $@" </filter>
                                    <link-entity name=""new_yv_product"" from=""new_yv_productid"" to=""new_yv_product"" alias=""product"">
                                    <attribute name=""new_name""/>
                                    <attribute name=""new_name_en""/>
                                    <attribute name=""new_display_on_counter""/>
                                   <filter type=""and"">
                                         <condition attribute=""new_display_on_counter"" operator=""eq"" value=""1"" />
                                    </filter>
                                    </link-entity>
                                    </entity>
                                    </fetch>";

                // <filter type=""and"">
                //  < condition attribute = ""new_display_on_counter"" operator= ""eq"" value = ""1"" />
                // </ filter >
                List<BidItem> bids = _onlineReservationRepository.GetBidItemList(fetchXmlBidItems, lang);
                return bids;
            }
            catch (Exception ex)
            {
                Log.Error($"GetReservation BidItems error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<BidItem> GetBidItems(List<Condition> conditions, string lang)
        {
            try
            {
                var fetchXmlBidItems = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                    <entity name=""new_bid"">
                                    <attribute name=""new_name""/>
                                    <attribute name=""createdon""/>
                                    <attribute name=""new_language""/>
                                    <attribute name=""new_discount""/>
                                    <attribute name=""new_discount_type""/>
                                    <attribute name=""new_total_price_after_discount""/>
                                    <attribute name=""new_total_price""/>
                                    <attribute name=""new_price""/>
                                    <attribute name=""new_yv_product""/>
                                    <attribute name=""new_amount""/>
                                    <attribute name=""new_bid_item_number""/>
                                    <attribute name=""new_total_price_after_discount_base""/>
                                    <attribute name=""new_price_base""/>
                                    <attribute name=""new_comments""/>
                                    <attribute name=""new_ticket_type""/>
                                    <attribute name=""new_bidid""/>
                                    <attribute name=""new_product_almog_sku""/>
                                    <order attribute=""new_name"" descending=""false""/>
                                    <filter type=""and"">";
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (string.IsNullOrEmpty(conditions[i].Value))
                    {
                        fetchXmlBidItems += $@"<condition attribute=""{conditions[i].Name}"" operator=""{conditions[i].Operator}""/>";

                    }
                    else
                    {
                        fetchXmlBidItems += $@"<condition attribute=""{conditions[i].Name}"" operator=""{conditions[i].Operator}"" value=""{conditions[i].Value}""/>";

                    }
                }

                fetchXmlBidItems += $@" </filter>
                                    <link-entity name=""new_yv_product"" from=""new_yv_productid"" to=""new_yv_product"" alias=""product"">
                                    <attribute name=""new_name""/>
                                    <attribute name=""new_name_en""/>
                                    <attribute name=""new_display_on_counter""/>
                                    </link-entity>
                                    </entity>
                                    </fetch>";
                List<BidItem> bids = _onlineReservationRepository.GetBidItemList(fetchXmlBidItems, lang);
                return bids;
            }
            catch (Exception ex)
            {
                Log.Error($"GetReservation BidItems error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public Guid SaveResourceSlotTime(BasicTourDetails tourDetails)
        {
            try
            {
                Log.Information($"\n------------- new scheduleResource in : {DateTime.Now} -------------\n");
                Log.Information($"SaveResourceSlotTime start tourDetails:{tourDetails.StartTime}, {tourDetails.Date}, NumberOfVisitors:{tourDetails.NumberOfVisitors}.");
                tourDetails.StartDateTime = DateTime.ParseExact(tourDetails.Date + tourDetails.StartTime, "dd/MM/yyyyHH:mm", null);
                var tourType = GetTourByCode(tourDetails.TourCode);
                var resourceGuid = tourType.GetAttributeValue<EntityReference>("new_resource").Id;
                if (_newReservationService.IsThereAvailablePlacesInResourceAtSlotTime(tourDetails, resourceGuid))
                {
                    bool isForUpdate = false;
                    Guid tryParseGuid = new Guid();
                    if (!string.IsNullOrEmpty(tourDetails.ScheduleEventGuid) && Guid.TryParse(tourDetails.ScheduleEventGuid, out tryParseGuid))
                    {
                        isForUpdate = true;
                        Log.Information($"SaveResourceSlotTime current schedule is for update:{tourDetails.ScheduleEventGuid}");
                    }
                    var scheduleResourceGuid = _newReservationRepository.CreateOrUpdateResourceScheduleEvent(tourDetails, resourceGuid, tourType, isForUpdate);
                    Log.Information($"SaveResourceSlotTime end, scheduleResourceGuid is {scheduleResourceGuid}");
                    return scheduleResourceGuid;
                }
                throw new Exception("NO_ROOM");
            }
            catch (Exception ex)
            {
                Log.Error($"SaveResourceSlotTime error, {ex.Message}.");
                throw new Exception(ex.Message);
            }
        }

        private ReservationAttachment createAtthachment(OnlineReservation reservation)
        {
            try
            {
                Log.Information($"createAtthachment start, reservation: {JsonConvert.SerializeObject(reservation)}");
                ReservationAttachment attachment = new ReservationAttachment();
                if (reservation.GuideLanguage.Value.Key != null)
                {
                    var langEntity = _crmService.Retrieve("new_language", new Guid(reservation.GuideLanguage.Value.Key), true);
                    attachment.VisitLangEn = langEntity.GetAttributeValue<string>("new_en_name");
                    attachment.VisitLang = langEntity.GetAttributeValue<string>("new_name");
                }
                else
                {
                    attachment.VisitLang = reservation.FormLang;

                }
                attachment.ReservationName = reservation.ReservationName;
                attachment.DocLang = reservation.FormLang;
                attachment.Total = reservation.TotalPriceBeforeDiscount.ToString();
                attachment.TotalPriceAfterDiscount = reservation.TotalPriceAfterDiscount.ToString();
                attachment.Discount = reservation.DiscountAmount.ToString();
                if (reservation.DiscountType?.Value != null)
                {
                    attachment.DiscountType = reservation.DiscountType.Value.Value.ToString();

                }
                attachment.OrderNum = reservation.ReservationNumber != null ? reservation.ReservationNumber.ToString() : "";
                attachment.GroupName = reservation.groupName != null ? reservation.groupName : "";
                attachment.GefenType = reservation.GefenType;
                attachment.tourName = reservation.TourType.BasicDetails.Name;
                //contact
                attachment.ContactEmail = reservation.Email;
                attachment.ContactPhone = reservation.Phone;
                attachment.ContactName = reservation.FirstName + " " + reservation.LastName;
                //institution
                attachment.InstitutionNameOnly = reservation.Institution.BranchName;
                attachment.InstitutionCompanyNumberOrSymbol = reservation.Institution.BrancheSymbol;
                attachment.InstitutionName = reservation.Institution.BranchName + " " + reservation.Institution.BrancheSymbol;
                attachment.InstitutionAddress = reservation.Institution.BranchAddress;
                //visitors
                attachment.Grade = reservation.Grade.Value;
                attachment.NumberOfParticipants = reservation.NumberOfVisitors.ToString();
                attachment.NumberOfGroups = reservation.GroupsList?.Count.ToString();
                //activity day
                attachment.ActivityDayList = new List<ActivityDay>();
                ActivityDay activityDay = new ActivityDay();
                activityDay.Location = "יד ושם";
                activityDay.LocationEn = "yad vashem";
                if (reservation.ReservationDate != null)
                {
                    activityDay.Date = convertDateTimeToString(reservation.ReservationDate);
                }
                else
                {
                    activityDay.Date = null;
                }
                activityDay.ParticipantsNum = reservation.NumberOfVisitors.ToString();
                activityDay.NumberOfGroups = reservation.GroupsList.Count.ToString();
                var startTimeString = reservation.StartTime.hours.ToString("D2") + ":" + reservation.StartTime.minutes.ToString("D2");
                activityDay.HouresFromTo = startTimeString + " - " + reservation.EndTime.Value.ToString("HH:mm");
                //activityDay.MuseumEntryTime =

                activityDay.detailsList = new List<Details>();
                var totalAfterDiscount = 0;
                var total = 0;
                reservation.BidItems?.ForEach(bidItem =>
                {
                    Details details = new Details();
                    details.Amount = bidItem.Amount.ToString();
                    details.Total = bidItem.TotalPriceBeforeDiscount.ToString();
                    details.ItemName = bidItem.Product.Value;
                    details.itemNameEn = bidItem.Product.Value;
                    details.Comments = bidItem.Comments != null ? bidItem.Comments : "";
                    details.Discount = bidItem.DiscountAmount.ToString();
                    details.DiscountType = bidItem.DiscountType.Key.ToString();
                    details.PricePerUnit = bidItem.Price.ToString();
                    //1=סכום
                    //2=אחוז
                    details.TotalAfterDiscount = bidItem.TotalPriceAfterDiscount.ToString();
                    totalAfterDiscount += (int)(bidItem.TotalPriceAfterDiscount);
                    total += bidItem.TotalPriceBeforeDiscount;
                    activityDay.detailsList.Add(details);
                }
                );
                activityDay.Total = total.ToString();
                activityDay.TotalAfterDiscount = totalAfterDiscount.ToString();
                attachment.ActivityDayList.Add(activityDay);
                //department
                attachment.Department = reservation.department.name;
                attachment.DepartmentEmail = reservation.department.email;
                attachment.DepartmentEn = reservation.department.nameEn;
                attachment.DepartmentFax = reservation.department.fax;
                attachment.DepartmentPhone = reservation.department.telephone;

                return attachment;
            }
            catch (Exception ex)
            {
                Log.Error($"createAtthachment error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private Entity getEventRegistration(string eventRegistartionId)
        {
            try
            {
                string fetchXml = @$"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                    <entity name=""new_event_registration"">
                    <attribute name=""new_name""/>
                    <attribute name=""createdon""/>
                    <attribute name=""new_role_in_school""/>
                    <attribute name=""new_age_group""/>
                    <attribute name=""new_teaching_subject""/>
                    <attribute name=""new_number_of_participants""/>
                    <attribute name=""new_school_name""/>
                    <attribute name=""new_contact""/>
                    <attribute name=""new_event_reservations""/>
                    <attribute name=""new_payment_status""/>
                    <attribute name=""new_division_router""/>
                    <attribute name=""new_num""/>
                    <attribute name=""new_country""/>
                    <attribute name=""new_reference""/>
                    <attribute name=""new_dietary_needs_other""/>
                    <attribute name=""new_dietary_needs""/>
                    <attribute name=""new_email_contact""/>
                    <attribute name=""new_event_registrationid""/>
                    <attribute name=""new_total_price""/>
                    <attribute name=""new_total_price_after_discount""/>
                    <attribute name=""new_lastname_contact""/>
                    <attribute name=""new_firstname_contact""/>
                    <attribute name=""new_mobile_contact""/>
                    <attribute name=""new_total_price_after_discount_base""/>
                    <attribute name=""new_form_language""/>   
                    <attribute name=""new_discount""/>
                    <attribute name=""new_discount_type""/>

                    <order attribute=""createdon"" descending=""true""/>
                    <filter type=""and"">
                    <condition attribute=""new_event_registrationid"" operator=""eq""  uitype=""new_event_registration"" value=""{eventRegistartionId}""/>
                    </filter>
                    <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" alias=""event"" link-type='outer'>
                        <attribute name=""new_language""/>
                        <attribute name=""new_event_code""/>
                        <attribute name=""new_gefen_type""/>


                    </link-entity>
                    <link-entity name=""new_event_registration_activity_day"" from=""new_event_registration"" to=""new_event_registrationid"" alias=""ead"" link-type='outer'>
                    <attribute name=""new_activity_day""/>     
                    </link-entity>
                    </entity>
                    </fetch>";
                var query = new FetchExpression(fetchXml);
                var res = _crmService.RetrieveMultiple(query);
                if (res != null)
                {
                    return res[0];
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"getEventRegistration error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        private Entity GetTourByEvent(string eventId)
        {
            try
            {
                Log.Information($"GetTourByEvent start, eventId:{eventId}");
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType", false, true);
                fetchXml += @$" 
                 <attribute name=""new_name""/>שם בעברית
                 <attribute name=""new_name_en""/>שם באנגלית
                 <attribute name=""new_tour_code""/>קוד סיור 
                 <attribute name=""new_event_reservations""/>ארוע -אחודה 
                 <attribute name=""new_budget_framework""/>מסגרת תקציבית
                 <attribute name=""new_note_watcher_he""/>הערה לוואצר
                 <attribute name=""new_note_watcher_en""/>הערה לוואצר
                <filter type=""and"">
                <condition attribute=""new_event_reservations"" operator=""eq"" value='{eventId}'/>
                </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var tourEntity = _crmService.RetrieveMultiple(query)[0];
                return tourEntity;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTourByEvent error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        private ReservationAttachment createAtthachmentEventRegistration(string eventRegistrationId)
        {
            try
            {
                Log.Information($"createAtthachmentEventRegistration start, eventRegistrationId: {eventRegistrationId}");
                ReservationAttachment attachment = new ReservationAttachment();
                var eventRegistrationEntity = getEventRegistration(eventRegistrationId);
                if (eventRegistrationEntity != null)
                {
                    var aliasedLang = eventRegistrationEntity.GetAttributeValue<AliasedValue>("event.new_language");
                    if (aliasedLang?.Value is EntityReference langReference)
                    {
                        var visitLangGuid = langReference.Id;
                        var langEntity = _crmService.Retrieve("new_language", visitLangGuid, true);
                        attachment.VisitLang = langEntity.GetAttributeValue<string>("new_name");
                        attachment.VisitLangEn = langEntity.GetAttributeValue<string>("new_en_name");
                    }
                    attachment.DocLang = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_form_language")?.Name;
                    attachment.Total = attachment.TotalPriceAfterDiscount = eventRegistrationEntity.GetAttributeValue<Money>("new_total_price_after_discount").Value.ToString();
                    attachment.TotalPriceAfterDiscount = eventRegistrationEntity.GetAttributeValue<Money>("new_total_price_after_discount").Value.ToString();
                    attachment.Discount = eventRegistrationEntity.GetAttributeValue<decimal>("new_discount").ToString();
                    if (eventRegistrationEntity.GetAttributeValue<OptionSetValue>("new_discount_type")?.Value != null)
                    {
                        attachment.DiscountType = eventRegistrationEntity.GetAttributeValue<OptionSetValue>("new_discount_type")?.Value.ToString();

                    }

                    attachment.OrderNum = eventRegistrationEntity.GetAttributeValue<string>("new_num");
                    if (!string.IsNullOrEmpty(eventRegistrationEntity.GetAttributeValue<AliasedValue>("event.new_gefen_type")?.Value.ToString()))
                    {
                        attachment.GefenType = _onlineReservationRepository.GetOptionSetValueLabel("new_event_reservations", "new_gefen_type", (int)eventRegistrationEntity.GetAttributeValue<AliasedValue>("event.new_gefen_type")?.Value, _crmService);
                    }
                    ////contact
                    attachment.ContactEmail = eventRegistrationEntity.GetAttributeValue<string>("new_email_contact");
                    attachment.ContactPhone = eventRegistrationEntity.GetAttributeValue<string>("new_mobile_contact");
                    attachment.ContactName = eventRegistrationEntity.GetAttributeValue<string>("new_firstname_contact") + " " + eventRegistrationEntity.GetAttributeValue<string>("new_lastname_contact");
                    attachment.ReservationName = attachment.ContactName;
                    ////institution
                    attachment.InstitutionNameOnly = attachment.ContactName;
                    //attachment.InstitutionCompanyNumberOrSymbol = reservation.Institution.BrancheSymbol;
                    //attachment.InstitutionName = reservation.Institution.BranchName + " " + reservation.Institution.BrancheSymbol;
                    //attachment.InstitutionAddress = reservation.Institution.BranchAddress;
                    ////visitors
                    //attachment.Grade = reservation.Grade.Value;
                    attachment.NumberOfParticipants = eventRegistrationEntity.GetAttributeValue<int>("new_number_of_participants").ToString();
                    ////activity day
                    var aliasedValue = eventRegistrationEntity.GetAttributeValue<AliasedValue>("ead.new_activity_day");
                    if (aliasedValue?.Value is EntityReference entity)
                    {
                        Guid activityId = entity.Id; // ID של ה-Entity
                        string eventReservation = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_event_reservations").Id.ToString();
                        var activityDayEntity = _crmService.Retrieve("new_activity_day", activityId, true);
                        var tourTypeEntity = GetTourByEvent(eventReservation);
                        //var tourTypeGuid = activityDayEntity.GetAttributeValue<EntityReference>("new_activity_day_type").Id;
                        //var tourTypeEntity = _crmService.Retrieve("new_tour_type", tourTypeGuid, true);

                        attachment.NumberOfGroups = activityDayEntity.GetAttributeValue<int>("new_number_of_groups").ToString();
                        attachment.GroupName = activityDayEntity.GetAttributeValue<string>("new_group_name");
                        if (attachment.DocLang == "עברית")
                        {
                            attachment.tourName = tourTypeEntity.GetAttributeValue<string>("new_name");
                        }
                        else
                        {
                            attachment.tourName = tourTypeEntity.GetAttributeValue<string>("new_name_en");
                        }
                        attachment.ActivityDayList = new List<ActivityDay>();
                        ActivityDay activityDay = new ActivityDay();
                        activityDay.Location = activityDayEntity.GetAttributeValue<EntityReference>("new_activity_day_location").Name;
                        activityDay.LocationEn = "yad vashem";
                        activityDay.Date = convertDateTimeToString(activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime());
                        activityDay.ParticipantsNum = attachment.NumberOfParticipants;
                        activityDay.NumberOfGroups = activityDayEntity.GetAttributeValue<int>("new_number_of_groups").ToString();
                        var startTime = activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().ToString("HH:mm");
                        //startTime.hours = activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").Hour;
                        //startTime.minutes = activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").Minute;
                        var endTime = activityDayEntity.GetAttributeValue<DateTime>("new_end_datetime").ToLocalTime().ToString("HH:mm");
                        //endTime.hours =activityDayEntity.GetAttributeValue<DateTime>("new_end_datetime").Hour;
                        //endTime.minutes = activityDayEntity.GetAttributeValue<DateTime>("new_end_datetime").Minute;
                        activityDay.HouresFromTo = startTime + " - " + endTime;
                        activityDay.detailsList = new List<Details>();
                        var conditionsList = new List<Condition>();
                        conditionsList.Add(new Condition("new_event_registration", "eq", eventRegistrationId));
                        var bidItems = GetBidItems(conditionsList, attachment.DocLang);
                        var totalAfterDiscount = 0;
                        var total = 0;
                        bidItems?.ForEach(bidItem =>
                        {
                            Details details = new Details();
                            details.Amount = bidItem.Amount.ToString();
                            details.Total = bidItem.TotalPriceBeforeDiscount.ToString();
                            details.ItemName = bidItem.Product.Value;
                            details.itemNameEn = bidItem.Product.Value;
                            details.Comments = bidItem.Comments != null ? bidItem.Comments : "";
                            details.Discount = bidItem.DiscountAmount.ToString();
                            details.DiscountType = bidItem.DiscountType.Key.ToString();
                            details.PricePerUnit = bidItem.Price.ToString();
                            //1=סכום
                            //2=אחוז
                            details.TotalAfterDiscount = bidItem.TotalPriceAfterDiscount.ToString();
                            totalAfterDiscount += (int)bidItem.TotalPriceAfterDiscount;
                            total += bidItem.TotalPriceBeforeDiscount;
                            activityDay.detailsList.Add(details);
                        });
                        activityDay.Total = total.ToString();
                        activityDay.TotalAfterDiscount = totalAfterDiscount.ToString();
                        attachment.ActivityDayList.Add(activityDay);
                        ////department
                        var budgetFrameworkGuid = tourTypeEntity.GetAttributeValue<EntityReference>("new_budget_framework").Id;
                        var budgetFrameworkEntity = _crmService.Retrieve("new_budget_framework", budgetFrameworkGuid, true);
                        var departmentGuid = budgetFrameworkEntity.GetAttributeValue<EntityReference>("new_rsv_department").Id;
                        var departmentEntity = _crmService.Retrieve("new_rsv_department", departmentGuid, true);
                        attachment.Department = departmentEntity.GetAttributeValue<string>("new_name");
                        attachment.DepartmentEmail = departmentEntity.GetAttributeValue<string>("new_email");
                        attachment.DepartmentEn = departmentEntity.GetAttributeValue<string>("new_en_name");
                        attachment.DepartmentFax = departmentEntity.GetAttributeValue<string>("new_fax");
                        attachment.DepartmentPhone = departmentEntity.GetAttributeValue<string>("new_telephone");
                    }
                    else
                    {
                        Console.WriteLine("Value is not an Entity or is null.");
                    }
                }
                return attachment;
            }
            catch (Exception ex)
            {
                Log.Error($"createAtthachmentEventRegistration error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public async Task<PdfResponse> CreatePdfAttachment(ReservationAttachment request)
        {
            try
            {
                Log.Information($"CreatePdfAttachment  start, request: {JsonConvert.SerializeObject(request)}");
                var url = _config.GetValue<string>("pdfCreatorUrl");
                //ReservationAttachment request = createAtthachment(reservation);
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                //if (url != null)
                //{
                //    _httpClient.BaseAddress = new Uri(url);
                //}
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var attachment = JsonConvert.DeserializeObject<PdfResponse>(jsonResponse);
                    Log.Information($"pdf created {attachment?.FullPath}");
                    return attachment;
                }
                else
                {
                    Log.Error($"not success calling PDF creator. ");
                    throw new Exception($"Error in CreatePdfAttachment: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreatePdfAttachment Exception, {ex.Message}.");
                throw new Exception(ex.Message);
            }
        }
        public async Task<PdfResponse> CreateTicketAttachment(ReservationTicket request)
        {
            try
            {
                Log.Information($"CreateTicketAttachment start, request: {JsonConvert.SerializeObject(request)}");
                var url = _config.GetValue<string>("ticketCreatorUrl");

                if (string.IsNullOrEmpty(url))
                {
                    throw new InvalidOperationException("ticketCreatorUrl is not configured");
                }

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(new Uri(url), content);
                //Log.Information($"CreateTicketAttachment  start, request: {JsonConvert.SerializeObject(request)}");
                //Log.Information($"CreateTicketAttachment  start, request: {JsonConvert.SerializeObject(request)}");
                //var url = _config.GetValue<string>("ticketCreatorUrl");
                ////ReservationAttachment request = createAtthachment(reservation);
                //var json = JsonConvert.SerializeObject(request);
                //var content = new StringContent(json, Encoding.UTF8, "application/json");
                //if (url != null)
                //{
                //    _httpClient.BaseAddress = new Uri(url);
                //}
                //var response = await _httpClient.PostAsync("", content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var attachment = JsonConvert.DeserializeObject<PdfResponse>(jsonResponse);
                    Log.Information($"ticket created {attachment?.FullPath}");
                    return attachment;
                }
                else
                {
                    Log.Error($"not success calling ticket creator. ");
                    throw new Exception($"Error in CreateTicketAttachment: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateTicketAttachment Exception, {ex.Message}.");
                throw new Exception(ex.Message);
            }
        }
        public void UpdateReservationWithAttachment(Guid reservationGuid, Guid attachment)
        {
            try
            {
                Log.Information($"UpdateReservationWithAttachment start, reservationGuid:{reservationGuid}, attachment: {attachment}");
                var reservationEntity = new Entity("new_reservation", reservationGuid);
                reservationEntity["new_new_reservation_new_attachment"] = attachment;
                _crmService.Update(reservationEntity);
                Log.Information("UpdateReservationWithAttachment end");
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateReservationWithAttachment error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> confirmation(OnlineReservation rsv, Guid rsvGuid, string digit4 = "", string cardBrand = "",bool isEdited=false)
        {
            Log.Information($"in confirmation guid: , {rsvGuid}.");
            //2.update reservation to be active 
            //UpdateReservationToActive(rsvGuid);

            //4.create pdf
            ReservationAttachment request = createAtthachment(rsv);
            request.creditCard4Digit = digit4;
            request.creditCardBrand = cardBrand;
            var PdfRes = await CreatePdfAttachment(request);

            Log.Information($"after creating pdf");
            //6.add attachment to the reservation in crm
            if (!string.IsNullOrEmpty(PdfRes.FullPath))
            {
                Guid CrmAttachment = this.createAttachmentInCrm(PdfRes.FullPath, rsvGuid, true);
            }
            //create ticket
            Log.Information($"start  creating ticket");

            var reservationTicket = createReservationTicket(rsvGuid);
            reservationTicket.PaymentMethod = digit4;
            var ticketRes = await CreateTicketAttachment(reservationTicket);
            if (ticketRes.FullPath != null && ticketRes.FullPath != "")
            {
                Guid CrmAttachment = this.createAttachmentInCrm(ticketRes.FullPath, rsvGuid, true);
            }
            //create list of file path to attach in email
            List<string> filePathList = new List<string>();
            var attachmaneFolder = _config.GetValue<string>("attachmaneFolder");
            filePathList.Add(attachmaneFolder + PdfRes.Name);
            var ticketFolder = _config.GetValue<string>("ticketFolder");
            filePathList.Add(ticketFolder + ticketRes.Name);
            //5.send confirmaton mail with pdf attachment
            Log.Error($"go to send mail");
            confirmationDetails confirmationDetails = new confirmationDetails();
            confirmationDetails.email = rsv.Email;
            confirmationDetails.reservationDate = convertDateTimeToString(rsv.ReservationDate);
            confirmationDetails.arrivalTime = rsv.StartTime.timeToString();
            confirmationDetails.name = rsv.FirstName + " " + rsv.LastName;
            confirmationDetails.reservationNumber = rsv.ReservationNumber;
            confirmationDetails.tourName = rsv.TourType.BasicDetails.Name;
            confirmationDetails.agenName = rsv.Institution.BranchName;
            confirmationDetails.creditCard4Digit = digit4;
            confirmationDetails.creditCardBrand = cardBrand;
            confirmationDetails.isEvent = false;
            confirmationDetails.totalPriceAfterDiscount = rsv.TotalPriceAfterDiscount.ToString();
            confirmationDetails.isNeedEvent = rsv.TourType.BasicDetails.IsNeedEvent;
            confirmationDetails.isEdited = isEdited;
            if (rsv.department != null && rsv.department.name == "הדרכות חינוכיות")
            {
                confirmationDetails.sendBccSchool = true;
            }

            var mailRes = await sendEmail(confirmationDetails, rsv.FirstName, filePathList, rsv.FormLang);
            if (mailRes)
            {
                Log.Error($"send mail success");
                return true;
            }
            else
            {
                Log.Error($"send mail error");
                return false;
            }
        }

        public async Task<bool> sendEmail(confirmationDetails confirmationDetails, string name, List<string> filePathList, string lang, bool isCancel = false)

        {
            string editLinkConfig = _config.GetValue<string>("EditReservationLink");
            string cancelLinkConfig = _config.GetValue<string>("CancelReservationLink");
            string sub = " Yad Vashem - Reservation Number " + confirmationDetails.reservationNumber;
            if (lang == "עברית")
            {

                editLinkConfig = _config.GetValue<string>("EditReservationLink_he");
                cancelLinkConfig = _config.GetValue<string>("CancelReservationLink_he");
                sub = "יד ושם - הזמנה מספר " + confirmationDetails.reservationNumber;
                if (confirmationDetails.isEdited)
                {
                    sub += " עדכון הזמנה ";
                }

            }
            if (!isCancel)
            {
                if (!confirmationDetails.isEvent)
                {
                    confirmationDetails.editLink = editLinkConfig
                     .Replace("{reservationNumber}", confirmationDetails.reservationNumber)
                     .Replace("{email}", confirmationDetails.email)
                     .Replace("{tourDate}", confirmationDetails.reservationDate);

                }
                confirmationDetails.cancelLink = cancelLinkConfig
                  .Replace("{reservationNumber}", confirmationDetails.reservationNumber)
                  .Replace("{email}", confirmationDetails.email);
            }
            Log.Information($"in sendEmail ");
            string institutionEmail = "";
            try
            {
                if (confirmationDetails.isNeedEvent)
                {
                    institutionEmail = GetInstitutionEmailFromEventRegistration(confirmationDetails.reservationNumber);
                }
                else
                {
                    institutionEmail = GetInstitutionEmail(confirmationDetails.reservationNumber);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to get institution email: {ex.Message}");
            }



            try
            {

                var url = _config.GetValue<string>("sendMailUrl");
                var bcc = _config.GetValue<string>("bcc")!;
                var from = _config.GetValue<string>("noReplayYadVashemEmail")!;

                var mailInfo = new MailInfo
                ()
                {
                    To = !string.IsNullOrEmpty(institutionEmail)
                    ? new List<string>() { confirmationDetails.email, institutionEmail }
                    : new List<string>() { confirmationDetails.email },
                    Cc = new List<string>(),
                    Bcc = new List<string>() { bcc },
                    From = from,
                    Subject = sub,
                    Message = GenerateConfirmationMessage(confirmationDetails, name, lang),

                };


                if (filePathList.Count > 0)
                {
                    mailInfo.Files = filePathList;
                }
                else// לא אמור בכלל להגיע לכאן
                {
                    mailInfo.Files = new List<string>();
                }
                if (confirmationDetails.sendBccSchool == true)
                {
                    var bccSchool = _config.GetValue<string>("bccSchool")!;
                    mailInfo.Bcc.Add(bccSchool);
                }

                var json = JsonConvert.SerializeObject(mailInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpClient httpClientEmail = new HttpClient();
                if (url != null)
                {
                    httpClientEmail.BaseAddress = new Uri(url);
                }
                var response = await httpClientEmail.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var wasEmailSent = JsonConvert.DeserializeObject<string>(jsonResponse);
                    Log.Information($"wasEmailSent {wasEmailSent}");
                    return jsonResponse == string.Empty ? true : false;
                }
                else
                {
                    Log.Error($"error sending email");
                    throw new Exception($"error sending email: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"sending email Exception, {ex.Message}.");
                throw new Exception(ex.Message);
            }
        }

        public string GetInstitutionEmail(string reservationNumber)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_reservation");
            fetchXml += $@"
        <filter type='and'>
            <condition attribute='new_num' operator='eq' value='{reservationNumber}' />
            <condition attribute='new_rsv_institution' operator='not-null' />
        </filter>
        <link-entity name='new_rsv_institution' from='new_rsv_institutionid' to='new_rsv_institution' alias='Institution' link-type=""outer"">
            <attribute name='new_email' />
        </link-entity>";
            fetchXml += _newReservationRepository.EndFetchXml();
            var entity = _crmService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.FirstOrDefault();
            return entity?.GetAttributeValue<AliasedValue>("Institution.new_email")?.Value?.ToString() ?? string.Empty;
        }


        public string GetInstitutionEmailFromEventRegistration(string registrationNumber)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_event_registration");
            fetchXml += $@"
        <filter type='and'>
            <condition attribute='new_num' operator='eq' value='{registrationNumber}' />
            <condition attribute='new_rsv_institution' operator='not-null' />
        </filter>
        <link-entity name='new_rsv_institution' from='new_rsv_institutionid' to='new_rsv_institution' alias='Institution' link-type='outer'>
            <attribute name='new_email' />
        </link-entity>";
            fetchXml += _newReservationRepository.EndFetchXml();

            var entity = _crmService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.FirstOrDefault();

            return entity?.GetAttributeValue<AliasedValue>("Institution.new_email")?.Value?.ToString() ?? string.Empty;
        }




        public string GenerateConfirmationMessage(confirmationDetails confirmationDetails, string recipientName, string lang)
        {
            string mail = _config.GetValue<string>("confirmationMail");
            string phone = _config.GetValue<string>("confirmationPhone");
            string returnMessage = "";
            if (lang == "עברית")
            {
                returnMessage = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;' dir='rtl'>
                    <p>שלום {recipientName},</p>

                    <p>הזמנתך התקבלה ואושרה על ידי מערכת ההזמנות המקוונת של יד ושם.<br>
                    <strong>{confirmationDetails.tourName}</strong></p>

                    <p>
                        <strong>תאריך הביקור:</strong> {confirmationDetails.reservationDate}  <strong>בשעה:</strong> {confirmationDetails.arrivalTime}<br>
                        <strong>מספר אישור ההזמנה:</strong> {confirmationDetails.reservationNumber}<br>
                        <strong>שם המזמין:</strong> {confirmationDetails.name}<br>";

                if (confirmationDetails.creditCard4Digit != "")
                {
                    returnMessage += $@"<strong>שולם באמצעות:</strong> ************{confirmationDetails.creditCard4Digit}  {confirmationDetails.creditCardBrand} <br>";
                }
                if (confirmationDetails.agenName != "" && confirmationDetails.Equals(string.Empty))
                {
                    returnMessage += $@"<strong>שם סוכנות נסיעות / חברה:</strong> {confirmationDetails.agenName}";
                }
                returnMessage += $@"

                    </p><br>
                    <p><strong>אישור זה מהווה אסמכתא סופית להזמנתך.</strong></p><br>
                    
                    <p>נא להציג את שובר הכניסה, לנציג בדלפק הקבוצות, במועד הביקור.</p><br><br>
                    <p>במידת הצורך ניצור עמך קשר לתיאום לוז ופרטים נוספים.</p>
                        ";
                if (!confirmationDetails.isEvent)
                {
                    returnMessage += $@"<br>
                       לעריכת הזמנה,  <a href='{confirmationDetails.editLink}'>הקלק כאן</a>.<br>";

                }
                returnMessage += $@"
                    <p> לביטול ההזמנה,  <a href='{confirmationDetails.cancelLink}'>הקלק כאן</a>.";
                returnMessage += $@" <strong>יש לשמור אישור זה למקרה של ביטול או שינוי ההזמנה.</strong>
                    </p>
                    <p>לבירורים ושאלות:<br>
                    דואר אלקטרוני: <a href='{mail}'>{mail}</a><br>
                    טלפון: {phone}</p>

                    <p>בברכה,<br>יד ושם, רשות הזיכרון לשואה ולגבורה </p>
                    </body>
                    </html>";

            }
            else
            {
                returnMessage = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <p>Shalom {recipientName},</p>

                    <p>Your reservation has been received and confirmed by Yad Vashem's online reservations system.<br><br>
                    <strong>{confirmationDetails.tourName}</strong><br></p>

                    <p>
                        <strong>The tour date:</strong> {confirmationDetails.reservationDate} <strong>on:</strong> {confirmationDetails.arrivalTime}<br>
                        <strong>Reservation Confirmation Number:</strong> {confirmationDetails.reservationNumber}<br>
                        <strong>name:</strong> {confirmationDetails.name}<br>
                    </p>
                    <strong>This confirmation is a final reference for your order.</strong><br><br>

                    <strong>Please present the attached Voucher to a representative at the Groups Desk upon arrival.</strong>
                    <p>If necessary, we will contact you to coordinate the schedule and other details.</p>
                    <p>This confirmation should be kept in case of cancellation or editing of your reservation.<br>
                    ";
                if (confirmationDetails.creditCard4Digit != "")
                {
                    returnMessage += $@"<strong>Paid with:</strong> ************{confirmationDetails.creditCard4Digit}  {confirmationDetails.creditCardBrand} <br>";
                }
                if (!confirmationDetails.isEvent)
                {

                    returnMessage += $@"If you need to edit your reservation, <a href='{confirmationDetails.editLink}'> please click here</a>.";
                }
                returnMessage += $@"If you need to cancel your reservation, <a href='{confirmationDetails.cancelLink}'> please click here</a>.<br>";
                returnMessage += $@"</p>

                    <p>For additional queries:<br>
                    Email: <a href='{mail}'>{mail}</a><br>
                    Tel: {phone}</p>

                    <p>Regards,<br>Yad Vashem Visitors' Center</p>

                </body>
                </html>";
            }
            return returnMessage;
        }


        public Dictionary<string, List<string>> GetArrivalTimes(int month, int year, decimal activityDurationHours, string tourTypeId)
        {
            try
            {
                DateTime startDate = DateTime.Now.Date;
                DateTime endDate = startDate.AddYears(1);
                var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                    <entity name = ""new_resource"">
                                    <attribute name = ""new_name""/>
                                    <attribute name = ""new_sunday_open""/>
                                    <attribute name = ""new_sunday_close""/>
                                    <attribute name = ""new_monday_open""/>
                                    <attribute name = ""new_monday_close""/>  
                                    <attribute name = ""new_tuesday_open""/>
                                    <attribute name = ""new_tuesday_close""/>
                                    <attribute name = ""new_wednesday_open""/>
                                    <attribute name = ""new_wednesday_close""/>
                                    <attribute name = ""new_thursday_open""/>
                                    <attribute name = ""new_thursday_close""/>
                                    <attribute name = ""new_friday_open""/>
                                    <attribute name = ""new_friday_close""/>
                                        <filter type = ""and"">
                                            <condition attribute = ""statecode"" operator= ""eq"" value = ""0""/>
                                            <condition attribute=""new_name"" operator=""eq"" value=""מוזיאון""/>
                                        </filter>
                                    </entity>
                                    </fetch>";
                var query = new FetchExpression(fetchXml);
                var museumEntity = _crmService.RetrieveMultiple(query);
                var openTime = _newReservationService.GetTimesForWeek(museumEntity[0], false);
                var closeTime = _newReservationService.GetTimesForWeek(museumEntity[0], true);
                if (month != null && year != null)
                {
                    startDate = new DateTime(year, month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                var startTimeList = new List<StartTimeItem>();
                var ec_closedHoursDates = getCloseHoursInMonth(startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"), tourTypeId);
                //var ec_closedHoursDates = getCloseHoursInMonth(startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                //startTimeList = _newReservationService.GetSpecialEventTimeListByTourType(tourTypeId, dateTime);

                //startTimeList = _newReservationService.GetTimeListByTourType(tourTypeId);
                //List<Condition> conditions = new List<Condition>();
                //conditions.Add(new Condition("new_tour_typeid", "eq", tourTypeId));
                //conditions.Add(new Condition("new_time", "not-null"));

                var TimesOnTour = _onlineReservationRepository.GetTourDateAndTime(month, year, tourTypeId, true);

                Dictionary<string, List<string>> monthlyTimeSlots;
                //if (startTimeList.Count <= 0)
                //{
                monthlyTimeSlots = GetMonthlyTimeSlots(TimesOnTour, ec_closedHoursDates, openTime, closeTime, activityDurationHours, month, year, tourTypeId);
                return monthlyTimeSlots;

                //}
                //else
                //{
                //    monthlyTimeSlots = GetMonthlyTimeSlotsToOpenHours(year, month, startTimeList, ec_closedHoursDates, tourTypeId);
                //    return monthlyTimeSlots;
                //}
            }
            catch (Exception ex)
            {
                Log.Error($"GetArrivalTimes error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }




        private (List<(TimeSpan From, TimeSpan To)> Closed,
                 List<(TimeSpan From, TimeSpan To)> Open)
        GetCloseArrivalHoursByDate(DateTime date, EntityCollection ec_closedHoursDates, string tourId, bool forArrivalTimes, bool educationalGroup = false)
        {
            try
            {
                var guidTourId = new Guid(tourId);
               
                var tourEntity = _crmService.Retrieve("new_tour_type", guidTourId, true);

                
                bool isNeedMuseum = false;
                bool isLocationYV = false;
                bool isOpeningHoursByMuseum = tourEntity.GetAttributeValue<bool>("new_opening_hours_by_museum");
                if (tourEntity.GetAttributeValue<EntityReference>("new_activity_day_location") != null)
                {
                    if (tourEntity.GetAttributeValue<EntityReference>("new_activity_day_location").Id.Equals(_config.GetValue<Guid>("YadVashemLocationGuid")))
                    {
                        isLocationYV = true;
                    }
                }
                if (tourEntity.GetAttributeValue<EntityReference>("new_resource") != null)
                {
                    if (tourEntity.GetAttributeValue<EntityReference>("new_resource").Id.Equals(_config.GetValue<Guid>("MuseumResourceId")))
                    {
                        isNeedMuseum = true;
                    }
                }
                var closeHours = new List<(TimeSpan From, TimeSpan To)>();
                var openHours = new List<(TimeSpan From, TimeSpan To)>();

                if (ec_closedHoursDates.Entities.Count > 0)
                {
                    var date1 = ec_closedHoursDates.Entities[0].GetAttributeValue<DateTime>("new_from_closing_date").Date.ToLocalTime();
                    var closedHoursDateList = ec_closedHoursDates.Entities.Where(entity => entity.GetAttributeValue<DateTime>("new_from_closing_date").Date == date.Date).ToList();
                    if (closedHoursDateList.Count >= 1)
                    {
                        closedHoursDateList.ForEach(entity =>
                        {
                            if (entity.GetAttributeValue<OptionSetValue>("new_closing_type").Value == 7)//מוזיאון פתוח
                            {
                                if (isNeedMuseum)
                                {
                                    var form = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                                    var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;
                                    //מוריד משך הפעילות
                                    var activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours_resource");
                                    //var activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours");
                                    int wholeHours = (int)activityDurationHours;
                                    decimal fractionalHours = activityDurationHours - wholeHours;
                                    TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                                    to = to - duration;
                                    openHours.Add((form, to));
                                }
                            }
                            if (entity.GetAttributeValue<OptionSetValue>("new_closing_type").Value == 4)//סגירת פעילות
                            {
                                if (entity.GetAttributeValue<AliasedValue>("ClosedDays.new_tour_typeid")?.Value.ToString() == tourId)
                                {
                                    var form = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                                    var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;
                                    closeHours.Add((form, to));
                                }
                            }
                            else if (entity.GetAttributeValue<OptionSetValue>("new_closing_type").Value == 2)//סגירת קמפוס יד ושם
                            {
                                if (isLocationYV)
                                {
                                    var form = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                                    var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;
                                    //מוריד משך הפעילות
                                    var activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours");
                                    int wholeHours = (int)activityDurationHours;
                                    decimal fractionalHours = activityDurationHours - wholeHours;
                                    TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                                    form = form - duration;
                                    closeHours.Add((form, to));
                                }
                            }
                            else if (entity.GetAttributeValue<OptionSetValue>("new_closing_type").Value == 3)//מוזיאון סגור
                            {
                                if (isNeedMuseum && isOpeningHoursByMuseum)
                                //רק אם אתה רוצה להתחשב בשעות שלו
                                {
                                    var form = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                                    var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;
                                    decimal activityDurationHours;
                                    if (forArrivalTimes)
                                    {
                                        //מוריד משך הפעילות
                                        activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours");
                                        if (educationalGroup)
                                        {

                                            //מוריד משך משאב
                                            activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours_resource");
                                        }
                                    }
                                    else
                                    {
                                        //מוריד משך משאב
                                        activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours_resource");


                                    }
                                    int wholeHours = (int)activityDurationHours;
                                    decimal fractionalHours = activityDurationHours - wholeHours;
                                    TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                                    form = form - duration;

                                    closeHours.Add((form, to));
                                }
                            }


                        });
                    }
                }
                closeHours.Sort((x, y) => x.From.CompareTo(y.From));
                return (closeHours, openHours);
            }
            catch (Exception ex)
            {
                Log.Error($"GetCloseArrivalHoursByDate error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public Dictionary<string, int> getDateTimeSlots(string date, string tourTypeId, string tourCode, decimal activityDurationHours)
        {
            try
            {
                var parsedTourTypeId = Guid.Parse(tourTypeId);
                var columns = new string[] { "new_opening_hours_by_museum" };
                var tourEntity = _crmService.Retrieve("new_tour_type", parsedTourTypeId, columns);

                var isOpeningHoursByMuseum = tourEntity.GetAttributeValue<bool>("new_opening_hours_by_museum");
                var currentDate = DateTime.ParseExact(date, "dd/MM/yyyy", null);
                Dictionary<string, int> dailyTimeSlots = new Dictionary<string, int>();
                Dictionary<string, Dictionary<string, int>> dateTimes = new Dictionary<string, Dictionary<string, int>>();
                if (currentDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    return dailyTimeSlots;
                }
                Dictionary<DayOfWeek, string> dayOfWeekMap = new Dictionary<DayOfWeek, string>
                {
                    {DayOfWeek.Sunday,"new_sunday"},
                    {DayOfWeek.Monday,"new_monday"},
                    {DayOfWeek.Tuesday, "new_tuesday" },
                    {DayOfWeek.Wednesday, "new_wednesday"},
                    {DayOfWeek.Thursday, "new_thursday" },
                    {DayOfWeek.Friday , "new_friday" },
                };
                var datesAndTimeEntities = _onlineReservationRepository.GetDalyTourDateAndTime(currentDate, tourTypeId);
                List<StartTimeItem> startTimeList = new List<StartTimeItem>();
                string thisDay = dayOfWeekMap[currentDate.DayOfWeek];
                foreach (var entity in datesAndTimeEntities.Entities)
                {
                    if (entity.Contains(thisDay) && entity.GetAttributeValue<bool>(thisDay) == true)
                    {
                        var startTime = new StartTimeItem();
                        startTime.StartTime = entity.GetAttributeValue<AliasedValue>("yv_time.new_name").Value.ToString();
                        startTimeList.Add(startTime);
                        //startTimeItems.AddRange(GetYvTimeListByDayAndTime(entity.Id.ToString()));

                    }
                }
                var ec_closedHoursDates = getCloseHoursInMonth(currentDate.ToString("yyyy-MM-dd"), currentDate.ToString("yyyy-MM-dd"), tourTypeId);
                startTimeList.AddRange(_newReservationService.GetSpecialEventTimeListByTourType(tourTypeId, currentDate));
                if (startTimeList.Count > 0)
                {
                    //לשאול את דבורה אם להתייחס גם כאן או רק כשתלוי במוזיאון
                    var (closeHoursList, openHoursList) = GetCloseArrivalHoursByDate(currentDate, ec_closedHoursDates, tourTypeId, true);

                    foreach (var item in startTimeList)
                    {
                        TimeSpan time = TimeSpan.Parse(item.StartTime);

                        if (!isInCloseHours(time, closeHoursList))
                        {
                            dailyTimeSlots[item.StartTime.ToString()] = CheckIfThereIsPlaceInMuseum(currentDate.ToString("dd/MM/yyyy"), new Time(time), tourCode);

                        }

                    }
                }
                //אם אין שעות בסוג פעילות וגם אין שעות מיוחדות   אז רק אם מסומן לך בסוג עילות יומית שאתה צריך הלביא מהמוזיאון

                else if (isOpeningHoursByMuseum)
                {
                    var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                    <entity name = ""new_resource"">
                    <attribute name = ""new_name""/>
                    <attribute name = ""new_sunday_open""/>
                    <attribute name = ""new_sunday_close""/>
                    <attribute name = ""new_monday_open""/>
                    <attribute name = ""new_monday_close""/>  
                    <attribute name = ""new_tuesday_open""/>
                    <attribute name = ""new_tuesday_close""/>
                    <attribute name = ""new_wednesday_open""/>
                    <attribute name = ""new_wednesday_close""/>
                    <attribute name = ""new_thursday_open""/>
                    <attribute name = ""new_thursday_close""/>
                    <attribute name = ""new_friday_open""/>
                    <attribute name = ""new_friday_close""/>
                        <filter type = ""and"">
                            <condition attribute = ""statecode"" operator= ""eq"" value = ""0""/>
                            <condition attribute=""new_name"" operator=""eq"" value=""מוזיאון""/>
                        </filter>
                    </entity>
                    </fetch>";
                    var query = new FetchExpression(fetchXml);
                    var museumEntity = _crmService.RetrieveMultiple(query);
                    var openTime = _newReservationService.GetTimesForWeek(museumEntity[0], false);
                    var closeTime = _newReservationService.GetTimesForWeek(museumEntity[0], true);
                    int dayOfWeekIndex = (int)currentDate.DayOfWeek;
                    // הוספת חצי שעה לפני הפתיחה
                    TimeSpan startTime = openTime[dayOfWeekIndex] - TimeSpan.FromMinutes(30);

                    //  הסגירה פחות משך הפעילות
                    int wholeHours = (int)activityDurationHours;
                    decimal fractionalHours = activityDurationHours - wholeHours;
                    TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                    TimeSpan endTime = closeTime[dayOfWeekIndex] - duration;

                    var (closeHoursList, openHoursList) = GetCloseArrivalHoursByDate(currentDate, ec_closedHoursDates, tourTypeId, true);
                    if (startTime < endTime)
                    {
                        TimeSpan currentTime = startTime;
                        while (currentTime <= endTime)
                        {
                            if (!isInCloseHours(currentTime, closeHoursList))//על כל שעת הגעה בודק האם קיימת בשעות סגורות
                            {
                                dailyTimeSlots[currentTime.ToString(@"hh\:mm")] = CheckIfThereIsPlaceInMuseum(currentDate.ToString("dd/MM/yyyy"), new Time(currentTime), tourCode);
                                //dailyTimeSlots.Add(currentTime.ToString(@"hh\:mm"));
                            }
                            currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                        }

                    }
                    var startTimes = getOpenHoursList(startTimeList, openHoursList, 30);
                    startTimes.ForEach(startTime =>
                    {
                        //dailyTimeSlots[startTime.StartTime] = CheckIfThereIsPlaceInMuseum(currentDate.ToString("dd/MM/yyyy"), new Time(startTime.StartTime), tourCode);
                    });
                    //if(openHoursList != null && openHoursList.Count>0)
                    //{
                    //    openHoursList.ForEach(openHours =>
                    //    {
                    //        // הוספת חצי שעה לפני הפתיחה
                    //        TimeSpan startTime = openHours.From - TimeSpan.FromMinutes(30);

                    //        //  הסגירה פחות משך הפעילות
                    //        int wholeHours = (int)activityDurationHours;
                    //        decimal fractionalHours = activityDurationHours - wholeHours;
                    //        TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                    //        TimeSpan endTime = openHours.To - duration;

                    //        if (startTime < endTime)
                    //        {
                    //            TimeSpan currentTime = startTime;
                    //            while (currentTime <= endTime)
                    //            {
                    //                // האם יתכנו סגירות שעותת שיסתרו את פתיחת מוזיאון?
                    //                //if (!isInCloseHours(currentTime, closeHoursList))//על כל שעת הגעה בודק האם קיימת בשעות סגורות
                    //                //{
                    //                dailyTimeSlots[currentTime.ToString(@"hh\:mm")] = CheckIfThereIsPlaceInMuseum(currentDate.ToString("dd/MM/yyyy"), new Time(currentTime), tourCode);
                    //                //dailyTimeSlots.Add(currentTime.ToString(@"hh\:mm"));
                    //                //}
                    //                currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                    //            }

                    //        }
                    //    });



                    //}
                    //להוריד משך פעילות 

                    //dailyTimeSlots = SubtractDurationFromArrivalTimeList(dailyTimeSlots, duration);

                }
                //dateTimes.Add(convertDateTimeToString(currentDate), dailyTimeSlots);
                return dailyTimeSlots;
            }
            catch (Exception ex)
            {
                Log.Error($"GetMonthlyTimeSlots error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        private Dictionary<string, List<string>> GetMonthlyTimeSlots(
    EntityCollection ec_TimesOnTour,
    EntityCollection ec_closedHoursDates,
    TimeSpan[] openingTimes,
    TimeSpan[] closingTimes,
    decimal activityDurationHours,
    int month,
    int year,
    string tourTypeId)
        {
            try
            {
                Log.Information("[GetMonthlyTimeSlots] START");

                Dictionary<string, List<string>> monthlyTimeSlots = new Dictionary<string, List<string>>();
                int daysInMonth = DateTime.DaysInMonth(year, month);

                var parsedTourTypeId = Guid.Parse(tourTypeId);
                var columns = new string[] { "new_enable_hours_by_default", "new_target_population" };

                var tourEntity = _crmService.Retrieve("new_tour_type", parsedTourTypeId, columns);

                var targetPopulation =
                    tourEntity.GetAttributeValue<OptionSetValue>("new_target_population") != null
                        ? tourEntity.GetAttributeValue<OptionSetValue>("new_target_population").Value.ToString()
                        : "";

                bool isEducationalGroup = targetPopulation == "3";
                bool isEnableHoursByDefault = tourEntity.GetAttributeValue<bool>("new_enable_hours_by_default");
                var specialEventsByDate =GetSpecialEventTimeListByTourTypeForMonth(tourTypeId, year, month);
                var closedHoursByDate = ec_closedHoursDates.Entities
                    .GroupBy(e => e.GetAttributeValue<DateTime>("new_from_closing_date").Date)
                    .ToDictionary(g => g.Key, g => g.ToList());
                for (int day = 1; day <= daysInMonth; day++)
                {
                    DateTime currentDate = new DateTime(year, month, day);
                    int dayOfWeekIndex = (int)currentDate.DayOfWeek;

                    if (dayOfWeekIndex == 6) continue; // שבת

                    Log.Information($"[GetMonthlyTimeSlots] Day {currentDate:yyyy-MM-dd} START");

                    HashSet<string> dailyTimeSlots = new HashSet<string>();

                    List<StartTimeItem> startTimeList = GetTourHoursByDate(ec_TimesOnTour, currentDate);

                    if (specialEventsByDate.TryGetValue(currentDate.Date, out var specialEvents))
                    {
                        startTimeList.AddRange(specialEvents);
                    }
               

                    var (closeHoursList, openHoursList) = GetCloseArrivalHoursByDateOptimized(currentDate, closedHoursByDate, tourEntity, true, isEducationalGroup);


                    if (startTimeList.Count > 0)
                    {
                        //ךשאול את דבורה האם תהיחס גם כאן לפתיחת מוזיאון או רק שמוגדר דיפולטיבי
                        startTimeList = getOpenHoursList(startTimeList, openHoursList, 30);

                        foreach (var item in startTimeList)
                        {
                            TimeSpan time = TimeSpan.Parse(item.StartTime);
                            if (!isInCloseHours(time, closeHoursList))
                                dailyTimeSlots.Add(item.StartTime);
                        }
                    }
                    //אם אין שעות בסוג פעילות וגם אין שעות מיוחדות - אז רק אם אתה מגדיר את זה בסוג סיור שלך תעשה באופן דיפולטיבי
                    else if (isEnableHoursByDefault)
                    {
                        TimeSpan startTime = openingTimes[dayOfWeekIndex] - TimeSpan.FromMinutes(30);
                        if (isEducationalGroup)
                        {
                            startTime = TimeSpan.Parse(_config.GetValue<string>("educationalGroupStartTime"));
                        }
                        ;
                        //  הסגירה פחות משך הפעילות
                        int wholeHours = (int)activityDurationHours;
                        decimal fractionalHours = activityDurationHours - wholeHours;
                        TimeSpan duration = TimeSpan.FromHours(wholeHours)
                            + TimeSpan.FromMinutes((double)(fractionalHours * 60));

                        TimeSpan endTime = closingTimes[dayOfWeekIndex] - duration;
                        if (isEducationalGroup)
                        {
                            endTime = TimeSpan.Parse(_config.GetValue<string>("educationalGroupEndTime")) - duration;
                        }
                        //var closeHoursList = GetCloseArrivalHoursByDate(currentDate, ec_closedHoursDates, tourTypeId, true, isEducationalGroup);
                        if (startTime < endTime)
                        {
                            TimeSpan currentTime = startTime;

                            while (currentTime <= endTime)
                            {
                                if (!isInCloseHours(currentTime, closeHoursList))
                                    dailyTimeSlots.Add(currentTime.ToString(@"hh\:mm"));

                                currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                            }

                        }
                        getOpenHoursList(startTimeList, openHoursList, 30).ForEach(item =>
                        {
                            //לשאול את דבורה על הוספת זמן לפני תיחה וכו'
                            dailyTimeSlots.Add(item.StartTime);

                        });
                        //להוריד משך פעילות 
                    }
                    monthlyTimeSlots.Add(convertDateTimeToString(currentDate), dailyTimeSlots.ToList());

                }


                return monthlyTimeSlots;
            }
            catch (Exception ex)
            {
                Log.Error($"GetMonthlyTimeSlots error: {ex.Message}");
                throw;
            }
        }
        public Dictionary<DateTime, List<StartTimeItem>> GetSpecialEventTimeListByTourTypeForMonth(
    string tourTypeId,
    int year,
    int month)
        {
            try
            {
                DateTime monthStart = new DateTime(year, month, 1);
                DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var result = new Dictionary<DateTime, List<StartTimeItem>>();

                var fetchXml = @$"
        <fetch version='1.0' output-format='xml-platform' mapping='logical'>
          <entity name='new_spcific_date_and_hour'>
            <attribute name='new_start_time' />
            <order attribute='new_start_time' descending='false' />
            <filter type='and'>
              <condition attribute='statecode' operator='eq' value='0' />
              <condition attribute='new_tour_type' operator='eq' value='{tourTypeId}' />
              <condition attribute='new_start_time' operator='on-or-after' value='{monthStart:yyyy-MM-dd}' />
              <condition attribute='new_start_time' operator='on-or-before' value='{monthEnd:yyyy-MM-dd}' />
            </filter>
          </entity>
        </fetch>";

                var entities = _crmService.RetrieveMultiple(new FetchExpression(fetchXml));

                foreach (var entity in entities.Entities)
                {
                    var dateTime = entity.GetAttributeValue<DateTime>("new_start_time").ToLocalTime();
                    var date = dateTime.Date;
                    var time = dateTime.ToString("HH:mm");

                    if (!result.ContainsKey(date))
                        result[date] = new List<StartTimeItem>();

                    result[date].Add(new StartTimeItem { StartTime = time });
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"GetSpecialEventTimeListByTourTypeForMonth error: {ex.Message}");
                throw;
            }
        }
        public (List<(TimeSpan From, TimeSpan To)> CloseHours, List<(TimeSpan From, TimeSpan To)> OpenHours)
    GetCloseArrivalHoursByDateOptimized(
        DateTime date,
        Dictionary<DateTime, List<Entity>> closedHoursByDate,
        Entity tourEntity,
        bool forArrivalTimes,
        bool educationalGroup = false)
        {
            try
            {
                bool isNeedMuseum = false;
                bool isLocationYV = false;
                bool isOpeningHoursByMuseum = tourEntity.GetAttributeValue<bool>("new_opening_hours_by_museum");

                var locationRef = tourEntity.GetAttributeValue<EntityReference>("new_activity_day_location");
                if (locationRef != null && locationRef.Id.Equals(_config.GetValue<Guid>("YadVashemLocationGuid")))
                    isLocationYV = true;

                var resourceRef = tourEntity.GetAttributeValue<EntityReference>("new_resource");
                if (resourceRef != null && resourceRef.Id.Equals(_config.GetValue<Guid>("MuseumResourceId")))
                    isNeedMuseum = true;

                var closeHours = new List<(TimeSpan From, TimeSpan To)>();
                var openHours = new List<(TimeSpan From, TimeSpan To)>();

                if (closedHoursByDate.TryGetValue(date.Date, out var closedHoursForDay))
                {
                    closedHoursForDay.ForEach(entity =>
                    {
                        var closingType = entity.GetAttributeValue<OptionSetValue>("new_closing_type")?.Value;

                        if (closingType == 7 && isNeedMuseum) // מוזיאון פתוח
                        {
                            var from = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                            var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;

                            // מוריד משך פעילות
                            decimal activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours");
                            int wholeHours = (int)activityDurationHours;
                            decimal fractionalHours = activityDurationHours - wholeHours;
                            TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                            to = to - duration;

                            openHours.Add((from, to));
                        }
                        else if (closingType == 4 && entity.GetAttributeValue<AliasedValue>("ClosedDays.new_tour_typeid")?.Value.ToString() == tourEntity.Id.ToString())
                        {
                            var from = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                            var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;
                            closeHours.Add((from, to));
                        }
                        else if (closingType == 2 && isLocationYV) // סגירת קמפוס יד ושם
                        {
                            var from = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                            var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;

                            decimal activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours");
                            int wholeHours = (int)activityDurationHours;
                            decimal fractionalHours = activityDurationHours - wholeHours;
                            TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                            from = from - duration;

                            closeHours.Add((from, to));
                        }
                        else if (closingType == 3 && isNeedMuseum && isOpeningHoursByMuseum) // מוזיאון סגור
                        {
                            var from = entity.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime().TimeOfDay;
                            var to = entity.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime().TimeOfDay;

                            decimal activityDurationHours;
                            if (forArrivalTimes)
                            {
                                activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours");
                                if (educationalGroup)
                                    activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours_resource");
                            }
                            else
                            {
                                activityDurationHours = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours_resource");
                            }

                            int wholeHours = (int)activityDurationHours;
                            decimal fractionalHours = activityDurationHours - wholeHours;
                            TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
                            from = from - duration;

                            closeHours.Add((from, to));
                        }
                    });
                }

                closeHours.Sort((x, y) => x.From.CompareTo(y.From));
                return (closeHours, openHours);
            }
            catch (Exception ex)
            {
                Log.Error($"GetCloseArrivalHoursByDateOptimized error: {ex.Message}");
                throw;
            }
        }

        //private Dictionary<string, List<string>> GetMonthlyTimeSlots(EntityCollection ec_TimesOnTour, EntityCollection ec_closedHoursDates, TimeSpan[] openingTimes, TimeSpan[] closingTimes, decimal activityDurationHours, int month, int year, string tourTypeId)
        //{
        //    try
        //    {
        //        Dictionary<string, List<string>> monthlyTimeSlots = new Dictionary<string, List<string>>();
        //        int daysInMonth = DateTime.DaysInMonth(year, month);
        //        var parsedTourTypeId = Guid.Parse(tourTypeId);
        //        var columns = new string[] { "new_enable_hours_by_default", "new_target_population" };
        //        var tourEntity = _crmService.Retrieve("new_tour_type", parsedTourTypeId, columns);
        //        var targetPopulation = tourEntity.GetAttributeValue<OptionSetValue>("new_target_population") != null ? tourEntity.GetAttributeValue<OptionSetValue>("new_target_population").Value.ToString() : "";
        //        bool isEducationalGroup = false;
        //        if (targetPopulation == "3")
        //        {
        //            isEducationalGroup = true;
        //        }
        //        var isEnableHoursByDefault = tourEntity.GetAttributeValue<bool>("new_enable_hours_by_default");
        //        for (int day = 1; day <= daysInMonth; day++)
        //        {
        //            DateTime currentDate = new DateTime(year, month, day);

        //            int dayOfWeekIndex = (int)currentDate.DayOfWeek;

        //            // התעלמות משבת (Index 6)
        //            if (dayOfWeekIndex == 6) continue;

        //            HashSet<string> dailyTimeSlots = new HashSet<string>();
        //            List<StartTimeItem> startTimeList = GetTourHoursByDate(ec_TimesOnTour, currentDate);
        //            startTimeList.AddRange(_newReservationService.GetSpecialEventTimeListByTourType(tourTypeId, currentDate));
        //            var (closeHoursList, openHoursList) = GetCloseArrivalHoursByDate(currentDate, ec_closedHoursDates, tourTypeId, true, isEducationalGroup);

        //            if (startTimeList.Count > 0)
        //            {
        //                //ךשאול את דבורה האם תהיחס גם כאן לפתיחת מוזיאון או רק שמוגדר דיפולטיבי
        //                startTimeList = getOpenHoursList(startTimeList, openHoursList,30);

        //                foreach (var item in startTimeList)
        //                {
        //                    TimeSpan time = TimeSpan.Parse(item.StartTime);

        //                    if (!isInCloseHours(time, closeHoursList))
        //                        dailyTimeSlots.Add(item.StartTime.ToString());

        //                }
        //            }
        //            //אם אין שעות בסוג פעילות וגם אין שעות מיוחדות - אז רק אם אתה מגדיר את זה בסוג סיור שלך תעשה באופן דיפולטיבי
        //            else if(isEnableHoursByDefault)
        //            {

        //                // הוספת חצי שעה לפני הפתיחה
        //                TimeSpan startTime = openingTimes[dayOfWeekIndex] - TimeSpan.FromMinutes(30);
        //                if (isEducationalGroup)
        //                {
        //                    startTime = TimeSpan.Parse(_config.GetValue<string>("educationalGroupStartTime"));
        //                };
        //                //  הסגירה פחות משך הפעילות
        //                int wholeHours = (int)activityDurationHours;
        //                decimal fractionalHours = activityDurationHours - wholeHours;
        //                TimeSpan duration = TimeSpan.FromHours(wholeHours) + TimeSpan.FromMinutes((double)(fractionalHours * 60));
        //                TimeSpan endTime = closingTimes[dayOfWeekIndex] - duration;
        //                if (isEducationalGroup)
        //                {
        //                    endTime = TimeSpan.Parse(_config.GetValue<string>("educationalGroupEndTime"))- duration;
        //                }
        //                //var closeHoursList = GetCloseArrivalHoursByDate(currentDate, ec_closedHoursDates, tourTypeId, true, isEducationalGroup);
        //                if (startTime < endTime)
        //                {
        //                    TimeSpan currentTime = startTime;
        //                    while (currentTime <= endTime)
        //                    {
        //                        if (!isInCloseHours(currentTime, closeHoursList))//על כל שעת הגעה בודק האם קיימת בשעות סגורות
        //                        {
        //                            dailyTimeSlots.Add(currentTime.ToString(@"hh\:mm"));
        //                        }
        //                        currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
        //                    }

        //                }
        //               getOpenHoursList(startTimeList, openHoursList,30).ForEach(item=> {
        //                   //לשאול את דבורה על הוספת זמן לפני תיחה וכו'
        //                   dailyTimeSlots.Add(item.StartTime);

        //               });
        //                //להוריד משך פעילות 

        //                //dailyTimeSlots = SubtractDurationFromArrivalTimeList(dailyTimeSlots, duration);

        //            }
        //            monthlyTimeSlots.Add(convertDateTimeToString(currentDate), dailyTimeSlots.ToList());
        //        }


        //        return monthlyTimeSlots;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"GetMonthlyTimeSlots error: {ex.Message}");
        //        throw new Exception(ex.Message);
        //    }
        //}
        public List<StartTimeItem> GetYvTimeListByDayAndTime(string DayAndTimeId)
        {
            try
            {
                var timeList = new List<StartTimeItem>();
                var fetchXml = @$"<fetch distinct=""false"" mapping=""logical"">
                        <entity name=""new_yv_time"">
                           <attribute name=""new_name"" />
                           <order attribute=""new_name"" descending=""false"" />
                           <filter type=""and"">
                              <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                           </filter>
                           <link-entity name=""new_new_day_and_time_new_yv_time"" to=""new_yv_timeid"" from=""new_yv_timeid"" link-type=""inner"">
                             <filter type=""and"">
                                 <condition attribute=""new_day_and_timeid"" operator=""eq"" value=""{DayAndTimeId}"" />
                           </filter>
                           </link-entity>
                        </entity>
                     </fetch>";

                var query = new FetchExpression(fetchXml);
                var timeEntitiesList = _crmService.RetrieveMultiple(query);
                foreach (var entity in timeEntitiesList.Entities)
                {
                    var time = entity.GetAttributeValue<string>("new_name");
                    timeList.Add(new StartTimeItem() { StartTime = time.PadLeft(5, '0') });
                }
                return timeList;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTimeListByTourType error: {ex.Message}");
                throw;
            }
        }

        //מביא שעות של הסיור לפי התאריך
        private List<StartTimeItem> GetTourHoursByDate(EntityCollection ec_TimesOnTour, DateTime date)
        {
            Dictionary<DayOfWeek, string> dayOfWeekMap = new Dictionary<DayOfWeek, string>
            {
                    {DayOfWeek.Sunday,"new_sunday"},
                    {DayOfWeek.Monday,"new_monday"},
                    {DayOfWeek.Tuesday, "new_tuesday" },
                    {DayOfWeek.Wednesday, "new_wednesday"},
                    {DayOfWeek.Thursday, "new_thursday" },
                    {DayOfWeek.Friday , "new_friday" },
            };
            List<StartTimeItem> startTimeItems = new List<StartTimeItem>();
            string thisDay = dayOfWeekMap[date.DayOfWeek];

            foreach (var entity in ec_TimesOnTour.Entities)
            {
                //אם יש תאריך התחלה
                if (entity.Contains("new_start_date"))
                {
                    if (entity.GetAttributeValue<DateTime>("new_start_date").ToLocalTime() <= date)
                    {
                        if (!entity.Contains("new_end_date") || entity.Contains("new_end_date") && (entity["new_end_date"] == null || entity.GetAttributeValue<DateTime>("new_end_date").ToLocalTime() >= date))
                        {
                            if (entity.Contains(thisDay) && entity.GetAttributeValue<bool>(thisDay) == true)
                            {
                                var startTime = new StartTimeItem();
                                startTime.StartTime = entity.GetAttributeValue<AliasedValue>("yv_time.new_name").Value.ToString();
                                startTimeItems.Add(startTime);

                            }

                        }
                    }
                }
                //אחרת זה בטוח בטווח של החודש (כי סונן כבר קודם
                else
                {
                    if (entity.Contains(thisDay) && entity.GetAttributeValue<bool>(thisDay) == true)
                    {
                        var startTime = new StartTimeItem();
                        startTime.StartTime = entity.GetAttributeValue<AliasedValue>("yv_time.new_name").Value.ToString();
                        startTimeItems.Add(startTime);
                        //startTimeItems.AddRange(GetYvTimeListByDayAndTime(entity.Id.ToString()));

                    }
                }
            }
            return startTimeItems;
        }

        private List<string> SubtractDurationFromArrivalTimeList(List<string> timeSlots, TimeSpan duration)
        {
            if (timeSlots == null || timeSlots.Count == 0)
                return new List<string>();

            // לקיחת האיבר האחרון ברשימה
            string lastTimeString = timeSlots.Last();
            TimeSpan lastTime = TimeSpan.Parse(lastTimeString);

            // חישוב הזמן המינימלי לאחר הפחתת משל הפעילות
            TimeSpan minTime = lastTime - duration;

            // סינון הרשימה לפי הזמן המינימלי
            return timeSlots.Where(time =>
            {
                TimeSpan currentTime = TimeSpan.Parse(time);
                return currentTime <= minTime;
            }).ToList();
        }
        private bool isInCloseHours(TimeSpan currentTime, List<(TimeSpan From, TimeSpan To)> closeHoursList)
        {
            if (closeHoursList.Count <= 0) { return false; }
            foreach (var closeHour in closeHoursList)
            {
                if (currentTime > closeHour.From && currentTime <= closeHour.To)
                {
                    return true; // השעה נמצאת בטווח חסום
                }
            }
            return false; // השעה לא בטווח חסום
        }
        private EntityCollection getCloseHoursInMonth(string startDate, string endDate, string tourTypeId)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_closed_days_and_activities");
                fetchXml += @$"<attribute name=""new_from_closing_date""/>
                            <attribute name=""new_to_closing_date""/>
\                            <attribute name=""new_closing_type""/>
                            <attribute name=""statecode""/>
                            <order attribute=""new_name"" descending=""false""/>
                            <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_is_closed_full_day"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_from_closing_date"" operator=""on-or-after"" value=""{startDate}""/>
                                <condition attribute=""new_from_closing_date"" operator=""on-or-before"" value=""{endDate}""/>
                                <filter type=""or"">
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""4""/>//סגירת פעילות
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""2""/>//קמפוס יד ושם סגור	
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""3""/>//סגירת מוזיאון
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""7""/>//פתיחת מוזיאון
                                </filter>
                            </filter>
                               <link-entity name=""new_new_closed_days_and_activities_new_tour"" from=""new_closed_days_and_activitiesid"" to=""new_closed_days_and_activitiesid"" alias=""ClosedDays"" link-type='outer' >
                                  <attribute name=""new_tour_typeid"" />                                  
                                   <filter type=""and"">
                                      <condition attribute=""new_tour_typeid"" operator=""eq"" value=""{tourTypeId}"" />
                                    </filter>
                                </link-entity>";

                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var res = _crmService.RetrieveMultiple(query);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"getCloseHoursInMonth error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public async Task<bool> sendEmailAndCreateAttachmentForBugsReservation()
        {
            var fetchXml = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>" +
                          "<entity name='new_reservation'>" +
                            "<attribute name='new_num'/>" +
                            "<attribute name='new_reservationid'/>" +
                            "<link-entity name='new_attachment' from='new_attachments' to='new_reservationid' alias='ab' link-type='outer'/>" +
                            "<filter type='and'>" +
                              "<condition attribute='statecode' operator='eq' value='0'/>" +
                              "<condition attribute='new_sub_source_reservation' operator='eq' value='1'/>" +
                              "<condition attribute='createdon' operator='on-or-after' value='2025-10-16'/>" +
                              "<condition attribute='createdon' operator='on-or-before' value='2025-10-19'/>" +
                              "<condition entityname='ab' attribute='new_attachmentid' operator='null'/>" +
                            "</filter>" +
                          "</entity>" +
                        "</fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            try
            {
                foreach (var entity in res.Entities)
                {
                    var rsvGuid = entity.Id;
                    OnlineReservation rsv = GetOnlineReservation(rsvGuid);
                    await confirmation(rsv, rsvGuid);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("sendEmailAndCreateAttachmentForBugsReservation error: " + ex.Message);
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> sendEmailAndCreateAttachmentForBugsEventRegistration()
        {
            var fetchXml = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>" +
                          "<entity name='new_event_registration'>" +
                            "<attribute name='new_num'/>" +
                            "<attribute name='new_event_registrationid'/>" +
                            "<link-entity name='new_attachment' from='new_event_registration_attachments' to='new_event_registrationid' alias='ab' link-type='outer'/>" +
                            "<filter type='and'>" +
                              "<condition attribute='statecode' operator='eq' value='0'/>" +
                              "<condition attribute=\"createdby\" operator=\"eq\" uiname=\"crmdev Yad Vashem\" uitype=\"systemuser\" value=\"{27985789-4AD5-E511-80C8-005056B13C97}\"/>" +
                              "<condition attribute='createdon' operator='on-or-after' value='2025-10-16'/>" +
                              "<condition attribute='createdon' operator='on-or-before' value='2025-10-19'/>" +
                              "<condition entityname='ab' attribute='new_attachmentid' operator='null'/>" +
                              "<condition attribute=\"new_payment_comments\" operator=\"ne\" value=\"קיוסק\"/>" +
                            "</filter>" +
                          "</entity>" +
                        "</fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            try
            {
                foreach (var entity in res.Entities)
                {
                    var rsvGuid = entity.Id;
                    await confirmationEventRegistration(rsvGuid.ToString());
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("sendEmailAndCreateAttachmentForBugsEventRegistration error: " + ex.Message);
                throw new Exception(ex.Message);
            }
        }
        public async Task<bool> confirmationReservationWithoutPayment(string reservationGuid, string? couponGuid = null, decimal? totalPriceAfterDiscount = null)
        {
            try
            {
                Log.Information($"confirmationReservationWithoutPayment start, reservationGuid: {reservationGuid}");
                var rsvGuid = Guid.Parse(reservationGuid);
                Guid? CouponGuid = !string.IsNullOrEmpty(couponGuid)
                    ? Guid.Parse(couponGuid)
                    : (Guid?)null;

                //3.get reservation details from crm
                var reservationEntity = _crmService.Retrieve("new_reservation", rsvGuid, new[] { "new_num" });
                var reservationNumber = reservationEntity.GetAttributeValue<string>("new_num");
                UpdateReservationToActive(rsvGuid, false, CouponGuid, totalPriceAfterDiscount);
                OnlineReservation rsv = GetOnlineReservation(rsvGuid);
                Log.Information("confirmationReservationWithoutPayment end before confirmation");
                return await confirmation(rsv, rsvGuid);
            }
            catch (Exception ex)
            {
                Log.Error("confirmationReservationWithoutPayment error: " + ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private void updateMuseumSchedulesToActive(Guid rsvGuid)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_event_item");
            fetchXml += @$"<attribute name=""new_name""/>
                            <order attribute=""createdon"" descending=""true""/>
                            <filter type=""and"">
                                <condition attribute=""new_reservation"" operator=""eq"" value=""{rsvGuid}""/>
                            </filter>";
            fetchXml += _newReservationRepository.EndFetchXml();
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            foreach (var entity in res.Entities)
            {
                var museumScheduleGuid = entity.GetAttributeValue<Guid>("new_event_itemid");
                var scheduleEventEntity = new Entity("new_event_item", museumScheduleGuid);
                scheduleEventEntity["new_status"] = new OptionSetValue(1);//מאושר
                _crmService.Update(scheduleEventEntity);
            }
        }

        public ReservationTicket createReservationTicket(Guid reservationGuid)
        {
            var reservationTicket = new ReservationTicket();
            Entity rsvEntity = _onlineReservationRepository.GetOnlineReservationEntity(reservationGuid);
            reservationTicket.FullName = rsvEntity.GetAttributeValue<string>("new_firstname_contact") + " " + rsvEntity.GetAttributeValue<string>("new_lastname_contact");
            reservationTicket.PurchaseDate = convertDateTimeToString(rsvEntity.GetAttributeValue<DateTime>("createdon").ToLocalTime());
            reservationTicket.TotalPrice = rsvEntity.GetAttributeValue<Money>("new_total_price_after_discount")?.Value.ToString("0.00"); ;
            reservationTicket.Lang = rsvEntity.GetAttributeValue<EntityReference>("new_form_language").Name;
            reservationTicket.ReservationGuid = reservationGuid.ToString();
            reservationTicket.ReservationNumber = rsvEntity.GetAttributeValue<string>("new_num");
            //activity day in reservation
            var activityDayEntity = GetActivityDate(reservationGuid.ToString());
            reservationTicket.VisitDate = convertDateTimeToString(activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime());

            reservationTicket.VisitStartTime = activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().ToString("HH:mm");
            if (reservationTicket.Lang == "עברית")
            {
                reservationTicket.ActivityName = rsvEntity.GetAttributeValue<AliasedValue>("tourType.new_name").Value.ToString();

            }
            else
            {
                reservationTicket.ActivityName = rsvEntity.GetAttributeValue<AliasedValue>("tourType.new_name_en").Value.ToString();

            }
            //QRcodeDetailsList
            reservationTicket.QRcodeItemList = new List<QRCodeItem>();
            reservationTicket.activitiesDetails = new List<ActivityDetails>();
            reservationTicket.visitorsDetails = new List<VisitorsDetails>();
            var visitorsDetails = new VisitorsDetails();
            visitorsDetails.Amount = rsvEntity.GetAttributeValue<int>("new_number_of_participants").ToString();
            reservationTicket.visitorsDetails.Add(visitorsDetails);
            var QRcodeDetails = new QRCodeItem();
            QRcodeDetails.Guid = reservationGuid.ToString();
            QRcodeDetails.IsEventReservation = false;
            QRcodeDetails.EventCode = "";
            reservationTicket.QRcodeItemList.Add(QRcodeDetails);
            //מוצרים שהם הראה בקיוסק
            reservationTicket.products = new List<productToTicket>();
            var condition = new Condition("new_activity_day", "eq", activityDayEntity.Id.ToString());
            var conditions = new List<Condition>();
            conditions.Add(condition);

            var productsToShow = GetProductsToShow(conditions, reservationTicket.Lang);
            foreach (var item in productsToShow)
            {
                productToTicket product = new productToTicket();
                product.Amount = item.Amount.ToString();
                product.Price = item.Price.ToString();
                product.Name = item.Product.Value;
                reservationTicket.products.Add(product);
            }
            //  את כל הרישומים לארוע בהזמנה מביא
            var eventRegistrationInReservation = _onlineReservationRepository.GetEventRegistrationsInReservation(reservationGuid).Entities;
            for (int i = 0; i < eventRegistrationInReservation.Count; i++)
            {
                var eventRegistrationEntity = eventRegistrationInReservation[i];
                QRcodeDetails = new QRCodeItem();
                QRcodeDetails.Guid = eventRegistrationEntity.Id.ToString();
                QRcodeDetails.IsEventReservation = true;
                QRcodeDetails.EventCode = eventRegistrationEntity.GetAttributeValue<AliasedValue>("event.new_event_code").Value.ToString();
                reservationTicket.QRcodeItemList.Add(QRcodeDetails);
                //activity 
                var activityDetails = new ActivityDetails();
                var activityDayID = (eventRegistrationEntity.GetAttributeValue<AliasedValue>("ead.new_activity_day").Value as EntityReference).Id.ToString();

                var EventActivityDayEntity = _crmService.Retrieve("new_activity_day", new Guid(activityDayID), true);
                activityDetails.VisitStartTime = EventActivityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().ToString("HH:mm"); ;
                activityDetails.VisitDate = convertDateTimeToString(EventActivityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime());
                var eventGuid = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_event_reservations").Id.ToString();
                var eventEntity = GetTourByEvent(eventGuid);
                if (reservationTicket.Lang == "עברית")
                {
                    activityDetails.ActivityName = eventEntity.GetAttributeValue<string>("new_name");

                }
                else
                {
                    activityDetails.ActivityName = eventEntity.GetAttributeValue<string>("new_name_en");

                }
                //visitors details
                activityDetails.visitorsDetails = new List<VisitorsDetails>();
                List<Condition> conditionsList = new List<Condition>();
                Condition bidItemCondition1 = new Condition("new_event_registration", "eq", eventRegistrationEntity.Id.ToString());
                Condition bidItemCondition2 = new Condition("new_ticket_type", "not-null");
                conditionsList.Add(bidItemCondition1);
                conditionsList.Add(bidItemCondition2);
                var bidItems = GetBidItems(conditionsList, reservationTicket.Lang);
                foreach (var item in bidItems)
                {
                    VisitorsDetails visitorsDetail = new VisitorsDetails();
                    visitorsDetail.Amount = item.Amount.ToString();
                    visitorsDetail.Price = item.Price.ToString();
                    visitorsDetail.Name = _onlineReservationRepository.GetTicketTypeName(new Guid(item.TicketTypeGuid), reservationTicket.Lang);
                    activityDetails.visitorsDetails.Add(visitorsDetail);
                }
                if (bidItems == null || bidItems.Count == 0)
                {
                    VisitorsDetails visitorsDetail = new VisitorsDetails();
                    visitorsDetail.Amount = eventRegistrationEntity.GetAttributeValue<int>("new_number_of_participants").ToString();
                    activityDetails.visitorsDetails.Add(visitorsDetail);
                }
                //מוצרים שהם הראה בקיוסק
                activityDetails.products = new List<productToTicket>();
                bidItemCondition1 = new Condition("new_activity_day", "eq", eventRegistrationEntity.Id.ToString());
                conditionsList = new List<Condition>();
                conditionsList.Add(bidItemCondition1);
                productsToShow = GetProductsToShow(conditionsList, reservationTicket.Lang);
                foreach (var item in productsToShow)
                {
                    productToTicket product = new productToTicket();
                    product.Amount = item.Amount.ToString();
                    product.Price = item.Price.ToString();
                    product.Name = item.Product.Value;
                    activityDetails.products.Add(product);
                }

                reservationTicket.activitiesDetails.Add(activityDetails);
            }

            return reservationTicket;

        }
        public ReservationTicket createEventRegistrationTicket(Guid eventRegistrationGuid)
        {
            var eventRegistrationTicket = new ReservationTicket();
            var eventRegistrationEntity = getEventRegistration(eventRegistrationGuid.ToString());

            eventRegistrationTicket.FullName = eventRegistrationEntity.GetAttributeValue<string>("new_firstname_contact") + " " + eventRegistrationEntity.GetAttributeValue<string>("new_lastname_contact");
            eventRegistrationTicket.PurchaseDate = convertDateTimeToString(eventRegistrationEntity.GetAttributeValue<DateTime>("createdon").ToLocalTime());
            //reservationTicket.PaymentMethod = rsv.;
            eventRegistrationTicket.TotalPrice = eventRegistrationEntity.GetAttributeValue<Money>("new_total_price_after_discount").Value.ToString("0.00"); ;
            eventRegistrationTicket.Lang = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_form_language").Name;
            eventRegistrationTicket.ReservationGuid = eventRegistrationGuid.ToString();
            eventRegistrationTicket.ReservationNumber = eventRegistrationEntity.GetAttributeValue<string>("new_num");

            //activity day in reservation

            var activityDayID = (eventRegistrationEntity.GetAttributeValue<AliasedValue>("ead.new_activity_day").Value as EntityReference).Id.ToString();
            var activityDayEntity = _crmService.Retrieve("new_activity_day", new Guid(activityDayID), true);
            eventRegistrationTicket.VisitStartTime = activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().ToString("HH:mm");
            eventRegistrationTicket.VisitDate = convertDateTimeToString(activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime());
            var eventGuid = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_event_reservations").Id.ToString();
            var eventEntity = GetTourByEvent(eventGuid);
            if (eventRegistrationTicket.Lang == "עברית")
            {
                eventRegistrationTicket.ActivityName = eventEntity.GetAttributeValue<string>("new_name");
                eventRegistrationTicket.TourNote = eventEntity.GetAttributeValue<string>("new_note_watcher_he");
            }
            else
            {
                eventRegistrationTicket.ActivityName = eventEntity.GetAttributeValue<string>("new_name_en");
                eventRegistrationTicket.TourNote = eventEntity.GetAttributeValue<string>("new_note_watcher_en");
            }
            //QRcodeDetailsList
            List<QRCodeItem> QRcodeDetailsList = new List<QRCodeItem>();
            var QRcodeDetails = new QRCodeItem();
            QRcodeDetails.Guid = eventRegistrationGuid.ToString();
            QRcodeDetails.IsEventReservation = true;
            QRcodeDetails.EventCode = eventRegistrationEntity.GetAttributeValue<string>("new_event_code");
            QRcodeDetailsList.Add(QRcodeDetails);
            eventRegistrationTicket.QRcodeItemList = QRcodeDetailsList;
            //visitors details
            eventRegistrationTicket.visitorsDetails = new List<VisitorsDetails>();
            List<Condition> conditionsList = new List<Condition>();
            Condition bidItemCondition1 = new Condition("new_event_registration", "eq", eventRegistrationGuid.ToString());
            Condition bidItemCondition2 = new Condition("new_ticket_type", "not-null");
            conditionsList.Add(bidItemCondition1);
            conditionsList.Add(bidItemCondition2);
            var bidItems = GetBidItems(conditionsList, eventRegistrationTicket.Lang);
            eventRegistrationTicket.products = new List<productToTicket>();
            var condition = new Condition("new_event_registration", "eq", eventRegistrationGuid.ToString().ToUpper());
            var conditions = new List<Condition>();
            conditions.Add(condition);
            var productsToShow = GetProductsToShow(conditions, eventRegistrationTicket.Lang);
            foreach (var item in productsToShow)
            {
                productToTicket product = new productToTicket();
                product.Amount = item.Amount.ToString();
                product.Price = item.Price.ToString();
                product.Name = item.Product.Value;
                eventRegistrationTicket.products.Add(product);
            }
            foreach (var item in bidItems)
            {
                VisitorsDetails visitorsDetail = new VisitorsDetails();
                visitorsDetail.Amount = item.Amount.ToString();
                visitorsDetail.Price = item.Price.ToString();
                visitorsDetail.Name = _onlineReservationRepository.GetTicketTypeName(new Guid(item.TicketTypeGuid), eventRegistrationTicket.Lang);
                eventRegistrationTicket.visitorsDetails.Add(visitorsDetail);
            }
            if (bidItems == null || bidItems.Count <= 0)
            {
                VisitorsDetails visitorsDetail = new VisitorsDetails();
                visitorsDetail.Amount = eventRegistrationEntity.GetAttributeValue<int>("new_number_of_participants").ToString();

                eventRegistrationTicket.visitorsDetails.Add(visitorsDetail);
            }

            //var c= await CreateTicketAttachment(eventRegistrationTicket);
            return eventRegistrationTicket;

        }

        public async Task<bool> confirmationEventRegistration(string eventRegistrationGuid, string digit4 = "", string cardBrand = "")
        {
            var request = createAtthachmentEventRegistration(eventRegistrationGuid);
            request.creditCard4Digit = digit4;
            request.creditCardBrand = cardBrand;
            //יצירת PDF+שמירה בCRM
            var PdfRes = await CreatePdfAttachment(request);
            if (PdfRes.FullPath != null && PdfRes.FullPath != "")
            {
                Guid CrmAttachment = this.createAttachmentInCrm(PdfRes.FullPath, new Guid(eventRegistrationGuid), false);
            }
            //יצירת כרטיס ביקור+שמירה בCRM
            var reservationTicket = createEventRegistrationTicket(new Guid(eventRegistrationGuid));
            reservationTicket.PaymentMethod = digit4;
            var ticketRes = await CreateTicketAttachment(reservationTicket);
            if (ticketRes.FullPath != null && ticketRes.FullPath != "")
            {
                Guid CrmAttachment = this.createAttachmentInCrm(ticketRes.FullPath, new Guid(eventRegistrationGuid), false);
            }
            //create file list to attach in email
            List<string> filePathList = new List<string>();
            var attachmaneFolder = _config.GetValue<string>("attachmaneFolder");
            filePathList.Add(attachmaneFolder + PdfRes.Name);
            var ticketFolder = _config.GetValue<string>("ticketFolder");
            filePathList.Add(ticketFolder + ticketRes.Name);
            //פרטים לגוף המייל
            confirmationDetails confirmationDetails = new confirmationDetails();
            confirmationDetails.email = request.ContactEmail;
            confirmationDetails.reservationDate = request.ActivityDayList[0].Date;
            confirmationDetails.arrivalTime = request.ActivityDayList[0].HouresFromTo;
            confirmationDetails.name = request.ContactName;
            confirmationDetails.reservationNumber = request.OrderNum;
            confirmationDetails.tourName = request.tourName;
            confirmationDetails.agenName = request.InstitutionName;
            confirmationDetails.isEvent = true;
            confirmationDetails.creditCard4Digit = digit4;
            confirmationDetails.creditCardBrand = cardBrand;
            confirmationDetails.totalPriceAfterDiscount = request.TotalPriceAfterDiscount.ToString();
            confirmationDetails.isNeedEvent = true;


            var mailRes = await sendEmail(confirmationDetails, request.ContactName, filePathList, request.DocLang);
            if (mailRes)
            {
                return true;
            }
            else
            {
                return false;
            }
            //OnlineReservation rsv = GetOnlineReservation(rsvGuid, eventRegistrationEntityNumber);
            // צריכים ליצור פונקציה של קבלת רישום לארוע כדי לשלוח ליצירת PDF
            //return await confirmation(rsv, rsvGuid);

        }
        public async Task<bool> confirmEventRegistrationWithoutPay(string eventRegistrationGuid, string? couponGuid = null, decimal? totalPriceAfterDiscount = null)
        {
            try
            {
                Log.Information($"confirmEventRegistrationWithoutPay start, eventRegistrationGuid: {eventRegistrationGuid}");
                var rsvGuid = Guid.Parse(eventRegistrationGuid);
                //3.get event registration details from crm
                var eventRegistrationEntity = _crmService.Retrieve("new_event_registration", rsvGuid, new[] { "new_num" });
                var eventRegistrationEntityNumber = eventRegistrationEntity.GetAttributeValue<string>("new_num");
                Guid? CouponGuid = !string.IsNullOrEmpty(couponGuid)
                ? Guid.Parse(couponGuid)
                : (Guid?)null;
                UpdateStatusEventRegistration(new Guid(eventRegistrationGuid), true, CouponGuid, totalPriceAfterDiscount);
                Log.Information($"confirmEventRegistrationWithoutPay end before confirmationEventRegistration");
                return await confirmationEventRegistration(eventRegistrationGuid);
            }
            catch (Exception ex)
            {
                Log.Error($"confirmEventRegistrationWithoutPay error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> confirmationReservationWithPayment(ResultData data, string reservationGuid, string? couponGuid = null, decimal? totalPriceAfterDiscount = null)
        {
            try
            {
                Log.Information($"confirmationReservationWithPayment start, reservationGuid: {reservationGuid}");
                Log.Information($"data: {JsonConvert.SerializeObject(data)} ");
                var rsvGuid = Guid.Parse(reservationGuid);
                Guid? CouponGuid = !string.IsNullOrEmpty(couponGuid)
                 ? Guid.Parse(couponGuid)
                 : (Guid?)null;
                var reservationEntity = _crmService.Retrieve("new_reservation", rsvGuid, new[] { "new_num" });
                var reservationNumber = reservationEntity.GetAttributeValue<string>("new_num");

                var ReservationNumberGuid = new ReservationNumberGuid();
                ReservationNumberGuid.ReservationGuid = reservationGuid;
                ReservationNumberGuid.ReservationNumber = reservationNumber;
                OnlineReservation rsv = GetOnlineReservation(rsvGuid, true);
                Log.Information($" rsv: {JsonConvert.SerializeObject(rsv)} ");
                CreatePaymentInCRM(data, rsv);
                UpdateReservationAfterPayment(ReservationNumberGuid);

                UpdateReservationToActive(rsvGuid, true, CouponGuid, totalPriceAfterDiscount);
                //עבור חבילות שהארועים בפנים אם הפכו לINACTIVE יחזרו להיות פעילים
                if (rsv.TourType.BasicDetails.isPackage)
                {
                    SetEventRegistartionStatus(reservationGuid.ToString(), true, 0);
                }
                Log.Information("confirmationReservationWithPayment end before confirmation");
                return await confirmation(rsv, rsvGuid, data.CreditCardNumber.Substring(data.CreditCardNumber.Length - 4), data.CreditCardBrand);

                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Log.Error($"confirmationReservationWithPayment error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public void CreatePaymentInCRMEventRegistration(ResultData responseData, string eventRegistrationId)
        {
            try
            {
                Log.Information($"CreatePaymentInCRMEventRegistration start, responseData: {responseData}, eventRegistrationId: {eventRegistrationId}");
                OnlineReservation rsv = new OnlineReservation();
                var eventRegistrationEntity = getEventRegistration(eventRegistrationId);
                if (eventRegistrationEntity != null)
                {
                    var aliasedLang = eventRegistrationEntity.GetAttributeValue<AliasedValue>("event.new_language");
                    if (aliasedLang?.Value is EntityReference langReference)
                    {
                        var visitLangGuid = langReference.Id;
                        var langEntity = _crmService.Retrieve("new_language", visitLangGuid, true);
                        rsv.GuideLanguage = new KeyValuePair<string, string>(langEntity.Id.ToString(), langEntity.GetAttributeValue<string>("new_name"));
                    }
                    else
                    {
                        rsv.GuideLanguage = new KeyValuePair<string, string>
                            (eventRegistrationEntity.GetAttributeValue<EntityReference>("new_form_language").Id.ToString(), eventRegistrationEntity.GetAttributeValue<EntityReference>("new_form_language").Name);
                    }
                    rsv.Guid = eventRegistrationId;
                    rsv.TotalPriceAfterDiscount = (int)(eventRegistrationEntity.GetAttributeValue<Money>("new_total_price_after_discount_base").Value);
                    rsv.ReservationNumber = eventRegistrationEntity.GetAttributeValue<string>("new_num");
                    ////contact
                    rsv.LastName = eventRegistrationEntity.GetAttributeValue<string>("new_lastname_contact");
                    rsv.ContactGuid = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_contact").Id.ToString();
                    var countryRef = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_country");
                    if (countryRef != null)
                    {
                        rsv.Country = new KeyValuePair<string, string>(countryRef.Id.ToString(), countryRef.Name);
                    }

                    rsv.Email = eventRegistrationEntity.GetAttributeValue<string>("new_email_contact");
                    rsv.Phone = eventRegistrationEntity.GetAttributeValue<string>("new_mobile_contact");
                    rsv.FirstName = eventRegistrationEntity.GetAttributeValue<string>("new_firstname_contact");
                    rsv.NumberOfVisitors = eventRegistrationEntity.GetAttributeValue<int>("new_number_of_participants");
                    ////activity day
                    var aliasedValue = eventRegistrationEntity.GetAttributeValue<AliasedValue>("ead.new_activity_day");
                    rsv.TourType = new OnlineTourType();
                    rsv.TourType.BasicDetails = new BasicTourType();
                    rsv.TourType.BasicDetails.Id = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_event_reservations").Id.ToString();
                    string tourName = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_event_reservations").Name.ToString();

                    //if (aliasedValue?.Value is EntityReference entity)
                    //{
                    //    Guid activityId = entity.Id; // ID של ה-Entity
                    //    var activityDayEntity = _crmService.Retrieve("new_activity_day", activityId, true);
                    //}
                    //else
                    //{
                    //    Console.WriteLine("Value is not an Entity or is null.");
                    //}
                    //bidItems
                    List<Condition> conditionList = new List<Condition>();
                    var bidItemsCondition = new Condition("new_event_registration", "eq", eventRegistrationId);
                    conditionList.Add(bidItemsCondition);
                    var bidItems = GetBidItems(conditionList, eventRegistrationEntity.GetAttributeValue<EntityReference>("new_form_language").Name);
                    rsv.BidItems = bidItems;
                    Log.Information($" event registration : {JsonConvert.SerializeObject(rsv)} ");
                    CreatePaymentInCRM(responseData, rsv, true, tourName);
                    Log.Information("CreatePaymentInCRMEventRegistration end");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreatePaymentInCRMEventRegistration error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> ConfirmEventRegistrationWithPayment(ResultData responseData, string eventRegistrationGuid, string? couponGuid = null, decimal? totalPriceAfterDiscount = null)
        {
            try
            {
                Log.Information($"ConfirmEventRegistrationWithPayment start, responseData: {responseData}, eventRegistrationGuid:{eventRegistrationGuid}");
                Log.Information($" data: {JsonConvert.SerializeObject(responseData)} ");
                var rsvGuid = Guid.Parse(eventRegistrationGuid);
                Guid? CouponGuid = !string.IsNullOrEmpty(couponGuid)
                 ? Guid.Parse(couponGuid)
                 : (Guid?)null;
                var eventRegistrationEntity = _crmService.Retrieve("new_event_registration", rsvGuid, new[] { "new_num" });
                var eventRegistrationNumber = eventRegistrationEntity.GetAttributeValue<string>("new_num");

                var ReservationNumberGuid = new ReservationNumberGuid();
                ReservationNumberGuid.ReservationGuid = eventRegistrationGuid;
                ReservationNumberGuid.ReservationNumber = eventRegistrationNumber;

                //OnlineReservation rsv = GetOnlineReservation(rsvGuid, eventRegistrationNumber);

                CreatePaymentInCRMEventRegistration(responseData, eventRegistrationGuid);
                UpdateStatusEventRegistration(rsvGuid, false, CouponGuid, totalPriceAfterDiscount);
                Log.Information($"ConfirmEventRegistrationWithPayment end before confirmationEventRegistration");
                return await confirmationEventRegistration(eventRegistrationGuid, responseData.CreditCardNumber.Substring(responseData.CreditCardNumber.Length - 4), responseData.CreditCardBrand);

                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Log.Error($"ConfirmEventRegistrationWithPayment error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public ReservationNumberGuid CreateEventRegistration(OnlineReservation eventRegistration, bool isForUpdate, Entity tourType, string reservationGuid = "")
        {
            try
            {
                Log.Information($"CreateEventRegistration start, eventRegistration:{eventRegistration} , isForUpdate: {isForUpdate}, reservationGuid: {reservationGuid} ");
                //בארוע רגיל שולפים פה ובחבילות מביאים את החבילה הפנימית מבחוץ
                if (tourType == null)
                {
                    tourType = GetTourByCode(eventRegistration.TourType.BasicDetails.TourCode);
                }
                ReservationNumberGuid reservationNumberGuid = new ReservationNumberGuid();
                string format = "dd/MM/yyyy"; // ציון הפורמט המתאים
                eventRegistration.ReservationDate = DateTime.ParseExact(eventRegistration.reservationDateString, format, null);

                Log.Information($"The eventRegistration date is: {eventRegistration.ReservationDate}");
                if (eventRegistration.StartTime != null && eventRegistration.ReservationDate != null)
                {
                    string startTime = eventRegistration.StartTime.hours.ToString() + ":" + eventRegistration.StartTime.minutes.ToString();
                    eventRegistration.StartDateTime = new DateTime(eventRegistration.ReservationDate.Value.Year, eventRegistration.ReservationDate.Value.Month, eventRegistration.ReservationDate.Value.Day, eventRegistration.StartTime.hours, eventRegistration.StartTime.minutes, 0);
                }
                //Checks if there is really still free space left
                var activityDayTodayList = _newReservationService.GetActivityDayListByEventReservation(eventRegistration.TourType.EventReservationId, eventRegistration.StartDateTime.Value);
                var activityDay = activityDayTodayList.Entities.ToList().Find(activityDay => activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime() == eventRegistration.StartDateTime);
                if (activityDay != null)
                {
                    if (activityDay.GetAttributeValue<int>("new_actual_number_of_participants") + eventRegistration.NumberOfVisitors > (int)tourType.GetAttributeValue<AliasedValue>("eventReservation.new_max_participants_per_event").Value)
                    {
                        throw new Exception("NO_ROON");
                    }
                }

                if (eventRegistration.StartTime != null)
                {
                    string startTime = eventRegistration.StartTime.hours.ToString() + ":" + eventRegistration.StartTime.minutes.ToString();
                    eventRegistration.StartDateTime = new DateTime(eventRegistration.ReservationDate.Value.Year, eventRegistration.ReservationDate.Value.Month, eventRegistration.ReservationDate.Value.Day, eventRegistration.StartTime.hours, eventRegistration.StartTime.minutes, 0);
                }
                var isNeetToSetToActive = false;

                var contactGuid = _helperService.GetContactByEmailOrMobile(eventRegistration.Email, eventRegistration.Phone);
                AdditionalTourDetails additionalTourDetails = new AdditionalTourDetails();
                additionalTourDetails.FirstName = eventRegistration.FirstName;
                additionalTourDetails.LastName = eventRegistration.LastName;
                additionalTourDetails.Email = eventRegistration.Email;
                additionalTourDetails.PhoneNumber = eventRegistration.Phone;
                additionalTourDetails.isReceivingNewsletterMessages = eventRegistration.IsSubscribe;
                contactGuid = _newReservationRepository.CreateOrUpdateContact(contactGuid, additionalTourDetails);
                eventRegistration.ContactGuid = contactGuid.ToString();

                var eventRegistrationGuid = CreateOrUpdateEventRegistration(eventRegistration, isNeetToSetToActive, reservationGuid);
                eventRegistration.Guid = eventRegistrationGuid.ToString();
                var newReservationEntity = _crmService.Retrieve("new_event_registration", eventRegistrationGuid, new[] { "new_num" });
                eventRegistration.ReservationNumber = newReservationEntity.GetAttributeValue<string>("new_num");
                reservationNumberGuid.ReservationGuid = eventRegistrationGuid.ToString();
                reservationNumberGuid.ReservationNumber = eventRegistration.ReservationNumber;
                if (eventRegistration.TourType.BasicDetails.IsNeedEvent && !isForUpdate)
                {
                    eventRegistration.ActivityDayEventRegistrationGuid = CreateOrUpdateActivityDayEventRegistration(eventRegistration, false).ToString();
                }
                Log.Information($"CreateEventRegistration end, reservationNumberGuid:{reservationNumberGuid}");
                return reservationNumberGuid;
            }
            catch (Exception ex)
            {
                Log.Error($"CreateEventRegistration error: {ex.Message}");
                throw new Exception(ex.Message);
            }

        }

        public Guid CreateOrUpdateEventRegistration(OnlineReservation eventRegistration, bool isNeetToSetToActive, string reservationGuid = "")
        {
            try
            {
                Log.Information($"CreateOrUpdateEventRegistration start, eventRegistration: {eventRegistration}, isNeetToSetToActive:{isNeetToSetToActive}, reservationGuid: {reservationGuid}");

                if (isNeetToSetToActive)
                {
                    _crmRepository.SetActiveAfterPayment("new_event_registration", Guid.Parse(eventRegistration.Guid));
                }
                Entity eventRegistrationEntity = string.IsNullOrEmpty(eventRegistration.Guid) ? new Entity("new_event_registration") : new Entity("new_event_registration", Guid.Parse(eventRegistration.Guid));
                if (eventRegistration.TotalPriceAfterDiscount > 0)
                {
                    eventRegistrationEntity["new_status"] = new OptionSetValue(8);// 9 ממתין לתשלום 8 , תשלום בקופה
                    eventRegistrationEntity["new_payment_status"] = new OptionSetValue(2);//טרם שולם
                    if (eventRegistration.TourType.BasicDetails.isPackage)
                    {
                        eventRegistrationEntity["new_payment_status"] = new OptionSetValue(18);//לחיוב בחבילה
                    }
                }
                else
                {
                    eventRegistrationEntity["new_status"] = new OptionSetValue(1);//מאושר
                    eventRegistrationEntity["new_payment_status"] = new OptionSetValue(9);// אין צורך בתשלום
                }
                Log.Information($"eventRegistration.TourType.BasicDetails.TourCode: {eventRegistration.TourType.BasicDetails.TourCode}");

                //if (eventRegistration.TourType.BasicDetails != null)
                //{
                //    var tourType = GetTourByCode(eventRegistration.TourType.BasicDetails.TourCode);
                //    if (tourType.GetAttributeValue<OptionSetValue>("new_division_router").Value != null)
                //    {
                //        eventRegistrationEntity["new_division_router"] = new OptionSetValue(tourType.GetAttributeValue<OptionSetValue>("new_division_router").Value);
                //    }
                //    else
                //    {
                //        eventRegistrationEntity["new_division_router"] = new OptionSetValue(1);//בי"ס

                //    }
                //}
                //else
                //{
                eventRegistrationEntity["new_division_router"] = new OptionSetValue(1);//בי"ס
                //}
                eventRegistrationEntity["new_contact"] = new EntityReference("contact", Guid.Parse(eventRegistration.ContactGuid));
                if (reservationGuid != "")
                {
                    eventRegistrationEntity["new_reservation"] = new EntityReference("new_reservation", Guid.Parse(reservationGuid));
                }
                eventRegistrationEntity["new_firstname_contact"] = eventRegistration.FirstName;
                eventRegistrationEntity["new_lastname_contact"] = eventRegistration.LastName;
                eventRegistrationEntity["new_mobile_contact"] = eventRegistration.Phone;
                var lang = eventRegistration.FormLang;
                var formLang = _config.GetValue<string>($"Lang-{lang}-Guid")!;
                if (formLang != "" && formLang != null) //FormLang
                {
                    eventRegistrationEntity["new_form_language"] = new EntityReference("new_language", Guid.Parse(formLang));
                }
                eventRegistrationEntity["new_email_contact"] = eventRegistration.Email;
                eventRegistrationEntity["new_name"] = "רישום לאירוע --> " + eventRegistration.LastName + " " + eventRegistration.FirstName;
                Money amount = new Money();
                amount.Value = Convert.ToDecimal(eventRegistration.TotalPriceAfterDiscount);
                eventRegistrationEntity["new_total_price_after_discount"] = amount;
                var currencyNIS = _config.GetValue<Guid>("CurrencyNIS");
                eventRegistrationEntity["transactioncurrencyid"] = new EntityReference("transactioncurrency", currencyNIS);
                if (eventRegistration.NumVisitorsTeensAges != null)
                {
                    eventRegistrationEntity["new_num_of_young_participants"] = eventRegistration.NumVisitorsTeensAges;
                }
                eventRegistrationEntity["new_group_name"] = eventRegistration.ReservationName;
                eventRegistrationEntity["new_event_reservations"] = new EntityReference("new_event_reservations", Guid.Parse(eventRegistration.TourType.EventReservationId));
                eventRegistrationEntity["new_number_of_participants"] = eventRegistration.NumberOfVisitors;
                eventRegistrationEntity["new_is_from_reservations"] = true;
                eventRegistrationEntity["new_is_package"] = eventRegistration.TourType.BasicDetails.isPackage;
                if (eventRegistration.Institution != null)
                {
                    if (Guid.Parse(eventRegistration.Institution.Id) != Guid.Empty)//מוסד או סוכן נסיעות
                    {
                        eventRegistrationEntity["new_rsv_institution"] = new EntityReference("new_rsv_institution", Guid.Parse(eventRegistration.Institution.Id));
                    }
                }

                Guid countryGuid = _onlineReservationRepository.GetGuidByCountryCode(eventRegistration.Country?.Key);
                if (countryGuid != Guid.Empty)
                {
                    eventRegistrationEntity["new_country"] = new EntityReference("new_country", countryGuid);
                }

                if (string.IsNullOrEmpty(eventRegistration.Guid))
                {
                    Guid guid = _crmService.Create(eventRegistrationEntity);
                    eventRegistration.Guid = guid.ToString();
                }
                else
                {
                    _crmService.Update(eventRegistrationEntity);
                    Guid.Parse(eventRegistration.Guid);
                }
                int bidNum = 0;
                if (eventRegistration.BidItems != null)
                {
                    foreach (var bidItem in eventRegistration.BidItems)
                    {
                        if (bidItem.Amount > 0)
                        {
                            bidNum++;
                            _onlineReservationRepository.CreateBidItem(eventRegistration, new Guid(), Guid.Parse(eventRegistration.Guid), bidItem, bidNum, false);
                        }
                    }
                }
                Log.Information($"CreateOrUpdateEventRegistration end, eventRegistrationGuid: {eventRegistration.Guid}");
                return Guid.Parse(eventRegistration.Guid);
            }
            catch (Exception ex)
            {
                Log.Error($"CreateOrUpdateEventRegistration error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        private Guid CreateOrUpdateActivityDayEventRegistration(OnlineReservation eventRegistration, bool isNeetToSetToActive)
        {
            var activityDayGuid = GetActivityDayIdCreateActivityDayIfNotExists(eventRegistration);
            Guid eventRegistrationActivityDayGuid = _onlineReservationRepository.CreateOrUpdateActivityDayEventRegistration(eventRegistration, activityDayGuid, isNeetToSetToActive);
            return eventRegistrationActivityDayGuid;
        }
        //private void UpdateBidItemInEventRegistration(OnlineReservation eventRegistration, Guid activityDayGuid)
        //{

        //    var bidItems = GetBidItemsByEventRegistration(eventRegistration.Guid);
        //    for(int i = 0; i < bidItems.Entities.Count; i++)
        //    {
        //        Entity bidLine = new Entity("new_bid", bidItems.Entities[i].Id);
        //        bidLine["new_activity_day"] = new EntityReference("new_activity_day", activityDayGuid);
        //        _crmService.Update(bidLine);


        //    }

        //}
        //private EntityCollection GetBidItemsByEventRegistration(string eventRegistration)
        //{
        //    try
        //    {
        //        var fetchXmlBidItems = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
        //                 <entity name=""new_bid"">
        //                 <attribute name=""new_name""/>
        //                 <attribute name=""createdon""/>
        //                 <attribute name=""new_language""/>
        //                 <attribute name=""new_discount""/>
        //                 <attribute name=""new_discount_type""/>
        //                 <attribute name=""new_total_price_after_discount""/>
        //                 <attribute name=""new_total_price""/>
        //                 <attribute name=""new_price""/>
        //                 <attribute name=""new_yv_product""/>
        //                 <attribute name=""new_amount""/>
        //                 <attribute name=""new_bid_item_number""/>
        //                 <attribute name=""new_total_price_after_discount_base""/>
        //                 <attribute name=""new_price_base""/>
        //                 <attribute name=""new_comments""/>
        //                 <attribute name=""new_ticket_type""/>
        //                 <attribute name=""new_bidid""/>
        //                 <order attribute=""new_name"" descending=""false""/>
        //                 <filter type=""and"">
        //                    <condition attribute=""new_event_registration"" operator=""eq"" value=""{eventRegistration}""/>
        //                </filter>

        //        </entity>
        //        </fetch>";
        //        var query1 = new FetchExpression(fetchXmlBidItems);
        //        return _crmService.RetrieveMultiple(query1);
        //    }
        //    catch(Exception ex)
        //    {
        //        Log.Error($"GetBidItemsByEventRegistration error: {ex.Message}");
        //        throw new Exception(ex.Message);
        //    }
        //}

        private Guid GetActivityDayIdCreateActivityDayIfNotExists(OnlineReservation eventRegistration)
        {
            try
            {
                Log.Information($"GetActivityDayIdCreateActivityDayIfNotExists start, eventRegistration:{eventRegistration}");
                var eventReservationEntity = _crmService.Retrieve("new_event_reservations", new Guid(eventRegistration.TourType.EventReservationId), new[] { "new_max_participants_per_event", "new_total_participants" });
                var maxVisitors = eventReservationEntity.GetAttributeValue<int>("new_max_participants_per_event");
                var activityDayTodayList = _newReservationService.GetActivityDayListByEventReservation(eventRegistration.TourType.EventReservationId, eventRegistration.StartDateTime.Value);
                var entity = activityDayTodayList.Entities.ToList().Find(activityDay => activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime() == eventRegistration.StartDateTime);
                var activityDayGuid = Guid.Empty;
                if (entity != null)
                {
                    //var sumOfParticipants = entity.GetAttributeValue<int>("new_number_of_participants") + eventRegistration.NumberOfVisitors;
                    //if (!string.IsNullOrEmpty(eventRegistration.ActivityDayEventRegistrationGuid) && !string.IsNullOrEmpty(eventRegistration.Guid))
                    //{
                    //    var activityDayEventRegistrationEntity = _crmService.Retrieve("new_event_registration_activity_day", Guid.Parse(eventRegistration.ActivityDayEventRegistrationGuid), new string[] { "new_activity_day" });
                    //    activityDayGuid = activityDayEventRegistrationEntity.GetAttributeValue<EntityReference>("new_activity_day").Id;
                    //    if (entity.Id.Equals(activityDayGuid))
                    //    {
                    //        var eventRegistrationEntity = _crmService.Retrieve("new_event_registration", Guid.Parse(eventRegistration.Guid), new string[] { "new_number_of_participants" });
                    //        sumOfParticipants -= eventRegistrationEntity.GetAttributeValue<int>("new_number_of_participants");
                    //    }
                    //}
                    //if (sumOfParticipants > maxVisitors)
                    //{
                    //    throw new Exception("NO_ROOM");
                    //}
                    //Log.Information($"GetActivityDayIdCreateActivityDayIfNotExists activityDay exists, activityDayGuid:{activityDayGuid}");
                    //activityDayGuid = entity.Id;
                    return entity.Id;
                }
                else
                {
                    //create activity day
                    activityDayGuid = _onlineReservationRepository.CreateActivityDay(eventRegistration, GetTourByCode(eventRegistration.TourType.BasicDetails.TourCode), false, 1);
                    Log.Information($"GetActivityDayIdCreateActivityDayIfNotExists create activityDay activityDayGuid: {activityDayGuid}");
                }
                return activityDayGuid;
            }
            catch (Exception ex)
            {
                Log.Error($"GetActivityDayIdCreateActivityDayIfNotExists error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public string convertDateTimeToString(DateTime? dateTime)
        {
            string day = dateTime?.Day.ToString("D2");  // דאג שהיום יהיה תמיד עם 2 ספרות
            string month = dateTime?.Month.ToString("D2");  // דאג שהחודש יהיה תמיד עם 2 ספרות
            string year = dateTime?.Year.ToString();  // שנה (4 ספרות)

            // מחזיר את התאריך במבנה dd/MM/yyyy
            return $"{day}/{month}/{year}";
        }
        public confirmationDetails ReservationDetailsConfirmation(string reservationId, string type)
        {
            try
            {
                string editLinkConfig = _config.GetValue<string>("EditReservationLink");
                string cancelLinkConfig = _config.GetValue<string>("CancelReservationLink");
                confirmationDetails confirmationDetails = new confirmationDetails();
                if (type == "event")
                {

                    var eventRegistrationEntity = getEventRegistration(reservationId);

                    if (eventRegistrationEntity != null)
                    {
                        confirmationDetails.reservationNumber = eventRegistrationEntity.GetAttributeValue<string>("new_num");
                        confirmationDetails.isEvent = true;
                        confirmationDetails.email = eventRegistrationEntity.GetAttributeValue<string>("new_email_contact");
                        confirmationDetails.totalPriceAfterDiscount = eventRegistrationEntity.GetAttributeValue<Money>("new_total_price_after_discount")?.Value.ToString();
                        var aliasedValue = eventRegistrationEntity.GetAttributeValue<AliasedValue>("ead.new_activity_day");
                        if (aliasedValue?.Value is EntityReference entity)
                        {
                            Guid activityId = entity.Id; // ID של ה-Entity
                            var activityDayEntity = _crmService.Retrieve("new_activity_day", activityId, true);
                            var eventId = eventRegistrationEntity.GetAttributeValue<EntityReference>("new_event_reservations").Id.ToString();
                            var tourEntity = GetTourByEvent(eventId);
                            if (eventRegistrationEntity.GetAttributeValue<EntityReference>("new_form_language").Name == "עברית")
                            {
                                confirmationDetails.tourName = tourEntity.GetAttributeValue<string>("new_name");
                                editLinkConfig = _config.GetValue<string>("EditReservationLink_he");
                                cancelLinkConfig = _config.GetValue<string>("CancelReservationLink_he");
                            }
                            else
                            {
                                confirmationDetails.tourName = tourEntity.GetAttributeValue<string>("new_name_en");

                            }
                            confirmationDetails.reservationDate = convertDateTimeToString(activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime());
                            confirmationDetails.arrivalTime = activityDayEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().ToString("HH:mm");

                        }
                        //in event registration there is no edit...
                        //  confirmationDetails.editLink = editLinkConfig
                        //.Replace("{reservationNumber}", confirmationDetails.reservationNumber)
                        //.Replace("{email}", confirmationDetails.email)
                        //.Replace("{tourDate}", confirmationDetails.reservationDate);
                        confirmationDetails.cancelLink = cancelLinkConfig
                        .Replace("{reservationNumber}", confirmationDetails.reservationNumber)
                        .Replace("{email}", confirmationDetails.email);
                    }
                }
                else
                {
                    var onlineRSVEntity = _crmService.Retrieve("new_reservation", new Guid(reservationId), true);
                    if (onlineRSVEntity != null)
                    {
                        confirmationDetails.isEvent = false;
                        confirmationDetails.email = onlineRSVEntity.GetAttributeValue<string>("new_email_contact");
                        confirmationDetails.reservationNumber = onlineRSVEntity.GetAttributeValue<string>("new_num");
                        confirmationDetails.totalPriceAfterDiscount = onlineRSVEntity.GetAttributeValue<Money>("new_total_price_after_discount")?.Value.ToString();
                        var activityDayFirstDate = onlineRSVEntity.GetAttributeValue<DateTime?>("new_activity_day_first_date");
                        confirmationDetails.reservationDate = activityDayFirstDate.HasValue ? convertDateTimeToString(activityDayFirstDate.Value.ToLocalTime()) : "";
                        var activityDay = GetActivityDate(reservationId);
                        var startTime = activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime();
                        if (startTime != null)
                        {
                            confirmationDetails.arrivalTime = startTime.Hour + ":" + startTime.Minute;

                        }
                        confirmationDetails.arrivalTime = startTime != null ? startTime.ToString("HH:mm") : "";
                        var tourTypeEntity = _crmService.Retrieve("new_tour_type", onlineRSVEntity.GetAttributeValue<EntityReference>("new_tour_type_internet").Id, true);
                        if (onlineRSVEntity.GetAttributeValue<EntityReference>("new_form_language").Name == "עברית")
                        {
                            confirmationDetails.tourName = tourTypeEntity.GetAttributeValue<string>("new_name");
                            editLinkConfig = _config.GetValue<string>("EditReservationLink_he");
                            cancelLinkConfig = _config.GetValue<string>("CancelReservationLink_he");
                        }
                        else
                        {
                            confirmationDetails.tourName = tourTypeEntity.GetAttributeValue<string>("new_name_en");

                        }
                        confirmationDetails.editLink = editLinkConfig
                         .Replace("{reservationNumber}", confirmationDetails.reservationNumber)
                         .Replace("{email}", confirmationDetails.email)
                         .Replace("{tourDate}", confirmationDetails.reservationDate);
                        confirmationDetails.cancelLink = cancelLinkConfig
                        .Replace("{reservationNumber}", confirmationDetails.reservationNumber)
                        .Replace("{email}", confirmationDetails.email);
                    }
                }


                return confirmationDetails;
            }
            catch (Exception ex)
            {
                Log.Error($"ReservationDetailsConfirmation error: {ex.Message}");
                throw new Exception(ex.Message);
            }

        }

        public string GenerateQRCode(QRCodeItem qRcodeDetails)
        {
            try
            {
                Log.Information("start GenerateQRCode details: " + @qRcodeDetails);
                var json = JsonConvert.SerializeObject(qRcodeDetails);
                // Create a QR code writer
                BarcodeWriter writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = 200,
                        Width = 200
                    }
                };

                // Encode the JSON string as a QR code
                Bitmap qrCode = writer.Write(json);

                var urlToSvae = qRcodeDetails.Guid + "_qrcode.png";
                // Save the QR code as an image file 
                qrCode.Save(urlToSvae);

                Log.Information("end GenerateQRCode url is: " + urlToSvae);

                return urlToSvae;

            }
            catch (Exception ex)
            {


                Log.Error("Error!!!faild to generateQRCode: " + ex.Message);

                return null;

            }


            // var bitmap = new Bitmap("qrcode.png");

            //// Decoding the barcode
            //var reader = new BarcodeReader();
            //var result = reader.Decode(bitmap);

            //var a = JsonConvert.DeserializeObject(result.Text);

            //// Printing the result
            //if (result != null)
            //{
            //    Console.WriteLine("QR code content: " + result.Text);
            //}
            //else
            //{
            //    Console.WriteLine("QR code could not be decoded");
            //}


        }

        public bool IsFriday(string dateString)
        {
            Log.Information($"IsFriday start date:{dateString}");
            // פורמט התאריך: "dd/MM/yyyy"
            string format = "dd/MM/yyyy";

            // מנסים להמיר את המחרוזת לתאריך
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                // בדיקה אם היום הוא שישי
                return date.DayOfWeek == DayOfWeek.Friday;
            }

            // אם לא הצלחנו להמיר את המחרוזת לתאריך, נזרוק שגיאה או נחזיר false
            Log.Error($"IsFriday Error date:{dateString} not good date");

            return false;

        }

        private List<OnlineTourType> GetTourTypesByTourType(string tourCode, string lang)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "tourType", false, true);
                var res = new List<OnlineTourType>();
                fetchXml += $@"<attribute name=""new_name""/>                                    
                            <order attribute=""new_name"" descending=""false""/>
                            <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}"" />
                            </filter>
                            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_type_first"" alias=""first"" link-type='outer'>
                                <attribute name=""new_tour_code""/>  
                            </link-entity>
                            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_type_second"" alias=""second"" link-type='outer'>
                                <attribute name=""new_tour_code""/>  
                            </link-entity>
                            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_type_third"" alias=""third"" link-type='outer'>
                                <attribute name=""new_tour_code""/>   
                            </link-entity>
                            <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_type_fourth"" alias=""fourth"" link-type='outer'>
                                <attribute name=""new_tour_code""/>    
                            </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var tourTypeList = new List<OnlineTourType>();
                var query = new FetchExpression(fetchXml);
                var tourTypeEntity = _crmService.RetrieveMultiple(query);
                if (tourTypeEntity.Entities.Count > 0)
                {
                    if (tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("first.new_tour_code") != null)
                    {
                        res.Add(GetTourTypeBasicDetailsToPackage((string)tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("first.new_tour_code").Value, lang));
                    }
                    if (tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("second.new_tour_code") != null)
                    {
                        res.Add(GetTourTypeBasicDetailsToPackage((string)tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("second.new_tour_code").Value, lang));
                    }
                    if (tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("third.new_tour_code") != null)
                    {
                        res.Add(GetTourTypeBasicDetailsToPackage((string)tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("third.new_tour_code").Value, lang));
                    }
                    if (tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("fourth.new_tour_code") != null)
                    {
                        res.Add(GetTourTypeBasicDetailsToPackage((string)tourTypeEntity.Entities.First().GetAttributeValue<AliasedValue>("fourth.new_tour_code").Value, lang));
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTagsByTourType error: {ex.Message}");
                throw new Exception(ex.Message);

            }
        }

        private List<OnlineTourType> GetOptionalTourTypesByTourType(string tourCode, string lang)
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "tourType", false, true);
                fetchXml += $@"<attribute name=""new_name""/>שם בעברית
                                <filter type='and'>
                                    <condition attribute='statecode' operator= 'eq' value = '0'/>
                                    <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}"" />
                                </filter>
                                <link-entity name='new_new_tour_type_new_tour_type_optional' from='new_tour_typeidone' to='new_tour_typeid' visible='false' intersect='true'>         
                                    <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeidtwo' alias='tourType'>
                                    <attribute name=""new_name""/>שם בעברית
                                    <attribute name=""new_name_en""/>שם באנגלית
                                    <attribute name=""new_description_{lang}""/>הסבר אנגלית
                                    <attribute name=""new_short_desc_{lang}""/>הסבר מקוצר עברית
                                    <attribute name=""new_tour_code""/>קוד סיור  
                                    <attribute name=""new_age""/>מתאים לגיל
                                    <attribute name=""new_tag""/> תגית
                                    <attribute name=""new_duration_of_hours""/>משך שעות
                                    <attribute name=""new_relevant_day_sunday""/>
                                    <attribute name=""new_relevant_day_monday""/>
                                    <attribute name=""new_relevant_day_tuesday""/>
                                    <attribute name=""new_target_population""/>
                                    <attribute name=""new_grade_1""/>
                                    <attribute name=""new_grade_2""/>
                                    <attribute name=""new_grade_3""/>
                                    <attribute name=""new_grade_4""/>
                                    <attribute name=""new_grade_5""/>
                                    <attribute name=""new_grade_6""/>
                                    <attribute name=""new_grade_7""/>
                                    <attribute name=""new_grade_8""/>
                                    <attribute name=""new_grade_9""/>
                                    <attribute name=""new_grade_10""/>
                                    <attribute name=""new_grade_11""/>
                                    <attribute name=""new_grade_12""/>
                                    <attribute name=""new_relevant_day_wednesday""/>
                                    <attribute name=""new_relevant_day_thursday""/>
                                    <attribute name=""new_is_need_event""/>
                                    <attribute name=""new_relevant_day_friday""/>
                                    <attribute name=""new_max_participants""/>
                                    <attribute name=""new_max_participants_in_group""/>
                                    <attribute name=""new_tour_typeid""/>
                                    <attribute name=""new_difference_between_arrival_to_start""/>
                                    <attribute name=""new_sort_weight_{lang}""/>//משקל למיון
                                    <attribute name=""new_event_reservations""/>//ארוע-מערכת אחודה
                                    <order attribute=""new_sort_weight_{lang}"" descending=""false""/>
                                    <order attribute=""new_name"" descending=""false""/>
                                    <filter type='and'>
                                        <condition attribute='statecode' operator= 'eq' value = '0'/>        
                                    </filter>
                                    </link-entity>
                                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetTourTypeOfPachageList(fetchXml, lang);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTagsByTourType error: {ex.Message}");
                throw new Exception(ex.Message);

            }
        }

        private OnlineTourType GetTourTypeBasicDetailsToPackage(string tourCode, string lang)
        {
            var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType", false, true);
            fetchXml += @$" <attribute name=""new_name""/>שם בעברית
                            <attribute name=""new_name_en""/>שם באנגלית
                            <attribute name=""new_description_{lang}""/>הסבר אנגלית
                            <attribute name=""new_short_desc_{lang}""/>הסבר מקוצר עברית
                            <attribute name=""new_tour_code""/>קוד סיור  
                            <attribute name=""new_age""/>מתאים לגיל
                            <attribute name=""new_tag""/> תגית
                            <attribute name=""new_duration_of_hours""/>משך שעות
                            <attribute name=""new_relevant_day_sunday""/>
                            <attribute name=""new_relevant_day_monday""/>
                            <attribute name=""new_relevant_day_tuesday""/>
                            <attribute name=""new_target_population""/>
                    <attribute name=""new_category""/>

                            <attribute name=""new_grade_1""/>
                            <attribute name=""new_grade_2""/>
                            <attribute name=""new_grade_3""/>
                            <attribute name=""new_grade_4""/>
                            <attribute name=""new_grade_5""/>
                            <attribute name=""new_grade_6""/>
                            <attribute name=""new_grade_7""/>
                            <attribute name=""new_grade_8""/>
                            <attribute name=""new_grade_9""/>
                            <attribute name=""new_grade_10""/>
                            <attribute name=""new_grade_11""/>
                            <attribute name=""new_grade_12""/>
                            <attribute name=""new_relevant_day_wednesday""/>
                            <attribute name=""new_relevant_day_thursday""/>
                            <attribute name=""new_tab_to_display""/>
                            <attribute name=""new_is_need_event""/>
                            <attribute name=""new_relevant_day_friday""/>
                            <attribute name=""new_max_participants""/>
                            <attribute name=""new_max_participants_in_group""/>
                            <attribute name=""new_tour_typeid""/>
                            <attribute name=""new_difference_between_arrival_to_start""/>
                            <attribute name=""new_sort_weight_{lang}""/>//משקל למיון
                            <attribute name=""new_event_reservations""/>//ארוע-מערכת אחודה
                            <attribute name=""new_discount_type""/>//סוג הנחה
                            <attribute name=""new_discount""/>//סך הנחה
                            <attribute name=""new_is_package""/>//האם חבילה
                            <attribute name=""new_cancel_registration_fees""/>//בטל דמי הרשמה וטיפול
                            <order attribute=""new_sort_weight_{lang}"" descending=""false""/>
                            <order attribute=""new_name"" descending=""false""/>
                            <filter type=""and"">;
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>
                                <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>
                            </filter>";
            fetchXml += _newReservationRepository.EndFetchXml();
            var res = _onlineReservationRepository.GetTourTypeList(fetchXml, lang);
            res.ForEach(tourType =>
            {
                tourType.BasicDetails.GuidLanguages = GetLanguagesByTourType(tourType.BasicDetails.TourCode, lang);
                tourType.BasicDetails.Tags = GetTagsByTourType(tourType.BasicDetails.TourCode, lang);
            });
            return res.FirstOrDefault();
        }

        public Dictionary<string, List<StartTimeItem>> GetGetArrivalTimeTourPackage(string packageCode, string arrivalTime, string date, string lang, bool isEducationalGroup = false)
        {
            var package = GetTourByCode(packageCode);
            List<OnlineTourType> toursList = GetTourTypesByTourType(packageCode, lang);
            Dictionary<string, List<StartTimeItem>> ArrivalTimesList = new Dictionary<string, List<StartTimeItem>>();
            decimal durationAllPrevTours = 0;
            decimal durationAllfollowingTours = 0;//משך הפעילות הבאות
            toursList.ForEach(tour =>
            {
                durationAllfollowingTours += tour.BasicDetails.DurationActivity;
            });

            for (int i = 0; i < toursList.Count; i++)
            {
                Entity tourEntity = GetTourByCode(toursList[i].BasicDetails.TourCode);
                var ArrivalTimes = GetAvailableTimes(toursList[i].BasicDetails.TourCode, date, isEducationalGroup, tourEntity).Result;
                var activityDuration = toursList[i].BasicDetails.DurationActivity;
                durationAllfollowingTours -= activityDuration;//שיהיה רק משך פעיליות הבאות אחריו
                if (tourEntity.GetAttributeValue<EntityReference>("new_resource") != null)//אם יש משאב אז לוקחים את משך פעילות המשאב
                {
                    activityDuration = tourEntity.GetAttributeValue<decimal>("new_duration_of_hours_resource");
                }
                var differenceBetweenArrivalToStart = tourEntity.GetAttributeValue<decimal>("new_difference_between_arrival_to_start");
                ArrivalTimes = FilterAvailableTimes(ArrivalTimes, durationAllPrevTours, arrivalTime, durationAllfollowingTours, package.GetAttributeValue<decimal>("new_duration_of_hours"), activityDuration, differenceBetweenArrivalToStart);
                durationAllPrevTours += toursList[i].BasicDetails.DurationActivity;//עדכון משך הפעילות של סיורים קודמים
                ArrivalTimesList.Add(toursList[i].BasicDetails.TourCode, ArrivalTimes);
            }
            return ArrivalTimesList;
        }

        private List<StartTimeItem> FilterAvailableTimes(List<StartTimeItem> ArrivalTimes, decimal durationAllPrevTours, string PackageArrivalTime, decimal durationAllfollowingTours, decimal packageDuration, decimal activityDuration, decimal differenceBetweenArrivalToStart)
        {
            //    if (durationAllPrevTours==0)//אם זה פעילות ראשונה בחבילה
            //    {
            //        var packageArrivalTime = TimeSpan.Parse(PackageArrivalTime);
            //        return ArrivalTimes.Where(item =>
            //        {
            //            return TimeSpan.Parse(item.StartTime) == packageArrivalTime;//שעת ההגעה של פעילות ראשונה היר כמו שעת ההגעה של החבילה
            //        }).ToList();
            //    }
            TimeSpan relevantStartTime = TimeSpan.Parse(PackageArrivalTime) + TimeSpan.FromHours((double)durationAllPrevTours) + TimeSpan.FromHours((double)differenceBetweenArrivalToStart);
            TimeSpan relevantEndTime = TimeSpan.Parse(PackageArrivalTime) + TimeSpan.FromHours((double)packageDuration) - TimeSpan.FromHours((double)durationAllfollowingTours) - TimeSpan.FromHours((double)activityDuration);
            //סינון שעות ההגעה הרלוונטיות
            return ArrivalTimes.Where(item =>
            {
                TimeSpan time = TimeSpan.Parse(item.StartTime);
                return time >= relevantStartTime && time <= relevantEndTime; // בדיקה אם הזמן נמצא בטווח
            }).ToList();
        }
        public HashSet<string> GetTourWithSpeceficDate(int month, int year)
        {
            HashSet<string> tourTypesCode = new HashSet<string>();
            var date = new DateTime(year, month, 1);
            var dateString = date.ToString("yyyy-MM-dd");
            var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                <entity name=""new_spcific_date_and_hour"">
                                <attribute name=""new_date""/>
                                <attribute name=""new_end_time""/>
                                <attribute name=""new_start_time""/>
                                <attribute name=""new_spcific_date_and_hourid""/>
                                <order attribute=""new_start_time"" descending=""false""/>
                                <filter type=""and"">
                                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                <condition attribute=""new_start_time"" operator=""on"" value=""{dateString}""/>
                                </filter>
                                <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_type"" alias=""tourType"">
                                <attribute name=""new_tour_code""/>
                                <filter type=""and"">
                                    <condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>
                                </filter>
                                </link-entity>
                                </entity>
                            </fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            foreach (var item in res.Entities)
            {
                tourTypesCode.Add(item["new_tour_code"].ToString());

            }
            return tourTypesCode;
        }
        public EntityCollection GetTourTypesByDate(int day, int month, int year)
        {
            var date = new DateTime(year, month, day);
            var dateString = date.ToString("yyyy-MM-dd");
            var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                <entity name=""new_tour_type"">
                                <link-entity name=""new_spcific_date_and_hour"" from=""new_tour_type"" to=""new_tour_typeid"" alias=""spcific_date"" link-type=""outer"">
                                    <filter type=""and"">
                                    <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                                    <condition attribute=""new_start_time"" operator=""on"" value=""{dateString}""/>
                                    </filter>
                                </link-entity>
        
                        <link-entity name=""new_new_tour_type_new_day_and_time"" from=""new_tour_typeid"" to=""new_tour_typeid"" alias=""mapping"">
                          <link-entity name=""new_day_and_time"" from=""new_day_and_timeid"" to=""new_day_and_timeid"" alias=""DayAndTime"" link-type=""outer"">
                            <attribute name=""new_name""/>
                        <attribute name=""new_sunday""/>
                        <attribute name=""new_monday""/>
                        <attribute name=""new_tuesday""/>
                        <attribute name=""new_wednesday""/>
                        <attribute name=""new_thursday""/>
                        <attribute name=""new_friday""/>
                        <attribute name=""new_start_date""/>
                        <attribute name=""new_end_date""/>
                        <attribute name=""new_start_month""/>
                        <attribute name=""new_end_month""/>
                        <filter type=""and"">
                            <filter type=""or"">
                               <!-- התייחסות לטווח חודשים-->
                                <filter type=""and"">
                                      <filter type=""or"">
                                        <!-- מקרה 1: יש שני חודשים מלאים -->
                                        <filter type=""and"">
                                            <condition attribute=""new_start_month"" operator=""not-null"" />
                                            <condition attribute=""new_end_month"" operator=""not-null"" />
                                            <condition attribute=""new_start_month"" operator=""le"" value=""{month}"" />
                                            <condition attribute=""new_end_month"" operator=""ge"" value=""{month}"" />
                                        </filter>
                                        <!-- מקרה 2: יש רק חודש התחלה -->
                                        <filter type=""and"">
                                            <condition attribute=""new_start_month"" operator=""not-null"" />
                                            <condition attribute=""new_end_month"" operator=""null"" />
                                            <condition attribute=""new_start_month"" operator=""le"" value=""{month}"" />
                                        </filter>
                                    </filter>
                                </filter>
                                 <!-- התייחסות לטווח תאריכים-->
                                 <filter type=""and"">
                                      <!-- תנאי שמוודא שתאריך ההתחלה של הישות הוא קטן או שווהלתארריך הנוכחי -->
                                      <condition attribute=""new_start_date"" operator=""on-or-before"" value=""{dateString}"" />
                                      <filter type=""or"">
                                        <!-- או שתאריך הסיום ריק -->
                                        <condition attribute=""new_end_date"" operator=""null"" />
                                        <!-- או שתאריך הסיום הוא אחרי או שווה לתאריך  (targetMonthStart) -->
                                        <condition attribute=""new_end_date"" operator=""on-or-after"" value=""{dateString}"" />
                                      </filter>
                                </filter>
                            </filter>
                        </filter>
                                           
                          </link-entity>
                        </link-entity>
                            </entity>
                            </fetch>
                            ";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            return res;
        }
        string getFetchXmlForCondition(InstituitionType instituitionType, bool isForPlayAndShowsOnly = false, string lang = "he", TourCategory[] tourCategories = null)
        {
            string conditionXml = "";

            if (!isForPlayAndShowsOnly)
            {
                string name = instituitionType switch
                {
                    InstituitionType.TRAVEL_AGENTS => "new_is_for_travel_agents",
                    InstituitionType.EDUCATIONAL_GROUPS => "new_is_for_institutions",
                    InstituitionType.PRIVATE => "new_is_for_private",
                    _ => ""
                };

                conditionXml = $@"<condition attribute=""{name}"" operator=""eq"" value=""1""/>";
            }
            else
            {
                conditionXml = $@"<condition attribute=""new_is_for_calendar_view"" operator=""eq"" value=""1""/>";
            }

            string fetchXml = $@"
            <fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
            <entity name=""new_tour_type"">
                <attribute name=""new_is_need_event""/>
                <attribute name=""new_max_participants""/>
                <attribute name=""new_event_reservations""/>
                <attribute name=""new_tour_code""/>
                <attribute name=""new_minimum_days""/>
                <attribute name=""new_category""/>";



            if (isForPlayAndShowsOnly)
            {
                fetchXml += $@"
            <attribute name=""new_name""/>
            <attribute name=""new_name_en""/>
            <attribute name=""new_description_{lang}""/>
            <attribute name=""new_short_desc_{lang}""/>
            <attribute name=""new_max_reservations_per_day""/>
            <attribute name=""new_age""/>
            <attribute name=""new_tag""/>
            <attribute name=""new_duration_of_hours""/>
            <attribute name=""new_relevant_day_sunday""/>
            <attribute name=""new_relevant_day_monday""/>
            <attribute name=""new_relevant_day_tuesday""/>
            <attribute name=""new_target_population""/>
            <attribute name=""new_category""/>
            <attribute name=""new_grade_1""/>
            <attribute name=""new_grade_2""/>
            <attribute name=""new_grade_3""/>
            <attribute name=""new_grade_4""/>
            <attribute name=""new_grade_5""/>
            <attribute name=""new_grade_6""/>
            <attribute name=""new_grade_7""/>
            <attribute name=""new_grade_8""/>
            <attribute name=""new_grade_9""/>
            <attribute name=""new_grade_10""/>
            <attribute name=""new_grade_11""/>
            <attribute name=""new_grade_12""/>
            <attribute name=""new_relevant_day_wednesday""/>
            <attribute name=""new_relevant_day_thursday""/>
            <attribute name=""new_relevant_day_friday""/>
            <attribute name=""new_tab_to_display""/>
            <attribute name=""new_max_participants_in_group""/>
            <attribute name=""new_tour_typeid""/>
            <attribute name=""new_difference_between_arrival_to_start""/>
            <attribute name=""new_sort_weight_{lang}""/>
            <attribute name=""new_discount_type""/>
            <attribute name=""new_discount""/>
            <attribute name=""new_is_package""/>
            <attribute name=""new_cancel_registration_fees""/>
            <order attribute=""new_sort_weight_{lang}"" descending=""false""/>";
            }

            fetchXml += $@"<filter type=""and"">
                    {conditionXml}
                    <condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>";

            if (tourCategories != null && tourCategories.Length > 0)
            {
                fetchXml += $@"<condition attribute=""new_category"" operator=""in"">";
                foreach (var cat in tourCategories)
                {
                    fetchXml += $@"<value>{(int)cat}</value>";
                }
                fetchXml += "</condition>";
            }

            fetchXml += @"</filter>
                <order attribute=""new_tour_code"" descending=""false"" />
                <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" link-type=""outer"" alias=""event"">
                    <attribute name=""new_max_participants_per_event""/>
                </link-entity>
            </entity>
        </fetch>";

            return fetchXml;
        }

        //string getFetchXmlForCondition(InstituitionType instituitionType, bool isForPlayAndShowsOnly = false, string lang="he", TourCategory[] tourCategory=null)

        // {
        //     string conditionXml = "";
        //     int category = (int)tourCategory;
        //     if (!isForPlayAndShowsOnly)
        //     {
        //         string name = "";

        //         switch (instituitionType)
        //         {
        //             case InstituitionType.TRAVEL_AGENTS:
        //                 name = "new_is_for_travel_agents";
        //                 break;
        //             case InstituitionType.EDUCATIONAL_GROUPS:
        //                 name = "new_is_for_institutions";
        //                 break;
        //             case InstituitionType.PRIVATE:
        //                 name = "new_is_for_private";
        //                 break;
        //         }

        //         conditionXml = $@"<condition attribute=""{name}"" operator=""eq"" value=""1""/>";

        //     }
        //     else
        //     {
        //         conditionXml = $@"
        //         <condition attribute=""new_is_for_calendar_view"" operator=""eq"" value=""1""/> ";
        //         //conditionXml = $@"
        //         //<condition attribute=""new_is_need_event"" operator=""eq"" value=""1""/> 
        //         //<condition attribute=""statecode"" operator=""eq"" value=""0""/> 
        //         //<condition attribute=""new_budget_framework"" operator=""in"">
        //         //    <value uiname=""חזיון אור קולי"" uitype=""new_budget_framework"">{{D3310719-52E1-EF11-813D-00505691117E}}</value>
        //         //    <value uiname=""תיאטרון לגעת בזכרון"" uitype=""new_budget_framework"">{{4009FBD9-52E1-EF11-813D-00505691117E}}</value>
        //         //</condition>";
        //     }

        //     string fetchXml = $@"
        //         <fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
        //             <entity name=""new_tour_type"">
        //                 <attribute name=""new_is_need_event""/>
        //                 <attribute name=""new_max_participants""/>
        //                 <attribute name=""new_event_reservations""/>
        //                 <attribute name=""new_tour_code""/>
        //                 <attribute name=""new_minimum_days""/>";
        //     if (isForPlayAndShowsOnly)
        //     {
        //         fetchXml+= $@"
        //           <attribute name=""new_name""/>שם בעברית
        //           <attribute name=""new_name_en""/>שם באנגלית
        //           <attribute name=""new_description_{lang}""/>הסבר אנגלית
        //           <attribute name=""new_short_desc_{lang}""/>הסבר מקוצר עברית
        //           <attribute name=""new_max_reservations_per_day""/>//מקסימום הזמנות ליום
        //           <attribute name=""new_age""/>מתאים לגיל
        //           <attribute name=""new_tag""/> תגית
        //           <attribute name=""new_duration_of_hours""/>משך שעות
        //           <attribute name=""new_relevant_day_sunday""/>
        //           <attribute name=""new_relevant_day_monday""/>
        //           <attribute name=""new_relevant_day_tuesday""/>
        //           <attribute name=""new_target_population""/>
        //           <attribute name=""new_grade_1""/>
        //           <attribute name=""new_grade_2""/>
        //           <attribute name=""new_grade_3""/>
        //           <attribute name=""new_grade_4""/>
        //           <attribute name=""new_grade_5""/>
        //           <attribute name=""new_grade_6""/>
        //           <attribute name=""new_grade_7""/>
        //           <attribute name=""new_grade_8""/>
        //           <attribute name=""new_grade_9""/>
        //           <attribute name=""new_grade_10""/>
        //           <attribute name=""new_grade_11""/>
        //           <attribute name=""new_grade_12""/>
        //           <attribute name=""new_relevant_day_wednesday""/>
        //           <attribute name=""new_relevant_day_thursday""/>
        //           <attribute name=""new_relevant_day_friday""/>
        //           <attribute name=""new_tab_to_display""/>
        //           <attribute name=""new_max_participants_in_group""/>
        //           <attribute name=""new_tour_typeid""/>
        //           <attribute name=""new_difference_between_arrival_to_start""/>
        //           <attribute name=""new_sort_weight_{lang}""/>//משקל למיון
        //           <attribute name=""new_discount_type""/>//סוג הנחה
        //           <attribute name=""new_discount""/>//סך הנחה
        //           <attribute name=""new_is_package""/>//האם חבילה
        //           <attribute name=""new_cancel_registration_fees""/>//בטל דמי הרשמה וטיפול
        //           <order attribute=""new_sort_weight_{lang}"" descending=""false""/>";
        //     }
        //     fetchXml += $@"<filter type=""and"">
        //                     {conditionXml}
        //                     <condition attribute=""new_show_on_website"" operator=""eq"" value=""1""/>";
        //                      if (tourCategory != null)
        //                         {
        //                             fetchXml += $@"<filter type=""or"">
        //                                             <condition attribute=""new_category"" operator=""eq"" value=""{category}""/>
        //                                             <condition attribute=""new_category"" operator=""ne"" value=""""/>

        //                                             </filter>";
        //                         }
        //     fetchXml +=$@" </filter>
        //                 <order attribute=""new_tour_code"" descending=""false"" />
        //                 <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" link-type=""outer"" alias=""event"">
        //                     <attribute name=""new_max_participants_per_event""/>
        //                 </link-entity>
        //             </entity>
        //         </fetch>";
        //     return fetchXml;
        // }
        public EntityCollection GetTourTypesByInstituitionType(InstituitionType instituitionType, bool isForPlayAndShowsOnly = false, TourCategory[] tourCategory = null)
        {
            try
            {
                Log.Information("start GetTourTypesByInstituitionType instituitionType= " + instituitionType);

                var fetchXml = getFetchXmlForCondition(instituitionType, isForPlayAndShowsOnly, "he", tourCategory);

                var query = new FetchExpression(fetchXml);
                var res = _crmService.RetrieveMultiple(query);
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTourTypesByInstituitionType error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<ReservationClosedDate> GetCloseActivitiesInDate(string date)
        {
            List<ReservationClosedDate> reservationClosedDate = new List<ReservationClosedDate>();
            var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                                <entity name=""new_closed_days_and_activities"">
                                <attribute name=""new_name""/>
                                <attribute name=""createdon""/>
                                <attribute name=""new_to_closing_date""/>
                                <attribute name=""new_closing_type""/>
                                <attribute name=""new_from_closing_date""/>
                                <attribute name=""new_closing_approved_by""/>
                                <attribute name=""new_is_closed_full_day""/>
                                <attribute name=""new_closed_days_and_activitiesid""/>
                                <filter type=""and"">
                                <condition attribute=""new_from_closing_date"" operator=""on-or-before"" value=""{date}""/>
                                <condition attribute=""new_to_closing_date"" operator=""on-or-after"" value=""{date}""/>
                                <condition attribute=""new_is_closed_full_day"" operator=""eq"" value=""1""/>
                                </filter>
                                </entity>
                                </fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            foreach (var entity_closedDate in res.Entities)
            {
                reservationClosedDate.Add(new ReservationClosedDate(entity_closedDate.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime(), entity_closedDate.GetAttributeValue<OptionSetValue>("new_closing_type").Value));

            }
            return reservationClosedDate;
        }

        public List<OnlineTourType> GetAllShowsAndPlaysBasicDetails(string lang)
        {
            List<OnlineTourType> showsAndPlaysTours = new List<OnlineTourType>();

            Log.Information($"GetShowsAndPlaysByDate start");
            string fetchXml = getFetchXmlForCondition(InstituitionType.PRIVATE, true, lang, null);
            var res = _onlineReservationRepository.GetTourTypeList(fetchXml, lang);
            res.ForEach(tourType =>
            {
                tourType.BasicDetails.GuidLanguages = GetLanguagesByTourType(tourType.BasicDetails.TourCode, lang);
                tourType.BasicDetails.Tags = GetTagsByTourType(tourType.BasicDetails.TourCode, lang);
                tourType.TourTypesRequiredList = GetTourTypesByTourType(tourType.BasicDetails.TourCode, lang);
            });
            showsAndPlaysTours = res;



            Log.Information($"GetShowsAndPlaysByDate end, res :{showsAndPlaysTours}");
            return showsAndPlaysTours;
        }
        public int getRelevantPlaces(Entity item)
        {
            if (item.GetAttributeValue<bool>("new_is_need_event") == true)//אם זה ארוע
            {

                int maxParticipentsInTourType = item.GetAttributeValue<int>("new_max_participants");
                if (maxParticipentsInTourType != null)
                {
                    return maxParticipentsInTourType;

                }
                else
                {
                    return int.Parse(item.GetAttributeValue<AliasedValue>("event.new_max_participants_per_event").Value.ToString());

                }

            }
            else

            {
                if (item.Attributes.Contains("new_max_participants"))
                {
                    return item.GetAttributeValue<int>("new_max_participants");

                    // לעשות עם הערך מה שצריך
                }
                else
                {
                    return 250;
                }
            }
        }
        AvailableTour getNewAvailableTour(Entity item, string tourCode, int participantsNum, TourCategory? tourCategory)
        {
            var tour = new AvailableTour
            {
                TourCode = tourCode,
                ExceededNumVisitors = false,
                DateUnavailable = false,
                RelevantPlaces = getRelevantPlaces(item),
                TourCategory = tourCategory
            };
            if (item.Attributes.Contains("new_max_participants"))
            {
                if (item.GetAttributeValue<int>("new_max_participants") >= participantsNum)
                {
                    tour.ExceededNumVisitors = false;
                }
                else
                {
                    tour.ExceededNumVisitors = true;
                }
            }

            return tour;
        }
        public Dictionary<string, List<AvailableTour>> GetTourTypesByStartDateAndParticipants(string? startDateString,
            string? endDateString,
            int participantsNum, InstituitionType instituitionType, bool isForPlayAndShowsOnly, TourCategory[] tourCategory = null)
        {
            Dictionary<string, List<AvailableTour>> availableToursByDate = new Dictionary<string, List<AvailableTour>>();


            var currentDate = DateTime.UtcNow.ToString("dd/MM/yyyy"); // פורמט "o" הוא ISO 8601
            var toursEntity = GetTourTypesByInstituitionType(instituitionType, isForPlayAndShowsOnly, tourCategory);
            if (string.IsNullOrEmpty(startDateString) || string.IsNullOrEmpty(endDateString))
            {
                availableToursByDate[currentDate] = new List<AvailableTour>();
            }
            else
            {
                var startDate = DateTime.ParseExact(startDateString, "dd/MM/yyyy", null);
                var endDate = DateTime.ParseExact(endDateString, "dd/MM/yyyy", null);

                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    string dateKey = date.ToString("dd/MM/yyyy");
                    availableToursByDate[dateKey] = new List<AvailableTour>();
                }
            }

            foreach (var item in toursEntity.Entities)
            {
                TourCategory? category = (TourCategory?)item.GetAttributeValue<OptionSetValue>("new_category")?.Value ?? null;

                string tourCode = item.GetAttributeValue<string>("new_tour_code");
                // if (tourCode != "264") continue;

                if (participantsNum != null && (string.IsNullOrEmpty(startDateString) || string.IsNullOrEmpty(endDateString)))//אם אין תאריך  בודק רק לפי כמות מבקרים
                {
                    availableToursByDate[currentDate].Add(getNewAvailableTour(item, tourCode, participantsNum, category));
                }
                else if (!string.IsNullOrEmpty(startDateString) && !string.IsNullOrEmpty(endDateString))
                {
                    var startDate = DateTime.ParseExact(startDateString, "dd/MM/yyyy", null);
                    var endDate = DateTime.ParseExact(endDateString, "dd/MM/yyyy", null);
                    //if (startDate.Month == DateTime.Now.Month && startDate.Year == DateTime.Now.Year)
                    //   // אם רוצים לראות אם
                    //{
                    //    continue;
                    //}

                    List<ReservationClosedDate> closedTourDates = _onlineReservationRepository.GetClosedDatesForTour(item, startDate, endDate);
                    List<DateTime> shabbatonClosedTourDates = _onlineReservationRepository.GetClosedDatesByType(closedTourDates, (int)ClosedType.SHABATON);
                    var spcificDateList = GetSpecialDateInTourBydate(item.Id.ToString());
                    var daysAndTimeList = GetTourRelevantDaysAndTime(tourCode);


                    for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                    {
                        AvailableTour tour = getNewAvailableTour(item, tourCode, participantsNum, category);
                        string dateKey = date.ToString("dd/MM/yyyy");


                        //שבת חסום!
                        if (date.DayOfWeek == DayOfWeek.Saturday)
                        {
                            tour.DateUnavailable = true;
                            availableToursByDate[dateKey].Add(tour);
                            continue;
                        }
                        //לבדוק ימי עבודה
                        int minimumDays = item.GetAttributeValue<int>("new_minimum_days");

                        int NumberOfDays = GetWorkingDays(DateTime.Today.Date, date, shabbatonClosedTourDates);//בודק כמה ימי עסקים בין 2 תאריכים
                        if (NumberOfDays < minimumDays)
                        {
                            tour.DateUnavailable = true;
                            availableToursByDate[dateKey].Add(tour);
                            continue;
                        }
                        //לבדוק אם יש סגירה יומית ביום הזה
                        //אם תאריך הוא בסגירת ימים אז הוא חסום
                        if (closedTourDates != null && closedTourDates.Count > 0)
                        {
                            if (closedTourDates.Any(item => item.date == date))
                            {
                                tour.DateUnavailable = true;
                                availableToursByDate[dateKey].Add(tour);
                                continue;
                            }
                        }
                        bool isInSpecificDate = false;
                        bool isDayIsExit = false;

                        if (spcificDateList.Entities.Count > 0)// ביום הזה אם יש לסיור שעות ותאריכים מיוחדים
                        {
                            foreach (var specialDate in spcificDateList.Entities)
                            {
                                DateTime startDateTime1 = specialDate.GetAttributeValue<DateTime>("new_start_time").ToLocalTime();

                                // בדיקה אם התאריך  שווה לתאריך  המבוקש
                                if (startDateTime1.ToString("yyyy-MM-dd") == date.ToString("yyyy-MM-dd"))
                                {
                                    isInSpecificDate = true;
                                    break;
                                }
                            }

                        }
                        if (daysAndTimeList.Entities.Count > 0)//אם לסיור יש ימים ושעות מוגדרים
                        {
                            foreach (var daysAndTime in daysAndTimeList.Entities)
                            {

                                if (daysAndTime.Contains("new_start_date"))//אם יש תאריך התחלה
                                {
                                    if (daysAndTime.GetAttributeValue<DateTime>("new_start_date").ToLocalTime() <= date)//בודק שהתאריך התחלה קטן מהתאריך הרצוי
                                    {
                                        if (!daysAndTime.Contains("new_end_date") || (daysAndTime["new_end_date"] == null || daysAndTime.GetAttributeValue<DateTime>("new_end_date").ToLocalTime() >= date))
                                        {

                                            if (daysAndTime.GetAttributeValue<bool>("new_" + date.DayOfWeek.ToString().ToLower()) == true)
                                            {
                                                isDayIsExit = true;
                                                break;
                                            }

                                        }

                                    }
                                }
                                //אם יש חודש התחלה
                                else if (daysAndTime.GetAttributeValue<OptionSetValue>("new_start_month")?.Value <= date.Month)
                                {
                                    if (!daysAndTime.Contains("new_end_month") || (daysAndTime["new_end_month"] == null || daysAndTime.GetAttributeValue<OptionSetValue>("new_end_month").Value >= date.Month))
                                    {
                                        if (daysAndTime.GetAttributeValue<bool>("new_" + date.DayOfWeek.ToString().ToLower()) == true)
                                        {
                                            isDayIsExit = true;
                                            break;
                                        }
                                    }
                                }
                            }

                        }
                        if (spcificDateList.Entities.Count > 0 && (daysAndTimeList.Entities.Count <= 0 || daysAndTimeList.Entities.Count > 0 && !isDayIsExit) && !isInSpecificDate)
                        {
                            tour.DateUnavailable = true;
                            availableToursByDate[dateKey].Add(tour);
                            continue;
                        }
                        //אם יש ימים מוגדרים והתאריך לא נמצא ביום כזה וגם לא נמצא בתאריך מיוחד (אם יש אז הוא חסום)
                        if (daysAndTimeList.Entities.Count > 0 && (spcificDateList.Entities.Count <= 0 || spcificDateList.Entities.Count > 0 && !isInSpecificDate) && !isDayIsExit)
                        {
                            tour.DateUnavailable = true;
                            availableToursByDate[dateKey].Add(tour);
                            continue;
                        }
                        //אחרת לבדוק האם יש מספיק מקומות

                        if (tour.RelevantPlaces < participantsNum)
                        {
                            tour.ExceededNumVisitors = true;
                            availableToursByDate[dateKey].Add(tour);
                            continue;
                        }
                        else
                        {
                            tour.ExceededNumVisitors = false;
                            availableToursByDate[dateKey].Add(tour);
                            continue;
                        }
                    }


                }

            }
            //if (isForPlayAndShowsOnly)
            //{
            //    List<OnlineTourType> tourTypes = GetAllShowsAndPlaysBasicDetails("he");

            //    foreach (var kvp in availableToursByDate)
            //    {
            //        string dateKey = kvp.Key;
            //        var date = DateTime.ParseExact(dateKey, "dd/MM/yyyy", null);

            //        List<AvailableTour> tours = kvp.Value;
            //         if (date.DayOfWeek == DayOfWeek.Saturday)
            //            {
            //                continue;
            //            }
            //        foreach (var tour in tours)
            //        {
            //            if (tour.DateUnavailable)
            //            {
            //                continue;
            //            }
            //            OnlineTourType tourFounded = tourTypes.Find(tourType => tourType.BasicDetails.TourCode == tour.TourCode);

            //            if (tourFounded != null)
            //            {
            //                tour.ArrivialHours 
            //                    = getDateTimeSlots(dateKey, tourFounded.BasicDetails.Id, tour.TourCode, tourFounded.BasicDetails.DurationActivity);
            //                tour.RelevantPlaces = 0;
            //                foreach (var hour in tour.ArrivialHours)
            //                {
            //                    tour.RelevantPlaces += hour.Value;
            //                }

            //            }
            //        }
            //    }
            //}

            return availableToursByDate;
        }
        public List<AvailableTour> GetTourTypesByDateAndParticipants(string dateString, int participantsNum, InstituitionType instituitionType)
        {

            List<AvailableTour> availableTours = new List<AvailableTour>();
            var toursEntity = GetTourTypesByInstituitionType(instituitionType);
            foreach (var item in toursEntity.Entities)
            {

                var tour = new AvailableTour();
                tour.TourCode = item.GetAttributeValue<string>("new_tour_code");
                tour.ExceededNumVisitors = false;
                tour.DateUnavailable = false;
                //if (tour.TourCode == "231")
                //{
                //    var x = "for easy debugger";
                //}
                //else
                //{
                //    continue;
                //}
                if (!string.IsNullOrEmpty(dateString))
                {
                    var date = DateTime.ParseExact(dateString, "dd/MM/yyyy", null);

                    //כמות מקומות רלוונטית
                    if (item.GetAttributeValue<bool>("new_is_need_event") == true)//אם זה ארוע
                    {
                        //*****************************************************************************************
                        //יש בעיה בקוד הזה כי יש מצב שהיום פעילות פתוח בסוג פעילות יומית אבל עדיין לא נפתח עבורו יום פעילות בארוע ובנוסף יש בארוע ימי פעילות 
                        //בתאריך הזה שנפתחו ידינית עבור בתי ספר ואין לנו דרך להבדיל אם היום פעילות עבור הקהל הרחב קיים או לא
                        //-כי הבדיקה פה היא לא לפי שעות אלא רק תאריך ולכן נחזיר את המקסימום נרשמים להזנה מהסוג פעילות יומית 
                        //ואם לא קיים אז את מקסימום נרשמים לארוע מהארוע
                        //*****************************************************************************************

                        //###################################################################################

                        //int maxParticipentsInTourType = item.GetAttributeValue<int>("new_max_participants");
                        //var eventId = item.GetAttributeValue<EntityReference>("new_event_reservations").Id;

                        //var activityDays = GetActivityDaysByDateAndEvent(date.ToString("yyyy-MM-dd"), eventId);
                        //var maxvisitorsForEvent = int.Parse(item.GetAttributeValue<AliasedValue>("event.new_max_participants_per_event").Value.ToString());
                        //bool hasPlace = false;
                        ////כמות המבקרים ביום זה היא המינמום בין מקסימום נרשמים להזמנה ל-מקסימום מקומות פנויים שנשאר באחד מהימי פעילות
                        //int MaxPlacesInActivityDays = 0;//
                        //foreach (var activityDay in activityDays.Entities)
                        //{
                        //    int Places = maxvisitorsForEvent - activityDay.GetAttributeValue<int>("new_actual_number_of_participants");
                        //    if (MaxPlacesInActivityDays < Places)
                        //    {
                        //        MaxPlacesInActivityDays = Places;

                        //    }
                        //}
                        //if (activityDays.Entities.Count == 0)
                        //{
                        //    MaxPlacesInActivityDays = maxvisitorsForEvent;
                        //}
                        //var RelevantPlaces = MaxPlacesInActivityDays > maxParticipentsInTourType ? maxParticipentsInTourType : MaxPlacesInActivityDays;
                        //tour.RelevantPlaces = RelevantPlaces;

                        //###################################################################################
                        int maxParticipentsInTourType = item.GetAttributeValue<int>("new_max_participants");
                        if (maxParticipentsInTourType != null)
                        {
                            tour.RelevantPlaces = maxParticipentsInTourType;

                        }
                        else
                        {
                            tour.RelevantPlaces = int.Parse(item.GetAttributeValue<AliasedValue>("event.new_max_participants_per_event").Value.ToString());

                        }

                    }
                    else

                    {
                        tour.RelevantPlaces = item.GetAttributeValue<int>("new_max_participants");
                    }

                    //שבת חסום!
                    if (date.DayOfWeek == DayOfWeek.Saturday)
                    {
                        tour.DateUnavailable = true;
                        availableTours.Add(tour);
                        continue;
                    }
                    List<ReservationClosedDate> closedTourDates = _onlineReservationRepository.GetClosedDatesForTour(item, date, date);
                    List<DateTime> shabbatonClosedTourDates = _onlineReservationRepository.GetClosedDatesByType(closedTourDates, (int)ClosedType.SHABATON);
                    //לבדוק ימי עבודה
                    int minimumDays = item.GetAttributeValue<int>("new_minimum_days");
                    int NumberOfDays = GetWorkingDays(DateTime.Today.Date, date, shabbatonClosedTourDates);//בודק כמה ימי עסקים בין 2 תאריכים
                    if (NumberOfDays < minimumDays)
                    {
                        tour.DateUnavailable = true;
                        availableTours.Add(tour);
                        continue;
                    }
                    //לבדוק אם יש סגירה יומית ביום הזה
                    //אם תאריך הוא בסגירת ימים אז הוא חסום
                    if (closedTourDates != null && closedTourDates.Count > 0)
                    {
                        if (closedTourDates.Any(item => item.date == date))
                        {
                            tour.DateUnavailable = true;
                            availableTours.Add(tour);
                            continue;
                        }
                    }
                    bool isInSpecificDate = false;
                    bool isDayIsExit = false;

                    var spcificDateList = GetSpecialDateInTourBydate(item.Id.ToString());
                    if (spcificDateList.Entities.Count > 0)// ביום הזה אם יש לסיור שעות ותאריכים מיוחדים
                    {
                        foreach (var specialDate in spcificDateList.Entities)
                        {
                            DateTime startDate = specialDate.GetAttributeValue<DateTime>("new_start_time").ToLocalTime();

                            // בדיקה אם התאריך  שווה לתאריך  המבוקש
                            if (startDate.ToString("yyyy-MM-dd") == date.ToString("yyyy-MM-dd"))
                            {
                                isInSpecificDate = true;
                                break;
                            }
                        }
                        //if (!isInSpecificDate&& daysAndTimeList==null)
                        //{
                        //    tour.DateUnavailable = true;
                        //    availableTours.Add(tour);
                        //    continue;
                        //}
                    }
                    var daysAndTimeList = GetTourRelevantDaysAndTime(tour.TourCode);
                    if (daysAndTimeList.Entities.Count > 0)//אם לסיור יש ימים ושעות מוגדרים
                    {
                        foreach (var daysAndTime in daysAndTimeList.Entities)
                        {

                            if (daysAndTime.Contains("new_start_date"))//אם יש תאריך התחלה
                            {
                                if (daysAndTime.GetAttributeValue<DateTime>("new_start_date").ToLocalTime() <= date)//בודק שהתאריך התחלה קטן מהתאריך הרצוי
                                {
                                    if (!daysAndTime.Contains("new_end_date") || (daysAndTime["new_end_date"] == null || daysAndTime.GetAttributeValue<DateTime>("new_end_date").ToLocalTime() >= date))
                                    {

                                        if (daysAndTime.GetAttributeValue<bool>("new_" + date.DayOfWeek.ToString().ToLower()) == true)
                                        {
                                            isDayIsExit = true;
                                            break;
                                        }

                                    }

                                }
                            }
                            //אם יש חודש התחלה
                            else if (daysAndTime.GetAttributeValue<OptionSetValue>("new_start_month")?.Value <= date.Month)
                            {
                                if (!daysAndTime.Contains("new_end_month") || (daysAndTime["new_end_month"] == null || daysAndTime.GetAttributeValue<OptionSetValue>("new_end_month").Value >= date.Month))
                                {
                                    if (daysAndTime.GetAttributeValue<bool>("new_" + date.DayOfWeek.ToString().ToLower()) == true)
                                    {
                                        isDayIsExit = true;
                                        break;
                                    }
                                }
                            }
                        }

                    }
                    //אם יש לי תאריכים מוחדים ואין לי ימים מסוימים אז אם התאריך לא נמצא הימים מיוחדים אז הוא חסום!
                    //אם יש לי תאריכים מיוחדים וגם שעות מוגדרות והתאריך לא נמצא בשעות מוגדרות וגם לא בימים מיוחדים אז הוא חסום
                    if (spcificDateList.Entities.Count > 0 && (daysAndTimeList.Entities.Count <= 0 || daysAndTimeList.Entities.Count > 0 && !isDayIsExit) && !isInSpecificDate)
                    {
                        tour.DateUnavailable = true;
                        availableTours.Add(tour);
                        continue;
                    }
                    //אם יש ימים מוגדרים והתאריך לא נמצא ביום כזה וגם לא נמצא בתאריך מיוחד (אם יש אז הוא חסום)
                    if (daysAndTimeList.Entities.Count > 0 && (spcificDateList.Entities.Count <= 0 || spcificDateList.Entities.Count > 0 && !isInSpecificDate) && !isDayIsExit)
                    {
                        tour.DateUnavailable = true;
                        availableTours.Add(tour);
                        continue;
                    }
                    //אחרת לבדוק האם יש מספיק מקומות

                    if (tour.RelevantPlaces < participantsNum)
                    {
                        tour.ExceededNumVisitors = true;
                        availableTours.Add(tour);
                        continue;
                    }
                    else
                    {
                        tour.ExceededNumVisitors = false;
                        availableTours.Add(tour);
                        continue;
                    }



                }
                else if (participantsNum != null) //אם אין תאריך בודק רק לפי כמות מבקרים
                {
                    tour.TourCode = item.GetAttributeValue<string>("new_tour_code");
                    tour.DateUnavailable = false;
                    tour.RelevantPlaces = item.GetAttributeValue<int>("new_max_participants");
                    if (item.GetAttributeValue<int>("new_max_participants") >= participantsNum)
                    {
                        tour.ExceededNumVisitors = false;
                    }
                    else
                    {
                        tour.ExceededNumVisitors = true;
                    }
                    availableTours.Add(tour);
                }
            }
            return availableTours;
        }
        private EntityCollection GetTourRelevantDaysAndTime(string tourCode)
        {
            var fetchXml = @$"<fetch distinct=""false"">
                <entity name=""new_day_and_time"">
                <attribute name=""new_name""/>
                <attribute name=""new_sunday""/>
                <attribute name=""new_monday""/>
                <attribute name=""new_tuesday""/>
                <attribute name=""new_wednesday""/>
                <attribute name=""new_thursday""/>
                <attribute name=""new_friday""/>
                <attribute name=""new_start_date""/>
                <attribute name=""new_end_date""/>
                 <attribute name=""new_end_month""/>
                <attribute name=""new_start_month""/>
                <link-entity name=""new_new_tour_type_new_day_and_time"" from=""new_day_and_timeid"" to=""new_day_and_timeid"" alias=""mapping"">
                    <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_tour_typeid"" alias=""tourType"">
                    <filter>
                        <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/> 
                    </filter>
                    <attribute name=""new_tour_typeid""/>
                    <attribute name=""new_name""/>
                    </link-entity>
                </link-entity>
                </entity>
                </fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            return res;


        }
        public void UpdateActualNumOfVisitors(string guid, int numOfParticipants, bool isEventRegistration)
        {
            Entity entity;
            if (isEventRegistration)
            {

                entity = new Entity("new_event_registration", new Guid(guid));
            }
            else
            {
                entity = new Entity("new_activity_day", new Guid(guid));

            }
            entity["new_actual_number_of_participants"] = numOfParticipants;
            entity["new_did_group_arrive"] = true;
            _crmService.Update(entity);
        }
        private EntityCollection GetActivityDaysByDateAndEvent(string date, Guid eventId)
        {
            var fetchXml = $@"
                <fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
                <entity name=""new_activity_day"">
                <attribute name=""new_name""/>
                <attribute name=""new_start_datetime""/>
                <attribute name=""new_activity_day_type""/>
                <attribute name=""new_number_of_groups""/>
                <attribute name=""new_number_of_participants""/>
                <attribute name=""new_actual_number_of_participants""/>
                <attribute name=""new_activity_dayid""/>
                <filter type=""and"">
                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                <condition attribute=""new_event_reservations"" operator=""eq"" uitype=""new_event_reservations"" value=""{eventId}""/>
                <condition attribute=""new_start_datetime"" operator=""on"" value=""{date}""/>
                </filter>
                </entity>
                </fetch>";
            var query = new FetchExpression(fetchXml);
            var res = _crmService.RetrieveMultiple(query);
            return res;
        }
        public List<QRCodeItem> GetEventTodayListForQrScsanner()
        {
            var yadVashemLocationGuid = _config.GetValue<string>("YadVashemLocationGuid")!;
            Console.WriteLine($"=== GetEventTodayListForQrScsanner ===");
            Console.WriteLine($"yadVashemLocationGuid: {yadVashemLocationGuid}");
            Log.Information($"GetEventTodayListForQrScsanner - yadVashemLocationGuid: {yadVashemLocationGuid}");
            var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                    <entity name=""new_event_reservations"">
                      <attribute name=""new_name"" />
                      <attribute name=""new_event_code"" />
                      <attribute name=""new_event_reservationsid"" />
                      <filter type=""and"">
                        <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                        <condition attribute=""new_event_reservation_status"" operator=""ne"" value=""3"" />
                      </filter>
                      <link-entity name=""new_activity_day"" from=""new_event_reservations"" to=""new_event_reservationsid"" alias=""ActivityDay"">
                      <attribute name=""new_start_datetime"" />  
                       <attribute name=""new_activity_dayid"" /> 
                      <filter type=""and"">
                          <condition attribute=""new_start_datetime"" operator=""today"" />
                          <condition attribute=""new_activity_day_location"" operator=""eq"" value=""{yadVashemLocationGuid}"" />
                        </filter>
                      </link-entity>
                    </entity>
                  </fetch>";
            Console.WriteLine($"fetchXml: {fetchXml}");
            Log.Information($"GetEventTodayListForQrScsanner - fetchXml: {fetchXml}");
            var result = _onlineReservationRepository.GetEventTodayListForQrScsanner(fetchXml);
            Console.WriteLine($"result count: {result.Count}");
            foreach (var item in result)
            {
                Console.WriteLine($"  - EventCode: {item.EventCode}, Name: {item.ActivityName}, Guid: {item.Guid}");
            }
            Log.Information($"GetEventTodayListForQrScsanner - result count: {result.Count}");
            return result;


        }


        public bool LoginQrScanner(QrScannerUser qrScannerUser)
        {
            var erroMessage = "שם משתמש או סיסמא שגויים";
            if (qrScannerUser != null && GetQrScannerUserList().Contains(qrScannerUser.Username))
            {

                if (qrScannerUser.Username.First() + "1234!" == qrScannerUser.Password)
                {
                    return true;

                }
                else
                {
                    throw new Exception(erroMessage);

                }
            }
            else
            {
                throw new Exception(erroMessage);
            }

        }
        public List<string> GetQrScannerUserList()
        {
            return _config.GetSection("qrScannerUserList").Get<List<string>>()!;

        }
        public QuickEntryDetails GetDetailsByQRCode(List<QRCodeItem> qRCodeItemList)
        {
            if (qRCodeItemList.Count == 1 && qRCodeItemList.First().IsEventReservation)
            {
                var ticketDetailsForEvent = GetEventByQRDetails(qRCodeItemList, false);
                return ticketDetailsForEvent;
            }
            QuickEntryDetails ticketDetailsForReservation;
            if (qRCodeItemList.Count == 1 && !qRCodeItemList.First().IsEventReservation)
            {
                ticketDetailsForReservation = GetReservationByQRDetails(qRCodeItemList.First());
                return ticketDetailsForReservation;
            }
            //ticket is reservation - חבילה
            var ticketDetailsForEvents = GetEventByQRDetails(qRCodeItemList.FindAll(item => item.IsEventReservation == true), true);
            ticketDetailsForReservation = GetReservationByQRDetails(qRCodeItemList.First());
            ticketDetailsForReservation.AcitivityList.AddRange(ticketDetailsForEvents.AcitivityList);

            return ticketDetailsForReservation;
        }

        public QuickEntryDetails GetEventByQRDetails(List<QRCodeItem> qRCodeItemList, bool isForReservation = false)
        {
            var guidList = "";
            qRCodeItemList.ForEach(qRCodeItem => { guidList += "<value>" + qRCodeItem.Guid + "</value>"; });

            Log.Information($"start GetEventByQRDetails details: {@qRCodeItemList}");
            try
            {
                string fetchXml = @$"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                    <entity name=""new_event_registration"">
                    <attribute name=""new_number_of_participants""/>
                    <attribute name=""new_actual_number_of_participants""/>
                    <attribute name=""new_event_registrationid""/>
                    <attribute name=""new_payment_status""/>
                    <attribute name=""new_did_group_arrive""/>
                    <attribute name=""new_num""/>
                    <filter type=""and"">
                        <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                        <condition attribute=""new_event_registrationid"" operator=""in"" >
                            {guidList}
                        </condition>
                    </filter>
                    <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" alias=""EventReservation"" link-type=""inner"" >
                        <attribute name=""new_event_code""/>
                        <attribute name=""new_name""/>
                    </link-entity>                    
                       <link-entity name=""new_event_registration_activity_day"" from=""new_event_registration"" to=""new_event_registrationid"" alias=""ae"">
                      <link-entity name=""new_activity_day"" from=""new_activity_dayid"" to=""new_activity_day"" alias=""ActivityDay"">
                        <attribute name=""new_start_datetime""/>
                        <attribute name=""new_activity_dayid""/>
                        <filter type=""and"">
                          <condition attribute=""new_start_datetime"" operator=""today"" />
                          <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                        </filter>
                        <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" alias=""TourType"">
                            <attribute name=""new_duration_of_hours""/>
                        </link-entity>
                      </link-entity>
                    </link-entity>
                    </entity>
                    </fetch>";
                return _onlineReservationRepository.GetEventDetailsByQrCode(fetchXml, isForReservation);
            }
            catch (Exception ex)
            {
                Log.Error($"GetEventByQRDetails error: {ex.Message}");
                throw new Exception(ex.Message);
            }


        }
        public QuickEntryDetails GetReservationByQRDetails(QRCodeItem qRCodeItem)
        {


            Log.Information($"start GetReservationByQRDetails details: {@qRCodeItem}");
            try
            {
                string fetchXml = @$"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                                      <entity name=""new_reservation"">
                                        <attribute name=""new_reservationid"" />
                                        <attribute name=""new_num"" />
                                        <attribute name=""new_payment_status"" />
                                        <filter type=""and"">
                                            <condition attribute=""new_reservationid"" operator=""eq"" value=""{{{qRCodeItem.Guid}}}"" />
                                            <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                        </filter>
                                        <link-entity name=""new_activity_day"" from=""new_reservation"" to=""new_reservationid"" alias=""ActivityDay"">
                                        <attribute name=""new_activity_dayid"" />
                                        <attribute name=""new_start_datetime"" />
                                        <attribute name=""new_number_of_participants"" />
                                        <attribute name=""new_actual_number_of_participants"" />
                                        <attribute name=""new_did_group_arrive"" />
                                          <filter type=""and"">
                                            <condition attribute=""new_start_datetime"" operator=""today"" />
                                            <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                            <condition attribute=""new_reservation_status"" operator=""ne"" value=""3"" />
                                            <condition attribute=""new_activity_day_status"" operator=""ne"" value=""3"" />
                                          </filter>
                                          <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" alias=""TourType"">
                                            <attribute name=""new_name"" />
                                            <attribute name=""new_duration_of_hours"" />
                                            </link-entity>
                                        </link-entity>
                                      </entity>
                                    </fetch>";
                return _onlineReservationRepository.GetReservationDetailsByQrCode(fetchXml);
            }
            catch (Exception ex)
            {
                Log.Error($"GetReservationByQRDetails error: {ex.Message}");
                throw new Exception(ex.Message);
            }


        }




        public OnlineTourType GetTourBasicAndAdditionalDetails(string tourCode, string lang)
        {
            OnlineTourType tourType;
            tourType = GetTourBasicDetails(tourCode, lang);
            tourType.AdditionalTourDetails = GetAdditionalTourDetails(tourCode, lang);
            //tourType.additionalProductList = GetAdditionalProductList()
            return tourType;
        }

        public List<ReservationQRScannerListItem> GetReservationTodayListForQrScsanner(string eventCode)
        {
            string eventCondition = "";
            if (!string.IsNullOrEmpty(eventCode))
            {
                eventCondition = $@"  <filter type=""and"">
                                        <condition attribute=""new_event_code"" operator=""eq"" value=""{eventCode}"" />
                                      </filter>";
            }
            var fetchXmlEventRegistration = @$"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
              <entity name=""new_event_registration"">
                <attribute name=""new_name"" />
                <attribute name=""new_number_of_participants"" />
                <attribute name=""new_num"" />
                <attribute name=""new_group_name"" />
                <attribute name=""new_mobile_contact"" />
                <attribute name=""new_firstname_contact"" />
                <attribute name=""new_lastname_contact"" />
                <attribute name=""new_event_registrationid"" />
                <filter type=""and"">
                  <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                </filter>
                <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" alias=""EventReservation"">
                    <attribute name=""new_event_code"" />

                    {eventCondition}
                  </link-entity>
                <link-entity name=""new_event_registration_activity_day"" from=""new_event_registration"" to=""new_event_registrationid"" alias=""ad"">
                  <link-entity name=""new_activity_day"" from=""new_activity_dayid"" to=""new_activity_day"" alias=""ActivityDay"">
                    <attribute name=""new_start_datetime"" />
                    <attribute name=""new_activity_dayid"" />
                    <filter type=""and"">
                      <condition attribute=""new_start_datetime"" operator=""today"" />
                       <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                       <condition attribute=""new_activity_day_status"" operator=""not-in"">
                        <value>6</value>
                        <value>3</value>
                    </condition>
                    </filter> 
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>";

            var eventRegistrationList = _onlineReservationRepository.GetReservationTodayListForQrScsanner(fetchXmlEventRegistration, true);

            return eventRegistrationList;
        }
        public List<string> getTourNameInEnglishByTourCode(string tourCode)
        {
            List<string> res = new List<string>();
            //לשנות שיהיה בקיוסק- מה שאמורים לקבל מהקיוסק
            string fetchXml = this._newReservationRepository.StartFetchXml("new_tour_type");
            fetchXml += $@"
                    <attribute name=""new_name_en""/>
                    <attribute name=""createdon""/>
                    <attribute name=""new_tour_code""/>
                    <attribute name=""new_tour_code""/>
                    
                    <attribute name=""new_almog_interface_id""/>
                    <attribute name=""new_tour_typeid""/>
                    <order attribute=""new_name"" descending=""false""/>
                    <filter type=""and"">
                        <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}""/>
                    </filter>";
            fetchXml += this._newReservationRepository.EndFetchXml();
            var tourTypes = _onlineReservationRepository.getEntitytItem(fetchXml);
            var tourTypeEntity = tourTypes.Entities.FirstOrDefault();
            string nameEn = tourTypeEntity.GetAttributeValue<string>("new_name_en").ToString();
            res.Add(nameEn);

            return res;
        }
        public TourType transformBasicTourTypeToTourType(OnlineTourType tour)
        {
            var res = getTourNameInEnglishByTourCode(tour.BasicDetails.TourCode);
            TourType newtour = new TourType();
            newtour.Id = tour.BasicDetails.Id;
            newtour.NameHe = tour.BasicDetails.Name;
            newtour.NameEn = res[0];
            newtour.TourCode = tour.BasicDetails.TourCode;
            newtour.DescriptionHe = tour.BasicDetails.Description;
            newtour.DescriptionEn = res[1];
            newtour.IsNeedEvent = tour.BasicDetails.IsNeedEvent;
            //IsForMuseum

            //            public class TourType
            //    {

            //        public string DescriptionHe { get; set; }
            //        public string DescriptionEn { get; set; }
            //        public string TourCode { get; set; }
            //        public bool IsForIndividuals { get; set; }
            //        public bool IsForMuseum { get; set; }
            //        public bool IsNeedEvent { get; set; }
            //        public string EventReservationId { get; set; }
            //        public List<Product> ProductList { get; set; }
            //    }
            //}


            //public BasicTourType? BasicDetails { get; set; }
            //public AdditionalDetails? AdditionalTourDetails { get; set; }
            //public List<Product>? additionalProductList { get; set; }
            //public string? EventReservationId { get; set; }
            //public List<OnlineTourType>? TourTypesRequiredList { get; set; }
            return newtour;
        }
        public string getXmlToursForIndividualsByDate(List<Condition> conditionlist)
        {


            var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType", false, true);
            fetchXml += @$"<attribute name=""new_tour_typeid"" />
                <attribute name=""new_is_need_event"" />
                <attribute name=""new_price_desc_kiosk_en"" />
                <attribute name=""new_price_desc_kiosk_he"" />
                <attribute name=""new_name_kiosk_en"" />
                <attribute name=""new_duration_of_hours"" />
                <attribute name=""new_name_kiosk_he"" />
                <attribute name=""new_tour_code"" />
                <attribute name=""new_category_individuals"" />
                <attribute name=""new_category_museum"" />
                <attribute name=""new_event_reservations"" />
                <order attribute=""new_kiosk_sort_weight""/>
                <filter type=""and"">";
            var conditions = "";
            foreach (var condition in conditionlist)
            {
                conditions += @$"<condition attribute=""{condition.Name}"" operator=""{condition.Operator}"" value=""{condition.Value}"" />";
            }
            conditions += "</filter>";
            fetchXml += conditions;
            fetchXml += _newReservationRepository.EndFetchXml();
            return fetchXml;
        }
        public List<TourType> GetToursForIndividualsByDate(string strDate, bool returnShowsAndPlayOnly = false)
        {
            List<TourType> availableTours = new List<TourType>();
            Log.Information($"GetToursForIndividualsByDate start, date:{strDate}");
            var date = DateTime.ParseExact(strDate, "dd/MM/yyyy", null);
            int year = date.Year;
            int month = date.Month;

            // var dayOfWeek = date.DayOfWeek.ToString().ToLower();
            strDate = date.ToString("yyyy-MM-dd");//crm format date

            var conditionlist = new List<Condition>();
            conditionlist.Add(new Condition("statecode", "eq", "0"));
            if (returnShowsAndPlayOnly)
            {
                conditionlist.Add(new Condition("new_show_on_website", "eq", "1"));

            }
            else
            {
                conditionlist.Add(new Condition("new_show_on_kiosk", "eq", "1"));
            }

            string fetchXml = getXmlToursForIndividualsByDate(conditionlist);
            var ToursList = _newReservationRepository.GetTourTypeList(fetchXml);

            foreach (var tour in ToursList)
            {
                var closeDates = GetCloseDates(tour.TourCode, year, month, new DateTime(year, month, 1));

                // If the given date is NOT in the closed dates, add it to available list
                if (!closeDates.Contains(date.Date))
                {
                    availableTours.Add(tour);
                }
            }

            var yadVashemGuid = _config.GetValue<string>("YadVashemLocationGuid")!;
            availableTours.ForEach(tourType => tourType.ProductList = _newReservationService.GetProductsByTourType(tourType.TourCode, strDate, yadVashemGuid));
            Log.Information($"GetToursForIndividualsByDate end, res count:{availableTours.Count}");

            return availableTours;
        }


        public List<BasicTourDetails> GetNewVisitorsCenterDatesAndTimes(int year, int month)
        {
            //int year = 2025;
            // int month = 4;
            var conditionlist = new List<Condition>();
            conditionlist.Add(new Condition("new_is_need_event", "eq", "1"));
            // conditionlist.Add(new Condition("new_show_on_website", "eq", "1"));
            conditionlist.Add(new Condition("statecode", "eq", "0"));
            var ToursList = GetTourTypesWithEventOnly(conditionlist);
            List<BasicTourDetails> days = new List<BasicTourDetails>();

            var numberOfdaysInMonth = DateTime.DaysInMonth(year, month);
            List<DateTime> daysInMonth = new List<DateTime>();
            for (int day = 1; day <= numberOfdaysInMonth; day++)
            {
                daysInMonth.Add(new DateTime(year, month, day));
            }
            //string[] tourCodes = new string[] { "222", "223","235","234" };
            /* string[] tourIDs = new string[] {
                 "df69aed7-2db8-ee11-8133-00505691117e",
                 "d0f58d83-5ff2-ee11-8134-00505691117e",
                 "2af93f45-25de-ef11-813d-00505691117e",
                 "ff04240e-66dd-ef11-813d-00505691117e"
                 };*/
            //int i = 0;
            Dictionary<string, List<DateTime>> dictCloseByTourType = new Dictionary<string, List<DateTime>>();
            foreach (var tour in ToursList)
            {
                var closedats = GetCloseDates(tour.BasicDetails.TourCode, year, month, new DateTime(year, month, 1))!;
                dictCloseByTourType[tour.BasicDetails.TourCode] = closedats;
                List<DateTime> closedatsGood = closedats ?? new List<DateTime>();
                List<DateTime> tourDates = daysInMonth.Except(closedatsGood).ToList();
                foreach (var date in tourDates)
                {


                    var times = getDateTimeSlots(date.ToString("dd/MM/yyyy"), tour.BasicDetails.Id, tour.BasicDetails.TourCode, tour.BasicDetails.DurationActivity);
                    foreach (var time in times)
                    {
                        BasicTourDetails day = new BasicTourDetails();
                        day.Date = date.ToString("dd/MM/yyyy");
                        day.StartTime = time.Key.ToString();
                        day.NumberOfVisitors = 0;
                        day.TourCode = tour.BasicDetails.TourCode;
                        //this is the id of the event!!
                        day.ScheduleEventGuid = tour.BasicDetails.Id;
                        days.Add(day);
                    }
                    // GetDateArrivalTime(date,"ff04240e-66dd-ef11-813d-00505691117e",234,1)

                }
            }


            return days;
        }

        public List<OnlineTourType> GetTourTypesWithEventOnly(List<Condition> conditionlist)
        {
            try
            {
                //only today


                var res = GetToursBasicDetailsWithCondition(conditionlist, "he");
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllToursBasicDetails error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public Guid? GetInstitutionGuidStrict(string institutionSymbol, string institutionNumber)
        {
            // נוודא שיש לפחות אחד מהשדות
            if (string.IsNullOrEmpty(institutionSymbol) && string.IsNullOrEmpty(institutionNumber))
            {
                throw new Exception("At least one of Institution Symbol or Institution Number must be provided.");
            }

            Entity bySymbol = null;
            Entity byNumber = null;

            // חיפוש לפי סמל מוסד
            if (!string.IsNullOrEmpty(institutionSymbol))
            {
                var query = new QueryExpression("new_rsv_institution")
                {
                    ColumnSet = new ColumnSet("new_rsv_institutionid", "new_company_number"),
                    Criteria = {
                Conditions = {
                    new ConditionExpression("new_institution_symbol", ConditionOperator.Equal, institutionSymbol)
                }
            }
                };
                bySymbol = _crmService.RetrieveMultiple(query).Entities.FirstOrDefault();
            }

            // חיפוש לפי ח.פ.
            if (!string.IsNullOrEmpty(institutionNumber))
            {
                var query = new QueryExpression("new_rsv_institution")
                {
                    ColumnSet = new ColumnSet("new_rsv_institutionid", "new_institution_symbol"),
                    Criteria = {
                Conditions = {
                    new ConditionExpression("new_company_number", ConditionOperator.Equal, institutionNumber)
                }
            }
                };
                byNumber = _crmService.RetrieveMultiple(query).Entities.FirstOrDefault();
            }

            // אם קיימים גם לפי סמל וגם לפי ח.פ.
            if (bySymbol != null && byNumber != null)
            {
                if (bySymbol.Id != byNumber.Id)
                {
                    throw new Exception("Institution Symbol and Company Number refer to different records.");
                }
                return bySymbol.Id;
            }

            // אם נמצא לפי סמל בלבד
            if (bySymbol != null)
            {
                var existingNumber = bySymbol.GetAttributeValue<string>("new_company_number");
                if (!string.IsNullOrEmpty(institutionNumber) &&
                    !string.IsNullOrEmpty(existingNumber) &&
                    !string.Equals(existingNumber, institutionNumber, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Institution Symbol exists with a different Company Number.");
                }
                return bySymbol.Id;
            }

            // אם נמצא לפי ח.פ. בלבד
            if (byNumber != null)
            {
                var existingSymbol = byNumber.GetAttributeValue<string>("new_institution_symbol");
                if (!string.IsNullOrEmpty(institutionSymbol) &&
                    !string.IsNullOrEmpty(existingSymbol) &&
                    !string.Equals(existingSymbol, institutionSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Company Number exists with a different Institution Symbol.");
                }
                return byNumber.Id;
            }

            // לא נמצא כלום - חדש
            return null;
        }
        public async Task<Guid> InstitutionRegistration(InstitutionRegistration institution)
        {
            try
            {
                Log.Information("InstitutionRegistration start");

                Entity institutionRegistrationEntity = new Entity("new_rsv_institution");

                institutionRegistrationEntity["new_name"] = institution.InstitutionName;
                institutionRegistrationEntity["new_characteristics_institution"] = new EntityReference("new_characteristics_institution", Guid.Parse(institution.InstitutionType.Id));

                if (!string.IsNullOrWhiteSpace(institution.InstitutionSymbol))
                {
                    institutionRegistrationEntity["new_institution_symbol"] = institution.InstitutionSymbol;
                }

                if (!string.IsNullOrWhiteSpace(institution.InstitutionNumber))
                {
                    institutionRegistrationEntity["new_company_number"] = institution.InstitutionNumber;
                }

                institutionRegistrationEntity["new_address"] = $"{institution.Street} {institution.Number}";
                institutionRegistrationEntity["new_city"] = institution.City;
                institutionRegistrationEntity["new_zipcode"] = institution.ZipCode;
                institutionRegistrationEntity["new_email"] = institution.InstitutionEmail;
                institutionRegistrationEntity["new_email_for_sending_invoices"] = institution.BillingEmail;
                institutionRegistrationEntity["new_telephone"] = institution.InstitutionPhone;
                institutionRegistrationEntity["new_fax"] = institution.InstitutionFax;
                institutionRegistrationEntity["new_contact_name"] = institution.ContactPersonName;

                Guid finalGuid = _crmService.Create(institutionRegistrationEntity); // הנחה: השירות CRM אינו אסינכרוני

                // שינוי סטטוס
                SetStateRequest req = new SetStateRequest
                {
                    EntityMoniker = new EntityReference("new_rsv_institution", finalGuid),
                    State = new OptionSetValue(1),
                    Status = new OptionSetValue(2)
                };
                _crmService.Execute(req);

                if (!string.IsNullOrWhiteSpace(institution.InstitutionEmail))
                {
                    Log.Information($"Attempting to send email to {institution.InstitutionEmail}");

                    var emailSent = await SendInstitutionEmail(institution.InstitutionName, institution.InstitutionEmail);

                    if (!emailSent)
                    {
                        Log.Error($"Failed to send email to {institution.InstitutionEmail}");
                        throw new Exception("המוסד נרשם בהצלחה, אך שליחת הדוא\"ל נכשלה. ודא שהכתובת תקינה ונסה שוב.");
                    }
                }

                Log.Information($"InstitutionRegistration end, institutionGuid: {finalGuid}");
                return finalGuid;
            }
            catch (Exception ex)
            {
                Log.Error($"InstitutionRegistration error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SendInstitutionEmail(string institutionName, string email)
        {
            try
            {
                Log.Information($"Start sending registration email to institution: {institutionName}, email: {email}");

                var subject = "יד ושם - בקשת רישום התקבלה";
                var body = $@"
<html>
<body dir='rtl' style='text-align: right; font-family: Arial, sans-serif;'>
    <p>שלום וברכה {institutionName},</p>
    <p>בקשתך לרישום התקבלה בהצלחה.</p>
   <p>להמשך תהליך הכן את הטופס הבא:</p>
    <p><strong>תעודת עוסק מורשה</strong></p>
    <p>אם יש לך מוסד משלם אנא צרף בנוסף:</p>        
    <ul>
        <li>תעודת עוסק מורשה של המוסד המשלם</li>
        <li>אישור של המוסד המשלם שאכן מוכן לשלם חובות ותשלומים של המוסד המקבל</li>
    </ul>
    <p>בברכה,<br/>יד ושם</p>
</body>
</html>";

                var mailInfo = new MailInfo
                {
                    To = new List<string> { email },
                    From = _config.GetValue<string>("noReplayYadVashemEmail")!,
                    Bcc = new List<string> { _config.GetValue<string>("bcc")! },
                    Subject = subject,
                    Message = body,
                    Files = new List<string>()
                };

                var json = JsonConvert.SerializeObject(mailInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = new HttpClient
                {
                    BaseAddress = new Uri(_config.GetValue<string>("sendMailUrl")!)
                };

                var response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information($"Registration email sent successfully to {email}");
                    return true;
                }
                else
                {
                    Log.Error($"Failed to send email to {email}. StatusCode: {response.StatusCode}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Exception while sending email to {email}: {ex.Message}");
                return false;
            }
        }

        public List<InstitutionType> GetInstitutionTypes()
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_characteristics_institution");
                fetchXml += $@"
               <attribute name=""new_characteristics_institutionid"" />  
                <attribute name=""new_name""/>
                <attribute name = ""createdon"" />
                <order attribute=""new_name"" descending=""false""/>
                <filter type=""and"">
                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                <condition attribute = ""new_display_on_website"" operator= ""eq"" value = ""1"" />
                </filter>";

                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetInstitutionTypes(fetchXml);
                Log.Information($"GetInstitutionTypes end, res count:{res.Count}");
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetInstitutionTypes error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public List<IdValue> GetCitiesList()
        {
            try
            {
                var fetchXml = _newReservationRepository.StartFetchXml("new_city");
                fetchXml += $@"
               <attribute name=""new_cityid"" />  
                <attribute name=""new_name""/>
                <attribute name = ""createdon"" />
                <order attribute=""new_name"" descending=""false""/>
                <filter type=""and"">
                <condition attribute=""statecode"" operator=""eq"" value=""0""/>
                </filter>";

                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _onlineReservationRepository.GetCitiesList(fetchXml);
                Log.Information($"GetCitiesList end, res count:{res.Count}");
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetCitiesList error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public List<OnlineTourType> Commit(string lang, InstituitionType instituitionType)
        {
            string instituitionTypeName = "";
            switch (instituitionType)
            {
                case InstituitionType.TRAVEL_AGENTS:
                    instituitionTypeName = "new_is_for_travel_agents";
                    break;
                case InstituitionType.EDUCATIONAL_GROUPS:
                    instituitionTypeName = "new_is_for_institutions";
                    break;
                case InstituitionType.PRIVATE:
                    instituitionTypeName = "new_is_for_private";
                    break;
            }
            string connectionString = _config.GetValue<string>("connectionString");
            string query = @$"select t.new_tour_typeId,
                                    t.new_name,
                                    t.new_name_en,
                                    t.new_description_{lang},
                                    t.new_short_desc_{lang},
                                    t.new_tour_code,
                                    t.new_max_reservations_per_day,
                                    t.new_age,
                                    t.new_tag,
                                    t.new_duration_of_hours,
                                    t.new_relevant_day_sunday,
                                    t.new_relevant_day_monday,
                                    t.new_relevant_day_tuesday,
                                    t.new_target_population,
                                    t.new_grade_1,
                                    t.new_grade_2,
                                    t.new_grade_3,
                                    t.new_grade_4,
                                    t.new_grade_5,
                                    t.new_grade_6,
                                    t.new_grade_7,
                                    t.new_grade_8,
                                    t.new_grade_9,
                                    t.new_grade_10,
                                    t.new_grade_11,
                                    t.new_grade_12,
                                    t.new_relevant_day_wednesday,
                                    t.new_relevant_day_thursday,
                                    t.new_relevant_day_friday,
                                    t.new_is_need_event,
                                    t.new_tab_to_display,
                                    t.new_max_participants,
                                    t.new_max_participants_in_group,
                                    t.new_difference_between_arrival_to_start,
                                    t.new_sort_weight_{lang},
                                    t.new_event_reservations,
                                    t.new_discount_type,
                                    t.new_discount,
                                    t.new_is_package,
                                    t.new_cancel_registration_fees,
                                    l.new_languageId
                            FROM new_tour_type t 
                            left join new_new_tour_type_new_language l on t.new_tour_typeId = l.new_tour_typeid 
                            left join new_new_tour_type_new_tag g on t.new_tour_typeId = g.new_tour_typeid 
                            where t.statuscode = 1 and t.new_show_on_website = 1 and t.{instituitionTypeName} = 1
                            order by new_sort_weight_{lang}, new_name";
            //Log.Information($"SQL Query: {query}");

            DataTable dt = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter da = new SqlDataAdapter(query, connection))
                {
                    try
                    {
                        connection.Open();
                        da.Fill(dt);

                    }
                    catch (Exception ex)
                    {
                        Log.Error("error" + ex.Message);
                    }
                }
            }
            List<OnlineTourType> tours = MapTours(dt, lang);
            return tours;
        }

        public List<OnlineTourType> MapTours(DataTable dt, string lang)
        {
            try
            {
                List<OnlineTourType> tours = new List<OnlineTourType>();
                foreach (DataRow row in dt.Rows)
                {
                    var id = row["new_tour_typeId"].ToString();
                    OnlineTourType tour = new OnlineTourType();
                    BasicTourType basicTourDetails;
                    basicTourDetails = new BasicTourType
                    {
                        Id = id,
                        Name = GetValue<string>(row, "new_name"),
                        Description = GetValue<string>(row, $"new_description_{lang}"),
                        DescriptionShort = GetValue<string>(row, $"new_short_desc_{lang}"),
                        TourCode = GetValue<string>(row, "new_tour_code"),
                        MaxReservationsPerDay = GetValue<int?>(row, "new_max_reservations_per_day"),
                        IsRecommended = GetBool(row, "new_is_recommended"),
                        Age = GetValue<string>(row, "new_age"),
                        DurationActivity = GetValue<decimal>(row, "new_duration_of_hours"),
                        DaysAllowed = new List<string>(),
                        GradsAllowed = new List<int>(),
                        Tags = new List<string>(),
                        GuidLanguages = new List<KeyValuePair<Guid, string>>(),
                        Info = new List<KeyValuePair<string, string>>(),
                        IsNeedEvent = GetBool(row, "new_is_need_event"),
                        isPackage = GetBool(row, "new_is_package"),
                        CancelRegistrationFees = GetValue<bool?>(row, "new_cancel_registration_fees"),
                        DiscountType = GetValue<int?>(row, "new_discount_type") is int d ? (DiscountType?)d : null,
                        Discount = GetValue<decimal?>(row, "new_discount"),
                        ltr_rtl = GetValue<int?>(row, "new_ltr_rtl") is int l ? (LTR_RTL)l : 0,
                        TabToDisplayIn = row["new_tab_to_display"] != DBNull.Value
                        ? new KeyValuePair<int, string>(Convert.ToInt32(row["new_tab_to_display"]), row["new_tab_to_display"].ToString())
                        : default,
                        maxVisitors = GetValue<int>(row, "new_max_participants"),
                        MinVisitors = GetValue<int>(row, "new_min_participants"),
                        maxVisitorsInGroup = GetValue<int>(row, "new_max_participants_in_group"),
                        TargetPopulation = GetValue<int?>(row, "new_target_population") is int tp
                        ? new KeyValuePair<int, string>(tp, row["new_target_population"].ToString())
                        : null,

                    };
                    basicTourDetails.Info = new List<KeyValuePair<string, string>>();
                    var difference = GetValue<int?>(row, "new_difference_between_arrival_to_start");
                    if (difference > 0)
                    {
                        basicTourDetails.IsGuided = true;
                    }
                    else
                    {
                        basicTourDetails.IsGuided = false;
                    }

                    string durationSuffix = lang == "he" ? "שעות" : "hours";
                    basicTourDetails.Info.Add(new KeyValuePair<string, string>("tourLength",
                    (Math.Round(basicTourDetails.DurationActivity, 1) % 1 == 0 ? Math.Round(basicTourDetails.DurationActivity) : basicTourDetails.DurationActivity) + " " + durationSuffix));

                    basicTourDetails.Info.Add(new KeyValuePair<string, string>("type", ""));
                    basicTourDetails.Info.Add(new KeyValuePair<string, string>("languages", ""));
                    basicTourDetails.Info.Add(new KeyValuePair<string, string>("day", ""));
                    basicTourDetails.Info.Add(new KeyValuePair<string, string>("hour", ""));
                    // ימי השבוע
                    var days = new[]
                    {
                        ("new_relevant_day_sunday", "Sunday"),
                        ("new_relevant_day_monday", "Monday"),
                        ("new_relevant_day_tuesday", "Tuesday"),
                        ("new_relevant_day_wednesday", "Wednesday"),
                        ("new_relevant_day_thursday", "Thursday"),
                        ("new_relevant_day_friday", "Friday")
                    };

                    foreach (var (columnName, dayName) in days)
                    {
                        if (GetBool(row, columnName))
                            basicTourDetails.DaysAllowed.Add(dayName);
                    }

                    // כיתות
                    for (int i = 1; i <= 12; i++)
                    {
                        if (GetBool(row, $"new_grade_{i}"))
                            basicTourDetails.GradsAllowed.Add(i);
                    }
                    //}

                    // שפות
                    if (dt.Columns.Contains("new_languageid") && row["new_languageid"] != DBNull.Value)
                    {
                        var langId = Guid.Parse(row["new_languageid"].ToString());
                        var langName = dt.Columns.Contains("new_language_name") ? row["new_language_name"]?.ToString() ?? "" : "";
                        if (!basicTourDetails.GuidLanguages.Any(x => x.Key == langId))
                            basicTourDetails.GuidLanguages.Add(new KeyValuePair<Guid, string>(langId, langName));
                    }

                    // תגיות
                    if (dt.Columns.Contains("new_tag") && row["new_tag"] != DBNull.Value)
                    {
                        var tag = row["new_tag"]?.ToString();
                        if (!string.IsNullOrEmpty(tag) && !basicTourDetails.Tags.Contains(tag))
                            basicTourDetails.Tags.Add(tag);
                    }
                    tour.BasicDetails = basicTourDetails;
                    tours.Add(tour);

                }

                return tours;
            }
            catch (Exception ex)
            {

                throw;
            }

        }


        private static T? GetValue<T>(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName) || row[columnName] == DBNull.Value)
                return default;

            return (T)Convert.ChangeType(row[columnName], Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }


        private static bool GetBool(DataRow row, string columnName) =>
    row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value && Convert.ToBoolean(row[columnName]);


        public List<Dictionary<string, object>> ConvertDataTableToList(DataTable table)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                list.Add(dict);
            }

            return list;
        }

        public OnlineTourType ConvertDataRowToTourType(DataRow row)
        {
            return new OnlineTourType
            {

            };
        }
        public ActivityDaySummary GetActivityDaySummary(Guid activityDayId)
        {
            var bidItems = GetBidItemsSummaryByActivityDay(activityDayId);
            var totalParticipants = GetTotalParticipantsByActivityDay(activityDayId);
            var totalArrivedParticipants = GetTotalParticipantsArrivedByActivityDay(activityDayId);

            return new ActivityDaySummary
            {
                BidItems = bidItems,
                TotalParticipants = totalParticipants,
                TotalArrivedParticipants = totalArrivedParticipants

            };
        }


        public List<BidItemSummary> GetBidItemsSummaryByActivityDay(Guid activityDayId)
        {
            var fetchXml = $@"
<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='new_bid'>
    <attribute name='new_bidid' />
    <attribute name='new_amount' />
    <attribute name='new_event_registration' />
    <filter type='and'>
      <condition attribute='statecode' operator='eq' value='0' />
    </filter>
    <link-entity name='new_event_registration' from='new_event_registrationid' to='new_event_registration' link-type='inner' alias='registration'>
      <link-entity name='new_event_registration_activity_day' from='new_event_registration' to='new_event_registrationid' link-type='inner' alias='regactivity'>
        <filter type='and'>
          <condition attribute='new_activity_day' operator='eq' value='{activityDayId}' />
        </filter>
      </link-entity>
    </link-entity>
    <link-entity name='new_yv_product' from='new_yv_productid' to='new_yv_product' link-type='inner' alias='product'>
      <attribute name='new_name' />
      <filter>
        <condition attribute='new_display_on_counter' operator='eq' value='1' />
      </filter>
    </link-entity>
  </entity>
</fetch>";
            return _onlineReservationRepository.GetBidItemsSummaryByActivityDay(fetchXml);
        }





        public int GetTotalParticipantsByActivityDay(Guid activityDayId)
        {
            var fetchXml = $@"
<fetch top='1'>
  <entity name='new_activity_day'>
    <attribute name='new_actual_number_of_participants' />
    <filter>
      <condition attribute='new_activity_dayid' operator='eq' value='{activityDayId}' />
    </filter>
  </entity>
</fetch>";

            return _onlineReservationRepository.GetTotalParticipantsByActivityDay(fetchXml);
        }
        public int GetTotalParticipantsArrivedByActivityDay(Guid activityDayId)
        {
            var fetchXml = $@"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='new_event_registration_activity_day'>
                    <attribute name='new_event_registration_activity_dayid' />
                    <filter type='and'>
                      <condition attribute='new_activity_day' operator='eq' value='{activityDayId}' />
                    </filter>
                    <link-entity name='new_event_registration' from='new_event_registrationid' to='new_event_registration' link-type='inner' alias='registration'>
                      <attribute name='new_actual_number_of_participants' />
                      <filter type='and'>
                        <condition attribute='new_did_group_arrive' operator='eq' value='1' />
                      </filter>
                    </link-entity>
                  </entity>
                </fetch>";
            return _onlineReservationRepository.GetTotalParticipantsArrivedByActivityDay(fetchXml);
        }

        public bool updateEventRegistrationDate(OnlineReservation eventRegistration)
        {
            try
            {
                Trace.WriteLine($"updatEventRegisrationDate ReservationNumber : {eventRegistration.ReservationNumber}");
                //Entity eventRegistrationEntity = new Entity("new_event_registration", Guid.Parse(eventRegistration.Guid));

                var tourType = GetTourByCode(eventRegistration.TourType.BasicDetails.TourCode);
                List<Entity> entitiesToUpdate = new List<Entity>(); //update only if  IsThereRoomInTheResource for all groups otherise do nothing.
                                                                    //dates
                string format = "dd/MM/yyyy"; // ציון הפורמט המתאים
                eventRegistration.ReservationDate = DateTime.ParseExact(eventRegistration.reservationDateString, format, null);

                Log.Information($"The updatEventRegisrationDate date is: {eventRegistration.ReservationDate}");


                eventRegistration.StartDateTime = new DateTime(eventRegistration.ReservationDate.Value.Year, eventRegistration.ReservationDate.Value.Month, eventRegistration.ReservationDate.Value.Day, eventRegistration.StartTime.hours, eventRegistration.StartTime.minutes, 0);

                if (eventRegistration.StartTime != null && eventRegistration.ReservationDate != null)
                {
                    string startTime = eventRegistration.StartTime.hours.ToString() + ":" + eventRegistration.StartTime.minutes.ToString();
                    eventRegistration.StartDateTime = new DateTime(eventRegistration.ReservationDate.Value.Year, eventRegistration.ReservationDate.Value.Month, eventRegistration.ReservationDate.Value.Day, eventRegistration.StartTime.hours, eventRegistration.StartTime.minutes, 0);
                }
                if (eventRegistration.StartDateTime < DateTime.Now)
                {
                    throw new InvalidOperationException("לא ניתן לבחור שעה או תאריך לפני הזמן הנוכחי.");
                }
                var activityDayTodayList = _newReservationService.GetActivityDayListByEventReservation(eventRegistration.TourType.EventReservationId, eventRegistration.StartDateTime.Value);
                var activityDay = activityDayTodayList.Entities.ToList().Find(activityDay => activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime() == eventRegistration.StartDateTime);
                if (activityDay != null)
                {
                    if (activityDay.GetAttributeValue<int>("new_actual_number_of_participants") + eventRegistration.NumberOfVisitors > (int)tourType.GetAttributeValue<AliasedValue>("eventReservation.new_max_participants_per_event").Value)
                    {
                        throw new Exception("NO_ROON");
                    }
                }

                /////Update activity day start date and end date:
                string fetchXml = getEventRegistrationActivityDay(Guid.Parse(eventRegistration.Guid));
                var activityDaysList = _onlineReservationRepository.getEntitytItem(fetchXml);
                if (activityDaysList.Entities.Count != 1)
                {

                    Trace.WriteLine(DateTime.Now + ": UpdateActivityDay - Error: found " + activityDaysList.Entities.Count + " ActivityDays instad of 1");
                    return false;
                }


                //eventRegistration.ActivityDayEventRegistrationGuid = CreateOrUpdateActivityDayEventRegistration(eventRegistration, false).ToString();
                eventRegistration.ActivityDayEventRegistrationGuid = activityDaysList[0].Id.ToString();
               var activityDayGuid = GetActivityDayIdCreateActivityDayIfNotExists(eventRegistration);
                Entity eventRegistrationActivityDayEntity = new Entity("new_event_registration_activity_day", activityDaysList[0].Id);

                eventRegistrationActivityDayEntity["new_activity_day"] = new EntityReference("new_activity_day", activityDayGuid);

                entitiesToUpdate.Add(eventRegistrationActivityDayEntity);


                //Entity activityDayEntity = new Entity("new_activity_day",);

                //activityDayEntity["new_start_datetime"] = eventRegistration.StartDateTime;
                //activityDayEntity["new_name"] = "Activity Day -> " + eventRegistration.StartDateTime?.ToString("yyyy-MM-dd HH:mm:ss") + tourType.GetAttributeValue<string>("new_name");
                var duration = tourType.GetAttributeValue<decimal>("new_duration_of_hours");
                //activityDayEntity["new_end_datetime"] = eventRegistration.StartDateTime?.AddHours(decimal.ToDouble(duration));
                
                //entitiesToUpdate.Add(activityDayEntity);

                //    // reservationEntity["new_number_of_times_updated_online"] = (GetReservationNumberOfTimesUpdatedOnline(Guid.Parse(onlineReservation.Guid)) + 1).ToString();
                //    //לשאול את דבורה מה זה .
                //האם יכול להיות יותר מקבוצה אחת בארוע?
        

                var qe = new QueryExpression("new_itinerary_item");
                qe.ColumnSet = new ColumnSet(true);
                qe.Criteria.AddCondition("new_activity_day", ConditionOperator.Equal, eventRegistration.ActivityDayEventRegistrationGuid);
                var linkedItems = _crmService.RetrieveMultiple(qe);
                foreach (var itinItem in linkedItems.Entities)
                {
                    var itinToUpdate = new Entity("new_itinerary_item", itinItem.Id);
                    itinToUpdate["new_start_time"] = eventRegistration.StartDateTime; ;
                    itinToUpdate["new_end_time"] = eventRegistration.StartDateTime?.AddHours(decimal.ToDouble(duration));
                    entitiesToUpdate.Add(itinToUpdate);
                }




                entitiesToUpdate.ForEach(entity => _crmService.Update(entity));

                //}
                //catch (Exception ex)
                //{
                //    Trace.WriteLine(DateTime.Now + ": UpdateActivityDay - Exception: " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace);
                //    return new CrmResultItem(false, "UpdateTourReservation - Exception: " + ex.Message);
                //}

            }
            catch (Exception ex)
            {
                Log.Error($"updateEventRegisrationDate error: {ex.Message}");
                return false;
            }

            return true;
        }

    }
}


