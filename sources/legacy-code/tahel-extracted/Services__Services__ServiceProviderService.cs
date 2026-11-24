using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;

namespace Services.Services
{
    public enum ServiceProviderType
    {
        None = 0,
        GuideEmployee = 1,
        LecturerEmployee = 2,
        Testimony = 3,
        Employee = 4,
        BatSherut = 5,
        GuidePoland = 6,
        Cantor = 7,
        Actor = 8
    }
    public class ServiceProviderService : BasicService<ServiceProviderItem, ServiceProvider, ServiceProviderCRM>, IServiceProviderService
    {
        private IServiceProviderRepository serviceProviderRepository;
        private IMapFunctionRepository mapFunctionRepository;
        private IEventItemRepository eventItemRepository;
        private IContractDaysRepository contractDaysRepository;
        private IProductRepository productRepository;
        private IQueryHelperRepository queryHelperRepository;
        private IAvailabilityForMonthRepository availabilityForMonthRepository;
        private IAvailabilityTimeInDayRepository availabilityTimeInDayRepository;
        public ServiceProviderService(IServiceProviderRepository serviceProviderRepository,
                                      IHelperService helperService,
                                      IMapFunctionRepository mapFunctionRepository,
                                      IEventItemRepository eventItemRepository,
                                      IContractDaysRepository contractDaysRepository,
                                      IProductRepository productRepository,
                                      IQueryHelperRepository queryHelperRepository,
                                      IAvailabilityForMonthRepository availabilityForMonthRepository,
                                      IAvailabilityTimeInDayRepository availabilityTimeInDayRepository
            ) : base(helperService, serviceProviderRepository, mapFunctionRepository)
        {
            this.serviceProviderRepository = serviceProviderRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.eventItemRepository = eventItemRepository;
            this.contractDaysRepository = contractDaysRepository;
            this.productRepository = productRepository;
            this.queryHelperRepository = queryHelperRepository;
            this.availabilityForMonthRepository = availabilityForMonthRepository;
            this.availabilityTimeInDayRepository = availabilityTimeInDayRepository;
        }

        public override decimal Count(string userId, List<Filter> filter)
        {
            List<Filter> new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end")).ToList();
            new_filter = helperService.BuildFilter<ServiceProvider, ServiceProviderCRM, ServiceProviderCRM>(new_filter);
            return serviceProviderRepository.Count(userId, new_filter);
        }
        /*public override List<ServiceProvider> Get(string userId, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            List<Filter> new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end")).ToList();
            List<Filter> additionalFilter = filter.Except(new_filter).ToList();
            new_filter = helperService.BuildFilter<ServiceProvider, ServiceProviderCRM, ServiceProviderCRM>(new_filter);
            additionalFilter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(additionalFilter);

            // sort = null;
            return serviceProviderRepository.Get(userId, paging, sort, new_filter, additionalFilter);
        }*/

        public override Tuple<int, List<ServiceProvider>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            List<Filter> new_filter = new List<Filter>();
            List<Filter> additionalFilter = new List<Filter>();
            Tuple<int, List<ServiceProvider>> serviceProviderList;
            string fetchXml2 = "";
            if (filter != null && filter.Find(x => x.Alias == "Languages") != null)
            {
                string language = filter.Find(x => x.Alias == "Languages").Value;
                fetchXml2 += @"<link-entity name='new_new_service_provider_new_language' from='new_service_providerid' to='new_service_providerid' visible='false' intersect='true'>
                   <link-entity name='new_language' from='new_languageid' to='new_languageid' alias='Language'><filter type='and'>
                   <condition attribute='new_languageid' operator='eq'  uitype='new_language' value='{" + language + @"}' /></filter></link-entity></link-entity>";
                filter.Remove(filter.Find(x => x.Alias == "Languages"));
            }
            if (filter != null)
            {
                new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end")).ToList();
                additionalFilter = filter.Except(new_filter).ToList();
                additionalFilter.Add(new Filter() { Field = "serviceProvider", Operator = "", Value = "XXX", Type = "not-null" });//ADINA BUG!
                new_filter = helperService.BuildFilter<ServiceProvider, ServiceProviderCRM, ServiceProviderCRM>(new_filter);
                additionalFilter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(additionalFilter);

            }


            sort = helperService.BuildSorting<ServiceProvider, ServiceProviderCRM>(sort);
            if (fetchXml2 == "")
                serviceProviderList = serviceProviderRepository.GetFetchXml(userId, false, paging, sort, new_filter, null);
            else
                serviceProviderList = serviceProviderRepository.GetFetchXml(userId, false, paging, sort, new_filter, fetchXml2);

            Tuple<int, List<ServiceProvider>> final = new Tuple<int, List<ServiceProvider>>(serviceProviderList.Item1, serviceProviderList.Item2);
            return final;
        }
        public Tuple<int, List<ServiceProvider>> GetFullFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            List<Filter> status_filter = new List<Filter>();
            List<Filter> new_filter = new List<Filter>();
            List<Filter> additionalFilter = new List<Filter>();
            List<ServiceProvider> serviceProviderList = new List<ServiceProvider>();
            Filter languageFilter = null;
            string additionalfetchXml = "";
            if (filter != null && filter.Find(x => x.Alias == "Languages") != null)
            {
                languageFilter = filter.Find(x => x.Alias == "Languages");
                filter.Remove(filter.Find(x => x.Alias == "Languages"));
            }
            if (filter != null)
            {
                status_filter = filter.Where(f => f.Alias == "ServiceProviderStatus").ToList();
                new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end") && f.Alias != "ServiceProviderStatus").ToList();
                additionalFilter = filter.Except(new_filter).ToList();
                additionalFilter = additionalFilter.Except(status_filter).ToList();
                additionalFilter.Add(new Filter() { Field = "serviceProvider", Operator = "", Value = "XXX", Type = "not-null" });//ADINA BUG!
                status_filter = helperService.BuildFilter<ServiceProviderStatus, ServiceProviderStatusCRM, ServiceProviderStatusCRM>(status_filter);
                new_filter = helperService.BuildFilter<ServiceProvider, ServiceProviderCRM, ServiceProviderCRM>(new_filter);
                additionalFilter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(additionalFilter);

            }

            List<Sort> serviceProviderSort = new List<Sort>();

            if (sort != null && !isAggregate)
            {
                sort.ForEach(x =>
                {
                    if (x.Active.Contains(":"))
                    {
                        x.Alias = x.Active.Split(':')[1];
                        x.Active = x.Active.Split(':')[0];
                    }
                });

                serviceProviderSort = helperService.BuildSorting<ServiceProviderItem, ServiceProviderItemCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "ServiceProvider"));
            }
            if (status_filter != null && status_filter.Count > 0)
            {
                additionalfetchXml = @"<link-entity name='new_service_provider_status' from='new_service_provider' to='new_service_providerid' alias='ad'> 
                            <filter type='and'>
                                <condition attribute='new_service_provider_type' operator='in'>";
                foreach (var item in status_filter)
                {
                    additionalfetchXml += @"<value>" + item.Value + "</value>";
                }
                additionalfetchXml += @"</condition>    
                                <condition attribute='statecode' operator='eq' value='0' />
                            </filter>
                            </link-entity>";
            }
            if (languageFilter == null)
            {

                serviceProviderList = serviceProviderRepository.GetFetchXml(userId, false, paging, serviceProviderSort, new_filter, additionalfetchXml).Item2;
            }
            else
            {
                string[] tempList = languageFilter.Value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                string fetchXml2 = @"
  <link-entity name='new_new_service_provider_new_language' 
               from='new_service_providerid' 
               to='new_service_providerid' 
               visible='false' intersect='true'>
    <link-entity name='new_language' from='new_languageid' to='new_languageid' alias='Language'>
      <filter type='and'>
        <condition attribute='new_languageid' operator='in'>";

                foreach (var lang in tempList)
                {
                    fetchXml2 += $"<value>{{{lang}}}</value>";
                }

                fetchXml2 += @"</condition>
      </filter>
    </link-entity>
  </link-entity>";

                fetchXml2 += additionalfetchXml;
                serviceProviderList = serviceProviderRepository.GetFetchXml(userId, false, paging, serviceProviderSort, new_filter, fetchXml2).Item2;
            }
            //foreach (var item in serviceProviderList)
            //{
            //    foreach (var item2 in item.StatusList)
            //    {
            //        if (item2.EndDate!="" && DateTime.Parse(item2.EndDate) < DateTime.Parse(additionalFilter.Find(x => x.Field == "new_start_datetime").Value.ToString()))
            //        {
            //            serviceProviderList.Remove(item);
            //        }
            //    }
            //}

            string languageFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='new_new_service_provider_new_language'>
    <attribute name='new_service_providerid' />
    <attribute name='new_languageid' />
    <link-entity name='new_language'
                 from='new_languageid'
                 to='new_languageid'
                 alias='lang'>
<attribute name='new_name' />
    </link-entity>
  </entity>
</fetch>";

            var list = basicRepository.GetAllListEntity(userId, languageFetchXml);

            foreach(var langEntity in list)
            {
                var serviceProviderId = langEntity.GetAttributeValue<Guid>("new_service_providerid").ToString();
                var langName = ((AliasedValue)langEntity["lang.new_name"]).Value.ToString();
                var langId = langEntity.GetAttributeValue<Guid>("new_languageid").ToString();

                var provider = serviceProviderList.FirstOrDefault(p => p.Id == serviceProviderId);
                if (provider != null)
                {
                    provider.LanguagesList.Add(new IdValuePair { Id = langId, Value = langName});
                }
            }

            string certificationFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='new_new_service_provider_new_certification'>
    <attribute name='new_service_providerid' />
    <attribute name='new_certificationid' />
    <link-entity name='new_certification'
                 from='new_certificationid'
                 to='new_certificationid'
                 alias='cert'>
<attribute name='new_name' />
    </link-entity>
  </entity>
</fetch>";

            var list1 = basicRepository.GetAllListEntity(userId, certificationFetchXml);

            foreach (var certEntity in list1)
            {
                var serviceProviderId = certEntity.GetAttributeValue<Guid>("new_service_providerid").ToString();
                var certName = ((AliasedValue)certEntity["cert.new_name"]).Value.ToString();
                var certId = certEntity.GetAttributeValue<Guid>("new_certificationid").ToString();

                var provider = serviceProviderList.FirstOrDefault(p => p.Id == serviceProviderId);
                if (provider != null)
                {
                    provider.CertificationsList.Add(new IdValuePair { Id = certId, Value = certName });
                }
            }


            //string availabilityFetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
            //                                  <entity name='new_availability_times_day_in_month'>
            //                                      <attribute name='new_availability_times_day_in_monthid' />
            //                                      <attribute name='new_start_date_time1' />
            //                                      <attribute name='new_end_date_time1' />
            //                                      <attribute name='new_start_date_time2' />
            //                                      <attribute name='new_end_date_time2' />
            //                                      <attribute name='new_availability_in_month' />
            //                                    <filter>
            //                                      <condition attribute='new_availability_month' operator='on' value='{}' />
            //                                    </filter>
            //                                  </entity>
            //                                </fetch>";

            //var availabilityList = basicRepository.GetAllListEntity(userId, availabilityFetchXml);
            //foreach (var availabilityEntity in availabilityList)
            //{
            //    var serviceProviderId = availabilityEntity.GetAttributeValue<Guid>("new_service_providerid").ToString();
            //    var provider = serviceProviderList.FirstOrDefault(p => p.Id == serviceProviderId);

            //    if (provider.AvailabilityForMonth == null)
            //    {
            //        provider.AvailabilityForMonth = availabilityForMonthRepository.SetItem(availabilityEntity);
            //    }
            //    AvailabilityTimeInDay day = availabilityTimeInDayRepository.SetItem(availabilityEntity);
            //    if (day != null)
            //    {
            //        provider.AvailabilityForMonth.AvailabilityTimes.Add(day);
            //    }
            //}

            List<EventItem> eventItemList = eventItemRepository.GetFetchXml(userId, false, null, null, additionalFilter, null).Item2;


            List<Filter> filterList = new List<Filter>();
            DateTime serverDateTime = DateTime.Parse(additionalFilter.Find(f => f.Field.Contains("start_datetime")).Value);
            var dateOnly = serverDateTime.Date;

            filterList.Add(new Filter
            {
                Field = "new_start_date_time1",
                Value = dateOnly.ToString("yyyy-MM-ddT00:00:00Z"),
                Operator = "ge",
                Type = "date"
            });

            filterList.Add(new Filter
            {
                Field = "new_start_date_time1",
                Value = dateOnly.ToString("yyyy-MM-ddT23:59:59Z"),
                Operator = "lt",
                Type = "date"
            });


            string avaliablityFetchXml = $@"<all-attributes/>
                                            <link-entity name='new_availability_service_provider_in_month' from='new_availability_service_provider_in_monthid' to='new_availability_in_month' alias='AvailabilityForMonth'>
                                                <all-attributes />
                                            </link-entity>";
            List<AvailabilityTimeInDay> availabilityTimeInDays = availabilityTimeInDayRepository.GetFetchXml(userId, false, null, null, filterList, avaliablityFetchXml).Item2;


            List<ContractDays> contractDaysList = new List<ContractDays>();
            if (!isAggregate)
            {
                contractDaysList = contractDaysRepository.GetFetchXml(userId, false, null).Item2;
            }

            var temp1 = serviceProviderList
                            .GroupJoin(
                                eventItemList,
                                serviceProvider => serviceProvider.Id,
                                eventItem => eventItem.ScheduleItem.Id,
                                (sp, events) =>
                                {
                                    sp.IsBusy = events.Any();

                                    if (!isAggregate)
                                        sp.ContractDaysByYear = contractDaysList
                                            .FindAll(cd => cd.ServiceProvider == sp.Id);
                                    sp.AvailabilityTimeInDays = availabilityTimeInDays
                                            .Where(av => av.ServiceProvider.Id == sp.Id)
                                            .ToList();
                                    return sp;
                                }
                            );

            List<ServiceProvider> temp2 = temp1.ToList();
            // יוצרים את הטופל להחזרה
            Tuple<int, List<ServiceProvider>> final = new Tuple<int, List<ServiceProvider>>(temp2.Count, temp2);
            return final;

        }

        public ServiceProviderForReservation GetServiceProviderForReservation(string userId, string guid)
        {
            return serviceProviderRepository.GetServiceProviderForReservation(userId, guid);
        }

        public List<KeyValuePair<string, List<string>>> GetServiceProvidersNamesAndEmails(string userId, List<string> list, bool fullData)
        {
            string tmp = "";
            tmp += "<filter type='or'>";
            var disList = list.Distinct();
            foreach (var str in disList)
            {
                tmp += "<condition attribute='new_service_providerid' operator= 'eq' value ='" + str + "'/>";
            }
            tmp += "</filter>";
            var result = serviceProviderRepository.GetFetchXml(userId, false, null, tmp);
            var ret = result.Item2
                .Where(item => fullData || !string.IsNullOrWhiteSpace(item.CommunicationInfo.Email))
                .Select(i => new { Key = i.Id.ToString(), mail = i.CommunicationInfo.Email.ToString(), name = i.FirstName + " " + i.LastName })
                .Distinct().ToList();
            //todo
            List<KeyValuePair<string, List<string>>> list1 = new List<KeyValuePair<string, List<string>>>();
            foreach (var item in ret)
            {
                var valueList = new List<string>();
                valueList.Add(item.name);
                valueList.Add(item.mail);

                list1.Add(new KeyValuePair<string, List<string>>(item.Key, valueList));
            }
            return list1;

        }

        public List<IdValuePair> GetServiceProvidersIdValue(string userId, List<Filter> filters)
        {
            var result = serviceProviderRepository.GetFetchXml(userId, false, null, filters);
            var ret = result.Item2
                .Select(i => new { Id = i.Id.ToString(), Value = i.FullName.ToString() })
                .Distinct().ToList();
            //todo
            List<IdValuePair> list1 = new List<IdValuePair>();
            foreach (var item in ret)
            {
                list1.Add(new IdValuePair(item.Id, item.Value));
            }
            return list1;
        }



        public List<ServiceProvider> GetServiceProviderListDetails(string userId, List<IdValuePair> list)
        {
            string tmp = "";
            tmp += "<filter type='or'>";
            var disList = list.Distinct();
            foreach (var str in disList)
            {
                tmp += "<condition attribute='new_service_providerid' operator= 'eq' value ='" + str.Id + "'/>";
            }
            tmp += "</filter>";
            var result = serviceProviderRepository.GetFetchXml(userId, false, null, tmp);
            return result.Item2;

        }

        public List<ServiceProvider> GetServiceProvidersWithContractDay(string userId, List<Filter> filters)
        {
            Filter filter = null;
            string tmp = @"<link-entity name='new_contract_days' from='new_contract_days' to='new_service_providerid' alias='ContractDays' link-type='inner'>
                    <filter type='and' >
                        <condition attribute ='new_yv_product' operator='not-null' />
                    </filter >
                 </link-entity >";
            if (filters != null)
            {
                filters = helperService.BuildFilter<ServiceProvider, ServiceProviderCRM, ServiceProviderCRM>(filters.FindAll(f => f.Alias == "serviceProvider"));
                filter = filters.Find(f => f.Alias == "serviceProvider");
            }

            var serviceProviderList = serviceProviderRepository.GetFetchXml(userId, false, filter, tmp).Item2;
            var contractDaysList = contractDaysRepository.GetFetchXml(userId, false, null).Item2;

            serviceProviderList.ToLookup(serviceProvider => serviceProvider.ContractDaysByYear = contractDaysList.FindAll(c => c.ServiceProvider == serviceProvider.Id));

            return serviceProviderList;

        }

        public List<ServiceProviderContractDayNotSchedule> GetServiceProviderWithContractDaysNotSchedule(string userId, DateTime startDate, DateTime endDate)
        {
            string fetchXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='new_service_provider'>
                                    <all-attributes />";

            fetchXml += $@"<link-entity name='new_contract_days' from='new_contract_days' to='new_service_providerid' alias='ContractDays' link-type='inner'>
                                      <attribute name='new_contract_daysid' />
                                      <attribute name='new_from_date' />
                                      <attribute name='new_to_date' />
                                      <attribute name='new_contract_day_sunday' />
                                      <attribute name='new_contract_day_monday' />
                                      <attribute name='new_contract_day_tuesday' />
                                      <attribute name='new_contract_day_wednesday' />
                                      <attribute name='new_contract_day_thursday' />
                                      <attribute name='new_contract_day_friday' />
                                      <attribute name='new_yv_product' />
                                     <filter type='and'>
                                      <condition attribute='new_from_date'
                                                 operator='on-or-before'
                                                 value='{endDate.ToString("yyyy-MM-dd")}' />
                                      <condition attribute='new_to_date'
                                                 operator='on-or-after'
                                                 value='{startDate.ToString("yyyy-MM-dd")}' />
                                    </filter>";
            //var filter = new Filter { Field = "StartDate", Value };
            //contractDaysRepository.GetLinkedEntity(false, )
            fetchXml += productRepository.GetLinkedEntity(false, null, null, "inner");

            fetchXml += $@"</link-entity>
                                    <link-entity name='new_event_item' from='new_service_provider' to='new_service_providerid' alias='EventItem' link-type='outer'>
                                      <attribute name='new_start_datetime' />
                                      <attribute name='new_end_datetime' />
                                      <attribute name='new_event_itemid' />
                                      <filter type='and'>
                                        <condition attribute='new_start_datetime' operator='on-or-after' value='{startDate.ToString("yyyy-MM-dd")}'/>
                                        <condition attribute='new_end_datetime' operator='on-or-before' value='{endDate.ToString("yyyy-MM-dd")}'/>
                                        <filter type='or'>
                                            <filter type='and'>
                                            <condition attribute='statecode' operator='eq' value='0'/>
                                            <condition attribute='new_status' operator='ne' value='10' />
                                            </filter>
                                            <filter type='and'>
                                              <condition attribute='new_status' operator='eq' value='10' />
                                              <condition attribute='new_event_type' operator='eq' value='20' />
                                            </filter>
                                          </filter>
                                      </filter>
                                    </link-entity>
                                  </entity>
                                </fetch>";

            List<Entity> entities = serviceProviderRepository.GetAllListEntity(userId, fetchXml);

            var serviceProviderDict = new Dictionary<string, ServiceProviderForContractDays>();
            var spContractIds = new Dictionary<string, HashSet<string>>(); // SP Id -> Set של Contract Ids שכבר נבנו
            var spEventIds = new Dictionary<string, HashSet<string>>();

            foreach (var entity in entities)
            {
                string spId = entity.Id.ToString();

                // בונים פעם אחת את הספק
                if (!serviceProviderDict.TryGetValue(spId, out var provider))
                {
                    ServiceProvider sp = serviceProviderRepository.SetItem(userId, entity);
                    provider = queryHelperRepository.CopyToDerived<ServiceProvider, ServiceProviderForContractDays>(sp);
                    provider.ContractDaysByYear = new List<ContractDays>(); // ריק – נוסיף אחר כך
                    provider.EventsList = new List<EventItem>();
                    serviceProviderDict[spId] = provider;
                    spContractIds[spId] = new HashSet<string>();
                    spEventIds[spId] = new HashSet<string>();
                }

                // בונים חוזה רק אם SP עדיין לא בנה את החוזה הזה
                string contractId = contractDaysRepository.GetItemId(userId, entity); // או מה שיש לך כ-Id
                if (!string.IsNullOrEmpty(contractId) && !spContractIds[spId].Contains(contractId))
                {
                    var contract = serviceProviderRepository.BuildContractDaysForServiceProvider(userId, entity);
                    if (contract != null)
                    {
                        provider.ContractDaysByYear.Add(contract);
                        spContractIds[spId].Add(contractId);
                    }
                }

                // מוסיפים את האירוע
                string eventId = eventItemRepository.GetItemId(userId, entity);

                if (!string.IsNullOrEmpty(eventId) && !spEventIds[spId].Contains(eventId))
                {
                    var eventItem = serviceProviderRepository.BuildEventItemForServiceProvider(userId, entity);

                    if (eventItem != null)
                    {
                        provider.EventsList.Add(eventItem);
                        spEventIds[spId].Add(eventId);
                    }
                }
            }

            return GetDayNotScheduleContractDays(serviceProviderDict, startDate, endDate)
                .Distinct()
                .OrderBy(x => x.ScheduleDate)
                .ThenBy(x => x.ServiceProvider.FullName)
                 .ToList();
        }

        private List<ServiceProviderContractDayNotSchedule> GetDayNotScheduleContractDays(Dictionary<string, ServiceProviderForContractDays> serviceProviderDict, DateTime startDate, DateTime endDate)
        {
            List<ServiceProviderContractDayNotSchedule> notScheduleList = new List<ServiceProviderContractDayNotSchedule>();

            // טווח תאריכים מוכן מראש
            int totalDays = (endDate.Date - startDate.Date).Days + 1;
            var dateRange = new DateTime[totalDays];
            for (int i = 0; i < totalDays; i++)
                dateRange[i] = startDate.Date.AddDays(i);

            foreach (var serviceProvider in serviceProviderDict.Values)
            {
                // HashSet של כל התאריכים בהם יש אירוע כדי לבדוק במהירות
                var eventDates = new HashSet<DateTime>(
                    serviceProvider.EventsList.Select(e =>
                        DateTime.ParseExact(e.Start, "dd/MM/yyyy", null).Date)
                );

                // עבור כל חוזה של נותן השירות
                foreach (var contract in serviceProvider.ContractDaysByYear)
                {
                    // המרת תאריכי חוזה מ-string ל-DateTime
                    if (!DateTime.TryParseExact(contract.Start,
                        new[] { "M/d/yyyy h:mm:ss tt", "MM/dd/yyyy h:mm:ss tt" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime fromDate))
                        continue;

                    if (!DateTime.TryParseExact(contract.End,
                        new[] { "M/d/yyyy h:mm:ss tt", "MM/dd/yyyy h:mm:ss tt" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime toDate))
                        continue;

                    // HashSet לימי חוזה
                    var contractDays = new HashSet<DayOfWeek>();
                    if (contract.ContractDay_Sunday) contractDays.Add(DayOfWeek.Sunday);
                    if (contract.ContractDay_Monday) contractDays.Add(DayOfWeek.Monday);
                    if (contract.ContractDay_Tuesday) contractDays.Add(DayOfWeek.Tuesday);
                    if (contract.ContractDay_Wednesday) contractDays.Add(DayOfWeek.Wednesday);
                    if (contract.ContractDay_Thursday) contractDays.Add(DayOfWeek.Thursday);
                    if (contract.ContractDay_Friday) contractDays.Add(DayOfWeek.Friday);

                    foreach (var date in dateRange)
                    {
                        // לבדוק שהתאריך בטווח החוזה
                        if (date < fromDate || date > toDate)
                            continue;

                        var schedule = notScheduleList.Find(item => item.ServiceProvider.Id == serviceProvider.Id && item.ScheduleDate.Date == date.Date);
                        
                        if (contractDays.Contains(date.DayOfWeek) && !eventDates.Contains(date) && schedule == null)
                        {
                            notScheduleList.Add(new ServiceProviderContractDayNotSchedule
                            {
                                ServiceProvider = serviceProvider,
                                IdNumber = serviceProvider.IdNumber,
                                ContractDay = (int)date.DayOfWeek,
                                Product = contract.FullProduct,
                                ScheduleDate = date
                            });
                        }
                    }
                }
            }

            return notScheduleList;
        }


    }
}
