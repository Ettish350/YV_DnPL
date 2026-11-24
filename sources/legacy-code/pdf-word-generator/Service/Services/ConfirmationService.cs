using MigraDoc.DocumentObjectModel;
using Service.Services.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Data.Repositories.Interfaces;
using Data.Models;
using System.Reflection;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Service.Services
{
    public class ConfirmationService: IConfirmationService
    {
        ISchoolConfirmationRepository  schoolConfirmationRepository;
        IAgencyConfirmationRepository  agencyConfirmationRepository;
        ICommemorationRepository commemorationRepository;
        private IHelperRepository helperRepository;
       List<string> mandatoryValues = new List<string>() { "orderNum"};
        public ConfirmationService(
            ISchoolConfirmationRepository schoolConfirmationRepository,
            IAgencyConfirmationRepository agencyConfirmationRepository,
            ICommemorationRepository commemorationRepository,
            IHelperRepository helperRepository)
        {
            this.schoolConfirmationRepository = schoolConfirmationRepository;
            this.agencyConfirmationRepository = agencyConfirmationRepository;
            this.commemorationRepository = commemorationRepository;
            this.helperRepository = helperRepository;
        }

        IConfiguration configuration = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .Build();
        public AttachmentItem CreateSchoolDocument(SchoolConfirmationData confirmationData)
        {
            Document document = new Document();
            if(!isDataValid(confirmationData))
            {
                Log.Error("Data is not valid");
                throw new Exception("Data is not valid");
            }
            Log.Error("confirmationData is valid");
            var createdFileName = schoolConfirmationRepository.CreateDocument(document, confirmationData);
            Log.Error("createdFileName: " + createdFileName);

            if (confirmationData.IsFromOnlineReservation) {
                return helperRepository.AttachDocument(createdFileName, ".pdf", "Confirmation");
            }
            return helperRepository.AttachDocument(createdFileName);

        }

        public AttachmentItem CreateAgencyDocument(AgencyConfirmationData confirmationData)
        {
            Document document = new Document();
            /*if (!isDataValid(confirmationData))
            {
                throw new Exception("Data is not valid");
            }*/
            var createdFileName = agencyConfirmationRepository.CreateDocument(document, confirmationData);
            return helperRepository.AttachDocument(createdFileName);

        }
        public AttachmentItem CreateCommemorationDocument(SchoolConfirmationData data)
        {
          
            var createdFileName = commemorationRepository.CreateDocument(data);
            return helperRepository.AttachDocument(createdFileName);

        }

        

        public bool isDataValid(SchoolConfirmationData data)
        {
            List<bool> results = new List<bool>();
            results.Add(checkData(data));
            if (data.ActivityDayList != null)
            {
                foreach (var item in data.ActivityDayList)
                {
                    results.Add(checkData(item));
                }
            }
            else {
                Log.Error("ActivityDayList is null");
            }
           
            return !results.Contains(false);
        }
        private bool checkData( object data)
        {
            bool res = true;
            //all values must be full
            try
            {
                foreach (PropertyInfo prop in data.GetType().GetProperties())
                {
                    Log.Error("prop name: " + prop.Name);
                    var temp = prop.GetValue(data, null);
                    if (temp == null)
                    {
                        Log.Error("temp is null");
                        if (mandatoryValues.Contains(prop.Name))
                        {
                            Log.Error("missing value: " + prop.Name);
                            res = false;
                        }
                        else
                        {
                            prop.SetValue(data, "");

                        }


                    }
                    else
                    {
                        Log.Error(" value of: " + prop.Name + " is: " + temp);
                    }
                }
            }
            catch (Exception ex) {
                Log.Error(" ex: " + ex.Message);
            }
            Log.Error(" before return res: " + res);
            return res;
        }
       
    }
}
