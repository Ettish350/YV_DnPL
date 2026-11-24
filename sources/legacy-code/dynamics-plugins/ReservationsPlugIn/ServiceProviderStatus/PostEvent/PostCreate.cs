using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlugIn;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace ReservationsPlugIn.ServiceProviderStatus.PostEvent
{
    public class PostCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            Entity entity = PluginUtils.GetContextEntity(context);
            if (entity != null)
            {
                if (entity.LogicalName != "new_service_provider_status") { return; }
                WriteToLog.LogToFile("new_service_provider_status post create", "\n");
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

                ServiceProviderStatusService oServiceProviderStatus = new ServiceProviderStatusService(systemService, userService);


                Entity preEntity = new Entity();
                Entity postEntity = (Entity)context.PostEntityImages["EntityImg"];
                Entity deEntity = PluginUtils.GetUpdateEntity(entity);

                bool bIsUpdate = false;
                oServiceProviderStatus.UpdateServiceProviderStatus(preEntity, postEntity, ref bIsUpdate, ref deEntity);
                #region update entity
                if (bIsUpdate == true)
                {

                    userService.Update(deEntity);
                }
                #endregion
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                WriteToLog.LogToFile("luba hello, have FaultException", "\n");
                WriteToLog.LogToFileError("FaultException<OrganizationServiceFault>: " + ex.Detail + " " + ex.InnerException + " " + ex.Message + " " + ex.StackTrace, sLogBaseData);
                throw new InvalidPluginExecutionException("An error occurred in the ServiceProviderStatusPostCreate plug-in " + ex.Message, ex);
            }

            catch (Exception ex)
            {
                WriteToLog.LogToFile("luba hello, have some Exception", "\n");
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
                        throw new InvalidPluginExecutionException("An error occurred in the HolocaustMemorialDayVisitorPostCreate plug-in " + ex.Message, ex);
                    }
                }
            }
        }



    }
}
