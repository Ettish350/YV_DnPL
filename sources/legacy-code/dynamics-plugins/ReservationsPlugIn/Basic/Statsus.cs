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
    public class Status
    {
        private static DataTable dtStatus = null;
        public enum StatusType { Incident = 1};

        public static Guid GetStatusIdByCodeAndType(int iCode, int iType, IOrganizationService service)
        {
            Guid gRet_ = Guid.Empty;


            if (dtStatus == null)
            {
                InitStatusTable(service);
            }//if (dtStatus == null)

            if (dtStatus != null)
            {
                DataView dv = new DataView(dtStatus);
               
                dv.RowFilter = "Code='" + iCode + "' and Type='" + iType + "'";
               if (dv.Count > 0)
                {
                    gRet_ = Guid.Parse( dv[0]["Id"].ToString());
                }
            }
            return gRet_;
        }//GetStatusByKey

        public static Guid GetStatusIdByCodeAndType(int iCode,int iType, OrganizationServiceProxy service)
        {
            Guid gRet_ = Guid.Empty;


            if (dtStatus == null)
            {
                InitStatusTable(service);
            }//if (dtStatus == null)

            if (dtStatus != null)
            {
                DataView dv = new DataView(dtStatus);
                dv.RowFilter = "Code='" + iCode + "' and Type='" + iType + "'";
                if (dv.Count > 0)
                {
                    gRet_ = Guid.Parse(dv[0]["Id"].ToString());
                }
            }
            return gRet_;
        }//GetStatusByKey



        public static int GetStatusCodeById(Guid gStatus, IOrganizationService service)
        {
            int iRet_ = 0;


            if (dtStatus == null)
            {
                InitStatusTable(service);
            }//if (dtStatus == null)

            if (dtStatus != null)
            {
                DataView dv = new DataView(dtStatus);
                dv.RowFilter = "Id='" + gStatus + "'";
                if (dv.Count > 0)
                {
                    iRet_ =int.Parse( dv[0]["Code"].ToString());
                }
            }
            return iRet_;
        }//GetStatusByKey

        public static int GetStatusCodeById(Guid gStatus, OrganizationServiceProxy service)
        {
            int iRet_ = 0;


            if (dtStatus == null)
            {
                InitStatusTable(service);
            }//if (dtStatus == null)

            if (dtStatus != null)
            {
                DataView dv = new DataView(dtStatus);
                dv.RowFilter = "Id='" + gStatus + "'";
                if (dv.Count > 0)
                {
                    iRet_ = int.Parse(dv[0]["Code"].ToString());
                }
            }
            return iRet_;
        }//GetStatusByKey


        public static void InitStatusTable(OrganizationServiceProxy service)
        {
            dtStatus = new DataTable();
            dtStatus.Columns.Add(new DataColumn("Name"));
            dtStatus.Columns.Add(new DataColumn("Code"));
            dtStatus.Columns.Add(new DataColumn("Type"));
            dtStatus.Columns.Add(new DataColumn("Id"));
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_status";

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
                string sName = EntityUtils.GetStringFromEntityValue("new_name", item);
                string iCode = EntityUtils.GetIntFromEntityValue("new_code", item).ToString();
                string iType = EntityUtils.GetOptionSetValueFromEntityValue("new_type", item).ToString();
                DataRow dr = dtStatus.NewRow();
                dr["Name"] = sName;
                dr["Code"] = iCode;
                dr["Type"] = iType;
                dr["Id"] = item.Id;
                dtStatus.Rows.Add(dr);
            }
        }//InitStatusTable

        public static void InitStatusTable(IOrganizationService service)
        {
            dtStatus = new DataTable();
            dtStatus.Columns.Add(new DataColumn("Name"));
            dtStatus.Columns.Add(new DataColumn("Code"));
            dtStatus.Columns.Add(new DataColumn("Type"));
            dtStatus.Columns.Add(new DataColumn("Id"));
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = "new_status";

            ColumnSet columns = new ColumnSet(true);

            query.ColumnSet = columns;

            RetrieveMultipleRequest retrieve = new RetrieveMultipleRequest();
            retrieve.Query = query;
            RetrieveMultipleResponse retrieved = (RetrieveMultipleResponse)service.Execute(retrieve);
            #endregion
            foreach (Entity item in retrieved.EntityCollection.Entities)
            {
                string sName = EntityUtils.GetStringFromEntityValue("new_name", item);
                string iCode = EntityUtils.GetIntFromEntityValue("new_code", item).ToString();
                string iType = EntityUtils.GetOptionSetValueFromEntityValue("new_type", item).ToString();
                DataRow dr = dtStatus.NewRow();
                dr["Name"] = sName;
                dr["Code"] = iCode;
                dr["Type"] = iType;
                dr["Id"] = item.Id;
                dtStatus.Rows.Add(dr);
            }
        }//InitStatusTable
    }
}
