using AutoMapper;
using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class ActivityDayService : BasicService<ActivityDay, ActivityDayListItem, ActivityDayCRM>, IActivityDayService
    {
        private readonly Lists lists = new Lists();


        private readonly IActivityDaySubjectRepository activityDaySubjectRepository;
        private readonly IdValueListService idValueListService;
        private readonly IActivityDayGroupedRepository activityDayGroupedRepository;
        private readonly IActivityDayRepository activityDayRepository;
        private readonly IActivityDayListRepository activityDayListRepository;
        private readonly IReservationsRepository reservationsRepository;
        private readonly IInstitutionsRepository institutionsRepository;
        private readonly IBudgetFrameworkRepository budgetFrameworkRepository;
        private readonly IBidItemRepository bidItemRepository;
        private readonly IEventItemRepository eventItemRepository;
        private readonly IFullEventItemRepository fullEventItemRepository;
        private readonly IItineraryGroupRepository itineraryGroupRepository;
        private IScheduleEventsHelperService scheduleEventsHelperService;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IEventReservationRepository eventReservationRepository;
        private readonly IEventRegistrationRepository eventRegistrationRepository;
        private readonly IProductRepository productRepository;
        private readonly IActivityDayTypeRepository activityDayTypeRepository;
        private readonly string environment;
        public ActivityDayService(IActivityDaySubjectRepository activityDaySubjectRepository,
            IdValueListService idValueListService,
            IActivityDayGroupedRepository activityDayGroupedRepository,
            IHelperService helperService,
            IBidItemRepository bidItemRepository,
            IActivityDayListRepository activityDayListRepository,
            IActivityDayRepository activityDayRepository,
            IReservationsRepository reservationsRepository,
            IInstitutionsRepository institutionsRepository,
            IScheduleEventsHelperService scheduleEventsHelperService,
            IBudgetFrameworkRepository budgetFrameworkRepository,
            IMapFunctionRepository mapFunctionRepository,
            IEventItemRepository eventItemRepository,
            IFullEventItemRepository fullEventItemRepository,
            IItineraryGroupRepository itineraryGroupRepository,
            IEventReservationRepository eventReservationRepository,
            IEventRegistrationRepository eventRegistrationRepository,
            IActivityDayTypeRepository activityDayTypeRepository,
            IProductRepository productRepository)
            : base(helperService, activityDayListRepository, mapFunctionRepository)
        {
            this.activityDaySubjectRepository = activityDaySubjectRepository;
            this.idValueListService = idValueListService;
            this.activityDayGroupedRepository = activityDayGroupedRepository;
            this.bidItemRepository = bidItemRepository;
            this.activityDayRepository = activityDayRepository;
            this.activityDayTypeRepository = activityDayTypeRepository;
            this.activityDayListRepository = activityDayListRepository;
            this.reservationsRepository = reservationsRepository;
            this.institutionsRepository = institutionsRepository;
            this.budgetFrameworkRepository = budgetFrameworkRepository;
            this.scheduleEventsHelperService = scheduleEventsHelperService;
            this.mapFunctionRepository = mapFunctionRepository;
            this.eventItemRepository = eventItemRepository;
            this.fullEventItemRepository = fullEventItemRepository;
            this.itineraryGroupRepository = itineraryGroupRepository;
            this.eventReservationRepository = eventReservationRepository;
            this.productRepository = productRepository;
            this.eventRegistrationRepository = eventRegistrationRepository;
            environment = ConfigurationManager.AppSettings["environment"];
        }

        public List<ItineraryGroupForActivityDay> GetItineraryGroupListByReservation(string userId, string reservationGuid)
        {
            List<ItineraryGroupForActivityDay> resultList = new List<ItineraryGroupForActivityDay>();
            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter()
            {
                Alias = "Reservation",
                Field = "Id",
                Operator = "eq",
                Type = "lookup",
                Value = reservationGuid
            });
            filters = helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filters);

            string fetchXml = @"<all-attributes/>";
            fetchXml += reservationsRepository.GetActivityDayLinkedEntity(filters, null);

            List<ItineraryGroup> itineraryGroupList = itineraryGroupRepository.GetFetchXml(userId, false, null, null, null, fetchXml).Item2;


            var results = (from itineraryGroup in itineraryGroupList
                           group itineraryGroup by itineraryGroup.ActivityDay.Id into g
                           select new ItineraryGroupForActivityDay(g.Key, g.ToList())).ToList();

            results.ForEach(activityDay =>
            {
                SetGroupNumberList(activityDay.ItineraryGroupList);
            });
            return results;
        }

        private void SetGroupNumberList(List<ItineraryGroup> items)
        {
            var list = items.Select((itineraryGroup, position) =>
            {
                return itineraryGroup.SchedulerItemList.Select(itineraryItem =>
                {
                    return new KeyValuePair<ItineraryItem, int>(itineraryItem, itineraryGroup.GroupNumber);
                });
            })
            .SelectMany(i => i);

            var groupList = list.GroupBy(i => i.Key.Id);
            groupList.ToList()
            .ForEach(group =>
            {
                var groupNumberList = group.Select(i => i.Value).ToList();
                group.ToList().ForEach(i => i.Key.GroupNumberList = groupNumberList);
                //group.Key.GroupNumberList = group.Select(i => i.Value).ToList();
                // new KeyValuePair<string, List<int>>(group.Key, group.Select(i => i.Value).ToList());
            });


        }

        public List<BidListItemForActivityDay> GetBidListByReservation(string userId, string reservationGuid)
        {
            List<BidListItemForActivityDay> resultList = new List<BidListItemForActivityDay>();
            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter()
            {
                Alias = "Reservation",
                Field = "Id",
                Operator = "eq",
                Type = "lookup",
                Value = reservationGuid
            });
            filters = helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filters);

            string fetchXml = @"<all-attributes/>";
            fetchXml += reservationsRepository.GetActivityDayLinkedEntity(filters, null);

            List<BidItem> bidItemList = bidItemRepository.GetFetchXml(userId, false, null, null, null, fetchXml).Item2;

            var results = (from bidItem in bidItemList
                           group bidItem by "יום פעילות בתאריך " + DateTime.Parse(bidItem.ActivityDayStartDate).ToString("dd/MM/yyyy") + " " +
                           " " + DateTime.Parse(bidItem.ActivityDayStartDate).ToString("HH:mm") + " - " +
                           DateTime.Parse(bidItem.ActivityDayEndDate).ToString("HH:mm") into g
                           select new BidListItemForActivityDay(g.Key, g.ToList())).ToList();

            return results;
        }

        public List<ActivityDay> GetActivityDaysForReservation(string userId, string reservationGuid)
        {

            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter()
            {
                Alias = "ActivityDay",
                Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("Reservation"),
                Operator = "eq",
                Type = "lookup",
                Value = reservationGuid
            });
            //filters.Add(new Filter() { Alias = "ActivityDay",
            //    Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("ActivityDayStatus"),
            //    Operator = "not-eq", Type = "optionset", Value = "6" });
            //filters.Add(new Filter() { Alias = "ActivityDay", Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("ActivityDayStatus"), Operator = "not-eq", Type = "optionset", Value = "3" });

            List<Sort> sort = new List<Sort>();
            sort.Add(new Sort()
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("StartDateTime"),
                Direction = "asc"
            });

            return activityDayRepository.GetFetchXml(userId, false, null, sort, filters, null).Item2;
            // return activityDayList;

        }


        public override Tuple<int, List<ActivityDayListItem>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            return GetFetchXml(userId, isAggregate, paging, sort, filter, "", false);
        }


        public override Tuple<int, List<ActivityDayListItem>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter, string type = "", bool getAllItems = false)
        {

            bool includeEventReservation = false;
            bool includEventRegistrationActivityDay = false;

            Filter item = filter.Find(x => x.Field == "eventReservationStatus");
            Filter eventRegistrationActivityDayFilter = filter.Find(x => x.Alias == "EventRegistrationActivityDay");
            if (item != null)
            {
                includeEventReservation = true;
            }
            if (eventRegistrationActivityDayFilter != null)
            {
                includEventRegistrationActivityDay = true;
            }
            List<Sort> activityDaySort = new List<Sort>();
            List<Sort> additionalSort = new List<Sort>();
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

                    if (x.Active == "startTime")
                    {
                        x.Active = "startDateTime";
                    }

                    if (x.Active == "endTime")
                    {
                        x.Active = "endDateTime";
                    }
                });

                activityDaySort = helperService.BuildSorting<ActivityDayListItem, ActivityDayCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "ActivityDay"));




                additionalSort.AddRange(helperService.BuildSorting<Reservation, ReservationCRM>(sort.FindAll(x => x.Alias == "Reservation")));
                //  if (includeEventReservation)
                // additionalSort.AddRange(helperService.BuildSorting<EventReservationItem, EventReservationCRM>(sort.FindAll(x => x.Alias == "EventReservation")));
                additionalSort.AddRange(helperService.BuildSorting<Institution, InstitutionCRM>(sort.FindAll(x => x.Alias == "Institution")));
                additionalSort.AddRange(helperService.BuildSorting<BudgetFramework, BudgetFrameworkCRM>(sort.FindAll(x => x.Alias == "BudgetFramework")));

            }

            List<Filter> activityDayFilters = new List<Filter>();
            List<Filter> eventRegistrationFilters = new List<Filter>();
            List<Filter> additionalFilters = new List<Filter>();
            List<Filter> budgetFrameworkFilters = new List<Filter>();
            List<Filter> multipleFilters = new List<Filter>();
            if (filter != null)
            {
                multipleFilters = filter.FindAll(x => x.Alias != null && x.Alias != "" && x.Alias.Contains("|"));
                if (multipleFilters.Count > 0)
                {
                    foreach (var filterItem in multipleFilters)
                    {
                        var aliasArray = filterItem.Alias.Split('|');
                        foreach (var alias in aliasArray)
                        {
                            filter.Add(new Filter
                            {
                                Field = filterItem.Field,
                                Value = filterItem.Value,
                                Operator = filterItem.Operator,
                                Type = filterItem.Type,
                                Alias = alias,
                                FilterType = filterItem.FilterType,
                            });
                        }
                    }
                }
                activityDayFilters = helperService.BuildFilter<ActivityDayListItem, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "ActivityDay"));
                additionalFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation")));
                if (includeEventReservation)
                    additionalFilters.AddRange(helperService.BuildFilter<EventReservationItem, EventReservationCRM, EventReservationCRM>(filter.FindAll(x => x.Alias == "EventReservation")));
                additionalFilters.AddRange(helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filter.FindAll(x => x.Alias == "Institution")));
                //ADINA 2022-02-24 additionalFilters.AddRange(helperService.BuildFilter<BudgetFramework, BudgetFrameworkCRM, BudgetFrameworkCRM>(filter.FindAll(x => x.Alias == "BudgetFramework")));

                //ADINA 2022-02-24:
                budgetFrameworkFilters = helperService.BuildFilter<BudgetFramework, BudgetFrameworkCRM, BudgetFrameworkCRM>(filter.FindAll(x => x.Alias == "BudgetFramework" || x.Alias == "BudgetFrameworkEventReservation"));
                if (includEventRegistrationActivityDay)
                {
                    eventRegistrationFilters.AddRange(helperService.BuildFilter<EventRegistration, EventRegistrationCRM, EventRegistrationCRM>(filter.FindAll(x => x.Alias == "EventRegistration")));
                }
                    
            }


            if (isAggregate)
            {
                //<entity name='new_activity_day' alias='ActivityDay'>
                fetchXml = @"<attribute name='new_number_of_groups' alias='new_number_of_groups' aggregate='sum' />
                <attribute name='new_number_of_participants' alias='new_number_of_participants' aggregate='sum' />";
            }
            else
            {
                fetchXml = @"<attribute name='new_end_datetime'/>
                <attribute name='new_start_datetime' />
                <attribute name='new_number_of_groups' />
                <attribute name='new_actual_number_of_participants' />
                <attribute name='new_number_of_participants' />
                <attribute name='new_reservation' />
                <attribute name='new_event_reservations' />
                <attribute name='new_activity_day_location' />
                <attribute name='new_activity_dayid' />
                <attribute name='new_total_price' />
                <attribute name='new_total_price_after_discount' />
                <attribute name='new_activity_day_type' />
                <attribute name='new_division_router' />
                <attribute name='new_is_museum_required' />
                <attribute name='new_is_campus_required' />  
                <attribute name='new_is_big_questions_required' />
                <attribute name='new_activity_day_status' />
                <attribute name='new_testimony_amount_scheduled' />
                <attribute name='new_testimony_amount_needed' />
                <attribute name='new_lecturer_amount_scheduled' />
                <attribute name='new_lecturer_amount_needed' />
                <attribute name='new_guide_amount_scheduled' />
                <attribute name='new_guide_amount_needed' />
                <attribute name='new_cantor_amount_scheduled' />
                <attribute name='new_cantor_amount_needed' />
                <attribute name='new_actor_amount_scheduled' />
                <attribute name='new_actor_amount_needed' />
                <attribute name='new_museum_amount_scheduled' />
                <attribute name='new_museum_amount_needed' />
                <attribute name='new_num_additional_participants' />
                <attribute name='new_big_questions_amount_scheduled' />
                <attribute name='new_big_questions_amount_needed' />
                <attribute name='new_did_group_arrive' />
                <attribute name='createdon' />
                <attribute name='new_is_activity_day_locked' />";

            }

            if (includeEventReservation)
            {
                //<filter type = 'or'><condition attribute='new_rsv_department' operator='in'><value>bdb1594c-eaaa-ea11-80d3-005056911b09</value></condition></filter>
                if (budgetFrameworkFilters.Count > 0)
                {
                    for (int i = budgetFrameworkFilters.Count; i > 0; i--)
                    {
                        budgetFrameworkFilters[i - 1].FilterType = "or";
                        budgetFrameworkFilters.Add(JsonConvert.DeserializeObject<Filter>(JsonConvert.SerializeObject(budgetFrameworkFilters[i - 1])));
                        budgetFrameworkFilters[i].Alias = "BudgetFrameworkEventReservation";
                    }

                    fetchXml += basicRepository.FilterFetchXML(budgetFrameworkFilters);
                }


                fetchXml += reservationsRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "outer", "includeEventReservation");
                //additionalFilters.AddRange(budgetFrameworkFilters);
                fetchXml += eventReservationRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "outer", "BudgetFrameworkEventReservation");
                var tourTypeFilters = filter.FindAll(f => f.Alias == "TourType"&& f.Field== "isNeedEvent");
                    if(tourTypeFilters.Count>0&&!isAggregate){
                    fetchXml += @"<link-entity name=""new_tour_type"" from=""new_tour_typeid"" to=""new_activity_day_type"" alias=""TourType"">
                                    <filter type=""and"">
                                        <condition attribute=""new_is_need_event"" operator=""eq"" value=""1""/>
                                    </filter>
                                 </link-entity>";
                }

            }
            else
            {
                additionalFilters.AddRange(budgetFrameworkFilters);
                string reservationLinkType = "inner";
                fetchXml += reservationsRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, reservationLinkType);
            }
            if (filter != null)
            {
                Filter productFilter = filter.Find(f => f.Field == "product" && f.Alias.Contains('|'));

                if (productFilter != null)
                {
                    productFilter.Field = "new_yv_product";
                    additionalFilters.Add(productFilter);
                    fetchXml += @"<link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_dayid' alias='ActivityDayProductsFilters' link-type='inner'>";
                    productFilter.Alias = "BidItem";
                    fetchXml += bidItemRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "outer", "ActivityDay");
                    productFilter.Alias = "ItineraryItem";
                    fetchXml += itineraryGroupRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "outer", "ActivityDay");
                    productFilter.Alias = "EventItem";
                    fetchXml += eventItemRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "outer", "ActivityDay");
                    fetchXml += @"</link-entity>
                                <filter type='or'>";

                    if (!isAggregate)
                    {
                        fetchXml += "<condition entityname='BidItem' attribute='statecode' operator='not-null' />";
                        fetchXml += "<condition entityname='ItineraryItem' attribute='statecode' operator='not-null' />";
                        fetchXml += "<condition entityname='EventItem' attribute='statecode' operator='not-null' />";

                    }
                    fetchXml += "</filter>";
                }
                if (includeEventReservation)
                {
                    fetchXml += "<filter type='or'><condition entityname='Reservation' attribute='statecode' operator='not-null' /><condition entityname='EventReservation' attribute='statecode' operator='not-null' /></filter>";
                }
            }
            if (includeEventReservation)
                fetchXml += "<filter type='or'><condition attribute='new_reservation' operator= 'not-null'/><condition attribute='new_event_reservations' operator= 'not-null'/></filter>";

            Tuple<int, List<ActivityDayListItem>> activityDayList;
            if(getAllItems == true)
            {
                activityDayList = activityDayListRepository.GetAllFetchXml(userId, isAggregate, paging, activityDaySort, activityDayFilters, fetchXml);
            }
            else
            {
                activityDayList = activityDayListRepository.GetFetchXml(userId, isAggregate, paging, activityDaySort, activityDayFilters, fetchXml);
            }
            if (includEventRegistrationActivityDay)
            {
                fetchXml = @"<attribute name='new_end_datetime'/>
                <attribute name='new_start_datetime' />
                <attribute name='new_number_of_groups' />
                <attribute name='new_actual_number_of_participants' />
                <attribute name='new_activity_day_location' />
                <attribute name='new_activity_dayid' />
                <attribute name='new_activity_day_type' />
                <attribute name='new_division_router' />
                <attribute name='new_is_museum_required' />
                <attribute name='new_is_campus_required' />  
                <attribute name='new_is_big_questions_required' />
                <attribute name='new_activity_day_status' />";
                activityDayFilters = activityDayFilters.FindAll(f => f.Field != "new_reservation" && f.Field != "new_activity_day_location");
                fetchXml += eventRegistrationRepository.GetFullEventRegistrationActivityDayLinkedEntity(isAggregate, eventRegistrationFilters, activityDaySort, "inner", "");
                Tuple<int, List<ActivityDayListItem>> activityDayListByEventRegistration;
                if (getAllItems == true)
                {
                    activityDayListByEventRegistration = activityDayListRepository.GetAllFetchXml(userId, isAggregate, paging, activityDaySort, activityDayFilters, fetchXml);
                }
                else
                {
                    activityDayListByEventRegistration = activityDayListRepository.GetFetchXml(userId, isAggregate, paging, activityDaySort, activityDayFilters, fetchXml);
                }
                if (activityDayListByEventRegistration.Item1 > 0)
                {
                    activityDayList = new Tuple<int, List<ActivityDayListItem>>(activityDayList.Item1 + activityDayListByEventRegistration.Item1, activityDayList.Item2.Concat(activityDayListByEventRegistration.Item2).ToList().OrderBy(activityDay => activityDay.StartDateTime).ToList());
                }
            }
            List<Filter> musemEventItemFilters = new List<Filter>();

            musemEventItemFilters.Add(new Filter()
            {
                Field = "IsResource",
                Value = "true",
                Type = "boolean"
            });
            musemEventItemFilters.Add(new Filter()
            {
                Field = "Resource",
                Value = ConfigurationManager.AppSettings[$"museumResourceId_{environment}"],
                Type = "lookup"
            });

            musemEventItemFilters = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(musemEventItemFilters);
            List<Filter> oldFilter = filter;
            filter = filter.Where(f => f.Field.Contains("date") || f.Field.Contains("start") || f.Field.Contains("end")).ToList();
            filter.ForEach(x =>
                {
                    x.Alias = "EventItem";
                });

            musemEventItemFilters.AddRange(filter.Where(f => f.Field.Contains("date") || f.Field.Contains("start") || f.Field.Contains("end")).ToList());

            List<Filter> attachmetFilter = oldFilter.FindAll(x => x.Alias == "Attachment");
            if (attachmetFilter.Count > 0)
            {
                string newFetchXml = fetchXml;
                newFetchXml += "<link-entity name='new_reservation' from='new_reservationid' to='new_reservation' >" +
                    "<link-entity name='new_attachment' from='new_attachments' to='new_reservationid'>" +
                    "<filter type='and'>" +
                    "<condition attribute='new_file_type' operator='eq' value='1' />" +
                    "<condition attribute='statecode' operator='eq' value='0' />" +
                    "</filter>" +
                    "</link-entity>" +
                    "</link-entity>";
                Tuple<int, List<ActivityDayListItem>> activityDayListWithBidAttachment;
                if(getAllItems == true)
                {
                    activityDayListWithBidAttachment = activityDayListRepository.GetAllFetchXml(userId, isAggregate, paging, activityDaySort, activityDayFilters, newFetchXml);
                }
                else
                {
                    activityDayListWithBidAttachment = activityDayListRepository.GetFetchXml(userId, isAggregate, paging, activityDaySort, activityDayFilters, newFetchXml);
                }
                foreach (var day in activityDayListWithBidAttachment.Item2)
                {
                    activityDayList.Item2.Remove(activityDayList.Item2.FirstOrDefault(d => d.Id == day.Id));
                }
                activityDayList =new Tuple<int, List<ActivityDayListItem>>(activityDayList.Item2.Count, activityDayList.Item2);

            }
            if (multipleFilters != null && multipleFilters.Count>0) {

                var distinctList = activityDayList.Item2.Distinct().ToList();
                activityDayList = new Tuple<int, List<ActivityDayListItem>>(distinctList.Count, distinctList);

            }
            return activityDayList;

        }

        public override void Cancel(ActivityDay item, string userId, bool isCancel = true)
        {
            //InheritIsCanceledToChildrenEntities(item);
            activityDayRepository.Cancel(item, userId, isCancel);
            //CancelReservationEntities(item, userId);
        }

        /*public Tuple<int, List<ActivityDayListItem>> GetActivityDaySummary(string userId, string date, string divisionRouter) {

            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                <entity name='new_activity_day' alias='ActivityDay'>
                <attribute name='new_number_of_groups' alias='new_number_of_groups' aggregate='sum' />
                <attribute name='new_number_of_participants' alias='new_number_of_participants' aggregate='sum' />
                <filter type='and'>
                <condition attribute='new_start_datetime' operator='on' value='" + DateTime.Parse(date).ToString("yyyy-MM-dd") + @"' />
                <condition attribute='statecode' operator='eq' value='0' />
                <condition attribute='new_activity_day_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>";

            if (divisionRouter != null && divisionRouter!="")
            {
                fetchXml += "<condition attribute = 'new_division_router' operator= 'eq' value = '" + divisionRouter + "' />";
            }

            fetchXml += @"</filter>
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

            Tuple<int, List<ActivityDayListItem>> activityDayList = activityDayListRepository.GetFetchXmlOnly(userId, fetchXml);

            return activityDayList;

        }*/

        public Tuple<int, List<ActivityDayGrouped>> GetActivityDaysGrouped(string userId, DataDate data, string divisionRouter)
        {
            //תאריך לועזי; תאריך עברי, יום בשבוע, מס' הזמנות ליום; מס' קבוצות; מס' משתתפים; 
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                <entity name='new_activity_day' alias='ActivityDay'>
                <attribute name='new_start_datetime' alias='day' groupby='true' dategrouping='day' />
                <attribute name='new_start_datetime' alias='month' groupby='true' dategrouping='month' />
                <attribute name='new_start_datetime' alias='year' groupby='true' dategrouping='year' />
                <attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
                <attribute name = 'new_activity_day_location' alias = 'new_activity_day_location' groupby = 'true' />
                <attribute name='new_number_of_groups' alias='new_number_of_groups' aggregate='sum' />
                <attribute name='new_number_of_participants' alias='new_number_of_participants' aggregate='sum' />
                <filter type='and'>
                <condition attribute='new_start_datetime' operator= 'on-or-after' value ='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            if (data.EndDate != "0")
            {
                fetchXml += "<condition attribute='new_start_datetime' operator= 'on-or-before' value ='" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
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
                <order alias='day'/>
                <filter type='and'>
                <condition entityname='Reservation' attribute='statecode' operator='eq' value='0' />
                <condition entityname='Reservation' attribute='new_reservation_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>
                </filter>
                <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'>
                    <link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' alias='BudgetFramework'>
                        <attribute name='new_rsv_department' alias='new_rsv_department' groupby='true' />
                    </link-entity>
                </link-entity>
                </entity>
                </fetch>";

            Tuple<int, List<ActivityDayGrouped>> tempActivityDayReservationList = activityDayGroupedRepository.GetFetchXmlOnly(userId, fetchXml);

            string fetchXmlEvent = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                <entity name='new_activity_day' alias='ActivityDay'>
                <attribute name='new_start_datetime' alias='day' groupby='true' dategrouping='day' />
                <attribute name='new_start_datetime' alias='month' groupby='true' dategrouping='month' />
                <attribute name='new_start_datetime' alias='year' groupby='true' dategrouping='year' />
                <attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
                <attribute name = 'new_activity_day_location' alias = 'new_activity_day_location' groupby = 'true' />
                <attribute name='new_number_of_groups' alias='new_number_of_groups' aggregate='sum' />
                <attribute name='new_number_of_participants' alias='new_number_of_participants' aggregate='sum' />
                <attribute name='new_event_type'  alias='new_event_type' groupby = 'true' />

                <filter type='and'>
                <condition attribute='new_start_datetime' operator= 'on-or-after' value ='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            if (data.EndDate != "0")
            {
                fetchXmlEvent += "<condition attribute='new_start_datetime' operator= 'on-or-before' value ='" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            }

            fetchXmlEvent += @"<condition attribute='statecode' operator='eq' value='0' />
                <condition attribute='new_event_reservations' operator= 'not-null'/>
                <condition attribute='new_activity_day_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>";

            if (divisionRouter != null && divisionRouter != "")
            {
                fetchXmlEvent += "<condition attribute = 'new_division_router' operator= 'eq' value = '" + divisionRouter + "' />";
            }

            fetchXmlEvent += @"</filter>
                <order alias='year'/>
                <order alias='month'/>
                <order alias='day'/>
                <filter type='and'>
                <condition entityname='EventReservation' attribute='statecode' operator='eq' value='0' />
                <condition entityname='EventReservation' attribute='new_event_reservation_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>
                </filter>
                <link-entity name='new_event_reservations' from='new_event_reservationsid' to='new_event_reservations' visible='false' alias='EventReservation'>
                    <link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' alias='BudgetFramework'>
                        <attribute name='new_rsv_department' alias='new_rsv_department' groupby='true' />
                    </link-entity>
                </link-entity>
                </entity>
                </fetch>";

            Tuple<int, List<ActivityDayGrouped>> tempActivityDayEventList = activityDayGroupedRepository.GetFetchXmlOnly(userId, fetchXmlEvent);
            List<ActivityDayGrouped> unionList = tempActivityDayReservationList.Item2.Union(tempActivityDayEventList.Item2.FindAll(x => x.EventType.Id == "2")).ToList();
           
            // var activityDayList = tempActivityDayReservationList.Item2.GroupBy(item => new { item.StartDateTime, item.HebrewDate })
            var activityDayList = unionList.GroupBy(item => new { item.StartDateTime, item.HebrewDate })
             .Select(group => new ActivityDayGrouped
             {
                 StartDateTime = group.Key.StartDateTime,
                 HebrewDate = group.Key.HebrewDate,
                 Count = group.Sum(ta => ta.Count), //number of reservations
                 NumberOfGroups = group.Sum(ta => ta.NumberOfGroups),
                 NumberOfGroupsByEvent = tempActivityDayEventList.Item2.FindAll(x => x.StartDateTime == group.Key.StartDateTime && x.EventType.Id != "2").Sum(ta => ta.NumberOfGroups),
                 NumberOfVisitors = group.Sum(ta => ta.NumberOfVisitors),
                 NumberOfEvents = tempActivityDayEventList.Item2.FindAll(x => x.StartDateTime == group.Key.StartDateTime && x.EventType.Id != "2").Sum(ta => ta.Count),
                 //MultipleCountList = GetMultipleCountList(group.ToList(), divisionRouter)
                 MultipleCountList = group.Select(x => new MultipleIdValueCount(x.Department, x.Location, x.NumberOfGroups)).GroupBy(x => new { Item1Id = x.Item1.Id, Item1Value = x.Item1.Value, Item2Id = x.Item2.Id, Item2Value = x.Item2.Value }, x => x.Count)
                           .Select(y => new MultipleIdValueCount
                           {
                               Item1 = new IdValuePair { Id = y.Key.Item1Id, Value = y.Key.Item1Value },
                               Item2 = new IdValuePair { Id = y.Key.Item2Id, Value = y.Key.Item2Value },
                               Count = y.Sum()
                           }).ToList(),
                 MultipleCountListByEvent = tempActivityDayEventList.Item2.FindAll(x => x.StartDateTime == group.Key.StartDateTime && x.EventType.Id != "2").Select(x => new MultipleIdValueCount(x.EventType, x.Location, x.NumberOfGroups)).ToList(),
             })
             .ToList();
            return new Tuple<int, List<ActivityDayGrouped>>(tempActivityDayReservationList.Item1, activityDayList);
        }

        private List<List<string>> GetMultipleCountList(List<ActivityDayGrouped> group, string divisionRouter)
        {
            List<List<string>> list = new List<List<string>>();

            List<IdValuePair> locationList = idValueListService.GetLookupListByName<ActivityDayLocationCRM>(lists.ActivityDayLocationList);
            List<IdValuePair> departmentList;
            if (divisionRouter != null && divisionRouter != "")
            {
                var filterField = mapFunctionRepository.GetDestinationPropertyFor<Department, DepartmentCRM>("DivisionRouter");
                departmentList = idValueListService.GetLookupListWithFilterByName<DepartmentCRM>(lists.Department, filterField, divisionRouter);
            }
            else
            {
                departmentList = idValueListService.GetLookupListByName<DepartmentCRM>(lists.Department);
            }

            List<string> row = new List<string>();
            row.Add("מחלקה");
            foreach (IdValuePair location in locationList)
            {
                row.Add(location.Value);
            }
            row.Add("סה\"כ קבוצות");
            list.Add(row);

            foreach (IdValuePair department in departmentList)
            {
                row = new List<string>();
                row.Add(department.Value);

                list.Add(row);
            }

            return list;
        }

        public List<IdValueCount> GetActivityDaysGroupedDetails(string userId, DataDate data, string divisionRouter, string type, string reservation = "Reservation")
        {
            List<IdValueCount> list = new List<IdValueCount>();
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                <entity name='new_activity_day' alias='ActivityDay'>";
            if (type == "Location")
            {
                fetchXml += "<attribute name = 'new_activity_day_location' alias = 'new_activity_day_location' groupby = 'true' />";
            }
            if (type == "EventType")
            {
                fetchXml += @" <attribute name='new_event_type' alias='new_event_type' groupby='true' />";

            }

            fetchXml += @"<attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
            <attribute name='new_number_of_groups' alias='new_number_of_groups' aggregate='sum' />
            <filter type='and'>";
            if (data.EndDate == "0")
                fetchXml += "<condition attribute='new_start_datetime' operator= 'on' value ='" + DateTime.ParseExact(data.StartDate, "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";

            if (data.EndDate != "0")
            {
                fetchXml += "<condition attribute='new_start_datetime' operator= 'on-or-after' value ='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
                fetchXml += "<condition attribute='new_start_datetime' operator= 'on-or-before' value ='" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            }
            fetchXml += @"<condition attribute='statecode' operator='eq' value='0' />";
            if (reservation == "Reservation")
            {
                fetchXml += @"<condition attribute = 'new_reservation' operator= 'not-null' />";
            }
            else if (reservation == "EventReservation")
            {
                fetchXml += @"<condition attribute = 'new_event_reservations' operator= 'not-null' />";//to check
                fetchXml += @"<condition attribute = 'new_event_type' operator= 'ne' value='2' />";//to check
            }
            else if (reservation == "EventStudyDay")
            {
                fetchXml += @"<condition attribute = 'new_event_reservations' operator= 'not-null' />";//to check
                fetchXml += @"<condition attribute = 'new_event_type' operator= 'eq' value='2' />";//to check
            }
            fetchXml += @"<condition attribute= 'new_activity_day_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>";

            if (divisionRouter != null && divisionRouter != "")
            {
                fetchXml += "<condition attribute = 'new_division_router' operator= 'eq' value = '" + divisionRouter + "' />";
            }

            fetchXml += @"</filter>";
            if (reservation == "Reservation")
            {
                fetchXml += @"<link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'><filter type='and'>
                <condition attribute='statecode' operator='eq' value='0' />
                <condition attribute='new_reservation_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>
                </filter>";
            }
            else if (reservation == "EventReservation" || reservation == "EventStudyDay")
            {
                fetchXml += @"<link-entity name='new_event_reservations' from='new_event_reservationsid' to='new_event_reservations' visible='false' alias='EventReservation'><filter type='and'>
                <condition attribute='statecode' operator='eq' value='0' />
                <condition attribute='new_event_reservation_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>
                </filter>";
            }


            if (type == "Department")
            {
                fetchXml += @"<link-entity name='new_budget_framework' from='new_budget_frameworkid' to='new_budget_framework' alias='BudgetFramework'>
                    <attribute name='new_rsv_department' alias='new_rsv_department' groupby='true' />
                    </link-entity>";
            }

            fetchXml += @"</link-entity>
                </entity>
                </fetch>";

            list = activityDayGroupedRepository.GetCountListByGroup(userId, fetchXml, type);
            return list;

        }

        public List<IdValueCount> GetCountByCountry(string userId, DataDate data, Boolean Israel = false)
        {
            List<IdValueCount> list = new List<IdValueCount>();
            var israelCountryGuid = ConfigurationManager.AppSettings[$"israelCountryId_{environment}"];

            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                                <entity name='new_activity_day'>
                                <attribute name='new_start_datetime'  alias='month' groupby='true' dategrouping='month'/>
                                <attribute name='new_start_datetime'  alias='year' groupby='true' dategrouping='year'/>
                                <filter type='and'><condition attribute='statecode' operator='eq' value='0' />";
            //fetchXml += "<condition attribute='new_start_datetime' operator='on-or-after'  value ='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            // fetchXml += "<condition attribute='new_end_datetime' operator= 'on-or-before'  value = '" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' /> ";
            fetchXml += "<condition attribute='new_start_datetime' operator='on-or-after' value='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' /><condition attribute='new_end_datetime' operator='on-or-before' value='" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"'/>";
            fetchXml += @"</filter><link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation'>
                          <attribute name='new_reservationid'  alias='new_reservationid' aggregate='count'/><filter type='and'>";
            if (Israel)
            {
                fetchXml += "<condition attribute='new_country' operator='eq'  value='{" + israelCountryGuid + "}' />";
            }
            else
            {
                fetchXml += "<condition attribute='new_country' operator='ne' value='{" + israelCountryGuid + "}' />";
            }
            fetchXml += "</filter></link-entity></entity></fetch>";
            list = activityDayGroupedRepository.GetCountListByGroup(userId, fetchXml, "Country");
            return list;
        }
        public List<IdValueCount> GetCountByInstitution(string userId, DataDate data, Boolean institution = true)
        {
            List<IdValueCount> list = new List<IdValueCount>();

            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                                <entity name='new_activity_day'>
                                <attribute name='new_start_datetime'  alias='month' groupby='true' dategrouping='month'/>
                                <attribute name='new_start_datetime'  alias='year' groupby='true' dategrouping='year'/>
                                <filter type='and'><condition attribute='statecode' operator='eq' value='0' />";
            //fetchXml += "<condition attribute='new_start_datetime' operator='on-or-after'  value ='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />";
            // fetchXml += "<condition attribute='new_end_datetime' operator= 'on-or-before'  value = '" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' /> ";
            fetchXml += "<condition attribute='new_start_datetime' operator='on-or-after' value='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' /><condition attribute='new_end_datetime' operator='on-or-before' value='" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"'/>";
            fetchXml += @"</filter><link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='reservation'>
                          <attribute name='new_reservationid'  alias='new_reservationid' aggregate='countcolumn' distinct='true' /><filter type='and'>";
            if (institution)
            {
                fetchXml += "<condition attribute = 'new_rsv_institution' operator= 'not-null' />";
            }
            else
            {
                fetchXml += "<condition attribute = 'new_rsv_institution' operator= 'null' />";
            }
            fetchXml += "</filter></link-entity></entity></fetch>";
            list = activityDayGroupedRepository.GetCountListByGroup(userId, fetchXml, "Institution");
            return list;
        }

        public List<GroupList> GetCountParticipantsByLocation(string userId, DataDate data)
        {
            List<GroupIdValueCountList> list = new List<GroupIdValueCountList>();
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                <entity name='new_activity_day' alias='ActivityDay'>
               <attribute name = 'new_start_datetime'  alias = 'month' groupby = 'true' dategrouping = 'month' />
               <attribute name = 'new_start_datetime'  alias = 'year' groupby = 'true' dategrouping = 'year' />";
            fetchXml += "<attribute name = 'new_activity_day_location' alias = 'new_activity_day_location' groupby = 'true' />";
            fetchXml += @"<attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
            <attribute name='new_number_of_participants' alias='new_number_of_participants' aggregate='sum' />
            <filter type='and'>";
            fetchXml += "<condition attribute='new_start_datetime' operator='on-or-after' value='" + DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' /><condition attribute='new_end_datetime' operator='on-or-before' value='" + DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"'/>";
            fetchXml += @"<condition attribute='statecode' operator='eq' value='0' />
                <condition attribute = 'new_reservation' operator= 'not-null' />
                <condition attribute= 'new_activity_day_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>";
            fetchXml += @"</filter>
                <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' visible='false' alias='Reservation'>
                <filter type='and'>
                <condition attribute='statecode' operator='eq' value='0' />
                <condition attribute='new_reservation_status' operator='not-in'>
                <value>6</value>
                <value>3</value>
                </condition>
                </filter>";

            fetchXml += @"</link-entity>
                </entity>
                </fetch>";

            list = activityDayGroupedRepository.GetCountListOfListsByGroup(userId, fetchXml, "Participants");
            int j = 0;
            List<GroupList> groupList = new List<GroupList>();
            foreach (GroupIdValueCountList item in list)
            {
                GroupList gl = new GroupList();
                gl.Location = item.Location;
                for (DateTime i = DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null); i <= DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null); i = i.AddMonths(1), j++)
                {
                    gl.Dates.Add(i.Month.ToString() + "-" + i.Year.ToString());
                    var count = item.Dates.Where(x => x.Id == i.Month.ToString() + "-" + i.Year.ToString()).Select(x => x.Count).ToList();
                    gl.Counts.Add(count.Count > 0 ? count[0].ToString() : "0");
                }
                groupList.Add(new GroupList
                {
                    Location = gl.Location,
                    Dates = gl.Dates,
                    Counts = gl.Counts
                });
            }

            return groupList;
        }

        public void SetActivityDayStatus(string id, string status, string userId)
        {
            activityDayRepository.SetActivityDayStatus(Guid.Parse(id), status, userId);
        }

        public ActivityDaySubject GetSubjectDetails(string userId, string guid)
        {
            return activityDaySubjectRepository.GetFetchXml(userId, guid);
        }

        public List<EventItemForExcel> GetPaymentListForExcel(string userId, string startDate, string endDate, bool withUnion)
        {
            //get all schedule/event-item per time (כל השיבוצים)
            List<EventItemForExcel> eventList = activityDayRepository.GetPaymentListForExcel(userId, startDate, endDate);
            List<EventItemForExcel> cascadedList = activityDayRepository.GetCascadedListForExcel(userId, startDate, endDate);
            List<EventItemForExcel> contractDaysandSik = activityDayRepository.GetContractDaysAndSikForExcel(userId, startDate, endDate);
            List<EventItemForExcel> finalList = new List<EventItemForExcel>();

            /*var a = eventList.FindAll(e => e.ReservationNum.Contains("789") && e.ServiceProviderFullName.Contains("מטיוס"));
            var d = cascadedList.FindAll(e => e.ReservationNum.Contains("789") && e.ServiceProviderFullName.Contains("מטיוס יונתן"));
            var f = cascadedList.FindAll(e => e.ReservationNum.Contains("789"));

            var b = eventList.FindAll(e => DateTime.Parse(e.EndEvent).Month != DateTime.ParseExact(e.End, "dd/MM/yyyy HH:mm:ss", null).Month);
            var c = cascadedList.FindAll(e => DateTime.Parse(e.EndEvent).Month != DateTime.ParseExact(e.End, "dd/MM/yyyy HH:mm:ss", null).Month);
            b.AddRange(c);*/
            if (withUnion)//regular doch
            {
                //return this.scheduleEventsHelperService.UnionScheduleEvents(eventList, cascadedList);
                finalList = this.scheduleEventsHelperService.UnionScheduleEvents(userId, eventList, cascadedList);


            }
            else//all events include cascaded list
            {
                eventList.AddRange(cascadedList);
                finalList = eventList;
            }
            finalList = this.scheduleEventsHelperService.CalculateKm(finalList);
            finalList.AddRange(contractDaysandSik);
            return finalList;
        }
        public List<EventItemForExcel> GetPaymentListByActivityDaylist(string userId, List<string> activityDayidList, List<string> eventIdList)
        {
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='false'>
                <entity name='new_activity_day' alias='EventItem'>
                <all-attributes/>
                <filter type='and'>
                <condition attribute='statecode' operator='eq' value='0' />
                <condition attribute= 'new_activity_dayid' operator='in'>";
            foreach (var id in activityDayidList)
            {
                fetchXml += "<value>" + id + "</value>";
            }
            fetchXml += @"</condition>
                </filter></entity></fetch>";

            List<EventItemForExcel> eventList = activityDayRepository.GetPaymentListByActivityDaylist(userId, fetchXml, eventIdList);
            List<EventItemForExcel> cascadedList = activityDayRepository.GetCascadedListForActivityDayIdList(userId, fetchXml, eventIdList);
            List<EventItemForExcel> finalList;
            finalList = this.scheduleEventsHelperService.UnionScheduleEvents(userId, eventList, cascadedList);
            eventList.AddRange(cascadedList);
            finalList = eventList;


            finalList = this.scheduleEventsHelperService.CalculateKm(finalList);
            return finalList;
        }
        public List<IdValuePair> GetProductsOutOfStockForCurrentDay(string userId, string date, string activityDayTypeId)
        {
            List<IdValuePair> productsOutOfStockForCurrentDayList = new List<IdValuePair>();
            //for now, get products for only current activity date type...
            string fetchXmlProducts = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                              <entity name='new_yv_product'>
                                <attribute name='new_max_products_per_day' />
                                <attribute name='new_yv_productid' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                                <link-entity name='new_new_tour_type_new_yv_product_optional' from='new_yv_productid' to='new_yv_productid' link-type='outer'>          
                                   <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeid' alias='TourType1' link-type='outer'>       
                                    <filter type='and'>
                                      <condition attribute='new_tour_typeid' operator='eq'  uitype='new_tour_type' value='{" + activityDayTypeId + @"}' />
                                    </filter>
                                  </link-entity>
                                </link-entity>
                                <link-entity name='new_new_tour_type_new_yv_product_required' from='new_yv_productid' to='new_yv_productid' link-type='outer'>
                                  <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeid' alias='TourType2' link-type='outer'>       
                                    <filter type='and'>
                                      <condition attribute='new_tour_typeid' operator='eq'  uitype='new_tour_type' value='{" + activityDayTypeId + @"}' />
                                    </filter>
                                  </link-entity>
                                </link-entity>
                                <link-entity name='new_new_tour_type_new_yv_product_no_cost' from='new_yv_productid' to='new_yv_productid'  link-type='outer'>
                                  <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeid' alias='TourType3' link-type='outer'>       
                                    <filter type='and'>
                                       <condition attribute='new_tour_typeid' operator='eq'  uitype='new_tour_type' value='{" + activityDayTypeId + @"}' />
                                    </filter>
                                  </link-entity>
                                </link-entity>
                                <link-entity name='new_tour_type_yv_product_required_no_cost' from='new_yv_productid' to='new_yv_productid'  link-type='outer'>
                                  <link-entity name='new_tour_type' from='new_tour_typeid' to='new_tour_typeid' alias='TourType4' link-type='outer'>       
                                    <filter type='and'>
                                      <condition attribute='new_tour_typeid' operator='eq'  uitype='new_tour_type' value='{" + activityDayTypeId + @"}' />
                                    </filter>
                                  </link-entity>
                                </link-entity>
                                <filter type='or'>
                                    <condition entityname='TourType1' attribute='new_tour_typeid' operator='not-null' />
                                    <condition entityname='TourType2' attribute='new_tour_typeid' operator='not-null' />
                                    <condition entityname='TourType3' attribute='new_tour_typeid' operator='not-null' />
                                    <condition entityname='TourType4' attribute='new_tour_typeid' operator='not-null' />
                                </filter>
                            </entity>
                        </fetch>";
            var productsList = productRepository.GetFetchXmlTake1(userId, fetchXmlProducts);
            if (productsList.Count > 0)
            {
                string fetchXmlCountByProduct = @"<fetch version='1.0' output-format='xml-platform' aggregate='true'>
                                        <entity name='new_itinerary_item'>
                                            <attribute name='new_itinerary_itemid' alias='new_product_count' aggregate='count' />
                                            <attribute name='new_yv_product' alias='new_yv_product' groupby ='true' />
                                            <filter type='and'>
                                                <condition attribute='statecode' operator='eq' value='0' />
                                                <condition attribute='new_start_time' operator='on' value='" +
                                                DateTime.ParseExact(date.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd") + @"' />  
                                                <condition attribute='new_yv_product' operator='in'>";
                foreach (var product in productsList)
                {
                    fetchXmlCountByProduct += "<value>" + product.Id + "</value>";
                }
                fetchXmlCountByProduct += @"</condition>
                                            </filter>
                                      </entity>
                                    </fetch>";
                var productListCount = activityDayGroupedRepository.GetCountListByGroup(userId, fetchXmlCountByProduct, "Product");
                foreach (var countProductItem in productListCount)
                {
                    var product = productsList.Find(p => p.Id == countProductItem.Id);
                    if (product != null && product.MaxProductsPerDay != 0 && (product.MaxProductsPerDay <= countProductItem.Count))
                    {
                        productsOutOfStockForCurrentDayList.Add(new IdValuePair(countProductItem.Id, countProductItem.Value));
                    }
                }
            }
            return productsOutOfStockForCurrentDayList;

        }

        public List<ActivityDay> CheckIfTheActivityDayExiestAndThereIsPlace(string userId, string eventId, DateTime selectedDate, int numberOfVisitors)
        {
            string formattedDate = selectedDate.ToString("dd/MM/yyyy");
            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter()
            {
                Alias = "ActivityDay",
                Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("StartDateTime"),
                Operator = "between-dates",
                Type = "date",
                Value = formattedDate + ","+ formattedDate
            });
            filters.Add(new Filter()
            {
                Alias = "ActivityDay",
                Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("EventReservation"),
                Operator = "eq",
                Type = "lookup",
                Value = eventId
            });

            List<Sort> sort = new List<Sort>();
            sort.Add(new Sort()
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("StartDateTime"),
                Direction = "asc"
            });

           var activityDay = activityDayRepository.GetFetchXml(userId, false, null, sort, filters, null).Item2;
           return activityDay;
        }


        /*<attribute name='new_activity_day_type' />
                                        <attribute name='new_number_of_groups' />
                                        <attribute name='new_number_of_participants' />
                                        <attribute name='new_reservation' />
                                        <attribute name='createdby' />
                                        <attribute name='new_activity_day_location' />
                                        <attribute name='new_campus_amount_needed' />
                                        <attribute name='new_museum_amount_needed' />
                                        <attribute name='new_big_questions_amount_needed' />*/

        /*public List<ActivityDayListItem> GetInvoiceListForExcel(string userId, string divisionRouter, List<Sort> sort, List<Filter> filter)
        {

            /*List<Filter> filters = new List<Filter>();
            filters.Add(new Filter() { Alias="ActivityDay", Field="startDateTime", Operator="between-dates", Type="date", Value=startDate+", "+endDate});
            filters.Add(new Filter() { Alias = "ActivityDay", Field = "activityDayStatus", Operator = "not-eq", Type = "optionset", Value = "6" });
            filters.Add(new Filter() { Alias = "Reservation", Field = "reservationStatus", Operator = "not-eq", Type = "optionset", Value = "6" });
            filters.Add(new Filter() { Alias = "Reservation", Field = "divisionRouter", Type = "divisionRouter", Value = divisionRouter });

            List<Sort> sort = new List<Sort>();
            sort.Add(new Sort() { Active = "startDateTime", Direction = "asc" });
            */
        // List<BidItem> bidList = BidItemRepository.GetFetchXml(userId, null, null, null).Item2;

        /* foreach (FrontDeskListItem item in list.Item2)
         {
             item.BidItem = bidList.Where(x => x.ActivityDay.Id == item.Id).ToList();
         }*/

        //List<EventItemForExcel> eventList = activityDayRepository.GetInvoiceListForExcel(userId, startDate, endDate);
        //return GetFetchXml(userId, null, sort, filters).Item2;

        //}

    }

}
