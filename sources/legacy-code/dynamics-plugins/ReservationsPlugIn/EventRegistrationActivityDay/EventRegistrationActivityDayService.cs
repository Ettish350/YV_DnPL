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
using Microsoft.Xrm.Sdk.Client;

namespace Reservations.EventRegistrationActivityDay
{
    class EventRegistrationActivityDayService
    {

        private IOrganizationService sysService;
        private IOrganizationService userService;
        private GlobalFunctions gf;


        public EventRegistrationActivityDayService(IOrganizationService crmSysService, IOrganizationService crmservice)
        {
            sysService = crmSysService;
            userService = crmservice;
            gf = new GlobalFunctions(sysService);

        }



        internal void UpdateActivityDayTotalParticipants(Entity preEntity, Entity postEntity)
        {
            OrganizationServiceContext orgContext = new OrganizationServiceContext(sysService);
            Guid gPreActivityDay = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", preEntity);
            Guid gPostActivityDay = EntityUtils.GetEntityReferenceFromEntityValue("new_activity_day", postEntity);
            // WriteToLog.LogToFile("gPostActivityDay "+ gPostActivityDay, "\n");

            var r = from eractd in orgContext.CreateQuery("new_event_registration_activity_day")
                    join er in orgContext.CreateQuery("new_event_registration")
                    on eractd["new_event_registration"] equals er["new_event_registrationid"]
                    where (((EntityReference)eractd["new_activity_day"]).Id == gPostActivityDay
                           && (int)eractd["statecode"] == 0)
                    select new
                    {
                        nump = er.GetAttributeValue<int?>("new_number_of_participants"),
                        numap = er.GetAttributeValue<int?>("new_actual_number_of_participants")
                    };

            var sum = r.ToList().Sum(x => x.nump ?? 0);
            //var actualSum = r.ToList().Sum(x => x.numap ?? 0);

            try
            {
                var activityDayEntity = userService.Retrieve("new_activity_day", gPostActivityDay, new ColumnSet(new string[] { "new_num_additional_participants" }));
                var actualNumOfParticipants = sum + activityDayEntity.GetAttributeValue<int>("new_num_additional_participants");

                Entity ad = new Entity("new_activity_day", gPostActivityDay);
                ad["new_number_of_participants"] = sum;
                ad["new_actual_number_of_participants"] = actualNumOfParticipants;
                sysService.Update(ad);
            }
            catch (Exception ex)
            {

                WriteToLog.LogToFile("update activity_day failed in EvevnRegistartionActivityDay 2 ----" + ex.Message, "\n");

            }

            if (gPreActivityDay != Guid.Empty && gPreActivityDay.CompareTo(Guid.Empty) != 0 && gPreActivityDay.CompareTo(gPostActivityDay) != 0)
            {
                var r1 = from eractd1 in orgContext.CreateQuery("new_event_registration_activity_day")
                         join er1 in orgContext.CreateQuery("new_event_registration")
                         on eractd1["new_event_registration"] equals er1["new_event_registrationid"]
                         where (((EntityReference)eractd1["new_activity_day"]).Id == gPreActivityDay
                                && (int)eractd1["statecode"] == 0)
                         select new
                         {
                             nump = er1.GetAttributeValue<int?>("new_number_of_participants"),
                             numap = er1.GetAttributeValue<int?>("new_actual_number_of_participants")
                         };
                var sum1 = r1.ToList().Select(x => Convert.ToInt32(x.nump)).Sum();

                var activityDayEntity1 = userService.Retrieve("new_activity_day", gPreActivityDay, new ColumnSet(new string[] { "new_num_additional_participants" }));
                var actualNumOfParticipants1 = sum1 + activityDayEntity1.GetAttributeValue<int>("new_num_additional_participants");

                try
                {
                    Entity ad = new Entity("new_activity_day");
                    ad.Id = gPreActivityDay;
                    ad["new_number_of_participants"] = sum1;
                    ad["new_actual_number_of_participants"] = actualNumOfParticipants1;
                    sysService.Update(ad);
                }
                catch (Exception ex)
                {

                    WriteToLog.LogToFile("update activity_day failed in eventRegistrationActivityDay ----" + ex.Message, "\n");

                }

            }


        }
    }
}