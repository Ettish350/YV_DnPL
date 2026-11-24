using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Serilog;
using Service.Services.Interfaces;


namespace Service.Services
{
    public class NewReservationService : INewReservationService
    {
        public readonly int TIME_SLOT_MINTUE = 10;
        private readonly ICRMConnection _crmService;
        private readonly INewReservationRepository _newReservationRepository;
        private readonly IConfiguration _config;
        private readonly IHelperService _helperService;
        public NewReservationService(ICRMConnection crmConnection, INewReservationRepository newReservationRepository, IConfiguration configuration, IHelperService helperService)
        {
            _crmService = crmConnection;
            _newReservationRepository = newReservationRepository;
            _config = configuration;
            _helperService = helperService;
        }

        public Guid SaveMuseumSlotTime(BasicTourDetails tourDetails)
        {
            try
            {
                Log.Information($"SaveMuseumSlotTime start tourDetails:{tourDetails.StartTime}, {tourDetails.Date}, NumberOfVisitors:{tourDetails.NumberOfVisitors}.");
                tourDetails.StartDateTime = DateTime.ParseExact(tourDetails.Date + tourDetails.StartTime, "dd/MM/yyyyHH:mm", null);
                var tourType = GetTourByCode("104");//סיור במוזיאון 
                var resourceGuid = tourType.GetAttributeValue<EntityReference>("new_resource").Id;
                if (IsThereAvailablePlacesInResourceAtSlotTime(tourDetails, resourceGuid))
                {
                    bool isForUpdate = false;
                    Guid tryParseGuid = new Guid();
                    if (!string.IsNullOrEmpty(tourDetails.ScheduleEventGuid) && Guid.TryParse(tourDetails.ScheduleEventGuid, out tryParseGuid))
                    {
                        isForUpdate = true;
                        Log.Information($"SaveMuseumSlotTime current schedule is for update:{tourDetails.ScheduleEventGuid}");
                    }
                    var scheduleMuseumGuid = _newReservationRepository.CreateOrUpdateResourceScheduleEvent(tourDetails, resourceGuid, tourType, isForUpdate);
                    Log.Information($"SaveMuseumSlotTime end, scheduleMuseumGuid is {scheduleMuseumGuid}");
                    return scheduleMuseumGuid;
                }
                throw new Exception("NO_ROOM");
            }
            catch (Exception ex)
            {

                Log.Error($"SaveMuseumSlotTime error, {ex.Message}.");
                throw new Exception(ex.Message);
            }
        }
        
        public bool IsThereAvailablePlacesInResourceAtSlotTime(BasicTourDetails tourDetails, Guid resourceGuid)
        {
            try
            {

                //if the museum is closed at start time return false
                var resourceClosedTimes = GetResourceClosedTimes(tourDetails.StartDateTime);
                foreach (var closedTime in resourceClosedTimes.Entities)
                {
                    if (closedTime.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime() <= tourDetails.StartDateTime && closedTime.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime() > tourDetails.StartDateTime)
                    {
                        return false;
                    }
                }
                int totalParticipants = tourDetails.NumberOfVisitors;
                int maxVisitors = GetResourceCapacity(resourceGuid);
                var eventsList = GetResourceScheduleEventsByDate(tourDetails.StartDateTime, resourceGuid, tourDetails.ScheduleEventGuid);
                foreach (var evntItemEntity in eventsList.Entities)
                {
                    if (evntItemEntity.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().Equals(tourDetails.StartDateTime))
                        totalParticipants += evntItemEntity.GetAttributeValue<int>("new_number_of_participants");
                }
                if (totalParticipants > maxVisitors)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"IsThereAvailablePlacesInResourceAtSlotTime error, {ex.Message} , resource:{resourceGuid}.");
                throw new Exception(ex.Message);
            }
        }
        private EntityCollection GetResourceClosedTimes(DateTime dateTime)
        {
            string dateTimeStr = dateTime.ToString("yyyy-MM-dd");

            string fetchXml = _newReservationRepository.StartFetchXml("new_closed_days_and_activities", "ClosedDates");
            fetchXml += $@" <attribute name=""new_from_closing_date"" />
                            <attribute name=""new_to_closing_date"" />
                            <filter type=""and"">
                              <condition attribute=""new_closing_type"" operator=""not-in"">
                                <value>4</value>
                                <value>5</value>
                                <value>7</value>
                              </condition>
                              <condition attribute=""new_from_closing_date"" operator=""on-or-before"" value='{dateTimeStr}' />
                              <condition attribute=""new_to_closing_date"" operator=""on-or-after""  value='{dateTimeStr}' />
                              <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                            </filter>";
            fetchXml += _newReservationRepository.EndFetchXml();
            var query = new FetchExpression(fetchXml);
            return _crmService.RetrieveMultiple(query);
        }
        public EntityCollection GetResourceScheduleEventsByDate(DateTime date, Guid resourceGuid, string? currenScheduleEventGuid = "", bool aggregate = false)
        {
            string dateTimeStr = date.ToString("yyyy-MM-dd");
            string currentEventItemCondition = "";
            Guid tryParseGuid;

            if (!string.IsNullOrEmpty(currenScheduleEventGuid) && Guid.TryParse(currenScheduleEventGuid, out tryParseGuid))
            {
                currentEventItemCondition = @$"<condition attribute=""new_event_itemid"" operator=""ne"" value='{tryParseGuid}' />";
            }

            var fetchXml = _newReservationRepository.StartFetchXml("new_event_item", "EventItem", aggregate);
            if (aggregate)
            {
                fetchXml += "<attribute name='new_number_of_participants' alias='new_number_of_participants_sum' aggregate='sum' /> ";
            }
            else
            {
                fetchXml += $@"<attribute name=""new_number_of_participants"" />
                               <attribute name=""new_start_datetime"" />";
            }
            fetchXml += $@"<filter type=""and"">
                          <condition attribute=""new_resource"" operator=""eq"" value='{resourceGuid}' />
                          <condition attribute=""new_is_resource"" operator=""eq"" value=""1"" />
                          <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                          <condition attribute=""new_start_datetime"" operator=""on"" value='{dateTimeStr}' />
                          {currentEventItemCondition}
                        </filter>";
            fetchXml += _newReservationRepository.EndFetchXml();
            var query = new FetchExpression(fetchXml);
            return _crmService.RetrieveMultiple(query);

        }
        private int GetResourceCapacity(Guid resourceGuid)
        {

            int capacity = 0;
            var resourceEntity = _crmService.Retrieve("new_resource", resourceGuid, new[] { "new_capacity" });
            if (resourceEntity != null)
            {
                capacity = resourceEntity.GetAttributeValue<int>("new_capacity");
            }
            return capacity;
        }
        private Entity GetFullResource(Guid resourceGuid)
        {
            var resourceEntity = _crmService.Retrieve("new_resource", resourceGuid, true);
            return resourceEntity;
        }

        public Reservation CreateReservation(Reservation reservation)
        {
            try
            {

                Log.Information($"CreateReservation");
                Log.Information(JsonConvert.SerializeObject(reservation));
                // start reservation:{reservation.BasicTourDetails.StartTime} {reservation.BasicTourDetails.Date}, {reservation.AdditionalTourDetails.FirstName} {reservation.AdditionalTourDetails.LastName}, {reservation.AdditionalTourDetails.Email}, NumberOfVisitors:{reservation.BasicTourDetails.NumberOfVisitors}, scheduleGuid:{reservation.BasicTourDetails.ScheduleEventGuid}, language: {reservation.Language}.");
                reservation.BasicTourDetails.StartDateTime = DateTime.ParseExact(reservation.BasicTourDetails.Date + reservation.BasicTourDetails.StartTime, "dd/MM/yyyyHH:mm", null);

                if (!IsMuseumScheduleActiveOrIsThereAvailablePlaces(reservation.BasicTourDetails))
                {
                    throw new Exception("NO_ROOM");
                }
                Entity tourType;
                var isSetInactiveBeforePayment = false;
                if (reservation.AdditionalTourDetails.IsWithAudioGuide)
                {
                    isSetInactiveBeforePayment = true;
                    tourType = GetTourByCode("103");//סיור במוזיאון עם אוזניות 
                }
                else
                {
                    tourType = GetTourByCode("104");//סיור במוזיאון 
                }
                var lang = reservation.Language;
                if (lang == "he")
                {
                    lang = "iw";//in crm Hebrew lang code is wi and not he...
                }
                var languageGuid = _newReservationRepository.GetGuidByFeild("new_language", "new_language_code", lang);
                var contactGuid = _helperService.GetContactByEmailOrMobile(reservation.AdditionalTourDetails.Email, reservation.AdditionalTourDetails.PhoneNumber);
                var reservationGuid = _newReservationRepository.CreateReservation(reservation, tourType, languageGuid, contactGuid, isSetInactiveBeforePayment);

                Log.Information($"CreateReservation end ReservationGuid:{reservation.Guid}, reservationNumber:{reservation.ReservationNumber}.");
                return reservation;
            }
            catch (Exception ex)
            {

                Log.Error($"CreateReservation error, Exception:{ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public bool IsMuseumScheduleActiveOrIsThereAvailablePlaces(BasicTourDetails basicTourDetails)
        {
            var eventItemEntity = _crmService.Retrieve("new_event_item", Guid.Parse(basicTourDetails.ScheduleEventGuid), new[] { "statecode" });
            if (eventItemEntity.GetAttributeValue<OptionSetValue>("statecode").Value == 1)//inactive ,more than 10 minutes passed.
            {
                if (IsThereAvailablePlacesInResourceAtSlotTime(basicTourDetails, _config.GetValue<Guid>("MuseumResourceId")))
                {
                    _helperService.ActivateEntity(eventItemEntity);
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        public EventRegistration CreateEventRegistration(EventRegistration eventRegistration, bool isForUpdate) {
            try
            {
                Log.Information($"CreateEventRegistration ");
                Log.Information(JsonConvert.SerializeObject(eventRegistration));
                eventRegistration.BasicTourDetails.StartDateTime = DateTime.ParseExact(eventRegistration.BasicTourDetails.Date + eventRegistration.BasicTourDetails.StartTime, "dd/MM/yyyyHH:mm", null);
                var isNeetToSetToActive = false;
                if (eventRegistration.TourType.IsForIndividuals && isForUpdate)
                {//Check if event registration is active - if 5 min didn't pass yet.
                    var entity = _crmService.Retrieve("new_event_registration", Guid.Parse(eventRegistration.Guid), new string[] { "statecode" });
                    if (!entity.GetAttributeValue<OptionSetValue>("statecode").Value.Equals(0))
                    {// because 5 min have passed, need to check again if there is avalible places for visitors.
                        isNeetToSetToActive = true;
                        CreateOrUpdateActivityDayEventRegistration(eventRegistration, isNeetToSetToActive);
                    }

                     var contactGuid = _helperService.GetContactByEmailOrMobile(eventRegistration.AdditionalTourDetails.Email, eventRegistration.AdditionalTourDetails.PhoneNumber);
                     contactGuid = _newReservationRepository.CreateOrUpdateContact(contactGuid, eventRegistration.AdditionalTourDetails);
                    eventRegistration.ContactGuid = contactGuid.ToString();
                }
                var eventRegistrationGuid = _newReservationRepository.CreateOrUpdateEventRegistration(eventRegistration ,isForUpdate, isNeetToSetToActive);
                eventRegistration.Guid = eventRegistrationGuid.ToString();
                var newReservationEntity = _crmService.Retrieve("new_event_registration", eventRegistrationGuid, new[] { "new_num" });
                eventRegistration.EventRegistrationNumber = newReservationEntity.GetAttributeValue<string>("new_num");
                if (eventRegistration.TourType.IsForIndividuals && !isForUpdate)
                {
                    eventRegistration.ActivityDayEventRegistrationGuid= CreateOrUpdateActivityDayEventRegistration(eventRegistration, false).ToString();
                }
                Log.Information($"CreateEventRegistration end, eventRegistrationGuid:{eventRegistrationGuid}");
                return eventRegistration;
            }
            catch (Exception ex){
                Log.Error($"CreateEventRegistration exception: {ex.Message}");
                throw new Exception(ex.Message);
            }

        }
        public Guid CreateOrUpdateActivityDayEventRegistration(EventRegistration eventRegistration,bool isNeetToSetToActive) {
            var activityDayGuid = GetActivityDayIdCreateActivityDayIfNotExists(eventRegistration);
            Guid eventRegistrationActivityDayGuid = _newReservationRepository.CreateOrUpdateActivityDayEventRegistration(eventRegistration, activityDayGuid, isNeetToSetToActive);
            return eventRegistrationActivityDayGuid;
        }
        public Guid GetActivityDayIdCreateActivityDayIfNotExists(EventRegistration eventRegistration) {
            try
            {
                Log.Information($"GetActivityDayIdCreateActivityDayIfNotExists start, date:{eventRegistration.BasicTourDetails.Date} {eventRegistration.BasicTourDetails.StartTime}");
                var eventReservationEntity = _crmService.Retrieve("new_event_reservations", new Guid(eventRegistration.TourType.EventReservationId), new[] { "new_max_participants_per_event", "new_total_participants" });
                var maxVisitors = eventReservationEntity.GetAttributeValue<int>("new_max_participants_per_event");
                var activityDayTodayList = GetActivityDayListByEventReservation(eventRegistration.TourType.EventReservationId, eventRegistration.BasicTourDetails.StartDateTime);
                var entity = activityDayTodayList.Entities.ToList().Find(activityDay => activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().Equals(eventRegistration.BasicTourDetails.StartDateTime));
                var activityDayGuid = Guid.Empty;
                if (entity != null)
                {
                    var sumOfParticipants = entity.GetAttributeValue<int>("new_number_of_participants") + eventRegistration.BasicTourDetails.NumberOfVisitors;
                    if (!string.IsNullOrEmpty(eventRegistration.ActivityDayEventRegistrationGuid)&& !string.IsNullOrEmpty(eventRegistration.Guid)) {
                        var activityDayEventRegistrationEntity = _crmService.Retrieve("new_event_registration_activity_day", Guid.Parse(eventRegistration.ActivityDayEventRegistrationGuid), new string[] { "new_activity_day" });
                        activityDayGuid = activityDayEventRegistrationEntity.GetAttributeValue<EntityReference>("new_activity_day").Id;
                        if (entity.Id.Equals(activityDayGuid)) {
                            var eventRegistrationEntity = _crmService.Retrieve("new_event_registration", Guid.Parse(eventRegistration.Guid), new string[] { "new_number_of_participants" });
                            sumOfParticipants-= eventRegistrationEntity.GetAttributeValue<int>("new_number_of_participants");
                        }
                    }
                    if (sumOfParticipants > maxVisitors)
                    {
                        throw new Exception( "NO_ROOM");
                    }
                    activityDayGuid = entity.Id;
                }
                else
                {
                    //create activity day
                     activityDayGuid = _newReservationRepository.CreateActivityDay(eventRegistration, GetTourByCode(eventRegistration.TourType.TourCode), false);
                }
                Log.Information($"GetActivityDayIdCreateActivityDayIfNotExists end, res:{activityDayGuid}");
                return activityDayGuid;
            }
            catch (Exception ex)
            {
                Log.Error($"GetActivityDayIdCreateActivityDayIfNotExists error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public List<TourType> GetAllAvailableTours(string strDate)
        {
            try
            {

                Log.Information($"GetAllAvailableTours start, date:{strDate}");
                var date = DateTime.ParseExact(strDate, "dd/MM/yyyy", null);
                var dayOfWeek = date.DayOfWeek.ToString().ToLower();
                strDate = date.ToString("yyyy-MM-dd");//crm format date
                var fetchXml = _newReservationRepository.StartFetchXml("new_tour_type", "TourType", false, true);
                fetchXml += @$"<attribute name=""new_tour_typeid"" />
                <attribute name=""new_is_need_event"" />
                <attribute name=""new_price_desc_kiosk_en"" />
                <attribute name=""new_price_desc_kiosk_he"" />
                <attribute name=""new_name_kiosk_en"" />
                <attribute name=""new_name_kiosk_he"" />
                <attribute name=""new_tour_code"" />
                <attribute name=""new_category_individuals"" />
                <attribute name=""new_category_museum"" />
                <attribute name=""new_event_reservations"" />
                <order attribute=""new_kiosk_sort_weight""/>
                <link-entity name=""new_spcific_date_and_hour"" from=""new_tour_type"" to=""new_tour_typeid"" alias=""SpecificDate"" link-type=""outer"">
                    <filter type=""and"">
                        <condition attribute=""new_start_time"" operator=""on"" value='{strDate}' />
                    </filter>
                    <attribute name=""new_tour_type"" />
                </link-entity>
                <filter type=""and"">
                  <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                  <condition attribute=""new_show_on_kiosk"" operator=""eq"" value=""1"" />
                  <filter type=""or"">
                    <condition entityname='SpecificDate' attribute='new_tour_type' operator='not-null' />
                    <condition attribute=""new_relevant_day_{dayOfWeek}"" operator=""eq"" value=""1"" />
                  </filter>
                </filter>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _newReservationRepository.GetTourTypeList(fetchXml);
                var yadVashemGuid = _config.GetValue<string>("YadVashemLocationGuid")!; 
                res.ForEach(tourType => tourType.ProductList = GetProductsByTourType(tourType.TourCode, strDate,yadVashemGuid));
                Log.Information($"GetAllAvailableTours end, res count:{res.Count}");
                return res;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllAvailableTours error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public List<Product> GetProductsByTourType( string tourCode, string date ,string location)
        {
            try
            {
                Log.Information($"GetProductsByTourType start, date:{date}, tourCode:{tourCode}");
                var fetchXml = _newReservationRepository.StartFetchXml("new_yv_product", "Product", false, true);
                fetchXml += $@"<attribute name=""new_yv_productid"" />
                        <attribute name=""new_name"" />
                        <attribute name=""new_name_en"" />
                        <attribute name=""new_sku_product"" />
                        <order attribute=""new_sort_weight"" descending=""false"" />
                         <filter type=""and"">
                          <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                          <condition attribute=""new_yv_productid"" operator=""ne"" value=""{_config.GetValue<string>("RegistrationAndHandlingFees")}"" />
                         </filter>
                        <link-entity name=""new_almog_interface"" from=""new_almog_interfaceid"" to=""new_almog_interface"" alias=""Almog"" link-type=""outer"">
                          <attribute name=""new_almog_sub_code"" />
                          <filter type=""and"">
                            <condition attribute=""new_almog_sub_code"" operator=""not-null"" />
                            <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                          </filter>
                        </link-entity>
                        <link-entity name=""new_yv_pricelist"" from=""new_yv_pricelist"" to=""new_yv_productid"" alias=""PriceList"">
                          <attribute name=""new_pricelist_type"" />
                          <attribute name=""new_activity_day_location"" />
                          <attribute name=""new_price_end_date"" />
                          <order attribute=""new_activity_day_location"" descending=""true"" />
                          <order attribute=""new_pricelist_type"" descending=""true"" />
                          <order attribute=""new_price_end_date"" descending=""true"" />
                          <filter type=""and"">
                           <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                           <filter type=""or"">
                              <condition attribute=""new_pricelist_type"" operator=""null"" />
                              <condition attribute=""new_pricelist_type"" operator=""eq"" value=""7"" />
                            </filter>
                            <filter type=""or"">
                              <condition attribute=""new_activity_day_location"" operator=""eq""  value=""{location}"" />
                              <condition attribute=""new_activity_day_location"" operator=""null"" />
                            </filter>
                            <filter type=""or"">
                              <condition attribute=""new_price_end_date"" operator=""on-or-after"" value=""{date}"" />
                              <condition attribute=""new_price_end_date"" operator=""null"" />
                            </filter>
                          </filter>
                          <link-entity name=""new_yv_pricelist_item"" from=""new_yv_pricelist_item"" to=""new_yv_pricelistid"" alias=""PriceListItem"">
                            <attribute name=""new_price"" />
                            <filter type=""and"">
                             <filter type=""or"">
                               <condition attribute=""new_min_quantity_product_units"" operator=""eq"" value=""1"" />
                               <condition attribute=""new_min_quantity_product_units"" operator=""null"" />
                             </filter>
                              <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                            </filter>
                          </link-entity>
                        </link-entity>
                         <link-entity name=""new_new_tour_type_new_yv_product_required"" to=""new_yv_productid"" from=""new_yv_productid"" link-type=""inner"">
                            <link-entity name=""new_tour_type"" to=""new_tour_typeid"" from=""new_tour_typeid"" link-type=""inner"">
                            <filter type=""and"">
                                <condition attribute=""new_tour_code"" operator=""eq"" value=""{tourCode}"" />
                            </filter>
                              <link-entity name=""new_budget_framework"" from=""new_budget_frameworkid"" to=""new_budget_framework"" link-type=""outer"">
                                 <link-entity name=""new_almog_interface"" from=""new_almog_interfaceid"" to=""new_almog_interface"" alias=""TourTypeAlmog"" link-type=""outer"">
                                 <attribute name=""new_almog_sub_code"" />
                                 <filter type=""and"">
                                     <condition attribute=""new_almog_sub_code"" operator=""not-null"" />
                                     <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                 </filter>
                                 </link-entity>
                             </link-entity>
                            </link-entity>
                        </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var res = _newReservationRepository.GetProductList(fetchXml);
                return res;
            }
            catch (Exception ex ) {
                Log.Error($"GetProductsByTourType error: {ex.Message}");
                throw new Exception(ex.Message);

            }
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
        public List<Entity> GetTourClosedTimes(string tourTypeId,DateTime date) {
            try
            {
                var strDate = date.ToString("yyyy-MM-dd");
                var fetchXml = @$"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                              <entity name=""new_closed_days_and_activities"">
                                <attribute name=""new_to_closing_date"" />
                                <attribute name=""new_closing_type"" />
                                <attribute name=""new_from_closing_date"" />
                                <attribute name=""new_is_closed_full_day"" />
                                <filter type=""and"">
                                  <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                  <condition attribute=""new_closing_type"" operator=""in"">
                                    <value>1</value>
                                    <value>3</value>
                                    <value>2</value>
                                    <value>4</value>
                                  </condition>
                                  <condition attribute=""new_from_closing_date"" operator=""on-or-before"" value=""{strDate}"" />
                                  <condition attribute=""new_to_closing_date"" operator=""on-or-after"" value=""{strDate}"" />
                                </filter>
                                <link-entity name=""new_new_closed_days_and_activities_new_tour"" from=""new_closed_days_and_activitiesid"" to=""new_closed_days_and_activitiesid"" alias=""ClosedDays"" link-type='outer' >
                                  <attribute name=""new_tour_typeid"" />                                  
                                   <filter type=""and"">
                                      <condition attribute=""new_tour_typeid"" operator=""eq"" value=""{tourTypeId}"" />
                                    </filter>
                                </link-entity>
                                <filter type=""or"">
                                   <filter type=""and"">
                                    <condition attribute=""new_closing_type"" operator=""eq"" value=""4"" />
                                    <condition entityname='ClosedDays' attribute='new_tour_typeid' operator='not-null' />
                                   </filter>
                                   <filter type=""and"">
                                    <condition attribute=""new_closing_type"" operator=""ne"" value=""4"" />
                                   </filter>
                                </filter>
                              </entity>
                            </fetch>";
                var query = new FetchExpression(fetchXml);
               return  _crmService.RetrieveMultiple(query).Entities.ToList();
            }
            catch (Exception ex) {
                Log.Error($"GetTourClosedTimes error: {ex.Message}");
                throw;

            }

        }
        public List<StartTimeItem> GetAvailableTourTimesForEventReservationTour(string strDate, TourType tourType, string? eventRegistrationGuid, string? activityDayeventRegistrationGuid)
        {
            try
            {
                var dateTime = DateTime.ParseExact(strDate, "dd/MM/yyyy", null);
                var tourClosedTimes = GetTourClosedTimes(tourType.Id,dateTime);
                var fullDayClosedItem = tourClosedTimes.Find(entity=>entity.GetAttributeValue<bool>("new_is_closed_full_day"));
                var startTimeList = new List<StartTimeItem>(); 
                if (fullDayClosedItem != null) {
                    return startTimeList;
                }
                startTimeList = GetSpecialEventTimeListByTourType(tourType.Id, dateTime);
                startTimeList.AddRange(GetTimeListByTourType(tourType.Id));//Check if can be both options...
             //   startTimeList = startTimeList.FindAll(item=>DateTime.ParseExact(item.StartTime,"HH:mm",null)>=DateTime.Now).OrderBy(item=>item.StartTime).ToList();

                if (startTimeList.Count > 0)
                {
                    var eventReservationEntity = _crmService.Retrieve("new_event_reservations", new Guid(tourType.EventReservationId), new[] { "new_max_participants_per_event", "new_total_participants" });
                    if (!tourType.IsForIndividuals)
                    {
                        var availablePlaces = eventReservationEntity.GetAttributeValue<int>("new_max_participants_per_event") - eventReservationEntity.GetAttributeValue<int>("new_total_participants");
                        startTimeList.ForEach(item => { item.AvailablePlaces = availablePlaces; });
                    }
                    else
                    {
                        var maxParticipantsPerEvent = eventReservationEntity.GetAttributeValue<int>("new_max_participants_per_event");
                        foreach (var startTimeItem in startTimeList) {
                            startTimeItem.AvailablePlaces = maxParticipantsPerEvent;
                            var date = DateTime.ParseExact(strDate+startTimeItem.StartTime, "dd/MM/yyyyHH:mm",null);
                            if (tourClosedTimes.Find(closeTime => closeTime.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime() <= date && date <= closeTime.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime()) != null)
                            {
                                startTimeItem.isClosed = true;
                            }
                        }
                        
                        var activityDayList = GetActivityDayListByEventReservation(tourType.EventReservationId,dateTime);
                        var activityDayGuid = Guid.Empty;
                        var currentNumOfParticipants = 0;
                        if (!string.IsNullOrEmpty(eventRegistrationGuid)&&!string.IsNullOrEmpty(activityDayeventRegistrationGuid)) {
                            var eventRegistrationEntity = _crmService.Retrieve("new_event_registration",Guid.Parse(eventRegistrationGuid),new string[] {"new_number_of_participants" });
                            currentNumOfParticipants = eventRegistrationEntity.GetAttributeValue<int>("new_number_of_participants");
                            var activityDayEventRegistrationEntity = _crmService.Retrieve("new_event_registration_activity_day",Guid.Parse(activityDayeventRegistrationGuid), new string[] { "new_activity_day" });
                            activityDayGuid = activityDayEventRegistrationEntity.GetAttributeValue<EntityReference>("new_activity_day").Id;
                        }
                        foreach (var activityDay in activityDayList.Entities)
                        {
                            var startTimeItem = startTimeList.Find(st => st.StartTime == activityDay.GetAttributeValue<DateTime>("new_start_datetime").ToLocalTime().ToString("HH:mm"));
                            if (startTimeItem != null)
                            {
                                startTimeItem.AvailablePlaces -= activityDay.GetAttributeValue<int>("new_number_of_participants");

                                if (activityDayGuid.Equals(activityDay.Id))
                                {
                                    startTimeItem.AvailablePlaces += currentNumOfParticipants;
                                }
                            }
                        }
                    }
                }
                return startTimeList;

            }
            catch (Exception ex)
            {
                Log.Error($"GetAvailableTourTimesForEventReservationTour error: {ex.Message}");
                throw;
            }
        }
        public EntityCollection GetActivityDayListByEventReservation(string eventReservatoinId,DateTime date ,string operatorForDateTime="on") {
            try
            {
                var dateTimeStr = "";
                if (operatorForDateTime == "on")
                {
                     dateTimeStr = date.ToString("yyyy-MM-dd");
                }
                else { //operator is eq
                    dateTimeStr = date.ToString("yyyy-mm-ddThh:mi:ss.mmmZ");
                }
                var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""false"">
              <entity name=""new_activity_day"">
                <attribute name=""new_start_datetime"" />
                 <attribute name=""new_number_of_participants"" />
                 <attribute name=""new_actual_number_of_participants"" />
                 <attribute name=""new_activity_dayid"" />
                <filter type=""and"">
                  <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                  <condition attribute=""new_event_reservations"" operator=""eq"" value=""{eventReservatoinId}""/>
                  <condition attribute=""new_start_datetime"" operator=""on"" value=""{dateTimeStr}"" />
                </filter>
                <link-entity name=""new_event_reservations"" from=""new_event_reservationsid"" to=""new_event_reservations"" alias=""event_reservations"">
                     <attribute name=""new_max_participants_per_event"" />
                </link-entity>
              </entity>
            </fetch>";
                var query = new FetchExpression(fetchXml);
                return _crmService.RetrieveMultiple(query);
            }
            catch (Exception ex)
            {
                Log.Error($"GetActivityDayListByEventReservation error: {ex.Message}");
                throw;
            }
        }
        public List<StartTimeItem> GetTimeListByTourType(string tourTypeId)
        {
            try { 
                var timeList = new List<StartTimeItem>();
                var fetchXml = @$"<fetch distinct=""false"" mapping=""logical"">
                               <entity name=""new_yv_time"">
                                  <attribute name=""new_name"" />
                                  <order attribute=""new_name"" descending=""false"" />
                                  <filter type=""and"">
                                     <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                  </filter>
                                  <link-entity name=""new_new_tour_type_new_yv_time"" to=""new_yv_timeid"" from=""new_yv_timeid"" link-type=""inner"">
                                    <filter type=""and"">
                                        <condition attribute=""new_tour_typeid"" operator=""eq"" value=""{tourTypeId}"" />
                                  </filter>
                                  </link-entity>
                               </entity>
                            </fetch>";

                var query = new FetchExpression(fetchXml);
                var timeEntitiesList = _crmService.RetrieveMultiple(query);
                foreach (var entity in timeEntitiesList.Entities)
                {
                    var time = entity.GetAttributeValue<string>("new_name");
                    timeList.Add(new StartTimeItem() {StartTime=time.PadLeft(5, '0')});
                }
                return timeList;
            }
            catch (Exception ex)
            {
                Log.Error($"GetTimeListByTourType error: {ex.Message}");
                throw;
            }
        }
        public List<StartTimeItem> GetSpecialEventTimeListByTourType(string tourTypeId,DateTime date)
        {
            try
            {
                string dateTimeStr = date.ToString("yyyy-MM-dd");
                var specialTimeList = new List<StartTimeItem>();
                var fetchXml = @$"<fetch version='1.0' output-format='xml-platform' mapping='logical' >
                          <entity name=""new_spcific_date_and_hour"" >
                            <attribute name=""new_start_time"" />
                            <order attribute=""new_start_time"" descending=""false"" />
                            <filter type=""and"" >
                              <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                              <condition attribute=""new_start_time"" operator=""on"" value=""{dateTimeStr}"" />
                              <condition attribute=""new_tour_type"" operator=""eq"" value='{tourTypeId}' />
                            </filter>
                          </entity>
                        </fetch>";
                var query = new FetchExpression(fetchXml);
                var specificDateTimeEntitiesList = _crmService.RetrieveMultiple(query);
                foreach (var entity in specificDateTimeEntitiesList.Entities)
                {
                    var time = entity.GetAttributeValue<DateTime>("new_start_time").ToLocalTime().ToString("HH:mm");
                    specialTimeList.Add(new StartTimeItem { StartTime = time});

                }
                return specialTimeList;
            }
            catch (Exception ex)
            {
                Log.Error($"GetSpecialEventTimeListByTourType error: {ex.Message}");
                throw;
            }
        }

        public Tuple<int, List<StartTimeItem>> GetMuseumOccupancyPerSlotTime(string strDate, string? currenScheduleEventGuid)
        {
            return GetResourcePerSlotTime(strDate, currenScheduleEventGuid,"104", _config.GetValue<Guid>("MuseumResourceId"));



        }
        private DateTime GetNextSlotTime()
        {
            var dt = DateTime.Now;
            var ts = TimeSpan.FromMinutes(TIME_SLOT_MINTUE);
            var modTicks = dt.Ticks % ts.Ticks;
            var delta = modTicks != 0 ? ts.Ticks - modTicks : 0;
            return new DateTime(dt.Ticks + delta, dt.Kind);
        }
        private DateTime GetResourceOpenOrCloseTime(Entity resourceEntity, DateTime dateTime, bool isForClose)
        {
            var times = GetTimesForWeek(resourceEntity, isForClose);
            dateTime = dateTime.Add(times[(int)dateTime.DayOfWeek]);
            return dateTime;
        }

        public TimeSpan[] GetTimesForWeek(Entity entity, bool isForClose)
        {
            TimeSpan[] timeArr = new TimeSpan[6]; // ערך ברירת מחדל הוא 00:00
            string type = isForClose ? "close" : "open";
            string[] days = { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday" };

            for (int i = 0; i < days.Length; i++)
            {
                string value = entity.GetAttributeValue<string>($"new_{days[i]}_{type}");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    timeArr[i] = DateTime.ParseExact(value, "HH:mm", null).TimeOfDay;
                }
                // אחרת, יישאר 00:00
            }

            return timeArr;
        }


        public List<Language> GetAudioguideAvailableLanguages(string lang)
        {
            try
            {
                Log.Information($"GetAudioguideAvailableLanguages, lang:{lang}");
                var fetchXml = _newReservationRepository.StartFetchXml("new_language", "Language");
                fetchXml += $@"<attribute name=""new_en_name"" />
                               <attribute name=""new_name"" />
                               <attribute name=""new_language_code"" />
                               <order attribute='{(lang == "he" ? "new_name" : "new_en_name")}' />
                               <link-entity name=""new_new_yv_product_new_language"" from=""new_languageid"" to=""new_languageid"" intersect=""true"">
                                <link-entity name=""new_yv_product"" from=""new_yv_productid"" to=""new_yv_productid"">
                                    <filter type=""and"">
                                        <condition attribute=""new_yv_productid"" operator=""eq"" value='{_config.GetValue<Guid>("AudioGuideProductId")}' />
                                        <condition attribute=""statecode"" operator=""eq"" value=""0"" />
                                    </filter>
                                </link-entity>
                              </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var result = _crmService.RetrieveMultiple(query);
                var langList = new List<Language>();
                var firstItem = new Language();
                langList.Add(firstItem);
                foreach (var langEntity in result.Entities)
                {
                    var langItem = new Language();
                    langItem.Code = langEntity.GetAttributeValue<string>("new_language_code");
                    if (lang == "he")
                    {
                        langItem.Name = langEntity.GetAttributeValue<string>("new_name");
                    }
                    else
                    {
                        langItem.Name = langEntity.GetAttributeValue<string>("new_en_name");
                    }
                    if (lang == langItem.Code || lang == "he" && langItem.Code == "iw")
                    {
                        firstItem.Code = langItem.Code;
                        firstItem.Name = langItem.Name;
                        continue;
                    }
                    langList.Add(langItem);
                }
                Log.Information($"GetAudioguideAvailableLanguages end");
                return langList;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAudioguideAvailableLanguages error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public Reservation GetReservation(string reservationNumber, string lang)
        {
            try
            {
                Log.Information($"GetReservation start, reservation number: {reservationNumber}");
                Reservation reservation = new Reservation();
                var fetchXml = _newReservationRepository.StartFetchXml("new_reservation");
                fetchXml +=
                $@"<attribute name='new_num'/>
                <attribute name='new_firstname_contact'/>
                <attribute name='new_lastname_contact'/>
                <attribute name='new_mobile_contact'/>
                <attribute name='new_email_contact'/>
                <attribute name='new_form_language'/>
                <filter type='and'>
	                <condition attribute='statecode' operator='eq' value='0'/>
	                <condition attribute='new_num' operator='eq' value='" + reservationNumber + @"'/>
                    <condition attribute='new_reservation_status' operator='not-in'>
                        <value>6</value>
                        <value>3</value>
                    </condition>
                </filter>

                <link-entity name='new_activity_day' from='new_reservation' to='new_reservationid' alias='activityDay'>
                    <attribute name='new_number_of_participants' />
                    <attribute name='new_activity_dayid' />
	                <filter type='and'>
		                <condition attribute='new_start_datetime' operator='today'/>
	                    <condition attribute='statecode' operator='eq' value='0'/>
                        <condition attribute='new_activity_day_status' operator='not-in'>
                            <value>6</value>
                            <value>3</value>
                        </condition>
	                </filter>
                    <link-entity name=""new_bid"" from=""new_activity_day"" to=""new_activity_dayid"" alias=""BidItem""  link-type='outer'>
                        <attribute name='new_amount' />
                        <attribute name='new_language'/>
                            <filter type=""and"">
                            <condition attribute=""new_yv_product"" operator=""eq"" value='{" + _config.GetValue<Guid>("AudioGuideProductId") + @"}'/>
                        </filter>
                        <link-entity name=""new_language"" from=""new_languageid"" to=""new_language"" alias=""Language""  link-type='outer'>
                            <attribute name='new_language_code' />
                        </link-entity>
                    </link-entity>
	                <link-entity name='new_itinerary_group' from='new_activity_day' to='new_activity_dayid' alias='itineraryGroup'>
                        <filter type='and'>
	                        <condition attribute='statecode' operator='eq' value='0'/>
	                    </filter>
		                <link-entity name='new_new_itinerary_item_new_itinerary_group' from='new_itinerary_groupid' to='new_itinerary_groupid'>
			                <link-entity name='new_itinerary_item' from='new_itinerary_itemid' to='new_itinerary_itemid' alias='itineraryItem'>
                                   <filter type='and'>
	                                   <condition attribute='statecode' operator='eq' value='0'/>
	                               </filter>
				                <link-entity name='new_event_item' from='new_event_itemid' to='new_scheduler_resource' alias='ab'>
					                <filter type='and'>
						                <condition attribute='new_resource' operator='eq' uitype='new_resource' value='{" + _config.GetValue<Guid>("MuseumResourceId") + @"}'/>
	                                   <condition attribute='statecode' operator='eq' value='0'/>
					                </filter>
				                </link-entity>
			                </link-entity>
		                </link-entity>
	                </link-entity>
                </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var result = _crmService.RetrieveMultiple(query);
                //query = new FetchExpression("");
                //result = _crmService.RetrieveMultiple(query);
                if (result.Entities.Count() > 0)
                {
                    reservation = _newReservationRepository.GetReservation(result.Entities[0]);
                    reservation.GroupList = GetGroups(result.Entities[0].GetAttributeValue<AliasedValue>("activityDay.new_activity_dayid").Value.ToString()!, lang);
                }
                else
                {
                    throw new Exception("NOT_FOUND");
                }
                Log.Information($"GetResrvation end");
                return reservation;
            }
            catch (Exception ex)
            {
                Log.Error($"GetReservation error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
       
        public List<Group> GetGroups(string activityDayGuid, string lang)
        {
            try
            {
                Log.Information($"GetGroups start, activityDayGuid: {activityDayGuid}");

                List<Group> groups = new List<Group>();
                var fetchXml = _newReservationRepository.StartFetchXml("new_itinerary_group");
                fetchXml +=
                @"<attribute name='new_number_of_participants' />
                <attribute name='new_name' />
                <attribute name='new_is_group_arrived' />
                    <filter type='and'>
	                    <condition attribute='statecode' operator='eq' value='0'/>
	                </filter>
                <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ActivityDay'>
                     <attribute name='new_did_group_arrive' />
                    <filter type='and'>
                        <condition attribute='new_activity_dayid' operator='eq' uitype='new_activity_day' value='{" + activityDayGuid + @"}' />
                    </filter>
                    <link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" alias=""ActivityType"">
                        <attribute name='new_name' />
                        <attribute name='new_name_en' />
                        <attribute name='new_tour_code' />
                    </link-entity>
		        </link-entity>
                <link-entity name='new_new_itinerary_item_new_itinerary_group' from='new_itinerary_groupid' to='new_itinerary_groupid'>
			        <link-entity name='new_itinerary_item' from='new_itinerary_itemid' to='new_itinerary_itemid' alias='itineraryItem'>
			            <attribute name='new_is_need_audio_guide'/>
			            <attribute name='new_is_need_headset'/>
                        <attribute name='new_start_time' />
                            <filter type='and'>
	                            <condition attribute='statecode' operator='eq' value='0'/>
	                        </filter>
                        <link-entity name='new_event_item' from='new_event_itemid' to='new_scheduler_resource' alias='ab'>
			                <filter type='and'>
	                            <condition attribute='new_resource' operator='eq' uitype='new_resource' value='{" + _config.GetValue<Guid>("MuseumResourceId") + @"}'/>
	                            <condition attribute='statecode' operator='eq' value='0'/>
			                </filter>
			            </link-entity>
			        </link-entity>
			    </link-entity>";
                fetchXml += _newReservationRepository.EndFetchXml();
                var query = new FetchExpression(fetchXml);
                var result = _crmService.RetrieveMultiple(query);
                if (result.Entities.Count() > 0)
                {
                    groups = _newReservationRepository.GetGroups(result.Entities, lang);
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
        public bool UpdateGroups(List<Group> groupList)
        {
            try
            {
                Log.Information($"UpdateGroups start, groupList: {@groupList}");
                _newReservationRepository.UpdateGroups(groupList);
                Log.Information($"UpdateGroups end");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateGroups error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public Reservation UpdateRsvStatus(Reservation reservation)
        {
            try
            {
                Log.Information($"UpdateRsvStatus start, reservation: {@reservation}");
                return  _newReservationRepository.UpdateRsvStatus(reservation);
             
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateRsvStatus error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        public void UpdateReservationAfterPayment(BasicReservationItem reservation, Guid paymentGuid)
        {
            try
            {
                Log.Information($"UpdateReservationAfterPayment in repository start, reservationid:{reservation.Guid} ,{((reservation is EventRegistration)? "EventRegistration" : "Reservation")}");
                var paymentEntity = _crmService.Retrieve("new_payment", paymentGuid, new[] { "new_num" });
                var paymentNumber = paymentEntity.GetAttributeValue<string>("new_num");
                _newReservationRepository.UpdateReservationAfterPayment(reservation, paymentNumber);

                Log.Information("UpdateReservationAfterPayment in repository end");
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateReservationAfterPayment error: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }

        public Tuple<int, List<StartTimeItem>> GetResourcePerSlotTime(string strDate, string? currenScheduleEventGuid, string tourCode,Guid resourceGuid)
        {
            try
            {
                Log.Information($"GetMuseumOccupancyPerSlotTime start, date:{strDate},currenScheduleEventGuid:{currenScheduleEventGuid}");
                var startTimeList = new List<StartTimeItem>();
                var dateTime = DateTime.ParseExact(strDate, "dd/MM/yyyy", null);
                //var resourceGuid = _config.GetValue<Guid>("MuseumResourceId");
                var resourceClosedTimes = GetResourceClosedTimes(dateTime);
                var museumResourceEntity = GetFullResource(resourceGuid);
                var openDateTime = GetResourceOpenOrCloseTime(museumResourceEntity, dateTime, false);
                var closeDateTime = GetResourceOpenOrCloseTime(museumResourceEntity, dateTime, true);
                var tourType = GetTourByCode(tourCode, true);
                decimal duration = tourType.GetAttributeValue<decimal>("new_duration_of_hours_resource");
                if (duration == 0)
                    duration = tourType.GetAttributeValue<decimal>("new_duration_of_hours");
                closeDateTime = closeDateTime.AddHours(-decimal.ToDouble(duration));
                int maxVisitorsPerSlotTime = museumResourceEntity.GetAttributeValue<int>("new_capacity");
                var eventList = GetResourceScheduleEventsByDate(dateTime, resourceGuid, currenScheduleEventGuid);
                var eventListGroupByDate = eventList.Entities.GroupBy(e => e.GetAttributeValue<DateTime>("new_start_datetime")).Select(x => new { Start = x.Key.ToLocalTime(), SumOfVisitors = x.Sum(a => a.GetAttributeValue<int>("new_number_of_participants")) }).ToList();
                int availablePlacesForFullDay = 0;
                if (openDateTime < DateTime.Now)
                {
                    openDateTime = GetNextSlotTime();
                }
                for (var date = openDateTime; date <= closeDateTime; date = date.AddMinutes(TIME_SLOT_MINTUE))
                {
                    var stringTime = date.ToString("HH:mm");
                    var startTimeItem = new StartTimeItem { StartTime = stringTime, AvailablePlaces = maxVisitorsPerSlotTime, isClosed = false };
                    foreach (var closedTime in resourceClosedTimes.Entities)
                    {
                        if (closedTime.GetAttributeValue<DateTime>("new_from_closing_date").ToLocalTime() <= date && closedTime.GetAttributeValue<DateTime>("new_to_closing_date").ToLocalTime() > date)
                        {
                            startTimeItem.isClosed = true;
                            break;
                        }
                    }
                    if (!startTimeItem.isClosed)
                    {
                        var groupByDateItem = eventListGroupByDate.FirstOrDefault(item => item.Start == date);
                        if (groupByDateItem != null)
                        {
                            startTimeItem.AvailablePlaces = maxVisitorsPerSlotTime - groupByDateItem.SumOfVisitors;
                        }
                        availablePlacesForFullDay += startTimeItem.AvailablePlaces;
                    }
                    startTimeList.Add(startTimeItem);
                }
                Log.Information($"GetMuseumOccupancyPerSlotTime end,currenScheduleEventGuid:{currenScheduleEventGuid}");
                return new Tuple<int, List<StartTimeItem>>(availablePlacesForFullDay, startTimeList);
            }
            catch (Exception ex)
            {
                Log.Error($"GetFullDayMuseumOccupancyByDate error: {ex.Message}");
                throw new Exception(ex.Message);
            }
            throw new NotImplementedException();
        }
    }
}
