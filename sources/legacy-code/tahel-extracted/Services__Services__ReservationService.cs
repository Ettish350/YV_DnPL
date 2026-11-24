using Data.Repositories.Interfaces;
using Data.Modals;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Modals.CRMModals;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Configuration;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Services.SendMailWS;
using System.Globalization;
using System.IO;
using System.Xml;
using Data.Repositories;
using Microsoft.Reporting.Map.WebForms;
using static System.Net.WebRequestMethods;
using MailInfo = Data.Modals.MailInfo;
using Data.SendMailWS;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Direction = Data.Modals.Direction;
using sendMailSoapClient = Services.SendMailWS.sendMailSoapClient;

namespace Services.Services
{
    public class ReservationService : BasicService<ReservationItem, Reservation, ReservationCRM>, IReservationsService
    {
        private readonly IFullProductRepository fullProductRepository;
        private readonly IProductRepository productRepository;
        private readonly IReservationsRepository reservationsRepository;
        private readonly IEventReservationRepository eventReservationRepository;
        private readonly IActivityDayRepository activityDayRepository;
        private readonly IBidItemRepository bidItemRepository;
        private readonly ITransportationBidItemRepository transportationBidItemRepository;
        private readonly IKitBidItemRepository kitBidItemRepository;
        private readonly IItineraryGroupRepository itineraryGroupRepository;
        private readonly IItineraryItemRepository itineraryItemRepository;
        private readonly IEventItemRepository eventItemRepository;
        private readonly ILearningDivisionRepository learningDivisionRepository;
        private readonly IWorkshopRepository workshopRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IPricelistRepository pricelistRepository;
        private readonly IActivityDayTypeRepository activityDayTypeRepository;
        private readonly IScheduleDateRepository scheduleDateRepository;
        private readonly IDepartmentRepository departmentRepository;
        private readonly ISupporterRepository supporterRepository;
        private readonly IAlmogRepository almogRepository;
        private readonly IFullReservationListRepository fullReservationListRepository;
        private readonly IParticipantRepository participantRepository;
        private readonly IBudgetFrameworkRepository budgetFrameworkRepository;
        private readonly IAttachmentRepository attachmentRepository;
        private readonly IInstitutionsRepository institutionsRepository;
        private readonly IAlmogDetailsRepository almogDetailsRepository;
        private readonly IActivityDayService activityDayService;
        private readonly IIdValueListRepository idValueListRepository;
        private readonly IIdValueListService idValueListService;

        private readonly IServiceProviderRepository serviceProviderRepository;
        private readonly IPickupStationRepository pickupStationRepository;
        private readonly IProductService productService;
        private readonly IQueryHelperRepository queryHelperRepository;
        private readonly IResourceRepository resourceRepository;
        private readonly IContactRepository contactRepository;
        private readonly IReportsService reportsService;
        private readonly IRightToDiscountRepository rightToDiscountRepository;
        private readonly IEventRegistrationRepository eventRegistrationRepository;
        private readonly ICRMConnection crmConnection;

        private readonly string environment;

        public Objects Objects { get; } = new Objects();

        public ReservationService(IFullProductRepository fullProductRepository,
            IIdValueListRepository idValueListRepository,
            IIdValueListService idValueListService,
                                IProductRepository productRepository,
                                  IActivityDayTypeRepository activityDayTypeRepository,
                                  IHelperService helperService,
                                  IReservationsRepository reservationsRepository,
                                  IActivityDayRepository activityDayRepository,
                                  IBidItemRepository bidItemRepository,
                                  IItineraryGroupRepository itineraryGroupRepository,
                                  IItineraryItemRepository itineraryItemRepository,
                                  ILearningDivisionRepository learningDivisionRepository,
                                  IWorkshopRepository workshopRepository,
                                  IEventItemRepository eventItemRepository,
                                  IMapFunctionRepository mapFunctionRepository,
                                  IPricelistRepository pricelistRepository,
                                  IScheduleDateRepository scheduleDateRepository,
                                  IDepartmentRepository departmentRepository,
                                  ISupporterRepository supporterRepository,
                                  IAlmogRepository almogRepository,
                                  IFullReservationListRepository fullReservationListRepository,
                                  IParticipantRepository participantRepository,
                                  IBudgetFrameworkRepository budgetFrameworkRepository,
                                  IAttachmentRepository attachmentRepository,
                                  IInstitutionsRepository institutionsRepository,
                                  IAlmogDetailsRepository almogDetailsRepository,
                                  IActivityDayService activityDayService,
                                  IServiceProviderRepository serviceProviderRepository,
                                  IPickupStationRepository pickupStationRepository,
                                  ITransportationBidItemRepository transportationBidItemRepository,
                                  IKitBidItemRepository kitBidItemRepository,
                                  IEventReservationRepository eventReservationRepository,
                                  IProductService productService,
                                  IQueryHelperRepository queryHelperRepository,
                                  IResourceRepository resourceRepository,
                                  IContactRepository contactRepository,
                                  IReportsService reportsService,
                                  ICRMConnection crmConnection,
                                  IRightToDiscountRepository rightToDiscountRepository,
                                  IEventRegistrationRepository eventRegistrationRepository


            ) : base(helperService, reservationsRepository, mapFunctionRepository)
        {
            this.fullProductRepository = fullProductRepository;
            this.productRepository = productRepository;
            this.reservationsRepository = reservationsRepository;
            this.activityDayRepository = activityDayRepository;
            this.bidItemRepository = bidItemRepository;
            this.itineraryGroupRepository = itineraryGroupRepository;
            this.itineraryItemRepository = itineraryItemRepository;
            this.learningDivisionRepository = learningDivisionRepository;
            this.activityDayTypeRepository = activityDayTypeRepository;
            this.workshopRepository = workshopRepository;
            this.eventItemRepository = eventItemRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.pricelistRepository = pricelistRepository;
            this.scheduleDateRepository = scheduleDateRepository;
            this.departmentRepository = departmentRepository;
            this.supporterRepository = supporterRepository;
            this.almogRepository = almogRepository;
            this.fullReservationListRepository = fullReservationListRepository;
            this.participantRepository = participantRepository;
            this.budgetFrameworkRepository = budgetFrameworkRepository;
            this.attachmentRepository = attachmentRepository;
            this.institutionsRepository = institutionsRepository;
            this.almogDetailsRepository = almogDetailsRepository;
            this.activityDayService = activityDayService;
            this.idValueListRepository = idValueListRepository;
            this.idValueListService = idValueListService;
            this.serviceProviderRepository = serviceProviderRepository;
            this.pickupStationRepository = pickupStationRepository;
            this.transportationBidItemRepository = transportationBidItemRepository;
            this.kitBidItemRepository = kitBidItemRepository;
            this.eventReservationRepository = eventReservationRepository;
            this.productService = productService;
            this.queryHelperRepository = queryHelperRepository;
            this.resourceRepository = resourceRepository;
            this.contactRepository = contactRepository;
            this.reportsService = reportsService;
            this.rightToDiscountRepository = rightToDiscountRepository;
            this.crmConnection = crmConnection;
            this.reportsService = reportsService;
            this.eventRegistrationRepository = eventRegistrationRepository;
            environment = ConfigurationManager.AppSettings["environment"];


        }

        public override Tuple<int, List<Reservation>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            List<Sort> reservationSort = new List<Sort>();
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
                });

                reservationSort = helperService.BuildSorting<Reservation, ReservationCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "ActivityDay"));
                additionalSort.AddRange(helperService.BuildSorting<BudgetFramework, BudgetFrameworkCRM>(sort.FindAll(x => x.Alias == "BudgetFramework")));

            }

            List<Filter> reservationFilters = new List<Filter>();
            List<Filter> institutionFilters = new List<Filter>();
            List<Filter> activityDayFilters = new List<Filter>();
            List<Filter> bidItemFilters = new List<Filter>();
            List<Filter> budgetFrameworkFilters = new List<Filter>();
            if (filter != null)
            {

                reservationFilters = helperService.BuildFilter<Reservation, ReservationCRM, ReservationCRM>(filter.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "Reservation"));
                institutionFilters.AddRange(helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filter.FindAll(x => x.Alias == "Institution")));
                budgetFrameworkFilters.AddRange(helperService.BuildFilter<BudgetFramework, BudgetFrameworkCRM, BudgetFrameworkCRM>(filter.FindAll(x => x.Alias == "BudgetFramework")));
                activityDayFilters.AddRange(helperService.BuildFilter<ActivityDay, ActivityDayCRM, ActivityDayCRM>(filter.FindAll(x => x.Alias == "ActivityDay")));
                bidItemFilters.AddRange(helperService.BuildFilter<BidItem, BidItemCRM, BidItemCRM>(filter.FindAll(x => x.Alias == "BidItem")));
            }

            if (isAggregate)
            {
                fetchXml = @"<attribute name='new_number_of_groups' alias='new_number_of_groups' aggregate='sum' />
            <attribute name='new_number_of_participants' alias='new_number_of_participants' aggregate='sum' />";
            }
            else
            {
                fetchXml = @"<all-attributes/>";
            }
            fetchXml += institutionsRepository.GetLinkedEntity(isAggregate, institutionFilters, additionalSort);
            fetchXml += budgetFrameworkRepository.GetLinkedEntity(isAggregate, budgetFrameworkFilters, null);
            //fetchXml += activityDayRepository.GetLinkedEntity(isAggregate, additionalFilters, additionalSort, "inner");
            if (filter.FindAll(x => x.Alias == "ActivityDay").Count > 0)
            {
                fetchXml += @"<link-entity name='new_activity_day' from='new_reservation' to='new_reservationid' alias='ActivityDay'>";

                if (activityDayFilters != null && activityDayFilters.Count > 0)
                {
                    fetchXml += "<filter type = 'and'>";
                    fetchXml += basicRepository.FilterFetchXML(activityDayFilters);
                    fetchXml += "</filter>";
                }

                //need to be neasted in ActivityDay
                if (filter.FindAll(x => x.Alias == "BidItem").Count > 0)
                {
                    fetchXml += @"<link-entity name='new_bid' from='new_activity_day' to='new_activity_dayid' alias='BidItem'>";

                    if (bidItemFilters != null && bidItemFilters.Count > 0)
                    {
                        fetchXml += "<filter type = 'and'>";
                        fetchXml += basicRepository.FilterFetchXML(bidItemFilters);
                        fetchXml += "</filter>";
                    }
                    fetchXml += "</link-entity>";
                }

                fetchXml += "</link-entity>";
            }
            
            Tuple<int, List<Reservation>> reservationList = reservationsRepository.GetFetchXml(userId, isAggregate, paging, reservationSort, reservationFilters, fetchXml);

            return reservationList;

        }
        public override ReservationItem GetFetchXml(string userId, string guid)
        {
            ReservationItem item = reservationsRepository.GetFetchXml(userId, guid);
            //unslash for separate loading - start
            //item.ActivityDayList = activityDayService.GetActivityDaysForReservation(userId, guid);
            //unslash for separate loading - end
            return item;
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

            return reservationsRepository.GetList(paging, filters, userId, fetchXml);

        }

        public override Guid Create(ReservationItem item, string userId)
        {
            if (item.ReservationStatus == null)
            {
                IdValuePair status = new IdValuePair();
                status.Id = ((int)ReservationStatus.New).ToString();
                item.ReservationStatus = status;
            }
            var guid = reservationsRepository.Create(item, userId);
            item.Id = guid.ToString();
            item.ReservationNum = reservationsRepository.GetReservationNum(guid, userId);
            item.GenerateSendAutomaticInvoice = reservationsRepository.GetGenerateSendAutomaticInvoice(guid, userId);
            CreateOrUpdateReservation(item, null, userId, true);
            return guid;
        }
        public override void Update(ReservationItem item, ReservationItem oldItem, string userId)
        {
            reservationsRepository.Update(item, oldItem, userId);
            CreateOrUpdateReservation(item, oldItem, userId);
        }

        public override void Delete(ReservationItem item, string userId)
        {
            InheritIsDeletedToChildrenEntities(item);
            reservationsRepository.Delete(item, userId);
            DeleteReservationEntities(item, userId);
        }
        public override void Cancel(ReservationItem item, string userId, bool isCancel = true)
        {
            InheritIsCanceledToChildrenEntities(item, isCancel);
            reservationsRepository.Cancel(item, userId, isCancel);
            CancelReservationEntities(item, userId);
        }

        public List<ReservationItem> GetFullReservationList(Paging paging, List<Sort> sort, List<Filter> filters, string userId)
        {
            return fullReservationListRepository.GetFetchXml(userId, false, paging, sort, filters).Item2;
        }

        public static object ConvertList(List<object> value, Type type)
        {
            var containedType = type.GenericTypeArguments.First();
            return value.Select(item => Convert.ChangeType(item, containedType)).ToList();
        }
        public Product GetBreakProduct(string userId)
        {

            var breakProductGuid = ConfigurationManager.AppSettings[$"breakProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        public IdValuePair GetIsraelCountry(string userId)
        {
            var israelCountryGuid = ConfigurationManager.AppSettings[$"israelCountryId_{environment}"];
            return new IdValuePair() { Id = israelCountryGuid, Value = "ישראל" };
        }
        public IdValuePair GetHebrewLanguage(string userId)
        {
            var hebrewLanguageGuid = ConfigurationManager.AppSettings[$"hebrewLanguageId_{environment}"];
            return new IdValuePair() { Id = hebrewLanguageGuid, Value = "עברית" };
        }

        public Product GetAudioGuideProduct(string userId)
        {
            var audioGuideProductGuid = ConfigurationManager.AppSettings[$"audioGuideProductId_{environment}"];
            return productRepository.GetFetchXml(userId, audioGuideProductGuid);
        }

        public Product GetHeadsetProduct(string userId)
        {
            var headsetProductGuid = ConfigurationManager.AppSettings[$"headsetProductId_{environment}"];
            return productRepository.GetFetchXml(userId, headsetProductGuid);
        }

        public Product GetMuseumProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"museumProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        //public Product GetEducationMobilityTransportationProduct(string userId)
        //{
        //    var ProductGuid = ConfigurationManager.AppSettings[$"educationalMobilityTransportation_{environment}"];
        //    return productRepository.Get(userId, ProductGuid);
        //}
        public Product GetCampusProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"campusProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }
        public Product GetRegistrationFeeProduct(string userId)
        {
            var registrationFeeProductGuid = ConfigurationManager.AppSettings[$"registrationFeeProductId_{environment}"];
            return productRepository.GetFetchXml(userId, registrationFeeProductGuid);
        }
        public Product GetBigQuestionsRoomProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"bigQuestionsRoomProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        public Product GetHolocaustPerformanceProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"holocaustPerformanceProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        public Product GetTheLifeBeforeProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"theLifeBeforeProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        public Product GetBackToLifeProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"backToLifeProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        public Product GetBhadimCityClassesProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"bhadimCityClassesProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        public Product GetBhadimCityProduct(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"bhadimCityProductId_{environment}"];
            return productRepository.GetFetchXml(userId, breakProductGuid);
        }

        public List<IdValuePair> GetListByDepartment(string userId, string guid, string listType)
        {
            var department = departmentRepository.GetFetchXml(userId, guid);
            if (listType == "supporter")
            {
                return department.Supporters;
            }
            else if (listType == "partner")
            {
                return department.Partners;
            }
            else if (listType == "activityDaySubject")
            {
                return department.ActivityDaySubjects;
            }
            return null;
        }
        public ActivityDayType GetActivityDayType(string userId, string guid)
        {
            return activityDayTypeRepository.GetFetchXml(userId, guid);
        }

        /*public Product GetProduct(string userId, string guid)
        {
            return productRepository.GetFetchXml(userId, guid);
        }*/

        /*public List<Pricelist> GetProductPricelists(string userId, string productGuid)
        {
            var filterList = new List<Filter>();

            filterList.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<Pricelist, PricelistCRM>("Product"),
                Value = productGuid,
                Type = "lookup"
            });

            var pricelist = pricelistRepository.GetFetchXml(userId, false, filterList).Item2;
            string message = "something went wrong in GetProductPricelist function";
            if (pricelist.Count == 0)
            {
                return null;
            }
            else
            {
                return pricelist;
            }

            throw new Exception(message);
        }*/

        public DataForInvoicePdf GetAlmogInvoice(string userId, string invoiceNumber)
        {
            try
            {
                DataForInvoicePdf dataForInvoicePdf = new DataForInvoicePdf();
                dataForInvoicePdf.tables = new List<TableDetails>();
                List<Filter> filterList = new List<Filter>();
                Filter filter = new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<AlmogItem, AlmogItemCRM>("InvoiceNumber"),
                    Value = invoiceNumber,
                    Type = "string"
                };
                filterList.Add(filter);

                //.GetFetchXml(userId, null, null, filterList, fetchXml);

                Tuple<int, List<AlmogItem>> almogInvoiceResult = almogRepository.GetFetchXml(userId, false, filterList);

                if (almogInvoiceResult.Item1 != 1)
                {
                    //shouldn't there be only one?
                    throw new Exception("הנתונים ליצירת הקובץ חסרים");
                }
                if (almogInvoiceResult.Item2[0].InvoiceScope.Id == ((int)InvoiceScope.Union).ToString())//union
                {
                    //throw new Exception("לא ניתן להפיק  קובץ עבור חשבון מאוחד");
                }
                dataForInvoicePdf.InvoiceScope = almogInvoiceResult.Item2[0].InvoiceScope.Id;
                InstitutionItem institution = institutionsRepository.GetFetchXml(userId, almogInvoiceResult.Item2[0].InstitutionGuid);
                if (institution.PayingInstitution != null)//יש לקוח משלם
                {
                    dataForInvoicePdf.name = institution.PayingInstitution.Name;
                    dataForInvoicePdf.address = string.Empty;
                    dataForInvoicePdf.relatedAccountName = institution.Name;
                    dataForInvoicePdf.relatedAccountAddress = institution.AddressInfo.Address + " " + institution.AddressInfo.City + " " + institution.AddressInfo.ZipCode;
                }
                else// אין לקוח משלם
                {
                    dataForInvoicePdf.name = institution.Name;
                    if (institution.AddressInfo != null)
                        dataForInvoicePdf.address = institution.AddressInfo.Address + " " + institution.AddressInfo.City + " " + institution.AddressInfo.ZipCode;
                    dataForInvoicePdf.relatedAccountName = string.Empty;
                    dataForInvoicePdf.relatedAccountAddress = string.Empty;
                }
                dataForInvoicePdf.semel = institution.institutionCompanyNumberOrSymbol;
                dataForInvoicePdf.status = "Copy";
                dataForInvoicePdf.institutionGuid = institution.Id;

                if (almogInvoiceResult.Item2[0].TransactionType.Value == "ביטול")
                {
                    dataForInvoicePdf.isCancel = true;
                }

                //if (almogInvoiceResult.Item2[0].InvoiceType.Id == ((int)InvoiceType.Manual).ToString())
                //{
                //dataForInvoicePdf.groupName = reservationItem.GroupName;
                //dataForInvoicePdf.reservationNum = reservationItem.ReservationNum;

                var table = new TableDetails();
                table.totalPrice = String.Format("{0:0.##}", almogInvoiceResult.Item2[0].TotalPrice);
                table.totalPriceAfterDiscount = String.Format("{0:0.##}", almogInvoiceResult.Item2[0].TotalPriceAfterDiscount);
                table.discount = String.Format("{0:0.##}", almogInvoiceResult.Item2[0].Discount);
                table.discountType = almogInvoiceResult.Item2[0].DiscountType.Id;

                if (almogInvoiceResult.Item2[0].Language != null && almogInvoiceResult.Item2[0].Language != string.Empty)
                {
                    dataForInvoicePdf.lang = almogInvoiceResult.Item2[0].Language;
                }
                else
                {
                    dataForInvoicePdf.lang = "עברית";
                }

                dataForInvoicePdf.comments = almogInvoiceResult.Item2[0].Comments;
                dataForInvoicePdf.invoiceNum = almogInvoiceResult.Item2[0].InvoiceNumber;
                if (almogInvoiceResult.Item2[0].Currency != null)
                {
                    dataForInvoicePdf.currencyGuid = almogInvoiceResult.Item2[0].Currency.Id;
                }
                table.contact = almogInvoiceResult.Item2[0].Contact;


                if (almogInvoiceResult.Item2[0].BudgetFramework != null && almogInvoiceResult.Item2[0].BudgetFramework.Id != string.Empty)
                {
                    BudgetFramework bf = budgetFrameworkRepository.GetFetchXml(userId, almogInvoiceResult.Item2[0].BudgetFramework.Id);
                    dataForInvoicePdf.department = bf.Department.Value;
                }
                table.reservationNum = almogInvoiceResult.Item2[0].ReservationNumber;
                dataForInvoicePdf.departmentGuid = almogInvoiceResult.Item2[0].Department.Id;
                dataForInvoicePdf.division = almogInvoiceResult.Item2[0].Division.Id;

                dataForInvoicePdf.tables.Add(table);

                table.tableRowDetails = new List<TableRowDetails>();
                TableRowDetails tableRowDetails;
                filterList = new List<Filter>();
                filter = new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<AlmogItemDetails, AlmogItemDetailsCRM>("AlmogItem"),
                    Value = almogInvoiceResult.Item2[0].Id,
                    Type = "lookup"
                };
                filterList.Add(filter);
                Tuple<int, List<AlmogItemDetails>> almogItemDetailsResult = almogDetailsRepository.GetFetchXml(userId, false, filterList);

                foreach (AlmogItemDetails almogItemDetails in almogItemDetailsResult.Item2)
                {
                    tableRowDetails = new TableRowDetails();
                    tableRowDetails.amount = almogItemDetails.Amount.ToString();

                    tableRowDetails.date = almogItemDetails.Date.ToString("yyyy-MM-dd");
                    tableRowDetails.dateForDisplay = almogItemDetails.Date.ToString("dd/MM/yyyy");
                    tableRowDetails.discount = String.Format("{0:0}", almogItemDetails.Discount);
                    tableRowDetails.discountType = almogItemDetails.DiscountType.Id;
                    tableRowDetails.language = "";// almogItemDetails.Language.Id;
                    tableRowDetails.perut = almogItemDetails.Details;
                    tableRowDetails.price = String.Format("{0:0.##}", almogItemDetails.Price);
                    tableRowDetails.total = String.Format("{0:0.##}", almogItemDetails.TotalPrice);
                    tableRowDetails.totalAfterDiscount = String.Format("{0:0.##}", almogItemDetails.TotalPriceAfterDiscount);
                    tableRowDetails.almogSubCode = almogItemDetails.AlmogSubCode;
                    tableRowDetails.sku = almogItemDetails.Sku;
                    tableRowDetails.reservation = almogItemDetails.Reservation;
                    table.tableRowDetails.Add(tableRowDetails);
                }
                dataForInvoicePdf.InvoiceTotalPrice = String.Format("{0:0.##}", almogInvoiceResult.Item2[0].TotalPrice);
                dataForInvoicePdf.InvoiceTotalPriceAfterDiscount = String.Format("{0:0.##}", almogInvoiceResult.Item2[0].TotalPriceAfterDiscount);
                // }
                // else {//type automatic:
                //taking first. Might need to change this in the future
                //     ReservationItem reservation = reservationsRepository.GetFetchXml(userId, almogInvoiceResult.Item2[0].ReservationGuidList[0]);

                // }
                return dataForInvoicePdf;

            }
            catch (Exception ex)
            {

                throw new Exception("error getting details for invoiceNumber: " + invoiceNumber);
            }

        }

        public void UpdateAlmogNumber(AlmogToCrm almogToCrm, string userId)
        {

            Trace.WriteLine("in UpdateAlmogNumber almogToCrm: ");

            string jsonTemp = JsonConvert.SerializeObject(almogToCrm);
            Trace.WriteLine(jsonTemp);

            ReservationCRM reservationCRM = new ReservationCRM();
            AlmogItem almogItem = new AlmogItem();
            List<ReservationItem> reservationList = new List<ReservationItem>();
            almogItem.InvoiceNumber = almogToCrm.InvoiceNumber;
            almogItem.InvoiceId = almogToCrm.InvoiceId;
            almogItem.Division = new IdValuePair();
            almogItem.Division.Id = almogToCrm.Division;
            almogItem.Department = new IdValuePair();
            almogItem.Department.Id = almogToCrm.DepartmentGuid;
            almogItem.Language = almogToCrm.Lang;
            almogItem.InvoiceType = new IdValuePair()
            {
                Id = ((int)almogToCrm.InvoiceType).ToString()
            };
            almogItem.InvoiceScope = new IdValuePair()
            {
                Id = ((int)almogToCrm.InvoiceScope).ToString()
            };
            if (!string.IsNullOrEmpty(almogToCrm.CurrencyGuid))
                almogItem.Currency = new IdValuePair()
                {
                    Id = almogToCrm.CurrencyGuid.ToString()
                };
            //TransactionType Credit = 1,
            //TransactionType Cancel = 2
            almogItem.TransactionType = new IdValuePair()
            {
                Id = ((int)almogToCrm.TransactionType).ToString()
            };
            Trace.Write("1");
            if (string.IsNullOrEmpty(almogToCrm.InvoiceToCancel) && almogToCrm.InstitutionGuid == string.Empty && almogToCrm.ReservationGuidList.Count == 0 && almogToCrm.EventReservationGuidList.Count == 0)
            {
                Trace.Write("3");
                throw new Exception("invoice to cancel is empty , there is no guid to connect to");
            }
            if (!string.IsNullOrEmpty(almogToCrm.InvoiceToCancel)) //means this is cancel mode)
            {
                Trace.Write("4");
                //need to go get the guid's of reservations and institution related to theinvoice to cancel,
                //so we can relate the new created invoice of cancel to the same guid's
                List<Filter> filterList = new List<Filter>();
                Filter filter = new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<AlmogItem, AlmogItemCRM>("InvoiceNumber"),
                    Value = almogToCrm.InvoiceToCancel,
                    Type = "string"
                };
                filterList.Add(filter);
                var almogInvoiceList = almogRepository.GetFetchXml(userId, false, filterList).Item2;
                if (almogInvoiceList != null && almogInvoiceList.Count == 1)
                {
                    var almogInvoice = almogInvoiceList[0];

                    var relationshipName = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogRelation");
                    //var linkedReservations = reservationsRepository.GetLinkedItemList(userId, relationshipName, "new_almog_invoice", almogInvoice.Id);
                    //WE DONT KNOW WHEN IT REACHES THIS LINE 
                    //SO WE DONT KNOW IF THIS WORKS!
                    //NOT TESTED!
                    Trace.Write("WE DONT KNOW WHEN IT REACHES THIS LINE.... NOT TESTED!");
                    var linkedReservations = reservationsRepository.GetLinkedItemListFetchXml(userId, relationshipName, "new_almog_invoice", almogInvoice.Id);
                    almogToCrm.InstitutionGuid = almogInvoice.InstitutionGuid;
                    if (almogToCrm.ReservationGuidList == null)
                    {
                        almogToCrm.ReservationGuidList = new List<string>();
                    }
                    foreach (var item in linkedReservations)
                    {
                        almogItem.ReservationNumber += item.ReservationNum + " ";
                        almogToCrm.ReservationGuidList.Add(item.Id);
                    }

                    var eventReservationRelation = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogEventReservationRelation");
                    var linkedEventReservations = eventReservationRepository.GetLinkedItemListFetchXml(userId, eventReservationRelation, "new_almog_invoice", almogInvoice.Id);
                    almogToCrm.InstitutionGuid = almogInvoice.InstitutionGuid;

                    foreach (var item in linkedEventReservations)
                    {
                        almogToCrm.EventReservationGuidList.Add(item.Id);
                    }

                    var activityDayRelation = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogActivityDayRelation");
                    var linkedActivityDays = activityDayRepository.GetLinkedItemListFetchXml(userId, activityDayRelation, "new_almog_invoice", almogInvoice.Id);

                    foreach (var item in linkedActivityDays)
                    {
                        almogToCrm.ActivityDayGuidList.Add(item.Id);
                    }

                    var bidItemRelation = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogBidItemRelation");
                    var linkedBidItems = bidItemRepository.GetLinkedItemListFetchXml(userId, bidItemRelation, "new_almog_invoice", almogInvoice.Id);
                    if (almogToCrm.BidItemGuidList == null)
                    {
                        almogToCrm.BidItemGuidList = new List<string>();
                    }

                    foreach (var item in linkedBidItems)
                    {
                        almogToCrm.BidItemGuidList.Add(item.Id);
                    }
                }

                if (string.IsNullOrEmpty(almogToCrm.InstitutionGuid) && almogToCrm.ReservationGuidList.Count == 0 && almogToCrm.EventReservationGuidList.Count == 0)
                {
                    throw new Exception("No guid was fount for invoice number: " + almogToCrm.InvoiceToCancel);
                }

            }
            Trace.Write("5");
            almogItem.InstitutionGuid = almogToCrm.InstitutionGuid;
            almogItem.Date = DateTime.Now;

            if (almogToCrm.InvoiceType == InvoiceType.Manual)
            {
                Trace.Write("manual start");
                //ידני
                almogItem.BudgetFramework = new IdValuePair();
                almogItem.BudgetFramework.Id = almogToCrm.BudgetFrameworkGuid;
                almogItem.Comments = almogToCrm.Comments;
                almogItem.Contact = almogToCrm.Contact;

                almogItem.DiscountType = new IdValuePair();
                almogItem.DiscountType.Id = almogToCrm.DiscountTypeId;
                Decimal.TryParse(almogToCrm.Discount, out decimal tempD);
                almogItem.Discount = tempD;
                Decimal.TryParse(almogToCrm.TotalPrice, out tempD);
                almogItem.TotalPrice = tempD;
                Decimal.TryParse(almogToCrm.TotalPriceAfterDiscount, out tempD);
                almogItem.TotalPriceAfterDiscount = tempD;
                almogItem.EventReservationCode = almogToCrm.EventReservationCode;
                Trace.Write("manual end");

            }
            else
            {

                Trace.Write("automatic start");

                foreach (var res in almogToCrm.ReservationGuidList)
                {
                    reservationList.Add(reservationsRepository.GetFetchXml(userId, res));
                }
                // reservation = reservationsRepository.GetFetchXml(userId, almogToCrm.ReservationGuidList[0]);
                foreach (var res in reservationList)
                {

                    almogItem.ReservationNumber += res.ReservationNum + " ";
                    almogItem.BudgetFramework = new IdValuePair();
                    almogItem.BudgetFramework.Id = res.BudgetFramework.Id;
                    if(res.YvComments.CompareTo(almogToCrm.Comments)!=0)
                    {
                        almogItem.Comments += res.YvComments + " " + almogToCrm.Comments;
                    }
                    else
                    {
                        almogItem.Comments += res.YvComments;
                    }
                    almogItem.Contact += res.Contact.FirstName + " " + res.Contact.LastName;
                    almogItem.DiscountType = new IdValuePair();
                    Trace.Write("6");
                    if (res.DiscountType.Id == "")
                    {
                        almogItem.DiscountType.Id = "0";
                    }
                    else
                    {
                        almogItem.DiscountType.Id = res.DiscountType.Id;
                    }
                    Decimal.TryParse(almogToCrm.Discount, out decimal tempD);
                    almogItem.Discount = tempD;
                    Decimal.TryParse(almogToCrm.TotalPrice, out tempD);
                    almogItem.TotalPrice = tempD;
                    Decimal.TryParse(almogToCrm.TotalPriceAfterDiscount, out tempD);
                    almogItem.TotalPriceAfterDiscount = tempD;
                
                    Trace.Write("automatic end");
                }
            }
            Trace.Write("before create");
            var AlmogItemGuid = almogRepository.Create(almogItem, userId);
            Trace.Write("after create");

            EntityReference refa = new EntityReference("new_almog_invoice", AlmogItemGuid);
            EntityReference refb;
            foreach (var reservationGuid in almogToCrm.ReservationGuidList)
            {
                Trace.Write("in associate");
                refb = new EntityReference("new_reservation", Guid.Parse(reservationGuid));
                basicRepository.Associate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogRelation"), userId);
            }
            foreach (var eventReservationGuid in almogToCrm.EventReservationGuidList)
            {
                Trace.Write("in associate");
                refb = new EntityReference("new_event_reservations", Guid.Parse(eventReservationGuid));
                basicRepository.Associate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogEventReservationRelation"), userId);
            }
            foreach (var activityDayGuid in almogToCrm.ActivityDayGuidList)
            {
                Trace.Write("in associate");
                refb = new EntityReference("new_activity_day", Guid.Parse(activityDayGuid));
                basicRepository.Associate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogActivityDayRelation"), userId);
            }
            if (almogToCrm.BidItemGuidList != null)
            {
                foreach (var bidItemGuid in almogToCrm.BidItemGuidList)
                {
                    Trace.Write("in associate");
                    refb = new EntityReference("new_bid", Guid.Parse(bidItemGuid));
                    basicRepository.Associate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogBidItemRelation"), userId);
                }
            }


            if (almogToCrm.InvoiceType == InvoiceType.Manual)
            {
                foreach (TableRowDetails invoiceItem in almogToCrm.InvoiceItemList)
                {
                    AlmogItemDetails almogItemDetails = new AlmogItemDetails();
                    Int32.TryParse(invoiceItem.amount, out int tempI);
                    almogItemDetails.Amount = tempI;
                    DateTime.TryParse(invoiceItem.date, out DateTime tempDate);
                    almogItemDetails.Date = tempDate;
                    almogItemDetails.Details = invoiceItem.perut;
                    Decimal.TryParse(invoiceItem.discount, out decimal tempD);
                    almogItemDetails.Discount = tempD;
                    almogItemDetails.DiscountType = new IdValuePair();
                    almogItemDetails.DiscountType.Id = invoiceItem.discountType;
                    Decimal.TryParse(invoiceItem.price, out tempD);
                    almogItemDetails.Price = tempD;
                    Decimal.TryParse(invoiceItem.total, out tempD);
                    almogItemDetails.TotalPrice = tempD;
                    Decimal.TryParse(invoiceItem.totalAfterDiscount, out tempD);
                    almogItemDetails.TotalPriceAfterDiscount = tempD;
                    almogItemDetails.AlmogSubCode = invoiceItem.almogSubCode;
                    almogItemDetails.Sku = invoiceItem.sku;
                    almogItemDetails.Reservation = invoiceItem.reservation;


                    //if (reservation.DivisionRouter == "1")//school
                    //{
                    //    almogItemDetails.AlmogSubCode = reservation.BudgetFramework.RevenueSection;
                    //}
                    //if (reservation.DivisionRouter == "2")//commemoration
                    //{
                    //    almogItemDetails.AlmogSubCode = "todo";
                    //}
                    if (!string.IsNullOrEmpty(almogToCrm.CurrencyGuid))
                        almogItemDetails.Currency = new IdValuePair()
                        {
                            Id = almogToCrm.CurrencyGuid.ToString()
                        };
                    almogItemDetails.AlmogItem = AlmogItemGuid.ToString();
                    Trace.Write("before manual create row");
                    Guid t = almogDetailsRepository.Create(almogItemDetails, userId);
                    Trace.Write("after manual create row");
                    //refb = new EntityReference("new_almog_invoice_item", AlmogItemGuid);
                    //basicRepository.Associate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("AlmogRelation"), userId);
                }
            }
            else
            {                
                foreach (var res in reservationList)
                {
                    if (almogToCrm.ActivityDayGuidList != null && almogToCrm.ActivityDayGuidList.Count > 0)
                    {
                        ReservationItem reservation = new ReservationItem();
                        reservation = reservationsRepository.GetFetchXml(userId, almogToCrm.ReservationGuidList[0]);//when has activity day list it is always only for one reservation
                        reservation.ActivityDayList = reservation.ActivityDayList.Where(activityDay => almogToCrm.ActivityDayGuidList.Find(id => id == activityDay.Id) != null).ToList();
                    }
                    if (almogToCrm.BidItemGuidList != null && almogToCrm.BidItemGuidList.Count > 0)
                    {
                        for (int i = 0; i < res.ActivityDayList.Count; i++)
                        {
                            res.ActivityDayList[i].BidItemList =
                                res.ActivityDayList[i].BidItemList.Where(bidItem => almogToCrm.BidItemGuidList.Contains(bidItem.Id)).ToList();
                        }
                    }

                    foreach (ActivityDay activityDay in res.ActivityDayList)
                    {
                        if (activityDay.BidItemList.Count > 0)
                        {
                            List<Product> productsList = new List<Product>();

                            //if (res.DivisionRouter == "2")
                            //{
                            Filter filter = new Filter
                            {
                                Field = "Id",
                                Type = "lookup",
                                Operator = "multiple",
                                Alias = "Product",
                            };

                            foreach (var bid in activityDay.BidItemList)
                            {

                                if (filter.Value != null)
                                {
                                    filter.Value += ";" + bid.Product.Id;
                                }
                                else
                                {
                                    filter.Value = bid.Product.Id;
                                }
                            }
                            string additionalFetchXml = @"<link-entity name='new_almog_interface' from='new_almog_interfaceid' to='new_almog_interface' alias='AlmogInterface' link-type='outer'>
                                                    <all-attributes />
                                                  </link-entity>";
                            //get all products for almogSubCode;
                            productsList = productService.GetAll(userId, "", filter, additionalFetchXml);
                            // }
                            foreach (BidItem bidItem in activityDay.BidItemList)
                            {
                                AlmogItemDetails almogItemDetails = new AlmogItemDetails();

                                almogItemDetails.Amount = bidItem.Amount;
                                DateTime.TryParse(activityDay.StartDateTime, out DateTime tempDate);
                                DateTime dt;
                                DateTime.TryParseExact(activityDay.StartDateTime,
                                                       "yyyy-MM-dd hh:mm tt",
                                                       CultureInfo.InvariantCulture,
                                                       DateTimeStyles.None,
                                                       out dt);
                                almogItemDetails.Date = tempDate;
                                almogItemDetails.Details = bidItem.Product.Value;
                                almogItemDetails.Discount = bidItem.Discount;
                                almogItemDetails.DiscountType = new IdValuePair();
                                almogItemDetails.DiscountType.Id = bidItem.DiscountType.Id;
                                almogItemDetails.Price = bidItem.UnitPrice;
                                almogItemDetails.TotalPrice = bidItem.TotalPrice;
                                almogItemDetails.TotalPriceAfterDiscount = bidItem.TotalPriceAfterDiscount;
                                almogItemDetails.Reservation = res.Id;
                                almogItemDetails.AlmogItem = AlmogItemGuid.ToString();
                                Product bidProduct = productsList.Find(p => p.Id == bidItem.Product.Id);
                                //if (res.DivisionRouter == "1")//school
                               // {
                                    if (!string.IsNullOrEmpty(bidProduct.RevenueSection))
                                    {
                                        almogItemDetails.AlmogSubCode = bidProduct.RevenueSection;
                                    }
                                    else
                                    {

                                        almogItemDetails.AlmogSubCode = res.BudgetFramework?.RevenueSection;
                                    }
                               // }
                                if (res.DivisionRouter == "2")//commemoration
                                {
                                    almogItemDetails.Sku = bidProduct.Sku; // bidItem.Product.RevenueSection;
                                }
                                Trace.Write("before automatic create row");

                                Guid t = almogDetailsRepository.Create(almogItemDetails, userId);
                                Trace.Write("after automatic create row");

                            }
                        }
                    }
                }


            }
            if (almogToCrm.ReservationGuidList.Count > 0)
            {
                //update payment status
                foreach (var guid in almogToCrm.ReservationGuidList)
                {
                    string currentStatus = "16";//סטטוס הופק חשבון חלקי
                    if (((int)almogToCrm.TransactionType).ToString() == "2")//cancel
                    {
                        currentStatus = "15";//סטטוס חשבון בוטל
                    }
                    else if(almogToCrm.InvoiceType == InvoiceType.Manual)
                    {
                        currentStatus = "";
                    }
                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<ReservationItem, ReservationCRM>("PaymentStatus"),
                        Value = currentStatus,
                        Type = "optionset"
                    }; 
                    


                    reservationsRepository.UpdateField(guid, updateCrmItem, userId);
                }
                foreach (var guid in almogToCrm.ActivityDayGuidList)
                {
                    string currentStatus = "16";//סטטוס הופק חשבון חלקי
                    if (((int)almogToCrm.TransactionType).ToString() == "2")//cancel
                    {
                        currentStatus = "15";//סטטוס חשבון בוטל
                    }

                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("PaymentStatus"),
                        Value = currentStatus,
                        Type = "optionset"
                    };
                    activityDayRepository.UpdateField(guid, updateCrmItem, userId);
                }
                if(almogToCrm.BidItemGuidList != null)
                {
                    foreach (var guid in almogToCrm.BidItemGuidList)
                    {
                        if (((int)almogToCrm.TransactionType).ToString() == "2")//cancel
                        {
                             UpdateCrmItem updateCrmItem = new UpdateCrmItem
                             {
                                 Field = mapFunctionRepository.GetDestinationPropertyFor<BidItem, BidItemCRM>("PaymentStatus"),
                                 Value = "15" ,//סטטוס חשבון בוטל,
                                 Type = "optionset"
                             };
                             bidItemRepository.UpdateField(guid, updateCrmItem, userId);
                        }
                    }

                }
            }
            else if (almogToCrm.EventReservationGuidList.Count > 0)
            {
                foreach (var guid in almogToCrm.EventReservationGuidList)
                {
                    string currentStatus = "16";//סטטוס הופק חשבון חלקי
                    if (((int)almogToCrm.TransactionType).ToString() == "2")//cancel
                    {
                        currentStatus = "15";//סטטוס חשבון בוטל
                    }

                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<EventReservationItem, EventReservationCRM>("PaymentStatus"),
                        Value = currentStatus,
                        Type = "optionset"
                    };
                    eventReservationRepository.UpdateField(guid, updateCrmItem, userId);
                }
            }
            Trace.Write("finish function");
        }


        // ------------------------------------------ private functions ------------------------------

        private void CreateOrUpdateReservation(ReservationItem item, ReservationItem oldItem, string userId, bool isCreate = false)
        {


            SetIdValueList(Objects.ReservationPartnerListRelation, Objects.Supporter, item.Id, item.ReservationPartnerList, userId, isCreate);
            try
            {
                SetActivityDays(userId, true, item.DivisionRouter, item.Id, item.ActivityDayList, oldItem?.ActivityDayList);
            }
            catch (Exception ex)
            {
                if (isCreate)
                {
                    reservationsRepository.DeleteForever(item, userId);
                    item.ActivityDayList.Where(a => !string.IsNullOrEmpty(a.Id)).ToList().ForEach(a => activityDayRepository.DeleteForever(a, userId));
                }
                throw ex;
            }
            /*foreach (var activityDay in item.ActivityDayList)
            {
                try
                {
                    activityDay.Reservation = new Reservation() { Id = item.Id.ToString() };
                    activityDay.Division = int.Parse(item.DivisionRouter);
                    activityDayRepository.CreateOrUpdateEntity(activityDay, userId);

                    foreach (var reserveResource in activityDay.ReserveResourcesList)//שיבוץ ליום פעילות שהוא חדר
                    {
                        reserveResource.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                        eventItemRepository.CreateOrUpdateEntity(reserveResource, userId);
                    }
                    foreach (var reserveResource in activityDay.EventItemsList)//שיבוץ ליום פעילות שאינו חדר
                    {
                        reserveResource.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                       
                        eventItemRepository.CreateOrUpdateEntity(reserveResource, userId);
                    }


                    foreach (var itineraryGroup in activityDay.ItineraryGroupList)
                    {
                        itineraryGroup.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                        DateTime activityDate = DateTime.ParseExact(activityDay.StartDateTime, "dd/MM/yyyy HH:mm:ss", null).Date;
                        if (itineraryGroup.GuideScheduleEvent != null)// was sent
                        {
                            numOfGuides++;
                            itineraryGroup.GuideScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                            //Shira add this code to fix date format & 1970 bugs 7.3.21
                            if (!string.IsNullOrEmpty(itineraryGroup.GuideScheduleEvent.Start))
                            {
                                
                                DateTime eventDateStart = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy

                                if (activityDay.StartDateTime != null &&
                                   activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                                {
                                    try
                                    {
                                        //Send mail
                                        sendMailSoapClient sendMail = new sendMailSoapClient();
                                        sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                            "no-bug@yadvashem.org.il", "bug in GuideScheduleEvent",
                                            "Guid: " + itineraryGroup.GuideScheduleEvent.Id
                                            +" Reservation Number:" + itineraryGroup.GuideScheduleEvent.Reservation.Value
                                            +" UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: "+ environment, "ltr");

                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.Write("send mail from no-bug faild, ex:" + ex);
                                    }

                                    itineraryGroup.GuideScheduleEvent.IsDirty = true;
                                    itineraryGroup.GuideScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                    itineraryGroup.GuideScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                }
                            }
                            eventItemRepository.CreateOrUpdateEntity(itineraryGroup.GuideScheduleEvent, userId);
                        }

                        if (itineraryGroup.RoomScheduleEvent != null)// was sent
                        {
                            itineraryGroup.RoomScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                            //Shira add this code to fix date format & 1970 bugs 7.3.21
                            DateTime eventDateStart = DateTime.ParseExact(itineraryGroup.RoomScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                            DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.RoomScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                                                                                                                            //DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "dd/MM/yyyy HH:mm:ss aa", null);

                            if (activityDay.StartDateTime != null &&
                               activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                            {
                                try
                                {
                                    //Send mail
                                    sendMailSoapClient sendMail = new sendMailSoapClient();
                                    sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                            "no-bug@yadvashem.org.il", "bug in RoomScheduleEvent",
                                            "Guid: " + itineraryGroup.RoomScheduleEvent.Id
                                            + " Reservation Number:" + itineraryGroup.RoomScheduleEvent.Reservation.Value
                                            + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");
                                }
                                catch (Exception ex)
                                {
                                    Trace.Write("send mail from no-bug faild, ex:" + ex);
                                }

                                itineraryGroup.RoomScheduleEvent.IsDirty = true;
                                itineraryGroup.RoomScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                itineraryGroup.RoomScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            }
                            eventItemRepository.CreateOrUpdateEntity(itineraryGroup.RoomScheduleEvent, userId);
                        }
                        
                        //itineraryGroup.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                        itineraryGroupRepository.CreateOrUpdateEntity(itineraryGroup, userId);
                        foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                        {
                            if (itineraryItem.SchedulerResource != null)
                            {
                                //Shira add this code to fix date format & 1970 bugs 7.3.21
                                DateTime eventDateStart = DateTime.ParseExact(itineraryItem.SchedulerResource.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                DateTime eventDateEnd = DateTime.ParseExact(itineraryItem.SchedulerResource.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                
                                if (activityDay.StartDateTime != null &&
                                   activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                                {
                                    try
                                    {
                                        //Send mail
                                        sendMailSoapClient sendMail = new sendMailSoapClient();
                                        sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                            "no-bug@yadvashem.org.il", "bug in SchedulerResource",
                                            "Guid: " + itineraryItem.SchedulerResource.Id
                                            + " Reservation Number:" + itineraryItem.SchedulerResource.Reservation.Value
                                            + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");

                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.Write("send mail from no-bug faild, ex:" + ex);
                                    }

                                    itineraryItem.SchedulerResource.IsDirty = true;
                                    itineraryItem.SchedulerResource.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                    itineraryItem.SchedulerResource.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                    eventItemRepository.CreateOrUpdateEntity(itineraryItem.SchedulerResource, userId);
                                    
                                }
                            }
                            
                            // Sum count of lecturer & testimony was scheduled
                            if (itineraryItem.SchedulerServiceProvider != null)
                            {
                                //Shira add this code to fix date format & 1970 bugs 7.3.21
                                DateTime eventDateStart = DateTime.ParseExact(itineraryItem.SchedulerServiceProvider.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                DateTime eventDateEnd = DateTime.ParseExact(itineraryItem.SchedulerServiceProvider.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy

                                if (activityDay.StartDateTime != null &&
                                   activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                                {
                                    try
                                    {
                                        //Send mail
                                        sendMailSoapClient sendMail = new sendMailSoapClient();
                                        sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                            "no-bug@yadvashem.org.il", "bug in SchedulerServiceProvider",
                                            "Guid: " + itineraryItem.SchedulerServiceProvider.Id
                                            + " Reservation Number:" + itineraryItem.SchedulerServiceProvider.Reservation.Value
                                            + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.Write("send mail from no-bug faild, ex:" + ex);
                                    }

                                    itineraryItem.SchedulerServiceProvider.IsDirty = true;
                                    itineraryItem.SchedulerServiceProvider.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                    itineraryItem.SchedulerServiceProvider.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                    eventItemRepository.CreateOrUpdateEntity(itineraryItem.SchedulerServiceProvider, userId);
                                }
                                if (itineraryItem.Product.ServiceProviderType == (int)ServiceProviderType.LecturerEmployee)
                                {
                                    // Distinct list (need if is united groups)
                                    if (!lecturerList.Any(itineraryItem.Id.Contains))
                                    {
                                        lecturerList.Add(itineraryItem.Id);
                                    }
                                }
                                if (itineraryItem.Product.ServiceProviderType == (int)ServiceProviderType.Testimony)
                                {
                                    if (!testimonyList.Any(itineraryItem.Id.Contains))
                                    {
                                        testimonyList.Add(itineraryItem.Id);
                                    }
                                }
                                if (itineraryItem.Product.ServiceProviderType == (int)ServiceProviderType.Cantor)
                                {
                                    if (!cantorList.Any(itineraryItem.Id.Contains))
                                    {
                                        cantorList.Add(itineraryItem.Id);
                                    }
                                }
                                if (itineraryItem.Product.ServiceProviderType == (int)ServiceProviderType.Actor)
                                {
                                    if (!actorList.Any(itineraryItem.Id.Contains))
                                    {
                                        actorList.Add(itineraryItem.Id);
                                    }
                                }
                            }
                            SetItineraryItem(activityDay, itineraryGroup, itineraryItem, item.ReservationNum, userId);
                        }
                    }
                    
                    activityDay.NumOfGuideNeeded = 0;
                    activityDay.NumOfLecturerNeeded = 0;
                    activityDay.NumOfTestimonyNeeded = 0;
                    activityDay.NumOfCantorNeeded = 0;
                    activityDay.NumOfActorNeeded = 0;
                    foreach (var bidItem in activityDay.BidItem)
                    {
                        bidItem.ActivityDay = new IdValuePair() { Id = activityDay.Id, Value = item.ReservationNum };
                        BidItemRepository.CreateOrUpdateEntity(bidItem, userId);
                        // Sum count of lecturer & testimony was needed
                        if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.GuideEmployee)
                        {
                            activityDay.NumOfGuideNeeded += bidItem.Amount;
                        }
                        if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.LecturerEmployee)
                        {
                            activityDay.NumOfLecturerNeeded += bidItem.Amount;
                        }
                        if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.Testimony)
                        {
                            activityDay.NumOfTestimonyNeeded += bidItem.Amount;
                        }
                        if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.Cantor)
                        {
                            activityDay.NumOfCantorNeeded += bidItem.Amount;
                        }
                        if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.Actor)
                        {
                            activityDay.NumOfActorNeeded += bidItem.Amount;
                        }

                    }
                    
                    //Update activity-day fields
                    activityDay.NumOfLecturerScheduled = lecturerList.Count;
                    activityDay.NumOfTestimonyScheduled = testimonyList.Count;
                    activityDay.NumOfGuideScheduled = numOfGuides;
                    activityDay.NumOfCantorScheduled = cantorList.Count;
                    activityDay.NumOfActorScheduled = actorList.Count;
                    activityDay.IsDirty = true;
                    activityDay.Reservation = new Reservation() { Id = item.Id.ToString() };
                    activityDayRepository.CreateOrUpdateEntity(activityDay, userId);
                    //Initial vars
                    numOfGuides = 0;
                    testimonyList = new List<string>();
                    lecturerList = new List<string>();
                    cantorList = new List<string>();
                    actorList = new List<string>();
                }
                catch (Exception ex)
                {
                    if (isCreate)
                    {
                        reservationsRepository.DeleteForever(item, userId);
                        item.ActivityDayList.Where(a => !string.IsNullOrEmpty(a.Id)).ToList().ForEach(a => activityDayRepository.DeleteForever(a, userId));
                    }
                    throw ex;
                }
            }
            */

            foreach (var participant in item.BarMitzvahParticipantList)
            {
                try
                {
                    participant.Reservation = new Reservation() { Id = item.Id.ToString() };
                    //participant.DateOfBirth = DateTime.Parse(participant.DateOfBirth).Date;
                    /*if (activityDay.StartDateTime != null)
                    {
                        itineraryItem.Date = DateTime.ParseExact(activityDay.StartDateTime, "dd/MM/yyyy HH:mm:ss", null);
                    }*/
                    Participant oldParticipant = null;
                    if (oldItem != null)
                    {
                        oldParticipant = oldItem.BarMitzvahParticipantList.Find(br => br.Id == participant.Id);
                    }
                    participantRepository.CreateOrUpdateEntity(participant, oldParticipant, userId);
                }
                catch (Exception ex)
                {
                    if (isCreate)
                    {
                        reservationsRepository.DeleteForever(item, userId);
                        item.BarMitzvahParticipantList.Where(a => !string.IsNullOrEmpty(a.Id)).ToList().ForEach(a => participantRepository.DeleteForever(a, userId));
                    }
                    throw ex;
                }
            }

        }

        public void SetActivityDays(string userId, bool isReservation, string divisionRouter, string guid, List<ActivityDay> activityDayList, List<ActivityDay> oldActivityDayList)
        {
            // int numOfGuides = 0;
            /* List<string> testimonyList = new List<string>();
             List<string> lecturerList = new List<string>();
             List<string> cantorList = new List<string>();
             List<string> actorList = new List<string>();
             */
            bool isBidItemDirty = false;
            foreach (var activityDay in activityDayList)
            {
                ActivityDay oldActivityDay = null;
                if (oldActivityDayList != null)
                {
                    oldActivityDay = oldActivityDayList.Find(oa => oa.Id == activityDay.Id);
                }
                if (isReservation)
                {
                    activityDay.Reservation = new Reservation() { Id = guid };
                }
                else
                {
                    activityDay.EventReservation = new EventReservation() { Id = guid };
                }
                
                activityDay.Division = int.Parse(divisionRouter);
                activityDay.IsDirty = true;
                activityDayRepository.CreateOrUpdateEntity(activityDay, oldActivityDay, userId);
                
                Trace.TraceInformation($"{DateTime.Now}:--------CreateOrUpdateActivityDay------");
                
                foreach (var reserveResource in activityDay.ReserveResourcesList)
                {
                    reserveResource.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                    EventItem oldReserveResource = null;
                    if (oldActivityDay != null   && oldActivityDay.ReserveResourcesList!=null)
                    {
                        oldReserveResource = oldActivityDay.ReserveResourcesList.Find(rs => rs.Id == reserveResource?.Id);
                    }
                    eventItemRepository.CreateOrUpdateEntity(reserveResource, oldReserveResource, userId);

                    // shirag add checking if the reserveResource is union
                    bool isUnionResourceCreated = false;
                    if (reserveResource.ScheduleItem != null)
                    {
                        if (resourceRepository.IsUnionResource(reserveResource.ScheduleItem.Id, userId))
                        {
                            reserveResource.IsUniteEventItem = true;
                            eventItemRepository.CreateOrUpdateEntity(reserveResource, oldReserveResource, userId);
                            isUnionResourceCreated = true;
                            eventItemRepository.UnionEventItem(reserveResource, oldReserveResource, userId);
                        }
                        else if (oldReserveResource != null && resourceRepository.IsUnionResource(oldReserveResource?.ScheduleItem.Id, userId))
                        {
                            List<EventItem> childUnionEventItemList = eventItemRepository.GetUnionChildEventItemList(oldReserveResource, userId);
                            if (childUnionEventItemList.Count > 0)
                            {
                                foreach (var chileEventItem in childUnionEventItemList)
                                {
                                    chileEventItem.IsDeleted = true;
                                    eventItemRepository.Delete(chileEventItem, userId);
                                }
                            }
                        }
                    }
                    if (!isUnionResourceCreated)
                    {
                        eventItemRepository.CreateOrUpdateEntity(reserveResource, oldReserveResource, userId);
                    }
                }

                //Trace.TraceInformation($"{DateTime.Now}:--------CreateOrUpdateEventItemsList------newCount:{activityDay.EventItemsList.Count}, oldCount:{(oldActivityDay != null && oldActivityDay.EventItemsList != null ? oldActivityDay.EventItemsList.Count : 0)}");

                foreach (var eventItem in activityDay.EventItemsList)
                {
                    eventItem.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                    EventItem oldEventItem = null;
                    if (oldActivityDay != null)
                    {
                        oldEventItem = oldActivityDay.EventItemsList.Find(ei => ei.Id == eventItem.Id);
                    }
                    Trace.TraceInformation($"{DateTime.Now}:--------CreateOrUpdateEventItem------");
                    Trace.TraceInformation($"newEventItem:{JsonConvert.SerializeObject(eventItem)}");
                    Trace.TraceInformation($"oldEventItem:{JsonConvert.SerializeObject(oldEventItem)}");

                    eventItemRepository.CreateOrUpdateEntity(eventItem, oldEventItem, userId);
                }

                foreach (var pickupStationItem in activityDay.PickupStationList)
                {
                    pickupStationItem.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                    PickupStation oldPickupStationItem = null;
                    if (oldActivityDay != null)
                    {
                        oldPickupStationItem = oldActivityDay.PickupStationList.Find(ei => ei.Id == pickupStationItem.Id);
                    }
                    pickupStationRepository.CreateOrUpdateEntity(pickupStationItem, oldPickupStationItem, userId);
                    pickupStationRepository.SetServiceProvider(pickupStationItem, oldPickupStationItem, userId);

                }

                SetItineraryGroup(activityDay, oldActivityDay, isReservation, true, false, guid, userId);

                foreach (var learningDivision in activityDay.LearningDivisionsList)
                {
                    learningDivision.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                    learningDivision.EventReservation = new IdValuePair() { Id = guid };
                    LearningDivision oldLearningDivision = null;
                    if (oldActivityDay != null)
                    {
                        oldLearningDivision = oldActivityDay.LearningDivisionsList.Find(lrd => lrd.Id == learningDivision.Id);
                    }
                    DateTime activityDate = DateTime.ParseExact(activityDay.StartDateTime, "dd/MM/yyyy HH:mm:ss", null).Date;

                    if (learningDivision.RoomScheduleEvent != null)// was sent
                    {
                        learningDivision.RoomScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                        if (isReservation)
                        {
                            learningDivision.RoomScheduleEvent.Reservation = new IdValuePair() { Id = guid };
                        }
                        else
                        {
                            learningDivision.RoomScheduleEvent.EventReservation = new IdValuePair() { Id = guid };
                        }
                        learningDivision.RoomScheduleEvent.ActivityDayLookup = new IdValuePair() { Id = activityDay.Id };
                        //Shira add this code to fix date format & 1970 bugs 7.3.21
                        DateTime eventDateStart = DateTime.ParseExact(learningDivision.RoomScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                        DateTime eventDateEnd = DateTime.ParseExact(learningDivision.RoomScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                                                                                                                         //DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "dd/MM/yyyy HH:mm:ss aa", null);

                        if (activityDay.StartDateTime != null &&
                               activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                        {
                            try
                            {
                                string parentDetails = isReservation ? " Reservation Number:" + learningDivision.RoomScheduleEvent?.Reservation?.Value : "Event Reservation Number:" + learningDivision.RoomScheduleEvent?.EventReservation?.Id;

                                //Send mail
                                sendMailSoapClient sendMail = new sendMailSoapClient();
                                sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                        "no-bug@yadvashem.org.il", "bug in RoomScheduleEvent",
                                        "Guid: " + learningDivision.RoomScheduleEvent.Id
                                        + parentDetails
                                        + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");
                            }
                            catch (Exception ex)
                            {
                                Trace.Write("send mail from no-bug faild, ex:" + ex);
                            }

                            learningDivision.RoomScheduleEvent.IsDirty = true;
                            learningDivision.RoomScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            learningDivision.RoomScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                        }
                        eventItemRepository.CreateOrUpdateEntity(learningDivision.RoomScheduleEvent, oldLearningDivision?.RoomScheduleEvent, userId);
                    }

                    learningDivisionRepository.CreateOrUpdateEntity(learningDivision, oldLearningDivision, userId);

                    foreach (EventItem guideScheduleEvent in learningDivision.GuideScheduleEventsList)
                    {
                        EventItem oldEventItem = null;
                        if (oldLearningDivision != null)
                        {
                            oldEventItem = oldLearningDivision.GuideScheduleEventsList?.Find(eventItem => eventItem.Id == guideScheduleEvent.Id);
                        }

                        guideScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                        if (isReservation)
                        {
                            guideScheduleEvent.Reservation = new IdValuePair() { Id = guid };
                        }
                        else
                        {
                            guideScheduleEvent.EventReservation = new IdValuePair() { Id = guid };
                        }
                        guideScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                        guideScheduleEvent.LearningDivision = new IdValuePair() { Id = learningDivision.Id };
                        //Shira add this code to fix date format & 1970 bugs 7.3.21
                        DateTime eventDateStart = DateTime.ParseExact(guideScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                        DateTime eventDateEnd = DateTime.ParseExact(guideScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                                                                                                                         //DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "dd/MM/yyyy HH:mm:ss aa", null)
                        if (activityDay.StartDateTime != null &&
                               activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                        {
                            try
                            {
                                string parentDetails = isReservation ? " Reservation Number:" + learningDivision.RoomScheduleEvent?.Reservation?.Value : "Event Reservation Number:" + learningDivision.RoomScheduleEvent?.EventReservation?.Id;

                                //Send mail
                                sendMailSoapClient sendMail = new sendMailSoapClient();
                                sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                        "no-bug@yadvashem.org.il", "bug in RoomScheduleEvent",
                                        "Guid: " + guideScheduleEvent.Id
                                        + parentDetails
                                        + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");
                            }
                            catch (Exception ex)
                            {
                                Trace.Write("send mail from no-bug faild, ex:" + ex);
                            }

                            guideScheduleEvent.IsDirty = true;
                            guideScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            guideScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                        }
                        eventItemRepository.CreateOrUpdateEntity(guideScheduleEvent, oldEventItem, userId);

                    }
                    
                    foreach (var workshop in learningDivision.WorkshopsList)
                    {
                        workshop.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                        workshop.LearningDivision = new IdValuePair() { Id = learningDivision.Id };
                        workshop.EventReservation = new IdValuePair() { Id = guid };
                        Workshop oldWorkshop = null;
                        if (oldLearningDivision != null)
                        {
                            oldWorkshop = oldLearningDivision.WorkshopsList.Find(wrs => wrs.Id == workshop.Id);
                        }

                        if (workshop.RoomScheduleEvent != null)// was sent
                        {
                            workshop.RoomScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                            if (isReservation)
                            {
                                workshop.RoomScheduleEvent.Reservation = new IdValuePair() { Id = guid };
                            }
                            else
                            {
                                workshop.RoomScheduleEvent.EventReservation = new IdValuePair() { Id = guid };
                            }
                            workshop.RoomScheduleEvent.ActivityDayLookup = new IdValuePair() { Id = activityDay.Id };
                            //Shira add this code to fix date format & 1970 bugs 7.3.21
                            DateTime eventDateStart = DateTime.ParseExact(workshop.RoomScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                            DateTime eventDateEnd = DateTime.ParseExact(workshop.RoomScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                                                                                                                             //DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "dd/MM/yyyy HH:mm:ss aa", null);

                            if (activityDay.StartDateTime != null &&
                                   activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                            {
                                try
                                {
                                    string parentDetails = isReservation ? " Reservation Number:" + workshop.RoomScheduleEvent?.Reservation?.Value : "Event Reservation Number:" + workshop.RoomScheduleEvent?.EventReservation?.Id;

                                    //Send mail
                                    sendMailSoapClient sendMail = new sendMailSoapClient();
                                    sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                            "no-bug@yadvashem.org.il", "bug in RoomScheduleEvent",
                                            "Guid: " + workshop.RoomScheduleEvent.Id
                                            + parentDetails
                                            + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");
                                }
                                catch (Exception ex)
                                {
                                    Trace.Write("send mail from no-bug faild, ex:" + ex);
                                }

                                workshop.RoomScheduleEvent.IsDirty = true;
                                workshop.RoomScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                workshop.RoomScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            }
                            eventItemRepository.CreateOrUpdateEntity(workshop.RoomScheduleEvent, oldWorkshop?.RoomScheduleEvent, userId);
                        }
                        workshopRepository.CreateOrUpdateEntity(workshop, oldWorkshop, userId);
                        foreach (EventItem guideScheduleEvent in workshop.GuideScheduleEventsList)
                        {
                            EventItem oldEventItem = null;
                            if (oldWorkshop != null)
                            {
                                oldEventItem = oldWorkshop.GuideScheduleEventsList?.Find(eventItem => eventItem.Id == guideScheduleEvent.Id);
                            }

                            guideScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                            if (isReservation)
                            {
                                guideScheduleEvent.Reservation = new IdValuePair() { Id = guid };
                            }
                            else
                            {
                                guideScheduleEvent.EventReservation = new IdValuePair() { Id = guid };
                            }
                            guideScheduleEvent.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                            guideScheduleEvent.WorkshopLookup = new IdValuePair() { Id = workshop.Id };
                            //Shira add this code to fix date format & 1970 bugs 7.3.21
                            DateTime eventDateStart = DateTime.ParseExact(guideScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                            DateTime eventDateEnd = DateTime.ParseExact(guideScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                                                                                                             //DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "dd/MM/yyyy HH:mm:ss aa", null)
                            if (activityDay.StartDateTime != null &&
                                   activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                            {
                                try
                                {
                                    string parentDetails = isReservation ? " Reservation Number:" + learningDivision.RoomScheduleEvent?.Reservation?.Value : "Event Reservation Number:" + learningDivision.RoomScheduleEvent?.EventReservation?.Id;

                                    //Send mail
                                    sendMailSoapClient sendMail = new sendMailSoapClient();
                                    sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                            "no-bug@yadvashem.org.il", "bug in RoomScheduleEvent",
                                            "Guid: " + guideScheduleEvent.Id
                                            + parentDetails
                                            + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");
                                }
                                catch (Exception ex)
                                {
                                    Trace.Write("send mail from no-bug faild, ex:" + ex);
                                }

                                guideScheduleEvent.IsDirty = true;
                                guideScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                                guideScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            }
                            eventItemRepository.CreateOrUpdateEntity(guideScheduleEvent, oldEventItem, userId);

                        }
                        SetItineraryGroup(workshop, oldWorkshop, isReservation, false, true, guid, userId);
                    }
                }

                /*activityDay.NumOfGuideNeeded = 0;
                activityDay.NumOfLecturerNeeded = 0;
                activityDay.NumOfTestimonyNeeded = 0;
                activityDay.NumOfCantorNeeded = 0;
                activityDay.NumOfActorNeeded = 0;
                */
                foreach (var transporttionBidItem in activityDay.TransportationBidItemList)
                {
                    transporttionBidItem.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                    BidItem oldTransporttionBidItem = null;
                    if (oldActivityDay != null)
                    {
                        oldTransporttionBidItem = oldActivityDay.BidItemList.Find(bi => bi.Id == transporttionBidItem.Id);

                    }
                    transportationBidItemRepository.CreateOrUpdateEntity(transporttionBidItem, oldTransporttionBidItem, userId);
                }

                foreach (var kitBidItem in activityDay.KitBidItemList)
                {
                    kitBidItem.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                    BidItem oldKitBidItem = null;
                    if (oldActivityDay != null)
                    {
                        oldKitBidItem = oldActivityDay.BidItemList.Find(bi => bi.Id == kitBidItem.Id);

                    }

                    kitBidItemRepository.CreateOrUpdateEntity(kitBidItem, oldKitBidItem, userId);
                }

                foreach (var bidItem in activityDay.BidItemList)
                {
                    bidItem.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                    BidItem oldBidItem = null;
                    if (oldActivityDay != null)
                    {
                        oldBidItem = oldActivityDay.BidItemList.Find(bi => bi.Id == bidItem.Id);

                    }
                    if (bidItem.IsDirty || bidItem.Id == null || bidItem.Id == string.Empty || bidItem.Id == "new")
                    {
                        isBidItemDirty = true;
                    }
                    bidItemRepository.CreateOrUpdateEntity(bidItem, oldBidItem, userId);
                    
                    // Sum count of lecturer & testimony was needed
                    /*MOVED TO CLIENT SIDE! ADINA AND DEVORA 
                    if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.GuideEmployee)
                    {
                        activityDay.NumOfGuideNeeded += bidItem.Amount;
                    }
                    if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.GuidePoland)
                    {
                        activityDay.NumOfGuideNeeded += bidItem.Amount;
                    }
                    if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.BatSherut)
                    {
                        activityDay.NumOfGuideNeeded += bidItem.Amount;
                    }
                    if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.LecturerEmployee)
                    {
                        activityDay.NumOfLecturerNeeded += bidItem.Amount;
                    }
                    if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.Testimony)
                    {
                        activityDay.NumOfTestimonyNeeded += bidItem.Amount;
                    }
                    if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.Cantor)
                    {
                        activityDay.NumOfCantorNeeded += bidItem.Amount;
                    }
                    if (bidItem.Product != null && bidItem.Product.ServiceProviderType == (int)ServiceProviderType.Actor)
                    {
                        activityDay.NumOfActorNeeded += bidItem.Amount;
                    }*/


                }

                //Update activity-day fields
                /*
                 MOVED TO CLIENT
                activityDay.NumOfLecturerScheduled = lecturerList.Count;
                activityDay.NumOfTestimonyScheduled = testimonyList.Count;
                activityDay.NumOfGuideScheduled = numOfGuides;
                activityDay.NumOfCantorScheduled = cantorList.Count;
                activityDay.NumOfActorScheduled = actorList.Count;
                */
                //if (isReservation)
                //{
                //    activityDay.Reservation = new Reservation() { Id = guid };
                //}
                //else
                //{
                //    activityDay.EventReservation = new EventReservation() { Id = guid };

                //}
                //activityDayRepository.CreateOrUpdateEntity(activityDay, oldActivityDay, userId);

                //Initial vars
                //  numOfGuides = 0;
                /*testimonyList = new List<string>();
                lecturerList = new List<string>();
                cantorList = new List<string>();
                actorList = new List<string>();
                */
            }
            if(isBidItemDirty)
            {
                updateReservationPaymentStatus(guid, userId);
            }
            
            Trace.TraceInformation($"{DateTime.Now}:--------End-CreateOrUpdateActivityDay------");
        }
        private void SetIdValueList(string relationshipName, string obj, string guid, List<IdValuePair> itemList, string userId, bool isCreate)
        {
            List<IdValuePair> oldItemList = new List<IdValuePair>();
            Guid.TryParse(guid, out Guid refReservationGuid);
            var relationshipNameCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(relationshipName);
            var objectCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj);
            var reservationCrm = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("Reservation");
            oldItemList = idValueListRepository.GetLinkedList<SupporterCRM>(userId, relationshipNameCrm, reservationCrm, objectCrm, guid, null);

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
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("Reservation"), refReservationGuid);
                        basicRepository.Disassociate(refa, refb, relationshipNameCrm, userId);
                    }
                }
            }

            foreach (var item in itemList)
            {
                Guid.TryParse(item.Id, out Guid refItemListGuid);

                if (oldItemList.Where(x => x.Id == item.Id).Count() == 0)
                {//is not in old list 
                    if (refItemListGuid != null && refReservationGuid != null)
                    {
                        EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj), refItemListGuid);
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("Reservation"), refReservationGuid);
                        basicRepository.Associate(refa, refb, relationshipNameCrm, userId);
                    }
                }
            }
        }

        private void DeleteReservationEntities(ReservationItem item, string userId)
        {
            foreach (var activity in item.ActivityDayList)
            {
                activityDayRepository.Delete(activity, userId);

                foreach (var itineraryGroup in activity.ItineraryGroupList)
                {

                    if (itineraryGroup.GuideScheduleEvent != null)// was sent
                    {
                        itineraryGroup.GuideScheduleEvent.ActivityDay = new IdValuePair() { Id = activity.Id };
                        eventItemRepository.Delete(itineraryGroup.GuideScheduleEvent, userId);
                    }

                    if (itineraryGroup.RoomScheduleEvent != null)// was sent
                    {
                        itineraryGroup.RoomScheduleEvent.ActivityDay = new IdValuePair() { Id = activity.Id };
                        eventItemRepository.Delete(itineraryGroup.RoomScheduleEvent, userId);
                    }

                    foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                    {
                        if (itineraryItem.SchedulerResource != null)
                        {
                            itineraryItem.SchedulerResource.ActivityDay = new IdValuePair() { Id = activity.Id };
                            eventItemRepository.Delete(itineraryItem.SchedulerResource, userId);
                        }
                        if (itineraryItem.SchedulerServiceProvider != null)
                        {
                            itineraryItem.SchedulerServiceProvider.ActivityDay = new IdValuePair() { Id = activity.Id };
                            eventItemRepository.Delete(itineraryItem.SchedulerServiceProvider, userId);
                        }
                        if (itineraryItem.IsDisassociate)
                        {
                            EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryGroup"), Guid.Parse(itineraryGroup.Id));
                            EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryItem"), Guid.Parse(itineraryItem.Id));

                            var relationshipName = mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("ItineraryGroup");
                            eventItemRepository.Disassociate(refa, refb, relationshipName, userId);
                        }
                        itineraryItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                        itineraryItemRepository.Delete(itineraryItem, userId);
                    }
                    itineraryGroup.ActivityDay = new IdValuePair() { Id = activity.Id };
                    itineraryGroupRepository.Delete(itineraryGroup, userId);
                }
                foreach (var bidItem in activity.BidItemList)
                {
                    bidItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    bidItemRepository.Delete(bidItem, userId);
                }
                foreach (var transportationBidItem in activity.TransportationBidItemList)
                {
                    transportationBidItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    transportationBidItemRepository.Delete(transportationBidItem, userId);
                }

                foreach (var kitBidItem in activity.KitBidItemList)
                {
                    kitBidItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    kitBidItemRepository.Delete(kitBidItem, userId);
                }
                foreach (var pickupStationItem in activity.PickupStationList)
                {
                    pickupStationItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    pickupStationRepository.Delete(pickupStationItem, userId);
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

            foreach (var attachment in item.AttachmentList)
            {
                attachmentRepository.Delete(attachment, userId);
            }
            contactRepository.Delete(item.Contact, userId);
        }


        private void CancelReservationEntities(ReservationItem item, string userId)
        {
            Trace.WriteLine("----------------- CancelReservationEntities --------------------");
            foreach (var activity in item.ActivityDayList)
            {
                activityDayRepository.Cancel(activity, userId);

                foreach (var itineraryGroup in activity.ItineraryGroupList)
                {
                    //if (itineraryGroup.GuideScheduleEvent != null)
                    //{
                    //    eventItemRepository.Cancel(itineraryGroup.GuideScheduleEvent, userId);
                    //}
                    if (itineraryGroup.RoomScheduleEvent != null)
                    {
                        itineraryGroup.RoomScheduleEvent.ActivityDay = new IdValuePair() { Id = activity.Id };
                        eventItemRepository.Cancel(itineraryGroup.RoomScheduleEvent, userId);
                        itineraryGroup.RoomScheduleEvent = null;
                    }
                    foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                    {
                        if (itineraryItem.SchedulerResource != null)
                        {
                            itineraryItem.SchedulerResource.ActivityDay = new IdValuePair() { Id = activity.Id };
                            eventItemRepository.Cancel(itineraryItem.SchedulerResource, userId);
                            itineraryItem.SchedulerResource = null;
                        }
                        //if (itineraryItem.SchedulerServiceProvider != null)
                        //{
                        //    eventItemRepository.Cancel(itineraryItem.SchedulerServiceProvider, userId);
                        //}
                        //itineraryItemRepository.Delete(itineraryItem, userId);
                    }

                    //itineraryGroupRepository.Delete(itineraryGroup, userId);

                }
                foreach (var bidItem in activity.BidItemList)
                {
                    bidItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    bidItemRepository.Cancel(bidItem, userId);
                }
                foreach (var reserveItem in activity.ReserveResourcesList)
                {
                    reserveItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    eventItemRepository.Cancel(reserveItem, userId);
                }
                foreach (var eventItem in activity.EventItemsList)
                {
                    eventItem.ActivityDay = new IdValuePair() { Id = activity.Id };
                    eventItemRepository.Cancel(eventItem, userId);
                }
            }
        }

        public void SetItineraryGroup(BaseItineraryGroupListParent parent, BaseItineraryGroupListParent oldParent, bool isReservation, bool isActivityDayParent, bool isWorkshopParent, string guid, string userId)
        {
            foreach (var itineraryGroup in parent.ItineraryGroupList)
            {
                if (isActivityDayParent == true)
                {
                    itineraryGroup.ActivityDay = new IdValuePair() { Id = parent.Id };
                }
                else if (isWorkshopParent == true)
                {
                    itineraryGroup.Workshop = new IdValuePair() { Id = parent.Id };
                }
                ItineraryGroup oldItineraryGroup = null;
                if (oldParent != null)
                {
                    oldItineraryGroup = oldParent.ItineraryGroupList.Find(itg => itg.Id == itineraryGroup.Id);
                }
                DateTime activityDate = isActivityDayParent ? DateTime.ParseExact(parent.StartDateTime, "dd/MM/yyyy HH:mm:ss", null).Date : DateTime.ParseExact(parent.StartDateTime, "MM/dd/yyyy HH:mm:ss", null).Date;
                if (itineraryGroup.GuideScheduleEvent != null)// was sent
                {
                    //numOfGuides++;
                    if (isActivityDayParent == true)
                    {
                        itineraryGroup.GuideScheduleEvent.ActivityDayLookup = new IdValuePair() { Id = parent.Id };
                    }
                    else if (isWorkshopParent == true)
                    {
                        itineraryGroup.GuideScheduleEvent.WorkshopLookup = new IdValuePair() { Id = parent.Id };
                    }
                    //Shira add this code to fix date format & 1970 bugs 7.3.21
                    if (!string.IsNullOrEmpty(itineraryGroup.GuideScheduleEvent.Start))
                    {

                        DateTime eventDateStart = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                        DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy

                        if (parent.StartDateTime != null &&
                           activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                        {
                            itineraryGroup.GuideScheduleEvent.IsDirty = true;
                            itineraryGroup.GuideScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            itineraryGroup.GuideScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                        }
                        
                        eventItemRepository.CreateOrUpdateEntity(itineraryGroup.GuideScheduleEvent, oldItineraryGroup?.GuideScheduleEvent, userId);
                    }
                    else
                    {
                        Trace.TraceError("Error in itineraryGroup.GuideScheduleEvent.Start");
                        throw new Exception("Error in itineraryGroup.GuideScheduleEvent.Start");
                    }
                }

                if (itineraryGroup.RoomScheduleEvent != null)// was sent
                {
                    if (isReservation)
                    {
                        itineraryGroup.RoomScheduleEvent.Reservation = new IdValuePair() { Id = guid };
                    }
                    else
                    {
                        itineraryGroup.RoomScheduleEvent.EventReservation = new IdValuePair() { Id = guid };
                    }
                    if(isActivityDayParent == true)
                    {
                        itineraryGroup.RoomScheduleEvent.ActivityDayLookup = new IdValuePair() { Id = parent.Id };
                    }
                    else if(isWorkshopParent == true)
                    {
                        itineraryGroup.RoomScheduleEvent.WorkshopLookup = new IdValuePair() { Id = parent.Id };
                    }
                    //Shira add this code to fix date format & 1970 bugs 7.3.21
                    DateTime eventDateStart = DateTime.ParseExact(itineraryGroup.RoomScheduleEvent.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                    DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.RoomScheduleEvent.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                                                                                                                                   //DateTime eventDateEnd = DateTime.ParseExact(itineraryGroup.GuideScheduleEvent.End, "dd/MM/yyyy HH:mm:ss aa", null);

                    if (parent.StartDateTime != null &&
                           activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                    {
                        itineraryGroup.RoomScheduleEvent.IsDirty = true;
                        itineraryGroup.RoomScheduleEvent.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                        itineraryGroup.RoomScheduleEvent.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                    }

                    eventItemRepository.CreateOrUpdateEntity(itineraryGroup.RoomScheduleEvent, oldItineraryGroup?.RoomScheduleEvent, userId);
                    
                    // shirag add checking if the RoomScheduleEvent is union
                    bool isUnionResourceCreated = false;
                    if (itineraryGroup.RoomScheduleEvent.ScheduleItem != null)
                    {
                        if (resourceRepository.IsUnionResource(itineraryGroup.RoomScheduleEvent.ScheduleItem.Id, userId))
                        {
                            itineraryGroup.RoomScheduleEvent.IsUniteEventItem = true;
                            itineraryGroupRepository.CreateOrUpdateEntity(itineraryGroup, oldItineraryGroup, userId);
                            isUnionResourceCreated = true;
                            eventItemRepository.UnionEventItem(itineraryGroup.RoomScheduleEvent, oldItineraryGroup?.RoomScheduleEvent, userId);
                        }
                        else if (oldItineraryGroup != null && oldItineraryGroup?.RoomScheduleEvent != null && resourceRepository.IsUnionResource(oldItineraryGroup?.RoomScheduleEvent?.ScheduleItem.Id, userId))
                        {
                            List<EventItem> childUnionEventItemList = eventItemRepository.GetUnionChildEventItemList(oldItineraryGroup?.RoomScheduleEvent, userId);
                            if (childUnionEventItemList.Count > 0)
                            {
                                foreach (var chileEventItem in childUnionEventItemList)
                                {
                                    chileEventItem.IsDeleted = true;
                                    //eventItemRepository.Delete(chileEventItem, userId);
                                }
                            }
                        }
                    }

                    if (!isUnionResourceCreated)
                    {
                        itineraryGroupRepository.CreateOrUpdateEntity(itineraryGroup, oldItineraryGroup, userId);
                    }

                }

                //itineraryGroup.ActivityDay = new IdValuePair() { Id = activityDay.Id };
                itineraryGroupRepository.CreateOrUpdateEntity(itineraryGroup, oldItineraryGroup, userId);
                List<Time> duplicateTime = new List<Time>();
                foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
                {
                    if (duplicateTime.Count() > 0 && duplicateTime.Find(a => a.Hours == itineraryItem.Start.Hours && a.Minutes == itineraryItem.Start.Minutes && itineraryItem.Product?.Id?.ToUpper() == ConfigurationManager.AppSettings["museumProductId_Test"].ToString().ToUpper()) != null)
                        continue;
                    if (itineraryItem != null && itineraryItem.Product != null && itineraryItem.Product.Id.ToUpper() == ConfigurationManager.AppSettings["museumProductId_Test"].ToString().ToUpper())
                        duplicateTime.Add(itineraryItem.Start);
                    ItineraryItem oldItineraryItem = null;
                    if (oldItineraryGroup != null)
                    {
                        oldItineraryItem = oldItineraryGroup.SchedulerItemList.Find(si => si.Id == itineraryItem.Id);
                    }
                    if (itineraryItem.SchedulerResource != null)
                    {
                        //Shira add this code to fix date format & 1970 bugs 7.3.21
                        DateTime eventDateStart = DateTime.ParseExact(itineraryItem.SchedulerResource.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                        DateTime eventDateEnd = DateTime.ParseExact(itineraryItem.SchedulerResource.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy

                        if (parent.StartDateTime != null &&
                           activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                        {
                            itineraryItem.SchedulerResource.IsDirty = true;
                            itineraryItem.SchedulerResource.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            itineraryItem.SchedulerResource.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            if (itineraryItem.SchedulerResource.ScheduleItem != null)
                            {
                                if (resourceRepository.IsUnionResource(itineraryItem.SchedulerResource.ScheduleItem.Id, userId))
                                {
                                    itineraryItem.SchedulerResource.IsUniteEventItem = true;
                                    eventItemRepository.UnionEventItem(itineraryItem.SchedulerResource, oldItineraryItem.SchedulerResource, userId);
                                }
                                else if (oldItineraryItem != null && oldItineraryItem.SchedulerResource != null && resourceRepository.IsUnionResource(oldItineraryItem?.SchedulerResource.ScheduleItem.Id, userId))
                                {
                                    List<EventItem> childUnionEventItemList = eventItemRepository.GetUnionChildEventItemList(oldItineraryItem.SchedulerResource, userId);
                                    if (childUnionEventItemList.Count > 0)
                                    {
                                        foreach (var chileEventItem in childUnionEventItemList)
                                        {
                                            chileEventItem.IsDeleted = true;
                                            eventItemRepository.Delete(chileEventItem, userId);
                                        }
                                    }
                                }
                            }
                            eventItemRepository.CreateOrUpdateEntity(itineraryItem.SchedulerResource, oldItineraryItem?.SchedulerResource, userId);

                        }
                    }

                    // Sum count of lecturer & testimony was scheduled
                    if (itineraryItem.SchedulerServiceProvider != null)
                    {
                        //Shira add this code to fix date format & 1970 bugs 7.3.21
                        DateTime eventDateStart = DateTime.ParseExact(itineraryItem.SchedulerServiceProvider.Start, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy
                        DateTime eventDateEnd = DateTime.ParseExact(itineraryItem.SchedulerServiceProvider.End, "MM/dd/yyyy HH:mm:ss", null);//automatic dd/MM/yyyy

                        if (parent.StartDateTime != null &&
                           activityDate != eventDateStart.Date || activityDate != eventDateEnd.Date)
                        {
                            try
                            {
                                string parentDetails = isReservation ? " Reservation Number:" + itineraryItem.SchedulerServiceProvider?.Reservation.Value : "Event Reservation Number:" + itineraryItem.SchedulerServiceProvider?.EventReservation?.Id;

                                //Send mail
                                sendMailSoapClient sendMail = new sendMailSoapClient();
                                sendMail.sendMailProcces("shira.roth@yadvashem.org.il", "esther.fuxbrumer@yadvashem.org.il", "",
                                    "no-bug@yadvashem.org.il", "bug in SchedulerServiceProvider",
                                    "Guid: " + itineraryItem.SchedulerServiceProvider.Id
                                    + parentDetails
                                    + " UserId: " + userId + " EventStartDate: " + eventDateStart.ToString() + " Environment: " + environment, "ltr");
                            }
                            catch (Exception ex)
                            {
                                Trace.Write("send mail from no-bug faild, ex:" + ex);
                            }

                            itineraryItem.SchedulerServiceProvider.IsDirty = true;
                            itineraryItem.SchedulerServiceProvider.Start = activityDate.AddHours(eventDateStart.Hour).AddMinutes(eventDateStart.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            itineraryItem.SchedulerServiceProvider.End = activityDate.AddHours(eventDateEnd.Hour).AddMinutes(eventDateEnd.Minute).ToString("MM/dd/yyyy HH:mm:ss");
                            eventItemRepository.CreateOrUpdateEntity(itineraryItem.SchedulerServiceProvider, oldItineraryItem?.SchedulerResource, userId);
                        }
                    }
                    SetItineraryItem(parent, itineraryGroup, itineraryItem, oldItineraryItem, isActivityDayParent, isWorkshopParent, userId);
                }
            }
        }
        private void SetItineraryItem(BaseItineraryGroupListParent parent, ItineraryGroup itineraryGroup, ItineraryItem itineraryItem, ItineraryItem oldItineraryItem, bool isActivityDayParent, bool isWorkshopParent, string userId)
        {
            if (itineraryItem.Id == "new")
            {
                if (isActivityDayParent == true)
                {
                    itineraryItem.ActivityDay = new IdValuePair() { Id = parent.Id.ToString() };
                }
                else if(isWorkshopParent)
                {
                    itineraryItem.Workshop = new IdValuePair() { Id = parent.Id.ToString() };
                }
            }
            if (itineraryItem.GroupNumberList.Count > 1 &&
                itineraryItem.GroupNumberList.Min() != itineraryGroup.GroupNumber)
            {
                itineraryItem = parent.ItineraryGroupList.Find(ig =>
                {
                    return ig.GroupNumber == itineraryItem.GroupNumberList.Min();
                })
                 .SchedulerItemList.Find(si =>
                 {
                     return si.Id == itineraryItem.Id;
                     //si.Product?.Id == itineraryItem.Product?.Id &&
                     //si.Start.Equals(itineraryItem.Start) &&
                     //si.End.Equals(itineraryItem.End);
                 });
                if (itineraryItem == null)
                {
                    return;
                }
                itineraryItem.ItineraryGroup = new IdValuePair() { Id = itineraryGroup.Id };
            }
            else
            {
                if (parent.StartDateTime != null)
                {
                    itineraryItem.Date = DateTime.ParseExact(parent.StartDateTime, "dd/MM/yyyy HH:mm:ss", null);
                }

                itineraryItem.ItineraryGroup = new IdValuePair() { Id = itineraryGroup.Id };
                bool isUnionResourceCreated = false;
                if (itineraryItem.SchedulerResource?.ScheduleItem != null)
                {
                    if (resourceRepository.IsUnionResource(itineraryItem.SchedulerResource.ScheduleItem.Id, userId))
                    {
                        itineraryItem.SchedulerResource.IsUniteEventItem = true;
                        itineraryItemRepository.CreateOrUpdateEntity(itineraryItem, oldItineraryItem, userId);
                        isUnionResourceCreated = true;
                        eventItemRepository.UnionEventItem(itineraryItem.SchedulerResource, oldItineraryItem?.SchedulerResource, userId);
                    }
                    else if (oldItineraryItem != null && oldItineraryItem.SchedulerResource != null && resourceRepository.IsUnionResource(oldItineraryItem.SchedulerResource.ScheduleItem.Id, userId))
                    {
                        List<EventItem> childUnionEventItemList = eventItemRepository.GetUnionChildEventItemList(oldItineraryItem.SchedulerResource, userId);
                        if (childUnionEventItemList.Count > 0)
                        {
                            foreach (var chileEventItem in childUnionEventItemList)
                            {
                                chileEventItem.IsDeleted = true;
                                eventItemRepository.Delete(chileEventItem, userId);
                            }
                        }
                    }
                }
                if (!isUnionResourceCreated)
                {
                    itineraryItemRepository.CreateOrUpdateEntity(itineraryItem, oldItineraryItem, userId);
                }
            }
            if (itineraryItem.ItineraryGroup != null)
            {
                Guid.TryParse(itineraryGroup.Id, out Guid refGroupGuid);
                Guid.TryParse(itineraryItem.Id, out Guid refItemGuid);

                if (refGroupGuid != null && refItemGuid != null)
                {
                    EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryGroup"), refGroupGuid);
                    EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ItineraryItem"), refItemGuid);

                    if (itineraryItem.ItineraryGroup.Id != string.Empty && itineraryItem.IsDirty)//need to Associate
                    {
                        reservationsRepository.Associate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("ItineraryGroup"), userId);
                    }
                }
            }
        }
        public void InheritIsDeletedToChildrenEntities(object item)
        {
            bool toDelete = ((BasicEntity)item).IsDeleted;
            PropertyInfo[] properties = item.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                {
                    var value = property.GetValue(item);//as List<ActivityDay>;
                    if (value != null)
                    {

                        Type type = property.PropertyType;
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type itemType = type.GetGenericArguments()[0]; // use this...
                                                                           //ConvertList(property.GetValue(item) as , itemType).Dump();
                            if (itemType.BaseType == typeof(BasicEntity))
                            {
                                foreach (var entity in value as ICollection)
                                {
                                    if (toDelete)// && ((BasicEntity)entity).Id != "new"
                                    {
                                        ((BasicEntity)entity).IsDeleted = true;
                                    }
                                    InheritIsDeletedToChildrenEntities(entity);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void InheritIsCanceledToChildrenEntities(object item, bool isCancel)
        {
            //bool toCancel = ((BasicEntity)item).IsCanceled;
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
                            if (InheritsFromOrIsBasicEntity(itemType))
                            {
                                foreach (var entity in value as ICollection)
                                {
                                    if (isCancel)
                                    {
                                        ((BasicEntity)entity).IsCanceled = true;
                                        ((BasicEntity)entity).IsActive = false;

                                    }
                                    else
                                    {
                                        ((BasicEntity)entity).IsActive = true;
                                        ((BasicEntity)entity).IsCanceled = false;
                                    }
                                    InheritIsCanceledToChildrenEntities(entity, isCancel);
                                }
                            }
                        }
                        // אם זה אובייקט בודד שהוא BasicEntity — נסמן - להשאיר את זה ואם לא רוצים שמשהו יתבטל אז לשנות בקוד שלא יגיעו אליו ויעשו לו cancel
                        else if (InheritsFromOrIsBasicEntity(type))
                        {
                            var entity = value as BasicEntity;
                            if (entity != null)
                            {
                                if (isCancel)
                                {
                                    entity.IsCanceled = true;
                                    entity.IsActive = false;
                                }
                                else
                                {
                                    entity.IsActive = true;
                                    entity.IsCanceled = false;
                                }

                                // המשך רקורסיה על האובייקט הבודד
                                InheritIsCanceledToChildrenEntities(entity, isCancel);
                            }
                        }
                    }
                }
            }
        }

        public bool InheritsFromOrIsBasicEntity(Type type)
        {
            if (type == typeof(BasicEntity))
            {
                return true;
            }

            // Check if the type directly inherits from BasicEntity
            if (type.BaseType == typeof(BasicEntity))
            {
                return true;
            }

            // Recursively check the parent type
            if (type.BaseType != null)
            {
                return InheritsFromOrIsBasicEntity(type.BaseType);
            }

            return false;
        }


        public ReservationItem CancelMuseum(string userId, Entity item)
        {
            return reservationsRepository.SetEntityToFullItem(userId, item);
        }

        public string GetModifiedOn(string guid, string userId)
        {
            return reservationsRepository.GetModifiedOn(guid, userId);
        }

        public List<Currency> GetCurrencyList(string userId)
        {
            string fetchxml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='transactioncurrency'>
                                <attribute name='currencyname' />
                                <attribute name='transactioncurrencyid' />
                                <attribute name='isocurrencycode' />
                              </entity>
                            </fetch>";
            var EntityList = basicRepository.GeneralFetchXml(userId, fetchxml);
            var currencyList = new List<Currency>();
            Currency item;
            foreach (var entity in EntityList)
            {
                var details = queryHelperRepository.GetAllValuesOfEntity(entity);
                item = new Currency();

                item.Id = queryHelperRepository.GetValueByAttributeName<Currency, CurrencyCRM>(entity, "Id");
                item.Name = queryHelperRepository.GetValueByAttributeName<Currency, CurrencyCRM>(entity, "Name");
                item.Code = queryHelperRepository.GetValueByAttributeName<Currency, CurrencyCRM>(entity, "Code");
                currencyList.Add(item);

            }

            return currencyList;


        }

        public void CreatePaymentObjectInCrm(string userId)
        {


            //        
            //         $payment->CreditCardType = $_GET["card_type"];
            //                 $payment->Amount = $_GET["amount"];
            //                 $payment->Currency = 1;
            //                 $payment->PaymentType = 1;//tehila
            //                 $payment->OrderNum =  $_GET["tahile_num"];

            //                 $payment->PaymentStatus = 1;

            //                 $payment->FirstName = $_GET["first"];
            //                 $payment->LastName = $_GET["last"];
            //                 $payment->ReceiptName =$_GET["first"].' '.$_GET["last"];
            //                 $payment->Emailaddress1 = $result->Email;
            //                 $payment->PaymentPurpose = $result->Purpose;
            //		$payment->HasPaymentLines = "true";
            //		$payment->PaymentLines = array();

            //$client = new SoapClient("http://172.24.1.230:8060/crm2016/AppealsCrmService.asmx?wsdl");
            //         dpm($payment);
            //}
            //$response=$client->CreatePayment($result->IncidentGuid,  'new_reservation','new_reservation_payment', $payment, true);


        }


        //       public void FixItineraryAddActivityDayToItineraryItem()
        //        {
        //            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
        //          <entity name='new_new_itinerary_item_new_itinerary_group'>
        //           <all-attributes />
        //          <link-entity name='new_itinerary_item' from='new_itinerary_itemid' to='new_itinerary_itemid' link-type='inner'>
        //            <filter type='and'>
        //            <condition attribute='statecode' operator='eq' value='0' />
        //             </filter>
        //           </link-entity>
        //          </entity>
        //        </fetch>";
        //            string userID = "74ec67fa-ad4e-e611-80ce-005056b13c97";
        ////          string  strQueryXML = @"  
        ////<fetch mapping='logical' paging-cookie='&lt;cookie page=&quot;1&quot;&gt;&lt;accountid last=&quot;{E062B974-7F8D-DC11-9048-0003FF27AC3B}&quot; first=&quot;{60B934EF-798D-DC11-9048-0003FF27AC3B}&quot;/&gt;&lt;/cookie&gt;' page='2' count='2'>  
        //// <entity name='account'>  
        ////  <all-attributes/>  
        //// </entity>  
        ////</fetch>";
        //  //< condition attribute = 'new_activity_day' operator= 'null' />
        //            //var list = basicRepository.GeneralFetchXml(userID, fetchXml);
        //            var list = basicRepository.FixItinerary(fetchXml);
        //            int i = 0;
        //            int missing_group_activity_day = 0;
        //            foreach (var item in list)
        //            {
        //                i++;
        //                try
        //                {


        //                var it_item_id =  item.Attributes.Values.ToList()[1].ToString();  //new_itinerary_itemid
        //                var it_item_group =  item.Attributes.Values.ToList()[2].ToString() ;  //new_itinerary_groupid

        //                  //  var it_item_id = "99751FA1-AD03-EB11-8105-00505691117E";
        //                   // var it_item_group = "98751FA1-AD03-EB11-8105-00505691117E";
        //                    var it_group_full = itineraryGroupRepository.GetFetchXml(userID, it_item_group);



        //                 if (it_group_full != null && it_group_full.ActivityDay!=null)
        //                {
        //                    var activity_day_id = it_group_full.ActivityDay.Id;
        //                        if(string.IsNullOrEmpty(activity_day_id))
        //                        {
        //                            missing_group_activity_day++;
        //                            continue;
        //                        }
        //                    var activity_day= activityDayRepository.GetFetchXml(userID, activity_day_id);
        //                    var it_item_old = itineraryItemRepository.GetFetchXml(userID, it_item_id);
        //                        //var serialized = JsonConvert.SerializeObject(it_item_old);
        //                       // var it_item_new = JsonConvert.DeserializeObject<ItineraryItem>(serialized);
        //                        if (!it_group_full.IsActive || !activity_day.IsActive)
        //                        {
        //                            //    it_item_new.IsDirty = true;
        //                            //    it_item_new.IsDeleted = true;
        //                            //    itineraryItemRepository.Delete(it_item_new, userID);

        //                            UpdateCrmItem updateCrmItem = new UpdateCrmItem
        //                            {
        //                                Field ="statecode",
        //                                Value = "1",
        //                                Type = "optionset"
        //                            };
        //                            itineraryItemRepository.UpdateField(it_item_id, updateCrmItem, userID);
        //                        }
        //                        if (it_item_old.ActivityDay == null || string.IsNullOrEmpty(it_item_old.ActivityDay.Id))
        //                        {
        //                            //it_item_new.ActivityDay = new IdValuePair();
        //                            //it_item_new.ActivityDay.Id = activity_day_id;
        //                            //it_item_new.IsDirty = true;
        //                            //  itineraryItemRepository.CreateOrUpdateEntity(it_item_new, it_item_old, userID);


        //                            UpdateCrmItem updateCrmItem = new UpdateCrmItem
        //                            {
        //                                Field = mapFunctionRepository.GetDestinationPropertyFor<ItineraryItem, ItineraryItemCRM>("ActivityDay"),
        //                                Value = activity_day_id,
        //                                Type = "lookup"
        //                            };
        //                            itineraryItemRepository.UpdateField(it_item_id, updateCrmItem, userID);

        //                        }

        //                    }
        //                }
        //                catch (Exception ex)
        //                {


        //                }
        //            }
        //        }

        public void FixAllDonationEntities()
        {
            

            string expressDonationXML = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='new_expressdonation'>
    <attribute name='new_name' />
    <attribute name='new_desk' />
    <attribute name='new_donation_source' />
    <attribute name='new_expressdonationid' />
    <order attribute='new_receipt_date' descending='true' />
    <filter type='and'>
      <condition attribute='statecode' operator='eq' value='0' />
      <condition attribute='new_desk' operator='null' />
      <condition attribute='new_donation_source' operator='eq' value='14' />
    </filter>
  </entity>
</fetch>";
            string userID = "74ec67fa-ad4e-e611-80ce-005056b13c97";

            var list = basicRepository.GetAllListEntity(userID, expressDonationXML);

            int i = 0;
            foreach (var item in list)
            {
                i++;
                try
                {
                    var details = queryHelperRepository.GetAllValuesOfEntity(item);
                    string itemId = details["new_expressdonationid"];
                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = "new_desk",
                        Value = "92391CDC-FFF8-DD11-BD07-0010C6BA714B",
                        Type = "lookup"
                    };
                    basicRepository.UpdateField(itemId, updateCrmItem, userID);
                    //string itinerary_start_time = queryHelperRepository.GetValue(details, "new_start_time", false, false);
                    //string itinerary_end_time = queryHelperRepository.GetValue(details, "new_end_time", false, false);
                    //string itinerary_item_id = queryHelperRepository.GetValue(details, "new_itinerary_itemid", false, false);
                    //string activity_day_date = queryHelperRepository.GetValue(details, "ab.new_start_datetime", false, false);
                    //string activity_day_id = queryHelperRepository.GetValue(details, "ab.new_activity_dayid", false, false);
                    //if (!string.IsNullOrEmpty(itinerary_start_time) && !string.IsNullOrEmpty(activity_day_date))
                    //{
                    //    if (Convert.ToDateTime(activity_day_date).ToLocalTime().Date != Convert.ToDateTime(itinerary_start_time).ToLocalTime().Date)
                    //    {
                    //        var keep_time = Convert.ToDateTime(itinerary_start_time).ToLocalTime();
                    //        var good_date = Convert.ToDateTime(activity_day_date).ToLocalTime();
                    //        var start = new DateTime(good_date.Date.Year, good_date.Date.Month, good_date.Date.Day, keep_time.Hour, keep_time.Minute, 0);

                    //        //new_time.AddHours() = keep_time.Hour;

                    //        UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    //        {
                    //            Field = "new_start_time",
                    //            Value = start.ToString(),
                    //            Type = "datetime"
                    //        };
                    //        basicRepository.UpdateField(itinerary_item_id, updateCrmItem, userID);
                    //    }
                    //    if (Convert.ToDateTime(activity_day_date).ToLocalTime().Date != Convert.ToDateTime(itinerary_end_time).ToLocalTime().Date)
                    //    {
                    //        var keep_time = Convert.ToDateTime(itinerary_end_time).ToLocalTime();
                    //        var good_date = Convert.ToDateTime(activity_day_date).ToLocalTime();
                    //        var start = new DateTime(good_date.Date.Year, good_date.Date.Month, good_date.Date.Day, keep_time.Hour, keep_time.Minute, 0);

                    //        //new_time.AddHours() = keep_time.Hour;

                    //        UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    //        {
                    //            Field = "new_end_time",
                    //            Value = start.ToString(),
                    //            Type = "datetime"
                    //        };
                    //        itineraryItemRepository.UpdateField(itinerary_item_id, updateCrmItem, userID);
                    //    }
                    //}


                }
                catch (Exception ex)
                {

                }
            }
        }

        public void isNeedToUpdateReservationPaymentStatus(ReservationItem oldReservation, string userId)
        {
            bool isBidItemDirty = false;           
            if (oldReservation != null)
            {
                oldReservation.ActivityDayList.ForEach(activityDay =>
                {                   
                    activityDay.BidItemList.ForEach(bidItem =>
                    {
                        if (bidItem.IsDeleted)
                        {
                            isBidItemDirty = true;
                        }
                    });

                    if (activityDay.IsDeleted)
                    {
                        isBidItemDirty = true;
                    }
                });
            }

            if (isBidItemDirty)
            {
                updateReservationPaymentStatus(oldReservation.Id, userId);
            }
        }

        public void UpdatePaymentStatus(string userId, List<string> bidItemsId, List<string> reservationsId)
        {
            if(bidItemsId != null && bidItemsId.Count > 0)//created invoice by bidItems
            {
                updateBidItemPaymentStatus(bidItemsId, userId);            
                reservationsId.ForEach(reservationId =>
                {
                    updateReservationPaymentStatus(reservationId, userId);
                });
            }    
            else
            {
                reservationsId.ForEach(reservationId =>
                {
                    ReservationItem reservation = reservationsRepository.GetFetchXml(userId, reservationId);
                    List<string> newBidItemsId = new List<string>();
                    if(reservation != null)
                    {
                        reservation.ActivityDayList.ForEach(day =>
                        {
                            day.BidItemList.ForEach(bid =>
                            {
                                newBidItemsId.Add(bid.Id);
                            });
                        });
                        updateBidItemPaymentStatus(newBidItemsId, userId);
                        updateReservationPaymentStatus(reservationId, userId);
                    }
                });
            }
        }
        public void updateBidItemPaymentStatus(List<string> bidItemsId, string userId)
        {
                //update PaymentStatus feild
                bidItemsId.ForEach(id =>
                {
                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<BidItem, BidItemCRM>("PaymentStatus"),
                        Value = "13",
                        Type = "optionset"
                    };

                    bidItemRepository.UpdateField(id, updateCrmItem, userId);
                });

        }
        public void updateActivityDayPaymentStatus(string reservationId, string userId)
        {
            ReservationItem reservation = reservationsRepository.GetFetchXml(userId, reservationId);
            if(reservation != null)
            {
                int numberOfBidItemsWithInovice = 0;
                int numberOfCanceledBidItems = 0;
                int numberOfNoPayingBidItems = 0;
                reservation.ActivityDayList.ForEach(day =>
                {
                    string paymentStatus = "";
                    day.BidItemList.ForEach(bidItem =>
                    {
                        if (bidItem.PaymentStatus.Id == "13")
                        {
                            numberOfBidItemsWithInovice++;
                            paymentStatus = "16";
                        }
                        else if (bidItem.PaymentStatus.Id == "15")
                        {
                            numberOfCanceledBidItems++;
                        }
                        else if (bidItem.TotalPriceAfterDiscount == 0)
                        {
                            numberOfNoPayingBidItems++;
                        }
                    });
                    if (numberOfNoPayingBidItems == day.BidItemList.Count)
                    {
                        paymentStatus = "";
                    }
                    else if (numberOfBidItemsWithInovice == day.BidItemList.Count - numberOfNoPayingBidItems)
                    {
                        paymentStatus = "13";
                    }
                    else if (numberOfCanceledBidItems == day.BidItemList.Count - numberOfNoPayingBidItems)
                    {
                        paymentStatus = "15";
                    }
                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDay, ActivityDayCRM>("PaymentStatus"),
                        Value = paymentStatus,
                        Type = "optionset"
                    };
                    activityDayRepository.UpdateField(day.Id, updateCrmItem, userId);
                    numberOfBidItemsWithInovice = 0;
                    numberOfCanceledBidItems = 0;
                    numberOfNoPayingBidItems = 0;
                });
            }           
        }
        public bool updateReservationPaymentStatus(string reservationId, string userId)
        {
            updateActivityDayPaymentStatus(reservationId, userId);
            int numberOfActivityDaysWithInovice = 0;
            int numberOfCanceledActivityDays = 0;
            int numberOfNoPayingActivityDays = 0;
            string paymentStatus = "";
            ReservationItem reservation = reservationsRepository.GetFetchXml(userId, reservationId);
            if(reservation != null)
            {
                foreach(var day in reservation.ActivityDayList)
                {
                    if (day.PaymentStatus.Id == "13")
                    {
                        numberOfActivityDaysWithInovice++;
                        paymentStatus = "16";
                    }
                    else if (day.PaymentStatus.Id == "15")
                    {
                        numberOfCanceledActivityDays++;
                    }
                    else if (day.PaymentStatus.Id == "16")
                    {
                        paymentStatus = "16";
                        break;
                    }
                    else if (day.TotalPriceAfterDiscount == 0)
                    {
                        numberOfNoPayingActivityDays++;
                    }
                }
                if (numberOfNoPayingActivityDays == reservation.ActivityDayList.Count)
                {
                    paymentStatus = "";
                }
                else if (numberOfCanceledActivityDays == reservation.ActivityDayList.Count - numberOfNoPayingActivityDays)
                {
                    paymentStatus = "15";
                }
                else if (numberOfActivityDaysWithInovice == reservation.ActivityDayList.Count - numberOfNoPayingActivityDays)
                {
                    paymentStatus = "13";
                }
                if (reservation.isEventReservation)
                {
                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<EventReservationItem, EventReservationCRM>("PaymentStatus"),
                        Value = paymentStatus,
                        Type = "optionset"
                    };
                    eventReservationRepository.UpdateField(reservation.Id, updateCrmItem, userId);
                }
                else
                {
                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<EventReservationItem, EventReservationCRM>("PaymentStatus"),
                        Value = paymentStatus,
                        Type = "optionset"
                    };
                    reservationsRepository.UpdateField(reservation.Id, updateCrmItem, userId);
                }
                return true;
            }
            return false;
        }

        public void sendReminderEmail(string file ,string id ,string type, string userId, string email = null)
        {
            //create mailInfo from reservation
            MailInfo mailInfo = new MailInfo();
            string from = "";
            List<string> to = new List<string>();
            List<string> cc = new List<string>();
            List<string> bcc = new List<string>();
            Direction direction = Direction.RTL; ;
            string subject = "";
            string message = "";
            if(type == "ReminderToPayInvoice")
            {
                ReservationItem reservation = reservationsRepository.GetFetchXml(userId, id);

                from = reservation.BudgetFramework != null ? reservation.BudgetFramework.Email : "no-reply@yadvashem.org";
                if (!string.IsNullOrEmpty(email))
                {
                    to.Add(email);
                }
                else
                {
                    to.Add(reservation.Contact.CommunicationInfo.Email);
                }
                

                if (!string.IsNullOrEmpty(reservation.Institution.EmailForSendingInvoices))
                {
                    cc.Add(reservation.Institution.EmailForSendingInvoices);
                }

                bcc.Add(from);

                if (reservation.FormLanguage.Value == "אנגלית")
                {
                    direction = Direction.LTR;
                    subject = MailContent.Subject["ReminderToPayInvoice_en"];
                    message = MailContent.Message["ReminderToPayInvoice_en"];

                }
                else
                {
                    subject = MailContent.Subject["ReminderToPayInvoice_he"];
                    message = MailContent.Message["ReminderToPayInvoice_he"];
                }
                subject = subject.Replace("replace_reservationNum", reservation.ReservationNum);
                subject = subject.Replace("replace_date", DateTime.UtcNow.ToString("dd/MM/yyyy"));
            }
            else if (type=="ScheduleForRegister")
            {
                from = "no-reply@yadvashem.org";

                if (!string.IsNullOrEmpty(email))
                {
                    to.Add(email);
                }
                else
                {
                    EventRegistration register = eventRegistrationRepository.GetFetchXml(userId, id);
                    to.Add(register.Contact.CommunicationInfo.Email);
                }

                bcc.Add("Yv.dev.logs@yadvashem.org.il");

                direction = Direction.LTR;
                subject = MailContent.Subject["ScheduleForRegister"];
                message = MailContent.Message["ScheduleForRegister"];

            }
            List<string> files = new List<string>();
            files.Add(file);

            mailInfo.From = from;
            mailInfo.To = to;
            mailInfo.Cc = cc;
            mailInfo.Bcc = bcc;
            mailInfo.Direction = direction;
            mailInfo.Subject = subject;
            mailInfo.Message = message;
            mailInfo.Files = files;

            string json = JsonConvert.SerializeObject(mailInfo);
            string environment = ConfigurationManager.AppSettings["environment"].ToString();
            string YVMailUrl = ConfigurationManager.AppSettings[$"baseUrlYVMail_{environment}"].ToString();
            try
            {
                string jsonResponse = reportsService.CreateRequest($"{YVMailUrl}", "POST", json);
                //update the last auto reminder date in CRM
                if (type == "ReminderToPayInvoice")
                {
                    UpdateCrmItem updateCrmItem = new UpdateCrmItem
                    {
                        Field = "new_last_auto_reminder_date_to_pay_invoice",
                        Value = DateTime.Now.ToString(),
                        Type = "datetime",
                        Alias = "ReservationItem"
                    };
                    reservationsRepository.UpdateField(id, updateCrmItem, userId);
                }
            }
            catch (Exception ex)
            {
                Trace.Write("send mail faild, ex:" + ex);
            }
        }
        private string sendMail(DataForAlmog dataForAlmog1, AlmogToCrm almogToCrm1)
        {
            Trace.WriteLine($"start CreateAlmogInvoice");
            string json = JsonConvert.SerializeObject(new { dataForAlmog = dataForAlmog1, almogToCrm = almogToCrm1 });
            Trace.WriteLine($"CreateAlmogInvoice => json {json.ToString()}");
            string environment = ConfigurationManager.AppSettings["environment"].ToString();
            Trace.WriteLine($"start CreateAlmogInvoice => environment{environment}");
            string YVAlmogUrl = ConfigurationManager.AppSettings[$"baseUrlYVAlmog_{environment}"].ToString();
            Trace.WriteLine($"start CreateAlmogInvoice => YVAlmogUrl {YVAlmogUrl}");
            //Trace.WriteLine($"start CreateAlmogInvoice => {ConfigurationManager.AppSettings[$"$baseUrlYVAlmog_{environment}"]}");
            string jsonResponse = reportsService.CreateRequest($"{YVAlmogUrl}/Invoices", "POST", json);
            Trace.WriteLine($"end CreateAlmogInvoice => jsonResponse {jsonResponse}");
            return jsonResponse;
        }



        public Tuple<bool, string> SendMailReservationVoucherByReservationGuid(string reservationId, string emailAddress, string userId, string language = "",bool isForCancel=false,bool isWithoutRefund=false)
        {

            ReservationItem reservationItem;
            try
            {
                reservationItem = GetFetchXml(userId, reservationId);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{DateTime.Now} ---GetFetchXml get reservation from crm exception---  {ex.Message}, reservationId: {reservationId}");
                return new Tuple<bool, string>(false, $"An error occurred while retriving resevation from crm, reservation id: {reservationId}");
            }
            if (reservationItem == null)
            {
                return new Tuple<bool, string>(false, $"Reservation with id {reservationId}, not found");
            }
            BasicConfirmationPdfData pdfData;
            FileItem confirmationFile = null;
            //if (reservationItem.DivisionRouter != ((int)MainDivision.School).ToString() &&!isForCancel)
            if (!isForCancel)
            {
                try
                {
                    if (reservationItem.DivisionRouter != ((int)MainDivision.School).ToString())
                    {
                        pdfData = SetConfirmationDataFromReservation(reservationItem, language, userId);
                    }
                    else {

                        pdfData = SetConfirmationDataFromReservationForSchool(reservationItem, language, userId);
                    }
                }
                catch (Exception ex)
                {

                    Trace.WriteLine($"{DateTime.Now} ---SetConfirmationDataFromReservation Exception---  {ex.Message}, reservationId: {reservationId}");
                    return new Tuple<bool, string>(false, "An error occurred while converting the data to a confirmation file data");
                }
                try
                {

                    confirmationFile = CreateConfirmationFile(pdfData);
                    if (confirmationFile == null || string.IsNullOrEmpty(confirmationFile.FullPath))
                    {
                        return new Tuple<bool, string>(false, "Confirmation file does not exists.");
                    }

                }
                catch (Exception ex)
                {

                    Trace.WriteLine($"{DateTime.Now} ---CreatePdfConfirmatiomPage Exception---  {ex.Message}, reservationNumber:{reservationItem.ReservationNum}, reservationId: {reservationId}");
                    return new Tuple<bool, string>(false, "An error occurred while creating a confirmation file");
                }
            }
            try
           {
                var message = SendConfirmationMail(emailAddress, confirmationFile, reservationItem ,language ,isForCancel, isWithoutRefund);
                return new Tuple<bool, string>(true, message);

            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{DateTime.Now} ---Send Confirmation Mail Exception---  {ex.Message}, reservationId: {reservationId}");
                return new Tuple<bool, string>(false, "An error occurred while sending mail with attachment");
            }
        }
        private string SendConfirmationMail(string emailAddress, FileItem confirmationFile, ReservationItem reservation,string language, bool isForCancel,bool isWithoutRefund)
        {               
            MailInfo mailInfo = new MailInfo
            {
                From = "no_reply@yadvashem.org.il",
                To = new List<string> { emailAddress },
                Files = new List<string>(),
                Cc = new List<string>()
            };
            if (!isForCancel)
            {
                mailInfo.Files.Add(confirmationFile.FullPath);
            }
            Trace.WriteLine($"{DateTime.Now} ---Send Confirmation Mail {JsonConvert.SerializeObject(mailInfo)}");
            var activityDate =  DateTime.ParseExact(reservation.ActivityDayFirstDay, "dd/MM/yyyy HH:mm:ss", null).ToString("dd/MM/yyyy");
            if (language=="en" ||(string.IsNullOrEmpty(language)&&reservation.FormLanguage.Value == "אנגלית"))
            {
                mailInfo.Direction = Direction.LTR;
                if (reservation.DivisionRouter == ((int)MainDivision.School).ToString())
                {
                    mailInfo.Subject = $"Yad Vashem - Reservation Number {reservation.ReservationNum} - {(isForCancel ? "Cancel Reservation" : "Update Reservation")}";
                    mailInfo.Message = $@"Hello {reservation.Contact.FirstName} {reservation.Contact.LastName},<br><br>
                    Your request for {(!isForCancel?"editing":"cancelling")} reservation details has been received.<br>
                    {(!isForCancel?$"New date of requested visit: {activityDate} at {(reservation.ActivityDayList.Count > 0 ? DateTime.Parse(reservation.ActivityDayList[0].StartDateTime).ToString("HH:mm") : $"Cancelling tour date: {activityDate}")}.<br>":"")}
                    Reservation Number: {reservation.ReservationNum}.<br>
                    Travel Agent / School Name: {reservation.Institution.OnlyName}.<br><br>
                    {(!isForCancel?$@"<b>This confirmation is a final reference for your order.</b><br>
                    Please present the attached Voucher upon your arrival.<br><br>
                    In the coming days, we will contact you to determine the hours of operation, coordinate the schedule and other details.<br><br>
                    If you need to cancel your reservation <a href='https://online-reservations.yadvashem.org/en/editCancelReservation?orderNumber={reservation.ReservationNum}&identify={emailAddress}&isEmailIdentity=true&language=en&isForCancel=true' target='_blank'>please click here</a><br>
                    If you need to edit your reservation <a href='https://forms.yadvashem.org/reservations-edit?reservation_number={reservation.ReservationNum}&email={emailAddress}&tour_date={activityDate}' target='_blank'>please click here</a><br>
                    <b>This confirmation should be kept in case of cancellation or editing of your reservation.</b><br><br>"
                    : $@"<br>{(isWithoutRefund? "According to Yad Vashem rules, if service was canceled too close to the tour date, the payment for it will not be refunded. " : 
                      $@"According to Yad Vashem rules, if registration and handling fees have been paid, they won't be refunded.<br>
                      If paid for additional services, the payment for them will be refunded.")} <br><a href='https://www.yadvashem.org/visiting/registration-fee.html'>Reservation procedure and information</a><br><br>"
                    )}
                   
                    Sincerely<br>
                    International School for Holocaust Studies, Yad Vashem";
                    mailInfo.Bcc = new List<string> { "yv.dev.logs@yadvashem.org.il", reservation.BudgetFramework.Email };
                    
                }
                else
                {
                    mailInfo.Subject = $"Yad Vashem - Reservation Number {reservation.ReservationNum} - {(isForCancel ? "Cancel Reservation" : "Update Reservation")}";
                    var message = $@"Shalom {reservation.Contact.FirstName}<br><br>";
                    if (!isForCancel)
                    {
                        message += "Your request for editing reservation details has been received.<br>";
                        message += "Your reservation has been received and confirmed by Yad Vashem's online reservations system.<br>" +
                                   "<b>This confirmation is a final reference for your order.</b><br>";

                        if (reservation.DivisionRouter == ((int)MainDivision.Archive).ToString())
                        {
                            message += "Please present the attached Voucher to a representative at the Archive Information Desk upon arrival.<br><br>";
                        }
                        else
                        {
                            message += "Please present the attached Voucher to a representative at the Groups Desk upon arrival.<br><br>";
                        }
                    }
                    else{
                        message += "The request to cancel your reservation has been received.<br>";
                    }
                    message += $@"{(isForCancel ? "The cancel details is: tour date" : "The new tour date is:")} {activityDate} at {(reservation.ActivityDayList.Count > 0 ? DateTime.Parse(reservation.ActivityDayList[0].StartDateTime).ToString("HH:mm") : "")}.<br>
                    Reservation Confirmation Number: {reservation.ReservationNum}<br>
                   {(reservation.Institution!=null&&!string.IsNullOrEmpty(reservation.Institution.OnlyName)? $"Travel Agent / School Name: {reservation.Institution.OnlyName}.<br><br>":"<br>")}
                    {(!isForCancel ? $@"If you need to cancel your reservation <a href='https://online-reservations.yadvashem.org/en/editCancelReservation?orderNumber={reservation.ReservationNum}&identify={emailAddress}&isEmailIdentity=true&language=en&isForCancel=true' target='_blank'>please click here</a><br>
                    If you need to edit your reservation <a href='https://forms.yadvashem.org/reservations-edit?reservation_number={reservation.ReservationNum}&email={emailAddress}&tour_date={activityDate}' target='_blank'>please click here</a><br>
                    <b>This confirmation should be kept in case of cancellation or editing of your reservation.</b><br><br>"
                    : $@"<br>{(isWithoutRefund ? "According to Yad Vashem rules, if service was canceled too close to the tour date, the payment for it will not be refunded. " :
                      $@"According to Yad Vashem rules, if registration and handling fees have been paid, they won't be refunded.<br>
                      If paid for additional services, the payment for them will be refunded.")} <br><a href='https://www.yadvashem.org/visiting/registration-fee.html'>Reservation procedure and information</a><br><br>"
                    )}";
                   
                    if (reservation.DivisionRouter == ((int)MainDivision.Archive).ToString())
                    {
                        message += @"For additional queries:<br>Email: <a href='mailto:ref@yadvashem.org.il'>ref@yadvashem.org.il</a><br>Tel: +972-2-644-3745 <br><br>
                        Regards<br>
                        Department for Reference and Information Services<br>
                        Yad Vashem Archives";
                    }
                    else
                    {
                        if (!isForCancel) {
                            message += @"For additional queries:<br>Email: <a href='mailto:visit@yadvashem.org.il'>visit@yadvashem.org.il</a><br>Tel: +972-2-644-3802 <br><br>";
                        }
                        message+=@"Regards<br>
                        Yad Vashem Visitors' Center";
                    }
                    mailInfo.Message = message;
                    mailInfo.Bcc = new List<string> { "yv.dev.logs@yadvashem.org.il", "online.visit@yadvashem.org.il" };//,
                }
            }
            else
            {
                mailInfo.Direction = Direction.RTL;
                if (reservation.DivisionRouter == ((int)MainDivision.School).ToString())
                {
                    mailInfo.Subject = $"יד ושם - הזמנה מס' {reservation.ReservationNum} - {(isForCancel ? "ביטול" : "עדכון")} הזמנה";
                    var message = $@"שלום {reservation.Contact.FirstName} {reservation.Contact.LastName},<br><br>";
                    if (!isForCancel)
                    {
                        message += $@"בקשתך לשינוי פרטי ההזמנה התקבלה. <br>
                        מועד הביקור החדש: {activityDate}.<br>
                        מספר ההזמנה: {reservation.ReservationNum}. <br>
                        שם המזמין: {reservation.Institution.OnlyName}. <br><br>
                        <b>אישור זה מהווה אסמכתא סופית להזמנתך.</b><br>
                        נא להציג את שובר הכניסה/ אישור ההזמנה בעת הגעתך.<br><br>
                        בימים הקרובים ניצור עמך קשר לקביעת שעות הפעילות, תיאום לו""ז ופרטים נוספים. <br><br>
                        לביטול ההזמנה, <a href='https://online-reservations.yadvashem.org/he/editCancelReservation?orderNumber={reservation.ReservationNum}&identify={emailAddress}&isEmailIdentity=true&language=he&isForCancel=true' target='_blank'>הקלק כאן</a><br>
                        לשינוי ההזמנה, <a href='https://forms.yadvashem.org/he/reservations-edit?reservation_number={reservation.ReservationNum}&email={emailAddress}&tour_date={activityDate}' target='_blank'>הקלק כאן</a><br>
                        <b>מומלץ לשמור אישור זה למקרה של ביטול או שינוי הזמנתך</b><br><br>
                        בברכה<br>
                        ביה""ס הבינלאומי להוראת השואה, יד ושם";
                       
                    }
                    else {
                        message += $@"בקשתך לביטול ההזמנה התקבלה. <br>
                        מועד הביקור שבוטל: {activityDate}.<br>
                        מספר ההזמנה: {reservation.ReservationNum}. <br>
                        שם המזמין: {reservation.Institution.OnlyName}. <br><br>
                        {(isWithoutRefund? "עפ\"י כללי יד ושם, שירות שבוטל סמוך למועד הסיור התשלום בגינו לא יוחזר." : @"
                        עפ"" י כללי יד ושם, במידה ושולמו דמי הרשמה וטיפול, התשלום לא יוחזר. <br>
                        במידה ושולם עבור שירותים נוספים, התשלום עבורם יוחזר.") }<br> <a href='https://www.yadvashem.org/he/visiting/registration-fee.html'>למידע ונהלים אודות הזמנת ביקור</a> <br><br><br>
                        בברכה<br>
                        ביה""ס הבינלאומי להוראת השואה, יד ושם";
                    }
                    mailInfo.Message = message;
                    mailInfo.Bcc = new List<string> { "yv.dev.logs@yadvashem.org.il", reservation.BudgetFramework.Email };
                }
                else {
                    mailInfo.Subject = $"יד ושם - הזמנה מס' {reservation.ReservationNum} - {(isForCancel ? "ביטול" : "עדכון")} הזמנה";
                    var message = $@"שלום {reservation.Contact.FirstName} {reservation.Contact.LastName},<br><br>";
                    if (!isForCancel)
                    {
                        message += $"בקשתך לשינוי פרטי ההזמנה התקבלה. <br>";
                        message += "הזמנתך התקבלה ואושרה על ידי מערכת ההזמנות המקוונת של יד ושם.<br>";
                        message += "<b>אישור זה מהווה אסמכתא סופית להזמנתך.</b><br>";

                        if (reservation.DivisionRouter == ((int)MainDivision.Archive).ToString())
                        {
                            message += "<b> נא להציג את שובר הכניסה, לנציג בדלפק המודיעין בארכיון, במועד הביקור.</b><br><br>";
                        }
                        else
                        {
                            message += "<b> נא להציג את שובר הכניסה, לנציג בדלפק הקבוצות, במועד הביקור.</b><br><br>";
                        }
                    }
                    else {
                        message += "בקשתך לביטול ההזמנה התקבלה.<br>";

                    }
                    message += $@"פרטי הביקור {(isForCancel ? "שבוטל" : "החדש")}: תאריך {activityDate} בשעה {(reservation.ActivityDayList.Count > 0 ? DateTime.Parse(reservation.ActivityDayList[0].StartDateTime).ToString("HH:mm") : "")}.<br>
                    מספר אישור ההזמנה: {reservation.ReservationNum} <br>
                   {(reservation.Institution != null && !string.IsNullOrEmpty(reservation.Institution.OnlyName) ? $"שם המזמין: {reservation.Institution.OnlyName}. <br>" : "")}<br>
                     {(isForCancel ? $@"<br> {(isWithoutRefund ? "עפ\"י כללי יד ושם, שירות שבוטל סמוך למועד הסיור התשלום בגינו לא יוחזר."
                     : @"עפ"" י כללי יד ושם, במידה ושולמו דמי הרשמה וטיפול, התשלום לא יוחזר. <br>
                        במידה ושולם עבור שירותים נוספים, התשלום עבורם יוחזר.")}
                        <br> <a href='https://www.yadvashem.org/he/visiting/registration-fee.html'>למידע ונהלים אודות הזמנת ביקור</a>  <br><br><br>"
                        : $@"לביטול ההזמנה, <a href='https://online-reservations.yadvashem.org/he/editCancelReservation?orderNumber={reservation.ReservationNum}&identify={emailAddress}&isEmailIdentity=true&language=he&isForCancel=true' target='_blank'>הקלק כאן</a><br>
                        לשינוי ההזמנה, <a href='https://forms.yadvashem.org/he/reservations-edit?reservation_number={reservation.ReservationNum}&email={emailAddress}&tour_date={activityDate}' target='_blank'>הקלק כאן</a><br>
                       <b>מומלץ לשמור אישור זה למקרה של ביטול או שינוי הזמנתך</b><br><br>"
                        )}";
                    if (reservation.DivisionRouter == ((int)MainDivision.Archive).ToString())
                    {
                        message += "לבירורים ושאלות:  <br>";
                        message += $@"דואר אלקטרוני: <a href='mailto:ref@yadvashem.org.il'>ref@yadvashem.org.il</a><br>טלפון: +972-2-644-3745<br><br>
                        בברכה <br> 
                        מח' יעץ ומידע <br> 
                        ארכיון יד ושם";
                    }
                    else
                    {
                        if (!isForCancel) {
                            message += "לבירורים ושאלות:  <br>";
                            message += $@"דואר אלקטרוני: <a href='mailto:visit@yadvashem.org.il'>visit@yadvashem.org.il</a><br>טלפון: +972-2-644-3802<br><br>";
                        }
                       message+=@"בברכה <br> 
                        מרכז הזמנות, יד ושם";
                    }
                    mailInfo.Message = message;
                    mailInfo.Bcc = new List<string> { "yv.dev.logs@yadvashem.org.il", "online.visit@yadvashem.org.il" };//,
                }
            }
            string json = JsonConvert.SerializeObject(mailInfo);
            string YVMailUrl = ConfigurationManager.AppSettings[$"baseUrlYVMail_{environment}"].ToString();
            reportsService.CreateRequest($"{YVMailUrl}", "POST", json);
            return mailInfo.Message;
        }
        private FileItem CreateConfirmationFile(BasicConfirmationPdfData data)
        {
            string json = JsonConvert.SerializeObject(data);
            string pdfCreatorUrl = ConfigurationManager.AppSettings[$"baseUrlPdfCreator_{environment}"].ToString();
            var paramUrl = data is SchoolConfirmationData ? "school" : "agency";
            string jsonResponse = reportsService.CreateRequest($"{pdfCreatorUrl}/confirmation/{paramUrl}", "POST", json);
            FileItem fileItem = JsonConvert.DeserializeObject<FileItem>(jsonResponse);
            return fileItem;
        }
        public AgencyConfirmationData SetConfirmationDataFromReservation(ReservationItem reservationItem,string lang, string userId)
        {

            var tourStartTime = reservationItem.ActivityDayList.Count > 0 ? DateTime.Parse(reservationItem.ActivityDayList[0].StartDateTime).ToString("HH:mm") : "";
            var tourEndTime = reservationItem.ActivityDayList.Count > 0 ? DateTime.Parse(reservationItem.ActivityDayList[0].EndDateTime).ToString("HH:mm") : "";
            var yvComments = !string.IsNullOrEmpty(reservationItem.YvComments) ? (lang=="en"? "Comments:" :"הערות למזמין: " )+ reservationItem.YvComments : "";
            var inviterComments = !string.IsNullOrEmpty(reservationItem.InviterComments) ? (lang=="en"? "Inviter comments:" : "הערות המזמין: ") + reservationItem.InviterComments : "";
            var activityType = reservationItem.ActivityDayList.Count > 0 && reservationItem.ActivityDayList[0].ActivityDayType != null ?
              reservationItem.ActivityDayList[0].ActivityDayType.Value : "";

            var comments = yvComments != "" ? inviterComments != "" ?
              yvComments + '\n' + inviterComments : yvComments : inviterComments;

            var languageValue = reservationItem.Language != null ? reservationItem.Language.Value : "";
            if (!string.IsNullOrEmpty(languageValue) && reservationItem.FormLanguage != null && reservationItem.FormLanguage.Value == "אנגלית")
            {
                languageValue = "English";
            }
            if (lang == "en"||(string.IsNullOrEmpty(lang) && reservationItem.FormLanguage != null && reservationItem.FormLanguage.Value == "אנגלית"))
            {
                if (reservationItem.ActivityDayList.Count > 0 && reservationItem.ActivityDayList[0].ActivityDayType != null)//this.reservationProductsService.activityDayTypeList
                {
                    activityType = activityDayTypeRepository.GetFetchXml(userId, reservationItem.ActivityDayList[0].ActivityDayType.Id)?.ValueEn;
                }
            }

            var tempDateList = new List<string>();

            reservationItem.DateList.ForEach(dateEl => {
                tempDateList.Add(DateTime.ParseExact(dateEl, "dd/MM/yyyy HH:mm:ss", null).ToString("dd/MM/yyyy"));
            });
            var pdfData = new AgencyConfirmationData
            {
                DocLang =!string.IsNullOrEmpty(lang)?lang : (reservationItem.FormLanguage != null && (string.IsNullOrEmpty(reservationItem.FormLanguage.Id) || reservationItem.FormLanguage.Value == "עברית")) ? "he" : "en",
                TourDate = tempDateList.Count > 0 ? string.Join(", ", tempDateList) : "",
                TourTime = reservationItem.ActivityDayList.Count > 0 && !string.IsNullOrEmpty(tourStartTime) && !string.IsNullOrEmpty(tourEndTime) ? tourStartTime : "",
                ActivityType = activityType,
                ApprovedBy = reservationItem.Coordinator != null ? reservationItem.Coordinator.Value : "",
                UpdatedOn = reservationItem.ModifiedOn,
                CreatedOn = reservationItem.CreatedOn,
                Lang = languageValue,
                OrderNum = reservationItem.ReservationNum,
                ContactName = reservationItem.Contact != null ? reservationItem.Contact.FirstName : "" + " " + reservationItem.Contact != null ? reservationItem.Contact.LastName : "",
                ContactPhone = reservationItem.Contact != null ? reservationItem.Contact.CommunicationInfo.Mobile : "",
                ContactEmail = reservationItem.Contact != null ? reservationItem.Contact.CommunicationInfo.Email : "",
                GroupName = reservationItem.GroupName,
                NumberOfParticipants = reservationItem.NumberOfVisitors.ToString(),
                Comments = comments,
                Total = decimal.ToDouble(reservationItem.TotalPrice).ToString(),
                TotalPriceAfterDiscount = decimal.ToDouble(reservationItem.TotalPriceAfterDiscount).ToString(),
                ProductList = new List<TourReservationProduct>()
            };
            

            SetBidDataForCommemorationPdf(reservationItem, pdfData.ProductList, lang,userId);

            return pdfData;
        }
        public void SetBidDataForCommemorationPdf(ReservationItem reservationItem, List<TourReservationProduct> tourProductList,string lang, string userId)
        {

            reservationItem.ActivityDayList.ForEach((day) =>
            {
                var start = DateTime.Parse(day.StartDateTime).ToString("HH:mm");
                var end = DateTime.Parse(day.EndDateTime).ToString("HH:mm");

                var productListId = new List<string>();
                day.BidItemList.ForEach((bid) =>
                {
                    if (bid.Product != null && !string.IsNullOrEmpty(bid.Product.Id))
                        productListId.Add(bid.Product.Id);
                });

                if (productListId.Count > 0)
                {
                    Filter filter = new Filter
                    {
                        Field = "Id",
                        Type = "lookup",
                        Operator = "multiple",
                        Alias = "Product",
                        Value = string.Join(";", productListId)
                    };
                    //Get full products details of activity day
                    var productList = productService.GetAll(userId, "", filter, "");

                    day.BidItemList.ForEach((bid) =>
                    {
                        var languageValue = bid.Language != null ? bid.Language.Value : "";
                        if (lang=="en"||(string.IsNullOrEmpty(lang)&&string.IsNullOrEmpty(languageValue) && reservationItem.FormLanguage != null && reservationItem.FormLanguage.Value == "אנגלית"))
                        {
                            languageValue = "English";
                        }
                        var productName = "";
                        if (bid.Product != null)
                        {
                            var fullProduct = productList.Find(product => product.Id == bid.Product.Id);
                            if (lang=="en"||(string.IsNullOrEmpty(lang) && reservationItem.FormLanguage != null && reservationItem.FormLanguage.Value == "אנגלית"))
                            {
                                productName = !string.IsNullOrEmpty(fullProduct.PaymentNameEn) ? fullProduct.PaymentNameEn : fullProduct.NameEn;
                            }
                            else
                            {   //he
                                productName = !string.IsNullOrEmpty(fullProduct.PaymentName) ? fullProduct.PaymentName : fullProduct.Name;
                            }
                        }
                        tourProductList.Add(new TourReservationProduct
                        {
                            Amount = bid.Amount.ToString(),
                            Name = productName,
                            Price = decimal.ToInt32(bid.UnitPrice).ToString(),
                            Language = languageValue,
                            TotalPriceAfterDiscount = decimal.ToDouble(bid.TotalPriceAfterDiscount).ToString(),
                            DiscountType = bid.DiscountType != null ? bid.DiscountType.Id : null,
                        });
                    });
                }
            });
        }
        public SchoolConfirmationData SetConfirmationDataFromReservationForSchool(ReservationItem reservationItem, string lang, string userId) {
            //var coordinator = reservationItem.Coordinator ==null ? null:serviceProviderRepository.GetServiceProviderForReservation(userId,reservationItem.Coordinator.Id);
            var confirmationData = new SchoolConfirmationData();
            confirmationData.Division = reservationItem.DivisionRouter;
            confirmationData.DocLang = lang;
            confirmationData.VisitLangEn = reservationItem.Language != null ? reservationItem.Language.Value : "";
            confirmationData.VisitLang = reservationItem.Language != null ? reservationItem.Language.Value : "";
            confirmationData.Total = reservationItem.TotalPrice.ToString();
            confirmationData.TotalPriceAfterDiscount = reservationItem.TotalPriceAfterDiscount.ToString();
            confirmationData.Discount = reservationItem.Discount.ToString();
            confirmationData.DiscountType = reservationItem.DiscountType!=null? reservationItem.DiscountType.Id.ToString():"";
            confirmationData.OrderNum= reservationItem.ReservationNum;
            confirmationData.ContactName = reservationItem.Contact != null ? reservationItem.Contact.FirstName + " " + reservationItem.Contact.LastName:"";
            confirmationData.CoordinatorEmail = reservationItem.Contact!=null && reservationItem.Contact.CommunicationInfo !=null? reservationItem.Contact.CommunicationInfo.Email:"";
            confirmationData.ContactPhone = reservationItem.Contact!=null && reservationItem.Contact.CommunicationInfo !=null? reservationItem.Contact.CommunicationInfo.Mobile:"";
            confirmationData.GroupName = reservationItem.GroupName;
            confirmationData.InstitutionName = reservationItem.Institution != null && reservationItem.Institution.Name!=null ? reservationItem.Institution.Name: "";
            confirmationData.InstitutionNameOnly = reservationItem.Institution != null && reservationItem.Institution.OnlyName !=null ? reservationItem.Institution.OnlyName : "";
            confirmationData.InstitutionCompanyNumberOrSymbol = reservationItem.Institution != null && reservationItem.Institution.InstitutionCompanyNumberOrSymbol != null ? reservationItem.Institution.InstitutionCompanyNumberOrSymbol : "";
            confirmationData.InstitutionAddress = reservationItem.Institution != null && reservationItem.Institution.AddressInfo != null && reservationItem.Institution.AddressInfo.Address != null ? reservationItem.Institution.AddressInfo.Address : "";
            confirmationData.InstitutionCity = reservationItem.Institution != null && reservationItem.Institution.AddressInfo != null && reservationItem.Institution.AddressInfo.City != null ? reservationItem.Institution.AddressInfo.City : "";
            confirmationData.Fax = reservationItem.Institution != null && reservationItem.Institution.CommunicationInfo != null && reservationItem.Institution.CommunicationInfo.Fax != null ? reservationItem.Institution.CommunicationInfo.Fax : "";
            confirmationData.NumberOfParticipants = reservationItem.NumberOfVisitors.ToString();
            confirmationData.NumberOfGroups = reservationItem.NumberOfGroups.ToString();
            confirmationData.Grade = reservationItem.Grade != null ? reservationItem.Grade.Value : "";
            confirmationData.AgeGroup = reservationItem.AgeGroup != null ? reservationItem.AgeGroup.Value : "";
            confirmationData.Department = reservationItem.BudgetFramework != null && reservationItem.BudgetFramework.Department != null ? reservationItem.BudgetFramework.Department.Value : "";
            confirmationData.DepartmentFax = reservationItem.BudgetFramework != null && reservationItem.BudgetFramework.Fax != null ? reservationItem.BudgetFramework.Fax:"";
            confirmationData.DepartmentPhone = reservationItem.BudgetFramework != null && reservationItem.BudgetFramework.Telephone != null ? reservationItem.BudgetFramework.Telephone:"";
            confirmationData.DepartmentEmail = reservationItem.BudgetFramework != null && reservationItem.BudgetFramework.Email != null ? reservationItem.BudgetFramework.Email:"";
            confirmationData.CoordinatorName = reservationItem.Coordinator != null ? reservationItem.Coordinator.Value : "";
            confirmationData.CoordinatorNameEn = "";// coordinator != null ?coordinator.FullName: "";
            confirmationData.CoordinatorPhone = "";// coordinator!=null&&coordinator.CommunicationInfo!=null?coordinator.CommunicationInfo.Telephone2:"";
            confirmationData.CoordinatorEmail = "";//coordinator != null && coordinator.CommunicationInfo != null ? coordinator.CommunicationInfo.Email : "";
            confirmationData.ActivityDayList= new List<ActivityDayPdfData>();
            confirmationData.ResponsibleAgent = reservationItem.ResponsibleAgent != null ? reservationItem.ResponsibleAgent.Value : "";
            confirmationData.CustomerComments = reservationItem.YvComments;
            confirmationData.FoodDetails = "";
            if (reservationItem.Language != null && !string.IsNullOrEmpty(reservationItem.Language.Id))
            {
                var key = mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, EnglishLanguageCRM>("Id");
                var langEn = idValueListService.GetLookupListWithFilterByName<EnglishLanguageCRM>("Language", key, reservationItem.Language.Id.ToUpper()).FirstOrDefault();
                confirmationData.VisitLangEn = langEn != null ? langEn.Value : "";
            }
            if (reservationItem.BudgetFramework != null && reservationItem.BudgetFramework.Department != null && !string.IsNullOrEmpty(reservationItem.BudgetFramework.Department.Id)) {
                var key = mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, EnglishDepartmentCRM>("Id");
                var departmentEn = idValueListService.GetLookupListWithFilterByName<EnglishDepartmentCRM>("Department", key, reservationItem.BudgetFramework.Department.Id.ToUpper()).FirstOrDefault();
                confirmationData.DepartmentEn = departmentEn != null ? departmentEn.Value : "";
            }
            
            SetActivityAndBidDataForPdf(reservationItem,confirmationData, userId);
            return confirmationData;


        }
        public void SetActivityAndBidDataForPdf(ReservationItem reservationItem, SchoolConfirmationData confirmationData,string userId) {
            reservationItem.ActivityDayList.ForEach(day => {
                var start =  DateTime.Parse(day.StartDateTime).ToString("HH:mm");
                var end =  DateTime.Parse(day.EndDateTime).ToString("HH:mm");
                var tempItem =new ActivityDayPdfData
                {
                    Date= DateTime.Parse(day.StartDateTime).ToString("dd/MM/yyyy"),
                    ParticipantsNum= day.ActualNumberOfVisitors.ToString(),
                    Location= day.Location.Value,
                    HouresFromTo= start + "-" + end,
                    NumberOfGroups= day.NumberOfGroups.ToString(),
                    Total= day.TotalPrice.ToString(),
                    TotalAfterDiscount= day.TotalPriceAfterDiscount.ToString(),
                    DetailsList= new List<Details>(),
                    CommentFood= day.Location.Value == "יד ושם" ? true : false,
                    CommentTestimony= false,
                    CommentLecture=false,
                    ScheduleList= new List<CommemorationSchedule>(),
                  };
                if (day.Location != null && !string.IsNullOrEmpty(day.Location.Id))
                {
                    var key = mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, EnglishActivityDayLocationCRM>("Id");
                    var locationEn = idValueListService.GetLookupListWithFilterByName<EnglishActivityDayLocationCRM>("ActivityDayLocationList", key, day.Location.Id.ToUpper()).FirstOrDefault();
                    tempItem.LocationEn = locationEn != null ? locationEn.Value : "";
                }

                if (day.RefreshmentsComments != null && day.RefreshmentsComments != ""){
                    confirmationData.FoodDetails += "\n" + day.RefreshmentsComments;
                }

                //      // push bid item
                SetBidDataForPdf(day, tempItem, userId);
                // SetScheduleForCommemorationPdf(day, tempItem);
                confirmationData.ActivityDayList.Add(tempItem);
            });

        
        }
        public void  SetBidDataForPdf( ActivityDay day,  ActivityDayPdfData pdfDay ,string userId)
        {
            // push bid item
            day.BidItemList.ForEach((bid) => {
                var details = new Details
                {
                    Amount = bid.Amount.ToString(),
                    Total = bid.TotalPrice.ToString(),
                    ItemName = bid.Product != null ? bid.Product.Value : "",
                    PricePerUnit = bid.UnitPrice.ToString(),
                    Discount = bid.Discount.ToString(),
                    DiscountType = bid.DiscountType != null ? bid.DiscountType.Id : null,
                    TotalAfterDiscount = bid.TotalPriceAfterDiscount.ToString(),
                    Comments = bid.Comments,
                };
                var product = productService.GetAll(userId, "", new Filter { Field = "Id", Value = bid.Product.Id, Operator = "eq", Alias = "Product" }, "").FirstOrDefault();
                if (product != null) {
                    details.ItemNameEn = product.NameEn;
                }
                pdfDay.DetailsList.Add(details);
            });
        }
    public ReservationItem GetReservationByReservationNum(string userId, string name)
        {
            List<Filter> filters = new List<Filter>();
            filters.Add(new Filter
            {
                Field = "new_num",
                Value = name,
                Type = "string",
                Operator = "eq"
            });
            Tuple<int, List<ReservationItem>> reservation = fullReservationListRepository.GetFetchXml(userId,false, filters);
            if(reservation.Item1 > 1)
            {
                throw new Exception("more then one reservation to the same ReservationNum");
            }
            else if (reservation.Item1 == 0)
            {
                return null;
            }
            return reservation.Item2[0];
        }

        public List<RightToDiscount> GetRightToDiscountList(string userId)
        {
            return rightToDiscountRepository.GetFetchXml(userId, false, null).Item2;
        }
    }
}


// ====================================== OLD FUNCTIONS - TO REMOVE ============================================
//private void CreateOrUpdateReservationOld(ReservationItem item)
//{
//    foreach (var activity in item.ActivityDayList)
//    {

//        activity.Reservation = new IdValuePair() { Id = item.Id.ToString() };
//        activity.Name = "activity-"+ DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");

//        activityDayRepository.CreateOrUpdateEntity(activity);

//        foreach (var itineraryGroup in activity.ItineraryGroupList)
//        {
//            itineraryGroupRepository.CreateOrUpdateEntity(itineraryGroup);
//            foreach (var itineraryItem in itineraryGroup.SchedulerItemList)
//            {

//                itineraryItemRepository.CreateOrUpdateEntity(itineraryItem);
//                if (itineraryItem.ItineraryGroup != null)
//                {
//                    Guid refGroupGuid;
//                    Guid.TryParse(itineraryGroup.Id, out refGroupGuid);
//                    Guid refItemGuid;
//                    Guid.TryParse(itineraryItem.Id, out refItemGuid);
//                    if (refGroupGuid != null && refItemGuid != null)
//                    {
//                        EntityReference refa = new EntityReference("new_itinerary_group", refGroupGuid);
//                        EntityReference refb = new EntityReference("new_itinerary_item", refItemGuid);

//                        if (itineraryItem.ItineraryGroup.Id != string.Empty)//need to Associate
//                        {
//                            basicRepository.Associate(refa, refb, "new_new_itinerary_item_new_itinerary_group");
//                        }
//                        else// need to Disassociate
//                        {
//                            basicRepository.Disassociate(refa, refb, "new_new_itinerary_item_new_itinerary_group");

//                        }
//                    }
//                }

//            }
//        }
//        foreach (var bidItem in activity.BidItem)
//        {
//            bidItem.Name = "bidItem-" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
//            bidItem.ActivityDay = new IdValuePair() { Id = actiGuid.ToString() };     
//            BidItemRepository.CreateOrUpdateEntity(bidItem);
//        }

//    }
//}

