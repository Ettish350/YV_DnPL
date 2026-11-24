using Data.Repositories.Interfaces;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Serilog;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class HelperService : IHelperService
    {
        readonly ICRMConnection _crmService;
        readonly IBasicRepository _basicRepository;
        public HelperService(ICRMConnection crmService, IBasicRepository basicRepository)
        {
            _crmService = crmService;
            _basicRepository = basicRepository;
        }
        public void ActivateEntity(Entity entity)
        {
            SetStateRequest setStateRequest = new SetStateRequest
            {
                EntityMoniker = new EntityReference(entity.LogicalName, entity.Id),
                State = new OptionSetValue(0),//active state
                Status = new OptionSetValue(1),//default crm value of active status code
            };
            _crmService.Execute(setStateRequest);
        }

        public Guid GetContactByEmailOrMobile(string email, string mobile)
        {
            var fetchXml = _basicRepository.StartFetchXml("contact");
            fetchXml += @$"<attribute name='contactid' />
             <filter type='and'>
               <condition attribute='statecode' operator='eq' value='0' />
               <filter type='or'>
                    <condition attribute='emailaddress1' operator='eq' value='{email}' />
                    <condition attribute='mobilephone' operator='eq' value='{mobile}' />
                </filter>
              </filter>";
            fetchXml += _basicRepository.EndFetchXml();
            var query = new FetchExpression(fetchXml);
            _crmService.RetrieveMultiple(query);
            Guid guid = Guid.Empty;
            var res = _crmService.RetrieveMultiple(query);
            if (res.Entities.Count > 0)//if exists
            {
                guid = res.Entities.First().Id;
            }
            return guid;

        }
        public string CreateRequest(string url, string metod, string data)
        {

            string response;
            StringBuilder sbUrl = new StringBuilder(url);
            HttpWebRequest omniRequest = (HttpWebRequest)WebRequest.Create(sbUrl.ToString());
            omniRequest.Method = metod;
            omniRequest.ContentType = "application/json";

            //Write the json details to the request
            using (var streamWriter = new StreamWriter(omniRequest.GetRequestStream()))
            {
                string json = data;
                streamWriter.Write(json);
            }

            //Get the response of the request
            try
            {
                using (HttpWebResponse statusResponse = (HttpWebResponse)omniRequest.GetResponse())
                {
                    using (Stream receiveStream = statusResponse.GetResponseStream())
                    {
                        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                        {
                            response = readStream.ReadToEnd();
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex) { throw ex; }
        }



    }
}
