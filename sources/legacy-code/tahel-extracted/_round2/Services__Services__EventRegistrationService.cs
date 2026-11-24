using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Microsoft.Xrm.Sdk;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Services.Services
{
    public class EventRegistrationService : BasicService<EventRegistration, EventRegistration, EventRegistrationCRM>, IEventRegistrationService
    {
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly string environment;
        private readonly IEventReservationRepository eventReservationRepository;
        private readonly IEventRegistrationRepository eventRegistrationRepository;
        private readonly IContactRepository contactRepository;
        private readonly IQueryHelperRepository queryHelperRepository;
        private readonly IClaimsAnswerRepository claimsAnswerRepository;
        private readonly IEventRegistrationActivityDayRepository eventRegistrationActivityDayRepository;
        private readonly IActivityDayRepository activityDayRepository;
        private readonly IPaymentRepository paymentRepository;
        private readonly IReservationsService reservationService;
        private readonly IReportsService reportsService;
        public Objects Objects { get; } = new Objects();
        public EventRegistrationService(IHelperService helperService,
            IContactRepository contactRepository,
            IEventReservationRepository eventReservationRepository,
            IEventRegistrationRepository eventRegistrationRepository,
            IMapFunctionRepository mapFunctionRepository,
            IQueryHelperRepository queryHelperRepository,
            IClaimsAnswerRepository claimsAnswerRepository,
            IEventRegistrationActivityDayRepository eventRegistrationActivityDayRepository,
            IActivityDayRepository activityDayRepository,
            IPaymentRepository paymentRepository,
            IReportsService reportsService,
            IReservationsService reservationService) 
            : base(helperService, eventRegistrationRepository, mapFunctionRepository)
        {
            
           
            this.contactRepository = contactRepository;
            this.eventReservationRepository = eventReservationRepository;
            this.eventRegistrationRepository = eventRegistrationRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.queryHelperRepository = queryHelperRepository;
            this.claimsAnswerRepository = claimsAnswerRepository;
            this.eventRegistrationActivityDayRepository = eventRegistrationActivityDayRepository;
            this.activityDayRepository = activityDayRepository;
            this.paymentRepository = paymentRepository;
            this.reportsService = reportsService;
            this.reservationService = reservationService;
            environment = ConfigurationManager.AppSettings["environment"];
        }

        public Tuple<int, List<EventRegistration>> GetContactList(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            Tuple<int, List<EventRegistration>> list = null;
            return list;
        }

  
        public override Tuple<int, List<EventRegistration>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            Tuple<int, List<EventRegistration>> eventRegistrationList = new Tuple<int, List<EventRegistration>>(0, null);
            bool isContactList = filter.Find(x => x.Field == "isContactList" && x.Value == "true") != null;
            filter.Remove(filter.Find(x => x.Field == "isContactList" && x.Value == "true"));
            List<Filter> registrationFilter = helperService.BuildFilter<EventRegistration, EventRegistrationCRM, EventRegistrationCRM>(filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "EventRegistration"));
            List<Filter> eventReservationFilter = helperService.BuildFilter<EventReservationItem, EventReservationCRM, EventReservationCRM>(filter.FindAll(x => x.Alias == "EventReservation"));
            string activityDayId = "";
            if (filter.Find(x => x.Alias == "EventRegistrationActivityDay") != null)
            {
                activityDayId = filter.Find(x =>x.Alias == "EventRegistrationActivityDay").Value;
            }
            List<Filter> activityDayFilter = helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay"));

            List<Sort> registrationSort = null;
            List<Sort> eventSort = null;
            List<Sort> contactSort = null;
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

                registrationSort = helperService.BuildSorting<EventRegistration, EventRegistrationCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "EventRegistration"));
                if (mapFunctionRepository.IsFieldsInObject<Contact, ContactCRM>(sort.Select(x => x.Active).ToList()))
                {
                    contactSort = helperService.BuildSorting<Contact, ContactCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "Contact"));
                }
                if (mapFunctionRepository.IsFieldsInObject<EventReservationItem, EventReservationCRM>(sort.Select(x => x.Active).ToList()))
                {
                    eventSort = helperService.BuildSorting<EventReservationItem, EventReservationCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "EventReservation"));
                }
            }
    
            string fetchXml = @"<all-attributes/>";
           
            if (isContactList)
            {
                registrationFilter.AddRange(eventReservationFilter);
                fetchXml += eventRegistrationRepository.GetLinkedEntity(isAggregate, registrationFilter, null, "", "contact");
                Tuple<int, List<Contact>> contactList = contactRepository.GetFetchXml(userId, false, paging, contactSort, registrationFilter, fetchXml);
                List<EventRegistration> tempList = new List<EventRegistration>();
                foreach (Contact contact in contactList.Item2)
                {
                    EventRegistration eventRegistration = new EventRegistration();
                    eventRegistration.IsActive = true;
                    eventRegistration.Contact = contact;
                    tempList.Add(eventRegistration);
                }

                tempList = tempList.GroupBy(x => x.Contact.Id).Select(x => x.FirstOrDefault()).ToList();
                eventRegistrationList = new Tuple<int, List<EventRegistration>>(contactList.Item1, tempList);
            }
            else if(activityDayId != "" && activityDayId != null)
            {
                fetchXml += eventRegistrationRepository.GetEventRegistrationActivityDayLinkedEntity(activityDayId, activityDayFilter);
                fetchXml += contactRepository.GetLinkedEntity(isAggregate, filter.FindAll(x => x.Alias == "Contact"), sort.FindAll(x => x.Alias == "Contact"));
                fetchXml += eventReservationRepository.GetLinkedEntity(isAggregate, eventReservationFilter, eventSort, "", "EventRegistration");
                eventRegistrationList = eventRegistrationRepository.GetFetchXml(userId, false, paging, registrationSort, registrationFilter, fetchXml);

            }
            else
            {
                fetchXml += eventReservationRepository.GetLinkedEntity(isAggregate, eventReservationFilter, eventSort, "", "EventRegistration");
                fetchXml += contactRepository.GetLinkedEntity(isAggregate, filter.FindAll(x => x.Alias == "Contact"), sort.FindAll(x => x.Alias == "Contact"));
                eventRegistrationList = eventRegistrationRepository.GetFetchXml(userId, false, null, registrationSort, registrationFilter, fetchXml);

            }

            return eventRegistrationList;

        }

        public override void Delete(EventRegistration item, string userId)
        {
            eventRegistrationRepository.Delete(item, userId);

        }
        public override Guid Create(EventRegistration item, string userId)
        {
           
           var id = eventRegistrationRepository.Create(item, userId);
            item.Id = id.ToString();
            if (item.EventRegistrationActivityDayFullItem != null)
            {
                item.EventRegistrationActivityDayFullItem.EventRegistration.Id = id.ToString();
                item.EventRegistrationActivityDayFullItem.Id =eventRegistrationActivityDayRepository.Create(item.EventRegistrationActivityDayFullItem,userId).ToString();

            }
            eventRegistrationRepository.SetFieldsAfterCreate(item,userId);
            return id;

        }
   
        public override void Update(EventRegistration item, EventRegistration oldItem, string userId)
        {
            eventRegistrationRepository.Update(item, oldItem, userId);
            CreateOrUpdateEventRegistration(item, oldItem, userId);
        }
             private void CreateOrUpdateEventRegistration(EventRegistration item, EventRegistration oldItem, string userId, bool isCreate = false)
        {
            if (item.MainContextWhichYouTeach != null) SetClaimsAnswerList("MainContextWhichYouTeach", Objects.MainContextWhichYouTeachListRelation, Objects.ClaimsAnswer, item.Id, item.MainContextWhichYouTeach, userId, isCreate);
            if (item.PrimaryReasonsForRegistering != null) SetClaimsAnswerList("PrimaryReasonsForRegistering",Objects.PrimaryReasonsForRegisteringListRelation, Objects.ClaimsAnswer, item.Id, item.PrimaryReasonsForRegistering, userId, isCreate);
            if (item.ProfessionalCapacity != null) SetClaimsAnswerList("ProfessionalCapacity",Objects.ProfessionalCapacityListRelation, Objects.ClaimsAnswer, item.Id, item.ProfessionalCapacity, userId, isCreate);
            if (item.StudentsAges != null) SetClaimsAnswerList("StudentsAges",Objects.StudentsAgesListRelation, Objects.ClaimsAnswer, item.Id, item.StudentsAges, userId, isCreate);
            if (item.TeachingMainSources != null) SetClaimsAnswerList("TeachingMainSources",Objects.TeachingMainSourcesListRelation, Objects.ClaimsAnswer, item.Id, item.TeachingMainSources, userId, isCreate);
            if (item.TeachingSubjects != null) SetClaimsAnswerList("TeachingSubjects",Objects.TeachingSubjectsListRelation, Objects.ClaimsAnswer, item.Id, item.TeachingSubjects, userId, isCreate);

            if (item.Contact != null)
            {
                if (oldItem != null)
                {
                    contactRepository.CreateOrUpdateEntity(item.Contact, oldItem.Contact, userId);
                }
                else
                {
                    contactRepository.CreateOrUpdateEntity(item.Contact, null, userId);
                }
                
            }
            if (item.EventRegistrationActivityDayFullItem != null)
            {
                if (oldItem != null)
                {
                    eventRegistrationActivityDayRepository.CreateOrUpdateEntity(item.EventRegistrationActivityDayFullItem, oldItem.EventRegistrationActivityDayFullItem, userId);
                }
                else
                {
                    eventRegistrationActivityDayRepository.CreateOrUpdateEntity(item.EventRegistrationActivityDayFullItem, null, userId);
                }

            }

            //SetItemToEntity(item.Contact, oldItem.Contact, Entity entity, string userId);


        }
        private void SetClaimsAnswerList(string fieldName,string relationshipName, string obj, string guid, List<ClaimsAnswer> itemList, string userId, bool isCreate)
        {
            List<ClaimsAnswer> oldItemList = new List<ClaimsAnswer>();
            Guid.TryParse(guid, out Guid refEventRegistrationGuid);
            var relationshipNameCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(relationshipName);
            var objectCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj);
            var eventRegistrationCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("EventRegistration");
            oldItemList = eventRegistrationRepository.GetClaimsAnswers(userId, guid, fieldName);


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
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("EventRegistration"), refEventRegistrationGuid);
                        basicRepository.Disassociate(refa, refb, relationshipNameCrm, userId);
                    }
                }
            }

            foreach (var item in itemList)
            {
                Guid.TryParse(item.Id, out Guid refItemListGuid);

                if (oldItemList.Where(x => x.Id == item.Id).Count() == 0)
                {//is not in old list 
                    if (refItemListGuid != null && refEventRegistrationGuid != null)
                    {
                        EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj), refItemListGuid);
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("EventRegistration"), refEventRegistrationGuid);
                        basicRepository.Associate(refa, refb, relationshipNameCrm, userId);
                    }
                }
            }
        }
        public void creatRegistertionFromConferences(string id, string userId)
        {
            //dictionary of all the activity days that related to the confereneces
            Dictionary<string, string> eventRegistrationActivityDayIds = new Dictionary<string, string>();
            eventRegistrationActivityDayIds.Add("day1", "fd38c701-bee1-ed11-8129-00505691117e");
            eventRegistrationActivityDayIds.Add("day2", "0439c701-bee1-ed11-8129-00505691117e");
            eventRegistrationActivityDayIds.Add("day1, day2", "fd38c701-bee1-ed11-8129-00505691117e;0439c701-bee1-ed11-8129-00505691117e");

            //get all the registers from crm:
            string fetchXml = @"<fetch>" +
                "<entity name = 'new_express_seminar'>" +
                "<all-attributes />" +
                "<link-entity name = 'new_payment' from = 'new_express_seminar' to = 'new_express_seminarid' link-type='outer' alias = 'ab'>" +
                "<all-attributes />" +
                "<filter type = 'and' >" +
                "<condition attribute='statecode' operator='eq' value='0' />" +
                "</filter>" +
                "</link-entity>" +
                "<link-entity name='contact' from='contactid' to='new_contact' alias='ac'>"+
                "<all-attributes />" +
                "<filter type = 'and' >" +
                "<condition attribute='statecode' operator='eq' value='0' />" +
                "</filter>" +
                "</link-entity>" + 
                "<filter type = 'and' >" +
                "<condition attribute = 'new_conference' operator= 'eq' value = '{" + id +"}'/>" +
                "<condition attribute='statecode' operator='eq' value='0' />" +
                "</filter>" +
                "<order attribute='createdon' descending='false'/>"+
                "</entity>" +
                "</fetch>";
            var entitysList = basicRepository.GeneralFetchXml(userId, fetchXml);

            foreach (var entity in entitysList)
            {
                    EventRegistration register = setEntityToRegister(entity, userId);
                    register.Id = eventRegistrationRepository.Create(register, userId).ToString();

                    //associate objects to EventRegistration:
                    ClaimsAnswer PrimaryReasonsForRegistering = new ClaimsAnswer();
                    PrimaryReasonsForRegistering.Answer = queryHelperRepository.GetValueByAttribute(entity, "new_hear_about_via");
                    PrimaryReasonsForRegistering.Id = claimsAnswerRepository.Create(PrimaryReasonsForRegistering, userId).ToString();
                    List<ClaimsAnswer> PrimaryReasonsForRegisteringList = new List<ClaimsAnswer>();
                    PrimaryReasonsForRegisteringList.Add(PrimaryReasonsForRegistering);
                    register.PrimaryReasonsForRegistering = PrimaryReasonsForRegisteringList;

                    ClaimsAnswer TeachingMainSources = new ClaimsAnswer();
                    TeachingMainSources.Answer = queryHelperRepository.GetValueByAttribute(entity, "new_teach_holocaust");
                    TeachingMainSources.Id = claimsAnswerRepository.Create(TeachingMainSources, userId).ToString();
                    List<ClaimsAnswer> TeachingMainSourcesList = new List<ClaimsAnswer>();
                    TeachingMainSourcesList.Add(TeachingMainSources);
                    register.TeachingMainSources = TeachingMainSourcesList;

                    //create EventRegistrationActivityDay for the releted days:
                    var ActivityDay = queryHelperRepository.GetValueByAttribute(entity, "new_participating_days");
                    register.EventRegistrationActivityDay = eventRegistrationActivityDayIds[ActivityDay];

                    if (!string.IsNullOrEmpty(register.EventRegistrationActivityDay))
                    {
                        string[] ActivityDayIds = register.EventRegistrationActivityDay.Split(';');
                        foreach(var dayId in ActivityDayIds)
                        {
                            EventRegistrationActivityDay day = new EventRegistrationActivityDay()
                            {
                                Name = "",
                                ActivityDay = new IdValuePair() { Id = dayId },
                                EventRegistration = new IdValuePair() { Id = register.Id }
                            };
                            eventRegistrationActivityDayRepository.Create(day, userId);
                        }
                    }

                    register.PaymentReference = queryHelperRepository.GetValueByAttribute(entity, "ab.new_paymentid");
                    if (!string.IsNullOrEmpty(register.PaymentReference))
                    {
                        Payment oldPayment = paymentRepository.GetFetchXml(userId, register.PaymentReference);
                        //Payment newPayment = oldPayment;
                        //newPayment.EventRegistration = new IdValuePair() { Id = register.Id };
                        //newPayment.IsDirty = true;
                        //paymentRepository.CreateOrUpdateEntity(newPayment, oldPayment, userId);
                        UpdateCrmItem updateCrmItem = new UpdateCrmItem
                        {
                            Field = mapFunctionRepository.GetDestinationPropertyFor<Payment, PaymentCRM>("EventRegistration"),
                            Value = register.Id,
                            Type = "lookup"
                        };

                        paymentRepository.UpdateField(oldPayment.Id, updateCrmItem, userId);
                    }

                    //update the changes in the crm:
                    CreateOrUpdateEventRegistration(register, null, userId, false);
                
            }
        }

        private EventRegistration setEntityToRegister(Entity entity , string userId)
        {
            EventRegistration register = new EventRegistration();
            {
                register.EventReservation = new IdValuePair()
                {
                    Id = "f938c701-bee1-ed11-8129-00505691117e",
                    Value = "כנס מורים ישראלים"
                };
                register.DivisionRouter = new IdValuePair { Id = "1", Value = "ביס" };
                register.PassportNumber = queryHelperRepository.GetValueByAttribute(entity, "new_passport");
                register.Citizenship = queryHelperRepository.GetValueByAttribute(entity, "new_citizenship");
                register.CitySchool = queryHelperRepository.GetValueByAttribute(entity, "new_work_address_city");
                register.SchoolName = queryHelperRepository.GetValueByAttribute(entity, "new_place_of_work");
                register.RoleInSchool = queryHelperRepository.GetValueByAttribute(entity, "new_teacher_status");
                register.TeachingSubject = queryHelperRepository.GetValueByAttribute(entity, "new_profession");
                register.IsForGmul = queryHelperRepository.GetBoolValueByAttribute(entity, "new_gmul_needed");
                register.AgeGroup = queryHelperRepository.GetOptionsetValue(entity, "new_age_group");
                register.PaymentComments = queryHelperRepository.GetValueByAttribute(entity, "new_comments");
                register.TotalPrice = queryHelperRepository.GetMoneyValueByAttribute(entity, "new_conference_fee_original_cur");
                register.TotalPriceAfterDiscount = queryHelperRepository.GetDecimalValueByAttribute(entity, "new_payment_amount_usdollar");
                register.Discount = queryHelperRepository.GetMoneyValueByAttribute(entity, "new_payment_amount");
                register.NumberOfParticipants =1;

                register.Contact = new Contact()
                {
                    FirstName = queryHelperRepository.GetValueByAttribute(entity, "ac.firstname"),
                    LastName = queryHelperRepository.GetValueByAttribute(entity, "ac.lastname"),
                    IdNumber = queryHelperRepository.GetValueByAttribute(entity, "ac.firstname"),
                    Gender = null,
                    CommunicationInfo = new CommunicationInfo()
                    {
                        Telephone = queryHelperRepository.GetValueByAttribute(entity, "ac.telephone1"),
                        Email = queryHelperRepository.GetValueByAttribute(entity, "ac.emailaddress1"),
                        Mobile = queryHelperRepository.GetValueByAttribute(entity, "ac.mobilephone"),
                        Fax = queryHelperRepository.GetValueByAttribute(entity, "ac.fax")
                    },
                    Address = new AddressInfo()
                    {
                        City = queryHelperRepository.GetValueByAttribute(entity, "ac.address1_city"),
                        Address = queryHelperRepository.GetValueByAttribute(entity, "ac.address1_line1"),
                        ZipCode = queryHelperRepository.GetValueByAttribute(entity, "ac.address1_postalcode"),
                        MailingAddress = queryHelperRepository.GetValueByAttribute(entity, "ac.telephone1"),
                        Country = null,
                        State = "",
                    },
                    ReceiveEmails = new IdValuePair
                    {
                        Id = queryHelperRepository.GetOptionsetKey(entity, "ac.donotemail"),
                        Value = queryHelperRepository.GetOptionsetValue(entity, "ac.donotemail")
                    },
                    Id = queryHelperRepository.GetOptionsetValue(entity, "ac.contactid"),
                };
                register.Name = "רישום לאירוע --> " + register.Contact.LastName + " " + register.Contact.FirstName;
                //PaymentStatus = null,

            };
            return register;
        }

        public bool scheduleAndSendMailToRegister(string userId, List<EventRegistration> registraionList)
        {
            if(registraionList != null)
            {
                registraionList.ForEach(register =>
                {
                    List<Filter> filterList = new List<Filter>();
                    filterList.Add(new Filter
                    {
                        Field = "reportName",
                        Value = "182. Schedule - Participants at the conference"
                    });
                    filterList.Add(new Filter
                    {
                        Field = "renderingExtension",
                        Value = "PDF"
                    });
                    filterList.Add(new Filter
                    {
                        Field = "new_num_p",
                        Value = register.ReservationNum
                    });
                    filterList.Add(new Filter
                    {
                        Field = "prinDirection",
                        Value = "portrait"
                    });
                    FileItem file = reportsService.ExportToFile(filterList);
                    string YVAttachmentsUrl = ConfigurationManager.AppSettings[$"baseUrlAttachments_{environment}"].ToString();
                    file.FullPath = $"{YVAttachmentsUrl}/reports/{file.Name}";
                    if (file != null && !string.IsNullOrEmpty(file.FullPath))
                    {
                        reservationService.sendReminderEmail(file.FullPath, register.Id, "ScheduleForRegister", userId , register.Contact.CommunicationInfo.Email);
                    }
                });
            }
            else
            {
                return false;
            }
            
            return true;
        }
    }

}
