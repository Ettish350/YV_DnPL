using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Xrm.Sdk.Discovery;
using Newtonsoft.Json;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Services.Services
{
    public class ActivityDayTypeService : IActivityDayTypeService
    {
        
        private readonly IActivityDayTypeRepository activityDayTypeRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IHelperService helperService;
        private readonly HttpClient sharedClient;
        private readonly string environment;


        public ActivityDayTypeService(
            IActivityDayTypeRepository activityDayTypeRepository,
            IMapFunctionRepository mapFunctionRepository,
            IHelperService helperService,
            HttpClient sharedClient)
        {
            this.activityDayTypeRepository = activityDayTypeRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.helperService = helperService;
            this.sharedClient = sharedClient;
            environment = ConfigurationManager.AppSettings["environment"];
        }

        /*public List<ActivityDayType> GetAll()
        {
            var sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<ActivityDayType, ActivityDayTypeCRM>("SortWeightHe"),
                Direction = "asc"
            };

            return activityDayTypeRepository.Get(userId, new List<Sort> { sort }, null);
        }

        public List<ActivityDayType> GetAllByFilter(string divisionRouter)
        {
            var filterList = new List<Filter>();

            filterList.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>("DivisionRouter"),
                Value = divisionRouter,
                Type = "optionset"
            });

            //Dictionary<string, List<Pricelist>> productlist = new Dictionary<string, List<Pricelist>>();
            var tempProductlist = activityDayTypeRepository.Get(userId, filterList);
            //foreach (var product in tempProductlist) {
            //    productlist.Add(product.Id, product.PriceList);
            // }
            // return productlist;
            return tempProductlist;
       
        }*/

        public List<ActivityDayType> GetAll(string userId, string divisionRouter)
        {

            // IdValuePair department = LoginRepository.userInfo.Department;
            try
            {
                List<Filter> filterList = new List<Filter>();
                filterList.Add(new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDayType, ActivityDayTypeCRM>("DivisionRouter"),
                    Value = divisionRouter,
                    Type = "optionset"
                });
                
                var sort = new Sort
                {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<ActivityDayType, ActivityDayTypeCRM>("SortWeightHe"),
                    Direction = "asc"
                };

                return activityDayTypeRepository.GetFetchXml(userId, false, new List<Sort> { sort }, filterList).Item2;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        public List<ActivityDayType> GetAllForEvents(string userId)
        {

            try
            {
                List<Filter> filterList = new List<Filter>
                {
                    new Filter
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDayType, ActivityDayTypeCRM>("IsNeedEvent"),
                        Value = "true",
                        Type = "bool"
                    },
                    new Filter
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<ActivityDayType, ActivityDayTypeCRM>("ShowInTahel"),
                        Value = "true",
                        Type = "bool"
                    }
                };

                var sort = new Sort
                {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<ActivityDayType, ActivityDayTypeCRM>("SortWeightHe"),
                    Direction = "asc"
                };

                return activityDayTypeRepository.GetFetchXml(userId, false, new List<Sort> { sort }, filterList).Item2;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private List<ActivityDayType> InternalServerError(Exception ex)
        {
            throw new NotImplementedException();
        }
        public async  Task<List<ActivityDayTypeListItem>> GetActivityDayTypeListWithSpecificTimesAsync(string userId,List<Filter>filters) {

            var activityTypeFilters = new List<Filter>();
            activityTypeFilters.AddRange(helperService.BuildFilter<ActivityDayType, ActivityDayTypeCRM, ActivityDayTypeCRM>(filters.FindAll(x => x.Alias == "TourType"||x.Alias == "")));
            var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                            <entity name=""new_tour_type"">
                                <attribute name=""new_name""/>
                                <attribute name=""new_tour_code""/>
                                <attribute name=""new_tour_typeid""/>
                                <attribute name=""new_duration_of_hours""/>
                                <attribute name=""new_event_reservations""/>
                                  {activityDayTypeRepository.FilterFetchXML(activityTypeFilters)}
                            </entity>
                          </fetch>";
            var activityDayTypelist = activityDayTypeRepository.GetActivityDayTypeListFetchXml(userId, fetchXml);
            //activityDayTypelist.ForEach(item => item.SpecificDateTimeList = GetSpecificTimeList(userId,item.Id, filters.FindAll(f => f.Alias == "SpecificDate")));
            var specificDateFilter = filters.Find(f => f.Alias == "SpecificDate");
            if (specificDateFilter != null && specificDateFilter.Value.Length > 0)
            {
                var dates = specificDateFilter.Value.Split(',');
                var startDate = DateTime.ParseExact(dates[0].Trim(),"dd/MM/yyyy",null);
                var endDate = DateTime.ParseExact(dates[1].Trim(), "dd/MM/yyyy", null);
                var month = startDate.Month.ToString();
                var year = startDate.Year.ToString();
                var newVisitorsCenterDatesAndTimesList = await GetNewVisitorsCenterDatesAndTimesAsync(year,month).ConfigureAwait(false);
                AddVisitorsCenterDatesAndTimesList(activityDayTypelist, newVisitorsCenterDatesAndTimesList);
                return activityDayTypelist;
            }
            return activityDayTypelist;

        }


        public void AddVisitorsCenterDatesAndTimesList(List<ActivityDayTypeListItem> activityDayTypelist, List<NewVisitorsCenterDatesAndTimes> newVisitorsCenterDatesAndTimesList) {
            foreach (var activityDayType in activityDayTypelist) {
                var datesList = newVisitorsCenterDatesAndTimesList.FindAll(item => item.ScheduleEventGuid == activityDayType.Id);
                activityDayType.SpecificDateTimeList = new List<string>();
                foreach (var visitorsCenterDate in datesList)
                {
                    var date = DateTime.ParseExact(visitorsCenterDate.Date+visitorsCenterDate.StartTime,"dd/MM/yyyyHH:mm",null);
                    activityDayType.SpecificDateTimeList.Add(date.ToString("dd/MM/yyyy HH:mm:ss"));
                }

            }
        }
        public async Task<List<NewVisitorsCenterDatesAndTimes>> GetNewVisitorsCenterDatesAndTimesAsync(string year,string month) {
            try
            {
                string baseUrl = ConfigurationManager.AppSettings[$"crmConnctionBaseUrl_{environment}"].ToString();
                //{ baseUrl}
                var uri = $"{baseUrl}/OnlineReservation/GetNewVisitorsCenterDatesAndTimes?year={year}&month={month}";
                var response = await sharedClient.GetAsync(uri).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var res = JsonConvert.DeserializeObject<NewVisitorsCenterDatesAndTimesResponse>(content);
                    return res.ResponseObject;
                }
                else {
                    throw new Exception("GetNewVisitorsCenterDatesAndTimes faild");
                }
            }

            catch (Exception ex)
            {
                Trace.TraceError("GetNewVisitorsCenterDatesAndTimes error:" + ex.Message);
                throw ex;
            }

        }
        public List<string> GetSpecificTimeList(string userId,string activityDayTypeGuid,List<Filter> filters) { 
            var specificTimeList = new List<string>();
            var specificTimeFeild = "new_start_time";

            if (filters.Count > 0) {
                filters.First().Field = specificTimeFeild;
            }
            var fetchXml = $@"<fetch version=""1.0"" output-format=""xml-platform"" mapping=""logical"" distinct=""true"">
                                <entity name=""new_spcific_date_and_hour"">
                                    <attribute name=""new_start_time""/>
                                      <filter type=""and"">
                                        <condition attribute=""new_tour_type"" operator=""eq"" value=""{activityDayTypeGuid}""/>
                                       </filter>
                                        {activityDayTypeRepository.FilterFetchXML(filters)}
                                 </entity>
                               </fetch>";
            var entities = activityDayTypeRepository.GeneralFetchXml(userId, fetchXml);
            entities.ForEach(entity => specificTimeList.Add(entity.GetAttributeValue<DateTime>(specificTimeFeild).ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")));
            return specificTimeList;
        }
    }
}
