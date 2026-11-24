using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.Xrm.Sdk;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Filter = Data.Modals.Filter;
using Institution = Data.Modals.Institution;

namespace Services.Services
{
    public class ScheduleEventService : BasicService<EventItem, EventItem, EventItemCRM>, IScheduleEventService
    {
        private IHelperService helperService;
        private IEventItemRepository eventItemRepository;
        private IReservationsRepository reservationsRepository;
        private IEventReservationRepository eventReservationRepository;
        private IActivityDayRepository activityDayRepository;
        private IServiceProviderRepository serviceProviderRepository;
        private IFullEventItemRepository fullEventItemRepository;
        private IQueryHelperRepository queryHelperRepository;
        private IScheduleEventsHelperService scheduleEventsHelperService;
        private IResourceRepository resourceRepository;
        private IReportsService reportsService;
        private IReservationsService reservationsService;

        protected readonly ICRMConnection crmConnection;


        public ScheduleEventService(IHelperService helperService,
            IEventItemRepository eventItemRepository,
            IReservationsRepository reservationsRepository,
            IActivityDayRepository activityDayRepository,
            IEventReservationRepository eventReservationRepository,
            IServiceProviderRepository serviceProviderRepository,
            IFullEventItemRepository fullEventItemRepository,
            IQueryHelperRepository queryHelperRepository,
            IMapFunctionRepository mapFunctionRepository,
            IScheduleEventsHelperService scheduleEventsHelperService,
            IResourceRepository resourceRepository,
            IReportsService reportsService,
            IReservationsService reservationsService,
            ICRMConnection crmConnection) : base(helperService, eventItemRepository, mapFunctionRepository)
        {
            this.helperService = helperService;
            this.eventItemRepository = eventItemRepository;
            this.reservationsRepository = reservationsRepository;
            this.eventReservationRepository = eventReservationRepository;
            this.activityDayRepository = activityDayRepository;
            this.serviceProviderRepository = serviceProviderRepository;
            this.fullEventItemRepository = fullEventItemRepository;
            this.queryHelperRepository = queryHelperRepository;
            this.scheduleEventsHelperService = scheduleEventsHelperService;
            this.resourceRepository = resourceRepository;
            this.reportsService = reportsService;
            this.crmConnection = crmConnection;
            this.reservationsService = reservationsService;
        }

        //public override void Update(EventItem item, EventItem oldItem, string userId)
        //{

        //    eventItemRepository.Update(item, oldItem, userId);
        //}

        public override void Delete(EventItem item, string userId)
        {
            item.IsDeleted = true;
            eventItemRepository.Delete(item, userId);
        }

        public void UpdateStatus(EventItem item, bool isApprove, string userId)
        {
            eventItemRepository.UpdateStatus(item, isApprove, userId);
        }
        public override Tuple<int, List<EventItem>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            List<Sort> eventItemSort = new List<Sort>();
            List<Sort> additionalSort = new List<Sort>();
            List<Sort> reservationSort = new List<Sort>();
            List<Sort> eventReservationSort = new List<Sort>();
            List<Sort> serviceProviderSort = new List<Sort>();
            List<Sort> activityDaySort = new List<Sort>();
            List<Sort> resourceySort = new List<Sort>();

            string fetchXml = "";

            if (sort != null && !isAggregate)
            {
                sort.ForEach(x =>
                {
                    if (x.Active.Contains(":"))
                    {
                        x.Alias = x.Active.Split(':')[1];
                        x.Active = x.Active.Split(':')[0];
                    }
                });

                eventItemSort = helperService.BuildSorting<EventItem, EventItemCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "EventItem"));
                reservationSort.AddRange(helperService.BuildSorting<Reservation, ReservationCRM>(sort.FindAll(x => x.Alias == "Reservation")));
                eventReservationSort.AddRange(helperService.BuildSorting<EventReservationItem, EventReservationCRM>(sort.FindAll(x => x.Alias == "EventReservation")));
                serviceProviderSort.AddRange(helperService.BuildSorting<ServiceProvider, ServiceProviderCRM>(sort.FindAll(x => x.Alias == "ServiceProvider")));
                activityDaySort.AddRange(helperService.BuildSorting<ActivityDay, ActivityDayCRM>(sort.FindAll(x => x.Alias == "ActivityDay")));
                resourceySort.AddRange(helperService.BuildSorting<ActivityDay, ActivityDayCRM>(sort.FindAll(x => x.Alias == "Resource")));
            }

            List<Filter> eventItemFilters = new List<Filter>();
            List<Filter> additionalFilters = new List<Filter>();
            Filter eventTypeFilter;
            if (filter != null)
            {

                eventItemFilters = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "EventItem"));
                additionalFilters.AddRange(helperService.BuildFilter<EventReservationItem, EventReservationCRM, EventReservationCRM>(filter.FindAll(x => x.Alias == "EventReservation" && !(x.Field == "eventType" && x.Value == "2"))));//if event type is studyDay return reservations only
                additionalFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias != null && x.Alias.Split(':')[0] == "Reservation")));
                additionalFilters.AddRange(helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay")));
                additionalFilters.AddRange(helperService.BuildFilter<Resource, ResourceCRM, ResourceCRM>(filter.FindAll(x => x.Alias == "Resource")));
                additionalFilters.AddRange(helperService.BuildFilter<ServiceProvider, ServiceProviderCRM, ServiceProviderCRM>(filter.FindAll(x => x.Alias == "ServiceProvider")));
                eventTypeFilter = filter.Find(x => x.Alias == "EventReservation" && x.Field == "eventType" && x.Value == "2");
                if (eventTypeFilter != null)
                {
                    eventTypeFilter.Field = "reservation";
                    eventTypeFilter.Value = "xxx";
                    eventTypeFilter.Type = "lookup";
                    eventTypeFilter.Operator = "not-null";
                    eventTypeFilter.Alias = "ActivityDay";
                    List<Filter> eventTypeFilterList = new List<Filter> { eventTypeFilter };
                    additionalFilters.AddRange(helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(eventTypeFilterList));
                }
                if (filter != null && filter.Find(x => x.Alias == "Languages") != null)
                {
                    additionalFilters.AddRange(helperService.BuildFilter<ServiceProvider, ServiceProviderCRM, ServiceProviderCRM>(filter.FindAll(x => x.Alias == "Languages")));
                }
            }

            fetchXml += @"<all-attributes/>";

            fetchXml += reservationsRepository.GetLinkedEntity(isAggregate, additionalFilters, reservationSort, "");
            fetchXml += eventReservationRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "");
            fetchXml += activityDayRepository.GetLinkedEntity(isAggregate, additionalFilters, activityDaySort, "");
            fetchXml += serviceProviderRepository.GetLinkedEntity(isAggregate, additionalFilters, serviceProviderSort, "");
            if (filter.FindAll(x => x.Alias == "Resource").Count>0)
                fetchXml += resourceRepository.GetLinkedEntity(isAggregate, additionalFilters, resourceySort, "inner");

            Tuple<int, List<EventItem>> eventItemList = eventItemRepository.GetFetchXml(userId, isAggregate, paging, eventItemSort, eventItemFilters, fetchXml);
            return eventItemList;
        }


        public List<EventItemForExcel> GetPaymentReport(string userId, List<Filter> filterList, List<string> eventItemList)
        {

            string cascadedCondition = string.Empty;
            string condition = string.Empty;
            string startDate = string.Empty;
            string endDate = string.Empty;
            List<Filter> department = new List<Filter>();
            List<Filter> activityDayLocation = new List<Filter>();
            List<Filter> divisionRouter = new List<Filter>();
            string departmentCondition = string.Empty;
            string responsibleAgentCondition = string.Empty;
            string activityDayLocationCondition = string.Empty;
            string divisionRouterCondition = string.Empty;
            if (filterList != null)
            {
                startDate = filterList.Where(x => x.Field == "Start").ToList()[0].Value;
                endDate = filterList.Where(x => x.Field == "End").ToList()[0].Value;
                department = filterList.Where(x => x.Field == "Department").ToList();
                activityDayLocation = filterList.Where(x => x.Field == "ActivityDayLocation").ToList();
                divisionRouter = filterList.Where(x => x.Field == "DivisionRouter").ToList();

                condition = @"<filter type='and'>
		<condition attribute='statecode' operator='eq' value='0' />
		<condition attribute='new_status' operator='eq' value='1' />
		<condition attribute='new_start_datetime' operator='on-or-after' value='" + startDate + @"' />
		<condition attribute='new_end_datetime' operator='on-or-before' value='" + endDate + @"' />
		<condition attribute='new_is_resource' operator='eq' value='0' />
        </filter>";

                cascadedCondition = @"<filter type='and'>
		<condition attribute='statecode' operator='eq' value='0' />
		<condition attribute='new_status' operator='eq' value='5' />
		<condition attribute='new_start_datetime' operator='on-or-after' value='" + startDate + @"' />
		<condition attribute='new_end_datetime' operator='on-or-before' value='" + endDate + @"' />
        <condition attribute='new_is_resource' operator='eq' value='0' />
		</filter>";
                if (department != null && department.Count > 0)
                {
                    departmentCondition = @"<filter type='and'><condition attribute='new_rsv_department' operator= 'in' >";
                    responsibleAgentCondition = @"<filter type='and'><condition attribute='new_responsible_agent' operator= 'in' >";
                    foreach (var item in department)
                    {
                        departmentCondition += "<value>" +  item.Value + @"</value>";
                        responsibleAgentCondition += "<value>" +  item.Value + @"</value>";
                    }
                    departmentCondition += " </condition> </filter> ";
                    responsibleAgentCondition += " </condition> </filter> ";
                }
                if (activityDayLocation != null && activityDayLocation.Count > 0)
                {
                    activityDayLocationCondition = @"<filter type = 'and'> <condition attribute='new_activity_day_location' operator='in'>";
                    foreach (var item in activityDayLocation)
                    {
                        activityDayLocationCondition += "<value>" + item.Value + @"</value>";
                    }
                    activityDayLocationCondition += "</condition> </filter>";
                }
                if (divisionRouter != null && divisionRouter.Count > 0)
                {
                    divisionRouterCondition = @"<filter type = 'and'><condition attribute = 'new_division_router' operator= 'in'>";
                    foreach (var item in divisionRouter)
                    {
                        divisionRouterCondition += "<value>"+ item.Value +@"</value>";
                    }
                    divisionRouterCondition += "</condition> </filter>";
                }
            }
            else if (eventItemList != null && eventItemList.Count > 0)
            {
                condition = @"<filter type='or'>";
                foreach (var eventItem in eventItemList)
                {
                    condition += "<condition attribute='new_event_itemid' operator='eq' value='" + eventItem + @"'/>";
                }
                condition += @"</filter>";
                cascadedCondition = condition;
            }
            else
            {
                throw new Exception();
            }




            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
            <entity name='new_event_item'>
		<attribute name='new_name' />
		<attribute name='createdon' />
		<attribute name='modifiedon' />
		<attribute name='new_end_datetime' />
		<attribute name='new_start_datetime' />
		<attribute name='new_status' />
		<attribute name='new_number_of_participants' />
		<attribute name='new_rsv_department' />
		<attribute name='new_comments' />
		<attribute name='new_reservation_status' />
		<attribute name='new_service_provider' />
		<attribute name='new_resource' />
		<attribute name='new_yv_product' />
		<attribute name='new_activity_day' />
		<attribute name='new_event_itemid' />
		<attribute name='new_is_for_pay' />
		<attribute name='new_is_pay_for_travel' />
        <attribute name='new_is_virtual' />
        <attribute name='new_is_taxi_required' />
        <attribute name='new_without_preparation' />
		<attribute name='new_status' />
		<attribute name='new_event_item_activity_location' />
		<attribute name='new_is_activity_location_different' />
		<attribute name='new_pay_for_travel_type' />
		<attribute name='new_pay_for_taxi_type' />
        <attribute name='new_event_type' />        
        <attribute name='new_comments' />
		<order attribute='new_name' descending='false' />
		" + condition + @"
		<link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider' link-type='outer'>
			<attribute name='new_first_name_he' />
			<attribute name='new_last_name_he' />
			<attribute name='new_id_number' />
			<attribute name='new_city' />
			<attribute name='new_rsv_status_is_yv_employee' />
	       <attribute name='new_rsv_status_is_national_service' />
           <attribute name='new_generation' />  
           <attribute name='new_main_department' />       
           <attribute name='new_start_work_date' />
	       <attribute name='new_employment_type' />
           <attribute name='new_is_for_malam' />
		</link-entity>
        <link-entity name='new_itinerary_group' from='new_guide_schedule_event' to='new_event_itemid' link-type='outer' alias='itineraryGroup'>
	       <attribute name='new_itinerary_groupid' />
        </link-entity>
        <link-entity name='new_itinerary_item' from='new_scheduler_service_provider' to='new_event_itemid' link-type='outer' alias = 'itineraryItem'>
	       <attribute name='new_itinerary_itemid' />
        </link-entity>
            <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' link-type='inner' alias='ActivityDay'>
			<attribute name='new_name' />
			<attribute name='new_activity_day_location' />
			<attribute name='new_activity_day_type' />
			<attribute name='new_activity_day_status' />
			<attribute name='new_number_of_groups' />
			<attribute name='new_start_datetime' />
			<attribute name='new_end_datetime' />
			<attribute name='new_activity_dayid' />"
             + divisionRouterCondition + activityDayLocationCondition;

            string fetchXmlForReservations = @"<link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation' link-type='inner'>
				<attribute name='new_name' />
				<attribute name='new_num' />
				<attribute name='new_number_of_groups' />
				<attribute name='new_activity_location_city' />
				<attribute name='new_activity_location_address' />
				<attribute name='new_is_address_different_from_institution' />
				<attribute name='new_is_reservation_locked' />
                <attribute name='new_responsible_agent' />"
                + responsibleAgentCondition +
                @"<link-entity name='new_rsv_institution' from='new_rsv_institutionid' to='new_rsv_institution' link-type='outer' alias='Institution' >
				<attribute name='new_name' />
				<attribute name='new_sushaime_code' />
				<attribute name='new_address' />
				<attribute name='new_city' />
				</link-entity>
				<link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' link-type='outer' alias='Reservation.BudgetFramework'>
				<attribute name='new_name' />
				<attribute name='new_framework_code' />
				<attribute name='new_malam_code' />
            </link-entity>
			</link-entity>
		    </link-entity>
		    </entity>
		    </fetch>";
            string fetchXmlForEvents = @"<link-entity name='new_event_reservations' from='new_event_reservationsid' to='new_event_reservations' alias='EventReservation' link-type='inner' >
			    <attribute name='new_name' />
			    <attribute name='new_event_code' />
			    <attribute name='new_location' />
			    <attribute name='new_city' />
			    <attribute name='new_division_router' />
			    <attribute name='new_budget_section' />
			    <attribute name='new_malam_code' />
            	<attribute name='new_event_type' />
                <attribute name='new_internal_name' />
			<link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' link-type='inner' alias='EventReservation.BudgetFramework'>
			    <attribute name='new_name' />
			    <attribute name='new_framework_code' />
			    <attribute name='new_malam_code' />
			    <attribute name='new_rsv_department' />"
                   + departmentCondition+
            @"</link-entity>
			</link-entity>
		</link-entity>
		</entity>
		</fetch>";
            string cascadedFetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
            <entity name='new_event_item'>
		<attribute name='new_name' />
		<attribute name='createdon' />
		<attribute name='modifiedon' />
		<attribute name='new_end_datetime' />
		<attribute name='new_start_datetime' />
		<attribute name='new_status' />
		<attribute name='new_number_of_participants' />
		<attribute name='new_rsv_department' />
		<attribute name='new_comments' />
		<attribute name='new_reservation_status' />
		<attribute name='new_service_provider' />
		<attribute name='new_resource' />
		<attribute name='new_yv_product' />
		<attribute name='new_activity_day' />
		<attribute name='new_event_itemid' />
		<attribute name='new_is_for_pay' />
		<attribute name='new_is_pay_for_travel' />
        <attribute name='new_is_virtual' />
		<attribute name='new_is_taxi_required' />
        <attribute name='new_without_preparation' />
        <attribute name='new_comments' />
		<attribute name='new_status' />
		<order attribute='new_name' descending='false' />
		" + cascadedCondition + @"
		<link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider' link-type='outer'>
			<attribute name='new_first_name_he' />
			<attribute name='new_last_name_he' />
			<attribute name='new_id_number' />
			<attribute name='new_city' />
			<attribute name='new_rsv_status_is_yv_employee' />
			<attribute name='new_rsv_status_is_national_service' />
           <attribute name='new_generation' />  
           <attribute name='new_main_department' />       
           <attribute name='new_start_work_date' />
		</link-entity>
		<link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' link-type='inner' alias='ActivityDay'>
			<attribute name='new_name' />
			<attribute name='new_activity_day_location' />
			<attribute name='new_activity_day_type' />
			<attribute name='new_activity_day_status' />
			<attribute name='new_number_of_groups' />
            <attribute name='new_start_datetime' />
			<attribute name='new_end_datetime' />"
            + divisionRouterCondition + activityDayLocationCondition;

           string cascadedFetchXmlForReservations =  @"<link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation' link-type='inner'>
				<attribute name='new_name' />
				<attribute name='new_num' />
				<attribute name='new_number_of_groups' />
				<attribute name='new_activity_location_city' />
				<attribute name='new_activity_location_address' />
				<attribute name='new_is_address_different_from_institution' />
				<attribute name='new_is_reservation_locked' />
                <attribute name='new_responsible_agent' />"
                + responsibleAgentCondition +
                @"<link-entity name='new_rsv_institution' from='new_rsv_institutionid' to='new_rsv_institution' link-type='outer' alias='Institution' >
				<attribute name='new_name' />
				<attribute name='new_sushaime_code' />
				<attribute name='new_address' />
				<attribute name='new_city' />
				</link-entity>
				<link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' link-type='outer' alias='Reservation.BudgetFramework'>
				<attribute name='new_name' />
				<attribute name='new_framework_code' />
				<attribute name='new_malam_code' />
            </link-entity>
		    </link-entity>
            </link-entity>
		    </entity>
		    </fetch>";
            string cascadedFetchXmlForEvents = @"</link-entity>
            <link-entity name='new_event_reservations' from='new_event_reservationsid' to='new_event_reservations' alias='EventReservation' link-type='inner' >
			    <attribute name='new_name' />
			    <attribute name='new_event_code' />
			    <attribute name='new_location' />
			    <attribute name='new_city' />
			    <attribute name='new_division_router' />
			    <attribute name='new_budget_section' />
	            <attribute name='new_event_type' /> 
                <attribute name='new_internal_name' />
			<link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' link-type='inner' alias='EventReservation.BudgetFramework'>
			    <attribute name='new_name' />  
			    <attribute name='new_framework_code' />
			    <attribute name='new_malam_code' />
			    <attribute name='new_rsv_department' />"
                + departmentCondition +
            @"</link-entity>
			</link-entity>
		</entity>
		</fetch>";

            //  string test_res_num = "SCH-0027318";
            // fetchXml = xmlForSpesificReservation(test_res_num).Item1;
            // cascadedFetchXml = xmlForSpesificReservation(test_res_num).Item2;
            DateTime dateTimebefore = DateTime.Now;
            var reservationsList = basicRepository.GetAllListEntity(userId, fetchXml + fetchXmlForReservations);
            
            var finalReservationsList = EventItemForExcelFromEntity(reservationsList);
            var eventsList = basicRepository.GetAllListEntity(userId, fetchXml + fetchXmlForEvents);
            var finalEventsList = EventItemForExcelFromEntity(eventsList);
            var finalList = finalReservationsList.Concat(finalEventsList).ToList();

            var cascadedReservationsList = basicRepository.GetAllListEntity(userId, cascadedFetchXml + cascadedFetchXmlForReservations);
            var cascadedFinalReservationsList = EventItemForExcelFromEntity(cascadedReservationsList);
            var cascadedEventsList = basicRepository.GetAllListEntity(userId, cascadedFetchXml + cascadedFetchXmlForEvents);
            var cascadedFinalEventsList = EventItemForExcelFromEntity(cascadedEventsList);
            var finalCascadedList = cascadedFinalReservationsList.Concat(cascadedFinalEventsList).ToList();
            // startDate = "01/03/2022";
            // endDate = "01/03/2022";
            var totalFinalList = scheduleEventsHelperService.UnionScheduleEvents(userId, finalList, finalCascadedList);

            totalFinalList = this.scheduleEventsHelperService.CalculateKm(totalFinalList);
            if (startDate != string.Empty && endDate != string.Empty)
            {
                List<EventItemForExcel> contractDaysandSik = activityDayRepository.GetContractDaysAndSikForExcel(userId, startDate, endDate);
                totalFinalList.AddRange(contractDaysandSik);

            }
            return totalFinalList;
        }
        public string CreateScheduleReportForServiceProviderByDates(string userId, string serviceProviderId, DataDate dataDate)
        {
            var fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                              <entity name='new_event_item'>
                                <attribute name='new_service_provider' />
                                <attribute name='new_activity_day' />
                                <attribute name='new_reservation' />
                                <attribute name='new_event_reservations' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <filter type='or'>
                                    <condition attribute='new_reservation' operator='not-null' />
                                    <condition attribute='new_event_reservations' operator='not-null' />
                                  </filter>
                                  <condition attribute='new_service_provider' operator='eq' value='{serviceProviderId}' />
                                  <condition attribute='new_start_datetime' operator='on-or-after' value='{dataDate?.StartDate}' />
                                  <condition attribute='new_start_datetime' operator='on-or-before' value='{dataDate?.EndDate}' />
                                  <condition attribute='new_status' operator='ne' value='5' />
                                </filter>
                                <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='am'>
                                  <filter type='and'>
                                    <condition attribute='statecode' operator='eq' value='0' />
                                    <condition attribute='new_activity_day_status' operator='not-in'>
                                      <value>6</value>
                                      <value>3</value>
                                    </condition>
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";
            var entitylist = basicRepository.GetAllListEntity("", fetchXml);
            if (entitylist.Count == 0)
            {
                return null;
            }
            List<Filter> filters = new List<Filter>();
            List<Sort> sorts = new List<Sort>();
            sorts.Add(new Sort { Active = "startDateTime" });
            var serviceProviderName = "";
            foreach (var entity in entitylist)
            {
                if (entity != null)
                {
                    var details = queryHelperRepository.GetAllValuesOfEntity(entity);
                    var eventReservationId = queryHelperRepository.GetValue(details, "new_event_reservations", true);
                    var activityDayId = queryHelperRepository.GetValue(details, "new_activity_day", true);
                    var reservationId = queryHelperRepository.GetValue(details, "new_reservation", true);

                    if (string.IsNullOrEmpty(serviceProviderName))
                    {
                        serviceProviderName = queryHelperRepository.GetValue(details, "new_service_provider", false, true);
                    }
                    if (!string.IsNullOrEmpty(reservationId) && filters.Find(f => f.Field == "reservation" && f.Value == reservationId) == null)
                    {
                        filters.Add(new Filter { Field = "reservation", Value = reservationId, Type = "lookup", Operator = "eq" });
                        reservationId = "";
                    }
                    if (!string.IsNullOrEmpty(eventReservationId) && filters.Find(f => f.Field == "eventReservation" && f.Value == eventReservationId) == null)
                    {
                        filters.Add(new Filter { Field = "eventReservation", Value = eventReservationId, Type = "lookup", Operator = "eq" });
                    }
                    if (!string.IsNullOrEmpty(activityDayId) && filters.Find(f => f.Alias == "ActivityDay" && f.Value == activityDayId) == null)
                    {
                        filters.Add(new Filter { Field = "id", Value = activityDayId, Type = "lookup", Operator = "eq", Alias = "ActivityDay" });
                    }
                }
            }
            var fulldata = reportsService.GetFullActivityDayList(userId, filters, sorts, null, serviceProviderId);
            if (fulldata.Count == 0)
            {
                return null;
            }
            var dataForPdfCreator = SetScheduleData(fulldata, serviceProviderName, -1, false);
            var file = reportsService.CreateScheduleWordReport(dataForPdfCreator);
            if (file == null)
            {
                return null;
            }
            return file.FullPath;

        }
        public string CreateScheduleReport(string userId, string reservationId, bool isHideServiceProviderName)
        {
            var fulldata = reservationsService.GetFetchXml(userId, reservationId);
            if (fulldata == null)
            {
                return null;
            }
            var fullDataToList = new List<ReservationItem>();
            fullDataToList.Add(fulldata);
            var dataForPdfCreator = SetScheduleData(fullDataToList, "", -1, isHideServiceProviderName);
            var file = reportsService.CreateScheduleWordReport(dataForPdfCreator);
            if (file == null)
            {
                return null;
            }
            return file.FullPath;
        }
        public override Guid Create(EventItem item, string userId)
        {
            Guid guid = new Guid();
            try
            {
                if (item.ScheduleItem != null && resourceRepository.IsUnionResource(item.ScheduleItem.Id, userId))
                {
                    item.IsUniteEventItem = true;
                    guid = eventItemRepository.Create(item, userId);
                    item.Id = guid.ToString();
                    eventItemRepository.UnionEventItem(item, null, userId);
                }
                else
                {
                    guid = eventItemRepository.Create(item, userId);
                    item.Id = guid.ToString();
                    if (item.EventStatus == (int)EventStatus.Cancelled && item.EventType == (int)EventType.NotForSchedule)
                    {
                        Delete(item, userId);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("error in create schedule event: ", ex.Message);
                throw;
            }
            return guid;
        }

        private object SetScheduleData(List<ReservationItem> fullDataList, string serviceProviderName = "", int day = -1, bool isHideServiceProviderName = false)
        {
            List<object> list = new List<object>();
            foreach (var item in fullDataList)
            {
                var depName = "";
                var depFax = "";
                var depTel = "";
                if (item.BudgetFramework != null)
                {
                    depName = item.BudgetFramework.DepartmentName;
                    depFax = item.BudgetFramework.DepartmentFax;
                    depTel = item.BudgetFramework.DepartmentTelephone;
                }
                else if (item.ActivityDayList[0].Reservation != null && item.ActivityDayList[0].Reservation.BudgetFramework != null)
                {
                    depName = item.ActivityDayList[0].Reservation.BudgetFramework.DepartmentName;
                    depFax = item.ActivityDayList[0].Reservation.BudgetFramework.DepartmentFax;
                    depTel = item.ActivityDayList[0].Reservation.BudgetFramework.DepartmentTelephone;
                }
                else if (item.ActivityDayList[0].EventReservation != null && item.ActivityDayList[0].EventReservation.BudgetFramework != null)
                {
                    depName = item.ActivityDayList[0].EventReservation.BudgetFramework.DepartmentName;
                    depFax = item.ActivityDayList[0].EventReservation.BudgetFramework.DepartmentFax;
                    depTel = item.ActivityDayList[0].EventReservation.BudgetFramework.DepartmentTelephone;
                }
                var coordinator = item.Coordinator != null ? item.Coordinator.Value : "";
                if (item.isEventReservation)
                {
                    if (item.ActivityDayList[0].EventReservation != null && item.ActivityDayList[0].EventReservation.Coordinator != null)
                    {
                        coordinator = item.ActivityDayList[0].EventReservation.Coordinator.Value;
                    }
                }
                var groupName = "";
                if (item.GroupName != null && item.GroupName != "no-name")
                {
                    groupName = item.GroupName;
                }
                var reservation = new
                {
                    reservationNum = item.ReservationNum,
                    isEventReservation = item.isEventReservation,
                    division = item.DivisionRouter != null ? item.DivisionRouter : "1",
                    instName = item.Institution.OnlyName,
                    instCity = item.Institution != null && item.Institution.AddressInfo != null && item.Institution.AddressInfo.City != null ? item.Institution.AddressInfo.City : "",
                    groupName = groupName,
                    coordinator = coordinator,
                    ageGroup = item.AgeGroup != null ? item.AgeGroup.Value : "",
                    language = item.Language.Value,
                    departmentName = depName,
                    departmentTelephone = depTel,
                    departmentFax = depFax,
                    days = new List<object>(),
                };
                if (day != -1)
                {
                    reservation.days.Add(SetScheduleSingleDay(item, day));
                }
                else
                {//all days
                    for (var i = 0; i < item.ActivityDayList.Count; i++)
                    {
                        reservation.days.Add(SetScheduleSingleDay(item, i));
                    }
                }
                list.Add(reservation);
            }
            return new { reservations = list, serviceProvider = serviceProviderName, isHideServiceProviderName = isHideServiceProviderName };
        }
        private object SetScheduleSingleDay(ReservationItem item, int day)
        {
            var currentStartDateTime = DateTime.Parse(item.ActivityDayList[day].StartDateTime);
            var currentEndDateTime = DateTime.Parse(item.ActivityDayList[day].EndDateTime);
            string[] daysOfweek = new string[] { "ראשון", "שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת" };

            var data = new
            {
                date = currentStartDateTime.ToString("dd/MM/yyyy"),
                startTime = currentStartDateTime.ToString("HH:mm"),
                endTime = currentEndDateTime.ToString("HH:mm"),
                dayOfWeek = daysOfweek[(int)currentStartDateTime.DayOfWeek],
                subject = item.ActivityDayList[day].ActivityDaySubject != null ? item.ActivityDayList[day].ActivityDaySubject.Value : "",
                numberOfGroups = item.ActivityDayList[day].NumberOfGroups,
                groups = new List<object>()
            };

            item.ActivityDayList[day].ItineraryGroupList
              .ForEach(group =>
              {
                  var curGroup = new
                  {
                      groupNumber = group.GroupNumber,
                      groupName = group.Name,
                      participantsNumber = group.NumberOfParticipants,
                      groupServiceProvider = group.GuideScheduleEvent != null ? group.GuideScheduleEvent.ScheduleItem.Value : "",
                      groupResource = group.RoomScheduleEvent != null ? group.RoomScheduleEvent.ScheduleItem.Value : "",
                      details = new List<object>()
                  };
                  group.SchedulerItemList.ForEach(sch =>
                  {
                      curGroup.details.Add(
                          new
                          {
                              startHour = sch.Start.Hours.ToString().PadLeft(2, '0') + ":" + sch.Start.Minutes.ToString().PadLeft(2, '0'),
                              endHour = sch.End.Hours.ToString().PadLeft(2, '0') + ":" + sch.End.Minutes.ToString().PadLeft(2, '0'),
                              product = sch.Product != null ? sch.Product.Value : "",
                              subject = sch.ProductSubject != null ? sch.ProductSubject.Value : "",
                              serviceProvider = sch.SchedulerServiceProvider != null ? sch.SchedulerServiceProvider.ScheduleItem.Value : "",
                              resource = sch.SchedulerResource != null ? sch.SchedulerResource.ScheduleItem.Value : "",
                              comments = sch.Comments
                          }
                  );
                  });
                  data.groups.Add(curGroup);
              });

            return data;
        }
        private List<EventItemForExcel> EventItemForExcelFromEntity(List<Entity> entities)
        {

            var list = new List<EventItemForExcel>();
            Dictionary<string, string> dic = new Dictionary<string, string>();
            EventItemForExcel excelItem;
            foreach (var entity in entities)
            {
                var details = queryHelperRepository.GetAllValuesOfEntity(entity);


                //event item
                excelItem = new EventItemForExcel();
                excelItem.ServiceProviderFullName = queryHelperRepository.GetValue(details, "new_service_provider", false, true);
                excelItem.EventItemId = queryHelperRepository.GetValue(details, "new_event_itemid");//to check this!
                excelItem.StartEvent = ToDate(queryHelperRepository.GetValue(details, "new_start_datetime"));
                excelItem.EndEvent = ToDate(queryHelperRepository.GetValue(details, "new_end_datetime"));
                excelItem.ModifiedEvent = ToDate(queryHelperRepository.GetValue(details, "modifiedon"));
               // excelItem.IsPaymentForTravel = Convert.ToBoolean(queryHelperRepository.GetValue(details, "new_is_pay_for_travel"));
                Boolean.TryParse(queryHelperRepository.GetValue(details, "new_is_virtual"), out bool isVirtual);
                excelItem.IsVirtual = isVirtual;
                Boolean.TryParse(queryHelperRepository.GetValue(details, "new_without_preparation"), out bool isWithoutPreparation);
                excelItem.IsWithoutPreparation = isWithoutPreparation;
                excelItem.IsForPay = Convert.ToBoolean(queryHelperRepository.GetValue(details, "new_is_for_pay"));
                excelItem.ActivityType = new IdValuePair();
                excelItem.ActivityType.Id = queryHelperRepository.GetValue(details, "new_yv_product", true, false).ToString();
                excelItem.ActivityType.Value = queryHelperRepository.GetValue(details, "new_yv_product", false, true).ToString();
                excelItem.EventStatus = Convert.ToInt32(queryHelperRepository.GetValue(details, "new_status"));
                excelItem.Comments = queryHelperRepository.GetValue(details, "new_comments");
                //excelItem.IsTaxiRequired = Convert.ToBoolean(queryHelperRepository.GetValue(details, "new_is_taxi_required"));
                //excelItem.IsTaxiRequired = Convert.ToBoolean(queryHelperRepository.GetValue(details, "new_is_taxi_required"));
                string paymetForTravelType = queryHelperRepository.GetValue(details, "new_pay_for_travel_type");
                excelItem.PaymentForTravelType = !string.IsNullOrEmpty(paymetForTravelType) ? Convert.ToInt32(paymetForTravelType) : 0;
                string paymetForTaxiType = queryHelperRepository.GetValue(details, "new_pay_for_taxi_type");
                excelItem.PaymentForTaxiType = !string.IsNullOrEmpty(paymetForTaxiType) ? Convert.ToInt32(paymetForTaxiType) : 0;
                string category = queryHelperRepository.GetValue(details, "EventReservation.new_event_type");
                switch (category)
                {
                    case "2":
                        excelItem.ReservationCategory = "יום עיון";
                        break;
                    case "3":
                        excelItem.ReservationCategory = "סמינר";
                        break;
                    case "1":
                        excelItem.ReservationCategory = "השתלמות";
                        break;
                    case "5":
                        excelItem.ReservationCategory = "הכשרה";
                        break;
                    case "4":
                        excelItem.ReservationCategory = "הנצחה";
                        break;
                    default:
                        excelItem.ReservationCategory = "הזמנה";
                        break;

                }
                //service provider
                excelItem.ServiceProviderId = queryHelperRepository.GetValue(details, "ServiceProvider.new_id_number");
                excelItem.isNationalService = queryHelperRepository.GetValue(details, "ServiceProvider.new_rsv_status_is_national_service");
                excelItem.EmploymentType = queryHelperRepository.GetValue(details, "ServiceProvider.new_employment_type");
                Boolean.TryParse(queryHelperRepository.GetValue(details, "ServiceProvider.new_is_for_malam"), out bool isForMalam);
                excelItem.IsForMalam = isForMalam;
                excelItem.MainDepartment = new IdValuePair();
                excelItem.MainDepartment.Id = queryHelperRepository.GetOptionsetKey(entity, "new_main_department", "ServiceProvider");
                excelItem.MainDepartment.Value = queryHelperRepository.GetOptionsetValue(entity, "new_main_department", "ServiceProvider");
                excelItem.Generation = new IdValuePair();
                excelItem.Generation.Id = queryHelperRepository.GetOptionsetKey(entity, "new_generation", "ServiceProvider");
                excelItem.Generation.Value = queryHelperRepository.GetOptionsetValue(entity, "new_generation", "ServiceProvider");
                excelItem.StartWorkDate = ToDate(queryHelperRepository.GetValue(details, "ServiceProvider.new_start_work_date"));

                excelItem.ServiceProviderCityLocation = queryHelperRepository.GetValue(details, "ServiceProvider.new_city").ToString();
                if (Boolean.TryParse(queryHelperRepository.GetValue(details, "ServiceProvider.new_rsv_status_is_yv_employee"), out bool temp))
                {
                    excelItem.IsServiceProviderEmployee = temp;
                }


                if (!string.IsNullOrEmpty(queryHelperRepository.GetValue(details, "itineraryItem.new_itinerary_itemid")))
                {
                   excelItem.associatedId = "שורת לוז";
                }
                else if (!string.IsNullOrEmpty(queryHelperRepository.GetValue(details, "itineraryGroup.new_itinerary_groupid")))
                {
                    excelItem.associatedId = "קבוצת לוז";
                }
                else if (!string.IsNullOrEmpty(queryHelperRepository.GetValue(details, "ActivityDay.new_activity_dayid")) && queryHelperRepository.GetValue(details, "new_event_id") == "5")
                {
                    excelItem.associatedId = "יום פעילות";
                }
                

                //ActivityDay
                var activityDayStatus = queryHelperRepository.GetValue(details, "ActivityDay.new_activity_day_status");
                excelItem.ActivityDayStatus = activityDayStatus == "1" ? "חדש" : activityDayStatus == "2" ? "סיום טיפול" : activityDayStatus == "3" ? "מבוטל" : activityDayStatus == "4" ? "ממתין לשיבוץ" : activityDayStatus == "5" ? "ממתין לאישור מרכז הזמנות" : activityDayStatus == "6" ? "הצעת מחיר" : "";
                excelItem.Location = new IdValuePair();
                excelItem.Location.Value = queryHelperRepository.GetValue(details, "ActivityDay.new_activity_day_location", false, true).ToString();
                if (queryHelperRepository.GetValue(details, "new_is_activity_location_different") == "True")
                {
                    excelItem.Location.Value = queryHelperRepository.GetValue(details, "new_event_item_activity_location");
                }
                if (excelItem.IsVirtual)
                {
                    excelItem.Location.Value = "וירטואלי";
                }
                excelItem.Start = ToDate(queryHelperRepository.GetValue(details, "ActivityDay.new_start_datetime"));
                excelItem.End = ToDate(queryHelperRepository.GetValue(details, "ActivityDay.new_end_datetime"));

                //reservation
                string prefix = "Reservation.";
                excelItem.ReservationNum = queryHelperRepository.GetValue(details, "Reservation.new_num");
                excelItem.NumberOfGroups = queryHelperRepository.GetValue(details, "ActivityDay.new_number_of_groups");
                if (excelItem.ReservationNum != string.Empty)//this is reservation
                {
                    excelItem.ReservationGroupName = queryHelperRepository.GetValue(details, "Reservation.new_name").Replace("no-name", "");
                    //  excelItem.NumberOfGroups = GetValue(details, "Reservation.new_number_of_groups");
                    excelItem.ResponsibleAgent = queryHelperRepository.GetValue(details, "Reservation.new_responsible_agent", false, true);
                    if (Boolean.TryParse(queryHelperRepository.GetValue(details, "Reservation.new_is_address_different_from_institution"), out bool result))
                    {
                        excelItem.IsAddressDifferentFromInstitution = result;
                    }
                    excelItem.ActivityLocationCity = queryHelperRepository.GetValue(details, "Reservation.new_activity_location_city").ToString();
                    excelItem.ActivityLocationAddress = queryHelperRepository.GetValue(details, "Reservation.new_activity_location_address").ToString();
                    if (Boolean.TryParse(queryHelperRepository.GetValue(details, "Reservation.new_is_reservation_locked"), out bool locked))
                    {
                        excelItem.IsReservationLocked = locked;
                    }
                    //Institution
                    excelItem.Institution = new Institution();
                    excelItem.Institution.Name = queryHelperRepository.GetValue(details, "Institution.new_name");
                    excelItem.Institution.SushaimeCode = queryHelperRepository.GetValue(details, "Institution.new_sushaime_code");
                    excelItem.Institution.AddressInfo = new AddressInfo();
                    excelItem.Institution.AddressInfo.Address = queryHelperRepository.GetValue(details, "Institution.new_address");
                    excelItem.Institution.AddressInfo.City = queryHelperRepository.GetValue(details, "Institution.new_city");

                }
                else//this is event reservation
                {
                    //event reservation
                    prefix = "EventReservation.";
                    excelItem.IsEventReservation = true;


                    excelItem.IsReservationLocked = false;
                    excelItem.DivisionRouter = queryHelperRepository.GetValue(details, "EventReservation.new_division_router");
                    excelItem.ReservationNum = queryHelperRepository.GetValue(details, "EventReservation.new_event_code");
                    excelItem.ReservationGroupName = queryHelperRepository.GetValue(details, "EventReservation.new_internal_name");
                    if (string.IsNullOrEmpty(excelItem.ReservationGroupName))
                    {
                        excelItem.ReservationGroupName = queryHelperRepository.GetValue(details, "EventReservation.new_name");
                    }
                    excelItem.NumberOfGroups = queryHelperRepository.GetValue(details, "ActivityDay.new_number_of_groups");
                    excelItem.Institution = new Institution();
                    excelItem.Institution.Name = "יד ושם- תשלומי השתלמויות וסמינרים";
                    excelItem.Institution.SushaimeCode = "9110684";
                    excelItem.ActivityLocationAddress = queryHelperRepository.GetValue(details, "EventReservation.new_location").ToString();
                    excelItem.ActivityLocationCity = queryHelperRepository.GetValue(details, "EventReservation.new_city").ToString();
                    excelItem.IsAddressDifferentFromInstitution = false;
                    if (excelItem.Location.Value == LocationTypeHelper.Customer)
                    {
                        excelItem.Institution.AddressInfo = new AddressInfo();
                        excelItem.Institution.AddressInfo.Address = excelItem.ActivityLocationAddress;
                        excelItem.Institution.AddressInfo.City = excelItem.ActivityLocationCity;
                    }





                }

                //BudgetFramework
                excelItem.BudgetFramework = new BudgetFramework();
                excelItem.BudgetFramework.TatPeilutCode = queryHelperRepository.GetValue(details, prefix + "BudgetFramework.new_framework_code");
                excelItem.BudgetFramework.Name = queryHelperRepository.GetValue(details, prefix + "BudgetFramework.new_name");
                excelItem.BudgetFramework.MalamCode = new IdValuePair();
                excelItem.BudgetFramework.MalamCode.Value = queryHelperRepository.GetValue(details, prefix + "BudgetFramework.new_malam_code");

                //to check if workes -TODO
                if (excelItem.IsEventReservation)
                {
                    if (excelItem.ReservationCategory == "סמינר")//in seminar need to take tat peilut from budget section instead of budget framework devora 7.2.22
                    {
                        string BudgetSection = queryHelperRepository.GetValue(details, "EventReservation.new_budget_section");
                        string malamCode = queryHelperRepository.GetValue(details, "EventReservation.new_malam_code");
                        if (!string.IsNullOrEmpty(BudgetSection))//סמינרים
                        {
                            excelItem.BudgetFramework.TatPeilutCode = BudgetSection;
                            excelItem.BudgetFramework.Name = "סעיף תקציבי ידני";
                            excelItem.BudgetFramework.MalamCode.Value = malamCode;
                        }
                    }
                    excelItem.BudgetFramework.Department = new IdValuePair();
                    excelItem.BudgetFramework.Department.Value = queryHelperRepository.GetValue(details, "EventReservation.BudgetFramework.new_rsv_department", false, true);


                    if (excelItem.BudgetFramework != null && excelItem.BudgetFramework.Department != null && excelItem.BudgetFramework.Department.Value != string.Empty)
                    {
                        excelItem.ResponsibleAgent = excelItem.BudgetFramework.Department.Value;

                    }

                }












                // 







                list.Add(excelItem);
            }
            //var temp1 = list.Where(x => x.ServiceProviderFullName == "יהודה ירון").ToList();
            return list;
        }





        public string ToDate(string dateTime)
        {
            try
            {
                return Convert.ToDateTime(dateTime).ToLocalTime().ToString();
            }
            catch (Exception)
            {

                return dateTime;
            }

        }

        public void CreateFakeSchedule(string userId, string startDate, string endDate)
        {
            if(startDate == null || endDate == null)
            {
                startDate = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");//תאריך של אתמול בדיפולט
                endDate = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            }
            DateTime start = DateTime.ParseExact(startDate, "dd/MM/yyyy", null).AddDays(-1);//תאריך של אתמול;
            DateTime end = DateTime.ParseExact(endDate, "dd/MM/yyyy", null).AddDays(-1);

            for (DateTime date = start; date <= end; date = date.AddDays(1))
            { 
                int dayOfWeek = (int)date.DayOfWeek + 1;//מוסיפים 1 כי זה מתחיל ב0
                string contractDay = Enum.GetName(typeof(ContractDayWeekDay), dayOfWeek);
                string fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                        <entity name='new_service_provider'>
                                            <attribute name='new_service_providerid'/>
                                            <attribute name='new_name'/>
                                            <attribute name='createdon'/>
                                            <link-entity name='new_contract_days' from='new_contract_days' to='new_service_providerid' alias='contractDay'>
                                                <attribute name='new_yv_product'/>                                            
                                                <filter type='and'>
                                                    <condition attribute='{contractDay}' operator='eq' value='1'/>
                                                    <condition attribute='new_yv_product' operator='not-null'/>
                                                </filter>
                                            </link-entity>
                                            <link-entity name='new_event_item' from='new_service_provider' to='new_service_providerid' alias='eventItem' link-type='outer'>
                                              <filter type='and'>
                                                <condition attribute='new_start_datetime' operator='on' value='{date.ToString("yyyy-MM-dd")}'/>
                                                <condition attribute='statecode' operator='eq' value='0'/>
                                              </filter>
                                            </link-entity>
                                            <filter type='and'>
                                              <condition entityname='eventItem' attribute='new_event_itemid' operator='null'/>
                                            </filter>
                                        </entity>
                                    </fetch>";

                List<ServiceProvider> serviceProviders = serviceProviderRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

                var dayConditions = new Dictionary<int, Func<ContractDays, bool>>
                    {
                        { 1, c => c.ContractDay_Sunday },
                        { 2, c => c.ContractDay_Monday },
                        { 3, c => c.ContractDay_Tuesday },
                        { 4, c => c.ContractDay_Wednesday },
                        { 5, c => c.ContractDay_Thursday },
                        { 6, c => c.ContractDay_Friday }
                    };

                foreach (var serviceProvider in serviceProviders)
                {
                    IdValuePair product = serviceProvider.ContractDaysByYear.Find(new Predicate<ContractDays>(dayConditions[dayOfWeek]))?.Product;
                    if (product != null)
                    {
                        EventItem scheduleEventItem = new EventItem
                        {
                            Start = date.ToString("MM/dd/yyyy"),
                            End = date.ToString("MM/dd/yyyy"),
                            StartTime = new Time { Hours = 9, Minutes = 0 },
                            EndTime = product.Value.Contains("3") ?
                                new Time { Hours = 12, Minutes = 0 } :
                                    product.Value.Contains("6") ? new Time { Hours = 15, Minutes = 0 } :
                                        product.Value.Contains("8") ? new Time { Hours = 17, Minutes = 0 } :
                                        null,
                            Product = product,
                            IsForPay = true,
                            EventStatus = (int)EventStatus.Approved,
                            Comments = "שיבוץ יום חוזה אוטומטי",
                            ReservationGroupName = "יום חוזה",
                            EventType = (int)EventType.ContractDayNoReservationScheduling,
                            Description = "שיבוץ יום חוזה ללא הזמנה"
                        };
                        eventItemRepository.Create(scheduleEventItem, userId);
                    }
                }
            }
        }

    }
}
