using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class ItineraryItemService : BasicService<ItineraryItem, ItineraryItem, ItineraryItemCRM>, IItineraryItemService
    {
        private IHelperService helperService;
        private IItineraryItemRepository itineraryItemRepository;
        private IActivityDayListRepository activityDayListRepository;
        private IFrontDeskResourceRepository frontDeskResourceRepository;
        private IReservationsRepository reservationsRepository;
        private IEventItemRepository eventItemRepository;
        private IQueryHelperRepository queryHelperRepository;
        public ItineraryItemService(IHelperService helperService,
            IItineraryItemRepository itineraryItemRepository,
            IActivityDayListRepository activityDayListRepository,
            IFrontDeskResourceRepository frontDeskResourceRepository,
            IEventItemRepository eventItemRepository,
            IReservationsRepository reservationsRepository,
             IQueryHelperRepository queryHelperRepository,

        IMapFunctionRepository mapFunctionRepository) : base(helperService, itineraryItemRepository, mapFunctionRepository)
        {
            this.helperService = helperService;
            this.itineraryItemRepository = itineraryItemRepository;
            this.activityDayListRepository = activityDayListRepository;
            this.frontDeskResourceRepository = frontDeskResourceRepository;
            this.eventItemRepository = eventItemRepository;
            this.reservationsRepository = reservationsRepository;
            this.queryHelperRepository = queryHelperRepository;
        }

        public override void Update(ItineraryItem item, ItineraryItem oldItem, string userId)
        {
            itineraryItemRepository.CreateOrUpdateEntity(item, oldItem, userId);
        }

        public Tuple<int, List<ItineraryItem>> GetItineraryListByResource(string userId, bool isAggregate, bool isExcel, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            string fetchXml = "";

            sort.ForEach(x => {
                if (x.Active.Contains(":"))
                {
                    x.Alias = x.Active.Split(':')[1];
                    x.Active = x.Active.Split(':')[0];
                }
                else
                {
                    x.Alias = "ItineraryItem";
                }
                if (x.Active == "startDateTime")
                    x.Active = "start";
                if (x.Active == "endDateTime")
                    x.Active = "end";
            });

            filter.ForEach(x => {
                if (x.Field == "startDateTime")
                    x.Field = "start";
                if (x.Field == "endDateTime")
                    x.Field = "end";
            });
            List<Sort> itineraryItemSort = helperService.BuildSorting<ItineraryItem, ItineraryItemCRM>(sort.FindAll(x => x.Alias == "ItineraryItem"));
            List<Filter> itineraryItemFilters = helperService.BuildFilter<ItineraryItem, ItineraryItemCRM, ItineraryItemCRM>(filter.FindAll(x => x.Alias == "ItineraryItem" || x.Alias == "" || x.Alias == null));

            //List<Sort> eventItemSort = helperService.BuildSorting<ItineraryItem, ItineraryItemCRM>(sort.FindAll(x => x.Alias == "ItineraryItem"));
            List<Filter> eventItemFilters = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(x => x.Alias == "EventItem" || (x.Alias != null && x.Alias.Split(':')[0] == "EventItem")));


            List<Sort> activityDaySort = null;//helperService.BuildSorting<ActivityDay, ActivityDayCRM>(sort.FindAll(x => x.Alias == "ActivityDay"));
            //List<Sort> additionalSort = null;//new List<Sort>();

            //additionalSort.AddRange(helperService.BuildSorting<Reservation, ReservationCRM>(sort.FindAll(x => x.Alias == "Reservation")));
            //additionalSort.AddRange(helperService.BuildSorting<Institution, InstitutionCRM>(sort.FindAll(x => x.Alias == "Institution")));

            List<Filter> activityDayFilters = helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay"));
            activityDayFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation")));
            activityDayFilters.AddRange(helperService.BuildFilter<EventReservationItem, EventReservationCRM, EventReservationCRM>(filter.FindAll(x => x.Alias == "EventReservation")));

            fetchXml = @"<attribute name='new_itinerary_itemid' />
                <attribute name='new_name' /> 
                <attribute name='new_start_time' />
                <attribute name='new_end_time' />
                <attribute name='new_scheduler_service_provider' />
                <attribute name='new_scheduler_resource' />";

            fetchXml += eventItemRepository.GetLinkedEntity(isAggregate, filter, null, "inner", "SchedulerResource");

            string serviceProviderLinkType = "outer";
            if(filter.FindAll(x => x.Alias == "EventItem:ServiceProvider" && x.Value != "").Count > 0)
            {
                serviceProviderLinkType = "inner";
            }
            fetchXml += eventItemRepository.GetLinkedEntity(isAggregate, filter, null, serviceProviderLinkType, "SchedulerServiceProvider");

            fetchXml += @"<filter type='or'>
                            <condition entityname='Reservation' attribute='statecode' operator='not-null' />
                            <condition entityname='EventReservation' attribute='statecode' operator='not-null' />
                        </filter>";
            Tuple<int, List<ItineraryItem>> tempReservations = itineraryItemRepository.GetFetchXml(userId, isAggregate, paging, itineraryItemSort, itineraryItemFilters, fetchXml);
            //Tuple<int, List<FrontDeskListItem>> tempEventReservations = frontDeskResourceRepository.GetFetchXml(userId, isAggregate, paging, itineraryItemSort, itineraryItemFilters, eventReservationFetchXml);


            return tempReservations;
        }

        public Tuple<int, List<ItineraryItem>> GetItineraryList(string userId, bool isAggregate, bool isExcel, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            
            string fetchXml = "";

            sort.ForEach(x => {
                if (x.Active.Contains(":"))
                {
                    x.Alias = x.Active.Split(':')[1];
                    x.Active = x.Active.Split(':')[0];
                }
                else
                {
                    x.Alias = "ItineraryItem";
                }
                if (x.Active == "startDateTime")
                    x.Active = "start";
                if (x.Active == "endDateTime")
                    x.Active = "end";
            });

            filter.ForEach(x => {
                if (x.Field == "startDateTime")
                    x.Field = "start";
                if (x.Field == "endDateTime")
                    x.Field = "end";
            });

            List<Sort> itineraryItemSort = helperService.BuildSorting<ItineraryItem, ItineraryItemCRM>(sort.FindAll(x => x.Alias == "ItineraryItem"));
            List<Filter> itineraryItemFilters = helperService.BuildFilter<ItineraryItem, ItineraryItemCRM, ItineraryItemCRM>(filter.FindAll(x => x.Alias == "ItineraryItem" || x.Alias == "" || x.Alias == null));

            //List<Sort> eventItemSort = helperService.BuildSorting<ItineraryItem, ItineraryItemCRM>(sort.FindAll(x => x.Alias == "ItineraryItem"));
            List<Filter> eventItemFilters = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(x => x.Alias == "EventItem" || (x.Alias != null && x.Alias.Split(':')[0] == "EventItem")));
            //     List<Filter> reservationFilters = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(x => x.Alias == "Reservation" || (x.Alias != null && x.Alias.Split(':')[0] == "EventItem")));


            List<Sort> activityDaySort = helperService.BuildSorting<ItineraryItem, ItineraryItemCRM>(sort.FindAll(x => x.Alias == "ActivityDay"));
            List<Filter> activityDayFilters = helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay"));
            List<Sort> reservationSort = helperService.BuildSorting<Reservation, ReservationCRM>(sort.FindAll(x => x.Alias == "Reservation"));
            List<Filter> reservationFilters = helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation"));
               fetchXml = @"<attribute name='new_itinerary_itemid' />
                <attribute name='new_name' /> 
                <attribute name='new_start_time' />
                <attribute name='new_end_time' />
                <attribute name='new_comments' />
                <attribute name='new_yv_product' />
                <attribute name='new_scheduler_service_provider' />";
            string serviceProviderLinkType = "outer";
            if (filter.FindAll(x => x.Alias == "EventItem:ServiceProvider" && x.Value != "").Count > 0)
            {
                serviceProviderLinkType = "inner";
            }
            fetchXml += eventItemRepository.GetLinkedEntity(isAggregate, filter, null, serviceProviderLinkType, "SchedulerServiceProvider");

            fetchXml += @"<link-entity name='new_new_itinerary_item_new_itinerary_group' from='new_itinerary_itemid' to='new_itinerary_itemid' link-type='inner' >
                             <link-entity name='new_itinerary_group' from='new_itinerary_groupid' to='new_itinerary_groupid' alias='ItineraryGroup' link-type='inner'>
                                <link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ActivityDay' link-type='inner'>
                                    <attribute name='new_end_datetime' />
                                    <attribute name='new_start_datetime' />
                                    <attribute name='new_number_of_groups' />
                                    <attribute name='new_number_of_participants' />
                                    <link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation'  link-type='inner'>
                                        <attribute name='new_reservationid' />
                                        <attribute name='new_name' />
                                        <attribute name='new_num' />
                                        <attribute name='new_number_of_groups' />
                                        <attribute name='new_rsv_institution' />";
            fetchXml += reservationsRepository.FilterFetchXML(reservationFilters);
            if (reservationSort.Count > 0)
            {
                fetchXml += queryHelperRepository.SortByFetchXml(reservationSort);
            }
            else
            {
                fetchXml += "<order attribute='new_num' descending='false' />";
            }
            fetchXml += @"</link-entity>";
            fetchXml +=activityDayListRepository.FilterFetchXML(activityDayFilters);
            fetchXml += @"</link-entity>
                                    <filter type='and'>
                                    <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                                </link-entity>
                            </link-entity>";
          

                Tuple<int, List<ItineraryItem>> tempReservations = itineraryItemRepository.GetFetchXml(userId, isAggregate, paging, itineraryItemSort, itineraryItemFilters, fetchXml);
            //Tuple<int, List<FrontDeskListItem>> tempEventReservations = frontDeskResourceRepository.GetFetchXml(userId, isAggregate, paging, itineraryItemSort, itineraryItemFilters, eventReservationFetchXml);


            return tempReservations;
        }

    }
}
