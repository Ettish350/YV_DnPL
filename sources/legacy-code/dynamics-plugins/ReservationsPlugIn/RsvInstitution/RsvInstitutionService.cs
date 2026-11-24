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

namespace Reservations.RsvInstitution
{
    class RsvInstitutionService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;

     
        public RsvInstitutionService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }



        internal void CheckId(Entity preEntity, Entity entity, ref bool bIsOk, ref string sErrorMsg)
        {
            string sInstitutionSymbol = string.Empty;
            string sCompanyNumber = string.Empty;
            Guid gPayingInstitution = Guid.Empty;
            if (entity.Contains("new_institution_symbol")) { sInstitutionSymbol = EntityUtils.GetStringFromEntityValue("new_institution_symbol", entity); }
            else { sInstitutionSymbol = EntityUtils.GetStringFromEntityValue("new_institution_symbol", preEntity); }
            if (entity.Contains("new_company_number")) { sCompanyNumber = EntityUtils.GetStringFromEntityValue("new_company_number", entity); }
            else { sCompanyNumber = EntityUtils.GetStringFromEntityValue("new_company_number", preEntity); }
            if (entity.Contains("new_paying_institution")) { gPayingInstitution = EntityUtils.GetEntityReferenceFromEntityValue("new_paying_institution", entity); }
            else { gPayingInstitution = EntityUtils.GetEntityReferenceFromEntityValue("new_paying_institution", preEntity); }
            if (sInstitutionSymbol.Length == 0 && sCompanyNumber.Length == 0)
            {
                bool bIsNotWithCode = false;
                if (entity.Contains("new_is_not_with_code")) { bIsNotWithCode = EntityUtils.GetBoolFromEntityValue("new_is_not_with_code", entity); }
                else { bIsNotWithCode = EntityUtils.GetBoolFromEntityValue("new_is_not_with_code", preEntity); }

                if (bIsNotWithCode == false)
                {
                    bIsOk = false;
                    sErrorMsg = "חובה למלא ח.פ. או סמל מוסד";
                }
            }
            else
            {
                if (bIsOk == true && sInstitutionSymbol.Length > 0)
                {
                    CheckInstitutionSymbol(sInstitutionSymbol, entity, gPayingInstitution, ref bIsOk, ref sErrorMsg);
                }
                if (bIsOk == true && sCompanyNumber.Length > 0)
                {
                    CheckCompanyNumber(sCompanyNumber, entity, gPayingInstitution, ref bIsOk, ref sErrorMsg);

                }

            }
        }
        private void CheckInstitutionSymbol(string CheckInstitutionSymbol, Entity entity,Guid gPayingInstitution, ref bool bIsOk, ref string sErrorMsg)
        {
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_rsv_institution";

            ColumnSet cols = new ColumnSet();
            cols.AllColumns = true;// AddColumns(new string[] { "incidentid", "title" });
            query.ColumnSet = cols;

            FilterExpression filter = new FilterExpression();
            filter.AddCondition("new_institution_symbol", ConditionOperator.Equal, CheckInstitutionSymbol);
            filter.AddCondition("statecode", ConditionOperator.Equal, 0);
            filter.AddCondition("new_rsv_institutionid", ConditionOperator.NotEqual, entity.Id);
            filter.FilterOperator = LogicalOperator.And;
            query.Criteria = filter;

            RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
            requestMult.Query = query;

            RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
            #endregion
            if (respMult.EntityCollection.Entities.Count > 0)
            {
                bIsOk = false;
                sErrorMsg = "קיים מוסד עם סמל מוסד נוכחי";
                foreach (Entity item in respMult.EntityCollection.Entities)
                {
                    if (item.Id.CompareTo(gPayingInstitution)==0)
                    {
                        bIsOk = true;
                        sErrorMsg = string.Empty;
                    }
                }
            }

        }
        private void CheckCompanyNumber(string sCompanyNumber, Entity entity,Guid gPayingInstitution, ref bool bIsOk, ref string sErrorMsg)
        {
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_rsv_institution";

            ColumnSet cols = new ColumnSet();
            cols.AllColumns = true;// AddColumns(new string[] { "incidentid", "title" });
            query.ColumnSet = cols;

            FilterExpression filter = new FilterExpression();
            filter.AddCondition("new_company_number", ConditionOperator.Equal, sCompanyNumber);
            filter.AddCondition("statecode", ConditionOperator.Equal, 0);
            filter.AddCondition("new_rsv_institutionid", ConditionOperator.NotEqual, entity.Id);
            filter.FilterOperator = LogicalOperator.And;
            query.Criteria = filter;

            RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
            requestMult.Query = query;

            RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;
            #endregion
            if ( respMult.EntityCollection.Entities.Count>0)
            {

                bIsOk = false;
                sErrorMsg = "קיים מוסד עם ח.פ. נוכחי";
                foreach (Entity item in respMult.EntityCollection.Entities)
                {
                    if (item.Id.CompareTo(gPayingInstitution) == 0)
                    {
                        bIsOk = true;
                        sErrorMsg = string.Empty;
                    }
                }
            }

        }


        internal void SetUsername(Entity entity)
        {
            Entity deNumerator = gf.GetNextNumerator(entity.LogicalName);
            if (deNumerator != null)
            {
                #region Get numerator data 
                string sAttributeName = EntityUtils.GetStringFromEntityValue("new_attribute_name", deNumerator);
                string sPreFix = EntityUtils.GetStringFromEntityValue("new_pre_fix", deNumerator);
                int iNumeratorNumberLength = EntityUtils.GetIntFromEntityValue("new_numerator_number_length", deNumerator);
                int iNextNumber = EntityUtils.GetIntFromEntityValue("new_next_number", deNumerator);
                #endregion

                //we have data for numerator
                if (iNextNumber > 0 && sAttributeName.Length > 0)
                {
                    string sNumerator = iNextNumber.ToString();

                    while (sNumerator.Length < iNumeratorNumberLength)
                    {
                        sNumerator = "0" + sNumerator;
                    }
                    //add entity numerator
                    entity[sAttributeName] = sPreFix + sNumerator;

                    #region save numerator entity in SharedVariables

                    //context.SharedVariables.Add(entity.LogicalName + "CurrentSequenceNumber", deNumerator);
                    deNumerator["new_next_number"] = iNextNumber + 1;
                    userService.Update(deNumerator);
                    //crmService.Update(entity);
                    #endregion
                }
            }//if (deNumerator != null)
        }

        internal void SetPassword(Entity entity)
        {
            string sNewPass = CreatePassword(3) + CreatePassword1(3);
            entity["new_password"] = sNewPass;
        }
        #region password
        public string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyz";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }
        public string CreatePassword1(int length)
        {
            const string valid = "1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

       /* public Entity GetNextNumerator(string entityName, IOrganizationService systemService)
        {
            Entity entity = null;
            //   try
            {
                // Query for next sequence number from enumerators entity
                QueryExpression query = new QueryExpression();
                query.EntityName = "new_numerator";

                ColumnSet cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_entity_name", "new_attribute_name", "new_next_number", "new_pre_fix", "new_numerator_number_length" });
                query.ColumnSet = cols;

                query.Criteria.AddCondition("new_entity_name", ConditionOperator.Equal, entityName);

                EntityCollection ec = systemService.RetrieveMultiple(query);

                if (ec.Entities.Count > 0)
                {
                    entity = ec.Entities[0];
                }

            }

            return entity;
        }*/

        #endregion





    }
}
