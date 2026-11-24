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

namespace Reservations.ServiceProvider
{
    class ServiceProviderService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;


        public ServiceProviderService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }

        internal void UpdateEmployeeStatus(Entity preEntity,ref Entity entity, ref bool bIsOk, ref string sErrorMsg)
        {   //מרצה
            bool bPreLecturer = EntityUtils.GetBoolFromEntityValue("new_rsv_status_is_lecturer", preEntity);
            bool bPostLecturer = EntityUtils.GetBoolFromEntityValue("new_rsv_status_is_lecturer", entity);
            //עובד
            bool bPreEmployee = EntityUtils.GetBoolFromEntityValue("new_rsv_status_is_yv_employee", preEntity);
            bool bPostEmployee = EntityUtils.GetBoolFromEntityValue("new_rsv_status_is_yv_employee", entity);
            ////מדריך
            bool bPreGuide = EntityUtils.GetBoolFromEntityValue("new_rsv_status_is_guide", preEntity);
            bool bPostGuide = EntityUtils.GetBoolFromEntityValue("new_rsv_status_is_guide", entity);

            //WriteToLog.LogToFile("\n name ---"+EntityUtils.GetStringFromEntityValue("new_first_name_he",entity),"");
            //WriteToLog.LogToFile("\n PreLecturer " + bPreLecturer, "");
            //WriteToLog.LogToFile("\n PostLecturer " + bPostLecturer, "");
            //WriteToLog.LogToFile("\n PreEmployee " + bPreEmployee, "");
            //WriteToLog.LogToFile("\n PostEmployee " + bPostEmployee, "");
            //WriteToLog.LogToFile("\n PreGuide " + bPreGuide, "");
            //WriteToLog.LogToFile("\n PostGuide " + bPostGuide, "");
            //אם אחד מהשדות מרצה/עובד השתנה
    
            //    if (bPreLecturer.CompareTo(bPostLecturer) != 0 || bPreEmployee.CompareTo(bPostEmployee) != 0)
            if (entity.Contains("new_rsv_status_is_lecturer")|| entity.Contains("new_rsv_status_is_yv_employee"))
            {
               // WriteToLog.LogToFile("אם אחד מהשדות מרצה/עובד השתנה " , "");
                //אם אחד מהם חיובי
                if (bPostEmployee || bPostLecturer) {
                   // WriteToLog.LogToFile("אם אחד מהם חיובי", "");
                    entity["new_rsv_status_is_lecturer_or_yv_employee"] = true;
                }
                else if((entity.Contains("new_rsv_status_is_lecturer") && !entity.Contains("new_rsv_status_is_yv_employee") && !bPreEmployee )||
                        (entity.Contains("new_rsv_status_is_yv_employee") && !entity.Contains("new_rsv_status_is_lecturer") && !bPreLecturer))
                {
                    //WriteToLog.LogToFile("\n****************מרצה או עובד שלילי**************", "");
                    entity["new_rsv_status_is_lecturer_or_yv_employee"] = false;
                }
            }
            //אם אחד מהשדות מדריך/עובד השתנה 
            if (entity.Contains("new_rsv_status_is_guide") || entity.Contains("new_rsv_status_is_yv_employee"))
            {
                //WriteToLog.LogToFile("אם אחד מהשדות מדריך/עובד השתנה ", "");

                //אם אחד מהם חיובי
                if (bPostEmployee || bPostGuide)
                {
                   // WriteToLog.LogToFile("אם אחד מהם חיובי", "");

                    entity["new_rsv_status_is_guide_or_yv_employee"] = true;
                }
                else if((entity.Contains("new_rsv_status_is_guide") && !entity.Contains("new_rsv_status_is_yv_employee") && !bPreEmployee) ||
                        (entity.Contains("new_rsv_status_is_yv_employee") && !entity.Contains("new_rsv_status_is_guide") && !bPreGuide))
                {
                   // WriteToLog.LogToFile("\n****************מדריך או עובד שלילי**************", "");
                    entity["new_rsv_status_is_guide_or_yv_employee"] = false;
                }
            }


          

        }



        internal void UpdateFullName(Entity preEntity, ref Entity entity, ref bool bIsOk, ref string sErrorMsg)
        {

            //first name he
            string sPreFirstNameHe = EntityUtils.GetStringFromEntityValue("new_first_name_he", preEntity);
            string sPosFirstNameHe = EntityUtils.GetStringFromEntityValue("new_first_name_he", entity);
            //last name he
            string sPostLastNameHe = EntityUtils.GetStringFromEntityValue("new_last_name_he", entity);
            string sPreLastNameHe = EntityUtils.GetStringFromEntityValue("new_last_name_he", preEntity);

           
            //first name en
            string sPreFirstNameEn = EntityUtils.GetStringFromEntityValue("new_first_name_en", preEntity);
            string sPosFirstNameEn = EntityUtils.GetStringFromEntityValue("new_first_name_en", entity);
            //last name en
            string sPostLastNameEn = EntityUtils.GetStringFromEntityValue("new_last_name_en", entity);
            string sPreLastNameEn = EntityUtils.GetStringFromEntityValue("new_last_name_en", preEntity);
            string fullnameHe1 = (sPreFirstNameHe.CompareTo(sPosFirstNameHe) != 0 && sPosFirstNameHe!=string.Empty) ? sPosFirstNameHe : sPreFirstNameHe;
            string fullnameEn1 = (sPreFirstNameEn.CompareTo(sPosFirstNameEn) != 0 && sPosFirstNameEn!=string.Empty) ? sPosFirstNameEn : sPreFirstNameEn;
            string fullnameHe2 = (sPreLastNameHe.CompareTo(sPostLastNameHe) != 0 && sPostLastNameHe != string.Empty)? sPostLastNameHe : sPreLastNameHe;
            string fullnameEn2 = (sPreLastNameEn.CompareTo(sPostLastNameEn) != 0 && sPostLastNameEn != string.Empty) ? sPostLastNameEn : sPreLastNameEn;




            if (sPreFirstNameHe.CompareTo(sPosFirstNameHe) != 0 || sPreLastNameHe.CompareTo(sPostLastNameHe) != 0)
                entity["new_name"] = fullnameHe1 +" "+ fullnameHe2;
            if (sPreFirstNameEn.CompareTo(sPosFirstNameEn) != 0 || sPreLastNameEn.CompareTo(sPostLastNameEn) != 0)
                entity["new_full_name_en"] = fullnameEn1+" " + fullnameEn2;


        }

        internal void UpdateInactiveServiceProviderEntities(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            Guid myGuid = postEntity.Id;
            OptionSetValue myState = (OptionSetValue)postEntity.Attributes["statecode"];

            if(myState.Value == 1)
            {
                //"new_service_provider"
                var allLinkedEntities = GetEntityCollection("new_service_provider_status", "new_service_provider", myGuid.ToString(), new ColumnSet(true));
                for (int i = 0; i < allLinkedEntities.Entities.Count; i++)
                {
                    OptionSetValue currState = (OptionSetValue)allLinkedEntities[i]["statecode"];
                    if(currState.Value==0)
                    {
                        allLinkedEntities[i]["new_end_date"] = DateTime.Today;
                        sysService.Update(allLinkedEntities[i]);
                    }
                    

                }

            }
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


    }
}
