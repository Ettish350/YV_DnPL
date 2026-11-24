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

namespace Reservations.BudgetFramework
{
    class BudgetFrameworkService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;


        public BudgetFrameworkService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }



        internal void SetAlmogInterface(Entity preEntity, Entity postEntity, ref bool bIsUpdate, ref Entity eEntity)
        {
            string sPreRevenueSection = EntityUtils.GetStringFromEntityValue("new_revenue_section", preEntity);
            string sPostRevenueSection = EntityUtils.GetStringFromEntityValue("new_revenue_section", postEntity);

            if (sPreRevenueSection.CompareTo(sPostRevenueSection) != 0)
            {
                bIsUpdate = true;

                if (sPostRevenueSection == string.Empty)
                {
                    eEntity["new_almog_interface"] = null;

                }
                else
                {
                    #region query
                    QueryExpression query = new QueryExpression();
                    query.EntityName = "new_almog_interface";

                    ColumnSet cols = new ColumnSet();
                    cols.AddColumns(new string[] { "new_almog_interfaceid", "new_name","new_almog_sub_code" });
                    query.ColumnSet = cols;


                    FilterExpression fe = new FilterExpression();
                    fe.FilterOperator = LogicalOperator.And;
                    fe.Conditions.Add(new ConditionExpression("new_almog_sub_code", ConditionOperator.Equal, sPostRevenueSection));
                    query.Criteria.AddFilter(fe);


                    RetrieveMultipleRequest requestMult = new RetrieveMultipleRequest();
                    requestMult.Query = query;

                    RetrieveMultipleResponse respMult = sysService.Execute(requestMult) as RetrieveMultipleResponse;


                    #endregion
                    if (respMult.EntityCollection.Entities.Count > 0)
                    {

                        Entity eItem = respMult.EntityCollection.Entities[0];

                     //   eEntity["new_almog_interface"] = new EntityReference("new_almog_interface", eItem.Id);
                        eEntity["new_almog_interface"] = new EntityReference(eItem.LogicalName, eItem.Id);
                       
                    }
                    else
                        eEntity["new_almog_interface"] = null;

                }


            }





        }
    }
}
