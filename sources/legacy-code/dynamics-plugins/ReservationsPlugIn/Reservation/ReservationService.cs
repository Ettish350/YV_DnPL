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

namespace Reservations.Reservation
{
    class ReservationService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;

     
        public ReservationService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }



        internal void SetNumerator(Entity preEntity, Entity postEntity,ref bool bIsUpdate , ref Entity eEntity)
        {
            Guid gPreBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", preEntity);
            Guid gPostBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", postEntity);
            if(gPreBudgetFramework.CompareTo(gPostBudgetFramework)!=0 && gPostBudgetFramework.CompareTo(Guid.Empty)!=0)
            {
                #region query
                QueryExpression query = new QueryExpression();
                query.EntityName = "new_budget_framework";

                ColumnSet cols = new ColumnSet();
                cols.AddColumns(new string[] { "new_budget_frameworkid", "new_rsv_department" });
                query.ColumnSet = cols;

                
                FilterExpression fe = new FilterExpression();
                fe.FilterOperator = LogicalOperator.And;
                fe.Conditions.Add(new ConditionExpression("new_budget_frameworkid", ConditionOperator.Equal, gPostBudgetFramework));
                query.Criteria.AddFilter(fe);

                LinkEntity le = new LinkEntity("new_budget_framework", "new_rsv_department", "new_rsv_department", "new_rsv_departmentid", JoinOperator.Inner);
                le.Columns.AddColumn("new_prefix");
                le.EntityAlias = "rsvd";
                query.LinkEntities.Add(le);
                RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                requestMult.Query = query;

                RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;

                
                #endregion
                if(respMult.EntityCollection.Entities.Count>0)
                {
                    Entity eItem = respMult.EntityCollection.Entities[0];
                    
                    string sPreFix = EntityUtils.GetStringAliasedFromEntityValue("rsvd.new_prefix",eItem);
                    string sNum = EntityUtils.GetStringFromEntityValue("new_num", postEntity);
                    if(sNum.Length>3)
                    {
                        sNum = sPreFix + sNum.Substring(3);
                        eEntity["new_num"] = sNum;
                        bIsUpdate = true;
                    }
                }
            }
        }

        internal void SetAutomaticInvoiceParam(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            Guid gPostBudgetFramework = EntityUtils.GetEntityReferenceFromEntityValue("new_budget_framework", postEntity);
            Guid gPostRsvInstitution = EntityUtils.GetEntityReferenceFromEntityValue("new_rsv_institution", postEntity);
 

            Entity eBudget = null, eInstitution = null;
            bool isBudgetAutomaticInvoice, isInstitutionNotAutomaticInvoice;

            if (gPostBudgetFramework.CompareTo(Guid.Empty) != 0)
                eBudget = GetEntityByGuid("new_budget_framework", gPostBudgetFramework, new ColumnSet(true));
            if (gPostRsvInstitution.CompareTo(Guid.Empty) != 0)
                eInstitution = GetEntityByGuid("new_rsv_institution", gPostRsvInstitution, new ColumnSet(true));

            if (eBudget == null)
                WriteToLog.LogToFile("SetAutomaticInvoiceParam : No BudgetID", "\n");
            if (eInstitution == null)
                WriteToLog.LogToFile("SetAutomaticInvoiceParam : No InstitutionID", "\n");

            if ((eBudget != null) && (eInstitution != null))
            {
                isBudgetAutomaticInvoice = EntityUtils.GetBoolFromEntityValue("new_generate_send_automatic_invoice", eBudget);
                isInstitutionNotAutomaticInvoice = EntityUtils.GetBoolFromEntityValue("new_do_not_generate_automatic_invoice", eInstitution);
                WriteToLog.LogToFile("SetAutomaticInvoiceParam : BudgetID=" + eBudget.Id + ", isBudgetAutomaticInvoice=" + isBudgetAutomaticInvoice.ToString(), "\n");
                WriteToLog.LogToFile("SetAutomaticInvoiceParam : InstitutionID=" + eInstitution.Id + ", isInstitutionNotAutomaticInvoice=" + isInstitutionNotAutomaticInvoice.ToString(), "\n");

                if (isBudgetAutomaticInvoice)
                    if (!isInstitutionNotAutomaticInvoice)
                    {
                        eEntity["new_generate_send_automatic_invoice"] = true;
                        bIsUpdate = true;
                        WriteToLog.LogToFile("SetAutomaticInvoiceParam updated the new_generate_send_automatic_invoice to be True", "\n");
                    }
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
