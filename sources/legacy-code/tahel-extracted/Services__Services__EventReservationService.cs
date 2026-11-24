using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Data.SendMailWS;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Services.Services.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class EventReservationService : BasicService<EventReservationItem, EventReservation, EventReservationCRM>, IEventReservationService
    {
        private readonly IActivityDaySubjectRepository activityDaySubjectRepository;
        private readonly IBidItemRepository bidItemRepository;
        private readonly IItineraryGroupRepository itineraryGroupRepository;
        private readonly IEventItemRepository eventItemRepository;
        private readonly IActivityDayRepository activityDayRepository;
        private readonly IActivityDayListRepository activityDayListRepository;
        private readonly IEventReservationRepository eventReservationRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IIdValueListRepository idValueListRepository;
        private readonly IReservationsService reservationsService;
        private readonly ItineraryItemRepository itineraryItemRepository;
        private readonly IAttachmentRepository attachmentRepository;
        private readonly IBudgetFrameworkRepository budgetFrameworkRepository;
        private readonly IFoodItemRepository foodItemRepository;
        private readonly IHotelReservationRepository hotelReservationRepository;
        private readonly IHotelReservationItemRepository hotelReservationItemRepository;
        private readonly IHotelReservationDetailsRepository hotelReservationDetailsRepository;
        private readonly ISoundSystemItemRepository soundSystemItemRepository;
        private readonly ITransportationRepository transportationRepository;
        private readonly ICouponRepository couponRepository;
        private readonly IHotelSpecialRepository hotelSpecialRepository;
        private readonly IHotelRateRepository hotelRateRepository;
        private readonly IAlmogRepository almogRepository;
        private readonly ILearningDivisionRepository learningDivisionRepository;
        private readonly IWorkshopRepository workshopRepository;
        protected readonly ICRMConnection crmConnection;



        private readonly string environment;

        public Objects Objects { get; } = new Objects();

        public EventReservationService(IHotelRateRepository hotelRateRepository , 
            IReservationsService reservationsService,
            IBidItemRepository bidItemRepository,
            IItineraryGroupRepository itineraryGroupRepository,
            IActivityDaySubjectRepository activityDaySubjectRepository,
            IEventItemRepository eventItemRepository,
            IActivityDayRepository activityDayRepository,
            IIdValueListRepository idValueListRepository,
            IActivityDayListRepository activityDayListRepository,
            IHelperService helperService,
            IEventReservationRepository eventReservationRepository,
            IMapFunctionRepository mapFunctionRepository,
            ItineraryItemRepository itineraryItemRepository,
            IAttachmentRepository attachmentRepository,
            IBudgetFrameworkRepository budgetFrameworkRepository,
            IFoodItemRepository foodItemRepository,
            IHotelReservationRepository hotelReservationRepository,
            IHotelReservationItemRepository hotelReservationItemRepository,
            IHotelReservationDetailsRepository hotelReservationDetailsRepository,
            ISoundSystemItemRepository soundSystemItemRepository,
            ITransportationRepository transportationRepository,
            IHotelSpecialRepository hotelSpecialRepository,
            IAlmogRepository almogRepository,
            ILearningDivisionRepository learningDivisionRepository,
            IWorkshopRepository workshopRepository,
            ICouponRepository couponRepository,
            ICRMConnection crmConnection

           )
            : base(helperService, eventReservationRepository, mapFunctionRepository)
        {
            this.hotelRateRepository = hotelRateRepository;
            this.attachmentRepository = attachmentRepository;
            this.itineraryItemRepository = itineraryItemRepository;
            this.reservationsService = reservationsService;
            this.activityDaySubjectRepository = activityDaySubjectRepository;
            this.bidItemRepository = bidItemRepository;
            this.itineraryGroupRepository = itineraryGroupRepository;
            this.eventItemRepository = eventItemRepository;
            this.activityDayRepository = activityDayRepository;
            this.idValueListRepository = idValueListRepository;
            this.activityDayListRepository = activityDayListRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.eventReservationRepository = eventReservationRepository;
            this.budgetFrameworkRepository = budgetFrameworkRepository;
            this.foodItemRepository = foodItemRepository;
            this.hotelReservationRepository = hotelReservationRepository;
            this.hotelReservationDetailsRepository = hotelReservationDetailsRepository;
            this.hotelReservationItemRepository = hotelReservationItemRepository;
            this.soundSystemItemRepository = soundSystemItemRepository;
            this.transportationRepository = transportationRepository;
            this.hotelSpecialRepository = hotelSpecialRepository;
            this.almogRepository = almogRepository;
            this.learningDivisionRepository = learningDivisionRepository;
            this.workshopRepository = workshopRepository;
            this.couponRepository = couponRepository;
            this.crmConnection = crmConnection;
            environment = ConfigurationManager.AppSettings["environment"];
        }

        public override void Update(EventReservationItem item, EventReservationItem oldItem, string userId)
        {
            eventReservationRepository.Update(item, oldItem, userId);
            CreateOrUpdateEventReservation(item, oldItem, userId);

        }

        public override Guid Create(EventReservationItem item, string userId)
        {
            /*if (item.ReservationStatus == null)
            {
                IdValuePair status = new IdValuePair();
                status.Id = ((int)ReservationStatus.New).ToString();
                item.ReservationStatus = status;
            }*/

            var guid = eventReservationRepository.Create(item, userId);
            item.Id = guid.ToString();
            item.EventCode = eventReservationRepository.GetEventCode(guid, userId);
            CreateOrUpdateEventReservation(item, null, userId, true);
            return guid;
        }

        public override void Cancel(EventReservationItem item, string userId, bool isCancel = true)
        {
            InheritIsCanceledToChildrenEntities(item);
            eventReservationRepository.Cancel(item, userId, isCancel);
            CancelEventReservationEntities(item, userId);
        }

        private void InheritIsCanceledToChildrenEntities(object item)
        {
            bool toCancel = ((BasicEntity)item).IsCanceled;
            PropertyInfo[] properties = item.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                {
                    var value = property.GetValue(item);
                    if (value != null)
                    {
                        Type type = property.PropertyType;
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type itemType = type.GetGenericArguments()[0]; // use this...
                                                                           //ConvertList(property.GetValue(item) as , itemType).Dump();
                            if (itemType.BaseType == typeof(BasicEntity) || itemType.BaseType.BaseType == typeof(BasicEntity))
                            {
                                foreach (var entity in value as ICollection)
                                {
                                    if (toCancel)
                                    {
                                        ((BasicEntity)entity).IsCanceled = true;
                                    }
                                    InheritIsCanceledToChildrenEntities(entity);
                                }
                            }
                        }
                        // אם זה אובייקט בודד שהוא BasicEntity — נסמן - להשאיר את זה ואם לא רוצים שמשהו יתבטל אז לשנות בקוד שלא יגיעו אליו ויעשו לו cancel
                        else if (type.BaseType == typeof(BasicEntity) || type.BaseType.BaseType == typeof(BasicEntity))
                        {
                            var entity = value as BasicEntity;
                            if (entity != null)
                            {
                                if (toCancel)
                                {
                                    entity.IsCanceled = true;
                                }

                                // רקורסיה על האובייקט הבודד
                                InheritIsCanceledToChildrenEntities(entity);
                            }
                        }
                    }
                }
            }
        }

        private void CancelEventReservationEntities(EventReservationItem item, string userId)
        {
            foreach (var activity in item.ActivityDayList)
            {
                activityDayRepository.Cancel(activity, userId);

                foreach (var itineraryGroup in activity.ItineraryGroupList)
                {
                    //if (itineraryGroup.GuideScheduleEvent != null)// was sent
                    //{
                    //    eventItemRepository.Cancel(itineraryGroup.GuideScheduleEvent, userId);
                    //}
                    if (itineraryGroup.RoomScheduleEvent != null)// was sent
                    {
                        eventItemRepository.Cancel(itineraryGroup.RoomScheduleEvent, userId);
                        itineraryGroup.RoomScheduleEvent = null;
                    }
                    //itineraryGroupRepository.Delete(itineraryGroup, userId);
                    foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                    {
                        if (itineraryItem.SchedulerResource != null)
                        {
                            eventItemRepository.Cancel(itineraryItem.SchedulerResource, userId);
                            itineraryItem.SchedulerResource = null;
                        }
                        //if (itineraryItem.SchedulerServiceProvider != null)
                        //{
                        //    eventItemRepository.Cancel(itineraryItem.SchedulerServiceProvider, userId);
                        //}
                        //itineraryItemRepository.Delete(itineraryItem, userId);

                    }
                }

                foreach (var learningDivision in activity.LearningDivisionsList)
                {
                    if (learningDivision.RoomScheduleEvent != null)// was sent
                    {
                        eventItemRepository.Cancel(learningDivision.RoomScheduleEvent, userId);
                        learningDivision.RoomScheduleEvent = null;
                    }
                    //foreach (var guidScheduleEvent in learningDivision.GuideScheduleEventsList)
                    //{
                    //    eventItemRepository.Cancel(guidScheduleEvent, userId);
                    //}
                    foreach (var workshop in learningDivision.WorkshopsList)
                    {
                        if (workshop.RoomScheduleEvent != null)
                        {
                            eventItemRepository.Cancel(workshop.RoomScheduleEvent, userId);
                            workshop.RoomScheduleEvent = null;
                        }
                        //foreach(var guidScheduleEvent in workshop.GuideScheduleEventsList)
                        //{
                        //    eventItemRepository.Cancel(guidScheduleEvent, userId);
                        //}
                        //workshopRepository.Delete(workshop, userId);

                        foreach (var itineraryGroup in workshop.ItineraryGroupList)
                        {
                            //if (itineraryGroup.GuideScheduleEvent != null)// was sent
                            //{
                            //    eventItemRepository.Cancel(itineraryGroup.GuideScheduleEvent, userId);
                            //}
                            if (itineraryGroup.RoomScheduleEvent != null)// was sent
                            {
                                eventItemRepository.Cancel(itineraryGroup.RoomScheduleEvent, userId);
                                itineraryGroup.RoomScheduleEvent = null;
                            }
                            //itineraryGroupRepository.Delete(itineraryGroup, userId);

                            foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                            {
                                if (itineraryItem.SchedulerResource != null)
                                {
                                    eventItemRepository.Cancel(itineraryItem.SchedulerResource, userId);
                                    itineraryItem.SchedulerResource = null;
                                }
                                //if (itineraryItem.SchedulerServiceProvider != null)
                                //{
                                //    eventItemRepository.Cancel(itineraryItem.SchedulerServiceProvider, userId);
                                //}
                                //itineraryItemRepository.Delete(itineraryItem, userId);

                            }
                        }
                    }
                }
                foreach (var bidItem in activity.BidItemList)
                {
                    bidItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    bidItemRepository.Cancel(bidItem, userId);
                }
                foreach (var reserveItem in activity.ReserveResourcesList)
                {
                    eventItemRepository.Cancel(reserveItem, userId);
                }
                foreach (var reserveItem in activity.EventItemsList)
                {
                    reserveItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    eventItemRepository.Cancel(reserveItem, userId);
                }
            }
        }

        public void DeleteEventReservationEntities(EventReservationItem item, string userId)
        {
            foreach (var activity in item.ActivityDayList)
            {
                activityDayRepository.Delete(activity, userId);

                foreach (var itineraryGroup in activity.ItineraryGroupList)
                {

                    if (itineraryGroup.GuideScheduleEvent != null)
                    {
                        eventItemRepository.Delete(itineraryGroup.GuideScheduleEvent, userId);
                    }

                    if (itineraryGroup.RoomScheduleEvent != null)// was sent
                    {
                        eventItemRepository.Delete(itineraryGroup.RoomScheduleEvent, userId);
                    }

                    itineraryGroupRepository.Delete(itineraryGroup, userId);
                    foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                    {
                        if (itineraryItem.SchedulerResource != null)
                        {
                            eventItemRepository.Delete(itineraryItem.SchedulerResource, userId);
                        }
                        if (itineraryItem.SchedulerServiceProvider != null)
                        {
                            eventItemRepository.Delete(itineraryItem.SchedulerServiceProvider, userId);
                        }
                        if (itineraryItem.IsDisassociate)
                        {
                            EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryGroup"), Guid.Parse(itineraryGroup.Id));
                            EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryItem"), Guid.Parse(itineraryItem.Id));

                            var relationshipName = mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("ItineraryGroup");
                            eventItemRepository.Disassociate(refa, refb, relationshipName, userId);
                        }
                        itineraryItemRepository.Delete(itineraryItem, userId);

                    }
                }

                foreach (var learningDivision in activity.LearningDivisionsList)
                {
                    if (learningDivision.RoomScheduleEvent != null)// was sent
                    {
                        eventItemRepository.Delete(learningDivision.RoomScheduleEvent, userId);
                    }
                    foreach (var guideScheduleEvent in learningDivision.GuideScheduleEventsList)
                    {
                        eventItemRepository.Delete(guideScheduleEvent, userId);
                    }
                    learningDivisionRepository.Delete(learningDivision, userId);

                    foreach (var workshop in learningDivision.WorkshopsList)
                    {
                        if (workshop.RoomScheduleEvent != null)
                        {
                            eventItemRepository.Delete(workshop.RoomScheduleEvent, userId);
                        }
                        foreach(var guideScheduleEvent in workshop.GuideScheduleEventsList)
                        {
                            eventItemRepository.Delete(guideScheduleEvent, userId);
                        }
                        workshopRepository.Delete(workshop, userId);

                        foreach (var itineraryGroup in workshop.ItineraryGroupList)
                        {

                            if (itineraryGroup.GuideScheduleEvent != null)// was sent
                            {
                                eventItemRepository.Delete(itineraryGroup.GuideScheduleEvent, userId);
                            }

                            if (itineraryGroup.RoomScheduleEvent != null)// was sent
                            {
                                eventItemRepository.Delete(itineraryGroup.RoomScheduleEvent, userId);
                            }
                            itineraryGroupRepository.Delete(itineraryGroup, userId);

                            foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                            {
                                itineraryItemRepository.Delete(itineraryItem, userId);

                                if (itineraryItem.SchedulerResource != null)
                                {
                                    eventItemRepository.Delete(itineraryItem.SchedulerResource, userId);
                                }
                                if (itineraryItem.SchedulerServiceProvider != null)
                                {
                                    eventItemRepository.Delete(itineraryItem.SchedulerServiceProvider, userId);
                                }
                                if (itineraryItem.IsDisassociate)
                                {
                                    EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryGroup"), Guid.Parse(itineraryGroup.Id));
                                    EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryItem"), Guid.Parse(itineraryItem.Id));

                                    var relationshipName = mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("ItineraryGroup");
                                    eventItemRepository.Disassociate(refa, refb, relationshipName, userId);
                                }
                            }
                        }

                    }
                }

                foreach (var bidItem in activity.BidItemList)
                {
                    bidItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    bidItemRepository.Delete(bidItem, userId);
                }

                foreach (var reserveItem in activity.ReserveResourcesList)
                {
                    reserveItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    eventItemRepository.Delete(reserveItem, userId);
                }
                foreach (var reserveItem in activity.EventItemsList)
                {
                    reserveItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    eventItemRepository.Delete(reserveItem, userId);
                }


            }
            if (item.HotelReservationList != null)
            {
                foreach (var hotelReservation in item.HotelReservationList)
                {
                    hotelReservationRepository.Delete(hotelReservation, userId);
                    foreach (var hotelReservationDetails in hotelReservation.HotelReservationDetails)
                    {
                        hotelReservationDetailsRepository.Delete(hotelReservationDetails, userId);
                    }
                    foreach (var hotelReservationItem in hotelReservation.HotelReservationItemList)
                    {
                        hotelReservationItemRepository.Delete(hotelReservationItem, userId);
                    }
                }
            }

            foreach (var attachment in item.AttachmentList)
            {
                attachmentRepository.Delete(attachment, userId);
            }
        }
        public override void Delete(EventReservationItem item, string userId)
        {
            reservationsService.InheritIsDeletedToChildrenEntities(item);
            eventReservationRepository.Delete(item, userId);
            DeleteEventReservationEntities(item, userId);
        }

        private void CreateOrUpdateEventReservation(EventReservationItem item, EventReservationItem oldItem, string userId, bool isCreate = false)
        {
            if (item.EventPartnerList != null) SetIdValueList(Objects.EventReservationsPartnerListRelation, Objects.Supporter, item.Id, item.EventPartnerList, userId, isCreate);
            if (item.EventSupporterList != null) SetIdValueList(Objects.EventReservationsSupporterListRelation, Objects.Supporter, item.Id, item.EventSupporterList, userId, isCreate);
            if (item.EventCountryList != null) SetIdValueList(Objects.EventReservationsCountryListRelation, Objects.Country, item.Id, item.EventCountryList, userId, isCreate);

            try
            {
                reservationsService.SetActivityDays(userId, false, item.DivisionRouter.Id, item.Id, item.ActivityDayList, oldItem?.ActivityDayList);
                if (item.HotelReservationList != null && !isCreate && item.IsLoadHotelReservationList)
                    //&& item.HotelReservationList.Count > 0
                {
                    Trace.TraceError($"{DateTime.Now}: seminar:" +item.EventCode+ " HotelReservationList.Count: "+ item.HotelReservationList.Count());
                    hotelReservationRepository.SetListItem(item.HotelReservationList, oldItem.HotelReservationList, userId, item.Id);
                }
                if (item.FoodList != null&& !isCreate &&item.IsLoadFoodList)
                    //&& item.FoodList.Count > 0)
                {
                    Trace.TraceError($"{DateTime.Now}: semina:" + item.EventCode + " FoodList.Count: " + item.FoodList.Count());
                    foodItemRepository.SetListItem(item.FoodList, oldItem.FoodList, userId, item.Id);
                }
                if (item.BidItemList != null && !isCreate && item.IsLoadMaterialList)
                    //&& item.BidItem.Count > 0)
                {
                    Trace.TraceError($"{DateTime.Now}: seminar:" + item.EventCode + " bidItemList.Count: " + item.BidItemList.Count());
                    bidItemRepository.SetListItem(item.BidItemList, oldItem.BidItemList, userId, item.Id);
                }
                if (item.SoundSystemList != null && !isCreate &&item.IsLoadSoundSystemList)
                    //&& item.SoundSystemList.Count > 0)
                {
                    Trace.TraceError($"{DateTime.Now}: seminar:" + item.EventCode + " SoundSystemList.Count: " + item.SoundSystemList.Count());
                    soundSystemItemRepository.SetListItem(item.SoundSystemList, oldItem.SoundSystemList, userId, item.Id);
                }
                if (item.HotelSpecialList != null && !isCreate &&item.IsLoadHotelSpecialList)
                    //&& item.HotelSpecialList.Count > 0)
                {
                    Trace.TraceError($"{DateTime.Now}: seminar:" + item.EventCode + " HotelSpecialList.Count: " + item.HotelSpecialList.Count());
                    hotelSpecialRepository.SetListItem(item.HotelSpecialList, oldItem.HotelSpecialList, userId, item.Id);
                }
                if (item.TransportationList != null && !isCreate && item.IsLoadTransportationList)
                    //&& item.TransportationList.Count > 0)
                {
                    Trace.TraceError($"{DateTime.Now}: seminar:" + item.EventCode + " TransportationList.Count: " + item.TransportationList.Count());
                    transportationRepository.SetListItem(item.TransportationList, oldItem.TransportationList, userId, item.Id);
                }
                if(item.CouponList != null && !isCreate)
                {
                    couponRepository.SetListItem(item.CouponList, oldItem.CouponList, userId, item.Id);
                }
            }
            catch (Exception ex)
            {
                if (isCreate)
                {
                    eventReservationRepository.DeleteForever(item, userId);
                    item.ActivityDayList.Where(a => !string.IsNullOrEmpty(a.Id)).ToList().ForEach(a => activityDayRepository.DeleteForever(a, userId));
                }
                throw ex;
            }

        }

        private void SetIdValueList(string relationshipName, string obj, string guid, List<IdValuePair> itemList, string userId, bool isCreate)
        {
           
            List<IdValuePair> oldItemList = new List<IdValuePair>();
            Guid.TryParse(guid, out Guid refEventReservationGuid);
            var relationshipNameCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(relationshipName);
            var objectCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj);
            var eventReservationCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("EventReservation");
            if (objectCrm != "new_country")
                oldItemList = idValueListRepository.GetLinkedList<SupporterCRM>(userId, relationshipNameCrm, eventReservationCrm, objectCrm, guid, null);
            else
                oldItemList = idValueListRepository.GetLinkedList<CountryCRM>(userId, relationshipNameCrm, eventReservationCrm, objectCrm, guid, null);

            if (!isCreate)
            {
                //disassociate from list if isnt in new list 
                foreach (var item in oldItemList)
                {
                    Guid.TryParse(item.Id, out Guid refItemListGuid);

                    if (itemList.Where(x => x.Id == item.Id).Count() == 0)
                    {
                        //disassociate
                        EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj), refItemListGuid);
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("EventReservation"), refEventReservationGuid);
                        basicRepository.Disassociate(refa, refb, relationshipNameCrm, userId);
                    }
                }
            }

            foreach (var item in itemList)
            {
                Guid.TryParse(item.Id, out Guid refItemListGuid);

                if (oldItemList.Where(x => x.Id == item.Id).Count() == 0)
                {//is not in old list 
                    if (refItemListGuid != null && refEventReservationGuid != null)
                    {
                        EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj), refItemListGuid);
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("EventReservation"), refEventReservationGuid);
                        basicRepository.Associate(refa, refb, relationshipNameCrm, userId);
                    }
                }
            }
        }


        public string GetModifiedOn(string guid, string userId)
        {
            return eventReservationRepository.GetModifiedOn(guid, userId);
        }


        public List<FoodItem> GetFoodList(string userId, string id)
        {
            Filter filter1 = new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<FoodItem, FoodItemCRM>("EventReservation"),
                Value = id,
                Type = "lookup"
            };
            Sort sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<FoodItem, FoodItemCRM>("StartDateTime"),
                Direction = "asc"
            };
            List<Sort> sorts = new List<Sort>
            {
                sort
            };
            return foodItemRepository.GetFetchXml(userId, false, null, sorts, new List<Filter>() { filter1 }).Item2;

        }
        public List<FoodItem> GetAllFoodListByDates(string userId, Filter filter)
        {
            return foodItemRepository.GetFetchXml(userId, false, null, null, new List<Filter>() { filter }).Item2;
        }

        public List<HotelReservation> GetHotelReservationList(string userId, string id)
        {
            Filter filter1 = new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<HotelReservation, HotelReservationCRM>("EventReservation"),
                Value = id,
                Type = "lookup"
            };
            Sort sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<HotelReservation, HotelReservationCRM>("StartDateTime"),
                Direction = "asc"
            };
            List<Sort> sorts = new List<Sort>
            {
                sort
            };
            return hotelReservationRepository.GetFetchXml(userId, false, null, sorts, new List<Filter>() { filter1 }).Item2;

        }


        public List<LearningDivision> GetLearningDivisionList(string userId, string id)
        {
            Filter filter1 = new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<LearningDivision, LearningDivisionCRM>("EventReservation"),
                Value = id,
                Type = "lookup"
            };
            Sort sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<LearningDivision, LearningDivisionCRM>("StartDateTime"),
                Direction = "asc"
            };
            List<Sort> sorts = new List<Sort>
            {
                sort
            };
            return learningDivisionRepository.GetFetchXml(userId, false, null, sorts, new List<Filter>() { filter1 }).Item2;

        }

        public List<SoundSystemItem> GetSoundSystemList(string userId, string id)
        {
            Filter filter1 = new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<SoundSystemItem, SoundSystemItemCRM>("EventReservation"),
                Value = id,
                Type = "lookup"
            };
            Sort sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<SoundSystemItem, SoundSystemItemCRM>("StartDateTime"),
                Direction = "asc"
            };
            List<Sort> sorts = new List<Sort>
            {
                sort
            };
            return soundSystemItemRepository.GetFetchXml(userId, false, null, sorts, new List<Filter>() { filter1 }).Item2;

        }

        public List<TransportationItem> GetTransportationList(string userId, string id)
        {
            Filter filter1 = new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<TransportationItem, TransportationItemCRM>("EventReservation"),
                Value = id,
                Type = "lookup"
            };
            Sort sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<TransportationItem, TransportationItemCRM>("StartDateTime"),
                Direction = "asc"
            };
            List<Sort> sorts = new List<Sort>
            {
                sort
            };
            return transportationRepository.GetFetchXml(userId, false, null, sorts, new List<Filter>() { filter1 }).Item2;

        }
        public List<BidItem> GetMaterialList(string userId, string id)
        {
            Filter filter1 = new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<BidItem, BidItemCRM>("EventReservation"),
                Value = id,
                Type = "lookup"
            };
            Sort sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<BidItem, BidItemCRM>("Product"),
                Direction = "asc"
            };
            List<Sort> sorts = new List<Sort>
            {
                sort
            };
            return bidItemRepository.GetFetchXml(userId, false, null, sorts, new List<Filter>() { filter1 }).Item2;

        }
        public List<HotelSpecial> GetHotelSpecialList(string userId, string id)
        {
            Filter filter1 = new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<HotelSpecial, HotelSpecialCRM>("EventReservation"),
                Value = id,
                Type = "lookup"
            };
            Sort sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<HotelSpecial, HotelSpecialCRM>("Subject"),
                Direction = "asc"
            };
            List<Sort> sorts = new List<Sort>
            {
                sort
            };
            return hotelSpecialRepository.GetFetchXml(userId, false, null, sorts, new List<Filter>() { filter1 }).Item2;

        }

        public List<HotelRate> GetHotelRatesList(string userId)
        {
            return hotelRateRepository.GetFetchXml(userId, false, null).Item2;
        }

        public override Tuple<int, List<EventReservation>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            string fetchXml = @"<all-attributes/>";
            List<Filter> eventReservationFilter = helperService.BuildFilter<EventReservationItem, EventReservationCRM, EventReservationCRM>(filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "EventReservation"));

            List<Sort> eventReservationSort = null;
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

                eventReservationSort = helperService.BuildSorting<EventReservationItem, EventReservationCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "EventReservation"));
            }

           
           if (!isAggregate)
            {
                List<Filter> budgetFrameworkFilter = helperService.BuildFilter<BudgetFramework, BudgetFrameworkCRM, BudgetFrameworkCRM>(filter.FindAll(x => x.Alias == "BudgetFramework"));
                fetchXml += budgetFrameworkRepository.GetLinkedEntity(false, budgetFrameworkFilter, null);
            }
       
            
            Tuple<int, List<EventReservation>> eventReservationList = eventReservationRepository.GetFetchXml(userId, isAggregate, paging, eventReservationSort, eventReservationFilter, fetchXml);
            if (!isAggregate)
            {
                string fetchXmlOnly = @"<fetch version='1.0' output-format='xml-platform' returntotalrecordcount='true' aggregate='true'>
                <entity name='new_activity_day' alias='ActivityDay'>
                <attribute name= 'new_activity_dayid' alias = 'count' aggregate = 'count' />
                <attribute name = 'new_event_reservations' alias = 'new_event_reservations' groupby = 'true' />
                <filter type = 'and'><condition attribute='statecode' operator= 'eq' value = '0'/>
                </filter>
                <link-entity name='new_event_reservations' from='new_event_reservationsid' to='new_event_reservations' visible='false' alias='EventReservation'>";
                fetchXmlOnly += eventReservationRepository.FilterFetchXML(eventReservationFilter);
                fetchXmlOnly += @"
                </link-entity>
                </entity>
                </fetch>";
                Tuple<int, List<ActivityDayListItem>> activityDayList = activityDayListRepository.GetFetchXmlOnly(userId, fetchXmlOnly);
                eventReservationList.Item2.ToLookup(eventReservaton => eventReservaton.NumberOfDays = activityDayList.Item2.Find(item => item.Id == eventReservaton.Id)?.NumberOfGroups.ToString());
            }

            return eventReservationList;

        }

        public List<AlmogItem> GetInvoicesList(string userId, string id)
        {

            var eventReservationRelation = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogEventReservationRelation");
            var linkedEventReservations = almogRepository.GetLinkedItemListFetchXml(userId, eventReservationRelation, "new_event_reservations", id );
            return linkedEventReservations;
        }

        public List<List<IdValuePair>> getAvailablePlaces(string eventItemId, string userId)
        {
            EventReservationItem eventItem =  eventReservationRepository.GetFetchXml(userId, eventItemId);
            List<IdValuePair> learningDivisionsList = eventItem.ActivityDayList
                    .SelectMany(x => x.LearningDivisionsList)
                    .Select(x => new IdValuePair { Id = x.Id, Value = x.NumberOfParticipants.ToString() })
                    .ToList();
            List<IdValuePair> workshopList = eventItem.ActivityDayList
                    .SelectMany(x => x.LearningDivisionsList)
                    .SelectMany(x => x.WorkshopsList)
                    .Select(x => new IdValuePair { Id = x.Id, Value = x.NumberOfParticipants.ToString() })
                    .ToList();
            return new List<List<IdValuePair>> {learningDivisionsList, workshopList};
        }

        public EventReservation CreateActivityDayForActivityDayType(string userId, ActivityDayTypeListItemFromClient activityDayTypeListItem)
        {
            EventReservation eventReservation = eventReservationRepository.GetFetchXml(userId, activityDayTypeListItem.EventReservation.Id);
            ActivityDay activityDay = new ActivityDay
            {
                Id = "new",
                EventReservation = eventReservation,
                ActivityDayStatus = new IdValuePair { Id = "4"},
                ActivityDayType = new IdValuePair { Id = activityDayTypeListItem .Id},
                StartDateTime = activityDayTypeListItem.Start,
                EndDateTime = activityDayTypeListItem.End,
                Department = eventReservation.Department,
                DivisionRouter = eventReservation.DivisionRouter,
                Division = int.Parse(eventReservation.DivisionRouter.Id),
                Location = eventReservation.DefaultActivityDayLocation,
        };
            activityDayRepository.Create(activityDay, userId);
            return eventReservation;
        }


        public List<IdValuePair> GetList(string userId, Paging paging, List<Filter> filters)
        {
            string fetchXml = "";

            if (filters != null)
            {
                List<Filter> almogCodeNotEmpty = filters.FindAll(x => x.Field == "AlmogCode" && x.Operator == "not-empty");
                if (almogCodeNotEmpty.Count > 0)
                {
                    fetchXml += "<filter type='or'>";
                    fetchXml += "<filter type='and'>";
                    fetchXml += "<condition attribute='new_almog_code' operator='not-null'/>";
                    fetchXml += "<condition attribute='new_is_not_with_code' operator='eq' value='false'/>";
                    fetchXml += "</filter>";
                    fetchXml += "<condition attribute='new_is_not_with_code' operator='eq' value='true'/>";
                    fetchXml += "</filter>";
                }

                filters = filters.FindAll(x => x.Field != "AlmogCode" && x.Operator != "not-empty");
                //filters = helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filters.FindAll(x => (x.Alias == "" || x.Alias == null || x.Alias == "Institution") && x.Field != "AlmogCode" && x.Operator != "not-empty"));
            }

            return eventReservationRepository.GetList(paging, filters, userId, fetchXml);

        }

    }
}
