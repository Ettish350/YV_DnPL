using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class ScheduleOccupancyService: IScheduleOccupancyService
    {
        //public static readonly int TIME_SLOT_MINTUE = 10;
        
        IHelperService helperService;
        IResourceService resourceService;
        IEventItemRepository eventItemRepository;
        IScheduleDateService scheduleDateService;
        IQueryHelperRepository queryHelperRepository;

        public ScheduleOccupancyService(IHelperService helperService,
                                        IResourceService resourceService,
                                        IScheduleDateService scheduleDateService,
                                        IEventItemRepository eventItemRepository,
                                        IQueryHelperRepository queryHelperRepository)
        {
            this.helperService = helperService;
            this.scheduleDateService = scheduleDateService;
            this.eventItemRepository = eventItemRepository;
            this.queryHelperRepository = queryHelperRepository;
            this.resourceService = resourceService;
        }

        public List<ScheduleOccupancyItem> Get(string userId, List<Filter> filter)
        {
            List<Filter> startList = filter.Where(s => s.Field == "start").ToList();
            if (startList != null)
            {
                try
                {
                    string additionalFetchXml = @"<attribute name='new_start_datetime'/>
                                                <attribute name='new_number_of_participants'/>
                                                <attribute name='new_event_itemid'/>";
                    if (filter.Find(f => f.Alias == "ActivityDay" && f.Field == "activityDayStatus") != null)
                    {
                        additionalFetchXml += @"<link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ac' link-type='outer'>
                                          <filter type='and'>
                                            <condition attribute='new_activity_day_status' operator='ne' value='3' />
                                          </filter>
                                        </link-entity>";
                    }
                    filter.Add(new Filter() { Field = "EventStatus", Operator = "not-eq", Type = "optionset", Value = "10" });
                    filter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(f => string.IsNullOrEmpty(f.Alias) || f.Alias == "EventItem"));

                    //List<EventItem> eventList = eventItemRepository.Get(userId, null, null, filter);
                    //DateTime dateTime = DateTime.ParseExact(startList.First().Value, "dd/MM/yy 00:00:00", null);

                    //Shira fix date format 18.5.21
                    //Trace.WriteLine("before  eventItemRepository.GetFetchXml");
                    var list = eventItemRepository.GetFetchXmlGeneralEntity(userId, false, null,null, filter, additionalFetchXml);
                    //Trace.WriteLine("after  eventItemRepository.GetFetchXml");
                    // Trace.WriteLine("PARSE:"+ startList.First().Value);
                    List<EventItem> eventList = new List<EventItem>();
                    list.ForEach(element => eventList.Add(new EventItem()
                    {
                        Start = queryHelperRepository.GetValueDateByAttributeName<EventItem, EventItemCRM>(element, "Start").ToString("MM/dd/yyyy HH:mm:ss"),
                        NumberOfVisitors = (int)queryHelperRepository.GetValueDecimalByAttributeName<EventItem, EventItemCRM>(element, "NumberOfVisitors")
                    }));
                    DateTime dateTime = DateTime.Parse(startList.First().Value);
                    return GetOccupancyList(userId, dateTime, eventList);

                }
                catch (Exception ex)
                {
                    Trace.WriteLine("IN CATCH PARSE:"+ startList.First().Value);
                    Trace.WriteLine("Error in service scheduleEvent ex:" + ex.Message);
                    throw;
                }
            }
            return new List<ScheduleOccupancyItem>();
        }
        private List<ScheduleOccupancyItem> GetOccupancyList(string userId, DateTime startDate, List<EventItem> eventList)
        {
            //IdValuePair commemorationDivision = new IdValuePair() { Id = "2", Value = "הנצחה" };
            var schoolDivision = new Department { Id = "1", Number = 1, Name = "בית ספר" };
            var commemorationDivision = new Department { Id = "2", Number = 2, Name = "הנצחה" };
            List<ScheduleOccupancyItem> scheduleOccupancyItems = new List<ScheduleOccupancyItem>();
            DateTime startTime = GetMuseumOpeningHour(userId, startDate);
            int maxCapability = resourceService.GetMuseumResource(userId).Capacity;
            DateTime endTime = GetMuseumClosingHour(userId, startDate);
            var division = schoolDivision;
            int index = 0;

            var result =eventList.GroupBy(e => e.Start).Select(x => new { Start = DateTime.Parse(x.Key), SumOfVisitors = x.Sum(a => a.NumberOfVisitors) }).ToList();
            List<ScheduleDate> scheduleDates = scheduleDateService.GetMuseumClosedTimesByDate(userId, startTime.ToString());

            for (DateTime date = startTime; date <= endTime; date = date.AddMinutes(queryHelperRepository.TIME_SLOT_MINTUE))//copy
            {
                foreach (ScheduleDate item in scheduleDates)
                {
                    if (date == DateTime.Parse(item.Start))
                    {
                        if(item.ClosingType.Id == "7")// פתיחת מוזיאון
                        {
                            endTime = DateTime.Parse(item.End);
                        }
                        else
                        {
                            date = DateTime.Parse(item.End);
                        }
                        continue;
                    }
                }

                var eventItem = result.Find(item => item.Start == date);
                int numOfVisitors = eventItem ==null?0:eventItem.SumOfVisitors;
               // int numOfVisitors = eventList.Where(s => DateTime.ParseExact(s.Start, "MM/dd/yyyy HH:mm:ss", null) == date).Sum(a => a.NumberOfVisitors);
                //int numOfVisitors = eventList.Where(s => DateTime.Parse(s.Start) == date).Sum(a => a.NumberOfVisitors);
                /*Shira Slash cleaning museum 20.5.21
                 * if (date.Hour == 12 && date.Minute == 30)
                {
                    date = date.AddMinutes(20);
                    continue;
                }*/
                switch (date.Minute)
                {
                    case 00:
                    case 10:
                    case 20:
                        division = commemorationDivision;
                        break;
                    default:
                        division = schoolDivision;
                        /*if (date.Hour == 8 && date.Minute == 30)
                        {
                            division = commemorationDivision;
                        }*/
                        break;
                }

                ScheduleOccupancyItem scheduleOccupancyItem = new ScheduleOccupancyItem()
                {
                    Start = date,
                    Id = index++,
                    End = date.AddMinutes(queryHelperRepository.TIME_SLOT_MINTUE),
                    Title = division.Name,
                    Division = division,
                    MaxCapability = maxCapability,
                    NumberOfVisitors = numOfVisitors,
                    IsAllDay = false,
                    IsReadOnly = true
                };
                scheduleOccupancyItems.Add(scheduleOccupancyItem);
            }
            return scheduleOccupancyItems;
        }


        public List<ScheduleOccupancyItem> GetBhadimCityScheduleOccupancy(string userId, List<Filter> filter)
        {
            List<Filter> startList = filter.Where(s => s.Field == "start").ToList();
            if (startList != null)
            {
                try
                {
                    string additionalFetchXml = "";
                    if (filter.Find(f => f.Alias == "ActivityDay" && f.Field == "activityDayStatus") != null)
                    {
                        additionalFetchXml = @"<link-entity name='new_activity_day' from='new_activity_dayid' to='new_activity_day' alias='ac'>
                                          <filter type='and'>
                                            <condition attribute='new_activity_day_status' operator='ne' value='3' />
                                          </filter>
                                        </link-entity>";
                    }
                    filter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter.FindAll(f => string.IsNullOrEmpty(f.Alias) || f.Alias == "EventItem"));
                    // filter = helperService.BuildFilter<EventItem, EventItemCRM, EventItemCRM>(filter);
                    //List<EventItem> eventList = eventItemRepository.Get(userId, null, null, filter);
                    //DateTime dateTime = DateTime.ParseExact(startList.First().Value, "dd/MM/yy 00:00:00", null);

                    //Shira fix date format 18.5.21
                    //Trace.WriteLine("before  eventItemRepository.GetFetchXml");
                    List<EventItem> eventList = eventItemRepository.GetFetchXml(userId, false, null, null, filter, additionalFetchXml).Item2;
                    //Trace.WriteLine("after  eventItemRepository.GetFetchXml");
                    // Trace.WriteLine("PARSE:"+ startList.First().Value);
                    DateTime dateTime = DateTime.Parse(startList.First().Value);
                    return GetBhadimCityOccupancyList(userId, dateTime, eventList);

                }
                catch (Exception ex)
                {
                    Trace.WriteLine("IN CATCH PARSE:" + startList.First().Value);
                    Trace.WriteLine("Error in service scheduleEvent ex:" + ex.Message);
                    throw;
                }
            }
            return new List<ScheduleOccupancyItem>();
        }

        private List<ScheduleOccupancyItem> GetBhadimCityOccupancyList(string userId, DateTime startDate, List<EventItem> eventList)
        {
            //IdValuePair commemorationDivision = new IdValuePair() { Id = "2", Value = "הנצחה" };
            var schoolDivision = new Department { Id = "1", Number = 1, Name = "בית ספר" };
            //var commemorationDivision = new Department { Id = "2", Number = 2, Name = "הנצחה" };
            List<ScheduleOccupancyItem> scheduleOccupancyItems = new List<ScheduleOccupancyItem>();
            DateTime startTime = GetBhadimCityOpeningHour(userId, startDate);
            int maxCapability = resourceService.GetBhadimCityResource(userId).Capacity;
            DateTime endTime = GetBhadimCityClosingHour(userId, startDate);
            var division = schoolDivision;
            int index = 0;

            //List<ScheduleDate> scheduleDates = scheduleDateService.GetMuseumClosedTimesByDate(userId, startTime.ToString());

            for (DateTime date = startTime; date <= endTime; date = date.AddMinutes(queryHelperRepository.IR_BHADMIN_SLOT_MINUTE))
            {
                //foreach (ScheduleDate item in scheduleDates)
                //{
                //    if (date == DateTime.Parse(item.Start))
                //    {
                //        date = DateTime.Parse(item.End);
                //        continue;
                //    }
                //}
                int numOfVisitors = eventList.Where(s => DateTime.ParseExact(s.Start, "MM/dd/yyyy HH:mm:ss", null) == date).Sum(a => a.NumberOfVisitors);

                ScheduleOccupancyItem scheduleOccupancyItem = new ScheduleOccupancyItem()
                {
                    Start = date,
                    Id = index++,
                    End = date.AddMinutes(queryHelperRepository.IR_BHADMIN_SLOT_MINUTE),
                    Title = division.Name,
                    Division = division,
                    MaxCapability = maxCapability,
                    NumberOfVisitors = numOfVisitors,
                    IsAllDay = false,
                    IsReadOnly = true
                };
                scheduleOccupancyItems.Add(scheduleOccupancyItem);
            }
            return scheduleOccupancyItems;
        }

        private DateTime GetMuseumOpeningHour(string userId, DateTime date)
        {
            TimeSpan ts = new TimeSpan();
            var museum = resourceService.GetMuseumResource(userId);
            var day = (int)date.DayOfWeek;
            ts = new TimeSpan(museum.OpeningTimes[day].Hours, museum.OpeningTimes[day].Minutes, 0);
            return date.Date.Add(ts);
        }

        private DateTime GetBhadimCityOpeningHour(string userId, DateTime date)
        {
            TimeSpan ts = new TimeSpan();
            var bhadimCity = resourceService.GetBhadimCityResource(userId);
            var day = (int)date.DayOfWeek;
            ts = new TimeSpan(bhadimCity.OpeningTimes[day].Hours, bhadimCity.OpeningTimes[day].Minutes, 0);
            return date.Date.Add(ts);
        }

        private DateTime GetMuseumClosingHour(string userId, DateTime date)
        {
            TimeSpan ts = new TimeSpan(); 
            var museum = resourceService.GetMuseumResource(userId);
            var day = (int)date.DayOfWeek;
            ts = new TimeSpan(museum.ClosingTimes[day].Hours, museum.ClosingTimes[day].Minutes, 0);
            return date.Date.Add(ts);

        }

        private DateTime GetBhadimCityClosingHour(string userId, DateTime date)
        {
            TimeSpan ts = new TimeSpan();
            var bhadimCity = resourceService.GetBhadimCityResource(userId);
            var day = (int)date.DayOfWeek;
            ts = new TimeSpan(bhadimCity.ClosingTimes[day].Hours, bhadimCity.ClosingTimes[day].Minutes, 0);
            return date.Date.Add(ts);

        }
    }
}
