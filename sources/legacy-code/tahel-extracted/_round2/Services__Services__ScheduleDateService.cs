using Data.Repositories.Interfaces;
using Data.Modals;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Modals.CRMModals;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Configuration;
using DocumentFormat.OpenXml.Office2010.ExcelAc;

namespace Services.Services
{
    public class ScheduleDateService : BasicService<ScheduleDate, ScheduleDate, ScheduleDateCRM>, IScheduleDateService
    {
       
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IScheduleDateRepository scheduleDateRepository;

        private readonly IQueryHelperRepository queryHelperRepository;
        private string environment;

        public Objects Objects { get; } = new Objects();

        public ScheduleDateService(IHelperService helperService,
                                  IMapFunctionRepository mapFunctionRepository,
                                  IScheduleDateRepository scheduleDateRepository,
                                  IQueryHelperRepository queryHelperRepository
            ) : base(helperService, scheduleDateRepository, mapFunctionRepository)
        {
            
            this.mapFunctionRepository = mapFunctionRepository;
            this.scheduleDateRepository = scheduleDateRepository;
            this.queryHelperRepository = queryHelperRepository;
            environment = ConfigurationManager.AppSettings["environment"];
        }


        public override Guid Create(ScheduleDate item, string userId)
        {
            Guid guid = new Guid();
            DateTime startTemp = DateTime.ParseExact(item.Start, "dd/MM/yyyy HH:mm:ss", null);
            DateTime endTemp = DateTime.ParseExact(item.End, "dd/MM/yyyy HH:mm:ss", null);

            if (startTemp.Date != endTemp.Date)
            { //more than one closing date
                for (DateTime date = startTemp.Date; date.Date <= endTemp.Date; date = date.AddDays(1))
                {
                    ScheduleDate itemTemp = new ScheduleDate();
                    itemTemp.Title = item.Title;
                    
                    itemTemp.ApprovedBy = item.ApprovedBy;
                    itemTemp.IsClosedAllDay = item.IsClosedAllDay;
                    itemTemp.ClosingType = item.ClosingType;
                    

                    if (!item.IsClosedAllDay)
                    {
                        itemTemp.Start = date.ToString("dd/MM/yyyy " + startTemp.ToString("HH:mm:ss"));
                        itemTemp.End = date.ToString("dd/MM/yyyy " + endTemp.ToString("HH:mm:ss"));
                    }
                    else {
                        itemTemp.Start = date.ToString("dd/MM/yyyy 00:00:00");
                        itemTemp.End = date.ToString("dd/MM/yyyy 00:00:00");
                    }

                    guid = scheduleDateRepository.Create(itemTemp, userId);

                    if (itemTemp.ClosingType.Id == ((int)ClosingType.Activities_Closed).ToString() || itemTemp.ClosingType.Id == ((int)ClosingType.Activities_Open).ToString())
                    {
                        itemTemp.ActivitiesList = item.ActivitiesList;
                        itemTemp.LanguagesList = item.LanguagesList;
                        itemTemp.ProductsList = item.ProductsList;
                        itemTemp.Id = guid.ToString();
                        CreateOrUpdateScheduleDateLists(itemTemp, userId, true);
                    }
                }
            }
            else {
                guid = scheduleDateRepository.Create(item, userId);

                if (item.ClosingType.Id == ((int)ClosingType.Activities_Closed).ToString() || item.ClosingType.Id == ((int)ClosingType.Activities_Open).ToString())
                {
                    item.Id = guid.ToString();
                    CreateOrUpdateScheduleDateLists(item, userId, true);
                }
            }
            
            return guid;
        }
        public override void Update(ScheduleDate item, ScheduleDate oldItem, string userId)
        {
            scheduleDateRepository.Update(item, oldItem,  userId);

            if (item.ClosingType.Id == ((int)ClosingType.Activities_Closed).ToString() || item.ClosingType.Id == ((int)ClosingType.Activities_Open).ToString())
            {
                CreateOrUpdateScheduleDateLists(item, userId);
            }
        }

        public List<ScheduleDate> GetMuseumClosedTimesByDate(string userId, string date)
        {
            var filterList = new List<Filter>();

            Filter filter = new Filter
            {
                Field = "new_from_closing_date",
                Operator = "eq-date",
                Type = "date",
                Value = date
            };

            filterList.Add(filter);
            var scheduleDateListTemp = scheduleDateRepository.GetFetchXml(userId, false, filterList).Item2;

            List<ScheduleDate> scheduleDateList = new List<ScheduleDate>();
            foreach (var scheduleDate in scheduleDateListTemp)
            {   //DateTime.Parse(scheduleDate.Start).Date == DateTime.Parse(date).Date && 
                if (scheduleDate.ClosingType.Id != ((int)ClosingType.Activities_Closed).ToString() && scheduleDate.ClosingType.Id != ((int)ClosingType.Activities_Open).ToString())
                {
                    scheduleDateList.Add(scheduleDate);
                }
            }

            return scheduleDateList;
        }

        public List<ScheduleDate> GetClosedDatesAndTimesByDates(string userId, string startDate, string endDate, List<ClosingType> closingType)
        {
            var filterList = new List<Filter>();

            filterList.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<ScheduleDate, ScheduleDateCRM>("Start"),
                Operator = "between-dates",
                Type = "date",
                Value = startDate + "," + endDate
            });

            filterList.Add(new Filter
            {
                Field= mapFunctionRepository.GetDestinationPropertyFor<ScheduleDate, ScheduleDateCRM>("ClosingType"),
                Operator = "multiple",
                Type = "optionset",
                Value = string.Join(";", closingType.Select(type => ((int)type).ToString()))
            });

            var scheduleDateListTemp = scheduleDateRepository.GetFetchXml(userId, false, filterList).Item2;

            List<ScheduleDate> scheduleDateList = new List<ScheduleDate>();
            foreach (var scheduleDate in scheduleDateListTemp)
            {   //DateTime.Parse(scheduleDate.Start).Date == DateTime.Parse(date).Date && 
                if (scheduleDate.ClosingType.Id != ((int)ClosingType.Activities_Closed).ToString() && scheduleDate.ClosingType.Id != ((int)ClosingType.Activities_Open).ToString())
                {
                    scheduleDateList.Add(scheduleDate);
                }
            }

            return scheduleDateList;
        }

        private void CreateOrUpdateScheduleDateLists(ScheduleDate item, string userId, bool isCreate = false)
        {
            SetIdValueList("ActivitiesList", Objects.ActivityDayType, item.Id, item.ActivitiesList, userId, isCreate);
            SetIdValueList("LanguagesList", Objects.Language, item.Id, item.LanguagesList, userId, isCreate);
            SetIdValueList("ProductsList", Objects.Product, item.Id, item.ProductsList, userId, isCreate);
        }

        private void SetIdValueList(string listName, string obj, string guid, List<IdValuePair> itemList, string userId, bool isCreate)
        {
            List<IdValuePair> oldItemList = new List<IdValuePair>();
            Guid.TryParse(guid, out Guid refScheduleDateGuid);

            if (!isCreate) {
                oldItemList = scheduleDateRepository.GetIdValueList(userId, listName, obj, refScheduleDateGuid);
                //disassociate from list if isnt in new list 
                foreach (var item in oldItemList)
                {
                    Guid.TryParse(item.Id, out Guid refItemListGuid);

                    if (itemList.Where(x => x.Id == item.Id).Count() == 0)
                    {
                        //disassociate
                        EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj), refItemListGuid);
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ScheduleDate"), refScheduleDateGuid);
                        basicRepository.Disassociate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<ScheduleDate, ScheduleDateCRM>(listName), userId);
                    }
                }
            }

            foreach (var item in itemList)
            {
                Guid.TryParse(item.Id, out Guid refItemListGuid);
                
                if(oldItemList.Where(x => x.Id == item.Id).Count() == 0)
                {//is not in old list 
                    if (refItemListGuid != null && refScheduleDateGuid != null)
                    {
                        EntityReference refa = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(obj), refItemListGuid);
                        EntityReference refb = new EntityReference(mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>("ScheduleDate"), refScheduleDateGuid);
                        basicRepository.Associate(refa, refb, mapFunctionRepository.GetDestinationPropertyFor<ScheduleDate, ScheduleDateCRM>(listName), userId);
                    }
                }
            }

        }
    }



}
