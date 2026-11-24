using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class AvailabilityForMonthService : BasicService<AvailabilityForMonth, AvailabilityForMonth, AvailabilityForMonthCRM>, IAvailabilityForMonthService
    {
        private readonly MapFunctionRepository mapFunctionRepository;
        private readonly IAvailabilityForMonthRepository availabilityForMonthRepository;
        private readonly IServiceProviderRepository serviceProviderRepository;
        private readonly IEventItemRepository eventItemRepository;
        public AvailabilityForMonthService(IAvailabilityForMonthRepository availabilityForMonthRepository,
            IServiceProviderRepository serviceProviderRepository, 
            IEventItemRepository eventItemRepository,
            MapFunctionRepository mapFunctionRepository,
                                                  IHelperService helperService)
             : base(helperService, availabilityForMonthRepository, mapFunctionRepository)
        {
            this.availabilityForMonthRepository = availabilityForMonthRepository;
            this.serviceProviderRepository = serviceProviderRepository;
            this.eventItemRepository = eventItemRepository;
            this.mapFunctionRepository = mapFunctionRepository;
        }


        public List<AvailabilityForMonth> GetAvailabilityForMonths(string userId, DataDate data)
        {
            var startDate = DateTime.ParseExact(
                data.StartDate.Trim(),
                "dd/MM/yyyy",
                null);

            var endDate = DateTime.ParseExact(
                data.EndDate.Trim(),
                "dd/MM/yyyy",
                null);

            string startDateString = startDate.ToString("yyyy-MM-dd");
            string endDateString = endDate.ToString("yyyy-MM-dd");

            // =========================================================
            // 1. Availability FetchXml
            // =========================================================

            string availabilityFetchXml = $@"
    <fetch version='1.0'
           output-format='xml-platform'
           mapping='logical'
           distinct='true'>

        <entity name='new_availability_service_provider_in_month'>

            <attribute name='new_availability_service_provider_in_monthid'/>
            <attribute name='new_comments'/>
            <attribute name='new_availability_month'/>
            <attribute name='new_service_provider'/>

            <filter type='and'>
                <condition attribute='statecode'
                           operator='eq'
                           value='0'/>

                <condition attribute='new_availability_month'
                           operator='on'
                           value='{startDateString}'/>
            </filter>

            <link-entity name='new_service_provider'
                         from='new_service_providerid'
                         to='new_service_provider'
                         alias='ServiceProvider'>

                <attribute name='new_service_providerid'/>
                <attribute name='new_name'/>

            </link-entity>

        </entity>

    </fetch>";

            var availabilityResult =
                availabilityForMonthRepository.GetFetchXmlOnly(
                    userId,
                    availabilityFetchXml);

            var availabilityForMonthsList = availabilityResult.Item2;

            // =========================================================
            // 2. Get all service provider ids
            // =========================================================

            var serviceProviderIds = availabilityForMonthsList
                .Where(x => x.ServiceProvider != null)
                .Select(x => x.ServiceProvider.Id)
                .Distinct()
                .ToList();

            if (!serviceProviderIds.Any())
                return availabilityForMonthsList;

            // =========================================================
            // 3. Build IN condition for FetchXml
            // =========================================================

            string serviceProviderValues = string.Join(
                "",
                serviceProviderIds.Select(id =>
                    $"<value>{id}</value>"));

            // =========================================================
            // 4. Events FetchXml
            // =========================================================

            string eventsFetchXml = $@"
    <fetch version='1.0'
           output-format='xml-platform'
           mapping='logical'
           distinct='true'>

        <entity name='new_event_item'>

            <attribute name='new_event_itemid'/>
            <attribute name='new_name'/>
            <attribute name='new_start_datetime'/>
            <attribute name='new_end_datetime'/>
            <attribute name='new_service_provider'/>

            <filter type='and'>

                <condition attribute='statecode'
                           operator='eq'
                           value='0'/>

                <!-- overlap logic -->

                <condition attribute='new_start_datetime'
                           operator='lt'
                           value='{endDate.AddDays(1):yyyy-MM-dd}'/>

                <condition attribute='new_end_datetime'
                           operator='gt'
                           value='{startDateString}'/>

                <condition attribute='new_service_provider'
                           operator='in'>

                    {serviceProviderValues}

                </condition>

            </filter>

        </entity>

    </fetch>";

            var eventsResult =
                eventItemRepository.GetFetchXmlOnly(
                    userId,
                    eventsFetchXml);

            var allEvents = eventsResult.Item2;

            // =========================================================
            // 5. Group events by service provider
            // =========================================================

            var eventsByProvider = allEvents
                .Where(x => x.ScheduleItem != null)
                .GroupBy(x => x.ScheduleItem.Id)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());

            // =========================================================
            // 6. Attach events to availability
            // =========================================================

            foreach (var availability in availabilityForMonthsList)
            {
                if (availability.ServiceProvider == null)
                {
                    availability.Events = new List<EventItem>();
                    continue;
                }

                availability.Events =
                    eventsByProvider.TryGetValue(
                        availability.ServiceProvider.Id,
                        out var events)
                            ? events
                            : new List<EventItem>();

                SetEventsOverlapAvailabilityTimesList(availability);
            }

            return availabilityForMonthsList;
        }
        public AvailabilityForMonth GetAvailabilityForMonthsByServiceProviderId(string userId, string serviceProviderId, DataDate data)
        {
            string fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                    <entity name='new_availability_service_provider_in_month'>
                                        <attribute name='new_availability_service_provider_in_monthid'/>
                                        <attribute name='new_comments'/>
                                        <attribute name='new_availability_month'/>
                                        <attribute name='new_service_provider'/>
                                        <filter type='and'>
                                            <condition attribute='statecode' operator='eq' value='0'/>
                                            <condition attribute='new_service_provider' operator='eq' value='{serviceProviderId}'/>
                                            <condition attribute='new_availability_month' operator='on' value='{DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd")}'/>
                                        </filter>";

            fetchXml += $@"<link-entity name='new_service_provider' from='new_service_providerid' to='new_service_provider' alias='ServiceProvider'>
                                        <link-entity name='new_event_item' from='new_service_provider' to='new_service_providerid' alias='EventItem' link-type='outer'>
                                            <attribute name='new_event_itemid'/>
                                            <attribute name='new_name'/>
                                            <attribute name='new_start_datetime'/>
                                            <attribute name='new_end_datetime'/>
                                            <filter type='and'>
                                                    <condition attribute='statecode' operator='eq' value='0'/>
                                                    <condition attribute='new_start_datetime' operator='on-or-after' value='{DateTime.ParseExact(data.StartDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd")}'/>
                                                    <condition attribute='new_start_datetime' operator='on-or-before' value='{DateTime.ParseExact(data.EndDate.Trim(), "dd/MM/yyyy", null).ToString("yyyy-MM-dd")}'/>
                                                </filter>
                                            </link-entity>
                                        </link-entity>";

            fetchXml += @"</entity>
                                </fetch>";

            List<AvailabilityForMonth> availabilityForMonths = availabilityForMonthRepository.GetFetchXmlOnly(userId, fetchXml).Item2;
            var availabilityForMonths1 = availabilityForMonths
                .GroupBy(x => x.Id)
                .Select(g =>
                {
                    var first = g.First();
                    first.Events = g.SelectMany(x => x.Events).ToList();
                    return first;
                })
                .ToList();
            if (availabilityForMonths1 != null && availabilityForMonths1.Count > 0)
            {
                AvailabilityForMonth availabilityForMonth = availabilityForMonths1[0];
                SetEventsOverlapAvailabilityTimesList(availabilityForMonth);
                return availabilityForMonth;
            }
            return null;
        }


        private void SetEventsOverlapAvailabilityTimesList(AvailabilityForMonth provider)
        {
            if (provider.AvailabilityTimes == null || provider.Events == null)
                return;

                foreach (var av in provider.AvailabilityTimes)
                {
                    var avStart = helperService.ParseDate(av.StartDateTime);
                    var avEnd = helperService.ParseDate(av.EndDateTime);

                    var r1 = CheckOverlapTimes(avStart, avEnd, provider.Events);

                    av.OverlapStatus = r1.Status;
                    av.EventStart = r1.EventStart?.ToString("MM/dd/yyyy HH:mm");
                    av.EventEnd = r1.EventEnd?.ToString("MM/dd/yyyy HH:mm");

                    // --- חפיפה 2 ---
                    var avStart2 = helperService.ParseDate(av.StartDateTime2);
                    var avEnd2 = helperService.ParseDate(av.EndDateTime2);
                    var r2 = CheckOverlapTimes(avStart2, avEnd2, provider.Events);

                    av.OverlapStatus2 = r2.Status;
                    av.EventStart2 = r2.EventStart?.ToString("MM/dd/yyyy HH:mm");
                    av.EventEnd2 = r2.EventEnd?.ToString("MM/dd/yyyy HH:mm");
                }
            }
        

        private OverlapResult CheckOverlapTimes(DateTime start, DateTime end, List<EventItem> events)
        {
            var res = new OverlapResult
            {
                Status = OverlapStatus.None
            };

            foreach (var ev in events)
            {
                var evStart = helperService.ParseDate(ev.Start);
                var evEnd = helperService.ParseDate(ev.End);

                bool full =
                    evStart <= start &&
                    evEnd >= end;

                bool partial =
                    evStart < end &&
                    evEnd > start &&
                    !full;

                if (full)
                {
                    res.Status = OverlapStatus.Full;
                    res.EventStart = evStart;
                    res.EventEnd = evEnd;
                    return res;
                }

                if (partial && res.Status != OverlapStatus.Full)
                {
                    res.Status = OverlapStatus.Partial;
                    res.EventStart = evStart;
                    res.EventEnd = evEnd;
                }
            }

            return res;
        }

    }
}
