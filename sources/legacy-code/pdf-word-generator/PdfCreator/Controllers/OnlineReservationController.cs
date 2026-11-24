using Data.Models;
using Microsoft.AspNetCore.Mvc;
using Service.Services;
using System.Diagnostics;
using System;
using Service.Services.Interface;
using Serilog;
using System.Collections.Generic;

namespace PdfCreator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnlineReservationController : ControllerBase
    {
        IOnlineReservationService onlineReservationService;
        public OnlineReservationController(IOnlineReservationService onlineReservationService)
        {
            this.onlineReservationService = onlineReservationService;
        }
      
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            Log.Error("                                 ");
            Log.Error("get is working");

            Log.Error("************************************* " + DateTime.Now.ToString() + " ****************************************");

            return new string[] { "Get OnlineReservationController is working", DateTime.Now.ToString()
            };
        }
        [HttpPost]
      
            public ActionResult<AttachmentItem> Post([FromBody] OnlineReservationTicket data)
            {
                Trace.WriteLine("in OnlineReservation post" + DateTime.Now.ToString());
                 Trace.WriteLine("QRcodeDetails" + data);

            try
            {
                    var createdTicket = onlineReservationService.CreateQRcodeTicket(data);
                    Trace.WriteLine("after created ticket");
                    return Ok(createdTicket);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("exception: " + ex.Message);
                    return StatusCode(500, ex);
                }
            
            }
    }
}



    


   
