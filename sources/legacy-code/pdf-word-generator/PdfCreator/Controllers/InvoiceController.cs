using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Serilog;
using Service.Services.Interface;
using Newtonsoft.Json;

namespace PdfCreator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   

    public class InvoiceController : ControllerBase
    {
        IInvoiceService _invoiceService;
      
        public InvoiceController(IInvoiceService service)
        {
            Log.Error("InvoiceController ctor");
            _invoiceService = service;
         

        }

        // GET api/Invoice
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            _invoiceService.Test();
            Log.Error("                                 ");
            Log.Error("get is working");

            Log.Error("************************************* " + DateTime.Now.ToString() + " ****************************************");

            return new string[] { "Get InvoiceController is working", DateTime.Now.ToString()
            };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public ActionResult<AttachmentItem> Post([FromBody] InvoiceData data)
        {
            Log.Error("in post                                 ");
            Log.Error("************************************* "+DateTime.Now.ToString()+" ****************************************");


            try
            {
                //if (!_invoiceService.isDataValid(data))
                //{
                //    Log.Error("data in not valid ");
                //    return BadRequest();
                //}
                Log.Error("go to create document.  ");
                string json = JsonConvert.SerializeObject(data);
                Log.Error(json);
                var createdDoc= _invoiceService.CreateDocument(data);

                Log.Error("after create document.  ");
                return Ok(createdDoc);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("exception: "+ex.Message);
                return StatusCode(500,ex);
            }
           

        }








        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
