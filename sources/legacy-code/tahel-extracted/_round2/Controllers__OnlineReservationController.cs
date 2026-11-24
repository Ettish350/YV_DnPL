using Data.Models;
using Data.Models.Data.Models;
using Data8.PowerPlatform.Dataverse.Client.Wsdl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.VisualBasic;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Service.Services;
using Service.Services.Interfaces;
using System;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Data;

namespace CrmConnection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnlineReservationController : ControllerBase
    {
        private readonly IOnlineReservationService _onlineReservationService;
        private readonly IPaymentService _paymentService;

        private readonly IConfiguration _configuration;
        public OnlineReservationController(IOnlineReservationService onlineReservationService, IPaymentService paymentService, IConfiguration configuration)
        {
            _onlineReservationService = onlineReservationService;
            _paymentService = paymentService;
            _configuration = configuration;
        }

        [Route("GetPasswordTravelAgentsToEmail")]
        [HttpGet]
        public async Task<CrmResultItem> GetPasswordTravelAgentsToEmail([Required] string instituitionSymbol, [Required] string language)
        {
            var res = new CrmResultItem();
            try
            {

                res = await _onlineReservationService.GetPasswordTravelAgentsToEmail(instituitionSymbol, language);
                return res;
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
                return res;

            }
        }


        [Route("GetReservationsActivitiesPerDayExtends")]
        [HttpGet]
        public ActionResult GetReservationsActivitiesPerDayExtends([Required] string tourId, [Required] string tourDate)
        {
            try
            {

                return Ok( _onlineReservationService.GetReservationsActivitiesPerDayExtends(tourId, tourDate));
                
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        [Route("GetDateArrivalTime")]
        [HttpGet]
        public ActionResult getDateTimeSlots(string date, string tourTypeId, string tourCode, decimal activityDurationHours)
        {
            try
            {
                return Ok(_onlineReservationService.getDateTimeSlots(date, tourTypeId, tourCode, activityDurationHours));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        } 
        [Route("CreateActivitiesDaysAhead")]
        [HttpGet]
        public ActionResult createActivitiesDaysAhead([FromQuery] int numOfMonth)
        {
           // JObject jObject = JObject.Parse(data.ToString());
           // int numOfMonth = Convert.ToInt32(jObject["numOfMonth"]);
            //var res = new re();
            try
            {
                return Ok(_onlineReservationService.createActivitiesDaysAhead(numOfMonth));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        [Route("GetPromotionTours")]
        [HttpGet]
        public ActionResult GetPromotionTours(string lang)
        {
            try
            {
                return Ok(_onlineReservationService.GetPromotionTours(lang));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }
        [Route("SaveResourceSlotTime")]
        [HttpPost]
        public ActionResult SaveResourceSlotTime([FromBody] BasicTourDetails tourDetails)
        {
            var res = new CrmResultItem();
            try
            {
                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.SaveResourceSlotTime(tourDetails);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        [Route("GetObjectToEditCancel")]
        [HttpGet]
        public ActionResult<CrmResultItem> GetObjectToEditCancel([FromQuery] string objectNumber, [FromQuery] string identity, [FromQuery] bool isEmailIdentity, [FromQuery] string language, [FromQuery] string? activityDate)
        {
            var res = new CrmResultItem();
            //an exsamlpe change
            try
            {
                var details = new ObjectToEditCancelDetails
                {
                    ObjectNumber = objectNumber,
                    Identity = identity,
                    IsEmailIdentity = isEmailIdentity,
                    Language = language,
                    ActivityDate = ""
                };
                bool isForEdit = !string.IsNullOrWhiteSpace(activityDate);
                if (isForEdit)
                {
                    details.ActivityDate = activityDate;
                }
                res = _onlineReservationService.GetObjectToEditCancel(details, isForEdit);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }

            return Ok(res);
        }



        [Route("CancelObject")]
        [HttpPost]
        public async Task<ActionResult> CancelObject([FromBody] ObjectToEditCancelDetails reservationDetails)
        {
            var res = new CrmResultItem();

            try
            {
                res = await _onlineReservationService.CancelObject(reservationDetails);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
     

        [Route("GetAllToursBasicDetails")]
        [HttpGet]
        public ActionResult GetAllToursBasicDetails(InstituitionType instituitionType, string lang)
        {
            try
            {
                return Ok(_onlineReservationService.GetAllToursBasicDetails(instituitionType, lang));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [Route("GetTourBasicDetails")]
        [HttpGet]
        public ActionResult GetTourBasicDetails(string tourCode, string lang)
        {
            try
            {
                return Ok(_onlineReservationService.GetTourBasicDetails(tourCode,lang));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [Route("GetSomeTourBasicDetails")]
        [HttpPost]
        public ActionResult GetSomeTourBasicDetails([FromBody]List<string> toursCode, string lang)
        {
            try
            {
                return Ok(_onlineReservationService.GetSomeTourBasicDetails(toursCode, lang));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }


        [Route("GetAllTours")]
        [HttpGet]
        public ActionResult GetAllTours()
        {
            try
            {
                return Ok(_onlineReservationService.GetAllTours());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }



        [Route("SetEventRegistartionToInactive")]
        [HttpPut]
        public ActionResult SetEventRegistartionToInactive(string eventRegistarionGuid, bool isPackage)
        {
            try
            {
                return Ok(_onlineReservationService.SetEventRegistartionStatus(eventRegistarionGuid, isPackage, 1));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }



        [Route("GetAdditionalTourDetails")]
        [HttpGet]
        public ActionResult GetAdditionalTourDetails(string tourCode, string lang)
        {
            try
            {
                return Ok(_onlineReservationService.GetAdditionalTourDetails(tourCode, lang));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }


        [Route("GetAdditionalProductList")]
        [HttpPost]
        public ActionResult GetAdditionalProductList([FromBody] FilterForProduct filterForProduct)
        {
            List<Product> productList = new List<Product>();
            try
            {
                filterForProduct.tourDate = DateTime.ParseExact(filterForProduct.stringTourDate, "dd/MM/yyyy", null);
                return Ok(_onlineReservationService.GetAdditionalProductList(filterForProduct));

            }
            catch (Exception ex)
            {
                Log.Information("error GetAdditionalProductList");
                Log.Information(ex.Message);
                //return Problem(ex.Message);
                return Ok(productList);
              

            }
        }


        [Route("GetRequiredProductsAvailablePerDate")]
        [HttpPost]
        public ActionResult GetRequiredProductsAvailablePerDate([FromBody] ProductRequestPerDate productRequestPerDate)
        {
            try
            {

                productRequestPerDate.tourDate = DateTime.ParseExact(productRequestPerDate.StringTourDate, "dd/MM/yyyy", null);
             
                return Ok(_onlineReservationService.GetRequiredProductsAvailablePerDate(productRequestPerDate));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }
        [Route("GetInstituitionList")]
        [HttpGet]
        public ActionResult GetInstituitionList([Required] string instituitionSymbol, string? password)
        {
            try
            {
                //, string? password
                //bool isWithPassword = password == "";
                //, isWithPassword
                return Ok(_onlineReservationService.GetInstituitionList(instituitionSymbol, password));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [Route("GetArrivalTimes")]
        [HttpGet]
        public ActionResult GetArrivalTimes(int month, int year, decimal activityDurationHours, string tourTypeId)
        {
            Log.Information("GetArrivalTimes controller start at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));

            try
            {

                Log.Information("Calling GetArrivalTimes at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                var res= Ok(_onlineReservationService.GetArrivalTimes(month, year, activityDurationHours, tourTypeId));
                Log.Information("finish GetArrivalTimes at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                return res;

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }
      
        [Route("GetReservationDetailsConfirmation")]
        [HttpGet]
        public ActionResult ReservationDetailsConfirmation()
        {
            try
            {
                string reservationId = Request.Query["reservationId"].ToString();
                string type = Request.Query["type"].ToString();
                return Ok(_onlineReservationService.ReservationDetailsConfirmation(reservationId, type));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [HttpPost]
        public async  Task<ActionResult> Post([FromBody] OnlineReservation reservation)
        {

            var res = new CrmResultItem();

            try
            {
                res.IsOk = true;

                res.ResponseObject = await  _onlineReservationService.CreateReservation(reservation);
                if (reservation.TotalPriceAfterDiscount == 0)//הזמנה ללא תשלום
                {
                    try
                    {

                   
                    Log.Information("reservation.TotalPrice 0 ");
                    var guid = (res.ResponseObject as ReservationNumberGuid).ReservationGuid;
                        var a = await _onlineReservationService.confirmationReservationWithoutPayment(guid, null, 0);
                    }
                    catch (Exception ex)
                    {
                        Log.Information("error creating pdf and sending mail");
                        Log.Information(ex.Message);
                        res.IsOk = false;
                        res.Message = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
        
        [Route("sendEmailAndCreateAttachmentForBugsReservation")]
        [HttpGet]
        public async Task<bool> sendEmailAndCreateAttachmentForBugsReservation()
        {
            var res=await _onlineReservationService.sendEmailAndCreateAttachmentForBugsReservation();
            return res;
        }

        [Route("sendEmailAndCreateAttachmentForBugsEventRegistration")]
        [HttpGet]
        public async Task<bool> sendEmailAndCreateAttachmentForBugsEventRegistration()
        {
            var res = await _onlineReservationService.sendEmailAndCreateAttachmentForBugsEventRegistration();
            return res;
        }

        [Route("GetCurrentDate")]
        [HttpGet]
        public ActionResult GetCurrentDate()
        {
            var res = new CrmResultItem();
            try
            {
                // מחזיר את התאריך הנוכחי בפורמט UTC עם פורמט ISO 8601
                var currentDate = DateTime.UtcNow.ToString("o"); // פורמט "o" הוא ISO 8601

                res.IsOk = true;
                res.ResponseObject = currentDate;
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);
        }

        //[Route("test-pdf")]
        //[HttpGet]
        //public ActionResult test()
        //{
        //    try
        //    {
        //        var reservation=new OnlineReservation();
        //        return Ok(_onlineReservationService.CreatePdfAttachment(reservation));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message);

        //    }
        //}


        [Route("confirmationReservationWithoutPayment")]
        [HttpPost]
        public async Task<ActionResult> confirmationReservationWithoutPayment([FromBody] dynamic body)
        {
            // string reservationGuid = body.ToString(); // קבלת הערך
            //var reservationGuid = body.GetProperty("reservationGuid").GetString();
            //var couponGuid = body.GetProperty("couponGuid")?.GetString();
            //var totalPriceAfterDiscount = body.GetProperty("totalPriceAfterDiscount")?.GetDecimal();
            var reservationGuid = body.GetProperty("reservationGuid").GetString();

            var couponGuid = body.TryGetProperty("couponGuid", out JsonElement couponGuidProp) && couponGuidProp.ValueKind != JsonValueKind.Undefined
                ? couponGuidProp.GetString()
                : null;

            var totalPriceAfterDiscount = body.TryGetProperty("totalPriceAfterDiscount", out JsonElement totalPriceProp) && totalPriceProp.ValueKind != JsonValueKind.Undefined
                ? totalPriceProp.GetDecimal()
                : (decimal?)null;




            var res = new CrmResultItem();

            try
            {
                res.IsOk = true;

                res.ResponseObject = await _onlineReservationService.confirmationReservationWithoutPayment(reservationGuid, couponGuid, totalPriceAfterDiscount);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }


        [Route("confirmEventRegistrationWithoutPay")]
        [HttpPost]
        public async Task<ActionResult> confirmEventRegistrationWithoutPay([FromBody] dynamic body)
        {
            //string reservationGuid = body.ToString(); // קבלת הערך
            //var eventRegistrationGuid = body.GetProperty("eventRegistrationGuid").GetString();
            // var couponGuid = body.GetProperty("couponGuid")?.GetString();
            //var totalPriceAfterDiscount = body.GetProperty("totalPriceAfterDiscount")?.GetDecimal();

            var eventRegistrationGuid = body.GetProperty("eventRegistrationGuid").GetString();

            var couponGuid = body.TryGetProperty("couponGuid", out JsonElement couponGuidProp) && couponGuidProp.ValueKind != JsonValueKind.Undefined
                ? couponGuidProp.GetString()
                : null;

            var totalPriceAfterDiscount = body.TryGetProperty("totalPriceAfterDiscount", out JsonElement totalPriceProp) && totalPriceProp.ValueKind != JsonValueKind.Undefined
                ? totalPriceProp.GetDecimal()
                : (decimal?)null;
            Log.Information($"in  ConfirmationWithPayment, read query string: ");



            // Now call the service with the correct types
          

            var res = new CrmResultItem();

            try
            {
                res.IsOk = true;

                res.ResponseObject = await _onlineReservationService.confirmEventRegistrationWithoutPay(eventRegistrationGuid, couponGuid, totalPriceAfterDiscount);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        //[HttpPost]
        //public ActionResult CreatePdfAttachment([FromBody] OnlineReservation reservation)
        //{
        //    var res = new CrmResultItem();

        //    try
        //    {
        //        res.IsOk = true;
        //        res.ResponseObject = _onlineReservationService.CreatePdfAttachment(reservation);
        //    }
        //    catch (Exception ex)
        //    {
        //        res.IsOk = false;
        //        res.Message = ex.Message;
        //    }
        //    return Ok(res);

        //}

        [Route("GetCountries")]
        [HttpGet]
        public ActionResult GetCountries()
        {
            try
            {
                return Ok(_onlineReservationService.GetCountries());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }
        [Route("GetCloseDates")]
        [HttpGet]
        public ActionResult GetCloseDates(string tourCode, int month, int year)
        {
            try
            {
                return Ok(_onlineReservationService.GetCloseDates(tourCode,year,month));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [Route("GetEventDates")]
        [HttpGet]
        public ActionResult GetEventDates(string tourCode, int month, int year)
        {
            try
            {
                return Ok(_onlineReservationService.GetCloseDates(tourCode, year, month));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }


        [Route("GetAvailableTime")]
        [HttpGet]
        public async Task<ActionResult> GetAvailableTime(string tourCode, string tourDate, bool isEducationalGroup)
        //, int numberOfVisitors
        {
            Log.Information("GetAvailableTime controller start at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
            try
            {

                Log.Information("Calling GetTourByCode at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                Entity tour = _onlineReservationService.GetTourByCode(tourCode);

                Log.Information("Calling GetAvailableTimes at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                var list = await _onlineReservationService.GetAvailableTimes(tourCode, tourDate, isEducationalGroup, tour);

                Log.Information("Calling GetAvailableTimesWithHourRegulation at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                var regulated = await _onlineReservationService.GetAvailableTimesWithHourGuidRegulation(list, tour, tourDate);
                var guideAmountRemainingForTour=regulated.guideAmountRemainingForTour;
                return Ok(regulated);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetAvailableTime error at {time}", DateTime.Now.ToString("HH:mm:ss.fff"));
                return Problem(ex.Message);

            }
        }
        [Route("ticket-test")]
        [HttpGet]
        public async Task<ActionResult> ticketTest(string reservationGuid,bool isEvent)
        {
            try
            {

                if(isEvent)
                {
                    return Ok(_onlineReservationService.createEventRegistrationTicket(new Guid(reservationGuid)));
                }
                else
                {
                    return Ok(_onlineReservationService.createReservationTicket(new Guid(reservationGuid)));

                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [Route("GetRightDiscountList")]
        [HttpGet]
        public ActionResult GetRightDiscountList()
        {
            try
            {
                return Ok(_onlineReservationService.GetRightDiscountList());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [Route("GetPaymentUrl")]
        [HttpPost]
        public ActionResult GetPaymentUrl([FromBody] object data)
        {
            //temp
            //data = "{\"total\":\"2\",\"paramX\":\""+DateTime.Now.ToString()+"\",\"lang\":\"he\",\"rsvGuid\":\"22222\"}";
            //end temp
            var res = new CrmResultItem();
            try
            {
                JObject jObject = JObject.Parse(data.ToString());
                double total = Convert.ToDouble(jObject["total"]);
                string paramX = jObject["paramX"].ToString();
                string lang = jObject["lang"].ToString();
                string guid = jObject["guid"].ToString();
                string type = jObject["type"].ToString();
                string tourCode = jObject["tourCode"].ToString();
                string couponGuid = jObject["couponGuid"]?.ToString();

                string currency = "1";//NIS
                res.IsOk = true;
                var response = _paymentService.GetPaymentUrl(total, currency, paramX, lang, guid,type,tourCode, couponGuid).Result;
                res.ResponseObject = response.URL;
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        [Route("ConfirmationWithPayment")]
        [HttpPost]
        public async Task<ActionResult> ConfirmationWithPayment([FromBody] dynamic data)
        {
            Log.Information($"in  ConfirmationWithPayment, read query string: ");
            string id = Request.Query["id"].ToString();
            string type = Request.Query["type"].ToString();

            //var couponGuid = data.TryGetProperty("couponGuid", out JsonElement couponGuidProp) && couponGuidProp.ValueKind != JsonValueKind.Undefined
            //? couponGuidProp.GetString()
            //: null;

            //var totalPrice = data.TryGetProperty("totalPrice", out JsonElement totalPriceProp) && totalPriceProp.ValueKind != JsonValueKind.Undefined
            //    ? totalPriceProp.GetDecimal()
            //    : (decimal?)null;
            string couponGuid = Request.Query["couponGuid"].ToString();
            decimal? totalPrice = decimal.TryParse(Request.Query["total"].ToString(), out var parsedTotal) ? parsedTotal : (decimal?)null;

            //STRING COUPON GUID, TOTAL PRICE
            Log.Information($"id {id} ");
            Log.Information($"type {type} ");
            Log.Information($"couponGuid {couponGuid} ");
            Log.Information($"totalPrice {totalPrice} ");
            

            if (type == "reservation")
                {
                    try
                    {
                    Log.Information($"confirmationReservationWithPayment ");
                    string jsonString = Convert.ToString(data);
                    ApiResponsePayment apiResponsePayment = JsonConvert.DeserializeObject<ApiResponsePayment>(jsonString);
                        var sucsess = await _onlineReservationService.confirmationReservationWithPayment(apiResponsePayment.ResultData, id, couponGuid, totalPrice);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"confirmationReservationWithPayment Exception");
                        return Problem(ex.Message);

                    }
                }
                if (type == "event")
                {
                    try
                    {
                    Log.Information($"ConfirmEventRegistrationWithPayment");
                    string jsonString = Convert.ToString(data);
                    ApiResponsePayment apiResponsePayment = JsonConvert.DeserializeObject<ApiResponsePayment>(jsonString);
                    Log.Information(jsonString);
                        var sucsess = await _onlineReservationService.ConfirmEventRegistrationWithPayment(apiResponsePayment.ResultData,id, couponGuid, totalPrice);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        Log.Information($"ConfirmEventRegistrationWithPayment Exception");

                        return Problem(ex.Message);
                    }
                }
            Log.Information($"ConfirmationWithPayment error");
            return Problem("ConfirmationWithPayment error");

        }
        [Route("updateEditedObject")]
        [HttpPost]
        public async Task<ActionResult> updateEditedObject([FromBody] OnlineReservation reservation)
        {
            var res = new CrmResultItem();

            try
            {
                res = await _onlineReservationService.updateEditedObject(reservation);
                if (res.IsOk)
                {
                    //try send mail
                    try
                    {



                        Log.Error($"Error sending confirmation mail in updateEditedObject: {reservation.ReservationName}");
                        res.IsOk = await _onlineReservationService.confirmUpdateReservation(reservation.Guid);


                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error sending confirmation mail in updateEditedObject: {ex.Message}");
                        res.IsOk = false;
                        res.ErrorMessageStatus = ErrorMessageStatus.UNKNOWN;
                        res.Message = ex.Message;
                    }
                }
            }

            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);
        }

        //[Route("confirmationReservationWithPayment")]
        //[HttpPost]
        //public async Task<ActionResult> confirmationReservationWithPayment([FromBody] dynamic data,string id)
        //{
        //    try
        //    {
        //        Log.Information($"confirmationReservationWithPayment ");
        //        var contentString = data.ToString();
        //        Log.Information(contentString);
        //        var sucsess = await _onlineReservationService.confirmationReservationWithPayment(data,id);
        //        return Ok(data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message);

        //    }
        //}

        //[Route("ConfirmEventRegistrationWithPayment")]
        //[HttpPost]
        //public async Task<ActionResult> ConfirmEventRegistrationWithPayment([FromBody] dynamic data)
        //{
        //    try
        //    {
        //        Log.Information($"ConfirmEventRegistrationWithPayment");
        //        var contentString = data.ToString();
        //        Log.Information(contentString);
        //        //var sucsess = await _onlineReservationService.ConfirmEventRegistrationWithPayment(contentString);
        //        return Ok(data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Problem(ex.Message);
        //    }
        //}

        [Route("CreateEventRegistration")]
        [HttpPost]
        public async Task<ActionResult> CreateEventRegistration([FromBody] OnlineReservation eventRegistration, [FromQuery] bool isForUpdate = false)
        {
            var res = new CrmResultItem();
            Log.Error("CreateEventRegistration start", eventRegistration, "isForUpdate", isForUpdate);
            try
            {
                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.CreateEventRegistration(eventRegistration, isForUpdate,null);
                if (eventRegistration.TotalPriceAfterDiscount == 0)//הזמנה ללא תשלום
                {
                    try
                    {


                        Log.Information("reservation.TotalPrice 0 ");
                        var guid = (res.ResponseObject as ReservationNumberGuid).ReservationGuid;
                        var a = await _onlineReservationService.confirmationEventRegistration(guid);
                    }
                    catch (Exception ex)
                    {
                        Log.Information("error creating pdf and sending mail");
                        Log.Information(ex.Message);
                        res.IsOk = false;
                        res.Message = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        [Route("GenerateQRCode")]
        [HttpPost]
        public ActionResult GenerateQRCode([FromBody] QRCodeItem qRcodeDetails)
        {
            var res = new CrmResultItem();
            Log.Error("GenerateQRCode qRcodeDetails: ", @qRcodeDetails);
            try
            {
                res.IsOk = true;
                //res.ResponseObject = _onlineReservationService.GenerateQRCode(qRcodeDetails);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }


        [Route("GetArrivalTimeTourPackage")]
        [HttpGet]
        public ActionResult GetArrivalTimeTourPackage( string packageCode, string arrivalTime,string date,string lang,bool isEducationalGroup)
        {
            try
            {
                return Ok(_onlineReservationService.GetGetArrivalTimeTourPackage(packageCode,arrivalTime,date,lang,isEducationalGroup));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Route("CheckIfThereIsPlaceInMuseum")]
        [HttpPost]
        public ActionResult CheckIfThereIsPlaceInMuseum([FromBody] Time startTime,string reservationDate,string tourCode)
        {
            try
            {

                return Ok(_onlineReservationService.CheckIfThereIsPlaceInMuseum(reservationDate, startTime, tourCode));

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }

        }
       
        [Route("LoginQrScanner")]
        [HttpPost]
        public ActionResult LoginQrScanner([FromBody] QrScannerUser user)
        {
            var res = new CrmResultItem();
            Log.Information("LoginQrScanner user: ", user.Username);
            try
            {
                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.LoginQrScanner(user);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        [Route("GetQrScannerUserList")]
        [HttpGet]
        public ActionResult GetQrScannerUserList()
        {
            var res = new CrmResultItem();
            Log.Information("GetQrScannerUserList");
            try
            {
                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.GetQrScannerUserList();
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
        [Route("GetDetailsByQRCode")]
        [HttpPost]
        public ActionResult GetDetailsByQRCode([FromBody] List<QRCodeItem> qRCodeItemList)
        {
            var res = new CrmResultItem();
            Log.Error("GetDetailsByQRCode qRCodeItem: ", @qRCodeItemList);
            try
            {

                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.GetDetailsByQRCode(qRCodeItemList);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);
        }

        [Route("UpdateActualNumOfVisitors")]
        [HttpPost]
        public ActionResult UpdateActualNumOfVisitors(string guid, int numOfParticipants, bool isEventRegistration)
        {
            var res = new CrmResultItem();
            Log.Error($"UpdateActualNumOfVisitors guid:{guid}, numOfParticipants:{numOfParticipants}, isEventRegistration:{isEventRegistration}");
            try
            {
                res.IsOk = true;
                _onlineReservationService.UpdateActualNumOfVisitors(guid, numOfParticipants, isEventRegistration);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);
        }
        [Route("GetTourTypesByStartDateAndParticipants")]
        [HttpGet]
        public ActionResult GetTourTypesByStartDateAndParticipants(string? startDate, string? endDate ,int participentNum, InstituitionType instituitionType, bool isForPlayAndShowsOnly=false)
        {
            try
            {
                TourCategory[]? tourCategory = null;

                return Ok(_onlineReservationService.GetTourTypesByStartDateAndParticipants(startDate, endDate, participentNum,instituitionType, isForPlayAndShowsOnly, tourCategory));

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }

        }

     



        [Route("GetTourBasicAndAdditionalDetails")]
        [HttpGet]
        public ActionResult GetTourBasicAndAdditionalDetails(string tourCode, string lang)
        {
            try
            {

                return Ok(_onlineReservationService.GetTourBasicAndAdditionalDetails(tourCode, lang));

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }

        }

        [Route("GetEventTodayListForQrScsanner")]//For login page
        [HttpGet]
        public ActionResult GetEventTodayListForQrScsanner()
        {
            var res = new CrmResultItem();
            Log.Error($"GetEventTodayListForQrScsanner");
            try
            {

                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.GetEventTodayListForQrScsanner();
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
    
        [Route("GetReservationTodayListForQrScsanner")]// For search reservation on scanner page
        [HttpGet]
        public ActionResult GetReservationTodayListForQrScsanner(string? eventCode = "")
        {
            var res = new CrmResultItem();
            Log.Error($"GetReservationTodayListForQrScsanner");
            try
            {

                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.GetReservationTodayListForQrScsanner(string.IsNullOrEmpty(eventCode) ? "" : eventCode);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        [Route("GetNewVisitorsCenterDatesAndTimes")]// For search reservation on scanner page
        [HttpGet]
        public ActionResult GetNewVisitorsCenterDatesAndTimes(int year, int month)
        {
            var res = new CrmResultItem();
            try
            {

                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.GetNewVisitorsCenterDatesAndTimes(year,month);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
        [Route("GetAllShowsAndPlaysBasicDetails")]
        [HttpGet]
        //[FromBody] List<string> dates
        public ActionResult GetAllShowsAndPlaysBasicDetails(string lang)
        {
            var res = new CrmResultItem();
            try
            {
                return Ok(_onlineReservationService.GetAllShowsAndPlaysBasicDetails(lang));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }

        [Route("GetInstitutionTypes")]
        [HttpGet]
        public ActionResult<IdValue> GetInstitutionTypes()
        {
            try
            {
                return Ok(_onlineReservationService.GetInstitutionTypes());
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }
        [Route("GetCitiesList")]
        [HttpGet]
        public ActionResult<IdValue> GetCitiesList()
        {
            try
            {
                var cities = _onlineReservationService.GetCitiesList();
                return Ok(cities);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        [Route("RegisterInstitution")]
        [HttpPost]
        public async Task<ActionResult> RegisterInstitution([FromBody] InstitutionRegistration institution)
        {
            var res = new CrmResultItem();
            Log.Information("RegisterInstitution start", institution);

            try
            {
                res.IsOk = true;
                res.ResponseObject = await _onlineReservationService.InstitutionRegistration(institution);

                

                return Ok(res);
            }
            catch (Exception ex)
            {
                Log.Error("RegisterInstitution error", ex);
                res.IsOk = false;
                res.Message = ex.Message;
                return Ok(res);
            }
        }
        [Route("GetActivityDaySummary")]
        [HttpGet]
        public ActionResult GetActivityDaySummary(Guid activityDayId)
        {
            var res = new CrmResultItem();

            try
            {
                res.IsOk = true;
                res.ResponseObject = _onlineReservationService.GetActivityDaySummary(activityDayId);
                return Ok(res);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
                return Ok(res);
            }
        }








        [Route("testGetFromSQL")]
        [HttpGet]
        public async Task<ActionResult> TestGetFromSQL(string lang, InstituitionType instituitionType)
        {

            try
            {
                var dt =  _onlineReservationService.Commit(lang, instituitionType);
                //if (dt.Rows.Count > 1)
                //{
                //    var toursList = new List<OnlineTourType>();
                //    foreach (DataRow item in dt.Rows)
                //    {
                //        OnlineTourType tourType = _onlineReservationService.ConvertDataRowToTourType(item);
                //        toursList.Add(tourType);
                //    }
                //    return Ok(toursList);
                //}

                //var result = _onlineReservationService.ConvertDataTableToList(dt);
                return Ok(dt);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


    }
}

