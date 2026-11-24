using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class ResourceService : BasicService<Resource, Resource, ResourceCRM>, IResourceService
    {
        private IResourceRepository resourceRepository;
        private IMapFunctionRepository mapFunctionRepository;
        private IEventItemRepository eventItemRepository;

        private string environment;

        public ResourceService(IResourceRepository resourceRepository,
                                      IHelperService helperService,
                                      IMapFunctionRepository mapFunctionRepository,
                                      IEventItemRepository eventItemRepository
            ) : base(helperService, resourceRepository, mapFunctionRepository)
        {
            this.resourceRepository = resourceRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.eventItemRepository = eventItemRepository;

            environment = ConfigurationManager.AppSettings["environment"];
        }

        public override decimal Count(string userId, List<Filter> filter)
        {
            List<Filter> new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end")).ToList();
            new_filter = helperService.BuildFilter<Resource, ResourceCRM, ResourceCRM>(new_filter);
            return resourceRepository.Count(userId, new_filter);
        }
        /*public override List<Resource> Get(string userId, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            List<Filter> new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end")).ToList();
            List<Filter> additionalFilter = filter.Except(new_filter).ToList();
            new_filter = helperService.BuildFilter<Resource, ResourceCRM, ResourceCRM>(new_filter);
            additionalFilter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(additionalFilter);
            sort = helperService.BuildSorting<Resource, ResourceCRM>(sort);
            return resourceRepository.Get(userId, paging, sort, new_filter, additionalFilter);
        }*/

        public override Tuple<int, List<Resource>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            //List<Filter> new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end")).ToList();
            //List<Filter> additionalFilter = filter.Except(new_filter).ToList();
            //new_filter = helperService.BuildFilter<Resource, ResourceCRM, ResourceCRM>(new_filter);
            //additionalFilter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(additionalFilter);
            //sort = helperService.BuildSorting<Resource, ResourceCRM>(sort);
            //return resourceRepository.GetFetchXml(userId, paging, sort, new_filter, additionalFilter);
            Filter eventReservationIdFilter = filter.Find(x => x.Alias == "eventReservation" && x.Field == "id");
            Filter reservationIdFilter = filter.Find(x => x.Alias == "reservation" && x.Field == "id");
            if(eventReservationIdFilter!=null)
               filter.Remove(eventReservationIdFilter);
            if (reservationIdFilter != null)
                filter.Remove(reservationIdFilter);


            List<Filter> new_filter = filter.Where(f => !f.Field.StartsWith("date") && !f.Field.StartsWith("start") && !f.Field.StartsWith("end")).ToList();
            List<Filter> datesRangeFilter = filter.Except(new_filter).ToList();
            List<Filter> additionalFilter = new List<Filter>();
            string additionalFetchXml = "";

            if (datesRangeFilter.Find(f => f.FilterType == "or") != null)
            {
                string start = datesRangeFilter.Find(f => f.Field == "start" && f.Operator == "gt" && f.FilterType == "or")?.Value;
                string end = datesRangeFilter.Find(f => f.Field == "start" && f.Operator == "lt" && f.FilterType == "or")?.Value;
                additionalFetchXml = $@"<filter type='or'>
                                     <filter type='and'>
                                        <condition attribute='new_start_datetime' operator='gt' value='{start}'/>
                                        <condition attribute='new_start_datetime' operator='lt' value='{end}'/>
                                      </filter>
                                      <filter type='and'>
                                        <condition attribute='new_start_datetime' operator='lt' value='{start}'/>
                                        <condition attribute='new_end_datetime' operator='gt' value='{start}'/>
                                      </filter>                                      
                                        <condition attribute='new_start_datetime' operator='eq' value='{start}'/>
                                        <condition attribute='new_end_datetime' operator='eq' value='{end}'/>                                     
                                   </filter>";
            }
            else
            {
                additionalFilter.AddRange(datesRangeFilter);
            }
            sort = helperService.BuildSorting<Resource, ResourceCRM>(sort);
            new_filter = helperService.BuildFilter<Resource, ResourceCRM, ResourceCRM>(new_filter);
            additionalFilter.Add(new Filter() { Field = "resource", Operator = "", Value = "XXX", Type = "not-null" });
            additionalFilter.Add(new Filter() { Field = "EventStatus", Type = "optionset", Operator = "not-eq", Value = "10" });
            additionalFilter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(additionalFilter);
            List<Resource> resourceList = resourceRepository.GetFetchXml(userId, false, paging, sort, new_filter, null).Item2;
            List<EventItem> eventItemList = eventItemRepository.GetFetchXml(userId, false, null, null, additionalFilter, additionalFetchXml).Item2;
            //  אם השיבוץ הוא שריון או משאב ליום שלם בשביל האירוע או ההזמנה הזאת- אז אפשר לשבץ אותו שוב
            // ולכן נסמן את השיבוצים שלא יהיהו תפוסים
            var matching = eventItemList.FindAll(x => (x.Reservation.Id == reservationIdFilter?.Value
            || x.EventReservation.Id == eventReservationIdFilter?.Value) && (x.EventType == 4 || x.EventType == 3 ))?.ToList();
            if (matching.Any())
            {
                eventItemList=eventItemList.Except(matching).ToList();
            }


            var temp1 = resourceList
             .GroupJoin(
                 eventItemList, resource => resource.Id, eventItem => eventItem.ScheduleItem.Id,
                 (x, y) =>
                 {
                   
                     x.IsBusy = y.Count() > 0;
                     return x;
                 }
             );
            

            List<Resource> temp2 = temp1.ToList();
            Tuple<int, List<Resource>> final = new Tuple<int, List<Resource>>(temp2.Count, temp2);
            return final;
        }

        public Tuple<int, List<Resource>> GetFetchXmlForRange(string userId, List<Sort> sort, List<List<Filter>> filter)
        {


            List<Filter> new_filter = filter.Last();
            List<List<Filter>> datesRangeFilter = filter.GetRange(0, filter.Count - 1);
            List<Filter> additionalFilter = new List<Filter>();
            string additionalFetchXml = "<filter type='or'>";

            foreach (List<Filter> datesRange in datesRangeFilter)
            {
                string start = datesRange.Find(f => f.Field == "start" && f.Operator == "gt" && f.FilterType == "or")?.Value;
                string end = datesRange.Find(f => f.Field == "start" && f.Operator == "lt" && f.FilterType == "or")?.Value;
                additionalFetchXml += $@"<filter type='and'>
                                        <condition attribute='new_start_datetime' operator='gt' value='{start}'/>
                                        <condition attribute='new_start_datetime' operator='lt' value='{end}'/>
                                      </filter>
                                      <filter type='and'>
                                        <condition attribute='new_start_datetime' operator='lt' value='{start}'/>
                                        <condition attribute='new_end_datetime' operator='gt' value='{start}'/>
                                      </filter>                                      
                                        <condition attribute='new_start_datetime' operator='eq' value='{start}'/>
                                        <condition attribute='new_end_datetime' operator='eq' value='{end}'/>";
            }
            additionalFetchXml += "</filter>";

            sort = helperService.BuildSorting<Resource, ResourceCRM>(sort);

            new_filter = helperService.BuildFilter<Resource, ResourceCRM, ResourceCRM>(new_filter);
            List<Resource> resourceList = resourceRepository.GetFetchXml(userId, false, null, sort, new_filter, null).Item2;

            additionalFilter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(additionalFilter);
                        additionalFilter.Add(new Filter()
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<EventItem, EventItemCRM>("Resource"),
                Value = new_filter.Find(f => f.Field == "new_resourceid").Value,
                Type = "lookup"
            });
            List<EventItem> eventItemList = eventItemRepository.GetFetchXml(userId, false, null, null, additionalFilter, additionalFetchXml).Item2;

            List<Resource> temp = new List<Resource>();

            filter.RemoveAll(f => f.Find(ff => ff.Field == "start") == null);

            foreach (List<Filter> f in filter)
            {
                Resource resource = helperService.Clone(resourceList.First());

                    resource.IsBusy = eventItemList.Find(item => DateTime.Parse(item.Start).Month == DateTime.Parse(f.First().Value).Month
                && DateTime.Parse(item.Start).Day == DateTime.Parse(f.First().Value).Day) != null;
                temp.Add(resource);
            }

            Tuple<int, List<Resource>> final = new Tuple<int, List<Resource>>(temp.Count, temp);
            return final;
        }

        public List<Resource> GetResourceList(string userId)
        {
            return resourceRepository.GetResourceList(userId);
        }

        public Resource GetMuseumResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"museumResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
        }

        public Resource GetCampusResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"campusResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
        }

        public Resource GetBigQuestionsRoomResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"bigQuestionsRoomResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
            //return Get(userId, breakProductGuid);
        }

        public Resource GetArchiveResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"archiveResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
            //return Get(userId, breakProductGuid);
        }

        public Resource GetBhadimCityResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"bhadimCityResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
            //return Get(userId, breakProductGuid);
        }

        public Resource GetHolocaustPerformanceResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"holocaustPerformanceResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
            //return Get(userId, breakProductGuid);
        }

        public Resource GetTheLifeBeforeResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"theLifeBeforeResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
            //return Get(userId, breakProductGuid);
        }

        public Resource GetBackToLifeResource(string userId)
        {
            var breakProductGuid = ConfigurationManager.AppSettings[$"backToLifeResourceId_{environment}"];
            return resourceRepository.GetMuseumResource(userId, breakProductGuid);
            //return Get(userId, breakProductGuid);
        }

    }
}
