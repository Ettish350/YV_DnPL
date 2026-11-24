using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlugIn;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using System.Globalization;

namespace ReservationsPlugIn.ServiceProviderStatus
{
    class ServiceProviderStatusService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;


        public ServiceProviderStatusService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }

        internal void SetDefaultProviderStatusName(Entity preEntity, ref Entity entity, ref bool bIsOk, ref string sErrorMsg)
        {

            String newName = "Service Provider Status: ";


            entity["new_name"] = newName;
        }

        internal void UpdateServiceProviderStatus(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {

       
            string strPreServiceProviderStatusName = EntityUtils.GetStringFromEntityValue("new_name", preEntity);
            string strPostServiceProviderType = EntityUtils.GetOptionSetValueFromEntityValueName("new_service_provider_type", postEntity, sysService);
            int intPostServiceProviderType = EntityUtils.GetOptionSetValueFromEntity("new_service_provider_type", postEntity).Value;
            Guid gPostServiceProviderGuid = EntityUtils.GetEntityReferenceFromEntityValue("new_service_provider", postEntity);
            String sPostStartDate = EntityUtils.GetDateTimeFromEntity("new_start_date", postEntity)?.ToString("dd/MM/yyyy");
            String sPostEndDate = EntityUtils.GetDateTimeFromEntity("new_end_date", postEntity)?.ToString("dd/MM/yyyy");


            if (sPostStartDate != null)
                WriteToLog.LogToFile("UpdateServiceProviderStatusName service function sPostStartDate:" + sPostStartDate, "\n");
            if(sPostEndDate != null)
            WriteToLog.LogToFile("UpdateServiceProviderStatusName service function sPostEndDate:" + sPostEndDate, "\n");
            Entity newServiceProviderEntity = GetEntityByGuid("new_service_provider", gPostServiceProviderGuid, new ColumnSet(true));


            String sServiceProviderName = "";
            if (gPostServiceProviderGuid != Guid.Empty)
            {
                sServiceProviderName = EntityUtils.GetStringFromEntityValue("new_name", newServiceProviderEntity);
            }

            String newName = "";
            

            if (!String.IsNullOrEmpty(sServiceProviderName))
                newName += sServiceProviderName;
            if (!String.IsNullOrEmpty(strPostServiceProviderType))
            {
                if (newName.Length > 0)
                    newName += ", " + strPostServiceProviderType;
                else newName += strPostServiceProviderType;
            }

            if (String.IsNullOrEmpty(newName))
                newName = "Service Provider Status";

            if (!String.IsNullOrEmpty(sPostStartDate))
                newName = newName + ", " + sPostStartDate;

            if (!String.IsNullOrEmpty(sPostEndDate))
                newName = newName  + " - " + sPostEndDate;


            if (String.Equals(strPreServiceProviderStatusName, newName))
            {
                UpdateServiceProviderEntity(newServiceProviderEntity, intPostServiceProviderType, EntityUtils.GetOptionSetValueFromEntity("statecode", postEntity).Value == 0);
                bIsUpdate = false;
            }
            else
            {
                eEntity["new_name"] = newName;
                bIsUpdate = true;
                DateTime endDate;

                if (String.IsNullOrEmpty(sPostEndDate))
                {
                    ActivateItem(eEntity);
                    UpdateServiceProviderEntity(newServiceProviderEntity, intPostServiceProviderType, true);
                }
                else
                {
                    Boolean isEnable = false;
                    endDate = DateTime.ParseExact(sPostEndDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    if (endDate.AddDays(1) > DateTime.Today)
                    {
                        ActivateItem(eEntity);
                        isEnable = true;
                    }
                    else
                        DeactivateItem(eEntity);

                    UpdateServiceProviderEntity(newServiceProviderEntity, intPostServiceProviderType, isEnable);
                }
            }

        }

        internal void DeactivateItem(Entity eEntity)
        {
            SetStateRequest setStateRequest = new SetStateRequest()
            {
                EntityMoniker = new EntityReference
                {
                    Id = eEntity.Id,
                    LogicalName = eEntity.LogicalName,
                },
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            };
            sysService.Execute(setStateRequest);
        }

        internal void ActivateItem(Entity eEntity)
        {
            SetStateRequest setStateRequest = new SetStateRequest()
            {
                EntityMoniker = new EntityReference
                {
                    Id = eEntity.Id,
                    LogicalName = eEntity.LogicalName,
                },
                State = new OptionSetValue(0),
                Status = new OptionSetValue(1)
            };
            sysService.Execute(setStateRequest);
        }


        internal void SetConnectedItemBooleanValue(Entity entity, string columnName, Boolean newValue)
        {
            entity[columnName] = newValue;
            sysService.Update(entity);
        }

        internal void UpdateServiceProviderEntity(Entity providerEntity, int serviceType, Boolean isEnabled)
        {
            Boolean value;
            if (isEnabled)
                value = true;
            else value = false;
            switch (serviceType)
            {
                case 5:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_national_service", value);
                    break;
                case 7:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_cantor", value);
                    break;
                case 1:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_guide", value);
                    break;
                case 15:
                    SetConnectedItemBooleanValue(providerEntity, "new_is_guide_with_tenure", value);
                    break;
                case 9:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_tour_guide", value);
                    break;
                case 2:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_lecturer", value);
                    break;
                case 3:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_testimony", value);
                    break;
                case 4:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_yv_employee", value);
                    break;
                case 11:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_photographer", value);
                    break;
                case 10:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_coordinator", value);
                    break;
                case 8:
                    SetConnectedItemBooleanValue(providerEntity, "new_rsv_status_is_actor", value);
                    break;
                default:
                    WriteToLog.LogToFile("UpdateServiceProviderEntity: unknown service provider status type: " + serviceType, " No changes in service provider item");
                    break;
            }
        }




        private Entity GetEntityByGuid(string entityName, Guid guid, ColumnSet cols)
        {
            return sysService.Retrieve(entityName, guid, cols);
        }

        private EntityCollection GetEntityCollection(string entityName, string attributeName, string attributeValue, ColumnSet cols)
        {
            QueryExpression query = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = cols,
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = attributeName,
                            Operator = ConditionOperator.Equal,
                            Values = { attributeValue }
                        }
                    }
                }
            };
            return sysService.RetrieveMultiple(query);
        }



    }
}
