using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class CommemorationService : ICommemorationService
    {
        private readonly ICommemorationRepository commemorationRepository;
        private readonly IEventItemRepository eventItemRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly string environment;

        public CommemorationService(
            ICommemorationRepository commemorationRepository,
            IEventItemRepository eventItemRepository,
            IMapFunctionRepository mapFunctionRepository
            )
        {
            this.commemorationRepository = commemorationRepository;
            this.eventItemRepository = eventItemRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            environment = ConfigurationManager.AppSettings["environment"];
        }

        public void SetReservationStatus(CommemorationReservationDetails data, string userId)
        {
            commemorationRepository.SetReservationStatus(data, userId);
        }

        public void SetQuoteToNewReservation(string id, string userId)
        {
            List<Filter> filterList = new List<Filter>();

            filterList.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Reservation"),
                Value = id,
                Type = "lookup"                
            });

            filterList.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("EventType"),
                Value = ((int)EventType.Reserve).ToString(),
                Type = "optionset"
            });

            List<EventItem> eventItemList = eventItemRepository.GetFetchXml(userId, false, filterList).Item2;
            foreach (var eventItem in eventItemList)
            {
                if (eventItem.ScheduleItem != null && (eventItem.ScheduleItem.Id.ToLower() == ConfigurationManager.AppSettings[$"museumResourceId_{environment}"].ToLower() ||
                    eventItem.ScheduleItem.Id.ToLower() == ConfigurationManager.AppSettings[$"bigQuestionsRoomResourceId_{environment}"].ToLower()))
                {
                    eventItem.EventType = (int)EventType.Scheduling;
                    eventItemRepository.Update(eventItem, null, userId);
                }
            }

            commemorationRepository.SetQuoteToNewReservation(id, userId);
            
        }



    }
}
