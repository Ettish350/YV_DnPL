using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Service.Services.Interface;

namespace PdfCreator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfirmationController : ControllerBase
    {

        IConfirmationService  confirmationService;

        public ConfirmationController(IConfirmationService confirmationService)
        {
            this.confirmationService = confirmationService;


        }
        // GET: api/Confirmation
        [HttpGet]
        public string Get()
        {
            return "ConfirmationController is working" + DateTime.Now.ToString();
        }

        // POST: api/Confirmation
        [Route("School/CreateDocumentTest")]
        [HttpPost]
        public ActionResult<AttachmentItem> CreateDocumentTest([FromBody] SchoolConfirmationData temp)
        {
            Log.Error("                                 ");
            Log.Error("************************************* " + DateTime.Now.ToString() + " ****************************************");
            //Log.Error("GroupName " + confirmationData.GroupName);
             try
             {
                 Log.Error("in test school  ");
                 AttachmentItem createdDoc = new AttachmentItem();
                createdDoc.Comments = temp.ContactName;
                createdDoc.FullPath = temp.ContactEmail;
                createdDoc.Name = temp.ShowDetails.ToString();
                 return Ok(createdDoc);
             }
             catch (Exception ex)
             {
                 Trace.WriteLine("exception: " + ex.Message);
                 return StatusCode(500, ex);
             }
        }


        // POST: api/Confirmation
        [Route("School")]
        [HttpPost]
        public ActionResult<AttachmentItem> CreateDocument([FromBody] SchoolConfirmationData confirmationData)
        {
            Log.Error("                                 ");
            Log.Error("************************************* " + DateTime.Now.ToString() + " ****************************************");
            
            try
            {
                Log.Error("go to create document.  ");
                AttachmentItem createdDoc;
                if (confirmationData.Division=="2")//"commemoration"
                {
                    createdDoc=confirmationService.CreateCommemorationDocument(confirmationData);
                }
                else
                {
                    Log.Error("adina GroupName " + confirmationData.GroupName);
                    Log.Error("go to create document.  ");
                    createdDoc = confirmationService.CreateSchoolDocument(confirmationData);
                }
                Log.Error("after create document.  ");
                return Ok(createdDoc);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("exception: " + ex.Message);
                return StatusCode(500, ex);
            }
        }

        // POST: api/Confirmation
        [Route("Agency")]
        [HttpPost]
        public ActionResult<AttachmentItem> Post([FromBody] AgencyConfirmationData confirmationData)
        {
            Log.Error("                                 ");
            Log.Error("************************************* " + DateTime.Now.ToString() + " ****************************************");
            
            try
            {

                Log.Error("go to create document.  ");
                var createdDoc = confirmationService.CreateAgencyDocument(confirmationData);
                Log.Error("after create document.  ");
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
