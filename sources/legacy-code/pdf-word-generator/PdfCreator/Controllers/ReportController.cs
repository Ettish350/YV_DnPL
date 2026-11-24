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
    public class ReportController : ControllerBase
    {
        IReportService reporteService;
        public ReportController(IReportService reporteService)
        {
            this.reporteService = reporteService;
        }

        // GET: api/ReportController
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "Get ReportController is working", DateTime.Now.ToString() };

        }

        // POST: api/ReportController
        [HttpPost]
        public ActionResult<AttachmentItem> Post([FromBody] ReportData data)
        {
            Trace.WriteLine("in Reports post" + DateTime.Now.ToString());
            try
            {
               var createdFile= reporteService.CreateCombinedFile(data);
                Trace.WriteLine("after created file");
                return Ok(createdFile);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("exception: " + ex.Message);
                return StatusCode(500, ex);
            }
        }

    }
}
