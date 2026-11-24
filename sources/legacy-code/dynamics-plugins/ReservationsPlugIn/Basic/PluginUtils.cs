using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace BasicPlugIn
{
    public class PluginUtils
    {
        /*

        /// <summary>
        /// Gets CRM Service with current user id
        /// </summary>
        /// <param name="context">current context</param>
        /// <returns>Microsoft.Xrm.SdkTypeProxy.CrmService </returns>
        public static Microsoft.Xrm.SdkTypeProxy.CrmService GetCRMService(IPluginExecutionContext context)
        {
            return (Microsoft.Xrm.SdkTypeProxy.CrmService)context.CreateCrmService(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPointUrl"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Microsoft.Xrm.SdkTypeProxy.CrmService GetCrmProxyUsingEndpointUrlInChildPipeline(string endPointUrl, IPluginExecutionContext context)
        {
            Microsoft.Xrm.SdkTypeProxy.CrmService childCrmService = new Microsoft.Xrm.SdkTypeProxy.CrmService();
            childCrmService.Url = endPointUrl;
            childCrmService.Credentials = System.Net.CredentialCache.DefaultCredentials;
            childCrmService.CrmAuthenticationTokenValue = new Microsoft.Xrm.Sdk.CrmAuthenticationToken();
            childCrmService.CrmAuthenticationTokenValue.AuthenticationType = AuthenticationType.AD;
            childCrmService.CrmAuthenticationTokenValue.OrganizationName = context.OrganizationName;
            childCrmService.CorrelationTokenValue = new Microsoft.Xrm.Sdk.CorrelationToken(context.CorrelationId, context.Depth, context.CorrelationUpdatedTime);
            return childCrmService;

        }


        */
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Entity GetContextEntity(IPluginExecutionContext context)
        {
            Entity entity = null;
            if (context.InputParameters.Contains("Target") &&
               context.InputParameters["Target"] is Entity)
            {

                entity = (Entity)context.InputParameters["Target"];
            }
            return entity;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static EntityReference GetContextForAssign(IPluginExecutionContext context)
        {
            EntityReference EntityReference = null;
            if (context.InputParameters.Contains("Target") &&
               context.InputParameters["Target"] is EntityReference)
            {

                EntityReference = (EntityReference)context.InputParameters["Target"];
            }
            return EntityReference;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static EntityReference GetContextForSetState(IPluginExecutionContext context)
        {
            EntityReference EntityReference = null;
            if (context.InputParameters.Contains("EntityEntityReference") &&
               context.InputParameters["EntityEntityReference"] is EntityReference)
            {

                EntityReference = (EntityReference)context.InputParameters["EntityEntityReference"];
            }

            else if (context.InputParameters.Contains("EntityMoniker") &&
             context.InputParameters["EntityMoniker"] is EntityReference)
            {

                EntityReference = (EntityReference)context.InputParameters["EntityMoniker"];
            }
            return EntityReference;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static EntityReference GetContextForMerge(IPluginExecutionContext context)
        {
            EntityReference EntityReference = null;
            if (context.InputParameters.Contains("Target") &&
               context.InputParameters["Target"] is EntityReference)
            {

                EntityReference = (EntityReference)context.InputParameters["Target"];
            }
            return EntityReference;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static EntityReference GetContextForDelete(IPluginExecutionContext context)
        {
            EntityReference EntityReference = null;
            if (context.InputParameters.Contains("Target") &&
               context.InputParameters["Target"] is EntityReference)
            {

                EntityReference = (EntityReference)context.InputParameters["Target"];
            }
            return EntityReference;

        }

        internal static Entity GetUpdateEntity(Entity entity)
        {
            Entity eEntity = new Entity(entity.LogicalName);
            eEntity.Id = entity.Id;
            return eEntity;
        }

        internal static Entity GetUpdateEntity(EntityReference erEntity)
        {
            Entity eEntity = new Entity(erEntity.LogicalName);
            eEntity.Id = erEntity.Id;
            return eEntity;
        }
    }
}
