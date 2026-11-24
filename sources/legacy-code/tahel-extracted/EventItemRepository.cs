using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk.Query;
using System.Security;
using Data.SendMailWS;

namespace Data.Repositories
{
    public class EventItemRepository : BasicRepository<EventItem, EventItem>, IEventItemRepository
    {
        IResourceRepository resourceRepository;
        IMapFunctionRepository mapFunctionRepository;
        IDepartmentRepository departmentRepository;
        IProductRepository productRepository;

        public EventItemRepository(IResourceRepository resourceRepository,
            IQueryHelperRepository queryHelperRepository,
            ICRMConnection crmConnection,
            IDepartmentRepository departmentRepository,
            IMapFunctionRepository mapFunctionRepository,
            IProductRepository productRepository
            )
            : base(queryHelperRepository, crmConnection)
        {
            this.resourceRepository = resourceRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.departmentRepository = departmentRepository;
            this.productRepository = productRepository;
            //  this.itineraryGroupRepository = itineraryGroupRepository;
            EntityName = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(Objects.EventItem);
            EntityId = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Id");
        }

        public override string EntityName { get; set; }
        public override string EntityId { get; set; }

        protected override IdValuePair SetEntityToIdValueList(string userId, Entity entity)
        {
            throw new NotImplementedException();
        }

        public string GetItemId(string userId, Entity entity)
        {
            string alias = "";
            var guid = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Id");
            if (guid == "")
            {
                alias = "EventItem";
                guid = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Id", alias);
            }
            return guid;
        }

        public int GetCountEvent(List<Filter> filters, string fieldName, string userId)
        {
            var finalList = new List<EventItem>();

            try
            {
                var basicQuery = crmConnection.CreateQuery(EntityName, userId);

                if (filters != null && filters.Count > 0)
                {
                    basicQuery = queryHelperRepository.FilterBy(basicQuery, filters);
                }
                // Get only Active record
                basicQuery = basicQuery.Where(a => a.GetAttributeValue<OptionSetValue>("statecode").Value.Equals(0));
                if (filters != null)
                {
                    Filter start = filters.Find(s => s.Field == "new_start_datetime");
                    Filter end = filters.Find(s => s.Field == "new_end_datetime");
                    Filter fId = filters.Find(s => s.Field == fieldName);
                    if (start != null && end != null)
                    {
                        /*
                                (e.start.toString() === start.toString() && e.end.toString() === end.toString()) ||
                              // currentevent containes in other event
                              (e.start < start && e.end > end) ||
                              // currentevent containing other events
                              (e.start > start && e.end < end) ||
                              // currentevent.start between events
                              (e.start < start && e.end > start) ||
                              // currentevent.end between events
                              (e.start < end && e.end > end) ||
                              // currentevent.start between or bigger and end equal
                              ((e.start < start && e.end > start || e.start > start) && e.end.toString() === end.toString()) ||
                              ((e.start < end && e.end > end || e.end < end) && e.start.toString() === start.toString())

                        /////////////////
                        
                        basicQuery = basicQuery.Where(a =>
                          (a.GetAttributeValue<DateTime>(start.Field) <= DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) >= DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       || (a.GetAttributeValue<DateTime>(start.Field) >= DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) <= DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       || (a.GetAttributeValue<DateTime>(start.Field) >= DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(start.Field) <= DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       || (a.GetAttributeValue<DateTime>(end.Field) >= DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) <= DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null)));

                         */
                        basicQuery = basicQuery.Where(a =>
                        (a.GetAttributeValue<string>(fId.Field) == fId.Value) &&
                        (
                        (a.GetAttributeValue<DateTime>(start.Field) == DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) == DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       // currentevent containes in other event
                       || (a.GetAttributeValue<DateTime>(start.Field) < DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) > DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       // currentevent containing other events
                       || (a.GetAttributeValue<DateTime>(start.Field) > DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) < DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       // currentevent.start between events
                       || (a.GetAttributeValue<DateTime>(start.Field) < DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) > DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null))
                       // currentevent.end between events
                       || (a.GetAttributeValue<DateTime>(start.Field) < DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) > DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       // currentevent.start between or bigger and end equal
                       || ((a.GetAttributeValue<DateTime>(start.Field) < DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) > DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null) || (a.GetAttributeValue<DateTime>(start.Field) > DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null))) && a.GetAttributeValue<DateTime>(end.Field) == DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))
                       || ((a.GetAttributeValue<DateTime>(start.Field) < DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null) && a.GetAttributeValue<DateTime>(end.Field) > DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null) || (a.GetAttributeValue<DateTime>(end.Field) < DateTime.ParseExact(end.Value, "dd/MM/yyyy HH:mm:ss", null))) && a.GetAttributeValue<DateTime>(start.Field) == DateTime.ParseExact(start.Value, "dd/MM/yyyy HH:mm:ss", null)))
                       );
                    }
                }

                var query = basicQuery
                     .Select(x => SetEntityToListItem(userId, x, null));

                finalList = query.ToList();
            }
            catch (Exception ex)
            {
                //Trace.TraceError($"{DateTime.Now}: GetReservations Failed to retrtieve list of reservations {ex.Message}");
                throw;
            }

            return finalList.Count;
        }
        public void UpdateStatus(EventItem item, bool isApprove, string userId)
        {
            var statusField = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventStatus");

            var entity = crmConnection.Retrieve(EntityName, Guid.Parse(item.Id), userId);
            entity[statusField] = new OptionSetValue(item.EventStatus);
            crmConnection.Update(entity, userId);

            if (!isApprove)
            {
                //var itineraryItemId = mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("SchedulerResource");

                item.IsDeleted = true;
                Delete(item, userId);
            }
        }

        protected override EventItem SetEntityToListItem(string userId, Entity entity, List<Filter> additionalFilter = null)
        {

            var item = new EventItem()
            {
                Id = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Id"),
                StartTime = queryHelperRepository.GetValueTimeByAttributeName<EventItem, EventItemCRM>(entity, "Start"),
                EndTime = queryHelperRepository.GetValueTimeByAttributeName<EventItem, EventItemCRM>(entity, "End"),
                Start = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(entity, "Start").ToString("MM/dd/yyyy HH:mm:ss"), // TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Start"))).ToString(),
                End = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(entity, "End").ToString("MM/dd/yyyy HH:mm:ss"),
                IsNeedRefreshments = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsNeedRefreshments"),
                isNeedModifyArrangement = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "isNeedModifyArrangement"),
                IsContractDay = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsContractDay"),
                IsForPay = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsForPay"),
                IsGuideLessHours = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsGuideLessHours"),
                IsMainGuide = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsMainGuide"),
                IsCascaded = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsCascaded"),
                IsResource = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsResource"),
                NumberOfVisitors = (int)queryHelperRepository.GetValueDecimalByAttributeName<EventItem, EventItemCRM>(entity, "NumberOfVisitors"),
                Description = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Description"),
                ShortDescription = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "ShortDescription"),
                Language = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Language"),
                Product = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Product"),
                Comments = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Comments"),
               // IsTaxiRequired = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsTaxiRequired"),
                IsVirtual = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsVirtual"),
                IsWithoutPreparation = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsWithoutPreparation"),
                // IsPaymentForTravel = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsPaymentForTravel"),
                PaymentRate = queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "PaymentRate"),
                UnitPrice = queryHelperRepository.GetValueMoneyByAttributeName<EventItem, EventItemCRM>(entity, "UnitPrice"),
                PriceAfterDiscount = queryHelperRepository.GetValueMoneyByAttributeName<EventItem, EventItemCRM>(entity, "PriceAfterDiscount"),
                //ActivityDay = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "ActivityDay"),
                CreatedBy = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "CreatedBy").Value,
                CreatedOn = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(entity, "CreatedOn").ToString(),
                ModifiedOn = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(entity, "ModifiedOn").ToString(),
                Currency = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Currency"),            
                ReasonForSaving = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "ReasonForSaving"),
                RequestingTheSaveing = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "RequestingTheSaveing"),
                IsWaitingToBeCancelled = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsWaitingToBeCancelled")
            };
            //mapFunctionRepository.GetDestinationPropertyFor<Reservation, ReservationCRM>("GroupName");
            //var res = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Reservation");
            item.PaymentForTravelType = queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "PaymentForTravelType");
            item.PaymentForTaxiType = queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "PaymentForTaxiType");
            item.EventItemActivityLocation = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "EventItemActivityLocation");
            item.IsActivityLocationDifferent = queryHelperRepository.GetValueBooleanByAttributeName<EventItem, EventItemCRM>(entity, "IsActivityLocationDifferent");
            item.EventReservation = new IdValuePair(
               queryHelperRepository.GetValueByAttributeName<EventReservationItem, EventReservationCRM>(entity,"Id", "EventReservation"),
               queryHelperRepository.GetValueByAttributeName<EventReservationItem, EventReservationCRM>(entity, "EventCode", "EventReservation")
            );
            if (queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "EventReservation") != null)
            {
                item.EventReservation = new IdValuePair(
                   queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "EventReservation").Id,
                  queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "EventReservation").Value
                );
            }

            if (queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Reservation") != null)
            {
                item.Reservation = new IdValuePair(
                   queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Reservation").Id,
                   queryHelperRepository.GetValueByAttributeName<Reservation, ReservationCRM>(entity, "ReservationNum", "Reservation")
                );
            }
            item.ReservationGroupName = queryHelperRepository.GetValueByAttributeName<Reservation, ReservationCRM>(entity, "GroupName", "Reservation");
            item.InstitutionName = queryHelperRepository.GetValueByAttributeName<Institution, InstitutionCRM>(entity, "Name", "Institution");
            item.ActivityDayLocation = queryHelperRepository.GetIdValueForEntityReferenceByName<ActivityDay, ActivityDayCRM>(entity, "Location", "ActivityDay")?.Value;

            item.ActivityDay = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "ActivityDay");
            
            item.LearningDivision = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "LearningDivision");
            item.WorkshopLookup = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Workshop");

            if (item.IsResource)
            {
                var resource = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Resource");
                item.ScheduleItem = resource;
            }
            else
            {

                var serviceProvider = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "ServiceProvider");

                item.ScheduleItem = serviceProvider;
                item.ServiceProviderCommunicationInfo = new CommunicationInfo
                {
                    Mobile = queryHelperRepository.GetValueByAttributeName<CommunicationInfo, ServiceProviderCRM>(entity, "Mobile", "ServiceProvider"),
                    Telephone = queryHelperRepository.GetValueByAttributeName<CommunicationInfo, ServiceProviderCRM>(entity, "Telephone", "ServiceProvider"),
                    Email = queryHelperRepository.GetValueByAttributeName<CommunicationInfo, ServiceProviderCRM>(entity, "Email", "ServiceProvider"),
                };
                item.ServiceProviderIdNumber = queryHelperRepository.GetValueByAttributeName<ServiceProvider, ServiceProviderCRM>(entity, "IdNumber", "ServiceProvider");
            }

            SetDivision(entity, item, userId);
            
            int.TryParse(queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "EventStatus")?.Id, out int eventStatus);
            item.EventStatus = eventStatus == 0 ? 1 : eventStatus;

            int.TryParse(queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "EventType")?.Id, out int eventType);
            item.EventType = eventType == 0 ? 1 : eventType;

            item.ReservationColor = item.Reservation != null && item.Reservation.Value != "" && item.Reservation.Value.Split('-')[1] != "" ? int.Parse(item.Reservation.Value.Split('-')[1]) % 5 :
                item.Reservation == null && item.EventReservation != null ? 6 : 0;
            item.ReservationStatus = queryHelperRepository.GetIdValueForOptionSetByName<Reservation, ReservationCRM>(entity, "ReservationStatus", "Reservation");
            item.ReservationGroupName = item.EventReservation.Id == "" ? item.ReservationGroupName : queryHelperRepository.GetValueByAttributeName<EventReservationItem, EventReservationCRM>(entity, "InternalName", "EventReservation");
            if (string.IsNullOrEmpty(item.ReservationGroupName)) {
                item.ReservationGroupName = queryHelperRepository.GetValueByAttributeName<EventReservationItem, EventReservationCRM>(entity, "Title", "EventReservation");
            }

            item.EventReservationType = queryHelperRepository.GetIdValueForOptionSetByName<EventReservationItem, EventReservationCRM>(entity, "EventType", "EventReservation");

            string numberOfGroup = queryHelperRepository.GetValueByAttributeName<Reservation, ReservationCRM>(entity, "NumberOfGroups", "Reservation");
            if (!string.IsNullOrEmpty(numberOfGroup))
                item.numberOfGroup = int.Parse(numberOfGroup);
            
            item.ServiceProviderIdNumber = queryHelperRepository.GetValueByAttributeName<ServiceProvider, ServiceProviderCRM>(entity, "IdNumber", "ServiceProvider");
            item.BudgetFramework = item.EventReservation.Id == "" ?  queryHelperRepository.GetIdValueForEntityReferenceByName<Reservation, ReservationCRM>(entity, "BudgetFramework", "Reservation") : queryHelperRepository.GetIdValueForEntityReferenceByName<EventReservationItem, EventReservationCRM>(entity, "BudgetFramework", "EventReservation");
            item.IsActive = queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "IsActive").Id == "0" ? true : false;
            return item;
        }
        protected override EventItem SetEntityToItem(string userId, Entity entity, string guid, List<Filter> additionalFilter = null)
        {
            return SetEntityToListItem(userId, entity);
        }
        protected override void SetItemToEntity(EventItem item, EventItem oldItem, Entity entity, string userId)
        {
            if (item.Name == null)
            {
                //item.Name = $"Event Item -> " + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
                item.Name = $"Event Item -> {DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}";

            }
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "Name");

            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "Comments");
            //Shira fix date format 4.4.21
            var startDate = DateTime.ParseExact(item.Start, "MM/dd/yyyy HH:mm:ss", null);
            var endDate = DateTime.ParseExact(item.End, "MM/dd/yyyy HH:mm:ss", null);
            entity[mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Start")] = startDate;
            entity[mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("End")] = endDate;

            //item.Start = "";
            //queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, "Start", typeof(DateTime));
            //queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, "End", typeof(DateTime));

            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsContractDay", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsForPay", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsGuideLessHours", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsWithoutPreparation", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsMainGuide", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsCascaded", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsResource", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsNeedRefreshments", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "isNeedModifyArrangement", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "Description");
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "ShortDescription");
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "NumberOfVisitors", typeof(int));
            queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "Product", Objects.Product, item.Product?.Id, oldItem?.Product?.Id);
            queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "Reservation", Objects.Reservation, string.IsNullOrEmpty(item.EventReservation?.Id) ? item.Reservation?.Id : "", oldItem?.Reservation?.Id);
            queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "EventReservation", Objects.EventReservation, item.EventReservation?.Id, oldItem?.EventReservation?.Id);
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "UnitPrice", typeof(Money));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "PriceAfterDiscount", typeof(Money));
            //queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsPaymentForTravel", typeof(bool));
            //queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsTaxiRequired", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsVirtual", typeof(bool));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsUniteEventItem", typeof(bool));
            queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "Currency", Objects.Currency, item.Currency?.Id, oldItem?.Currency?.Id);
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "ReasonForSaving" );
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "RequestingTheSaveing");

           if(item.PaymentForTaxiType?.Id==((int)TravelType.FULL).ToString())
            {
                item.PaymentForTravelType.Id = ((int)TravelType.NO).ToString();
            }
            /*if (item.IsTaxiRequired)
            {
                item.IsPaymentForTravel = false;
                queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsPaymentForTravel", typeof(bool));
            }*/
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "PaymentRate", typeof(OptionSetValue));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "PaymentForTravelType", typeof(OptionSetValue));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "PaymentForTaxiType", typeof(OptionSetValue));
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "EventItemActivityLocation");
            queryHelperRepository.SetEntityField<EventItem, EventItemCRM>(entity, item, oldItem, "IsActivityLocationDifferent");

            item.EventStatus = item.EventStatus == 0 ? 1 : item.EventStatus;
            entity[mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventStatus")] = new OptionSetValue(item.EventStatus);
            item.EventType = item.EventType == 0 ? 1 : item.EventType;
            entity[mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventType")] = new OptionSetValue(item.EventType);
            queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "Language", Objects.Language, item.Language?.Id, oldItem?.Language?.Id);
            queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "Division", Objects.Language, item.Division?.Id, oldItem?.Division?.Id);
            //Shira need to union follow fields
            if (item.ActivityDayLookup != null)
            {
                queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "ActivityDay", Objects.ActivityDay, item.ActivityDayLookup?.Id, oldItem?.ActivityDayLookup?.Id);
            }

            if (item.ActivityDay != null)
            {
                queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "ActivityDay", Objects.ActivityDay, item.ActivityDay?.Id, oldItem?.ActivityDay?.Id);
            }

            if (item.LearningDivision != null)
            {
                queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "LearningDivision", Objects.LearningDivision, item.LearningDivision?.Id, oldItem?.LearningDivision?.Id);
            }
            if (item.WorkshopLookup != null)
            {
                queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "Workshop", Objects.Workshop, item.WorkshopLookup?.Id, oldItem?.WorkshopLookup?.Id);
            }

            if (item.IsResource)
            {
                queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "Resource", Objects.Resource, item.ScheduleItem?.Id, oldItem?.ScheduleItem?.Id);
                if (item.EventType == 8)
                {
                    queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "UnitedEventItem", Objects.Resource, item.UnitedEventItem?.Id, oldItem?.UnitedEventItem?.Id);
                }
            }
            else
            {
                queryHelperRepository.SetEntityReferenceField<EventItem, EventItemCRM>(entity, "ServiceProvider", Objects.ServiceProvider, item.ScheduleItem?.Id, oldItem?.ScheduleItem?.Id);
            }
        }

        public override void Delete(EventItem item, string userId)
        {
            if (item.IsDeleted)
            {
                try
                {
                    if (string.IsNullOrEmpty(item.Id) || item.Id.ToLower() == "new" || !Guid.TryParse(item.Id, out Guid id))
                    {
                        Trace.TraceError("error in delete eventItem. Id is: ", item.Id);
                        throw new Exception();
                    }

                    Entity entity = crmConnection.Retrieve(EntityName, Guid.Parse(item.Id), userId);
                    if (resourceRepository.IsUnionResource(item.ScheduleItem.Id, userId))
                    {
                        List<EventItem> childEventItemList = GetUnionChildEventItemList(item, userId);
                        foreach (var childEvent in childEventItemList)
                        {
                            childEvent.IsDeleted = true;
                            Delete(childEvent, userId);
                        }
                    }

                    entity["statecode"] = new OptionSetValue(1);
                    crmConnection.Update(entity, userId);
                    item.Id = null;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"{DateTime.Now}: 2 Failed to deletes: {typeof(EventItem).Name} -item: {item} -item:id {item.Id} - {ex.Message}");
                    throw;
                }
            }
        }

        public override void Cancel(EventItem item, string userId, bool isCancel = true)
        {
            if (item.IsCanceled)
            {
                try
                {
                    if(item.Id == null || item.Id == String.Empty || item.Id == "new")
                    {
                        Trace.TraceError("id is error in cancel. id: ", item.Id);
                    }
                    Entity entity = crmConnection.Retrieve(EntityName, Guid.Parse(item.Id), userId);
                    
                    if (resourceRepository.IsUnionResource(item.ScheduleItem.Id, userId))
                    {
                        List<EventItem> childEventItemList = GetUnionChildEventItemList(item, userId);
                        foreach (var childEvent in childEventItemList)
                        {
                            childEvent.IsCanceled = true;
                            base.Cancel(childEvent, userId);
                        }
                    }

                    entity["new_status"] = new OptionSetValue(10);
                    entity["statecode"] = new OptionSetValue(1);
                    crmConnection.Update(entity, userId);
                    item.Id = null;
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"{DateTime.Now}: 8 Failed to deletes: {typeof(EventItem).Name} -item: {item} -item:id {item.Id} - {ex.Message}");
                    throw;
                }
            }
        }


        public List<EventItemForExcel> GetEventListForExcel(EventItemForExcel item, string entityName, string userId)
        {
            var reservationField = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Reservation");
            var activityDayField = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("ActivityDay");
            var eventReservationField = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventReservation");

            var isResourceField = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("IsResource");
            var statusField = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventStatus");
            var eventTypeField = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventType");
            var reservationId = Guid.Parse(item.ReservationId);
            var activityDayId = Guid.Parse(item.ActivityDayId);
            List<EventItemForExcel> list;
            if (entityName == reservationField)
            {
                list = crmConnection.CreateQuery(EntityName, userId)
                .Where(e =>
                // Shira slash, because add event reservation 21.10.21
                e.GetAttributeValue<EntityReference>(entityName).Id.Equals(reservationId) &&
                    e.GetAttributeValue<EntityReference>(activityDayField).Id.Equals(activityDayId)
                    && !e.GetAttributeValue<bool>(isResourceField)
                    // && !e.GetAttributeValue<OptionSetValue>(statusField).Value.Equals(5))//Cascaded
                    && (e.GetAttributeValue<OptionSetValue>(eventTypeField).Value.Equals(1) || e.GetAttributeValue<OptionSetValue>(eventTypeField).Value.Equals(4))//שיבוץ או שיבוץ ליום שלם
                    && e.GetAttributeValue<OptionSetValue>("statecode").Value.Equals(0))
                .Select(e => GetItemForExcelFromEntity(userId, e, item))
                .ToList();
            }
            else
            {   // Shira 31.10.21, event reservation doesn't have lookup to eventReservation(only to activityday)

                list = crmConnection.CreateQuery(EntityName, userId)
                .Where(e =>
                    // Shira slash, because add event reservation 21.10.21 - and event reservation doesn't have lookup to eventReservation(only to activityday)
                    //open below slash if all activities day contains lookup to eventReservation(additional check)
                    //e.GetAttributeValue<EntityReference>(entityName).Id.Equals(reservationId) &&
                    e.GetAttributeValue<EntityReference>(activityDayField).Id.Equals(activityDayId)
                    && !e.GetAttributeValue<bool>(isResourceField)
                    // && !e.GetAttributeValue<OptionSetValue>(statusField).Value.Equals(5))//Cascaded
                    && (e.GetAttributeValue<OptionSetValue>(eventTypeField).Value.Equals(1) || e.GetAttributeValue<OptionSetValue>(eventTypeField).Value.Equals(4))//שיבוץ או שיבוץ ליום שלם
                    && e.GetAttributeValue<OptionSetValue>("statecode").Value.Equals(0))
                .Select(e => GetItemForExcelFromEntity(userId, e, item))
                .ToList();
            }
            //Shira add 27.1 to remove event that not in the current activityDay
            return list.Where(e => e.ActivityDayId.ToString() == item.ActivityDayId).ToList();
            /*list.Where(e => {
                var a = DateTime.ParseExact(e.End, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() == DateTime.ParseExact(e.EndEvent, "MM/dd/yyyy HH:mm:ss", null).ToShortDateString();
            return a;
        }).ToList();*/
        }

        private void SetDivision(Entity entity, EventItem item, string userId)
        {
            var divisionId = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Division")?.Id;
            if (divisionId != "")
            {
                //item.Division = GetDivision(divisionId, userId);
                item.Division = new Department();
                item.Division.Id = divisionId;
                item.Division.Name = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Division")?.Value;
            }
        }
        private Department GetDivision(string divisionId, string userId)
        {

            //Filter filter = new Filter
            //{
            //    Field = mapFunctionRepository.GetDestinationPropertyFor<Department, DepartmentCRM>("Id"),
            //    Value = divisionId,
            //    Type = "lookup"
            //};
            return departmentRepository.GetFetchXml(userId, divisionId); //.FirstOrDefault();
        }
        private EventItemForExcel GetItemForExcelFromEntity(string userId, Entity entity, EventItemForExcel item)
        {
            //item contains properties of activityDay&reservation... and this function add prop of event-item,
            //need to do clone (serialize&deserialize) for all event-items for the data won't be overwrite by the last iteration(last event item)
            var serialized = JsonConvert.SerializeObject(item);
            EventItemForExcel new_item = JsonConvert.DeserializeObject<EventItemForExcel>(serialized);
            new_item.EventItemId = queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Id");
            new_item.StartEvent = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(entity, "Start").ToString("MM/dd/yyyy HH:mm:ss"); // TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(queryHelperRepository.GetValueByAttributeName<EventItem, EventItemCRM>(entity, "Start"))).ToString(),
            new_item.EndEvent = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(entity, "End").ToString("MM/dd/yyyy HH:mm:ss");
            int.TryParse(queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "EventStatus")?.Id, out int eventStatus);
            new_item.EventStatus = eventStatus == 0 ? 1 : eventStatus;
           // new_item.IsTaxiRequired = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsTaxiRequired");
            new_item.IsVirtual = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsVirtual");
            //  new_item.IsPaymentForTravel = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsPaymentForTravel");
            new_item.PaymentForTravelType = Convert.ToInt32( queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "PaymentForTravelType")?.Id);
            new_item.PaymentForTaxiType = Convert.ToInt32(queryHelperRepository.GetIdValueForOptionSetByName<EventItem, EventItemCRM>(entity, "PaymentForTaxiType")?.Id);

            new_item.IsForPay = queryHelperRepository.GetValueBoolByAttributeName<EventItem, EventItemCRM>(entity, "IsForPay");
            new_item.ModifiedEvent = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(entity, "ModifiedOn").ToString();
            new_item.ActivityType = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "Product");
            var serviceProviderId = queryHelperRepository.GetIdValueForEntityReferenceByName<EventItem, EventItemCRM>(entity, "ServiceProvider")?.Id;
            if (!string.IsNullOrEmpty(serviceProviderId))
            {
                var serviceProviderEntity = GetServiceProvider(serviceProviderId, userId);
                if (serviceProviderEntity != null)
                {
                    new_item.ServiceProviderId = serviceProviderEntity.GetAttributeValue<string>(mapFunctionRepository.GetDestinationPropertyFor<ServiceProvider, ServiceProviderCRM>("IdNumber"));
                    new_item.ServiceProviderFullName = serviceProviderEntity.GetAttributeValue<string>(mapFunctionRepository.GetDestinationPropertyFor<ServiceProvider, ServiceProviderCRM>("FullName"));
                    new_item.IsServiceProviderEmployee = serviceProviderEntity.GetAttributeValue<bool>(mapFunctionRepository.GetDestinationPropertyFor<ServiceProvider, ServiceProviderCRM>("ServiceProviderType_Employee"));
                    //new_item.GuideType = queryHelperRepository.GetIdValueForOptionSetByName<ServiceProvider, ServiceProviderCRM>(serviceProviderEntity, "GuideType")?.Value;
                    new_item.ServiceProviderCityLocation = serviceProviderEntity.GetAttributeValue<string>(mapFunctionRepository.GetDestinationPropertyFor<AddressInfo, ServiceProviderCRM>("City"));
                }

            }

            return new_item;
        }

        private Entity GetServiceProvider(string serviceProviderGuid, string userId)
        {
            var serviceProviderEntityName = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(Objects.ServiceProvider);
            var serviceProviderIdField = mapFunctionRepository.GetDestinationPropertyFor<ServiceProvider, ServiceProviderCRM>("IdNumber");
            var serviceProviderFullNameField = mapFunctionRepository.GetDestinationPropertyFor<ServiceProvider, ServiceProviderCRM>("FullName");
            var serviceProviderEmployeeField = mapFunctionRepository.GetDestinationPropertyFor<ServiceProvider, ServiceProviderCRM>("ServiceProviderType_Employee");
            var serviceProviderGuideTypeField = mapFunctionRepository.GetDestinationPropertyFor<ServiceProvider, ServiceProviderCRM>("GuideType");
            var serviceProviderCityField = mapFunctionRepository.GetDestinationPropertyFor<AddressInfo, ServiceProviderCRM>("City");
            var entity = crmConnection.Retrieve(serviceProviderEntityName, Guid.Parse(serviceProviderGuid), new string[] { serviceProviderIdField, serviceProviderFullNameField, serviceProviderCityField, serviceProviderEmployeeField, serviceProviderGuideTypeField }, userId);

            if (entity.Contains(serviceProviderIdField) || entity.Contains(serviceProviderFullNameField))
            {
                return entity;
            }
            return null;
        }

        public override string GlobalSearchFetchXml(List<Filter> filters)
        {
            string tmp = "";
            var reservationNum = mapFunctionRepository.GetDestinationPropertyFor<Reservation, ReservationCRM>("ReservationNum");
            var start = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Start");
            var end = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("End");
            var product = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Product");
            var description = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Description");
            var comments = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Comments");

            var filter = filters.Find(f => f.Type == "global");

            tmp += "<filter type='or'>";
            //tmp += "<condition attribute='" + start + "' operator= 'like' value = '%" + SecurityElement.Escape(filter.Value) + "%'/>";
            //tmp += "<condition attribute='" + end + "name' operator= 'like' value = '%" + SecurityElement.Escape(filter.Value) + "%'/>";

            tmp += "<condition attribute='" + product + "name' operator= 'like' value = '%" + SecurityElement.Escape(filter.Value) + "%'/>";

            //Adina put in remarks 2021-06-02
            //there is no reservation num on event item
            //tmp += "<condition attribute='" + reservationNum + "' operator= 'like' value = '%" + SecurityElement.Escape(filter.Value) + "%'/>";
            //Adina until here 

            //Adina added for save spot page 2021-06-02
            tmp += "<condition attribute='" + description + "' operator= 'like' value = '%" + SecurityElement.Escape(filter.Value) + "%'/>";
            tmp += "<condition attribute='" + comments + "' operator= 'like' value = '%" + SecurityElement.Escape(filter.Value) + "%'/>";
            //Adina until here 

            tmp += "</filter>";

            //.mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(objName)
            // queryHelperRepository.GetIdValueForEntityReferenceByName<Reservation, ReservationCRM>(entity, "Institution").Value.Contains(filter.Value);

            return tmp;
        }

        public override string GetLinkedEntity(bool isAggregate, List<Filter> filters, List<Sort> sort, string linkType, string additionalType)
        {
            string fetchXml = "";

            if (linkType != "inner")
                linkType = "outer";

            if (additionalType == "SchedulerResource")
            {
                fetchXml = @"<link-entity name='new_event_item' from='new_event_itemid' to='new_scheduler_resource' alias='EventItem' type='" + linkType + "'>";
                fetchXml += @"<all-attributes/>";

                List<Filter> reservationFilters = filters.FindAll(x => x.Alias == "Reservation");
                List<Filter> eventReservationFilters = filters.FindAll(x => x.Alias == "EventReservation");
                List<Filter> activityDayFilters = filters.FindAll(x => x.Alias == "ActivityDay");

                if (filters != null && filters.Count > 0)
                {
                    fetchXml += FilterFetchXML(filters.FindAll(x => x.Alias == "EventItem:SchedulerResource"));
                }

                fetchXml += resourceRepository.GetLinkedEntity(isAggregate, filters, sort, "outer", additionalType);

                fetchXml += @"<link-entity name='new_reservation' from='new_reservationid' to='new_reservation' alias='Reservation' link-type='outer'>
                        <all-attributes/>
                            <link-entity name='new_budget_framework' from='new_budget_frameworkid' link-type='outer' to='new_budget_framework' alias='BudgetFramework'>
                            <attribute name='new_rsv_department'/>
                            </link-entity>";
                if (reservationFilters.Count > 0)
                {
                    fetchXml += FilterFetchXML(reservationFilters);
                }
                fetchXml += @"</link-entity>";
                ////
                fetchXml += @"<link-entity name='new_event_reservations' from='new_event_reservationsid' to='new_event_reservations' alias='EventReservation' link-type='outer'>
                            <all-attributes/>
                            <link-entity name='new_budget_framework' from='new_budget_frameworkid' link-type='outer' to='new_budget_framework' alias='EventBudgetFramework'>
                              <attribute name='new_rsv_department'/>
                            </link-entity>";
                if (eventReservationFilters.Count > 0)
                {
                    fetchXml += FilterFetchXML(eventReservationFilters);
                }
                fetchXml += @"</link-entity>";
                ////

                fetchXml += @"<link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ActivityDay' link-type='outer'>
                            <all-attributes/>";
                if (activityDayFilters.Count > 0)
                {
                    fetchXml += FilterFetchXML(activityDayFilters);
                }
                fetchXml += @"</link-entity>";

                fetchXml += @"</link-entity>";
            }
            if (additionalType == "SchedulerServiceProvider")
            {
                fetchXml = @"<link-entity name='new_event_item' from='new_event_itemid' to='new_scheduler_service_provider' alias='EventItemServiceProvider' link-type='" + linkType + "'>";

                if (filters != null && filters.FindAll(x => x.Alias == "EventItem:ServiceProvider").Count > 0)
                {
                    fetchXml += FilterFetchXML(filters.FindAll(x => x.Alias == "EventItem:ServiceProvider"));
                }
                fetchXml += @"<link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider' link-type='outer'>
                   <attribute name='new_mobile' />
                  <attribute name='new_work_telephone' />
                  <attribute name='new_telephone' />
                              </link-entity>";
                fetchXml += @"</link-entity>";
            }
            else if (additionalType == "ActivityDay")
            {

                fetchXml = @"<link-entity name='new_event_item' from='new_activity_day' to='new_activity_dayid' alias='EventItem' link-type='outer'>";
                if (!isAggregate)
                {
                    fetchXml += "<attribute name='statecode'/>";
                }
                if (filters != null && filters.Count > 0)
                {
                    fetchXml += FilterFetchXML(filters.FindAll(x => x.Alias == "EventItem"));
                }
                fetchXml += @"<filter type='and'>
                                 <condition attribute='statecode' operator='eq' value='0' />
                             </filter> 
                          </link-entity>";
            }

            return fetchXml;

        }
        public void UnionEventItem(EventItem item, EventItem oldItem, string userId)
        {
            string fetchXml = "";
            List<Resource> resource;

            if (oldItem != null && oldItem.ScheduleItem.Id != item.ScheduleItem.Id)
            {

                //delete old union schedule events
                if (resourceRepository.IsUnionResource(oldItem.ScheduleItem.Id, userId))
                {
                    List<EventItem> childUnionEventItemList = GetUnionChildEventItemList(oldItem, userId);
                    if (childUnionEventItemList.Count > 0)
                    {
                        foreach (var chileEventItem in childUnionEventItemList)
                        {
                            chileEventItem.IsDeleted = true;
                            Delete(chileEventItem, userId);
                        }
                    }
                }
            }
            // create new union schedule events
            if (oldItem != null && oldItem.ScheduleItem.Id != item.ScheduleItem.Id || oldItem == null)
            {
                fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                               <entity name='new_resource'>
                                 <attribute name='new_resourceid' />
                                 <attribute name='new_name' />
                                 <attribute name='createdon' />
                                 <order attribute='new_name' descending='false' />
                                 <filter type='and'>
                                   <condition attribute='new_united_resource' operator='eq' uitype='new_resource' value='" + item.ScheduleItem.Id + @"' />
                                   <condition attribute='new_is_it_consolidable' operator='eq' value='1' />
                                   <condition attribute='statecode' operator='eq' value='0' />
                                 </filter>
                               </entity>
                             </fetch>";
                Tuple<int, List<Resource>> ChildResource = resourceRepository.GetFetchXmlOnly(userId, fetchXml);
                if (ChildResource.Item2.Count > 0)
                {
                    foreach (var childResource in ChildResource.Item2)
                    {
                        string temp = JsonConvert.SerializeObject(item);
                        EventItem childEventItem = JsonConvert.DeserializeObject<EventItem>(temp);
                        childEventItem.EventType = 8;
                        childEventItem.ScheduleItem.Id = childResource.Id;
                        childEventItem.ScheduleItem.Value = childResource.Value;
                        childEventItem.IsUniteEventItem = false;
                        childEventItem.UnitedEventItem = new IdValuePair(id: item.Id.ToString(), value: "");
                        var childGuid = Create(childEventItem, userId);
                    }
                }
            }
            else
            {
                if (resourceRepository.IsUnionResource(item.ScheduleItem.Id, userId))
                {
                    List<EventItem> childUnionEventItemList = GetUnionChildEventItemList(oldItem, userId);
                    for (int i = 0; i < childUnionEventItemList.Count; i++)
                    {
                        EventItem tempChildEventItem = childUnionEventItemList[i];
                        string temp = JsonConvert.SerializeObject(item);
                        childUnionEventItemList[i] = JsonConvert.DeserializeObject<EventItem>(temp);
                        childUnionEventItemList[i].EventType = 8;
                        childUnionEventItemList[i].UnitedEventItem = tempChildEventItem.UnitedEventItem;
                        childUnionEventItemList[i].IsUniteEventItem = false;
                        childUnionEventItemList[i].ScheduleItem = tempChildEventItem.ScheduleItem;
                        childUnionEventItemList[i].Id = tempChildEventItem.Id;
                        Update(childUnionEventItemList[i], tempChildEventItem, userId);
                    }
                }

            }
        }
        public List<EventItem> GetUnionChildEventItemList(EventItem item, string userId)
        {
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='new_event_item'>
                                            <attribute name='new_event_itemid' />
                                            <attribute name='new_name' />
                                            <attribute name='createdon' />
                                            <order attribute='new_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='new_united_event_item' operator='eq'  uitype='new_event_item' value='" + item.Id + @"' />
                                              <condition attribute='statecode' operator='eq' value='0' />
                                            </filter>
                                          </entity>
                                        </fetch>";
            List<EventItem> chileEventItemList = GetFetchXmlOnly(userId, fetchXml).Item2;
            return chileEventItemList;
        }
    }
}
