using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using BasicPlugIn;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
namespace BasicPlugIn
{
   public class MailTemplates
    {
        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;
      
        public MailTemplates(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }


        public Entity GetMailTemplateByCode(int iCode, string sLogBaseData)
        {

            Entity eRet_ = null;

            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_email_template";

            ColumnSet cols = new ColumnSet();
            cols.AllColumns = true;// AddColumns(new string[] { "incidentid", "title" });
            query.ColumnSet = cols;
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("new_code", ConditionOperator.Equal, iCode);


            EntityCollection ec = sysService.RetrieveMultiple(query);
            if(ec.Entities.Count>0)
            {
                eRet_ = (Entity) ec[0];

            }
            #endregion
            return eRet_;
        }

        public Guid CreateAnEmailByTemplateCode(int iCode, Guid gEntity,Guid gUserid, string sLogBaseData)
        {

            Guid gRet_ = Guid.Empty;
            Entity eTemplate = GetMailTemplateByCode(iCode, sLogBaseData);
            if (eTemplate != null)
            {
                string sEntitySchema = EntityUtils.GetStringFromEntityValue("new_entity_schema", eTemplate);
                if (sEntitySchema.Length > 0)
                {
                    Entity eBasicEntityAllData = GetBasicEntityData(sEntitySchema, gEntity, sLogBaseData);
                    if(eBasicEntityAllData!=null)
                    {
                        string sSubject = "";
                        string sDescription = "";

                        string sSenderType = "";
                        Guid gSenderId = Guid.Empty;

                        string sToType = "";
                        Guid gToId = Guid.Empty;

                        GetToData(ref sToType, ref gToId, eBasicEntityAllData, eTemplate, sLogBaseData);
                        sSubject = GetTextData(EntityUtils.GetStringFromEntityValue("new_subject", eTemplate), eBasicEntityAllData,  sLogBaseData);
                        sDescription = GetTextData(EntityUtils.GetStringFromEntityValue("new_description", eTemplate), eBasicEntityAllData,  sLogBaseData);

                      Guid gQueue=  EntityUtils.GetEntityReferenceFromEntityValue("new_from_queue", eTemplate);
                        if (gQueue.CompareTo(Guid.Empty) != 0)
                        {
                            sSenderType = "queue";
                            gSenderId = gQueue;
                        }
                        else
                        {
                            sSenderType = "systemuser";
                            gSenderId = gUserid;
                        }

                        #region create an email

                        Entity eEmail = new Entity("email");
                        eEmail["regardingobjectid"] = new EntityReference(sEntitySchema, eBasicEntityAllData.Id);

                        eEmail["subject"] = sSubject;
                        eEmail["description"] = sDescription;

                        if (sSenderType.Length > 0 && gSenderId.CompareTo(Guid.Empty) != 0)
                        {
                            Entity sender = new Entity("activityparty");
                            sender["partyid"] = new EntityReference(sSenderType, gSenderId);
                            eEmail["from"] = new Entity[] { sender };
                        }

                        if (sToType.Length > 0 && gToId.CompareTo(Guid.Empty) != 0)
                        {
                            EntityCollection colAPTo = new EntityCollection();
                            Entity activityPartyTo = new Entity("activityparty");
                            activityPartyTo["partyid"] = new EntityReference(sToType, gToId);
                            colAPTo.Entities.Add(activityPartyTo);
                            eEmail["to"] = colAPTo;
                        }
                         gRet_ = userService.Create(eEmail);

                        #endregion

                    }//if(eBasicEntityAllData!=null)

                }//if(sEntitySchema.Length>0)
            }// if (eTemplate != null)
            return gRet_;
        }//CreateAnEmailByTemplateCode

     

        private void GetToData(ref string sToType, ref Guid gToId, Entity eBasicEntityAllData, Entity eTemplate, string sLogBaseData)
        {
            EntityReference er = null;
            string sToAtt = EntityUtils.GetStringFromEntityValue("new_to", eTemplate);
            if (sToAtt.Length > 0)
            {
                er = GetEntityReferenceData(sToAtt, eBasicEntityAllData, sLogBaseData);

            }
            if (er == null)
            {
                sToAtt = EntityUtils.GetStringFromEntityValue("new_to2", eTemplate);
                if (sToAtt.Length > 0) { er = GetEntityReferenceData(sToAtt, eBasicEntityAllData, sLogBaseData); }
            }


            if (er != null)
            {
                sToType = er.LogicalName;
                gToId = er.Id;
            }
        }

        private EntityReference GetEntityReferenceData(string sToAtt, Entity eBasicEntityAllData,string sLogBaseData)
        {
            EntityReference er = null;
            int iStart = sToAtt.IndexOf('{');
            int iEnd = sToAtt.IndexOf('}');
            if (iStart == 0 && iEnd > 0)
            {

                string sParameter = sToAtt.Substring(iStart + 1, iEnd - iStart - 1);

                if (eBasicEntityAllData.Attributes.Contains(sParameter))
                {
                    string sType = eBasicEntityAllData.Attributes[sParameter].GetType().ToString();
                    switch (sType)
                    {

                        case "Microsoft.Xrm.Sdk.EntityReference":
                            er = EntityUtils.GetEntityReferenceFromEntity(sParameter, eBasicEntityAllData);
                            break;
                        default:
                            break;
                    }
                    //WriteToLog.LogToFile("GetTextData  sParameter=" + sParameter + " sType=" + sType, sLogBaseData);
                    //WriteToLog.LogToFile("GetTextData  sBasicData=" + sBasicData, sLogBaseData);
                }
            }
            return er;
        }

        private string GetTextData(string sBasicData, Entity eBasicEntityAllData, string sLogBaseData)
        {
           // WriteToLog.LogToFile("GetTextData Start sBasicData=" + sBasicData, sLogBaseData);
           
            int iStart = sBasicData.IndexOf('{');
            int iEnd = sBasicData.IndexOf('}');
            while (iStart > 0 && iEnd > 0)
            {

                string sParameter = sBasicData.Substring(iStart + 1, iEnd - iStart - 1);
                string sParameterType = "";
                if (sParameter.IndexOf('.') > 0)
                {
                    sParameterType = sParameter.Substring(sParameter.IndexOf('.') + 1);
                    sParameter = sParameter.Substring(0, sParameter.IndexOf('.'));
                }
                //WriteToLog.LogToFile("GetTextData  sParameter=" + sParameter + " sParameterType=" + sParameterType, sLogBaseData);
                if (eBasicEntityAllData.Attributes.Contains(sParameter))
                {
                    string sType = eBasicEntityAllData.Attributes[sParameter].GetType().ToString();
                    switch (sType)
                    {
                        case "System.String":
                            sBasicData = sBasicData.Replace("{" + sParameter + "}", EntityUtils.GetStringFromEntityValue(sParameter, eBasicEntityAllData));
                            sBasicData = sBasicData.Replace("\n", "</br>");
                            break;
                        case "Microsoft.Xrm.Sdk.EntityReference":
                            if (sParameterType == "Name"){sBasicData = sBasicData.Replace("{" + sParameter + ".Name}", EntityUtils.GetEntityReferenceFromEntityValueName(sParameter, eBasicEntityAllData)); }
                            else if (sParameterType == "Id") { sBasicData = sBasicData.Replace("{" + sParameter + ".Id}", EntityUtils.GetEntityReferenceFromEntityValue(sParameter, eBasicEntityAllData).ToString()); }
                            else if (sParameterType == "LogicalName") { sBasicData = sBasicData.Replace("{" + sParameter + ".LogicalName}", EntityUtils.GetEntityReferenceFromEntityLogicalName(sParameter, eBasicEntityAllData)); }
                            break;
                        default:
                            break;
                    }
                }
                iStart = sBasicData.IndexOf('{');
                iEnd = sBasicData.IndexOf('}');
            }//while (iStart>0 && iEnd>0)


            string sRet_ = sBasicData;
            return sRet_;
        }//GetTextData

        private Entity GetBasicEntityData(string sEntitySchema, Guid gEntity, string sLogBaseData)
        {
            WriteToLog.LogToFile("GetBasicEntityData Start sEntitySchema=" + sEntitySchema + " gEntity="+ gEntity, sLogBaseData);

            Entity eRet_ = null;

            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = sEntitySchema;

            ColumnSet cols = new ColumnSet();
            cols.AllColumns = true;// AddColumns(new string[] { "incidentid", "title" });
            query.ColumnSet = cols;
            query.Criteria.AddCondition(sEntitySchema+"id", ConditionOperator.Equal, gEntity);


            EntityCollection ec = sysService.RetrieveMultiple(query);

            if (ec.Entities.Count > 0)
            {
                eRet_ = (Entity)ec[0];

            }
            #endregion
            return eRet_;
        }
    }
}
