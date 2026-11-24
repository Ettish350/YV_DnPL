using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Service.Services
{
    public class ServiceProviderAvailabilityService : IServiceProviderAvailabilityService
    {
        private readonly IClosedDaysService _closedDaysService;
        private readonly ICRMConnection _crmService;
        private readonly IServiceProviderAvailabilityRepository _serviceProviderAvailabilityRepository;
        public ServiceProviderAvailabilityService(IClosedDaysService closedDaysService,
            IServiceProviderAvailabilityRepository serviceProviderAvailabilityRepository,
            ICRMConnection crmService)
        {
            _closedDaysService = closedDaysService;
            _crmService = crmService;
            _serviceProviderAvailabilityRepository = serviceProviderAvailabilityRepository;
        }

        public bool CheckIdNumberIsServiceProvider(string idNumber)
        {
            ServiceProviderDeatils serviceProvider = GetServiceProviderIdByIdNumber(idNumber);
            return serviceProvider != null && serviceProvider.Id != Guid.Empty;
        }

        public List<ClosedDays> GetClosedDaysForCalendar(DateTime fromDate, DateTime toDate)
        {
            return _closedDaysService.GetClosedDays(fromDate, toDate, new List<ClosedType> { ClosedType.SERVICE_PROVIDER_CLOSED, ClosedType.YV_CLOSED, ClosedType.SHABATON });
        }

        public ServiceProviderAvailabilityForMonth CreateAvailabilityForMonth(ServiceProviderAvailabilityForMonth availabilityForMonth)
        {
            ServiceProviderDeatils serviceProvider = GetServiceProviderIdByIdNumber(availabilityForMonth.IdNumber);
            availabilityForMonth.ServiceProviderId = serviceProvider.Id;
            if(availabilityForMonth.ServiceProviderId == Guid.Empty)
            {
                throw new Exception();
            }
            availabilityForMonth.FullName = serviceProvider.FullName;
            availabilityForMonth.Id = _serviceProviderAvailabilityRepository.CreateAvailabilityForMonth(availabilityForMonth);

            foreach (var availabilityTimeInDay in availabilityForMonth.availabilityTimes)
            {
                availabilityTimeInDay.AvailabilityForMonthId = (Guid)availabilityForMonth.Id;
                availabilityTimeInDay.Id = _serviceProviderAvailabilityRepository.CreateAvailabilityTimeInDay(availabilityTimeInDay);
            }
            return availabilityForMonth;
        }

        public ServiceProviderAvailabilityForMonth UpdateAvailabilityForMonth(ServiceProviderAvailabilityForMonth availabilityForMonthOld, ServiceProviderAvailabilityForMonth availabilityForMonth)
        {
            ServiceProviderDeatils serviceProvider = GetServiceProviderIdByIdNumber(availabilityForMonth.IdNumber);
            if (serviceProvider == null || serviceProvider.Id == Guid.Empty)
            {
                throw new Exception();
            }
            availabilityForMonth.ServiceProviderId = serviceProvider.Id;

            if (availabilityForMonth.Id != null)
            {
                _serviceProviderAvailabilityRepository.UpdateAvailabilityForMonth(availabilityForMonth);

                //delete all times
                foreach (var availabilityTimeInDayOld in availabilityForMonthOld.availabilityTimes)
                {
                    _serviceProviderAvailabilityRepository.DeleteAvailabilityTimeInDay(availabilityTimeInDayOld);
                }
                //create the new times
                foreach (var availabilityTimeInDay in availabilityForMonth.availabilityTimes)
                {
                    availabilityTimeInDay.AvailabilityForMonthId = (Guid)availabilityForMonth.Id;
                    availabilityTimeInDay.Id = _serviceProviderAvailabilityRepository.CreateAvailabilityTimeInDay(availabilityTimeInDay);
                }
            }

            else
            {
                throw new Exception();
            }
            return availabilityForMonth;
            
        }


        public ServiceProviderAvailabilityForMonth GetAvailabilityServiceProviderForMonth(ServiceProviderDeatils serviceProvider, DateTime month)
        {
            ServiceProviderAvailabilityForMonth availabilityMonth = new ServiceProviderAvailabilityForMonth
            {
                FullName = serviceProvider.FullName,
                IdNumber = serviceProvider.IdNumber,
                ServiceProviderId = serviceProvider.Id
            };

            string fetchXmlAvailabilityMonth = @$"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                <entity name='new_availability_service_provider_in_month'>
                                                    <filter type='and'>
                                                        <condition attribute='new_availability_month' operator='on' value='{month.ToString("yyyy-MM-dd")}'/>
                                                        <condition attribute='new_service_provider' operator='eq' value='{serviceProvider.Id}'/>
                                                    </filter>
                                                </entity>
                                            </fetch>";

            var query_availabilityMonth = new FetchExpression(fetchXmlAvailabilityMonth);
            EntityCollection ec_availabilityMonth = _crmService.RetrieveMultiple(query_availabilityMonth);
            
            if (ec_availabilityMonth.Entities.Count > 0)
            {
                var e_availabilityMonth = ec_availabilityMonth.Entities[0];

                availabilityMonth.Id = e_availabilityMonth.Id;
                availabilityMonth.Comment = e_availabilityMonth.GetAttributeValue<string?>("new_comments");
                availabilityMonth.availabilityTimes = GetAvailabilityTimeInDay(e_availabilityMonth.Id, month, serviceProvider.Id);
            }
            return availabilityMonth;
        }

        public ServiceProviderDeatils GetServiceProviderIdByIdNumber(string idNumber)
        {
            string fetchXmlServiceProvider = @$"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                    <entity name='new_service_provider'>
                                                        <attribute name='new_service_providerid'/>
                                                        <attribute name='new_first_name_he'/>
                                                        <attribute name='new_last_name_he'/>
                                                        <filter type='and'>
                                                            <condition attribute='new_id_number' operator='eq' value='{idNumber}'/>
                                                        </filter>
                                                    </entity>
                                                </fetch>";
            var query_serviceProvider = new FetchExpression(fetchXmlServiceProvider);
            EntityCollection ec_serivceProvider = _crmService.RetrieveMultiple(query_serviceProvider);
            if(ec_serivceProvider.Entities != null && ec_serivceProvider.Entities.Count > 0)
            {
                var e_serivceProvider = ec_serivceProvider.Entities.First();
                return new ServiceProviderDeatils
                {
                    Id = e_serivceProvider.Id,
                    IdNumber = idNumber,
                    FullName = e_serivceProvider.GetAttributeValue<string>("new_first_name_he") + " " +
                    e_serivceProvider.GetAttributeValue<string>("new_last_name_he")
                };
            }
            else
            {
                return null;
            }

        }

        private List<ServiceProviderAvailabilityTimeInDay> GetAvailabilityTimeInDay(Guid availabilityForMonthId, DateTime month, Guid serviceProviderId)
        {
            List<ServiceProviderAvailabilityTimeInDay> timesList = new List<ServiceProviderAvailabilityTimeInDay>();

            string fetchXmlAvailabilityTimes = @$"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                    <entity name='new_availability_times_day_in_month'>
                                        <order attribute='new_name' descending='false'/>
                                        <filter type='and'>
                                            <condition attribute='new_availability_in_month' operator='eq' value='{availabilityForMonthId}'/>
                                        </filter>
                                    </entity>
                                </fetch>";
            var query_availabilityTimes = new FetchExpression(fetchXmlAvailabilityTimes);
            EntityCollection ec_availabilityTimes = _crmService.RetrieveMultiple(query_availabilityTimes);

            var firstDay = new DateTime(month.Year, month.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            string fetchXmlScheduleEvevnts = @$"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                <entity name='new_event_item'>
                                                    <attribute name='new_event_itemid'/>
                                                    <attribute name='new_start_datetime'/>
                                                    <attribute name='new_end_datetime'/>
                                                    <order attribute='new_name' descending='false'/>
                                                    <filter type='and'>
                                                        <condition attribute='new_service_provider' operator='eq' value='{serviceProviderId}'/>
                                                        <condition attribute='new_start_datetime' operator='on-or-after' value='{firstDay:yyyy-MM-dd}'/>
                                                        <condition attribute='new_end_datetime' operator='on-or-before' value='{lastDay:yyyy-MM-dd}'/>
                                                        <condition attribute='new_status' operator='not-in'><value>10</value><value>4</value></condition>
                                                        <condition attribute='statecode' operator='eq' value='0'/>
                                                    </filter>
                                                </entity>
                                               </fetch>";

            var query_schedulesEvents = new FetchExpression(fetchXmlScheduleEvevnts);
            EntityCollection ec_schedulesEvents = _crmService.RetrieveMultiple(query_schedulesEvents);

            // כל הימים שיש בהם שיבוץ
            HashSet<DateTime> busyDays = new HashSet<DateTime>();

            if (ec_schedulesEvents.Entities.Count > 0)
            {
                foreach (var ev in ec_schedulesEvents.Entities)
                {
                    var start = ev.GetAttributeValue<DateTime>("new_start_datetime");

                    // חשוב לקחת רק את התאריך בלי שעה
                    busyDays.Add(start.Date);
                }
            }

            if (ec_availabilityTimes.Entities.Count > 0)
            {
                foreach (Entity e_availabilityTimes in ec_availabilityTimes.Entities)
                {
                    DateTime startTime = e_availabilityTimes.GetAttributeValue<DateTime>("new_start_date_time1");

                    ServiceProviderAvailabilityTimeInDay availabilityTimes = new ServiceProviderAvailabilityTimeInDay
                    {
                        Id = e_availabilityTimes.Id,
                        StartTime = startTime,
                        EndTime = e_availabilityTimes.GetAttributeValue<DateTime>("new_end_date_time1"),
                        StartTime2 = e_availabilityTimes.GetAttributeValue<DateTime?>("new_start_date_time2"),
                        EndTime2 = e_availabilityTimes.GetAttributeValue<DateTime?>("new_end_date_time2"),
                        AvailabilityForMonthId = availabilityForMonthId,
                        IsBusy = busyDays.Contains(startTime.Date)
                    };

                    timesList.Add(availabilityTimes);
                }
            }
            return timesList;
        }

    }
}
