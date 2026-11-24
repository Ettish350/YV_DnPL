using Services.Services.Interfaces;
using Data.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using System.Web.Http.Cors;
using Data.Modals.CRMModals;
using System.Diagnostics;
using Data.Repositories.Interfaces;
using System.Web.UI.WebControls.Expressions;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using Services.Services;

namespace YVReservationsAPI.Controllers
{
    public class ReservationsController : ApiController
    {
        private readonly IReservationsService reservationService;
        private readonly IInvoiceService invoiceService;
        private readonly IResourceService resourceService;
        private readonly IHelperService helperService;
        private readonly IScheduleDateService scheduleDateService;
        private readonly IAttachmentsService attachmentsService;
        private readonly IServiceProviderService serviceProviderService;
        private readonly IActivityDayService activityDayService;
        private readonly IGoogleApiService googleApiService;
       ICRMConnection CRMConnection;

        public ReservationsController()
        { }
        public ReservationsController(IReservationsService reservationService,
                                      IHelperService helperService,
                                      IResourceService resourceService,
                                      IInvoiceService invoiceService,
                                      IScheduleDateService scheduleDateService,
                                      IAttachmentsService attachmentsService,
                                      IServiceProviderService serviceProviderService,
                                      IActivityDayService activityDayService,
                                      IGoogleApiService googleApiService,
                                      ICRMConnection CRMConnection
                                      )
        {
            this.reservationService = reservationService;
            this.helperService = helperService;
            this.resourceService = resourceService;
            this.invoiceService = invoiceService;
            this.scheduleDateService = scheduleDateService;
            this.attachmentsService = attachmentsService;
            this.serviceProviderService = serviceProviderService;
            this.activityDayService = activityDayService;
            this.googleApiService = googleApiService;
            this.CRMConnection = CRMConnection;
        }

        [Route("Reservations")]
        // Post: api/Reservations
        public IHttpActionResult Post([FromBody] DataFromClient data, [FromUri] string userId)

        {
            //Trace.WriteLine("start get reservation list");
            try
            {
                //List<Reservation> list = reservationService.Get(userId, data.Paging, data.Sort, data.FilterList);
                Tuple<int, List<Reservation>> list = reservationService.GetFetchXml(userId, false, data.Paging, data.Sort, data.FilterList); //data.Paging
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(list)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: Get reservation List, Exception: " + ex.Message);
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/Count/")]
        [HttpPost]
        public IHttpActionResult Count([FromUri] string userId, [FromBody] List<Filter> filterList)
        {
            //Trace.WriteLine("start get reservation list count");
            try
            {
                var count = reservationService.Count(userId, filterList);
                //var count = 4;
                //Trace.WriteLine("before return reservation list count: " + count);
                return Ok(count);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: Get reservation list count, exception: " + ex.Message);
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetList")]
        [HttpPost]
        public IHttpActionResult GetList([FromUri] string userId, [FromBody] DataFromClient data)
        {
            try
            {
                var list = reservationService.GetList(userId, data.Paging, data.FilterList);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(list)));
            }
            catch (Exception ex)
            {
                //Trace.TraceError("Institutions/GetList - " + ex.Message);
                return InternalServerError(ex);
            }
        }

        // GET: api/Reservations/5
        [Route("Reservations/{id}")]
        public IHttpActionResult Get([FromUri] string userId, string id)
    {
            
            //Trace.WriteLine("start get reservation by id: " + id);
            try
            {
                var reservationItem = reservationService.GetFetchXml(userId, id);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(reservationItem)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: get reservation by id: {id}, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        
        [Route("Reservations/GetBidList/{id}")]
        [HttpPost]
        public IHttpActionResult GetBidList([FromUri] string userId, string id)
        {
            try
            {
                List<BidListItemForActivityDay> bidList = activityDayService.GetBidListByReservation(userId, id);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(bidList)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: GetBidList by id: {id}, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/Summary")]
        [HttpPost]
        public IHttpActionResult Summary([FromUri] string userId, [FromBody] DataFromClient data)
        {
            try
            {
                var list = reservationService.GetFetchXml(userId, true, data.Paging, data.Sort, data.FilterList);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(list)));
            }
            catch (Exception ex)
            {
                Trace.TraceError("Get reservations summary, Exception: " + ex.Message);
                return InternalServerError(ex);
            }
        }



        [Route("Reservations/GetItineraryList/{id}")]
        public IHttpActionResult GetItineraryList([FromUri] string userId, string id)
        {
            try
            {
                List<ItineraryGroupForActivityDay> itineraryGroupList = activityDayService.GetItineraryGroupListByReservation(userId, id);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(itineraryGroupList)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: GetBidList by id: {id}, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/Create")]
        [HttpPost]
        public IHttpActionResult Create([FromBody] ReservationItem data, [FromUri] string userId)
        {
            //Trace.WriteLine("Create reservation item");
            try
            {
               var id= reservationService.Create(data, userId);
                //Trace.WriteLine("Create reservation before return, id:" + data.Id);
                data.ModifiedOn = reservationService.GetModifiedOn(id.ToString(), userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(data)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: Create reservation exception: " + ex.Message);
                return InternalServerError(ex);
            }

        }

        [Route("Reservations/UpdateField/{id}")]
        [HttpPost]
        public IHttpActionResult UpdateField(string id, [FromBody] UpdateCrmItem item, [FromUri] string userId)
        {
            try
            {
                reservationService.UpdateField(id, item, userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(item)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: Update reservations item {id}, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/UpdateFields/{id}")]
        [HttpPost]
        public IHttpActionResult UpdateFields(string id, [FromBody] List<UpdateCrmItem> items, [FromUri] string userId)
        {
            try
            {
                reservationService.UpdateFields(id, items, userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(items)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: Update FrontDeskListItem {id}, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }



        [Route("Reservations/Update/{id}")]
        [HttpPost]
        public  IHttpActionResult Update([FromUri] string userId, string id, [FromBody] UpdateReservationNewOld data)
        {
            try
            {
                string lastUpdated = reservationService.GetModifiedOn(id, userId);
               if (data==null || lastUpdated!= data.Old.ModifiedOn)//sometimes null. check why
                {
                    return Content(HttpStatusCode.Conflict, "this text does not show in client side");
                }
               // Trace.TraceInformation($"{DateTime.Now}: ======================Update reservation================, userId: {userId}, id: {id}======= {JsonConvert.SerializeObject(data)}");

                 reservationService.Update(data.New, data.Old, userId);

                 reservationService.Delete(data.Old, userId);
                 reservationService.isNeedToUpdateReservationPaymentStatus(data.Old, userId);
                data.New.ModifiedOn= reservationService.GetModifiedOn(id, userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(data.New)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: Update reservation {id}, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }

       [Route("Reservations/CancelMuseum")]
        public bool CancelMuseum([FromUri] string userId)
        {

            try
            {
                List<string> list = new List<string>() { "SCH-0000066","SCH-0000067","SCH-0000068","SCH-0000069",
                    "SCH-0000070","SCH-0000073","SCH-0000074","SCH-0000077","SCH-0000078","SCH-0000080","SCH-0000081",
                    "SCH-0000088","SCH-0000089","SCH-0000090","SCH-0000091","SCH-0000092","SCH-0000094","SCH-0000096",
                    "SCH-0000097","SCH-0000098","SCH-0000099","SCH-0000100","SCH-0000101","SCH-0000102","SCH-0000103",
                    "SCH-0000104","SCH-0000106","SCH-0000108","SCH-0000169","SCH-0000244","SCH-0000267","SCH-0000327",
                    "SCH-0000395","SCH-0000398","SCH-0000401","SCH-0000402","SCH-0000403","SCI-0000296" };
                foreach (var res_number in list)
                {

                    //var item = crmConnection.Retrieve(EntityName, Guid.Parse(item.Id), userId);

                    //Query for the GUID of the Account using Account Name
                    Microsoft.Xrm.Sdk.Query.QueryExpression query = new Microsoft.Xrm.Sdk.Query.QueryExpression("new_reservation");
                    query.Criteria = new FilterExpression();
                    query.Criteria.AddCondition("new_num", ConditionOperator.Equal, res_number);
                    query.ColumnSet = new ColumnSet(null);
                    var item = CRMConnection.RetrieveMultiple(query, userId);

                    Microsoft.Xrm.Sdk.Entity fullItem = CRMConnection.Retrieve("new_reservation", item.Entities[0].Id, true, userId);
                    ReservationItem reservationItem = reservationService.CancelMuseum(userId, fullItem);
                    //Casting the reference to GUID
                    // Guid accountId = (Guid)account[0].Attributes["accountid"];

                    reservationItem.IsCanceled = true;
                    reservationService.Cancel(reservationItem, userId);

                }

                return true;
            }
            catch (Exception ex)
            {
                return false;

            }
        }

        [Route("Reservations/Cancel/{id}/{isCancel}")]
        public IHttpActionResult Cancel([FromUri] string userId, string id,bool isCancel, [FromBody] ReservationItem item)
        {
            try
            {
                if (isCancel)
                    item.IsCanceled = true;
                else
                    item.IsActive = true;
                reservationService.Cancel(item, userId, isCancel);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(item)));
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [Route("Reservations/SaveAttachments/{id}")]
        public IHttpActionResult SaveAttachments([FromUri] string userId, string id, [FromBody] List<Attachment> attachmentList)
        {
            //Trace.WriteLine($"start reservation {id} saveAttachments");
            try
            {
                var attachmentListWithIds = attachmentsService.SaveAttachments(id, attachmentList, userId);
                var reservationModifiedOn = "";
                if (attachmentList.Count > 0 && attachmentList[0].Reservation!=null)
                {
                    reservationModifiedOn = reservationService.GetModifiedOn(id, userId);
                }
                //Trace.WriteLine($"reservation {id} saveAttachments, before return");

                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(new { AttachmentList = attachmentListWithIds, ModifiedOn = reservationModifiedOn })));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: reservation {id} saveAttachments, exception: {ex.Message}");
                return InternalServerError(ex);
            }

        }
       
        
        
        
        /////////////////////////////////////////////////////////////
        [Route("Reservations/GetBreakProduct")]
        [HttpGet]
        public IHttpActionResult GetBreakProduct([FromUri] string userId)
        {
            try
            {
                var breakProduct = reservationService.GetBreakProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetAudioGuideProduct")]
        [HttpGet]
        public IHttpActionResult GetAudioGuideProduct([FromUri] string userId)
        {

            try
            {
                var audioGuideProduct = reservationService.GetAudioGuideProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(audioGuideProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetAudioGuideProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetHeadsetProduct")]
        [HttpGet]
        public IHttpActionResult GetHeadsetProduct([FromUri] string userId)
        {

            try
            {
                var headsetProduct = reservationService.GetHeadsetProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(headsetProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetHeadsetProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetMuseumProduct")]
        [HttpGet]
        public IHttpActionResult GetMuseumProduct([FromUri] string userId)
        {
            // Trace.WriteLine($"{DateTime.Now} devora temp log GetMuseumProduct ");

            try
            {
                var breakProduct = reservationService.GetMuseumProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetMuseumProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        //[Route("Reservations/GetEducationMobilityTransportationProduct")]
        //[HttpGet]
        //public IHttpActionResult GetEducationMobilityTransportationProduct([FromUri] string userId)
        //{
        //    try
        //    {
        //        var product = reservationService.GetEducationMobilityTransportationProduct(userId);
        //        return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(product)));
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceError($"{DateTime.Now} ReservationsController => GetMuseumProduct: {ex.Message}");
        //        return InternalServerError(ex);
        //    }
        //}


     


        [Route("Reservations/GetCampusProduct")]
        [HttpGet]
        public IHttpActionResult GetCampusProduct([FromUri] string userId)
        {
           // Trace.WriteLine($"{DateTime.Now} GetCampusProduct ");

            try
            {
                var breakProduct = reservationService.GetCampusProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetCampusProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }
        [Route("Reservations/GetRegistrationFeeProduct")]
        [HttpGet]
        public IHttpActionResult GetRegistrationFeeProduct([FromUri] string userId)
        {
            // Trace.WriteLine($"{DateTime.Now} GetCampusProduct ");

            try
            {
                var registrationFeeProduct = reservationService.GetRegistrationFeeProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(registrationFeeProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetCampusProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }
        [Route("Reservations/GetIsraelCountry")]
        [HttpGet]
        public IHttpActionResult GetIsraelCountry([FromUri] string userId)
        {
            try
            {
                var israelCountry = reservationService.GetIsraelCountry(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(israelCountry)));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{DateTime.Now}: ReservationsController => getIsraelCountry: {ex.Message}");
                return InternalServerError(ex);
            }
        }


        [Route("Reservations/GetHebrewLanguage")]
        [HttpGet]
        public IHttpActionResult GetHebrewLanguage([FromUri] string userId)
        {
            try
            {
                var hebrewLanguage = reservationService.GetHebrewLanguage(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(hebrewLanguage)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetBigQuestionsRoomProduct")]
        [HttpGet]
        public IHttpActionResult GetBigQuestionsRoomProduct([FromUri] string userId)
        {
            try
            {
                var breakProduct = reservationService.GetBigQuestionsRoomProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetHolocaustPerformanceProduct")]
        [HttpGet]
        public IHttpActionResult GetHolocaustPerformanceProduct([FromUri] string userId)
        {
            try
            {
                var breakProduct = reservationService.GetHolocaustPerformanceProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetTheLifeBeforeProduct")]
        [HttpGet]
        public IHttpActionResult GetTheLifeBeforeProduct([FromUri] string userId)
        {
            try
            {
                var breakProduct = reservationService.GetTheLifeBeforeProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetBackToLifeProduct")]
        [HttpGet]
        public IHttpActionResult GetBackToLifeProduct([FromUri] string userId)
        {
            try
            {
                var breakProduct = reservationService.GetBackToLifeProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetBhadimCityClassesProduct")]
        [HttpGet]
        public IHttpActionResult GetBhadimCityClassesProduct([FromUri] string userId)
        {
            try
            {
                var breakProduct = reservationService.GetBhadimCityClassesProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetBhadimCityProduct")]
        [HttpGet]
        public IHttpActionResult GetBhadimCityProduct([FromUri] string userId)
        {
            try
            {
                var breakProduct = reservationService.GetBhadimCityProduct(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetMuseumResource")]
        [HttpGet]
        public IHttpActionResult GetMuseumResource([FromUri] string userId)
        {
            try
            {

                var museumResource = resourceService.GetMuseumResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(museumResource)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetCampusResource")]
        [HttpGet]
        public IHttpActionResult GetCampusResource([FromUri] string userId)
        {
            try
            {

                var campusResource = resourceService.GetCampusResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(campusResource)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetBigQuestionsRoomResource")]
        [HttpGet]
        public IHttpActionResult GetBigQuestionsRoomResource([FromUri] string userId)
        {
            try
            {
                var breakProduct = resourceService.GetBigQuestionsRoomResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBigQuestionsRoomResource: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetArchiveResource")]
        [HttpGet]
        public IHttpActionResult GetArchiveResource([FromUri] string userId)
        {
            try
            {
                var breakProduct = resourceService.GetArchiveResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetArchiveResource: {ex.Message}");
                return InternalServerError(ex);
            }
        }


        [Route("Reservations/GetBhadimCityResource")]
        [HttpGet]
        public IHttpActionResult GetBhadimCityResource([FromUri] string userId)
        {
            try
            {
                var breakProduct = resourceService.GetBhadimCityResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetHolocaustPerformanceResource")]
        [HttpGet]
        public IHttpActionResult GetHolocaustPerformanceResource([FromUri] string userId)
        {
            try
            {
                var breakProduct = resourceService.GetHolocaustPerformanceResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }


        [Route("Reservations/GetTheLifeBeforeResource")]
        [HttpGet]
        public IHttpActionResult GetTheLifeBeforeResource([FromUri] string userId)
        {
            try
            {
                var breakProduct = resourceService.GetTheLifeBeforeResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetBackToLifeResource")]
        [HttpGet]
        public IHttpActionResult GetBackToLifeResource([FromUri] string userId)
        {
            try
            {
                var breakProduct = resourceService.GetBackToLifeResource(userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(breakProduct)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetBreakProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        //רשימת שותפים
        /*[Route("Reservations/GetPartnerListByDepartment/{department}")]
        [HttpGet]
        public IHttpActionResult GetPartnerListByDepartment(string department)
        {

            return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(reservationService.GetSupporterListByDepartment(department, "partner"))));
        }*/

        /*[Route("Reservations/GetProduct/{id}")]
        [HttpGet]
        public IHttpActionResult GetProduct([FromUri] string userId, string id)
        {
            try
            {
                var activityDayType = reservationService.GetProduct(userId, id);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(activityDayType)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetProduct: {ex.Message}");
                return InternalServerError(ex);
            }
        }*/


        [Route("Reservations/GetActivityDayType/{id}")]
        [HttpGet]
        public IHttpActionResult GetActivityDayType([FromUri] string userId, string id)
        {
            try
            {
                var activityDayType = reservationService.GetActivityDayType(userId, id);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(activityDayType)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetActivityDayType: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        /*[Route("Reservations/GetActivityDayType/{id}/{location}/{budget}/{date}")]
        [HttpGet]
        public IHttpActionResult GetActivityDayType(string id, string location, string budget, string date)
        {
            try
            {
                var activityDayType = reservationService.GetActivityDayType(id, location, budget, date);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(activityDayType)));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error! ReservationsController => GetActivityDayType: {ex.Message}");
                return InternalServerError(ex);
            }
        }*/






        /*[Route("Reservations/GetProductPricelist/{product}/{location}/{pricelist_type}")]
        [HttpGet]
        public IHttpActionResult GetProductPricelist(string product, string location, string pricelist_type)
        {
            try
            {
                var productPrice = reservationService.GetProductPricelist(product, location, pricelist_type);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(productPrice)));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error! ReservationsController => GetActivityDayType: {ex.Message}");
                return InternalServerError(ex);
            }
        }*/

        /*[Route("Reservations/GetProductPricelists/{product}")]
        [HttpGet]
        public IHttpActionResult GetProductPricelists([FromUri] string userId, [FromUri] string product)
        {
            try
            {
                var productPrice = reservationService.GetProductPricelists(userId, product);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(productPrice)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetActivityDayType: {ex.Message}");
                return InternalServerError(ex);
            }
        }*/

        // [Route("Reservations/GetReservationForInvoice/{reservationGuid}/{activityDayGuid}")]//
        [Route("Reservations/GetReservationForInvoice")]
        [HttpPost]
        public IHttpActionResult GetReservationForInvoice([FromUri] string userId, [FromBody] ReservationForInvoiceData data)
        {
            try
            {
                var result = invoiceService.GetInvoiceDataFromReservation(userId, data.reservationGuidList, data.activityDayGuidList, data.bidItemGuidList); 
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(result)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetDataForAlmog: {ex.Message}");
                return InternalServerError(ex);
            }
        }


        [Route("Reservations/GetAlmogInvoice")]
        [HttpGet]
        public IHttpActionResult GetAlmogInvoice([FromUri] string userId, [FromUri] string invoiceNumber)
        {
            try
            {
                //Trace.WriteLine(" in GetReservationForInvoice");
                var result = reservationService.GetAlmogInvoice(userId, invoiceNumber);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(result)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetDataForAlmog: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/UpdateAlmogNumber/{userId}")]
        [HttpPost]
        public IHttpActionResult UpdateAlmogNumber(string userId, [FromBody]AlmogToCrm almogToCrm)
       {
            try
            {
                reservationService.UpdateAlmogNumber(almogToCrm, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => UpdateAlmogNumber: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetServiceProviderForReservation/{guid}")]
        [HttpGet]
        public IHttpActionResult GetServiceProviderForReservation([FromUri] string userId, string guid)
        {
            try
            {
                //Trace.WriteLine(" in GetServiceProviderForReservation");
                var result = serviceProviderService.GetServiceProviderForReservation(userId, guid);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(result)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => GetServiceProviderForReservation: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetKmByLatLng")]
        [HttpPost]
        public IHttpActionResult GetKmByLatLng([FromBody] GoogleCordinations cordinations)
        {
            try
            {

                var result = googleApiService.GetKmByLatLng(cordinations);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(result)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: GetKmByLatLng: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetCurrencyList")]
        [HttpGet]
        public IHttpActionResult GetCurrencyList([FromUri] string userId)
        {
            var list = reservationService.GetCurrencyList(userId.ToString());
            return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(list)));
           
        }


        [Route("Reservations/GetReservatiobByNum")]
        [HttpGet]
        public IHttpActionResult GetReservatiobByNum([FromUri] string userId, [FromUri] string num)
        {
            var filter = new Filter
            {
                Field = "ReservationNum",
                Value = num,
                Type = "string"
            };
            var list = reservationService.GetFetchXml(userId,false,null,null,new List<Filter>() { filter }).Item2;
            if (list.Count == 1)
            {
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(list[0].Id)));
            }
            throw new Exception();

        }

        [Route("Reservations/FixItinerary")]
        [HttpGet]
        public void FixItinerary()
        {
            //reservationService.FixItinerary();
        }

        [Route("Reservations/GetFullReservationList")]
        [HttpPost]
        public IHttpActionResult GetFullReservationList([FromUri] string userId,[FromBody]  DataFromClient data)
        {
            try
            {
                //DataFromClient data = new DataFromClient();
                //data.FilterList = new List<Filter>();
                //data.FilterList.Add(new Filter
                //{
                //    Value = "D1AF38EF-D96F-ED11-80E1-005056911B09",
                //    Field = "new_reservationid",
                //    Type = "lookup",
                //    Operator = "eq"
                //});
                var result = reservationService.GetFullReservationList(data.Paging,data.Sort,data.FilterList,userId);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(result)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetDataForAlmog: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/createInvoiceForReservation/{reservationId}")]
        [HttpGet]
        public IHttpActionResult createInvoiceForReservation([FromUri] string userId, [FromBody] ReservationForInvoiceData data)
        {
            try
            {
                var invoiceData = invoiceService.createInvoiceForReservation(userId, data);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(invoiceData)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => GetDataForAlmog: {ex.Message}");
                return InternalServerError(ex);
            }
        }


        [Route("Reservations/createInvoiceAndSendEmail")]
        [HttpPost]
        public IHttpActionResult createInvoiceAndSendEmail([FromUri] string userId, [FromBody] ReservationForInvoiceData data)
        {
            Trace.TraceError($"{DateTime.Now} ReservationsController => createInvoiceAndSendEmail start");
            try
            {
                var invoiceData = invoiceService.createInvoiceForReservationAuto(userId, data);
                if (!string.IsNullOrEmpty(invoiceData))
                {
                    reservationService.sendReminderEmail(invoiceData , data.reservationGuidList[0], "ReminderToPayInvoice",userId);
                }
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(invoiceData)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => createInvoiceAndSendEmail: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/createInvoiceForReservationAuto")]
        [HttpPost]
        public IHttpActionResult createInvoiceForReservationAuto([FromUri] string userId, [FromBody] ReservationForInvoiceData data)
        {
            Trace.TraceError($"{DateTime.Now} ReservationsController => createInvoiceForReservationAuto start");
            try
            {
                var invoiceData = invoiceService.createInvoiceForReservationAuto(userId, data);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(invoiceData)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now} ReservationsController => createInvoiceForReservationAuto: {ex.Message}");
                UpdateCrmItem updateFeild = new UpdateCrmItem()
                {
                    Field = "AccountHandlingComments",
                    Type = "string",
                    Value = ex.Message
                };
                reservationService.UpdateField(data.reservationGuidList[0], updateFeild,userId);
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/updatePaymentStatus")]
        [HttpPost]
        public IHttpActionResult UpdatePaymentStatus([FromUri] string userId, [FromBody] ReservationForInvoiceData data)
        {
            try
            {
                reservationService.UpdatePaymentStatus( userId, data.bidItemGuidList,data.reservationGuidList);
                return Ok();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => UpdatePaymentStatus: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/updateReservationPaymentStatus/{reservationId}")]
        [HttpGet]
        public IHttpActionResult updateReservationPaymentStatus([FromUri] string userId, string reservationId)
        {
            try
            {
                reservationService.updateReservationPaymentStatus(reservationId, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => updateReservationPaymentStatus: {ex.Message}");
                return InternalServerError(ex);
            }
        }
        [Route("Reservations/SendMailReservationVoucher/{reservationId}")]
        [HttpPost]
        public IHttpActionResult SendMailReservationVoucherByReservationGuid([FromUri] string userId,[FromUri]  string emailAddress,  string reservationId , [FromUri] string language = "",[FromUri] bool isForCancel = false,[FromUri] bool isWithoutRefund = false)
        {
            try
            {
                var result  = reservationService.SendMailReservationVoucherByReservationGuid(reservationId, emailAddress, userId, language,isForCancel, isWithoutRefund);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: ReservationsController => SendMailReservationVoucherByReservationGuid: {ex.Message}");
                return InternalServerError(ex);
            }
        }
        
        // GET: api/Reservations/GetReservationByName/5
        [Route("Reservations/GetReservationByReservationNum/{name}")]
        public IHttpActionResult GetReservationByReservationNum([FromUri] string userId, string name)
        {
            try
            {
                var reservationItem = reservationService.GetReservationByReservationNum(userId, name);
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(reservationItem)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: get reservation by name: {name}, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        [Route("Reservations/GetRightToDiscountList")]
        [HttpGet]
        public IHttpActionResult GetRightToDiscountList([FromUri] string userId)
        {
            try
            {
                var list = reservationService.GetRightToDiscountList(userId.ToString());
                return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(list)));
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: GetRightToDiscountList, exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        //[Route("Reservations/GetEnglishForPdf")]
        //[HttpPost]
        //public IHttpActionResult GetEnglishForPdf()
        //{

        //    try
        //    {
        //        //[FromUri]
        //        string userId = "74ec67fa-ad4e-e611-80ce-005056b13c97";
        //        //,[FromBody]
        //        EnglishFieldsPdf EnglishFieldsPdf = new EnglishFieldsPdf();
        //        EnglishFieldsPdf.locationGuid = "2347c5ae-f818-eb11-8106-00505691117e";
        //        var result = reservationService.GetEnglishForPdf(EnglishFieldsPdf,userId); 
        //        return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(result)));
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceError($"{DateTime.Now}: Get reservation List, Exception: " + ex.Message);
        //        return InternalServerError(ex);
        //    }
        //}
        //public IHttpActionResult GetEnglishForPdf([FromUri] string userId, [FromUri] string invoiceNumber)
        //{
        //    try
        //    {
        //        //Trace.WriteLine(" in GetReservationForInvoice");
        //        var result = reservationService.GetAlmogInvoice(userId, invoiceNumber);
        //        return Ok(JsonConvert.DeserializeObject(helperService.ObjConvertToJson(result)));
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceError($"{DateTime.Now} ReservationsController => GetDataForAlmog: {ex.Message}");
        //        return InternalServerError(ex);
        //    }
        //}

        /*[Route("Reservations/SetReservationStatus/{id}/{status}")]
        [HttpPost]
        public IHttpActionResult SetReservationStatus(string id, string status, [FromUri] string userId)
        {
            try
            {
                reservationService.SetReservationStatus(id, status, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: reservationService SetReservationStatus Exception: {ex.Message}");
                return InternalServerError(ex);
            }
        }*/
    }
}