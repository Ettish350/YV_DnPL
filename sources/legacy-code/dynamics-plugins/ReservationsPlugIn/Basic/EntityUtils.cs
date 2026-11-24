using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Messages;

namespace BasicPlugIn
{
    public class EntityUtils
    {
        #region Double
        public static double GetDoubleFromEntityValue(string sAttribute, Entity Entity)
        {
            double dRet_ = 0;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is Double)
            {

                dRet_ = (Double)Entity.Attributes[sAttribute];
            }
            return dRet_;
        }//GetDoubleFromEntityValue
        #endregion Double

        #region EntityReference
        public static EntityReference GetEntityReferenceFromEntity(string sAttribute, Entity Entity)
        {
            EntityReference erRet_ = null;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is EntityReference)
            {

                erRet_ = (EntityReference)Entity.Attributes[sAttribute];
            }
            return erRet_;
        }//GetEntityReferenceFromEntityValue


        public static Guid GetEntityReferenceFromEntityValue(string sAttribute, Entity Entity)
        {
            Guid gRet_ = Guid.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is EntityReference)
            {

                gRet_ = ((EntityReference)Entity.Attributes[sAttribute]).Id;
            }
            return gRet_;
        }//GetEntityReferenceFromEntityValue

        public static string GetEntityReferenceFromEntityLogicalName(string sAttribute, Entity Entity)
        {
            string sRet_ = ""; ;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is EntityReference)
            {

                sRet_ = ((EntityReference)Entity.Attributes[sAttribute]).LogicalName;
            }
            return sRet_;
        }//GetEntityReferenceFromEntityValue

        public static string GetEntityReferenceFromEntityValueName(string sAttribute, Entity Entity)
        {
            string sRet_ = string.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is EntityReference)
            {
                if (((EntityReference)Entity.Attributes[sAttribute]).Name != null)
                {
                    sRet_ = ((EntityReference)Entity.Attributes[sAttribute]).Name;
                }
            }
            return sRet_;
        }//GetEntityReferenceFromEntityValueName

       
        #endregion EntityReference



        #region OptionSetValue

        public static OptionSetValue GetOptionSetValueFromEntity(string sAttribute, Entity Entity)
        {
            OptionSetValue osRet_ = null;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is OptionSetValue)
            {

                osRet_ = (OptionSetValue)Entity.Attributes[sAttribute];
            }
            return osRet_;
        }//GetOptionSetValueFromEntityValue

        public static int GetOptionSetValueFromEntityValue(string sAttribute, Entity Entity)
        {
            int iRet_ = 0;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is OptionSetValue)
            {

                iRet_ = ((OptionSetValue)Entity.Attributes[sAttribute]).Value;
            }
            return iRet_;
        }//GetOptionSetValueFromEntityValue

        public static string GetOptionSetValueFromEntityValueName(string sAttribute, Entity Entity, IOrganizationService service)
        {
            string sRet_ = string.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is OptionSetValue)
            {

                int iRet_ = ((OptionSetValue)Entity.Attributes[sAttribute]).Value;
                switch (sAttribute)
                {
                    case "statuscode":
                        sRet_ = GetOptionsSetTextOnValueForStatus(service, Entity.LogicalName, sAttribute, iRet_);
                        break;
                    case "statecode":
                        sRet_ = GetOptionsSetTextOnValueForState(service, Entity.LogicalName, sAttribute, iRet_);
                        break;
                    default:
                        sRet_ = GetOptionsSetTextOnValue(service, Entity.LogicalName, sAttribute, iRet_);
                        break;
                }
            }
            return sRet_;
        }//GetOptionSetValueFromEntityValueName
        private static string GetOptionsSetTextOnValue(IOrganizationService service, string entityName, string attributeName, int selectedValue)
        {

            RetrieveAttributeRequest retrieveAttributeRequest = new
             RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };
            // Execute the request.
            RetrieveAttributeResponse retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);
            // Access the retrieved attribute.

            Microsoft.Xrm.Sdk.Metadata.PicklistAttributeMetadata retrievedPicklistAttributeMetadata = (Microsoft.Xrm.Sdk.Metadata.PicklistAttributeMetadata)

            retrieveAttributeResponse.AttributeMetadata;// Get the current options list for the retrieved attribute.
            OptionMetadata[] optionList = retrievedPicklistAttributeMetadata.OptionSet.Options.ToArray();
            string selectedOptionLabel = string.Empty;
            foreach (OptionMetadata oMD in optionList)
            {
                if (oMD.Value == selectedValue)
                {
                    selectedOptionLabel = oMD.Label.UserLocalizedLabel.Label;


                }


            }
            return selectedOptionLabel;
        }
        private static string GetOptionsSetTextOnValueForStatus(IOrganizationService service, string entityName, string attributeName, int selectedValue)
        {

            RetrieveAttributeRequest retrieveAttributeRequest = new
             RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };
            // Execute the request.
            RetrieveAttributeResponse retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);
            // Access the retrieved attribute.

            Microsoft.Xrm.Sdk.Metadata.StatusAttributeMetadata retrievedPicklistAttributeMetadata = (Microsoft.Xrm.Sdk.Metadata.StatusAttributeMetadata)

            retrieveAttributeResponse.AttributeMetadata;// Get the current options list for the retrieved attribute.
            OptionMetadata[] optionList = retrievedPicklistAttributeMetadata.OptionSet.Options.ToArray();
            string selectedOptionLabel = string.Empty;
            foreach (OptionMetadata oMD in optionList)
            {
                if (oMD.Value == selectedValue)
                {
                    selectedOptionLabel = oMD.Label.UserLocalizedLabel.Label;


                }


            }
            return selectedOptionLabel;
        }
        private static string GetOptionsSetTextOnValueForState(IOrganizationService service, string entityName, string attributeName, int selectedValue)
        {

            RetrieveAttributeRequest retrieveAttributeRequest = new
             RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };
            // Execute the request.
            RetrieveAttributeResponse retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);
            // Access the retrieved attribute.

            Microsoft.Xrm.Sdk.Metadata.StateAttributeMetadata retrievedPicklistAttributeMetadata = (Microsoft.Xrm.Sdk.Metadata.StateAttributeMetadata)

            retrieveAttributeResponse.AttributeMetadata;// Get the current options list for the retrieved attribute.
            OptionMetadata[] optionList = retrievedPicklistAttributeMetadata.OptionSet.Options.ToArray();
            string selectedOptionLabel = string.Empty;
            foreach (OptionMetadata oMD in optionList)
            {
                if (oMD.Value == selectedValue)
                {
                    selectedOptionLabel = oMD.Label.UserLocalizedLabel.Label;


                }


            }
            return selectedOptionLabel;
        }

        #endregion OptionSetValue

        #region bool

        public static bool GetBoolFromEntityValue(string sAttribute, Entity Entity)
        {
            bool bRet_ = false;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is bool)
            {

                bRet_ = (bool)Entity.Attributes[sAttribute];
            }
            return bRet_;
        }//GetboolFromEntity

        public static string GetBoolFromEntityValueName(string sAttribute, Entity Entity, IOrganizationService service)
        {
            string sRet_ = string.Empty;

            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is bool)
            {

                bool bRet_ = (bool)Entity.Attributes[sAttribute];
                sRet_ = GetBooleadnTextOnValue(service, Entity.LogicalName, sAttribute, bRet_);

            }
            return sRet_;
        }//GetboolFromEntity

        private static string GetBooleadnTextOnValue(IOrganizationService service, string entityName, string attributeName, bool bValue)
        {
            string sRet_ = "";
            RetrieveAttributeRequest retrieveAttributeRequest = new
             RetrieveAttributeRequest
            {
                EntityLogicalName = entityName,
                LogicalName = attributeName,
                RetrieveAsIfPublished = true
            };
            // Execute the request.
            RetrieveAttributeResponse retrieveAttributeResponse = (RetrieveAttributeResponse)service.Execute(retrieveAttributeRequest);
            // Access the retrieved attribute.

            Microsoft.Xrm.Sdk.Metadata.BooleanAttributeMetadata retrievedPicklistAttributeMetadata = (Microsoft.Xrm.Sdk.Metadata.BooleanAttributeMetadata)

            retrieveAttributeResponse.AttributeMetadata;// Get the current options list for the retrieved attribute.
            if (bValue == false)
            {
                sRet_ = retrievedPicklistAttributeMetadata.OptionSet.FalseOption.Label.UserLocalizedLabel.Label;
            }
            else
            {
                sRet_ = retrievedPicklistAttributeMetadata.OptionSet.TrueOption.Label.UserLocalizedLabel.Label;
            }

            return sRet_;
        }


        #endregion bool

        #region date
        public static DateTime? GetDateTimeFromEntity(string sAttribute, Entity Entity)
        {
            DateTime? btRet_ = null;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is DateTime)
            {

                btRet_ = (DateTime)Entity.Attributes[sAttribute];

                DateTime dtTime = (DateTime)Entity.Attributes[sAttribute];
                int iDay = dtTime.Day + 1;
                btRet_ = dtTime.ToLocalTime();


            }
            return btRet_;
        }//GetDateTimeFromEntity

        public static string GetDateTimeFromEntityValue(string sAttribute, Entity Entity)
        {
            string bRet_ = string.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is DateTime)
            {

                bRet_ = ((DateTime)Entity.Attributes[sAttribute]).ToString("s");
            }
            return bRet_;
        }//GetDateTimeFromEntityValue

        public static string GetDateTimeFromEntityValueLocalTime(string sAttribute, Entity Entity)
        {
            string bRet_ = string.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is DateTime)
            {

                bRet_ = ((DateTime)Entity.Attributes[sAttribute]).ToLocalTime().ToString("s");
            }
            return bRet_;
        }//GetDateTimeFromEntityValue
        public static string GetDateTimeFromEntityValueName(string sAttribute, Entity Entity)
        {
            string bRet_ = string.Empty;
            if (Entity != null &&  Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is DateTime)
            {
                DateTime dtTime = (DateTime)Entity.Attributes[sAttribute];
                int iDay = dtTime.Day + 1;
                bRet_ = dtTime.ToLocalTime().ToString();
            }
            return bRet_;
        }//GetDateTimeFromEntityValue

        public static string GetDateTimeFromEntityValueDateName(string sAttribute, Entity Entity)
        {
            string sRet_ = string.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is DateTime)
            {
                DateTime dtTime = (DateTime)Entity.Attributes[sAttribute];
                int iDay = dtTime.Day + 1;
                sRet_ = dtTime.ToLocalTime().ToString().Split(' ')[0];
            }
            return sRet_;
        }//GetDateTimeFromEntityValue
        #endregion date



        #region Money

        public static decimal GetMoneyFromEntityValue(string sAttribute, Entity Entity)
        {
            decimal bRet_ = 0;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is Money)
            {

                bRet_ = ((Money)Entity.Attributes[sAttribute]).Value;
            }
            return bRet_;
        }//GetMoneyFromEntityValue
        /*
        public static string GetMoneyFromEntityName(string sAttribute, Entity Entity)
        {
            string bRet_ = string.Empty;
            if (Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is Money)
            {

                bRet_ = ((Money)Entity.Attributes[sAttribute]).formattedvalue;
            }
            return bRet_;
        }//GetStatusFromEntityName*/

        #endregion Status



        #region String
        public static string GetStringFromEntityValue(string sAttribute, Entity Entity)
        {

            string sRet_ = string.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is String)
            {

                sRet_ = ((string)Entity.Attributes[sAttribute]);
            }
            return sRet_;

        }//GetStringFromEntityValue

        internal static string GetStringAliasedFromEntityValue(string sAttribute, Entity Entity)
        {

            string sRet_ = string.Empty;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is AliasedValue &&
                   ((AliasedValue) Entity.Attributes[sAttribute]).Value is String)
            {

                sRet_ = ((string)((AliasedValue)Entity.Attributes[sAttribute]).Value);
            }
            return sRet_;
        }
        #endregion Nvarchar

        #region int
        public static int GetIntFromEntityValue(string sAttribute, Entity Entity)
        {
            int iRet_ = 0;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is int)
            {

                iRet_ = (int)Entity.Attributes[sAttribute];
            }
            return iRet_;
        }//GetintFromEntityValue
        #endregion int

        #region decimal
        public static decimal GetDecimalFromEntityValue(string sAttribute, Entity Entity)
        {
            decimal dRet_ = 0;

            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is decimal)
            {

                dRet_ = (decimal)Entity.Attributes[sAttribute];
            }
            return dRet_;
        }//GetDoubleFromEntity
        #endregion decimal


          public static EntityReference GetEntityPartyFirstEntityReference(string sAttribute, Entity Entity)
        {
            EntityReference erRet_ = null;
            if (Entity != null && Entity.Attributes.Contains(sAttribute) &&
                    Entity.Attributes[sAttribute] != null &&
                    Entity.Attributes[sAttribute] is EntityCollection )
            {
                if (((EntityCollection)Entity.Attributes[sAttribute]).Entities.Count>0)
                {
                   if(((EntityCollection)Entity.Attributes[sAttribute]).Entities[0].Contains("partyid"))
                   {
                       erRet_ = (EntityReference)((EntityCollection)Entity.Attributes[sAttribute]).Entities[0].Attributes["partyid"];
                   }
                }
            }
            return erRet_;
        }//GetEntityReferenceFromEntityValueName


       




    }
}
