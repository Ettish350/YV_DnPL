using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Data.SendMailWS;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Services.Services
{
    public class InvoiceService : IInvoiceService
    {
        IFullProductRepository fullProductRepository;
        IReservationsRepository reservationsRepository;
        IBudgetFrameworkRepository  budgetFrameworkRepository;
        IIdValueListService idValueListService;
        IProductService productService;
        IFullReservationListRepository fullReservationListRepository;
        ILoginService loginService;
        IReportsService reportsService;
        IAttachmentsService attachmentsService;
        IReservationsService reservationsService;
        IEventRegistrationRepository eventRegistrationRepository;
        InstitutionsService institutionsService;
        private readonly IMapFunctionRepository mapFunctionRepository;


        public InvoiceService(
            IFullProductRepository fullProductRepository,
            IReservationsRepository reservationsRepository,
            IBudgetFrameworkRepository budgetFrameworkRepository,
            IIdValueListService idValueListService,
            IProductService productService,
            IFullReservationListRepository fullReservationListRepository,
            ILoginService loginService,
            IReportsService reportsService,
            IAttachmentsService attachmentsService,
            IReservationsService reservationsService,
            IMapFunctionRepository mapFunctionRepository,
            IEventRegistrationRepository eventRegistrationRepository,
            InstitutionsService institutionsService
            )
        {
            this.fullProductRepository = fullProductRepository;
            this.reservationsRepository = reservationsRepository;
            this.budgetFrameworkRepository = budgetFrameworkRepository;
            this.idValueListService = idValueListService;
            this.productService = productService;
            this.fullReservationListRepository = fullReservationListRepository;
            this.loginService = loginService;
            this.reportsService = reportsService;
            this.attachmentsService = attachmentsService;
            this.reservationsService = reservationsService;
            this.mapFunctionRepository= mapFunctionRepository;
            this.eventRegistrationRepository= eventRegistrationRepository;
            this.institutionsService= institutionsService;
        }


        public ReservationForInvoice GetInvoiceDataFromReservation(string userId, List<string> reservationGuidList, List<string> activityDayGuidList = null, List<string> bidItemGuidList =null)
        {
            try
            {
                Filter filter = new Filter
                {
                    Field = "new_reservationid",
                    Type = "lookup",
                    Operator = "multiple",
                    Value = String.Join(";", reservationGuidList.ToArray())
                };


                var reservationsList = fullReservationListRepository.GetFetchXml(userId, false, filter).Item2;
                //remove the bidItems that empty
                reservationsList.ForEach(reservation =>
                {
                    reservation.ActivityDayList.ForEach(day =>
                    {
                        day.BidItemList = day.BidItemList.Where(bid => string.IsNullOrEmpty(bid.Product.Id) == false).ToList();
                    });
                });
                Trace.WriteLine($"GetInvoiceDataFromReservation => reservationsList {reservationsList}");
                bool isAllReservationSelected = false;
                int numOfBidItems = 0;

                //    //check if all the activity days in reservation is selected
                //    if(reservationsList[0].ActivityDayList.Count == activityDayGuidList.Count)
                //    {
                //        //check if all the bid items in activity days is selected
                //        reservationsList[0].ActivityDayList.ForEach(day => numOfBidItems += day.BidItemList.Count);
                //        if(numOfBidItems == bidItemGuidList.Count)
                //        {
                //            isAllReservationSelected = true;
                //        }
                //    }
                if (reservationsList.Count == 1)
                { //remove all the days and bid items that not relevant for the invoice
                    reservationsList[0].ActivityDayList = reservationsList[0].ActivityDayList.Where(activityDay => activityDayGuidList.Contains(activityDay.Id.ToLower())).ToList();
                    for (int i = 0; i < reservationsList[0].ActivityDayList.Count; i++)
                    {
                        reservationsList[0].ActivityDayList[i].BidItemList =
                            reservationsList[0].ActivityDayList[i].BidItemList.Where(bidItem => bidItemGuidList.Contains(bidItem.Id.ToLower())).ToList();
                    }
                }
                //else //בחשבון מאוחד כל המוצרים נבחרו
                //{
                //    isAllReservationSelected = true;
                //}
                ReservationForInvoice reservationForInvoice = new ReservationForInvoice();
                //if(!reservation.IsOrderConfirmed)// devora- take care in client side
                //{
                //    throw new Exception("ההזמנה עדיין לא אושרה");
                //}
                reservationForInvoice.DataForAlmog = GetDataForAlmog(userId, reservationsList);
                Trace.WriteLine($"GetInvoiceDataFromReservation => DataForAlmog {reservationForInvoice.DataForAlmog}");
                reservationForInvoice.DataForInvoicePdf = GetDataForInvoicePdf(userId, reservationsList);
                Trace.WriteLine($"GetInvoiceDataFromReservation => DataForInvoicePdf {reservationForInvoice.DataForInvoicePdf}");
                return reservationForInvoice;

            }
            catch (Exception ex)
            {

                throw new Exception("error getting details for reservation guid: " + String.Join(";", reservationGuidList.ToArray()));
            }
        }
        public ReservationForInvoice GetInvoiceDataFromEventRegistration(string userId, List<string> eventRegistratiionGuidList, List<string> bidItemGuidList = null)
        {
            try
            {
                Filter filter = new Filter
                {
                    Field = "new_event_registrationid",
                    Type = "lookup",
                    Operator = "multiple",
                    Value = string.Join(";", eventRegistratiionGuidList.ToArray())
                };


                var eventRegistrationList = eventRegistrationRepository.GetFetchXml(userId, false, filter).Item2;
                //remove the bidItems that empty
                eventRegistrationList.ForEach(eventRegistration =>
                {
                        eventRegistration.BidItemList = eventRegistration.BidItemList.Where(bid => string.IsNullOrEmpty(bid.Product.Id) == false).ToList();
                });
                
                if (eventRegistrationList.Count == 1)
                { //remove all  bid items that not relevant for the invoice
                    eventRegistrationList[0].BidItemList = eventRegistrationList[0].BidItemList.Where(bidItem => bidItemGuidList.Contains(bidItem.Id.ToLower())).ToList();     
                }
                ReservationForInvoice reservationForInvoice = new ReservationForInvoice();

                reservationForInvoice.DataForAlmog = GetEventRegistrationDataForAlmog(userId, eventRegistrationList);
                Trace.WriteLine($"GetInvoiceDataFromReservation => DataForAlmog {reservationForInvoice.DataForAlmog}");
                reservationForInvoice.DataForInvoicePdf = GetDataEventRegistrationForInvoicePdf(userId, eventRegistrationList);
                Trace.WriteLine($"GetInvoiceDataFromReservation => DataForInvoicePdf {reservationForInvoice.DataForInvoicePdf}");
                return reservationForInvoice;

            }
            catch (Exception ex)
            {

                throw new Exception("error getting details for eventRegistratiionGuidList guid: " + string.Join(";", eventRegistratiionGuidList.ToArray()));
            }
        }

        private DataForAlmog GetEventRegistrationDataForAlmog(string userId, List<EventRegistration> eventRegistrationList)
        {
            string itemNameDescr = string.Empty;
            var dataForAlmog = new DataForAlmog();
            dataForAlmog.lines = new List<LineForAlmog>();
            dataForAlmog.externalId = "";
            dataForAlmog.externalReference = string.Empty;
            foreach (var eventRegistration in eventRegistrationList)
            {
                var date = eventRegistration.StartDateTime.ToString("dd-MM-yyyy");

                dataForAlmog.ledgerShortDescription = "פעילות ב:" + date;

                dataForAlmog.externalReference += eventRegistration.ReservationNum + " ";
                dataForAlmog.GroupName = eventRegistration.GroupName;
                LineForAlmog line;
                if (eventRegistration.DivisionRouter.Id == "2")//commemoration 
                {
                    dataForAlmog.ledgerShortDescription = "דרישת תשלום - יד ושם";
                }
                decimal discountInPrice = 0;
                decimal totalDiscountInPrice = 0;

                if (eventRegistration.DiscountType.Id != "" && eventRegistration.DiscountType.Id != "0" && eventRegistration.TotalPrice != 0)
                {
                    discountInPrice = eventRegistration.TotalPrice - eventRegistration.TotalPriceAfterDiscount;
                }

                var dayDate = eventRegistration.StartDateTime.ToString("dd-MM-yyyy");
                Filter productFilter = new Filter
                {
                    Field = "Id",
                    Type = "lookup",
                    Operator = "multiple",
                    Alias = "Product",
                };
                //create filter list with for one request insted of multiple requests...
                foreach (var bid in eventRegistration.BidItemList)
                {
                    if (productFilter.Value != null)
                    {
                        productFilter.Value += ";" + bid.Product.Id;
                    }
                    else
                    {
                        productFilter.Value = bid.Product.Id;
                    }

                    line = new LineForAlmog(
                            bid.UnitPrice.ToString(),// צריך לחשב את המחיר
                            bid.TotalPriceAfterDiscount.ToString(),
                            bid.Product.Id,// איכלוס  של המוצר ממנו צריך להביא את הנתון
                            bid.Product.Value,
                            bid.Amount.ToString()
                        );
                    if (discountInPrice != 0)
                    {
                        var discountForLine = System.Math.Round((bid.TotalPrice / eventRegistration.TotalPrice) * discountInPrice, 2);
                        totalDiscountInPrice += discountForLine;
                        line.sumAfterDiscount = (bid.TotalPrice - discountForLine).ToString();
                    }
                    dataForAlmog.lines.Add(line);

                }
                string additionalFetchXml = @"<link-entity name='new_almog_interface' from='new_almog_interfaceid' to='new_almog_interface' alias='AlmogInterface' link-type='outer'>
                                                <all-attributes />
                                                </link-entity>";
                //get all products for almogSubCode;
                var productList = productService.GetAll(userId, "", productFilter, additionalFetchXml);
                Filter filterBudgetFramework = new Filter
                {
                    Field = "new_event_reservations",
                    Operator = "eq",
                    Type = "lookup",
                    Value = eventRegistration.EventReservation.Id
                };
                var eventReservationRevenueSection = "";
                var budgetFrameworkList = budgetFrameworkRepository.GetFetchXml(userId, false, filterBudgetFramework).Item2;
                if (budgetFrameworkList.Count > 0)
                {
                    eventReservationRevenueSection = budgetFrameworkList.First().RevenueSection;
                }
                foreach (var lineForAlmog in dataForAlmog.lines)
                {

                    Product product = productList.Find(p => p.Id == lineForAlmog.almogSubCode);
                    if (product != null)
                    {
                        if (!string.IsNullOrEmpty(product.RevenueSection))
                        {
                            lineForAlmog.almogSubCode = product.RevenueSection;
                        }
                        else
                        {
                            lineForAlmog.almogSubCode = eventReservationRevenueSection;
                        }

                        if (!string.IsNullOrEmpty(product.Sku))
                        {
                            lineForAlmog.sku = product.Sku;
                        }
                    }
                }

                //חישוב הפרשים לאחר עיגול  ההנחה ל2 מקומות אחרי הנקודה העשרונית
                if (dataForAlmog.lines.Count > 0)
                {
                    dataForAlmog.lines[0].sumAfterDiscount = (decimal.Parse(dataForAlmog.lines[0].sumAfterDiscount) - (discountInPrice - totalDiscountInPrice)).ToString();
                }

                //כשיש מוסד משלם- נשלח אותו  כדי שיפתחו עליו חשבון, ובמוסד הקשור נשלח את הנתונים של המוסד בן
                // במצב שאין מוסד משלם, נשלח את הפרטים של המוסד עצמו

                var institution = institutionsService.GetFetchXml(userId, eventRegistration.Institution.Id);
                    
                if (institution.PayingInstitution != null)//יש מוסד משלם
                {
                    dataForAlmog.account = institution.PayingInstitution.InstitutionCompanyNumberOrSymbol;
                    dataForAlmog.code = institution.PayingInstitution.AlmogCode;
                    dataForAlmog.relatedAccount = institution.institutionCompanyNumberOrSymbol;
                    dataForAlmog.relatedCode = institution.AlmogCode;

                }
                else//אין מוסד משלם
                {
                    dataForAlmog.account = institution.institutionCompanyNumberOrSymbol;
                    dataForAlmog.code = institution.AlmogCode;
                    }
            }
            return dataForAlmog;

        }
        private DataForInvoicePdf GetDataForInvoicePdf(string userId, List<ReservationItem> reservationItemList)
        {
            var dataForInvoicePdf = new DataForInvoicePdf();
            dataForInvoicePdf.tables = new List<TableDetails>();
            decimal invoiceTotalPrice = 0;
            decimal invoiceTotalPriceAfterDiscount = 0;
            decimal totalAfterDiscount = 0;
            decimal total = 0;
            var firstReservationItem = reservationItemList[0];
            dataForInvoicePdf.status = "original";
            dataForInvoicePdf.lang = firstReservationItem.FormLanguage.Value;
            dataForInvoicePdf.invoiceNum = string.Empty;
            dataForInvoicePdf.departmentGuid = firstReservationItem.BudgetFramework.Department.Id;
            dataForInvoicePdf.department = firstReservationItem.BudgetFramework.Department.Value;
            var temp = idValueListService.GetLookupListByName<EnglishDepartmentCRM>("Department")
                .Where(l => l.Id == firstReservationItem.BudgetFramework.Department.Id).FirstOrDefault();
            dataForInvoicePdf.departmentEn = temp.Value;
            dataForInvoicePdf.division = firstReservationItem.DivisionRouter;
            dataForInvoicePdf.gefenType = firstReservationItem.GefenType.Value;


            foreach (var res in reservationItemList)
            {
                var table= new TableDetails();
                table.tableRowDetails = new List<TableRowDetails>();
                table.groupName = res.GroupName;
                table.reservationNum = res.ReservationNum;
                dataForInvoicePdf.comments += res.YvComments;
                table.contact = res.Contact.FirstName + " " + res.Contact.LastName;
                //כשיש הנחה על כל ההזמנה יש לחשבן אותהולא את ההנחה לפי פריט כי כל ההזמנה נבחרה
                if (res.Discount != 0)
                {
                    table.totalPrice = string.Format("{0:0.##}", res.TotalPrice);
                    table.totalPriceAfterDiscount = string.Format("{0:0.##}", res.TotalPriceAfterDiscount);
                    invoiceTotalPrice += res.TotalPrice;
                    invoiceTotalPriceAfterDiscount += res.TotalPriceAfterDiscount;
                    table.discount = string.Format("{0:0.##}", res.Discount);
                    table.discountType = res.DiscountType.Id;
                }

                TableRowDetails tableRowDetails;

                foreach (var activityDay in res.ActivityDayList)
                {
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
                    var productList = productService.GetAll(userId, "", filter, additionalFetchXml);

                    foreach (var bidItem in activityDay.BidItemList)
                    {
                        //GET PRODUCT:

                        Product bidProduct = productList.Find(p => p.Id == bidItem.Product.Id);
                        tableRowDetails = new TableRowDetails();
                        tableRowDetails.amount = bidItem.Amount.ToString();
                        tableRowDetails.date = DateTime.Parse(activityDay.EndDateTime).ToString("yyyy-MM-dd");
                        tableRowDetails.dateForDisplay = DateTime.Parse(activityDay.EndDateTime).ToString("dd/MM/yyyy");
                        tableRowDetails.discount = string.Format("{0:0}", bidItem.Discount);
                        tableRowDetails.discountType = bidItem.DiscountType.Id;
                        tableRowDetails.language = bidItem.Language.Id;
                        tableRowDetails.perut = bidItem.Product.Value;
                        tableRowDetails.perutEn = bidProduct.NameEn; // "TODO ADINA DEVORAH PRODUCTS"; // bidItem.Product.NameEn;
                        tableRowDetails.price = string.Format("{0:0.##}", bidItem.UnitPrice);
                        tableRowDetails.total = string.Format("{0:0.##}", bidItem.TotalPrice);
                        tableRowDetails.totalAfterDiscount = string.Format("{0:0.##}", bidItem.TotalPriceAfterDiscount);
                        tableRowDetails.reservation = res.Id;

                       // if (res.DivisionRouter == "1")
                        //{
                            //for school
                            //if there is a spesific RevenueSection use it else use RevenueSection of reservation.
                            tableRowDetails.almogSubCode = bidProduct.RevenueSection;
                            if (string.IsNullOrEmpty(tableRowDetails.almogSubCode))
                            {
                                tableRowDetails.almogSubCode = res.BudgetFramework.RevenueSection;
                            }
                            totalAfterDiscount += bidItem.TotalPriceAfterDiscount;
                            total += bidItem.TotalPrice;
                        //}
                        //if (res.DivisionRouter == "2")
                        //{
                            //for commemoration
                          //  tableRowDetails.almogSubCode = bidProduct.RevenueSection;
                            tableRowDetails.sku = bidProduct.Sku;
                            //totalAfterDiscount += bidItem.TotalPriceAfterDiscount;
                            //total += bidItem.TotalPrice;
                        //}

                        table.tableRowDetails.Add(tableRowDetails);

                    }
                    //כשאין הנחה על כל ההזמנה יש לחשבן את ההנחה לפי פריט
                    if (res.Discount == 0)
                    {
                        table.totalPriceAfterDiscount = string.Format("{0:0.##}", totalAfterDiscount);
                        table.totalPrice = string.Format("{0:0.##}", total);
                        table.discount = "";
                        table.discountType = null;
                        invoiceTotalPrice += total;
                        invoiceTotalPriceAfterDiscount += totalAfterDiscount;
                    }                   
                }
                dataForInvoicePdf.tables.Add(table);
                total = 0;
                totalAfterDiscount = 0;
            }


            dataForInvoicePdf.InvoiceTotalPrice = string.Format("{0:0.##}", invoiceTotalPrice);
            dataForInvoicePdf.InvoiceTotalPriceAfterDiscount = string.Format("{0:0.##}", invoiceTotalPriceAfterDiscount);



            if (firstReservationItem.Institution.PayingInstitution != null)//יש לקוח משלם
            {
                dataForInvoicePdf.name = firstReservationItem.Institution.PayingInstitution.Name;
                dataForInvoicePdf.address = string.Empty;
                dataForInvoicePdf.relatedAccountName = firstReservationItem.Institution.Name;
                dataForInvoicePdf.relatedAccountAddress = firstReservationItem.Institution.AddressInfo.Address + " " + firstReservationItem.Institution.AddressInfo.City + " " + firstReservationItem.Institution.AddressInfo.ZipCode;
                dataForInvoicePdf.telephone = firstReservationItem.Institution.PayingInstitution.Telephone;
                dataForInvoicePdf.semel = firstReservationItem.Institution.PayingInstitution.CompanyNumber;

            }
            else// אין לקוח משלם
            {
                dataForInvoicePdf.name = firstReservationItem.Institution.OnlyName;
                if (firstReservationItem.Institution.AddressInfo != null)
                {
                    dataForInvoicePdf.address = firstReservationItem.Institution.AddressInfo.Address + " " + firstReservationItem.Institution.AddressInfo.City + " " + firstReservationItem.Institution.AddressInfo.ZipCode;

                }
                if (firstReservationItem.Institution.CommunicationInfo != null)
                {
                    dataForInvoicePdf.telephone = firstReservationItem.Institution.CommunicationInfo.Telephone;
                }
                dataForInvoicePdf.relatedAccountName = string.Empty;
                dataForInvoicePdf.relatedAccountAddress = string.Empty;
                dataForInvoicePdf.semel = firstReservationItem.Institution.InstitutionCompanyNumberOrSymbol;
            }
            dataForInvoicePdf.institutionGuid = firstReservationItem.Institution.Id;


            return dataForInvoicePdf;

        }
        private DataForInvoicePdf GetDataEventRegistrationForInvoicePdf(string userId, List<EventRegistration> eventRegistrationsList)
        {
            var dataForInvoicePdf = new DataForInvoicePdf();
            dataForInvoicePdf.tables = new List<TableDetails>();
            decimal invoiceTotalPrice = 0;
            decimal invoiceTotalPriceAfterDiscount = 0;
            decimal totalAfterDiscount = 0;
            decimal total = 0;
            var firstEventRegistrationItem = eventRegistrationsList.First();
            dataForInvoicePdf.status = "original";
            dataForInvoicePdf.lang = firstEventRegistrationItem.Language.Value;
            dataForInvoicePdf.invoiceNum = string.Empty;
            Filter filterBudgetFramework = new Filter
            {
                Field = "new_event_reservations",
                Operator = "eq",
                Type = "lookup",
                Value = firstEventRegistrationItem.EventReservation.Id
            };
            var budgetFrameworkList = budgetFrameworkRepository.GetFetchXml(userId, false, filterBudgetFramework).Item2;
            var eventReservationRevenueSection = "";
            if (budgetFrameworkList.Count > 0)
            {
                 eventReservationRevenueSection = budgetFrameworkList[0].RevenueSection;
                var department = budgetFrameworkList[0].Department;

                dataForInvoicePdf.departmentGuid = department.Id;
                dataForInvoicePdf.department = department.Value;
                var temp = idValueListService.GetLookupListByName<EnglishDepartmentCRM>("Department")
                    .Where(l => l.Id == department.Id).FirstOrDefault();
                dataForInvoicePdf.departmentEn = temp.Value;
            }
            dataForInvoicePdf.division = firstEventRegistrationItem.DivisionRouter.Id;
            dataForInvoicePdf.gefenType = null;//firstReservationItem.GefenType.Value;


            foreach (var eventRegistration in eventRegistrationsList)
            {
                var table = new TableDetails();
                table.tableRowDetails = new List<TableRowDetails>();
                table.groupName = eventRegistration.GroupName;
                table.reservationNum = eventRegistration.ReservationNum;
                dataForInvoicePdf.comments += eventRegistration.Comments;
                table.contact = eventRegistration.Contact.FirstName + " " + eventRegistration.Contact.LastName;
                //כשיש הנחה על כל ההזמנה יש לחשבן אותהולא את ההנחה לפי פריט כי כל ההזמנה נבחרה
                if (eventRegistration.Discount != 0)
                {
                    table.totalPrice = string.Format("{0:0.##}", eventRegistration.TotalPrice);
                    table.totalPriceAfterDiscount = string.Format("{0:0.##}", eventRegistration.TotalPriceAfterDiscount);
                    invoiceTotalPrice += eventRegistration.TotalPrice;
                    invoiceTotalPriceAfterDiscount += eventRegistration.TotalPriceAfterDiscount;
                    table.discount = string.Format("{0:0.##}", eventRegistration.Discount);
                    table.discountType = eventRegistration.DiscountType.Id;
                }

                TableRowDetails tableRowDetails;

                Filter filter = new Filter
                {
                    Field = "Id",
                    Type = "lookup",
                    Operator = "multiple",
                    Alias = "Product",
                };
                    foreach (var bid in firstEventRegistrationItem.BidItemList)
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
                    var productList = productService.GetAll(userId, "", filter, additionalFetchXml);

                    foreach (var bidItem in eventRegistration.BidItemList)
                    {
                        //GET PRODUCT:

                        Product bidProduct = productList.Find(p => p.Id == bidItem.Product.Id);
                        tableRowDetails = new TableRowDetails();
                        tableRowDetails.amount = bidItem.Amount.ToString();
                        tableRowDetails.date = eventRegistration.StartDateTime.ToString("yyyy-MM-dd");
                        tableRowDetails.dateForDisplay = eventRegistration.StartDateTime.ToString("dd/MM/yyyy");
                        tableRowDetails.discount = string.Format("{0:0}", bidItem.Discount);
                        tableRowDetails.discountType = bidItem.DiscountType.Id;
                        tableRowDetails.language = bidItem.Language.Id;
                        tableRowDetails.perut = bidItem.Product.Value;
                        tableRowDetails.perutEn = bidProduct.NameEn; // "TODO ADINA DEVORAH PRODUCTS"; // bidItem.Product.NameEn;
                        tableRowDetails.price = string.Format("{0:0.##}", bidItem.UnitPrice);
                        tableRowDetails.total = string.Format("{0:0.##}", bidItem.TotalPrice);
                        tableRowDetails.totalAfterDiscount = string.Format("{0:0.##}", bidItem.TotalPriceAfterDiscount);
                        tableRowDetails.reservation = eventRegistration.Id;

               
                        tableRowDetails.almogSubCode = bidProduct.RevenueSection;
                        if (string.IsNullOrEmpty(tableRowDetails.almogSubCode))
                        {
                            tableRowDetails.almogSubCode = eventReservationRevenueSection;
                        }
                        totalAfterDiscount += bidItem.TotalPriceAfterDiscount;
                        total += bidItem.TotalPrice;
                  
                        tableRowDetails.sku = bidProduct.Sku;
                        table.tableRowDetails.Add(tableRowDetails);

                    }
                    //כשאין הנחה על כל ההזמנה יש לחשבן את ההנחה לפי פריט
                    if (eventRegistration.Discount == 0)
                    {
                        table.totalPriceAfterDiscount = string.Format("{0:0.##}", totalAfterDiscount);
                        table.totalPrice = string.Format("{0:0.##}", total);
                        table.discount = "";
                        table.discountType = null;
                        invoiceTotalPrice += total;
                        invoiceTotalPriceAfterDiscount += totalAfterDiscount;
                    }
                
                dataForInvoicePdf.tables.Add(table);
                total = 0;
                totalAfterDiscount = 0;
            }


            dataForInvoicePdf.InvoiceTotalPrice = string.Format("{0:0.##}", invoiceTotalPrice);
            dataForInvoicePdf.InvoiceTotalPriceAfterDiscount = string.Format("{0:0.##}", invoiceTotalPriceAfterDiscount);

            var institution = institutionsService.GetFetchXml(userId, firstEventRegistrationItem.Institution.Id);

            if (institution.PayingInstitution != null)//יש לקוח משלם
            {
                dataForInvoicePdf.name = institution.PayingInstitution.Name;
                dataForInvoicePdf.address = string.Empty;
                dataForInvoicePdf.relatedAccountName = institution.Name;
                dataForInvoicePdf.relatedAccountAddress = institution.AddressInfo.Address + " " + institution.AddressInfo.City + " " + institution.AddressInfo.ZipCode;
                dataForInvoicePdf.telephone = institution.PayingInstitution.Telephone;
                dataForInvoicePdf.semel = institution.PayingInstitution.CompanyNumber;

            }
            else// אין לקוח משלם
            {

                dataForInvoicePdf.name = institution.Name;
                if (institution.AddressInfo != null)
                {
                    dataForInvoicePdf.address = institution.AddressInfo.Address + " " + institution.AddressInfo.City + " " + institution.AddressInfo.ZipCode;

                }
                if (institution.CommunicationInfo != null)
                {
                    dataForInvoicePdf.telephone = institution.CommunicationInfo.Telephone;
                }
                dataForInvoicePdf.relatedAccountName = string.Empty;
                dataForInvoicePdf.relatedAccountAddress = string.Empty;
                dataForInvoicePdf.semel = institution.institutionCompanyNumberOrSymbol;
            }
            dataForInvoicePdf.institutionGuid = institution.Id;


            return dataForInvoicePdf;

        }

        private DataForAlmog GetDataForAlmog(string userId, List<ReservationItem> reservationItemList)
        {
            string itemNameDescr = string.Empty;
            var dataForAlmog = new DataForAlmog();
            dataForAlmog.lines = new List<LineForAlmog>();
            dataForAlmog.externalId = "";
            dataForAlmog.externalReference = string.Empty;
            foreach (var reservationItem in reservationItemList)
            {
                try
                {
                    string dates = string.Empty;
                    foreach (var item in reservationItem.ActivityDayList)
                    {
                        var date = DateTime.Parse(item.StartDateTime);
                        dates += date.ToString("dd-MM-yyyy") + " ,";
                    }
                    dataForAlmog.ledgerShortDescription = "פעילות ב:" + dates;
                }
                catch (Exception)
                {

                    dataForAlmog.ledgerShortDescription = "הדרכה";
                }

                dataForAlmog.externalReference += reservationItem.ReservationNum + " ";
                dataForAlmog.GroupName = reservationItem.GroupName;
                LineForAlmog line;
                //if (reservationItem.DivisionRouter == "1")//school
                //{
                //    line = new LineForAlmog(
                //        reservationItem.TotalPriceAfterDiscount.ToString(),
                //        reservationItem.TotalPriceAfterDiscount.ToString(),// אין התיחסות להנחה בחשבונית המחיר מגיע כמחיר סופי
                //        reservationItem.BudgetFramework.RevenueSection,
                //        itemNameDescr,
                //        "1"
                //    );
                //    dataForAlmog.lines.Add(line);
                //}
                //else//commemoration 
                //{
                if (reservationItem.DivisionRouter == "2")//commemoration 
                {
                   dataForAlmog.ledgerShortDescription = "דרישת תשלום - יד ושם";
                } 
                decimal discountInPrice = 0;
                decimal totalDiscountInPrice = 0;

                if (reservationItem.DiscountType.Id != "" && reservationItem.DiscountType.Id != "0" && reservationItem.TotalPrice != 0)
                {
                    discountInPrice = reservationItem.TotalPrice - reservationItem.TotalPriceAfterDiscount;
                }
                foreach (var day in reservationItem.ActivityDayList)
                {
                    var dayDate = DateTime.Parse(day.StartDateTime).ToString("dd-MM-yyyy");
                    Filter filter = new Filter
                    {
                        Field = "Id",
                        Type = "lookup",
                        Operator = "multiple",
                        Alias = "Product",
                    };
                    //create filter list with for one request insted of multiple requests...
                    foreach (var bid in day.BidItemList)
                    {
                        if (filter.Value != null)
                        {
                            filter.Value += ";" + bid.Product.Id;
                        }
                        else
                        {
                            filter.Value = bid.Product.Id;
                        }

                        line = new LineForAlmog(
                                bid.UnitPrice.ToString(),// צריך לחשב את המחיר
                                bid.TotalPriceAfterDiscount.ToString(),
                                bid.Product.Id,// איכלוס  של המוצר ממנו צריך להביא את הנתון
                                bid.Product.Value,
                                bid.Amount.ToString()
                            );
                        if (discountInPrice != 0)
                        {
                            var discountForLine = System.Math.Round((bid.TotalPrice / reservationItem.TotalPrice)* discountInPrice,2);
                            totalDiscountInPrice += discountForLine;
                            line.sumAfterDiscount = (bid.TotalPrice - discountForLine).ToString();
                        }
                        dataForAlmog.lines.Add(line);

                    }
                    string additionalFetchXml = @"<link-entity name='new_almog_interface' from='new_almog_interfaceid' to='new_almog_interface' alias='AlmogInterface' link-type='outer'>
                                                    <all-attributes />
                                                  </link-entity>";
                    //get all products for almogSubCode;
                    var productList = productService.GetAll(userId, "", filter, additionalFetchXml);

                    foreach (var lineForAlmog in dataForAlmog.lines)
                    {

                        Product product = productList.Find(p => p.Id == lineForAlmog.almogSubCode);
                        if (product != null)
                        {
                            if (!string.IsNullOrEmpty(product.RevenueSection))
                            {
                                lineForAlmog.almogSubCode = product.RevenueSection;
                            }
                            else
                            {
                                lineForAlmog.almogSubCode = reservationItem.BudgetFramework.RevenueSection;
                            }

                            if (!string.IsNullOrEmpty(product.Sku))
                            {
                                lineForAlmog.sku = product.Sku;
                            }
                            
                            //if (reservationItem.DivisionRouter == "1")//scholl
                            //{ 
                            //if there is a spesific RevenueSection use it else use RevenueSection of reservation.                               
                            //if (!string.IsNullOrEmpty(product.RevenueSection)) 
                            //{ 
                            //    lineForAlmog.almogSubCode = product.RevenueSection;
                            //}
                            //else
                            //{
                            //lineForAlmog.almogSubCode = reservationItem.BudgetFramework.RevenueSection;
                            //}                       
                            //}
                            //else //commemoration 
                            //{
                            //    lineForAlmog.almogSubCode = product.RevenueSection;
                            //    lineForAlmog.sku = product.Sku;
                            //}
                        }
                    }

                }
                //חישוב הפרשים לאחר עיגול  ההנחה ל2 מקומות אחרי הנקודה העשרונית
                if (dataForAlmog.lines.Count > 0)
                {
                    dataForAlmog.lines[0].sumAfterDiscount = (decimal.Parse(dataForAlmog.lines[0].sumAfterDiscount)-(discountInPrice - totalDiscountInPrice)).ToString();
                }

            }
            //כשיש מוסד משלם- נשלח אותו  כדי שיפתחו עליו חשבון, ובמוסד הקשור נשלח את הנתונים של המוסד בן
            // במצב שאין מוסד משלם, נשלח את הפרטים של המוסד עצמו
            var firstReservationItem = reservationItemList[0];//institution is the same for all reservations
            if (firstReservationItem.Institution.PayingInstitution != null)//יש מוסד משלם
            {
                dataForAlmog.account = firstReservationItem.Institution.PayingInstitution.InstitutionCompanyNumberOrSymbol;
                dataForAlmog.code = firstReservationItem.Institution.PayingInstitution.AlmogCode;
                dataForAlmog.relatedAccount = firstReservationItem.Institution.InstitutionCompanyNumberOrSymbol;
                dataForAlmog.relatedCode = firstReservationItem.Institution.AlmogCode;

            }
            else//אין מוסד משלם
            {
                dataForAlmog.account = firstReservationItem.Institution.InstitutionCompanyNumberOrSymbol;
                dataForAlmog.code = firstReservationItem.Institution.AlmogCode;
            }

            return dataForAlmog;
        }

        public Tuple<DataForAlmog, AlmogToCrm, DataForInvoicePdf> createInvoiceForReservation(string userId, ReservationForInvoiceData data)
        {
            ReservationForInvoice reservationForInvoice =  GetInvoiceDataFromReservation(userId, data.reservationGuidList , data.activityDayGuidList , data.bidItemGuidList);
            UserInfo currentUser = loginService.GetUserInfo();
            Trace.WriteLine($"createInvoiceForReservation => currentUser {currentUser}");
            List<TableRowDetails> allTableRowDetails = new List<TableRowDetails>();
            reservationForInvoice.DataForInvoicePdf.tables.ForEach(table => {
                table.tableRowDetails.ForEach(row =>
                {
                    allTableRowDetails.Add(row);
                });
            });
            DataForInvoicePdf pdfData = reservationForInvoice.DataForInvoicePdf;
            AlmogToCrm almogToCrm = new AlmogToCrm();
            almogToCrm.ReservationGuidList = data.reservationGuidList;
            almogToCrm.ActivityDayGuidList = data.activityDayGuidList;
            almogToCrm.BidItemGuidList = data.bidItemGuidList;
            almogToCrm.Comments = pdfData.comments;
            almogToCrm.Contact = pdfData.tables[0].contact;
            almogToCrm.Date = DateTime.Today.ToShortDateString();
            almogToCrm.Discount = pdfData.tables[0].discount;
            almogToCrm.DiscountTypeId = pdfData.tables[0].discountType;
            almogToCrm.TotalPrice = pdfData.InvoiceTotalPrice;
            almogToCrm.TotalPriceAfterDiscount = pdfData.InvoiceTotalPriceAfterDiscount;
            almogToCrm.InvoiceItemList = allTableRowDetails;
            almogToCrm.Division = pdfData.division;
            almogToCrm.DepartmentGuid = pdfData.departmentGuid;
            almogToCrm.Lang = pdfData.lang;
            almogToCrm.TransactionType = TransactionType.Credit;
            almogToCrm.InvoiceType = InvoiceType.Automatic;
            almogToCrm.InvoiceScope = InvoiceScope.Reservation;
            almogToCrm.InstitutionGuid = pdfData.institutionGuid;
            almogToCrm.UserId = userId;
            almogToCrm.InvoiceItemList = allTableRowDetails;
            almogToCrm.EventReservationGuidList = new List<string>();
            reservationForInvoice.DataForAlmog.UserAlmogId = currentUser.AlmogId;
            //almogToCrm.EventReservationCode = pdfData.eventReservationCode;
            return Tuple.Create(reservationForInvoice.DataForAlmog, almogToCrm , pdfData);
        }

        public string createInvoiceForReservationAuto(string userId, ReservationForInvoiceData data)
        {
            Trace.WriteLine($" start createInvoiceForReservationAuto");
            UpdateCrmItem updateCrmItem = new UpdateCrmItem
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<Reservation, ReservationCRM>("PreCreateInvoice"),
                Value = DateTime.Now.ToString(),
                Type = "datetime"
            };
            foreach (var reservationGuid in data.reservationGuidList)
            {
                reservationsRepository.UpdateField(reservationGuid, updateCrmItem, userId);
            }

            //get the data for the invoice:
            Tuple<DataForAlmog,AlmogToCrm,DataForInvoicePdf> reservationForInovice = createInvoiceForReservation(userId, data);
            Trace.WriteLine($" createInvoiceForReservation => reservationForInovice {reservationForInovice}");

            //create invoice in almog
            string inoviceFromYVAlmogResponse = CreateAlmogInvoice(reservationForInovice.Item1, reservationForInovice.Item2);
            AlmogResultObject inoviceFromYVAlmog = JsonConvert.DeserializeObject<AlmogResultObject>(inoviceFromYVAlmogResponse);
            Trace.WriteLine($" CreateAlmogInvoice => inoviceFromYVAlmogString {inoviceFromYVAlmogResponse}");

            if (inoviceFromYVAlmog.Status == "OK" )
            {
                //update payment status - after the account created in almog
                reservationsService.UpdatePaymentStatus(userId, data.bidItemGuidList, data.reservationGuidList);

                //create invoice PDF
                UserInfo currentUser = loginService.GetUserInfo();
                reservationForInovice.Item3.invoiceNum = inoviceFromYVAlmog.CreatedObject.Code;
                reservationForInovice.Item3.createdBy = currentUser.FirstName + ' ' + currentUser.LastName;             
                Attachment fileItem =CreateInvicePdf(reservationForInovice.Item3, userId);
                Trace.WriteLine($"CreateInvicePdf => fileItem {fileItem}");

                if(fileItem != null)
                {
                    //save attachment in CRM
                    List<Attachment> attachments = new List<Attachment>();
                    fileItem.RelatedEntityName = new Objects().Reservation;
                    fileItem.FileType = new IdValuePair("2", "חשבון");
                    fileItem.Comments = "חשבון למוסד";
                    attachments.Add(fileItem);
                    var result = attachmentsService.SaveAttachments(data.reservationGuidList[0],attachments , userId);
                    if(result.Count < 0)
                    {
                        //Send mail
                        try
                        {
                            sendMailSoapClient sendMail = new sendMailSoapClient();
                            sendMail.sendMailProcces("devora.weyl@yadvashem.org.il", "shira.deutsch@yadvashem.org.il", "tahel.support@yadvashem.org.il",
                                    "no-bug@yadvashem.org.il", "bug in createInvoiceForReservationAuto",
                                    "Guid: " + data.reservationGuidList
                                    + " UserId: " + userId + "Exe: CreateInvicePdf -> fileItem is null", "ltr");
                        }
                        catch (Exception ex)
                        {
                            Trace.Write("send mail from no-bug faild, ex:" + ex);
                        }
                        return "שגיאה בשמירת הצרופה בCRM";
                    }
                }
                else
                {
                    //Send mail
                    try
                    {
                        sendMailSoapClient sendMail = new sendMailSoapClient();
                        sendMail.sendMailProcces("devora.weyl@yadvashem.org.il", "shira.deutsch@yadvashem.org.il", "tahel.support@yadvashem.org.il",
                                "no-bug@yadvashem.org.il", "bug in createInvoiceForReservationAuto",
                                "Guid: " + data.reservationGuidList
                                + " UserId: " + userId + "Exe: שגיאה בשמירת הצרופה בCRM", "ltr");
                    }
                    catch (Exception ex)
                    {
                        Trace.Write("send mail from no-bug faild, ex:" + ex);
                    }
                }
                return fileItem.FullPath;
            }
            else
            {
                return "שגיאה בהתחברות לאלמוג";
            }
        }
        private string CreateAlmogInvoice(DataForAlmog dataForAlmog , AlmogToCrm almogToCrm)
        {
            Trace.WriteLine($"start CreateAlmogInvoice");
            string json = JsonConvert.SerializeObject(new {dataForAlmog, almogToCrm});
            string environment = ConfigurationManager.AppSettings["environment"].ToString();
            string YVAlmogUrl = ConfigurationManager.AppSettings[$"baseUrlYVAlmog_{environment}"].ToString();
            try
            {
                string jsonResponse = reportsService.CreateRequest($"{YVAlmogUrl}/Invoices", "POST", json);
                Trace.WriteLine($"end CreateAlmogInvoice => jsonResponse {jsonResponse}");
                return jsonResponse;
            }
            catch
            {
                throw new Exception("שגיאה ביצירת החשבון באלמוג");
            }           
        }
        private Attachment CreateInvicePdf(DataForInvoicePdf data , string userId)
        {
            string json = JsonConvert.SerializeObject(data);
            var environment = ConfigurationManager.AppSettings["environment"].ToString();
            string pdfCreatorUrl = ConfigurationManager.AppSettings[$"baseUrlPdfCreator_{environment}"].ToString();
            string jsonResponse = reportsService.CreateRequest($"{pdfCreatorUrl}/Invoice?userId={userId}", "POST", json);
            try
            {
                Attachment fileItem = JsonConvert.DeserializeObject<Attachment>(jsonResponse);
                fileItem.Extention = "docx";
                return fileItem;
            }
            catch
            {
                throw new Exception("שגיאה ביצירת הPDF");
            }
        }
    }

}
