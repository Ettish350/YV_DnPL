using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;

//using System.Configuration;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;

namespace BasicPlugIn
{
    class PublishingGlobalFunctions
    {
        private IOrganizationService sysService;
        private GlobalFunctions gf;


        public enum KlitaStatusCode { KlitaAndFirst = 1, WaitingForDetails = 100000000, SupportEditor = 100000001, PublishEditor = 100000002, Lektor1 = 100000003, Lektor2 = 100000005, Opinion = 100000006, SupportCommittee = 100000007, PublishCommittee = 100000008, ApprovedWithCondition = 100000009, PublishAproved = 100000010, Maarechet = 100000011, Rejected = 100000012, WasShelved = 100000013, FurtherConsultation = 100000014, SupportApproved = 100000015, WasPublished = 100000016, Frozen = 100000017, SupportHasBeenImplemented = 100000018, WasPublishedAnother = 100000019, InActive = 2, RejectInActive = 100000020, InActiveMovedToPublish = 100000021, InActiveMovedToSupport = 100000022 , SupportNotApproved = 100000023};

        public enum MergeandeditoropinionStatusCode { CheckAndSelect = 1, Lektor1 = 100000000, Lektor2 = 100000001, MovedToSupport = 100000002, Rejected = 100000003, PublishCommittee = 100000004, PublishAproved = 100000005, ApprovedWithCondition = 100000006, Maarechet = 100000007, InActive = 2, InActiveMovedToSupport = 100000008 };
        public enum SupportLektorStatusCode { Klita = 1, Lektor1 = 100000000, Lektor2 = 100000001, SupportCommittee = 100000002, Rejected = 100000003, SupportApproved = 100000004, SupportHasBeenImplemented = 100000005, WasPublished = 100000006, InActiveMovedToPublish = 100000007, InActive = 2 , SupportNotApproved = 100000008};
        public enum PublicationBoardStatusCode { Active = 1, Rejected = 100000000, Maarechet = 100000001, ApprovedWithCondition = 100000002, PublishAproved = 100000003, InActiveMovedToSupport = 100000004, MovedToSupport= 100000005, InActive = 2 };
        public enum SupportCommitteeStatusCode { Active = 1, Rejected = 100000000, SupportHasBeenImplemented = 100000001, SupportApproved = 100000002, NoComitee = 100000003, InActiveMovedToPublish = 100000004, InActive = 2 ,SupportNotApproved= 100000005 }
        public enum KtavyadMaarehetStatusCode { BeforeWork = 1, Contract = 100000000, InWork = 100000001, WasShelved = 100000002, WasPublished = 100000003, Frozen = 100000004, InActive = 2 }
        public enum MaarehetKliteWithConds { Active = 1, Maarechet = 100000000,  InActive = 2 }
        public enum FollowTmiha { BeforeUsed = 1, SupportHasBeenImplemented = 100000000, DoNotWontToImplemented = 100000001, InActive = 2 }



        public enum PublicationBoardDesicion { Approved = 1, NotApprove = 2, ApprovedWithCond = 3, MoreDiscion = 4, OtherLektor = 5, RequestForFix = 6, ApproveAfterFix = 7 }

        public PublishingGlobalFunctions(IOrganizationService SysSservice)
        {
            sysService = SysSservice;
            gf = new GlobalFunctions(sysService);
        }
        public RetrieveMultipleResponse GetSubEntityList(Entity postEntity, string sEntityName)
        {
            #region query
            QueryExpression query = new QueryExpression();
            query.EntityName = sEntityName;

            ColumnSet columns = new ColumnSet(true);

            query.ColumnSet = new ColumnSet(true);

            query.Criteria = new FilterExpression();
            query.Criteria.FilterOperator = LogicalOperator.And;

            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = postEntity.LogicalName;
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(postEntity.Id);

            ConditionExpression condition2 = new ConditionExpression();
            condition2.AttributeName = "statecode";
            condition2.Operator = ConditionOperator.Equal;
            condition2.Values.Add(0);

            query.Criteria.Conditions.Add(condition1);
            query.Criteria.Conditions.Add(condition2);

            RetrieveMultipleRequest retrieve = new RetrieveMultipleRequest();
            retrieve.Query = query;
            RetrieveMultipleResponse retrieved = (RetrieveMultipleResponse)sysService.Execute(retrieve);
            #endregion
            return retrieved;
        }



        public void UpdateDataFromKtavYadKlitaOnKtavyadKlitaChange(Entity preEntity, Entity postEntity, ref Entity deEntity, ref bool bIsUpdate)
        {
            Guid gPreKtavYadKlita = EntityUtils.GetEntityReferenceFromEntityValue("new_ktavyad_klita", preEntity);
            Guid gPostKtavYadKlita = EntityUtils.GetEntityReferenceFromEntityValue("new_ktavyad_klita", postEntity);
            if (gPreKtavYadKlita.CompareTo(gPostKtavYadKlita) != 0)
            {
                Entity eKtavyadKlita = gf.GetRecord("new_ktavyad_klita", gPostKtavYadKlita, new string[] { "new_ktavyad_klitaid", "new_name", "new_authoreditorname", "new_authoreditorname2", "new_deliveredby1", "new_deliveredby2", "new_deliveredby_account" , "new_publishing_genre", "new_taarichhagaa","new_mark_type","new_quantity","new_number_of_accepted_files","new_hekef_update_date"});

                DateTime dtHekefUpdateDate = DateTime.MinValue;
                DateTime dtPostHekefUpdateDate = DateTime.MinValue;
                DateTime dtTaarichhagaa = DateTime.MinValue;
                DateTime dtPostTaarichhagaa = DateTime.MinValue;
                #region ktavyad klita data
                Guid gAuthoreditorName = EntityUtils.GetEntityReferenceFromEntityValue("new_authoreditorname", eKtavyadKlita);
                Guid gAuthoreditorName2 = EntityUtils.GetEntityReferenceFromEntityValue("new_authoreditorname2", eKtavyadKlita);
                Guid gDeliveredby1 = EntityUtils.GetEntityReferenceFromEntityValue("new_deliveredby1", eKtavyadKlita);
                Guid gDeliveredby2 = EntityUtils.GetEntityReferenceFromEntityValue("new_deliveredby2", eKtavyadKlita);
                Guid gDeliveredbyAccount = EntityUtils.GetEntityReferenceFromEntityValue("new_deliveredby_account", eKtavyadKlita);
                string sName = EntityUtils.GetStringFromEntityValue("new_name", eKtavyadKlita);

                int iMarkType = EntityUtils.GetOptionSetValueFromEntityValue("new_mark_type", postEntity);
                double dQuantity = EntityUtils.GetDoubleFromEntityValue("new_quantity", postEntity);
                int iNumberOfAccepted_Files = EntityUtils.GetIntFromEntityValue("new_number_of_accepted_files", postEntity);
                DateTime? dHekefUpdateDate = EntityUtils.GetDateTimeFromEntity("new_hekef_update_date", postEntity);
                if (dHekefUpdateDate.HasValue) { dtHekefUpdateDate = dHekefUpdateDate.Value; }
                int iPublishingGenre = EntityUtils.GetOptionSetValueFromEntityValue("new_publishing_genre", postEntity);

                DateTime? dTaarichhagaa = EntityUtils.GetDateTimeFromEntity("new_hekef_update_date", postEntity);
                if (dTaarichhagaa.HasValue) { dtTaarichhagaa = dTaarichhagaa.Value; }
               
                #endregion
                #region post data
                Guid gPostAuthoreditorName = EntityUtils.GetEntityReferenceFromEntityValue("new_authoreditorname", postEntity);
                Guid gPostAuthoreditorName2 = EntityUtils.GetEntityReferenceFromEntityValue("new_authoreditorname2", postEntity);
                Guid gPostDeliveredby1 = EntityUtils.GetEntityReferenceFromEntityValue("new_deliveredby1", postEntity);
                Guid gPostDeliveredby2 = EntityUtils.GetEntityReferenceFromEntityValue("new_deliveredby2", postEntity);
                Guid gPostDeliveredbyAccount = EntityUtils.GetEntityReferenceFromEntityValue("new_deliveredby_account", postEntity);
                string sPostName = EntityUtils.GetStringFromEntityValue("new_name", postEntity);

                int iPostMarkType = EntityUtils.GetOptionSetValueFromEntityValue("new_mark_type", postEntity);
                double dPostQuantity = EntityUtils.GetDoubleFromEntityValue("new_quantity", postEntity);
                int iPostNumberOfAccepted_Files = EntityUtils.GetIntFromEntityValue("new_number_of_accepted_files", postEntity);
                DateTime? dPostHekefUpdateDate = EntityUtils.GetDateTimeFromEntity("new_hekef_update_date", postEntity);
                if (dPostHekefUpdateDate.HasValue) { dtPostHekefUpdateDate = dPostHekefUpdateDate.Value; }

                int iPostPublishingGenre = EntityUtils.GetOptionSetValueFromEntityValue("new_genre", postEntity);
                DateTime? dPostTaarichhagaa = EntityUtils.GetDateTimeFromEntity("new_hekef_update_date", postEntity);
                if (dPostTaarichhagaa.HasValue) { dtPostTaarichhagaa = dPostTaarichhagaa.Value; }

                #endregion


                #region Authors
                if (gAuthoreditorName.CompareTo(gPostAuthoreditorName) != 0)
                {
                    bIsUpdate = true;

                    if (gAuthoreditorName.CompareTo(Guid.Empty) != 0) { deEntity["new_authoreditorname"] = new EntityReference("contact", gAuthoreditorName); }
                    else { deEntity["new_authoreditorname"] = null; }
                }
                if (gAuthoreditorName2.CompareTo(gPostAuthoreditorName2) != 0)
                {
                    bIsUpdate = true;

                    if (gAuthoreditorName2.CompareTo(Guid.Empty) != 0) { deEntity["new_authoreditorname2"] = new EntityReference("contact", gAuthoreditorName2); }
                    else { deEntity["new_authoreditorname2"] = null; }
                }
                #endregion
                #region deliveries
                if (gDeliveredby1.CompareTo(gPostDeliveredby1) != 0)
                {
                    bIsUpdate = true;

                    if (gDeliveredby1.CompareTo(Guid.Empty) != 0) { deEntity["new_deliveredby1"] = new EntityReference("contact", gDeliveredby1); }
                    else { deEntity["new_deliveredby1"] = null; }
                }
                if (gDeliveredby2.CompareTo(gPostDeliveredby2) != 0)
                {
                    bIsUpdate = true;

                    if (gDeliveredby2.CompareTo(Guid.Empty) != 0) { deEntity["new_deliveredby2"] = new EntityReference("contact", gDeliveredby2); }
                    else { deEntity["new_deliveredby2"] = null; }
                }
                if (gDeliveredbyAccount.CompareTo(gPostDeliveredbyAccount) != 0)
                {
                    bIsUpdate = true;

                    if (gDeliveredbyAccount.CompareTo(Guid.Empty) != 0) { deEntity["new_deliveredby_account"] = new EntityReference("account", gDeliveredbyAccount); }
                    else { deEntity["new_deliveredby_account"] = null; }
                }
                #endregion
                if (sName.CompareTo(sPostName) != 0 && postEntity.LogicalName.CompareTo("new_ktavyad_maarehet")!=0)
                {
                    bIsUpdate = true;
                    deEntity["new_name"] = sName;
                }
                if (postEntity.LogicalName.CompareTo("new_mergeandeditoropinion") == 0)
                {
                    if (iMarkType.CompareTo(iPostMarkType) != 0)
                    {
                        bIsUpdate = true;
                        if (iMarkType > 0) { deEntity["new_mark_type"] = new OptionSetValue(iMarkType); }
                        else { deEntity["new_mark_type"] = null; }
                    }

                    if (dQuantity.CompareTo(dPostQuantity) != 0)
                    {
                        bIsUpdate = true;
                        if (dQuantity > 0) { deEntity["new_quantity"] = dQuantity; }
                        else { deEntity["new_quantity"] = null; }
                    }
                    if (iNumberOfAccepted_Files.CompareTo(iPostNumberOfAccepted_Files) != 0)
                    {
                        bIsUpdate = true;
                        if (iNumberOfAccepted_Files > 0) { deEntity["new_number_of_accepted_files"] = iNumberOfAccepted_Files; }
                        else { deEntity["new_number_of_accepted_files"] = null; }
                    }
                    if (dtHekefUpdateDate.CompareTo(dtPostHekefUpdateDate) != 0)
                    {
                        bIsUpdate = true;
                        if (dHekefUpdateDate > DateTime.MinValue) { deEntity["new_hekef_update_date"] = dHekefUpdateDate; }
                        else { deEntity["new_hekef_update_date"] = null; }
                    }
                }
                if (postEntity.LogicalName.CompareTo("new_ktavyad_maarehet") == 0)
                {
                    if (iPostPublishingGenre== 0)
                    {
                        bIsUpdate = true;
                        if (iPublishingGenre > 0) { deEntity["new_genre"] = new OptionSetValue(iPublishingGenre); }
                        else { deEntity["new_genre"] = null; }
                    }

                  
                    if (dtPostTaarichhagaa.CompareTo(dtTaarichhagaa) != 0)
                    {
                        bIsUpdate = true;
                        if (dtTaarichhagaa > DateTime.MinValue) { deEntity["new_delivery_date"] = dtTaarichhagaa; }
                        else { deEntity["new_delivery_date"] = null; }
                    }
                }

              
            }
        }

    }
}
