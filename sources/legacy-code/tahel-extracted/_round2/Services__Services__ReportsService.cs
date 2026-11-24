using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Data.SendMailWS;
using Microsoft.Reporting.WebForms.Internal.Soap.ReportingServices2005.Execution;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using regeditor;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;

using Warning = Microsoft.Reporting.WebForms.Internal.Soap.ReportingServices2005.Execution.Warning;

namespace Services.Services
{
    public class ReportsService : BasicService<FrontDeskListItem, FrontDeskListItem, ActivityDayCRM>, IReportsService
    {
        private readonly IQueryHelperRepository queryHelperRepository;
        private readonly IItineraryGroupRepository itineraryGroupRepository;
        private readonly IBidItemRepository bidItemRepository;
        private readonly IFullEventItemRepository fullEventItemRepository;
        private readonly IActivityDayRepository activityDayRepository;
        private readonly IFrontDeskRepository frontDeskRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IItineraryItemRepository itineraryItemRepository;
        private readonly ActivityDayGroupedRepository activityDayGroupedRepository;
        private readonly string environment;
        protected readonly ICRMConnection crmConnection;

        public ReportsService(IQueryHelperRepository queryHelperRepository,
            ActivityDayGroupedRepository activityDayGroupedRepository,
            ICRMConnection crmConnection,
            IItineraryGroupRepository itineraryGroupRepository,
            IItineraryItemRepository itineraryItemRepository,
            IBidItemRepository bidItemRepository,
            IActivityDayRepository activityDayRepository,
            IHelperService helperService,
            IMapFunctionRepository mapFunctionRepository,
            IFullEventItemRepository fullEventItemRepository,
            IFrontDeskRepository frontDeskRepository
            )
            : base(helperService, frontDeskRepository, mapFunctionRepository)
        {

            this.queryHelperRepository = queryHelperRepository;
            this.crmConnection = crmConnection;
            this.itineraryItemRepository = itineraryItemRepository;
            this.activityDayGroupedRepository = activityDayGroupedRepository;
            this.itineraryGroupRepository = itineraryGroupRepository;
            this.bidItemRepository = bidItemRepository;
            this.activityDayRepository = activityDayRepository;
            this.fullEventItemRepository = fullEventItemRepository;
            this.frontDeskRepository = frontDeskRepository;
            environment = ConfigurationManager.AppSettings["environment"];
        }

        public List<FrontDeskListItem> GetActivityDayListWithServiceProviders(string userId, List<Filter> filters)
        {
            Filter dateFilter = filters.Find(f => f.Field == "startDateTime");
        
            string divisionValue = "";
            string conditions = "";

            Filter responsibleAgent = filters.Find(f => f.Field == "ResponsibleAgent");//should be responsibleAgent

            Filter division = filters.Find(f => f.Field == "divisionRouter");
            if (division != null)
            {
                divisionValue = division.Value;
            }
            if (dateFilter != null)
            {
                var date1 = dateFilter.Value.Split(',')[0];
                var date2 = dateFilter.Value.Split(',')[1];
                conditions += "<condition attribute='new_start_datetime' operator='on-or-after' value='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date1).ToString("yyyy-MM-dd")
                if (date2.Trim() != "0")
                {
                    conditions += "<condition attribute='new_start_datetime' operator='on-or-before' value='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date2).ToString("yyyy-MM-dd")
                }
            }
            Filter idFilter = filters.Find(f => f.Field == "id" && (f.Alias == "ActivityDay"));
            if (idFilter!=null)
            {
                conditions += $"<condition attribute='new_activity_dayid' operator='eq' value='{idFilter.Value}' />";
               
            }
            List<FrontDeskListItem> report = new List<FrontDeskListItem>();
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_activity_day'>
                      <attribute name='new_end_datetime' />
                      <attribute name='new_start_datetime' />
                      <attribute name='new_activity_day_subject' />
                      <attribute name='new_name' />
                      <attribute name='new_testimony_amount_needed' />
                      <attribute name='new_activity_day_status' />
                      <attribute name='new_activity_day_type' />
                      <attribute name='new_activity_day_location' />
                      <attribute name='new_lecturer_amount_needed' />
                      <attribute name='new_number_of_groups' />
                      <attribute name='new_number_of_participants' />
                      <attribute name='new_guide_amount_needed' />
                      <attribute name='new_cantor_amount_needed' />
                        <attribute name='new_actor_amount_needed' />
                      <attribute name='new_reservation' />
                      <attribute name='new_testimony_amount_scheduled' />
                      <attribute name='new_lecturer_amount_scheduled' />
                      <attribute name='new_guide_amount_scheduled' />
                      <attribute name='new_cantor_amount_scheduled' />
                        <attribute name='new_actor_amount_scheduled' />
                      <filter type='and'>" + conditions + @"
                        <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_activity_day_status' operator='not-in'>
                          <value>6</value>
                          <value>3</value>
                        </condition>";
                        if (divisionValue != "")
                        {
                            fetchXml += "<condition attribute='new_division_router' operator='eq' value='" + divisionValue + @"' />";
                        }

            fetchXml += @"<filter type='or'>
                            <condition attribute='new_cantor_amount_needed' operator='gt' value='0' />
                            <condition attribute='new_actor_amount_needed' operator='gt' value='0' />
                            <condition attribute='new_guide_amount_needed' operator='gt' value='0' />
                            <condition attribute='new_lecturer_amount_needed' operator='gt' value='0' />
                            <condition attribute='new_testimony_amount_needed' operator='gt' value='0' />
                        </filter>
                    </filter>
                    <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'>
                      <attribute name='new_rsv_institution' />

                    <link-entity name='new_rsv_institution' from='new_rsv_institutionid' to='new_rsv_institution' alias='Institution' link-type='outer'>
                                          <attribute name='new_is_haredi' />
                    </link-entity>

                      <attribute name='new_country' />
                      <attribute name='new_name' />
                      <attribute name='new_num' />
                      <attribute name='new_firstname_contact' />
                      <attribute name='new_lastname_contact' />
                      <attribute name='new_email_contact' />
                      <attribute name='new_mobile_contact' />
                      <attribute name='new_language' />
                      <attribute name='new_internal_comments' />
                      <attribute name='new_yv_comments' />
                      <attribute name='new_payment_status' />
                      <attribute name='new_reservation_status' />
                      <attribute name='new_is_order_confirmed' />
                      <attribute name='new_division_router' />
                      <attribute name='new_budget_framework' />
                      <attribute name='new_yv_coordinator' />
                      <attribute name='new_responsible_division' />
                      <attribute name='new_responsible_agent' />
                    <attribute name='new_activity_location_address' />
                    <attribute name='new_activity_location_city' />";

            if (responsibleAgent != null)
            {
                fetchXml += @"<attribute name='new_is_include_ceremony' />
                        <attribute name='new_is_visitor_vip' />
                        <attribute name='new_visitor_name' />
                        <attribute name='new_visitor_role' />
                        <attribute name='new_visitor_present' />";
            }


            fetchXml += @"<link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' alias='BudgetFramework' link-type='outer'>
                        <attribute name='new_rsv_department' /></link-entity>
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_reservation_status' operator='not-in'>
                          <value>6</value>
                          <value>3</value>
                        </condition>";
            if (responsibleAgent != null)
                    {
                        fetchXml += "<condition attribute='new_responsible_agent' operator= 'eq' value = '{" + responsibleAgent.Value + @"}'/>";

                    }


            fetchXml += @"</filter>
            </link-entity>
                  </entity>
                </fetch>";

            report = frontDeskRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_event_item'>
                    <attribute name='new_name' />
                    <attribute name='createdon' />
                    <attribute name='new_end_datetime' />
                    <attribute name='new_start_datetime' />
                    <attribute name='new_status' />
                    <attribute name='new_number_of_participants' />
                    <attribute name='new_rsv_department' />
                    <attribute name='new_comments' />
                    <attribute name='new_event_type' />
                    <attribute name='new_reservation_status' />
                    <attribute name='new_service_provider' />
                    <attribute name='new_resource' />
                    <attribute name='new_yv_product' />
                    <attribute name='new_activity_day' />
                    <attribute name='new_event_itemid' />
                    <order attribute='new_name' descending='false' />
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                      <condition attribute='new_status' operator='eq' value='1' />
                       <filter type='or'>
                            <condition attribute='new_event_type' operator='eq' value='1' /> 
                            <condition attribute='new_event_type' operator='eq' value='4' /> 
                      </filter>" + conditions + @"<condition attribute='new_is_resource' operator='eq' value='0' />
                    </filter>
                  <link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider' link-type='inner'>
                  <attribute name='new_first_name_he' />
                  <attribute name='new_last_name_he' />
                  <attribute name='new_mobile' />
                  <attribute name='new_work_telephone' />
                  <attribute name='new_telephone' />
                   <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                 </link-entity>
                  </entity>
                </fetch>";

            List<FullEventItem> fullEvents = fullEventItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            foreach (FrontDeskListItem frontDeskItem in report)
            {
                frontDeskItem.ServiceProviderList = fullEvents.Where(x => x.ActivityDay.Id == frontDeskItem.Id).ToList();
            }

            return report;
        }
        public List<FullEventItem> GetServiceProviderListForIndividualsTourReport(string userId,string activityDayId) {

           var  fetchXml =$@"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_event_item'>
                    <attribute name='new_activity_day' />
                    <attribute name='new_event_itemid' />
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                       <filter type='or'>
                            <condition attribute='new_event_type' operator='eq' value='1' /> 
                            <condition attribute='new_event_type' operator='eq' value='4' /> 
                      </filter> 
                        <condition attribute='new_is_resource' operator='eq' value='0' />
                        <condition attribute='new_activity_day' operator='eq' value='{activityDayId}' />
                    </filter>
                  <link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider' link-type='inner'>
                  <attribute name='new_first_name_he' />
                  <attribute name='new_last_name_he' />
                  <attribute name='new_mobile' />
                  <attribute name='new_work_telephone' />
                  <attribute name='new_telephone' />
                   <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                 </link-entity>
                  </entity>
                </fetch>";

            List<FullEventItem> fullEvents = fullEventItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;
            return fullEvents;
        }
        public List<FrontDeskListItem> GetActivityDayListWithEverything(string userId, List<Filter> filters)
        {
            Filter item = filters.Find(f => f.Field == "startDateTime");
            string conditions = "";
            string date1 = item.Value.Split(',')[0];
            string date2 = item.Value.Split(',')[1];
            string divisionValue = "";
            Filter division = filters.Find(f => f.Field == "divisionRouter");
            if (division != null)
            {
                divisionValue = division.Value;
            }
            conditions += "<condition attribute='new_start_datetime' operator='on-or-after' value='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date1).ToString("yyyy-MM-dd")
            if (date2.Trim() != "0")
            {
                conditions += "<condition attribute='new_start_datetime' operator='on-or-before' value='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date2).ToString("yyyy-MM-dd")
            }

            List<FrontDeskListItem> report = new List<FrontDeskListItem>();
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_activity_day'>
                      <attribute name='new_end_datetime' />
                      <attribute name='new_start_datetime' />
                      <attribute name='new_activity_day_subject' />
                      <attribute name='new_name' />
                      <attribute name='new_testimony_amount_needed' />
                      <attribute name='new_activity_day_status' />
                      <attribute name='new_activity_day_type' />
                      <attribute name='new_activity_day_location' />
                      <attribute name='new_lecturer_amount_needed' />
                      <attribute name='new_number_of_groups' />
                      <attribute name='new_number_of_participants' />
                      <attribute name='new_guide_amount_needed' />
                      <attribute name='new_cantor_amount_needed' />
                        <attribute name='new_actor_amount_needed' />
                      <attribute name='new_reservation' />
                      <attribute name='new_testimony_amount_scheduled' />
                      <attribute name='new_lecturer_amount_scheduled' />
                      <attribute name='new_guide_amount_scheduled' />
                      <attribute name='new_cantor_amount_scheduled' />
                        <attribute name='new_actor_amount_scheduled' />
                      <filter type='and'>" + conditions + @"
                        <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_activity_day_status' operator='not-in'>
                          <value>6</value>
                          <value>3</value>
                        </condition>";
            if (divisionValue != "")
                fetchXml += "<condition attribute='new_division_router' operator='eq' value='" + divisionValue + @"' />";
            fetchXml += @"</filter>
                    <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'>
                      <attribute name='new_rsv_institution' />

                    <link-entity name='new_rsv_institution' from='new_rsv_institutionid' to='new_rsv_institution' alias='Institution' link-type='outer'>
                                          <attribute name='new_is_haredi' />
                    </link-entity>
                      <attribute name='new_name' />
                      <attribute name='new_num' />
                      <attribute name='new_firstname_contact' />
                      <attribute name='new_lastname_contact' />
                      <attribute name='new_email_contact' />
                      <attribute name='new_mobile_contact' />
                      <attribute name='new_language' />
                      <attribute name='new_internal_comments' />
                      <attribute name='new_yv_comments' />
                      <attribute name='new_payment_status' />
                      <attribute name='new_reservation_status' />
                      <attribute name='new_is_order_confirmed' />
                      <attribute name='new_division_router' />
                      <attribute name='new_budget_framework' />
                      <attribute name='new_yv_coordinator' />
                      <attribute name='new_responsible_division' />
                      <attribute name='new_responsible_agent' />
                    <attribute name='new_activity_location_address' />
                    <attribute name='new_activity_location_city' />
                    <link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' alias='BudgetFramework' link-type='outer'>
                        <attribute name='new_rsv_department' /></link-entity>
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_reservation_status' operator='not-in'>
                          <value>6</value>
                          <value>3</value>
                        </condition>
                    </filter>
                    </link-entity>
                  </entity>
                </fetch>";

            report = frontDeskRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_event_item'>
                    <attribute name='new_name' />
                    <attribute name='createdon' />
                    <attribute name='new_end_datetime' />
                    <attribute name='new_start_datetime' />
                    <attribute name='new_status' />
                    <attribute name='new_number_of_participants' />
                    <attribute name='new_rsv_department' />
                    <attribute name='new_comments' />
                    <attribute name='new_event_type' />
                    <attribute name='new_reservation_status' />
                    <attribute name='new_service_provider' />
                    <attribute name='new_resource' />
                    <attribute name='new_yv_product' />
                    <attribute name='new_activity_day' />
                    <attribute name='new_event_itemid' />
                    <order attribute='new_name' descending='false' />
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                      <condition attribute='new_status' operator='eq' value='1' />
                       <filter type='or'>
                            <condition attribute='new_event_type' operator='eq' value='1' /> 
                            <condition attribute='new_event_type' operator='eq' value='4' /> 
                      </filter>" + conditions + @"<condition attribute='new_is_resource' operator='eq' value='0' />
                    </filter>
                  <link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider' link-type='inner'>
                  <attribute name='new_first_name_he' />
                  <attribute name='new_last_name_he' />
                  <attribute name='new_mobile' />
                  <attribute name='new_work_telephone' />
                  <attribute name='new_telephone' />
                   <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                 </link-entity>
                  </entity>
                </fetch>";

            List<FullEventItem> fullEvents = fullEventItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            foreach (FrontDeskListItem frontDeskItem in report)
            {
                frontDeskItem.ServiceProviderList = fullEvents.Where(x => x.ActivityDay.Id == frontDeskItem.Id).ToList();
            }

            return report;
        }
        public Dictionary<string, List<ReservationItem>> GetMultipleActivityDayListByServiceProvides(string userId, ServiceProviderScheduleForReportList data)
        {
            Filter filter;
            Sort sort = new Sort();
            List<Filter> filters = new List<Filter>();
            List<Sort> sorts = new List<Sort>();
            var keyValuePairs = new Dictionary<string, List<ReservationItem>>();
            List<string> failedList = new List<string>();
            sort.Active = "startDateTime";
            sorts.Add(sort);
            foreach (var item in data.ScheduleForReportsList)
            {
                filters.Clear();
                foreach (string id in item.ReservationList)
                {
                    filter = new Filter();
                    filter.Field = "reservation";
                    filter.Value = id;
                    filters.Add(filter);
                }
                foreach (string id in item.EventReservationList)
                {
                    filter = new Filter();
                    filter.Field = "eventReservation";
                    filter.Value = id;
                    filters.Add(filter);
                }
                foreach (string id in item.ActivityDayList)
                {
                    filter = new Filter();
                    filter.Field = "id";
                    filter.Value = id;
                    filter.Alias = "ActivityDay";
                    filters.Add(filter);
                }
                //filter = new Filter();
                //filter.Field = "ServiceProvider";
                //filter.Value = item.ServiceProviderId;
                //filter.Type = "lookup";
                //filter.Operator = "eq";
                //filters.Add(filter);

                try
                {
                    var FullActivityDayList = GetFullActivityDayList(userId, filters, sorts, data.CurrentUserEmail, item.ServiceProviderId);
                    keyValuePairs.Add(item.ServiceProviderId, FullActivityDayList);

                }
                catch (Exception ex)
                {

                    Trace.TraceError($"{DateTime.Now}: GetFullActivityDayList Failed serviceProvider (name:{item.ServiceProviderName},guid:{item.ServiceProviderId}), error: {ex.Message}");
                    failedList.Add(item.ServiceProviderName);
                }
            }
            if (failedList.Count > 0)
            {
                string message = $@"<b> יצירת קובץ לוז נכשלה עבור {failedList.Count} מדריכים:</b>
                               <br/><br/>";
                foreach (var name in failedList)
                {
                    message += name + "<br/> ";
                }
                message += "<br/> ";
                message += " מייל זה נשלח אוטומטית מן המערכת נא לא להשיב.";
                try
                {
                    Trace.TraceError("error in GetMultipleActivityDayListByServiceProvides failedList count:" + failedList.Count);
                    //Send mail
                    sendMailSoapClient sendMail = new sendMailSoapClient();
                    //הוספת שמרית טל ככתובת מייל לביקורת עד שהמערכת תכנס לשגרה...
                    sendMail.sendMailProcces(data.CurrentUserEmail, "", "shimrit.tal@yadvashem.org.il", "no-reply@yadvashem.org", data.EmailSubject, message, "rtl");

                }
                catch (Exception ex)
                {
                    Trace.Write("send mail to shimrit in GetMultipleActivityDayListByServiceProvides faild, ex:" + ex);
                }
            }
            return keyValuePairs;
        }
        public List<ReservationItem> GetFullActivityDayList(string userId, List<Filter> filters, List<Sort> sorts, string currentUserEmail = null, string serviceProviderId = null)
        {
            //reservationFilters = helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "Reservation"));
            Filter startFilter = filters.Find(f => f.Field == "startDateTime");
            string conditions = "";
            string conditions2 = "";
            string divisionCondition = "";
            string sortXML = "";
            string linkType = "inner";
            bool isEventReservation = false;
            bool isReservation = false;
            if (startFilter != null)
            {
                string date1 = startFilter.Value.Split(',')[0];
                string date2 = startFilter.Value.Split(',')[0];
                conditions += "<condition attribute='new_start_datetime' operator='on' value='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date1).ToString("yyyy-MM-dd")
            }
            List<Filter> idFilters = filters.FindAll(f => f.Field == "id"&&(f.Alias== "ActivityDay"|| f.Alias ==""));
            if (idFilters.Count > 0)
            {
                conditions += "<condition attribute='new_activity_dayid' operator='in'>";
                foreach (var filter in idFilters)
                {
                    conditions += "<value>{" + filter.Value + "}</value>";
                }
                conditions += "</condition>";
            }
            idFilters = filters.FindAll(f => f.Field == "reservation");
            if (idFilters.Count > 0)
            {
                isReservation = true;
                conditions2 += "<condition attribute='new_reservation' operator='in'>";
                foreach (var filter in idFilters)
                {
                    conditions2 += "<value>{" + filter.Value + "}</value>";
                }
                conditions2 += "</condition>";
            }
            idFilters = filters.FindAll(f => f.Field == "eventReservation");
            if (idFilters.Count > 0)
            {
                linkType = "outer";
                isEventReservation = true;
                conditions2 += "<condition attribute='new_event_reservations' operator='in'>";
                foreach (var filter in idFilters)
                {
                    conditions2 += "<value>{" + filter.Value + "}</value>";
                }
                conditions2 += "</condition>";
            }
            if (!isReservation && !isEventReservation)
            {
                //take both as default for daily schedule
                isReservation = true;
                isEventReservation = true;
                linkType = "outer";

            }
            Filter division = filters.Find(f => f.Field == "divisionRouter");
            if (division != null)
            {
                divisionCondition = "<condition attribute='new_division_router' operator='eq' value = '" + division.Value + @"' />";
            }
            List<Filter> additionalFilter = new List<Filter>();

            Filter serviceProvider = filters.Find(f => f.Field == "ServiceProvider");
            if (serviceProvider != null)
            {
                serviceProviderId = serviceProvider.Value;
            }
            if (sorts != null && sorts.Count > 0)
            {
                sorts = sorts.FindAll(s => s.Active == "startDateTime");
                foreach (var sortItem in sorts)
                {
                    sortXML += "<order attribute='new_start_datetime' descending='" + (sortItem.Direction == "desc" ? "true" : "false") + "'/>";
                }
            }

            //if (date2.Trim() != "0")
            //{
            //    conditions += "<condition attribute='new_start_datetime' operator='on-or-before' value='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date2).ToString("yyyy-MM-dd")
            //}

            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_activity_day'>
                      <attribute name='new_end_datetime' />
                      <attribute name='new_start_datetime' />
                      <attribute name='new_activity_day_subject' />
                      <attribute name='new_name' />
                      <attribute name='new_testimony_amount_needed' />
                      <attribute name='new_activity_day_status' />
                      <attribute name='new_activity_day_type' />
                      <attribute name='new_activity_day_location' />
                      <attribute name='new_lecturer_amount_needed' />
                      <attribute name='new_number_of_groups' />
                      <attribute name='new_number_of_participants' />
                      <attribute name='new_guide_amount_needed' />
                      <attribute name='new_cantor_amount_needed' />
                      <attribute name='new_actor_amount_needed' />
                      <attribute name='new_reservation' />
                      <attribute name='new_division_router' />
                      <attribute name='new_event_reservations' />
                      <attribute name='new_testimony_amount_scheduled' />
                      <attribute name='new_lecturer_amount_scheduled' />
                      <attribute name='new_guide_amount_scheduled' />
                      <attribute name='new_cantor_amount_scheduled' />
                      <attribute name='new_actor_amount_scheduled' />
                      <filter type='and'>" + conditions
                  + @"<condition attribute='statecode' operator='eq' value='0' />
                     <condition attribute='new_activity_day_status' operator='not-in'>
                       <value>6</value>
                       <value>3</value>
                     </condition>" + divisionCondition + @"
                      </filter>
                      <filter type='or'>" + conditions2 + @"
                   </filter>" + sortXML;
            if (isReservation)
            {
                fetchXml += @"
                    <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'  link-type='" + linkType + @"'>
                      <attribute name='new_rsv_institution' />
                      <attribute name='new_name' />
                      <attribute name='new_num' />
                      <attribute name='new_language' />
                      <attribute name='new_firstname_contact' />
                      <attribute name='new_lastname_contact' />
                      <attribute name='new_email_contact' />
                      <attribute name='new_mobile_contact' />
                      <attribute name='new_language' />
                      <attribute name='new_internal_comments' />
                      <attribute name='new_yv_comments' />
                      <attribute name='new_payment_status' />
                      <attribute name='new_reservation_status' />
                      <attribute name='new_is_order_confirmed' />
                      <attribute name='new_division_router' />
                      <attribute name='new_budget_framework' />
                      <attribute name='new_age_group' />
                      <attribute name='new_yv_coordinator' />
                    <attribute name='new_responsible_division' />
                    <attribute name='new_activity_location_address' />
                    <attribute name='new_activity_location_city' /> 
                    <link-entity name='new_rsv_institution' from='new_rsv_institutionid' to='new_rsv_institution' alias='Institution' link-type='outer'>
                                   <attribute name='new_city'/>
                     </link-entity>
                    <filter type='and'>
                        <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_reservation_status' operator='not-in'>
                            <value>6</value>
                            <value>3</value>
                        </condition>
                    </filter>
                        <link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' alias='BudgetFramework' link-type='outer'>
                            <link-entity name='new_rsv_department' from='new_rsv_departmentid' to='new_rsv_department' alias='Department' link-type='outer'>
                               <attribute name='new_email'/>
                                <attribute name='new_fax'/>
                                <attribute name='new_telephone'/>
                                <attribute name='new_name'/>
                               </link-entity>
                        </link-entity> 
                    </link-entity>";
            }
            if (isEventReservation)
            {
                fetchXml += @"
                    <link-entity name='new_event_reservations' from='new_event_reservationsid' visible='false' alias='EventReservation' to='new_event_reservations'  link-type='" + linkType + @"'>
                        <attribute name='new_language'/>
                        <attribute name='new_name'/>
                        <attribute name='new_event_code'/>
                        <attribute name='new_yv_coordinator'/>
                        <filter type='and'>
                            <condition attribute='statecode' operator='eq' value='0' />
                          </filter>
                        <link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' alias='BudgetFramework2' link-type='outer'>
                            <link-entity name='new_rsv_department' from='new_rsv_departmentid' to='new_rsv_department' alias='Department2' link-type='outer'>
                               <attribute name='new_email'/>
                                <attribute name='new_fax'/>
                                <attribute name='new_telephone'/>
                                <attribute name='new_name'/>
                               </link-entity>
                            </link-entity>  
                        </link-entity>";

            }
            fetchXml += @"</entity>
                 </fetch>";
            List<ActivityDay> temp = activityDayRepository.GetFetchXmlOnly(userId, fetchXml, additionalFilter).Item2;

            List<ReservationItem> report = new List<ReservationItem>();

            foreach (ActivityDay activityDay in temp)
            {
                bool isRelevant = false;
                if (serviceProviderId != null)
                {
                    foreach (ItineraryGroup itineraryGroup in activityDay.ItineraryGroupList)
                    {
                        if (itineraryGroup.GuideScheduleEvent != null && itineraryGroup.GuideScheduleEvent.ScheduleItem.Id == serviceProviderId)
                        {
                            isRelevant = true;
                        }
                        else
                        {
                            if (itineraryGroup.SchedulerItemList != null)
                            {
                                itineraryGroup.SchedulerItemList = itineraryGroup.SchedulerItemList.Where(item => item.SchedulerServiceProvider != null && item.SchedulerServiceProvider.ScheduleItem.Id == serviceProviderId).ToList();
                                if (itineraryGroup.SchedulerItemList.Count > 0)
                                {
                                    isRelevant = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    isRelevant = true;
                }
                if (isRelevant)
                {
                    ReservationItem reservation = null;
                    if (activityDay.Reservation != null)
                    {
                        reservation = report.Find(r => r.Id == activityDay.Reservation.Id);
                        if (reservation == null)
                        {
                            reservation = new ReservationItem();
                            reservation.Id = activityDay.Reservation.Id;
                            reservation.ReservationNum = activityDay.Reservation.ReservationNum;
                            reservation.GroupName = activityDay.Reservation.GroupName;
                            reservation.Language = activityDay.Reservation.Language;
                            reservation.Institution = new InstitutionForReservation();
                            reservation.Institution.Id = activityDay.Reservation.Institution.Id;
                            reservation.Institution.OnlyName = activityDay.Reservation.Institution.Value;
                            reservation.DivisionRouter = activityDay.DivisionRouter.Id;
                            reservation.AgeGroup = activityDay.Reservation.AgeGroup;
                            reservation.ActivityDayList = new List<ActivityDay>();
                            reservation.Coordinator = activityDay.Reservation.Coordinator;
                            reservation.Institution.AddressInfo = new AddressInfo();
                            reservation.Institution.AddressInfo.City = activityDay.Reservation.InstitutionCity;
                            report.Add(reservation);
                        }
                    }
                    else if (activityDay.EventReservation != null)
                    {
                        reservation = report.Find(r => r.Id == activityDay.EventReservation.Id);
                        if (reservation == null)//devora
                        {
                            reservation = new ReservationItem();
                            reservation.isEventReservation = true;
                            reservation.Id = activityDay.EventReservation.Id;
                            reservation.ReservationNum = activityDay.EventReservation.EventCode;
                            reservation.GroupName = activityDay.EventReservation.Title;
                            reservation.Language = activityDay.EventReservation.Language;
                            reservation.Institution = new InstitutionForReservation();
                            reservation.ActivityDayList = new List<ActivityDay>();
                            report.Add(reservation);
                        }
                    }
                    if (reservation != null && reservation.ActivityDayList != null)
                    {
                        reservation.ActivityDayList.Add(activityDay);
                    }
                }
            }
            return report;
        }


        public List<ReservationItem> GetFullActivityDayListDevora(string userId, List<Filter> filters)
        {
            Filter item = filters.Find(f => f.Field == "startDateTime");
            string conditions = "";
            string date1 = item.Value.Split(',')[0];
            string divisionValue = "2";

            conditions += "<condition attribute='new_start_datetime' operator='on' value='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date1).ToString("yyyy-MM-dd")


            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_activity_day'>
                      <attribute name='new_end_datetime' />
                      <attribute name='new_start_datetime' />
                      <attribute name='new_activity_day_subject' />
                      <attribute name='new_name' />
                      <attribute name='new_testimony_amount_needed' />
                      <attribute name='new_activity_day_status' />
                      <attribute name='new_activity_day_type' />
                      <attribute name='new_activity_day_location' />
                      <attribute name='new_lecturer_amount_needed' />
                      <attribute name='new_number_of_groups' />
                      <attribute name='new_number_of_participants' />
                      <attribute name='new_guide_amount_needed' />
                      <attribute name='new_cantor_amount_needed' />
                      <attribute name='new_actor_amount_needed' />
                      <attribute name='new_reservation' />
                      <attribute name='new_testimony_amount_scheduled' />
                      <attribute name='new_lecturer_amount_scheduled' />
                      <attribute name='new_guide_amount_scheduled' />
                      <attribute name='new_cantor_amount_scheduled' />
                      <attribute name='new_actor_amount_scheduled' />
                      <filter type='and'>" + conditions + @"
                        <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_activity_day_status' operator='not-in'>
                          <value>6</value>
                          <value>3</value>
                        </condition>
                        <condition attribute='new_division_router' operator='eq' value='" + divisionValue + @"' />
                       
                    </filter>
                  </entity>
                </fetch>";

            List<ActivityDay> temp = activityDayRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            List<ReservationItem> report = new List<ReservationItem>();

            foreach (ActivityDay activityDay in temp)
            {
                ReservationItem reservation = new ReservationItem();
                reservation.Id = activityDay.Reservation.Id;
                reservation.ReservationNum = activityDay.Reservation.ReservationNum;
                reservation.GroupName = activityDay.Reservation.GroupName;
                reservation.Institution = new InstitutionForReservation();
                reservation.Institution.Id = activityDay.Reservation.Institution.Id;
                reservation.Institution.OnlyName = activityDay.Reservation.Institution.Value;
                reservation.ActivityDayList = new List<ActivityDay>();
                reservation.ActivityDayList.Add(activityDay);
                report.Add(reservation);
            }

            return report;
        }

        public List<FrontDeskListItem> GetCommemorationInternalBilling(string userId, List<Filter> filters)
        {
            Filter item = filters.Find(f => f.Field == "startDateTime");
            string date1 = item.Value.Split(',')[0];
            string date2 = item.Value.Split(',')[1];

            string conditions = "<condition attribute='new_start_datetime' operator='on-or-after' value='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date1).ToString("yyyy-MM-dd")
            if (date2.Trim() != "0")
            {
                conditions += "<condition attribute='new_start_datetime' operator='on-or-before' value='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date2).ToString("yyyy-MM-dd")
            }

            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                <entity name='new_activity_day'>
		            <attribute name='new_activity_dayid' />
		            <attribute name='new_start_datetime' />
                    <attribute name='new_activity_day_status' />
                    <attribute name='new_guide_amount_needed' />
                    <attribute name='new_guide_amount_scheduled' />
                    <attribute name='new_lecturer_amount_needed' />
                    <attribute name='new_lecturer_amount_scheduled' />
                    <attribute name='new_testimony_amount_needed' />
                    <attribute name='new_testimony_amount_scheduled' />
                    <attribute name='new_cantor_amount_scheduled' />
                    <attribute name='new_cantor_amount_needed' />
                    <attribute name='new_actor_amount_scheduled' />
                    <attribute name='new_actor_amount_needed' />
		            <order attribute='new_start_datetime' descending='false' />
		            <filter type='and'>" + conditions + @"
                        <condition attribute='new_activity_day_status' operator='ne' value='6' />
                        <condition attribute='new_activity_day_status' operator='ne' value='3' />
                        <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
		            <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation'>
			            <attribute name='new_reservation_status' />
			            <attribute name='new_num' />
                        <attribute name='new_responsible_division' />
                        <attribute name='new_responsible_agent' />
			            <attribute name='new_name' />
			            <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
			             <condition attribute='new_budget_framework' operator='in'>
                          <value>{08E70F6D-BE33-EB11-8109-00505691117E}</value>
                          <value>{8F2AA193-0880-EB11-810B-00505691117E}</value>
                          <value>{183301D5-762D-EB11-8108-00505691117E}</value>
                        </condition>
	
			            <condition attribute='new_division_router' operator='eq' value='2' />
			            <condition attribute='new_reservation_status' operator='ne' value='6' />
                        <condition attribute='new_reservation_status' operator='ne' value='3' />
			            </filter>
		            </link-entity>
                </entity>
            </fetch>";

            //List<ActivityDay> list = activityDayRepository.GetFetchXmlOnly(userId, fetchXml).Item2;
            List<FrontDeskListItem> report = frontDeskRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_event_item'>
                    <attribute name='new_service_provider' />
                    <attribute name='new_yv_product' />
                    <attribute name='new_activity_day' />
                    <attribute name='new_total_price_after_discount' />
                    <order attribute='new_name' descending='false' />
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                      <condition attribute='new_status' operator='eq' value='1' />
                       <filter type='or'>
                            <condition attribute='new_event_type' operator='eq' value='1' /> 
                            <condition attribute='new_event_type' operator='eq' value='4' /> 
                      </filter>" + conditions + @"<condition attribute='new_is_resource' operator='eq' value='0' />
                    </filter>
                  <link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider' link-type='inner'>
                  <attribute name='new_first_name_he' />
                  <attribute name='new_last_name_he' />
                   <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                 </link-entity>
               </entity>
             </fetch>";

            List<FullEventItem> fullEvents = fullEventItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            foreach (FullEventItem fullEventItem in fullEvents)
            {
                fullEventItem.Amount = 1;
            }

            foreach (FrontDeskListItem frontDeskItem in report)
            {
                frontDeskItem.ServiceProviderList = fullEvents.Where(x => x.ActivityDay.Id == frontDeskItem.Id).ToList();
            }

            fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_bid'>
                        <attribute name='new_bidid' />
                        <attribute name='new_name' />
                        <attribute name='createdon' />
                        <attribute name='new_discount' />
                        <attribute name='new_discount_typename' />
                        <attribute name='new_total_price_after_discount' />
                        <attribute name='new_total_price' />
                        <attribute name='new_price' />
                        <attribute name='new_yv_product' />
                        <attribute name='new_amount' />
	                    <attribute name='new_language' />
                        <attribute name='new_activity_day' />
                        <order attribute='new_name' descending='false' />
                        <filter type='and'>
                        <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                        <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ActivityDay'>
                            <attribute name='new_name' />
                            <filter type='and'>" + conditions + @"</filter>
		                    <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation'>
			                    <attribute name='new_name' />
                                <attribute name='new_reservationid' />
                                <filter type='and'>
			                         <condition attribute='new_budget_framework' operator='in'>
                                      <value>{08E70F6D-BE33-EB11-8109-00505691117E}</value>
                                      <value>{8F2AA193-0880-EB11-810B-00505691117E}</value>
                                      <value>{183301D5-762D-EB11-8108-00505691117E}</value>
                                    </condition>
			                        <condition attribute='new_division_router' operator='eq' value='2' />
			                        <condition attribute='new_reservation_status' operator='ne' value='6' />
                                    <condition attribute='new_reservation_status' operator='ne' value='3' />
                                    <condition attribute='statecode' operator='eq' value='0' />
			                    </filter>
		                    </link-entity>
                        </link-entity>
                        <link-entity name='new_yv_product' from='new_yv_productid' to='new_yv_product' alias='Product' link-type='inner'>
                          <filter type='and'>
                            <condition attribute='new_is_required_service_provider' operator='eq' value='0' />
                          </filter>
                        </link-entity>
                    </entity>
                </fetch>";


            List<BidItem> bidList = bidItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            List<FullEventItem> tempBidList = new List<FullEventItem>();
            foreach (BidItem bid in bidList)
            {
                FullEventItem temp = new FullEventItem();
                temp.ActivityDay = bid.ActivityDay;
                temp.Product = bid.Product;
                temp.Amount = bid.Amount;
                temp.PriceAfterDiscount = bid.TotalPriceAfterDiscount;
                tempBidList.Add(temp);
            }

            foreach (FrontDeskListItem frontDeskItem in report)
            {
                //frontDeskItem.BidItem = tempBidList.Where(x => x.ActivityDay.Id == frontDeskItem.Id).ToList();
                frontDeskItem.ServiceProviderList.AddRange(tempBidList.Where(x => x.ActivityDay.Id == frontDeskItem.Id).ToList());
            }

            return report;

        }

        public List<FrontDeskListItem> GetCommemorationPayingAgentsReservations(string userId, List<Filter> filters)
        {
            Filter filter = filters.Find(f => f.Field == "startDateTime");
            string date1 = filter.Value.Split(',')[0];
            string date2 = filter.Value.Split(',')[1];

            string conditions = "<condition attribute='new_start_datetime' operator='on-or-after' value='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date1).ToString("yyyy-MM-dd")
            if (date2.Trim() != "0")
            {
                conditions += "<condition attribute='new_start_datetime' operator='on-or-before' value='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + "'/>"; //DateTime.Parse(date2).ToString("yyyy-MM-dd")
            }

            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='new_activity_day'>
                      <attribute name='new_activity_dayid' />
                      <attribute name='new_start_datetime' />
                      <attribute name='new_division_router' />
                      <attribute name='new_activity_day_status' />
                      <attribute name='new_activity_day_type' />
                      <attribute name='new_number_of_participants' />
	                  <order attribute='new_start_datetime' descending='false' />
	                  <filter type='and'>" + conditions + @"
                     <condition attribute='new_activity_day_status' operator='ne' value='6' />
                        <condition attribute='new_activity_day_status' operator='ne' value='3' />
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
	                  <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation' link-type='inner' >
	                   <attribute name='new_reservationid' />
                    <attribute name='new_name' />
                    <attribute name='new_firstname_contact' />
                    <attribute name='new_lastname_contact' />
                    <attribute name='new_reservation_status' />
                    <attribute name='new_num' />
                    <attribute name='new_rsv_institution' />
                    <attribute name='new_mobile_contact' />
                    <attribute name='new_is_order_confirmed' />
                    <attribute name='new_email_contact' />
                    <attribute name='new_is_sent_email_confirmation' />
                      <filter type='and'>
                        <condition attribute='new_rsv_institution' operator='not-null' />
                        <condition attribute='new_payment_status' operator='eq' value='10' />
		                <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_division_router' operator='eq' value='2' />
			            <condition attribute='new_reservation_status' operator='ne' value='6' />
                        <condition attribute='new_reservation_status' operator='ne' value='3' />
                      </filter>
                    </link-entity>
                  </entity>
                </fetch>";

            List<FrontDeskListItem> report = frontDeskRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='new_bid'>
                    <attribute name='new_yv_product' />
                    <attribute name='new_amount' />
                    <attribute name='new_activity_day' />
                      <filter type='and'>
                      <condition attribute='new_yv_product' operator='eq' uiname='הדרכה קולית' uitype='new_yv_product' value='{3A7AC1B3-2FC0-EA11-80FF-00505691117E}' />
	                  <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                    <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' visible='false' link-type='outer' alias='ActivityDay'>
                      <filter type='and'>" + conditions + @"
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                    </link-entity>
                  </entity>
                </fetch>";

            List<BidItem> bidList = bidItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

            foreach (FrontDeskListItem item in report)
            {
                //item.BidItem = bidList.FindAll(x => x.ActivityDay.Id == item.Id);
                item.NumOfAudioGuides = bidList.FindAll(x => x.ActivityDay.Id == item.Id).Sum(x => Convert.ToInt32(x.Amount));
            }

            return report;


        }

        public List<string> DeactivateLooseEventItems(string userId)
        {
            List<string> list = new List<string>();
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_event_item'>
                    <attribute name='new_name' />
                    <filter type='and'>
                    <condition attribute='new_status' operator='eq' value='1' />
                    <condition attribute='statecode' operator='eq' value='0' />
                      <condition attribute='modifiedon' operator='on-or-after' value='2021-05-01'/>
                     <condition attribute = 'modifiedon' operator= 'on-or-before' value = '2021-05-19' />
                   </filter>
               </entity>
             </fetch>";
            /* <filter type='or'>
                            <condition attribute='new_event_type' operator='eq' value='1' /> שיבוץ
                            <condition attribute='new_event_type' operator='eq' value='4' /> 
                      </filter>*/
            //<condition attribute='statecode' operator='eq' value='0' />
            //<condition attribute='modifiedon' operator='on-or-after' value='2021-05-18'/>
            //< condition attribute = 'modifiedon' operator= 'on-or-before' value = '2021-05-18' />
            var query = new FetchExpression(fetchXml);
            EntityCollection results = crmConnection.RetrieveMultiple(query, userId);

            foreach (Entity item in results.Entities)
            {
                fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_itinerary_item'>
                    <attribute name='new_name' />
                    <attribute name='new_itinerary_itemid' />
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                      <filter type='or'>
                           <condition attribute='new_scheduler_service_provider' operator='eq' value='{" + item.Id + @"}' />
                           <condition attribute='new_scheduler_resource' operator='eq' value='{" + item.Id + @"}' />
                    </filter>
                </filter>
               </entity>
             </fetch>";

                List<ItineraryItem> itineraryItems = itineraryItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

                fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' mapping = 'logical' distinct = 'false'>
                    <entity name='new_itinerary_group'>
                    <attribute name='new_name' />
                    <attribute name='new_itinerary_groupid' />
                    <filter type='and'>
                      <condition attribute='statecode' operator='eq' value='0' />
                      <filter type='or'>
                           <condition attribute='new_room_schedule_event' operator='eq' value='{" + item.Id + @"}' />
                           <condition attribute='new_guide_schedule_event' operator='eq' value='{" + item.Id + @"}' />
                    </filter>
                </filter>
               </entity>
             </fetch>";

                List<ItineraryGroup> itineraryGroups = itineraryGroupRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

                if (itineraryItems.Count == 0 && itineraryGroups.Count == 0)
                {
                    //deactivate item
                    list.Add(item.Id.ToString());
                    //item["statecode"] = new OptionSetValue(1);
                    //crmConnection.Update(item, userId);
                }

            }

            return list;

        }

        public List<SemiAnnualItem> SemiAnnual(string userId, List<Filter> filters)
        {
            Filter filter = filters.Find(f => f.Field == "startDateTime");
            string date1 = filter.Value.Split(',')[0];
            string date2 = filter.Value.Split(',')[1];

            string divisionRouter = "2";
            Filter division = filters.Find(f => f.Field == "divisionRouter");
            if (division != null)
            {
                divisionRouter = division.Value;
            }

            /*   List<SemiAnnualItem> list = new List<SemiAnnualItem>();

               //get all reservations:
               string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                   <entity name='new_activity_day' alias='ActivityDay'>
                   <attribute name='new_start_datetime' alias='month' groupby='true' dategrouping='month' />
                   <attribute name='new_start_datetime' alias='year' groupby='true' dategrouping='year' />
                   <attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
                   <filter type='and'>
                   <condition attribute='new_start_datetime' operator= 'on-or-after' value ='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
               if(date2.Trim() != "0")
               {
                   fetchXml += "<condition attribute='new_start_datetime' operator= 'on-or-before' value ='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
               }

               fetchXml += @"<condition attribute='statecode' operator='eq' value='0' />
                   <condition attribute='new_reservation' operator= 'not-null'/>
                   <condition attribute='new_activity_day_status' operator='not-in'>
                   <value>6</value>
                   <value>3</value>
                   </condition>";

               if (divisionRouter != null && divisionRouter != "")
               {
                   fetchXml += "<condition attribute = 'new_division_router' operator= 'eq' value = '" + divisionRouter + "' />";
               }

               fetchXml += @"</filter>
                   <order alias='year'/>
                   <order alias='month'/>
                   <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'>
                   <filter type='and'>
                   <condition attribute='statecode' operator='eq' value='0' />
                   <condition attribute='new_reservation_status' operator='not-in'>
                   <value>6</value>
                   <value>3</value>
                   </condition>
                   </filter>
                   </link-entity>
                   </entity>
                   </fetch>";

               List<ActivityDayGrouped> allActivityDayList = activityDayGroupedRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

               //get cancelled activity days:
               fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                   <entity name='new_activity_day' alias='ActivityDay'>
                   <attribute name='new_start_datetime' alias='month' groupby='true' dategrouping='month' />
                   <attribute name='new_start_datetime' alias='year' groupby='true' dategrouping='year' />
                   <attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
                   <filter type='and'>
                   <condition attribute='new_start_datetime' operator= 'on-or-after' value ='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd")  + @"' />";
               if(date2.Trim() != "0")
               {
                   fetchXml += "<condition attribute='new_start_datetime' operator= 'on-or-before' value ='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
               }

               fetchXml += @"<condition attribute='statecode' operator='eq' value='0' />
                   <condition attribute='new_reservation' operator= 'not-null'/>
                   <condition attribute='new_activity_day_status' operator='eq' value='3'>
                   </condition>";

               if (divisionRouter != null && divisionRouter != "")
               {
                   fetchXml += "<condition attribute = 'new_division_router' operator= 'eq' value = '" + divisionRouter + "' />";
               }

               fetchXml += @"</filter>
                   <order alias='year'/>
                   <order alias='month'/>
                   <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'>
                   <filter type='and'>
                   <condition attribute='statecode' operator='eq' value='0' />
                   <condition attribute='new_reservation_status' operator='eq' value ='3'/>
                   </filter>
                   </link-entity>
                   </entity>
                   </fetch>";

            //   List<SemiAnnualItem> cancelledActivityDayList = semiAnnualItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;

               //get cancelled activity days:
               fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                   <entity name='new_activity_day' alias='ActivityDay'>
                   <attribute name='new_start_datetime' alias='month' groupby='true' dategrouping='month' />
                   <attribute name='new_start_datetime' alias='year' groupby='true' dategrouping='year' />
                   <filter type='and'>
                   <condition attribute='new_start_datetime' operator= 'on-or-after' value ='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
               if(date2.Trim() != "0")
               {
                   fetchXml += "<condition attribute='new_start_datetime' operator= 'on-or-before' value ='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
               }

               fetchXml += @"<condition attribute='statecode' operator='eq' value='0' />
                   <condition attribute='new_reservation' operator= 'not-null'/>
                   </condition>";

               if (divisionRouter != null && divisionRouter != "")
               {
                   fetchXml += "<condition attribute = 'new_division_router' operator= 'eq' value = '" + divisionRouter + "' />";
               }

               fetchXml += @"</filter>
                   <order alias='year'/>
                   <order alias='month'/>
                   <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'>
                   <attribute name= 'new_reservationid' alias = 'count' aggregate = 'count' />
                   <filter type='and'>
                   <condition attribute='statecode' operator='eq' value='0' />
                   <condition attribute='new_reservation_status' operator='eq' value ='3'/>
                   </filter>
                   </link-entity>
                   </entity>
                   </fetch>";

           //    List<SemiAnnualItem> cancelledReservationList = semiAnnualItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;
           */
            string audioGuideProductGuid = ConfigurationManager.AppSettings[$"audioGuideProductId_{environment}"];
            string externalGuideProductGuid = ConfigurationManager.AppSettings[$"externalGuideProductId_{environment}"];

            //get all reservations:
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' aggregate='true'>
              <entity name='new_bid'>
                  <filter type='and'>
                    <condition attribute='new_yv_product' operator='eq' value='{" + audioGuideProductGuid + @"}' />
                  </filter>
                  <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ActivityDay'>
                    <attribute name='new_start_datetime' alias='month' groupby='true' dategrouping='month' />
                     <attribute name='new_start_datetime' alias='year' groupby='true' dategrouping='year' />
                    <attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
                    <filter type='and'>
                      <condition attribute='new_start_datetime' operator= 'on-or-after' value ='" + DateTime.ParseExact(date1.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            if (date2.Trim() != "0")
            {
                fetchXml += "<condition attribute='new_start_datetime' operator= 'on-or-before' value ='" + DateTime.ParseExact(date2.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            }
            fetchXml += @"<condition attribute='new_activity_day_status' operator='not-in'>
                      <value>6</value>
                        <value>3</value>
                      </condition>
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                    <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation'>
                      <filter type='and'>
                        <condition attribute='statecode' operator='eq' value='0' />
                        <condition attribute='new_reservation_status' operator='not-in'>
                          <value>6</value>
                          <value>3</value>
                        </condition>
                      </filter>
                    </link-entity>
                  </link-entity>
              </entity>
            </fetch>";

            //      List<SemiAnnualItem> audioGuideActivityDayList = semiAnnualItemRepository.GetFetchXmlOnly(userId, fetchXml).Item2;
            List<IdValuePair> kvList = new List<IdValuePair>();

            var query = new FetchExpression(fetchXml);
            EntityCollection results = crmConnection.RetrieveMultiple(query, userId);

            foreach (Entity entity in results.Entities)
            {
                foreach (KeyValuePair<String, Object> attribute in entity.Attributes)
                {
                    //tring value = GetValueByAttributeName(attribute.Value);
                    //kvList.Add(new { Id = attribute.Key, Value= value });
                    Console.WriteLine(attribute.Key + ": " + attribute.Value);

                }
            }
            //get cancelled reservations:


            return null;
        }

        public FileItem ExportToFile(List<Filter> filterList)
        {
            FileItem fileItem = new FileItem();
            var environment = ConfigurationManager.AppSettings["environment"].ToString();
            string reportsFolder = ConfigurationManager.AppSettings[$"reportsFolder_{environment}"].ToString();

            string fileEnding = "";

            string renderingExtension = filterList.Find(x => x.Field == "renderingExtension").Value;

            //RenderingExtension: XML, CSV, PDF, IMAGE, EXCEL, WORD
            switch (renderingExtension)
            {
                case "EXCEL":
                    fileEnding += "xls";
                    break;
                case "PDF":
                    fileEnding += "pdf";
                    break;
                case "WORD":
                    fileEnding += "doc";
                    break;
                default:
                    break;
            }

            string reportName = filterList.Find(x => x.Field == "reportName").Value;
            ReportExecutionService rs = new ReportExecutionService();
            rs.Credentials = CredentialCache.DefaultCredentials;
           
            rs.UseDefaultCredentials = false;
            string crmDevUserName = ConfigurationManager.AppSettings["crmDevUserName"].ToString();
            string crmDevPassword = ConfigurationManager.AppSettings["crmDevPassword"].ToString();
            rs.Credentials = new System.Net.NetworkCredential(crmDevUserName, crmDevPassword);

            rs.Url = "http://sqlcrm2016/reportserver/reportexecution2005.asmx";

            rs.ExecutionHeaderValue = new ExecutionHeader();
            rs.Timeout = 6000000;
            var executionInfo = new ExecutionInfo();

            executionInfo = rs.LoadReport("/yadvashem2016_MSCRM/" + reportName, null);

            List<ParameterValue> parameters = new List<ParameterValue>();
            List<Filter> newFilterList = filterList.FindAll(x => x.Field != "reportName" && x.Field != "renderingExtension" && x.Field != "prinDirection");

            //new
            /*
            foreach (Filter filter in newFilterList)
            {
                parameters.Add(new ParameterValue { Name = filter.Field, Value = filter.Value });
            }
            */
            //old
            foreach (Filter filter in newFilterList)
            {
                parameters.Add(new ParameterValue { Name = queryHelperRepository.FirstCharToUpper(filter.Field), Value = filter.Value });
            }

            rs.SetExecutionParameters(parameters.ToArray(), "en-US");
            string deviceInfo;
            //string deviceInfo = "<DeviceInfo><Toolbar>False</Toolbar></DeviceInfo>";
            if (filterList.Find(x => x.Field == "prinDirection")?.Value == "portrait")
                deviceInfo = "<DeviceInfo><Toolbar>False</Toolbar><PageHeight>29.7cm</PageHeight><PageWidth>21cm</PageWidth></DeviceInfo>";
            else
                deviceInfo = "<DeviceInfo><Toolbar>False</Toolbar><PageHeight>21cm</PageHeight><PageWidth>29.7cm</PageWidth></DeviceInfo>";

            string mimeType;
            string encoding;
            string[] streamId;
            Warning[] warning;

            var result = rs.Render(renderingExtension, deviceInfo, out mimeType, out encoding, out encoding, out warning, out streamId);

            //adina-if need blob use here  

            /* if (!Directory.Exists(@"{reportsFolder}")) {
                 Directory.CreateDirectory(@"{reportsFolder}");
             }*/

            string fileName = $@"{reportsFolder}\{reportName}_{DateTime.Now.ToString("yyyy-MM-dd")}.{fileEnding}";

            string username = ConfigurationManager.AppSettings["username"].ToString();
            string password = ConfigurationManager.AppSettings["password"].ToString();
            string domain = ConfigurationManager.AppSettings["domain"].ToString();

            using (Impersonator wi = new Impersonator(username, domain, password))
            {
                Trace.TraceError($"{DateTime.Now}: in imp");
                WindowsIdentity.RunImpersonated(wi.Identity.AccessToken, () =>
                {


                    try
                    {
                        int i = 0;
                        while (File.Exists(fileName))
                        {
                            i++;
                            //fileName = @"c:\reports\" + reportName + "(" + i + ")" + fileEnding;
                            fileName = $@"{reportsFolder}\{reportName}_{DateTime.Now.ToString("yyyy-MM-dd")}({i}).{fileEnding}";
                        }

                        Trace.TraceError($"{DateTime.Now}: going to Create {fileName}");
                        File.Create(fileName).Dispose();
                        File.WriteAllBytes(fileName, result);

                        fileItem.Extention = fileEnding;
                        //fileItem.FullPath = fileName;
                        //fileItem.Name = 
                        fileItem.Name = Path.GetFileName(fileName);


                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"{DateTime.Now}: error, exception: {ex.Message}");
                    }
                });
            }




            return fileItem;


        }

        public FileItem GetAllFileDetails(List<Filter> filterList)
        {
            FileItem fileItem = new FileItem();
            var environment = ConfigurationManager.AppSettings["environment"].ToString();
            string reportsFolder = ConfigurationManager.AppSettings[$"reportsFolder_{environment}"].ToString();

            string fileEnding = "";

            string renderingExtension = filterList.Find(x => x.Field == "renderingExtension").Value;

            //RenderingExtension: XML, CSV, PDF, IMAGE, EXCEL, WORD
            switch (renderingExtension)
            {
                case "EXCEL":
                    fileEnding += "xls";
                    break;
                case "PDF":
                    fileEnding += "pdf";
                    break;
                default:
                    break;
            }

            string reportName = filterList.Find(x => x.Field == "reportName").Value;
            ReportExecutionService rs = new ReportExecutionService();
            rs.Credentials = CredentialCache.DefaultCredentials;

            rs.UseDefaultCredentials = false;
            string crmDevUserName = ConfigurationManager.AppSettings["crmDevUserName"].ToString();
            string crmDevPassword = ConfigurationManager.AppSettings["crmDevPassword"].ToString();
            rs.Credentials = new System.Net.NetworkCredential(crmDevUserName, crmDevPassword);

            rs.Url = "http://sqlcrm2016/reportserver/reportexecution2005.asmx";

            rs.ExecutionHeaderValue = new ExecutionHeader();
            var executionInfo = new ExecutionInfo();

            executionInfo = rs.LoadReport("/yadvashem2016_MSCRM/" + reportName, null);

            List<ParameterValue> parameters = new List<ParameterValue>();
            List<Filter> newFilterList = filterList.FindAll(x => x.Field != "reportName" && x.Field != "renderingExtension");

            //כאן ניגשת לCRM
            //מביאה רשימה של GUID שעונים על הפילטור

            //foreach:


            foreach (Filter filter in newFilterList)
            {
                parameters.Add(new ParameterValue { Name = queryHelperRepository.FirstCharToUpper(filter.Field), Value = filter.Value });
            }

            rs.SetExecutionParameters(parameters.ToArray(), "en-US");

            //string deviceInfo = "<DeviceInfo><Toolbar>False</Toolbar></DeviceInfo>";
            string deviceInfo = "<DeviceInfo><Toolbar>False</Toolbar><PageHeight>21cm</PageHeight><PageWidth>29.7cm</PageWidth></DeviceInfo>";

            string mimeType;
            string encoding;
            string[] streamId;
            Warning[] warning;

            var result = rs.Render(renderingExtension, deviceInfo, out mimeType, out encoding, out encoding, out warning, out streamId);

            //result לשרשר לקובץ
            //fetch attachments using guid
            //לשרשר את הקבצים מATTACHMENT File.OpenRead()

            //foreach until here


            //adina-if need blob use here  

            /* if (!Directory.Exists(@"{reportsFolder}")) {
                 Directory.CreateDirectory(@"{reportsFolder}");
             }*/

            string fileName = $@"{reportsFolder}\{reportName}_{DateTime.Now.ToString("yyyy-MM-dd")}.{fileEnding}";

            string username = ConfigurationManager.AppSettings["username"].ToString();
            string password = ConfigurationManager.AppSettings["password"].ToString();
            string domain = ConfigurationManager.AppSettings["domain"].ToString();

            using (Impersonator wi = new Impersonator(username, domain, password))
            {
                Trace.TraceError($"{DateTime.Now}: in imp");
                WindowsIdentity.RunImpersonated(wi.Identity.AccessToken, () =>
                {


                    try
                    {
                        int i = 0;
                        while (File.Exists(fileName))
                        {
                            i++;
                            //filename = @"c:\reports\" + reportName + "(" + i + ")" + fileEnding;
                            fileName = $@"{reportsFolder}\{reportName}_{DateTime.Now.ToString("yyyy-MM-dd")}({i}).{fileEnding}";
                        }

                        Trace.TraceError($"{DateTime.Now}: going to Create {fileName}");
                        File.Create(fileName).Dispose();
                        File.WriteAllBytes(fileName, result);

                        fileItem.Extention = fileEnding;
                        //fileItem.FullPath = fileName;
                        //fileItem.Name = 
                        fileItem.Name = Path.GetFileName(fileName);


                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"{DateTime.Now}: error, exception: {ex.Message}");
                    }
                });
            }




            return fileItem;


        }

        public FileItem GetUnrelatedCombinedPdfFile(List<Filter> filterList, string userId)
        {
            string filterXml = "";
            List<Filter> newFilterList = filterList.FindAll(x => x.Field != "reportName" && x.Field != "renderingExtension");
            Filter yearFilter = filterList.Find(x => x.Field == "year");
            if (yearFilter != null)
            {
                filterXml += "<condition attribute='new_year' operator='eq' value='" + yearFilter.Value + "' />";
            }
            string fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                  <entity name='new_maya_application'>
                    <attribute name='new_maya_applicationid' />
                    <filter type='and'>" + filterXml +
                        @"<condition attribute='statecode' operator= 'eq' value = '0'/>
                        </filter>
                  </entity>
                </fetch>";

            var query = new FetchExpression(fetchXml);
            EntityCollection results = crmConnection.RetrieveMultiple(query, userId);
            List<string> guidList = new List<string>();
            FileItem fileItem = new FileItem();

            foreach (Entity entity in results.Entities)
            {
                guidList.Add(entity.Attributes.Values.First().ToString());
            }
            if (guidList.Count() == 0)
            {
                return fileItem;
            }
            var environment = ConfigurationManager.AppSettings["environment"].ToString();
            string reportsFolder = ConfigurationManager.AppSettings[$"reportsFolder_{environment}"].ToString();

            string fileEnding = "pdf";

            string renderingExtension = "PDF";// filterList.Find(x => x.Field == "renderingExtension").Value;

            string reportName = filterList.Find(x => x.Field == "reportName").Value;
            ReportExecutionService rs = new ReportExecutionService();
            rs.Credentials = CredentialCache.DefaultCredentials;

            rs.UseDefaultCredentials = false;
            string crmDevUserName = ConfigurationManager.AppSettings["crmDevUserName"].ToString();
            string crmDevPassword = ConfigurationManager.AppSettings["crmDevPassword"].ToString();
            rs.Credentials = new System.Net.NetworkCredential(crmDevUserName, crmDevPassword);

            rs.Url = "http://sqlcrm2016/reportserver/reportexecution2005.asmx";

            rs.ExecutionHeaderValue = new ExecutionHeader();
            var executionInfo = new ExecutionInfo();

            executionInfo = rs.LoadReport("/yadvashem2016_MSCRM/" + reportName, null);

            List<string> pagesList = new List<string>();
            List<ParameterValue> parameters = new List<ParameterValue>();
            //foreach (Filter filter in newFilterList)
            //{
            //    parameters.Add(new ParameterValue { Name = queryHelperRepository.FirstCharToUpper(filter.Field), Value = filter.Value });
            //}
            parameters.Add(new ParameterValue { Name = "Guid", Value = null });

            string deviceInfo = "<DeviceInfo><Toolbar>False</Toolbar></DeviceInfo>";
            string mimeType;
            string encoding;
            string[] streamId;
            Warning[] warning;
            int i = 0;
            if (reportName.Contains("GUID"))
            {
                reportName = reportName.Split('G')[0];
            }
            // while (File.Exists($@"{reportsFolder}\{reportName}_{DateTime.Now.ToString("yyyy-MM-dd")}({i++})")) ;

            string fileName = $@"{reportsFolder}\{reportName}_{DateTime.Now.ToString("yyyy-MM-dd")}";
            string username = ConfigurationManager.AppSettings["username"].ToString();
            string password = ConfigurationManager.AppSettings["password"].ToString();
            string domain = ConfigurationManager.AppSettings["domain"].ToString();
            Trace.TraceInformation($"{DateTime.Now}: ---------------------going to Create {fileName}----------------");
            WebClient webClient = new WebClient();


            foreach (string guid in guidList)
            {
                if (parameters[parameters.Count - 1].Name == "Guid")
                {
                    parameters[parameters.Count - 1].Value = guid;
                }
                filterXml = "<condition attribute='new_maya_application_attachment' operator='eq' value='" + guid + "' />";
                fetchXml = @"
                <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                  <entity name='new_attachment'>
                    <attribute name='new_name' />
                    <filter type='and'>" + filterXml +
                @"<condition attribute='statecode' operator= 'eq' value = '0'/>
                </filter>
                  </entity>
                </fetch>";

                query = new FetchExpression(fetchXml);
                results = crmConnection.RetrieveMultiple(query, userId);
                List<string> linkList = new List<string>();
                foreach (Entity entity in results.Entities)
                {
                    if (entity.Attributes.Count != 0)
                    {
                        linkList.Add(entity.Attributes.Values.First().ToString());
                    }
                }
                rs.SetExecutionParameters(parameters.ToArray(), "en-US");
                var result = rs.Render(renderingExtension, deviceInfo, out mimeType, out encoding, out encoding, out warning, out streamId);
                using (Impersonator wi = new Impersonator(username, domain, password))
                {
                    Trace.TraceInformation($"{DateTime.Now}: running maya application report for:{guid}");
                    WindowsIdentity.RunImpersonated(wi.Identity.AccessToken, () =>
                    {
                        try
                        {
                            while (File.Exists($"{fileName}({i}).pdf")) { i++; };
                            string name = $"{fileName}({i}).{fileEnding}";
                          //  Trace.TraceInformation($"{DateTime.Now}:saving report for guid:{name}");
                            File.WriteAllBytes(name, result);
                            pagesList.Add(name);
                            //check if file exists
                        //    Trace.TraceInformation($"{DateTime.Now}: number of attachments for current guid:{linkList.Count}");

                            foreach (string link in linkList)
                            {
                                try
                                {
                                    fileEnding = "pdf";
                                 //   Trace.TraceInformation($"{DateTime.Now}:attachment link:{link}");

                                    if (string.IsNullOrEmpty(link))
                                    {
                                        continue;
                                    }
                                    if (!link.ToLower().EndsWith("pdf"))
                                    {
                                        if (link.ToLower().EndsWith("docx"))
                                        {
                                            fileEnding = "docx";
                                        }
                                        else if (link.ToLower().EndsWith("doc"))
                                        {
                                            fileEnding = "doc";
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                        while (File.Exists($"{fileName}({i}).{fileEnding}")) { i++; };
                                        name = $"{fileName}({i}).{fileEnding}";
                                        File.WriteAllBytes(name, webClient.DownloadData(link));
                                        var word = Type.GetTypeFromProgID("word.application");
                                        dynamic app = Activator.CreateInstance(word);
                                        while (File.Exists($"{fileName}({i}).pdf")) { i++; };
                                        var outPath = $"{fileName}({i}).pdf";
                                        dynamic doc = app.Documents.Open(name);
                                        doc.ExportAsFixedFormat(outPath,
                                                ExportFormat: 17/*pdf*/);
                                        doc.Close(0/*DoNotSaveChanges*/);
                                        name = outPath;
                                    }
                                    else
                                    {
                                        while (File.Exists($"{fileName}({i}).{fileEnding}")) { i++; };
                                        name = $"{fileName}({i}).{fileEnding}";
                                        File.WriteAllBytes(name, webClient.DownloadData(link));
                                    }
                                    pagesList.Add(name);
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError($"{DateTime.Now}: error, exception: failed while creating file: {name} ,ex.Message: {ex.Message}");
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError($"{DateTime.Now}: error, exception: {ex.Message}");
                        }
                    });
                }
            }
            fileName = Path.GetFileName(fileName);
            var file = CombineFiles(fileName, pagesList);
            if (file != null)
            {
                fileItem.Extention = file.Extention;
                fileItem.Name = file.Name;
                fileItem.FullPath = file.FullPath;
            }
            return fileItem;

        }

        public FileItem CombineFiles(string fileName, List<string> pagesList)
        {

            string json = JsonConvert.SerializeObject(new { reportsPathList = pagesList, fileName = fileName });
            var environment = ConfigurationManager.AppSettings["environment"].ToString();
            string pdfCreatorUrl = ConfigurationManager.AppSettings[$"baseUrlPdfCreator_{environment}"].ToString();
            string jsonResponse = CreateRequest($"{pdfCreatorUrl}/Report", "POST", json);
            FileItem fileItem = JsonConvert.DeserializeObject<FileItem>(jsonResponse);
            fileItem.Extention = "pdf";
            return fileItem;
        }
        public FileItem CreateScheduleWordReport(object data) {
            string json = JsonConvert.SerializeObject(data);
            var environment = ConfigurationManager.AppSettings["environment"].ToString();
            string pdfCreatorUrl = ConfigurationManager.AppSettings[$"baseUrlPdfCreator_{environment}"].ToString();
            string jsonResponse = CreateRequest($"{pdfCreatorUrl}/Schedule", "POST", json);
            FileItem fileItem = JsonConvert.DeserializeObject<FileItem>(jsonResponse);
            fileItem.Extention = "docx";
            return fileItem;
        }

        public string CreateRequest(string url, string metod, string data)
        {

            string response;
            StringBuilder sbUrl = new StringBuilder(url);
            HttpWebRequest omniRequest = (HttpWebRequest)WebRequest.Create(sbUrl.ToString());
            omniRequest.Method = metod;
            omniRequest.ContentType = "application/json";

            //Write the json details to the request
            using (var streamWriter = new StreamWriter(omniRequest.GetRequestStream()))
            {
                string json = data;
                streamWriter.Write(json);
            }

            //Get the response of the request
            try
            {
                using (HttpWebResponse statusResponse = (HttpWebResponse)omniRequest.GetResponse())
                {
                    using (Stream receiveStream = statusResponse.GetResponseStream())
                    {
                        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                        {
                            response = readStream.ReadToEnd();
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
        }

        public bool UploadExcelForNewActivityDays(string excelFileUrl, string userId)
        {

            List<ExcelOutput> days;
            try
            {
                days = ReadWrite.ReadExcelData(new List<string>() { "year", "month", "day", "visitors" }, excelFileUrl, ConfigurationManager.AppSettings["relevantExcelWorksheet"]);

                if (days == null || days.Count == 0)
                {
                    Trace.TraceError("No activityDays found in the excel file " + excelFileUrl);
                    return false;
                }
            }catch (Exception e)
            {
                Trace.TraceError("Getting Exception while reading from " + excelFileUrl);
                Trace.TraceError(e.Message);
                return false;
            }
                

            int cnt = 0;
            string env = ConfigurationManager.AppSettings["environment"]; 
            string bitan27Guid = ConfigurationManager.AppSettings["bitan27Guid_"+env];
            string bitan27ParentReservation = ConfigurationManager.AppSettings["bitan27ParentReservation_"+env];

            // get last activityDay in beitan27
            DateTime lastDate = DateTime.MinValue;
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' aggregate='true'>
                                <entity name='new_activity_day'>
                                <attribute name='new_start_datetime' alias='startDate' aggregate='max' />
                                <filter type='and'>
                                <condition attribute='statecode' operator='eq' value='0' />
                                <condition attribute='new_activity_day_location' operator='eq' value='{"+ bitan27Guid + @"}' />
                                </filter>
                                </entity>
                                </fetch>";
            var top = activityDayRepository.GeneralFetchXml(userId, fetchXml).First();
            if (top == null)
            {
                Trace.WriteLine(DateTime.Today + " : Beitan27 - Failed to fetch the latest activity day");
            }
            else
            {

                try
                {
                    object dt;
                    if (top.Attributes.TryGetValue("startDate", out dt))
                    {
                        Microsoft.Xrm.Sdk.AliasedValue value = (AliasedValue)top.Attributes.First(x => x.Key == "startDate").Value;

                        lastDate = Convert.ToDateTime(value.Value.ToString());
                    }
                }
                catch { lastDate = DateTime.MinValue; }
            }

            if (lastDate == DateTime.MinValue)
                Trace.WriteLine(DateTime.Today + " : Beitan27 - loading all data from " + excelFileUrl);
            else
                Trace.WriteLine(DateTime.Today + " : Beitan27 - loading activity days after " + lastDate + " from " + excelFileUrl);

            //ActivityDay lastDay = activityDayRepository
            foreach (ExcelOutput day in days)
            {
                cnt++;

                
                var theDate = day.GetDateOrNull();

                //invalid data
                if (theDate == DateTime.MinValue || day.visitors < 1)
                {
                    Trace.TraceError($"Beitan27 - Invalid input data in line {cnt} : Year: {day.year} Month: {day.month} Day: {day.day} Visitors: {day.visitors}");
                    continue;
                }

                //irrelevant data - activity day already exists
                if (theDate < lastDate)
                    continue;

                //create activity day
                ActivityDay data = new ActivityDay();
                data.StartDateTime = (theDate.Date +new TimeSpan(8, 0, 0)).ToString("dd/MM/yyyy HH:mm:ss");
                data.EndDateTime = (theDate.Date + new TimeSpan(20, 0, 0)).ToString("dd/MM/yyyy HH:mm:ss");
                data.NumberOfVisitors = day.visitors;
                data.ActualNumberOfVisitors = day.visitors;
                data.Location = new IdValuePair(bitan27Guid, "");
                data.Reservation = new Reservation() { Id = bitan27ParentReservation };

                data.NumberOfGroups = 1;
                data.Division = 7;
                data.ActivityDayStatus = new IdValuePair("2", "");
                data.ReservationStatus = new IdValuePair("2", "");
                data.Name = "";

                try
                {
                    activityDayRepository.Create(data, userId);
                }catch (Exception ex)
                {
                    Trace.WriteLine($"Beitan27 - Activity Day Creation Failed: {cnt} : Year: {day.year} Month: {day.month} Day: {day.day} Visitors: {day.visitors}" +
    $" {data.StartDateTime} {data.EndDateTime} {data.NumberOfVisitors} {data.ActualNumberOfVisitors} {data.Location} {data.Reservation}" +
    $"  ; Exception message: {ex.Message}");

                }
            }
            return true;
        }

    }
}
