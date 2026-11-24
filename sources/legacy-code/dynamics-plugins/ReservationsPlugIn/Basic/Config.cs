using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BasicPlugIn
{
    public class Config
    {
        private static DataTable dtConfig = null;
        public static string GetConfigByKey(string Key, IOrganizationService service)
        {
            string sRet_ = "";
            if (dtConfig == null)
            {
                InitConfigTable(service);
            }//if (dtConfig == null)

            DataView dv = new DataView(dtConfig);
            dv.RowFilter = "Key='" + Key + "'";
            if (dv.Count > 0)
            {
                sRet_ = dv[0]["Value"].ToString();
            }
            return sRet_;
        }//GetConfigByKey

        public static string GetConfigByKey(string Key, OrganizationServiceProxy service)
        {
            string sRet_ = "";


            if (dtConfig == null)
            {
                InitConfigTable(service);
            }//if (dtConfig == null)

            if (dtConfig != null)
            {
                DataView dv = new DataView(dtConfig);
                dv.RowFilter = "Key='" + Key + "'";
                if (dv.Count > 0)
                {
                    sRet_ = dv[0]["Value"].ToString();
                }
            }
            return sRet_;
        }//GetConfigByKey

        public static void InitConfigTable(OrganizationServiceProxy service)
        {
            dtConfig = new DataTable();
            dtConfig.Columns.Add(new DataColumn("Key"));
            dtConfig.Columns.Add(new DataColumn("Value"));
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_configuration_setting";

            ColumnSet columns = new ColumnSet(true);

            query.ColumnSet = columns;

            query.Criteria = new FilterExpression();
            query.Criteria.FilterOperator = LogicalOperator.And;

            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = "statecode";
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(0);


            query.Criteria.Conditions.Add(condition1);

            RetrieveMultipleRequest retrieve = new RetrieveMultipleRequest();
            retrieve.Query = query;
            RetrieveMultipleResponse retrieved = (RetrieveMultipleResponse)service.Execute(retrieve);
            #endregion
            foreach (Entity item in retrieved.EntityCollection.Entities)
            {
                string sKey = EntityUtils.GetStringFromEntityValue("new_key", item);
                string sValue = EntityUtils.GetStringFromEntityValue("new_value", item);
                DataRow dr = dtConfig.NewRow();
                dr["Key"] = sKey;
                dr["Value"] = sValue;
                dtConfig.Rows.Add(dr);
            }
        }//InitConfigTable

        public static void InitConfigTable(IOrganizationService service)
        {
            dtConfig = new DataTable();
            dtConfig.Columns.Add(new DataColumn("Key"));
            dtConfig.Columns.Add(new DataColumn("Value"));
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_configuration_setting";

            ColumnSet columns = new ColumnSet(true);

            query.ColumnSet = columns;

            RetrieveMultipleRequest retrieve = new RetrieveMultipleRequest();
            retrieve.Query = query;
            RetrieveMultipleResponse retrieved = (RetrieveMultipleResponse)service.Execute(retrieve);
            #endregion
            foreach (Entity item in retrieved.EntityCollection.Entities)
            {
                string sKey = EntityUtils.GetStringFromEntityValue("new_key", item);
                string sValue = EntityUtils.GetStringFromEntityValue("new_value", item);
                DataRow dr = dtConfig.NewRow();
                dr["Key"] = sKey;
                dr["Value"] = sValue;
                dtConfig.Rows.Add(dr);
            }
        }//InitConfigTable
    }
}
