using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class FrontDeskService : BasicService<FrontDeskListItem, FrontDeskListItem, ActivityDayCRM>, IFrontDeskService
    {

        private readonly IFrontDeskSoundSystemRepository frontDeskSoundSystemRepository;
        private readonly IActivityDayService activityDayService;
        private readonly IEventRegistrationRepository eventRegistrationRepository;
        private readonly IItineraryItemRepository itineraryItemRepository;
        private readonly IItineraryGroupRepository itineraryGroupRepository;
        private readonly IReservationsRepository reservationsRepository;
        private readonly IBidItemRepository bidItemRepository;
        private readonly IProductRepository productRepository;
        private readonly IFrontDeskRepository frontDeskRepository;
        private readonly IFrontDeskResourceRepository frontDeskResourceRepository;
        private readonly IActivityDayListRepository activityDayListRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IFullEventItemRepository fullEventItemRepository;
        private readonly IFullItineraryItemRepository fullItineraryItemRepository;
        private readonly IEventItemRepository eventItemRepository;
        private readonly IQueryHelperRepository queryHelperRepository;
        private IServiceProviderRepository serviceProviderRepository;
        private IActivityDayRepository activityDayRepository;
        private readonly ISoundSystemItemRepository soundSystemItemRepository;
        private readonly IEventReservationRepository eventReservationRepository;
        private readonly IBudgetFrameworkRepository budgetFrameworkRepository;
        private readonly string environment;

        public FrontDeskService(
            IActivityDayRepository activityDayRepository,
            IFrontDeskSoundSystemRepository frontDeskSoundSystemRepository,
            IQueryHelperRepository queryHelperRepository,
            IActivityDayService activityDayService,
            IEventRegistrationRepository eventRegistrationRepository,
            IItineraryItemRepository itineraryItemRepository,
            IItineraryGroupRepository itineraryGroupRepository,
            IFrontDeskRepository frontDeskRepository,
            IProductRepository productRepository,
            IFrontDeskResourceRepository frontDeskResourceRepository,
            IReservationsRepository reservationsRepository,
            IActivityDayListRepository activityDayListRepository,
            IHelperService helperService,
            IMapFunctionRepository mapFunctionRepository,
            IBidItemRepository bidItemRepository,
            IFullEventItemRepository fullEventItemRepository,
            IFullItineraryItemRepository fullItineraryItemRepository,
            IEventItemRepository eventItemRepository,
            IServiceProviderRepository serviceProviderRepository,
            ISoundSystemItemRepository soundSystemItemRepository,
            IEventReservationRepository eventReservationRepository,
            IBudgetFrameworkRepository budgetFrameworkRepository)

            : base(helperService, frontDeskRepository, mapFunctionRepository)
        {
            this.activityDayRepository = activityDayRepository;
            this.frontDeskSoundSystemRepository = frontDeskSoundSystemRepository;
            this.queryHelperRepository = queryHelperRepository;
            this.activityDayService = activityDayService;
            this.eventRegistrationRepository = eventRegistrationRepository;
            this.itineraryItemRepository = itineraryItemRepository;
            this.itineraryGroupRepository = itineraryGroupRepository;
            this.productRepository = productRepository;
            this.bidItemRepository = bidItemRepository;
            this.fullEventItemRepository = fullEventItemRepository;
            this.fullItineraryItemRepository = fullItineraryItemRepository;
            this.eventItemRepository = eventItemRepository;
            this.frontDeskRepository = frontDeskRepository;
            this.frontDeskResourceRepository = frontDeskResourceRepository;
            this.reservationsRepository = reservationsRepository;
            this.activityDayListRepository = activityDayListRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.serviceProviderRepository = serviceProviderRepository;
            this.soundSystemItemRepository = soundSystemItemRepository;
            this.eventReservationRepository = eventReservationRepository;
            this.budgetFrameworkRepository= budgetFrameworkRepository;
            environment = ConfigurationManager.AppSettings["environment"];
        }

        public Tuple<int, List<FrontDeskListItem>> GetFetchXmlForExcel(string userId, Paging paging, List<Sort> sort, List<Filter> filter, string type = "")
        {
            Tuple<int, List<FrontDeskListItem>> list = GetListByResource(userId, false, true, paging, sort, filter);

            if (type != "ServiceProviders")
            {
                if (sort != null)
                {
                    sort.ForEach(x =>
                    {
                        if (x.Active.Contains(":"))
                        {
                            x.Alias = x.Active.Split(':')[1];
                            x.Active = x.Active.Split(':')[0];
                        }
                    });
                }

                filter.FindAll(x => x.Alias == "" || x.Alias == null).ForEach(x =>
                {
                    x.Alias = "ActivityDay";
                });

                string fetchXml = @"<attribute name='new_yv_product' />
                <attribute name='new_price' />
                <attribute name='new_amount' />
                <attribute name='new_discount' />
                <attribute name='new_total_price' />
                <attribute name='new_discount_type' />
                <attribute name='new_total_price_after_discount' />
                <attribute name='new_activity_day' />
                <attribute name='new_comments' />";

                fetchXml += activityDayListRepository.GetLinkedEntity(false, filter, null);
                List<BidItem> bidList = bidItemRepository.GetFetchXml(userId, false, null, null, null, fetchXml).Item2;

                foreach (FrontDeskListItem item in list.Item2)
                {
                    item.BidItem = bidList.Where(x => x.ActivityDay.Id == item.Id).ToList();
                }
            }


            return list;
        }

        public Tuple<int, List<FrontDeskListItem>> GetActivityDayList(string userId, bool isAggregate, bool isExcel, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            Tuple<int, List<ActivityDayListItem>> activityDayList = activityDayService.GetFetchXml(userId, isAggregate, paging, null, filter);
            List<BidItem> bidList = new List<BidItem>();

            if (sort != null && !isAggregate)
            {
                sort.ForEach(x =>
                {
                    if (x.Active.Contains(":"))
                    {
                        x.Alias = x.Active.Split(':')[1];
                        x.Active = x.Active.Split(':')[0];
                    }

                    if (x.Active == "startTime" || x.Active == "startDateTime")
                    {
                        x.Active = "StartDateTimeTemp";
                    }

                    if (x.Active == "endTime" || x.Active == "endDateTime")
                    {
                        x.Active = "EndDateTimeTemp";
                    }
                });
            }


            if (isExcel)
            {
                List<Filter> activityDayFilters = filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "ActivityDay");

                string fetchXml = @"<attribute name='new_yv_product' />
                <attribute name='new_price' />
                <attribute name='new_amount' />
                <attribute name='new_discount' />
                <attribute name='new_total_price' />
                <attribute name='new_discount_type' />
                <attribute name='new_total_price_after_discount' />
                <attribute name='new_activity_day' />
                <attribute name='new_comments' />";

                fetchXml += activityDayListRepository.GetLinkedEntity(false, activityDayFilters, null);
                bidList = bidItemRepository.GetFetchXml(userId, false, null, null, null, fetchXml).Item2;
                /*
                List<Filter> activityDayFilters = helperService.BuildFilter<ActivityDayListItem, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "ActivityDay"));

                 string conditions = "";
                 string fetchXml = @"<link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ActivityDay'>
                             <attribute name='new_name' />
                             <filter type='and'>" + conditions + @"</filter>
                             </link-entity>";

                 List<BidItem> bidList = bidItemRepository.GetFetchXml(userId, false, null, null, null, fetchXml).Item2;*/
            }

            List<FrontDeskListItem> tempList = new List<FrontDeskListItem>();

            activityDayList.Item2.ForEach(activityDay =>
            {

                int actualNumberOfVisitors = activityDay.ActualNumberOfVisitors;
                if (actualNumberOfVisitors == 0 || actualNumberOfVisitors == 1)
                {
                    actualNumberOfVisitors = activityDay.NumberOfVisitors;
                }





                var item = new FrontDeskListItem
                {
                    Id = activityDay.Id,
                    NumberOfGroups = activityDay.NumberOfGroups,
                    NumberOfVisitors = actualNumberOfVisitors,
                    StartDateTime = activityDay.StartDateTime,
                    EndDateTime = activityDay.EndDateTime,
                    StartDateTimeTemp = DateTime.Parse(activityDay.StartDateTime),
                    EndDateTimeTemp = DateTime.Parse(activityDay.EndDateTime),
                    Location = activityDay.Location,
                    ActivityDayStatus = activityDay.ActivityDayStatus,
                    TotalPrice = activityDay.TotalPrice,
                    TotalPriceAfterDiscount = activityDay.TotalPriceAfterDiscount,
                    ActivityDayType = activityDay.ActivityDayType,
                    Reservation = new IdValuePair(activityDay.Reservation.Id, activityDay.Reservation.ReservationNum),
                    EventReservation = new IdValuePair(activityDay.EventReservation.Id, activityDay.EventReservation.EventCode),
                    EventType = activityDay.EventReservation.EventType,
                    ReservationNum = activityDay.Reservation.ReservationNum,
                    GroupName = activityDay.Reservation.GroupName,
                    Language = activityDay.Reservation.Language,
                    ReservationStatus = activityDay.Reservation.ReservationStatus,
                    PaymentStatus = activityDay.Reservation.PaymentStatus,
                    FullNameContact = activityDay.Reservation.FullNameContact,
                    DivisionRouter = activityDay.DivisionRouter != null ? activityDay.DivisionRouter : activityDay.EventReservation.DivisionRouter,
                    DidGroupArrive = activityDay.DidGroupArrive,
                    Telephone = activityDay.Reservation.MobileContact,
                    InstitutionTelephone = activityDay.Reservation.InstitutionTelephone,
                    MuseumEventItemList = new List<EventItem>(),
                    ActivityDay = new IdValuePair(activityDay.Id, ""),
                    EventCode = activityDay.EventReservation.EventCode,
                    Title = activityDay.EventReservation.Title,
                    Department = activityDay.Department,
                };

                if (isExcel)
                {
                    item.BidItem = bidList.FindAll(x => x.ActivityDay.Id == activityDay.Id);
                }

                if (activityDay.Reservation.Institution.Id != "")
                {
                    item.Institution = activityDay.Reservation.Institution;
                }

                if (item.ActualNumberOfVisitors == 0 || item.ActualNumberOfVisitors == 1)
                {
                    item.ActualNumberOfVisitors = item.NumberOfVisitors;
                }

                tempList.Add(item);
            });

            if (sort != null)
            {
                var param = sort[0].Active;
                param = UppercaseFirst(param);
                
                var propertyInfo = typeof(FrontDeskListItem).GetProperty(param);
                if (propertyInfo.PropertyType.Name == "IdValuePair" && sort[0].Direction == "desc") {
                   
                        //tempList.Sort((a, b) => a.Institution.Value.CompareTo(b.Institution.Value));
                        tempList = tempList.OrderByDescending(x => GetPropertyValue(x, param + ".Value")).ToList();
                }
                else if (propertyInfo.PropertyType.Name == "IdValuePair")
                    {
                    tempList = tempList.OrderBy(x => GetPropertyValue(x, param + ".Value")).ToList();
                }
                else if (sort[0].Direction == "desc") {
                    //tempList = tempList.OrderByDescending(x => x.StartDateTimeTemp).ToList();
                    tempList = tempList.OrderByDescending(x => propertyInfo.GetValue(x, null)).ToList();

                }
                else {
                    // tempList = tempList.OrderBy(x => x.StartDateTimeTemp).ToList();
                    tempList = tempList.OrderBy(x => propertyInfo.GetValue(x, null)).ToList();
                    
                }

            }
            // s.GetType().GetProperty("PropertyName").GetValue(s)

            return new Tuple<int, List<FrontDeskListItem>>(tempList.Count, tempList);

        }

        public static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                foreach (var prop in propertyName.Split('.').Select(s => obj.GetType().GetProperty(s)))
                {
                    obj = prop.GetValue(obj, null);
                }
                return obj;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }

        public Tuple<int, List<FrontDeskListItem>> GetListByResource(string userId, bool isAggregate, bool isExcel, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            string fetchXml = "";

            sort.ForEach(x =>
            {
                if (x.Active.Contains(":"))
                {
                    x.Alias = x.Active.Split(':')[1];
                    x.Active = x.Active.Split(':')[0];
                }
                else
                {
                    x.Alias = "EventItem";
                }
                if (x.Active == "startDateTime")
                    x.Active = "start";
                if (x.Active == "endDateTime")
                    x.Active = "end";
            });

            filter.ForEach(x =>
            {
                if (x.Field == "startDateTime")
                    x.Field = "start";
                if (x.Field == "endDateTime")
                    x.Field = "end";
            });

            List<Sort> eventItemSort = helperService.BuildSorting<EventItem, EventItemCRM>(sort.FindAll(x => x.Alias == "EventItem"));
            List<Filter> eventItemFilters = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(x => x.Alias == "EventItem"));

            List<Sort> activityDaySort = null;//helperService.BuildSorting<ActivityDay, ActivityDayCRM>(sort.FindAll(x => x.Alias == "ActivityDay"));
            //List<Sort> additionalSort = null;//new List<Sort>();

            //additionalSort.AddRange(helperService.BuildSorting<Reservation, ReservationCRM>(sort.FindAll(x => x.Alias == "Reservation")));
            //additionalSort.AddRange(helperService.BuildSorting<Institution, InstitutionCRM>(sort.FindAll(x => x.Alias == "Institution")));

            List<Filter> activityDayFilters = helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay"));
            activityDayFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation")));

           // List<Filter> BudgetframeworkFilters = helperService.BuildFilter<BudgetFramework, BudgetFrameworkCRM, BudgetFrameworkCRM>(filter.FindAll(x => x.Alias == "Budgetframework"));
            /*if(BudgetframeworkFilters.Count>0)
            {
                BudgetframeworkFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation")));

            }
            */

            //List<Filter> eventRegistrationFilter = new List<Filter>(filter.FindAll(x => x.Alias == "Reservation" && x.Type == "global"));
            //filter.FindAll(x => x.Alias == "Reservation" && x.Type == "global");
            //eventRegistrationFilter.ForEach(x => x.Alias = "EventRegistration");
            Filter fullGlobalFilter = filter.Find(x => x.Alias == "Reservation|EventReservation" && x.Type == "global");
            if (fullGlobalFilter != null)
            {
                List<Filter> fullGlobalFilters = new List<Filter>();
                fullGlobalFilters.Add(new Filter
                {
                    Field = "ReservationNum",
                    Value = fullGlobalFilter.Value,
                    Type = "global",
                    Alias = "Reservation"
                });
                fullGlobalFilters.Add(new Filter
                {
                    Field = "EventCode",
                    Value = fullGlobalFilter.Value,
                    Type = "global",
                    Alias = "EventReservation"
                });
                activityDayFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(fullGlobalFilters));

            }

            if (filter.FindAll(x => (x.Alias == "Reservation" || x.Alias == "Reservation|EventReservation") && x.Type == "global").Count > 0)
            {
                List<Filter> eventRegistrationFilter = new List<Filter>();

                eventRegistrationFilter.Add(new Filter
                {
                    Field = filter.Find(x => (x.Alias == "Reservation" || x.Alias == "Reservation|EventReservation") && x.Type == "global").Field,
                    Value = "",
                    Type = "global",
                    Alias = "EventRegistration"
                });
                activityDayFilters.AddRange(helperService.BuildFilter<EventRegistration, EventRegistrationCRM, EventRegistrationCRM>(eventRegistrationFilter));
            }

            activityDayFilters.AddRange(helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filter.FindAll(x => x.Alias == "Institution")));

            if (isAggregate)
            {
                fetchXml = @"<attribute name='new_number_of_participants' alias='new_number_of_participants' aggregate='sum' />";

            }
            else
            {
                fetchXml = @"<attribute name='new_event_itemid' />
                <attribute name='new_name' />
                <attribute name='new_end_datetime' />
                <attribute name='new_start_datetime' />
                <attribute name='new_status' />
                <attribute name='new_event_type' />
                <attribute name='new_activity_day' />
                <attribute name='new_reservation' />
                <attribute name='new_comments' />
                <attribute name='new_number_of_participants' />";
            }
           

            //ובתוכו:
            string eventReservationFetchXml = fetchXml + activityDayListRepository.GetLinkedEntity(isAggregate, activityDayFilters, activityDaySort, "", "EventReservationItem");

            fetchXml += activityDayListRepository.GetLinkedEntity(isAggregate, activityDayFilters, activityDaySort, "inner");
          

            Tuple<int, List<FrontDeskListItem>> tempReservations = frontDeskResourceRepository.GetFetchXml(userId, isAggregate, paging, eventItemSort, eventItemFilters, fetchXml);
            Tuple<int, List<FrontDeskListItem>> tempEventReservations = frontDeskResourceRepository.GetFetchXml(userId, isAggregate, paging, eventItemSort, eventItemFilters, eventReservationFetchXml);

            if (isAggregate)
            {
                tempReservations.Item2[0].NumberOfGroups += tempEventReservations.Item2[0].NumberOfGroups;
                tempReservations.Item2[0].NumberOfVisitors += tempEventReservations.Item2[0].NumberOfVisitors;
                //tempReservations.Item2[0].NumOfAudioGuides += tempEventReservations.Item2[0].NumOfAudioGuides;
                //tempReservations.Item2[0].NumOfHeadsets += tempEventReservations.Item2[0].NumOfHeadsets;
                tempReservations.Item2[0].NumOfServiceProvidersNeeded += tempEventReservations.Item2[0].NumOfServiceProvidersNeeded;
                tempReservations.Item2[0].NumOfServiceProvidersScheduled += tempEventReservations.Item2[0].NumOfServiceProvidersScheduled;
            }
            else
            {
                tempReservations.Item2.AddRange(tempEventReservations.Item2);
            }

            int total = tempReservations.Item1 + tempEventReservations.Item1;
            List<FrontDeskListItem> templist = tempReservations.Item2;
            var property = typeof(FrontDeskListItem).GetProperty(queryHelperRepository.FirstCharToUpper(sort[0].Active));

            if (sort.Count>0)
            {
                if (sort[0].Active == "new_start_datetime")
                    property = typeof(FrontDeskListItem).GetProperty("StartDateTimeTemp");
                if (sort[0].Active == "new_end_datetime")
                    property = typeof(FrontDeskListItem).GetProperty("EndDateTimeTemp");
                if (sort[0].Direction == "desc")
                {
                    templist = templist.OrderByDescending(x => property.GetValue(x)).ToList();
                }
                else
                {
                    templist = templist.OrderBy(x => property.GetValue(x)).ToList();
                }
            }

            Tuple<int, List<FrontDeskListItem>> temp = new Tuple<int, List<FrontDeskListItem>>(total, templist);

            /*if (isAggregate)
            {
                fetchXml = @"<attribute name='new_amount' alias='new_amount' aggregate='sum' />
                            <attribute name='new_yv_product' alias='new_yv_product' groupby='true' />";

                List<Filter> tmpFilter = activityDayFilters.FindAll(x => x.Alias == "ActivityDay");
                tmpFilter.AddRange(eventItemFilters.FindAll(x => x.Field.Contains("date")));

                fetchXml += activityDayListRepository.GetShortLinkedEntity(isAggregate, tmpFilter, null);
               
                List<BidItem> bidList = bidItemRepository.GetFetchXml(userId, isAggregate, null, null, null, fetchXml).Item2;
                var headsetProductGuid = ConfigurationManager.AppSettings[$"headsetProductId_{environment}"];
                var audioGuideProductGuid = ConfigurationManager.AppSettings[$"audioGuideProductId_{environment}"];

                temp.Item2[0].NumOfAudioGuides = bidList.Find(x => x.Product.Id.ToLower() == audioGuideProductGuid.ToLower()) != null ? bidList.Find(x => x.Product.Id.ToLower() == audioGuideProductGuid.ToLower()).Amount : 0;
                temp.Item2[0].NumOfHeadsets = bidList.Find(x => x.Product.Id.ToLower() == headsetProductGuid.ToLower()) != null ? bidList.Find(x => x.Product.Id.ToLower() == headsetProductGuid.ToLower()).Amount : 0;

            }
            else
            {
                //BID ITEM LIST
                fetchXml = @"<attribute name='new_yv_product' />
                    <attribute name='new_price' />
                    <attribute name='new_amount' />
                    <attribute name='new_discount' />
                    <attribute name='new_total_price' />
                    <attribute name='new_discount_type' />
                    <attribute name='new_total_price_after_discount' />
                    <attribute name='new_activity_day' />
                    <attribute name='new_comments' />";

                List<Filter> tmpFilter = activityDayFilters.FindAll(x => x.Alias == "ActivityDay");
                tmpFilter.AddRange(eventItemFilters.FindAll(x => x.Field.Contains("date")));

                fetchXml += activityDayListRepository.GetShortLinkedEntity(isAggregate, tmpFilter, null);
                fetchXml += productRepository.GetLinkedEntity(false, null, null);
                List<BidItem> bidList = bidItemRepository.GetFetchXml(userId, isAggregate, null, null, null, fetchXml).Item2;
                var headsetProductGuid = ConfigurationManager.AppSettings[$"headsetProductId_{environment}"];
                var audioGuideProductGuid = ConfigurationManager.AppSettings[$"audioGuideProductId_{environment}"];

                foreach (FrontDeskListItem item in temp.Item2)
                {
                    item.BidItem = bidList.Where(x => x.ActivityDay.Id.ToLower() == item.ActivityDay.Id.ToLower()).ToList();
                    item.NumOfHeadsets = item.BidItem.Where(x => x.Product.Id.ToLower() == headsetProductGuid.ToLower()).Sum(x => x.Amount);
                    item.NumOfAudioGuides = item.BidItem.Where(x => x.Product.Id.ToLower() == audioGuideProductGuid.ToLower()).Sum(x => x.Amount);
                }
            }
             */
            return temp;
        }


        public override Tuple<int, List<FrontDeskListItem>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter, string type = "", bool getAllItems = false)
        {
            //TODO isAggregate

            List<Sort> additionalSort = new List<Sort>();
            List<Sort> activityDaySort = new List<Sort>();

            if (sort != null)
            {
                sort.ForEach(x =>
                {
                    if (x.Active.Contains(":"))
                    {
                        x.Alias = x.Active.Split(':')[1];
                        x.Active = x.Active.Split(':')[0];
                    }
                });

                activityDaySort = helperService.BuildSorting<ActivityDay, ActivityDayCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "ActivityDay"));
                additionalSort.AddRange(helperService.BuildSorting<Reservation, ReservationCRM>(sort.FindAll(x => x.Alias == "Reservation")));
                additionalSort.AddRange(helperService.BuildSorting<Institution, InstitutionCRM>(sort.FindAll(x => x.Alias == "Institution")));
            }

            List<Filter> activityDayFilters = helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay"));
            List<Filter> additionalFilters = new List<Filter>();
            List<Filter> musemEventItemFilters = new List<Filter>();

            additionalFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation")));
            additionalFilters.AddRange(helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filter.FindAll(x => x.Alias == "Institution")));

            activityDayFilters.ForEach(x =>
            {
                x.Alias = "ActivityDay";
            });

            string fetchXml = @"<attribute name='new_end_datetime'/>
                <attribute name='new_start_datetime' />
                <attribute name='new_number_of_groups' />
                <attribute name='new_reservation' />
                <attribute name='new_activity_day_location' />
                <attribute name='new_activity_day_type' />
                <attribute name='new_activity_dayid' />
                <attribute name='new_activity_day_status' />
                <attribute name='new_total_price_after_discount' />
                <attribute name='new_total_price' />
                <attribute name='new_actual_number_of_participants' />
                <attribute name='new_number_of_participants' />
                <attribute name='new_did_group_arrive' />";

            fetchXml += reservationsRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "inner");

            Tuple<int, List<FrontDeskListItem>> tempFrontDeskList = frontDeskRepository.GetFetchXml(userId, isAggregate, paging, activityDaySort, activityDayFilters, fetchXml);

            Tuple<int, List<FrontDeskListItem>> frontDeskList = new Tuple<int, List<FrontDeskListItem>>(tempFrontDeskList.Item1, tempFrontDeskList.Item2.FindAll(x => x.ReservationNum != ""));
            //SHELLY DOESNT NEED THIS!
            if (type == "FrontDesk")
            {
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

                musemEventItemFilters.AddRange(filter.Where(f => f.Field.Contains("date") || f.Field.Contains("start") || f.Field.Contains("end")).ToList());





                List<EventItem> eventItemList = eventItemRepository.GetFetchXml(userId, isAggregate, null, null, musemEventItemFilters, null).Item2;

                foreach (FrontDeskListItem frontDeskItem in frontDeskList.Item2)
                {
                    frontDeskItem.MuseumEventItemList = eventItemList.Where(x => x.ActivityDay.Id == frontDeskItem.Id).ToList();
                }

            }
            if (type == "ServiceProviders")
            {

                List<Filter> filters = filter.FindAll(x => x.Alias == "EventItem");
                filters = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filters);
                filters.AddRange(filter.Where(f => f.Field.Contains("date") || f.Field.Contains("start") || f.Field.Contains("end")).ToList());

                fetchXml = @"<attribute name='new_name' />
                <attribute name='new_end_datetime' />
                <attribute name='new_start_datetime' />
                <attribute name='new_comments' />
                <attribute name='new_activity_day' />";

                fetchXml += serviceProviderRepository.GetLinkedEntity(isAggregate, null, null);

                List<FullEventItem> serviceProviderEventList = fullEventItemRepository.GetFetchXml(userId, isAggregate, null, null, filters, fetchXml).Item2;

                foreach (FrontDeskListItem frontDeskItem in frontDeskList.Item2)
                {
                    frontDeskItem.ServiceProviderList = serviceProviderEventList.Where(x => x.ActivityDay.Id == frontDeskItem.Id).ToList();
                }

            }

            return frontDeskList;

        }


        public Tuple<int, List<FrontDeskListItem>> GetSoundSystemList(string userId, bool isAggregate, bool isExcel, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            string fetchXml = "";
            //devora, do i need the if inside?
            sort.ForEach(x =>
            {
                if (x.Active.Contains(":"))
                {
                    x.Alias = x.Active.Split(':')[1];
                    x.Active = x.Active.Split(':')[0];
                }
                else
                {
                    x.Alias = "ItineraryItem";
                }
            });

            List<Sort> itineraryItemSort = helperService.BuildSorting<ItineraryItem, ItineraryItemCRM>(sort.FindAll(x => x.Alias == "ItineraryItem"));
            List<Filter> itineraryItemFilters = helperService.BuildFilter<ItineraryItem, ItineraryItemCRM, ItineraryItemCRM>(filter.FindAll(x => x.Alias == "ItineraryItem"));
            List<Filter> additionalFilters = new List<Filter>();
            List<Sort> additionalSort = new List<Sort>();

            Filter fullGlobalFilter = filter.Find(x => x.Alias == "Reservation|EventReservation" && x.Type == "global");
            if (fullGlobalFilter != null)
            {
                List<Filter> fullGlobalFilters = new List<Filter>();
                fullGlobalFilters.Add(new Filter
                {
                    Field = "ReservationNum",
                    Value = fullGlobalFilter.Value,
                    Type = "global",
                    Alias = "Reservation"
                });
                fullGlobalFilters.Add(new Filter
                {
                    Field = "EventCode",
                    Value = fullGlobalFilter.Value,
                    Type = "global",
                    Alias = "EventReservation"
                });
                additionalFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(fullGlobalFilters));
            }
            additionalSort.AddRange(helperService.BuildSorting<ActivityDay, ActivityDayCRM>(sort.FindAll(x => x.Alias == "ActivityDay")));

            additionalFilters.AddRange(helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation")));
           

            additionalFilters.AddRange(helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay")));
            additionalFilters.AddRange(helperService.BuildFilter<BudgetFramework, BudgetFrameworkCRM, BudgetFrameworkCRM>(filter.FindAll(x => x.Alias == "Budgetframework")));


            fetchXml += @"<all-attributes />
                            <filter type='and'>
                                <condition attribute='statecode' operator='eq' value='0' />
                                <filter type='or'>
                                    <condition attribute='new_is_need_headset' operator='eq' value='1' />
                                    <condition attribute='new_is_need_audio_guide' operator='eq' value='1' />
                                </filter>
                            </filter>";

            fetchXml += @"<link-entity name='new_event_item' from='new_event_itemid' to='new_scheduler_service_provider' visible='false' link-type='outer' alias='SchedulerServiceProvider'>
                            <filter type='and'>
                                <condition attribute='statecode' operator='eq' value='0' />
                            </filter>
                            <link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' visible='false' link-type='outer' alias='ServiceProvider'>
                                <filter type='and'>
                                    <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
		                        <attribute name='new_mobile' />		                        
                                <attribute name='new_first_name_he' />
                                <attribute name='new_last_name_he' />
                            </link-entity>
                          </link-entity>";

            fetchXml += @"<link-entity name='new_event_item' from='new_event_itemid' to='new_scheduler_resource' visible='false' link-type='outer' alias='SchedulerResource'>
                            <attribute name='new_number_of_participants' />
                            <attribute name='new_event_itemid' />
                            <filter type='and'>
                                <condition attribute='statecode' operator='eq' value='0' />
                            </filter>
                            <link-entity name='new_resource' from='new_resourceid' to='new_resource' visible='false' link-type='outer' alias='Resource'>
                                <filter type='and'>
                                    <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
		                        <attribute name='new_name' />
                                <attribute name='new_resourceid' />
                            </link-entity>
                          </link-entity>";

            fetchXml += @"<link-entity name='new_new_itinerary_item_new_itinerary_group' from='new_itinerary_itemid' to='new_itinerary_itemid' >
                            <link-entity name='new_itinerary_group' from='new_itinerary_groupid' to='new_itinerary_groupid' alias='ItineraryGroup'>
                                <all-attributes />
                                <filter type='and'>
                                    <condition attribute='statecode' operator='eq' value='0' />
                                </filter>";

            string eventReservationFetchXml = fetchXml + activityDayListRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "", "EventReservationItem");
            eventReservationFetchXml += "</link-entity></link-entity>";

            fetchXml += activityDayListRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "", "FrontDeskListItem");
            fetchXml += "</link-entity></link-entity>";

            Tuple<int, List<FrontDeskListItem>> tempFrontDeskList = frontDeskSoundSystemRepository.GetFetchXml(userId, isAggregate, paging, itineraryItemSort, itineraryItemFilters, fetchXml);
           Tuple<int, List<FrontDeskListItem>> tempEventReservations = frontDeskSoundSystemRepository.GetFetchXml(userId, isAggregate, paging, itineraryItemSort, itineraryItemFilters, eventReservationFetchXml);
            tempFrontDeskList.Item2.AddRange(tempEventReservations.Item2);

            List<FrontDeskListItem> frontDeskList = new List<FrontDeskListItem>();
            var frontDeskListGrouped = tempFrontDeskList.Item2.GroupBy(item => item.Id);
            foreach (var group in frontDeskListGrouped)
            {
                int sum = 0;
                int sumActual = 0;
                int sumHeadsets = 0;
                int sumAudioGuides = 0;
                bool isFirst = true;
                FrontDeskListItem tempItem = new FrontDeskListItem();
                foreach (FrontDeskListItem item in group)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        tempItem = item;
                    }
                    sum += item.NumberOfVisitors;
                    sumActual += item.ActualNumberOfVisitors;
                    sumHeadsets += item.NumOfHeadsets;
                    sumAudioGuides += item.NumOfAudioGuides;
                }
                tempItem.NumberOfVisitors = sum;
                tempItem.ActualNumberOfVisitors = sumActual;
                tempItem.NumOfHeadsets = sumHeadsets;
                tempItem.NumOfAudioGuides = sumAudioGuides;

                frontDeskList.Add(tempItem);
            }
            if(frontDeskList.Count()>1)
               frontDeskList=frontDeskList.OrderBy(x => Convert.ToDateTime(x.StartDateTime)).ToList();
            return new Tuple<int, List<FrontDeskListItem>>(frontDeskList.Count, frontDeskList);
        }

        
        public Tuple<int, List<SoundSystemItem>> GetSeminarsSoundSystemList(string userId, bool isAggregate, bool isExcel, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            string fetchXml = "";
            //devora, do i need the if inside?
            sort.ForEach(x =>
            {
                if (x.Active.Contains(":"))
                {
                    x.Alias = x.Active.Split(':')[1];
                    x.Active = x.Active.Split(':')[0];
                }
                else
                {
                    x.Alias = "SoundSystem";
                }
            });

            List<Sort> soundSystemSort = helperService.BuildSorting<SoundSystemItem, SoundSystemItemCRM>(sort.FindAll(x => x.Alias == "SoundSystem"));
            List<Filter> soundSystemFilters = helperService.BuildFilter<SoundSystemItem, SoundSystemItemCRM, SoundSystemItemCRM>(filter.FindAll(x => x.Alias == "SoundSystem"));

            List<Filter> additionalFilters = new List<Filter>();
            List<Sort> additionalSort = new List<Sort>();

            additionalFilters.AddRange(helperService.BuildFilter<EventReservationItem, EventReservationCRM, EventReservation>(filter.FindAll(x => x.Alias == "EventReservation")));
            additionalSort.AddRange(helperService.BuildSorting<ActivityDay, ActivityDayCRM>(sort.FindAll(x => x.Alias == "EventReservation")));

            fetchXml = @"<all-attributes />" + eventReservationRepository.GetLinkedEntity(isAggregate, additionalFilters.FindAll(x => x.Alias == "EventReservation").ToList(), additionalSort.FindAll(x => x.Alias == "AcEventReservationtivityDay").ToList());
            return soundSystemItemRepository.GetFetchXml(userId, isAggregate, paging, soundSystemSort, soundSystemFilters, fetchXml);
        }

        public void UpdateActivityDayActualNumberOfVisitors(string userId, string activityDayGuid, int difference)
        {
            ActivityDay activityDayItem = activityDayRepository.GetFetchXml(userId, activityDayGuid);

            int actualNumberOfVisitors = activityDayItem.NumberOfVisitors + difference;
            UpdateCrmItem item = new UpdateCrmItem
            {
                Field = "ActualNumberOfVisitors",
                Value = actualNumberOfVisitors.ToString(),
                Type = "int",
                Alias = "ActivityDay"
            };
            activityDayService.UpdateField(activityDayGuid, item, userId);
        }

        public List<FullEventItem> GetServiceProviderList(string userId, string activityDayGuid)
        {

            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("ActivityDay"),
                Value = activityDayGuid,
                Type = "lookup"
            });
            filters.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("IsResource"),
                Value = "false",
                Type = "boolean"
            });
            filters.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventStatus"),
                Value = ((int)EventStatus.Approved).ToString(),
                Type = "optionset"
            });

            string fetchXml = @"<attribute name='new_name' />
                <attribute name='new_end_datetime' />
                <attribute name='new_start_datetime' />
                    <attribute name='new_comments' />";

            fetchXml += serviceProviderRepository.GetLinkedEntity(false, null, null);
            fetchXml += @"<filter type='or'>
                            <condition entityname='ItineraryItem' attribute='new_scheduler_service_provider' operator='not-null' />
                            <condition entityname='ItineraryGroup' attribute='new_guide_schedule_event' operator='not-null' />
                         </filter> 
                    <link-entity name='new_itinerary_item' from='new_scheduler_service_provider' to='new_event_itemid' alias='ItineraryItem' link-type='outer'></link-entity>
                    <link-entity name='new_itinerary_group' from='new_guide_schedule_event' to='new_event_itemid' alias='ItineraryGroup' link-type='outer'></link-entity>
                    ";

            List<FullEventItem> serviceProviderEventList = fullEventItemRepository.GetFetchXml(userId, false, null, null, filters, fetchXml).Item2;
            //serviceProviderEventList = serviceProviderEventList.GroupBy(x => x.ServiceProvider.Id).Select(y => y.First()).ToList();
            /*   serviceProviderEventList= serviceProviderEventList.GroupBy(x => new { x.ServiceProvider.Id, x.Start })
         .Select(group => group.First())  // instead of First you can also apply your logic here what you want to take, for example an OrderBy
         .ToList();*/

            return serviceProviderEventList;


        }
        public List<FrontDeskListItem> GetActivityDayListWithServiceProviders(string userId, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            List<Filter> activityDayFilter = helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay"));
            List<Filter> reservationFilter = helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "Reservation"));
            activityDayFilter.AddRange(reservationFilter);

            //Tuple<int, List<FrontDeskListItem>> activityDayList = GetFetchXml(userId, null, sort, activityDayFilter);


            Tuple<int, List<FrontDeskListItem>> activityDayList = GetFetchXmlForExcel(userId, null, null, filter, "ActivityDayListWithServiceProviders");

            filter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(x => x.Alias == "EventItem"));

            List<FullEventItem> eventItemList = fullEventItemRepository.GetFetchXml(userId, false, null, null, filter, null).Item2;

            foreach (FrontDeskListItem activityDayListItem in activityDayList.Item2)
            {
                activityDayListItem.ServiceProviderList = eventItemList.Where(x => x.ActivityDay.Id == activityDayListItem.Id).ToList();
            }

            return null;// activityDayList;
        }
        public List<ItineraryGroup> GetGroupsList(string userId, string eventItemId)
        {
            List<ItineraryGroup> itineraryGroupList = new List<ItineraryGroup>();
            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("SchedulerResource"),
                Value = eventItemId,
                Type = "lookup"
            });

            //List<FullItineraryItem> itineraryItemList = fullItineraryItemRepository.GetFetchXml(userId, null, null, filters, null).Item2;
            List<ItineraryItem> itineraryItemList = itineraryItemRepository.GetFetchXml(userId, false, null, null, filters, null).Item2;

            string relationshipName = mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("ItineraryGroup");
            string parentEntityName = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryItem");

            //string serviceProviderFetchXml = serviceProviderRepository.GetLinkedEntity(isAggregate, null, null);

            itineraryItemList.ForEach(itineraryItem =>
            {
                var linkedGroups = itineraryGroupRepository.GetLinkedItemListFetchXml(userId, relationshipName, parentEntityName, itineraryItem.Id);
                itineraryGroupList.AddRange(linkedGroups);
            });
            // Get n:n linked
            itineraryGroupList.ForEach(itineraryGroup =>
            {

                List<Sort> sorts = new List<Sort>();
                Sort sort = new Sort
                {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<Contact, EventRegistrationCRM>("LastName"),
                    Direction = "asc"
                };
                sorts.Add(sort);
                sort = new Sort
                {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<Contact, EventRegistrationCRM>("FirstName"),
                    Direction = "asc"
                };
                sorts.Add(sort);
                relationshipName = mapFunctionRepository.GetDestinationPropertyFor<ItineraryGroup, ItineraryGroupCRM>("EventRegistrationList");
                parentEntityName = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryGroup");
                itineraryGroup.EventRegistrationList = eventRegistrationRepository.GetLinkedItemListFetchXml(userId, relationshipName, parentEntityName, itineraryGroup.Id, sorts);
                //itineraryGroup.GuideScheduleEvent
                itineraryGroup.GuideInfo = serviceProviderRepository.GetFetchXml(userId, itineraryGroup.GuideScheduleEvent.ScheduleItem.Id);
            });
            return itineraryGroupList;


        }

        /*  public decimal GetTotalPriceForList(string userId, List<Filter> filterList) {
              decimal totalPrice = 0;
              return totalPrice;
          }*/

        public List<BidItem> GetBidList(string userId, string activityDayGuid)
        {

            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<BidItem, BidItemCRM>("ActivityDay"),
                Value = activityDayGuid,
                Type = "lookup"
            });

            List<BidItem> bidList = bidItemRepository.GetFetchXml(userId, false, null, null, filters).Item2;
            return bidList;

        }

        public List<EventItem> GetMuseumList(string userId, string activityDayGuid)
        {

            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("ActivityDay"),
                Value = activityDayGuid,
                Type = "lookup"
            });

            filters.Add(new Filter()
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("IsResource"),
                Value = "true",
                Type = "boolean"
            });

            filters.Add(new Filter()
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Resource"),
                Value = ConfigurationManager.AppSettings[$"museumResourceId_{environment}"],
                Type = "lookup"
            });

            List<EventItem> eventItemList = eventItemRepository.GetFetchXml(userId, false, null, null, filters).Item2;
            return eventItemList;

        }
    }


}
