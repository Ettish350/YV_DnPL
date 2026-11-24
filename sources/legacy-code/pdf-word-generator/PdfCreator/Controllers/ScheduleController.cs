using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Services.Interface;

namespace PdfCreator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        IScheduleService scheduleService;
        public ScheduleController(IScheduleService scheduleService)
        {
            this.scheduleService = scheduleService;
        }
       
        // GET: api/Schedule
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Get ScheduleController is working", DateTime.Now.ToString() };

        }

        // POST: api/Schedule
        [HttpPost]
        public ActionResult<AttachmentItem> Post([FromBody] ScheduleReservationList data)
        {
            Trace.WriteLine("in schedule post" + DateTime.Now.ToString());
            try
            {
               var createdDoc= scheduleService.CreateDocument(data);
                Trace.WriteLine("after created doc");
                return Ok(createdDoc);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("exception: " + ex.Message);
                return StatusCode(500, ex);
            }
        }
        //api/Schedule/Multiple
        [Route("Multiple")]
        [HttpPost]
        public ActionResult<Dictionary<string, AttachmentItem>> MultiplePost([FromBody] Dictionary<string, ScheduleReservationList> data)
        {
            Trace.WriteLine("in schedule post" + DateTime.Now.ToString());
            try
            {
                Dictionary<string, AttachmentItem> createdDocList = new Dictionary<string, AttachmentItem>();
                foreach (var item in data) {
                    var createdDoc = scheduleService.CreateDocument(item.Value);
                    createdDocList.Add(item.Key, createdDoc);
                }
                Trace.WriteLine("after created docs");
                return Ok(createdDocList);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("exception: " + ex.Message);
                return StatusCode(500, ex);
            }
        }
        //api/Schedule/ScheduleResource
        [Route("ScheduleResource")]
        [HttpPost]
        public ActionResult<Dictionary<string, AttachmentItem>> ScheduleResource([FromBody] ScheduleResourceItem data)
        {
            Trace.WriteLine("in schedule post" + DateTime.Now.ToString());
            try
            {
                var createdDoc = scheduleService.createScheduleResourceDoc(data);
                Trace.WriteLine("after created doc");
                return Ok(createdDoc);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("exception: " + ex.Message);
                return StatusCode(500, ex);
            }
        }
    }
}
