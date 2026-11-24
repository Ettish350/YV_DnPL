using Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Service.Services;
using System.Reflection;

namespace YVKiosk.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }
       
        [HttpGet]
        public string Get()
        {
            return "in payment controller get: "+ DateTime.Now.ToString();
        }

        // GET: PaymentController
        [Route("ReservationPay")]
        [HttpPost]
        public ActionResult ReservationPay([FromBody] Reservation rsv, [FromQuery] string kiosk)
        {
            var res = new CrmResultItem();           
            try
            {
                res.IsOk = true;
                res.ResponseObject = _paymentService.Pay(rsv, kiosk).Result;
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }
        [Route("EventRegistrationPay")]
        [HttpPost]
        public ActionResult EventRegistrationPay([FromBody] EventRegistration rsv, [FromQuery] string kiosk)
        {
            var res = new CrmResultItem();
            try
            {
                res.IsOk = true;
                res.ResponseObject = _paymentService.Pay(rsv, kiosk).Result;
            }
            catch (Exception ex)
            {
                res.IsOk = false;
                res.Message = ex.Message;
            }
            return Ok(res);

        }

      


    }
}
