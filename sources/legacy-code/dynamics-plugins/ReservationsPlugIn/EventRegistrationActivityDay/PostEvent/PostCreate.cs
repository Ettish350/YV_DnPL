using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlugIn;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace Reservations.EventRegistrationActivityDay.PostEvent
{
    public class PostCreate : IPlugin

    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            Entity entity = PluginUtils.GetContextEntity(context);
            if (entity != null)
            {
                if (entity.LogicalName != "new_event_registration_activity_day") { return; }
            }
            else
            {
                return;
            }
            string _timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfffffff");
            string sLogBaseData = _timeStamp + " " + this.GetType().FullName + " Id=" + entity.Id + " LogicalName=" + entity.LogicalName + " ";
            try
            {

                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService systemService = serviceFactory.CreateOrganizationService(null);
                IOrganizationService userService = serviceFactory.CreateOrganizationService(context.UserId);
                Entity preEntity = new Entity();
                Entity postEntity = (Entity)context.PostEntityImages["EntityImg"];
                bool bIsUpdate = false;

                Entity eEntity = PluginUtils.GetUpdateEntity(entity);

                EventRegistrationActivityDayService oEventRegistrationActivityDayService = new EventRegistrationActivityDayService(systemService, userService);

                oEventRegistrationActivityDayService.UpdateActivityDayTotalParticipants(preEntity, postEntity);

                #region Update entity
                if (bIsUpdate)
                {
                    userService.Update(eEntity);
                }

                #endregion
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                WriteToLog.LogToFileError("FaultException<OrganizationServiceFault>: " + ex.Detail + " " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace, sLogBaseData);
                throw new InvalidPluginExecutionException("PostCreate: " + ex.Message, ex);
            }

            catch (Exception ex)
            {
                if (ex.GetType().Name.CompareTo("CrmErrorException") == 0)
                {
                    WriteToLog.LogToFile("CrmErrorException: " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace, sLogBaseData);

                    throw new InvalidPluginExecutionException(ex.Message, ex);
                }
                else
                {
                    if (ex.GetType().Name.CompareTo("CrmErrorException") == 0)
                    {
                        WriteToLog.LogToFile("CrmErrorException: " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace, sLogBaseData);

                        throw new InvalidPluginExecutionException(ex.Message, ex);
                    }
                    else
                    {
                        WriteToLog.LogToFileError("Exception: " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace, sLogBaseData);
                        throw new InvalidPluginExecutionException("PostCreate:" + ex.Message, ex);
                    }
                }
            }
        }
    }
}
