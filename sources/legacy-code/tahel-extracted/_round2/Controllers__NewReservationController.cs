using Microsoft.AspNetCore.Mvc;
using Data.Models;
using Service.Services.Interfaces;
using Serilog;
using System.Runtime.CompilerServices;
using Service.Services;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace YVKiosk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewReservationController : ControllerBase
    {
        private readonly INewReservationService _newReservationService;
        private readonly IOnlineReservationService _onlineReservationService;
        private readonly IConfiguration _configuration;
        public NewReservationController(INewReservationService newReservationService,IOnlineReservationService onlineReservationService, IConfiguration configuration)
        {
            _newReservationService = newReservationService;
            _onlineReservationService= onlineReservationService; 
            _configuration = configuration;
        }
        [Route("test")]
        [HttpGet]
        public string Test()
        {
            return "server is working, crm url connection is: "+_configuration.GetValue<string>("CrmUrl");

        }

        [Route("GetMuseumOccupancyPerSlotTime")]
        [HttpGet]
        public ActionResult GetMuseumOccupancyPerSlotTime(string date, string? scheduleEventGuid)
        {
            try
            {
                return Ok(_newReservationService.GetMuseumOccupancyPerSlotTime(date, scheduleEventGuid));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Route("GetAudioguideAvailableLanguages")]
        [HttpGet]
        public ActionResult GetAudioguideAvailableLanguages(string lang)
        {
            try
            {
                return Ok(_newReservationService.GetAudioguideAvailableLanguages(lang));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        [Route("{reservationNumber}")]
        [HttpGet]
        public ActionResult Get(string reservationNumber,string lang)
        {
            var res = new CrmResultItem();

            try
            {
                res.IsOk = true;
                res.ResponseObject = _newReservationService.GetReservation(reservationNumber,lang);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);
        }
    
    [Route("GetAllAvailableTours")]
    [HttpGet]
    public ActionResult GetAllAvailableTours(string date)
    {
        try
        {
                return Ok(_newReservationService.GetAllAvailableTours(date));
        }
        catch (Exception ex)
        {
                return Problem(ex.Message);

            }
        }
        [Route("GetAvailableTourTimesForEventReservationTour")]
        [HttpPost]
        public ActionResult GetAvailableTourTimesForEventReservationTour([FromBody] TourType tourType,[FromQuery] string date, [FromQuery] string? eventRegistrationGuid="", [FromQuery] string? activityDayEventRegistrationGuid="")
        {
            try
            {
                ////devora 2025-04-21 get houres the new way from tour type for KIOSK
                ///צריך לפתוח את הקוד הזה אחרי יום השואה ולבדוק שהקיוסק עובד טוב עם השעות, גם אם אין
                ///יש בעיה אם ה]פעילות קייממת אבל עדיין לא נפתח היום פעילות
                var startTimeList = new List<StartTimeItem>();
                var duration = (decimal)(tourType.DurationActivity!=null ? tourType.DurationActivity : 1);
                var result = _onlineReservationService.getDateTimeSlots(date, tourType.Id, tourType.TourCode, duration);
                foreach (var item in result)
                {
                    startTimeList.Add(new StartTimeItem
                    {
                        AvailablePlaces = item.Value,
                        exprensiveHour = null,
                        isClosed = false,
                        StartTime = item.Key
                    });
                }
                return Ok(startTimeList);
                //return Ok(_newReservationService.GetAvailableTourTimesForEventReservationTour(date, tourType,eventRegistrationGuid,activityDayEventRegistrationGuid));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);

            }
        }
        [HttpPost]
        public ActionResult Post([FromBody] Reservation reservation)
        {
            var res = new CrmResultItem();

            try
            {
                res.IsOk = true;
                res.ResponseObject = _newReservationService.CreateReservation(reservation);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        [Route("SaveMuseumSlotTime")]
        [HttpPost]
        public ActionResult SaveMuseumSlotTime([FromBody] BasicTourDetails tourDetails)
        {
            var res = new CrmResultItem();
            try
            {
                res.IsOk = true;
                res.ResponseObject = _newReservationService.SaveMuseumSlotTime(tourDetails);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
        [Route("CreateEventRegistration")]
        [HttpPost]
        public ActionResult CreateEventRegistration([FromBody] EventRegistration eventRegistration, [FromQuery] bool isForUpdate=false)
        {
            var res = new CrmResultItem();

            try
            {
                res.IsOk = true;
                res.ResponseObject = _newReservationService.CreateEventRegistration(eventRegistration, isForUpdate);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
        [Route("UpdateGroups")]
        [HttpPost]
        public ActionResult UpdateGroups([FromBody] List<Group> groupList)
        {
            var res = new CrmResultItem();
            try
            {
                res.IsOk = true;
                res.ResponseObject = _newReservationService.UpdateGroups(groupList);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

        [Route("UpdateRsvStatus")]
        [HttpPost]
        public async Task<ActionResult> UpdateRsvStatusAsync([FromBody] Reservation reservation)
        {
            var res = new CrmResultItem();
            try
            {
                res.IsOk = true;
                res.ResponseObject = _newReservationService.UpdateRsvStatus(reservation);
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);
        }


        [Route("GetToursForIndividualsByDate")]// For search reservation on scanner page
        [HttpGet]
        public ActionResult GetToursForIndividualsByDate(string date)
        {

            try
            {

                return Ok(_onlineReservationService.GetToursForIndividualsByDate(date));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }

        }
    }
}
