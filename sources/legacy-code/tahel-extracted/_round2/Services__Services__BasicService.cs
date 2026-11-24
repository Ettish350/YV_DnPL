using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class BasicService<TItem, TList, TCrm> : IBasicService<TItem, TList, TCrm>
    {
        protected readonly IHelperService helperService;
        protected readonly IBasicRepository<TItem, TList> basicRepository;
        IMapFunctionRepository mapFunctionRepository;
        public BasicService(IHelperService helperService, 
            IBasicRepository<TItem, TList> basicRepository,
            IMapFunctionRepository mapFunctionRepository)
        {
            this.helperService = helperService;
            this.basicRepository = basicRepository;
            this.mapFunctionRepository = mapFunctionRepository;
        }
        public virtual Guid Create(TItem item, string userId)
        {
            var guid = basicRepository.Create(item, userId);
            return guid;
        }
        public virtual void Update(TItem item, TItem oldItem, string userId)
        {
            basicRepository.Update(item, oldItem, userId);
        }

        public virtual void UpdateField(string id, UpdateCrmItem item, string userId)
        {
            item.Field = mapFunctionRepository.GetDestinationPropertyFor<TItem, TCrm>(item.Field);
            if(item.Type == "lookup")
            {
                if (!string.IsNullOrEmpty(item.EntityReferenceType))
                {
                    item.EntityReferenceType = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(item.EntityReferenceType);
                }
                else
                {
                    throw new Exception("entityReferenceType can not be null");
                }
            }
            
            basicRepository.UpdateField(id, item, userId);
        }

        public virtual void UpdateFields(string id, List<UpdateCrmItem> items, string userId)
        {
            items.ForEach(item =>
            {
                item.Field = mapFunctionRepository.GetDestinationPropertyFor<TItem, TCrm>(item.Field);
                if (item.Type == "lookup")
                {
                    if (!string.IsNullOrEmpty(item.EntityReferenceType))
                    {
                        item.EntityReferenceType = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(item.EntityReferenceType);
                    }
                    else
                    {
                        throw new Exception("entityReferenceType can not be null");
                    }
                }
                basicRepository.UpdateField(id, item, userId);
            });
        }

        public virtual void Cancel(TItem item, string userId, bool isCancel = true)
        {
            basicRepository.Cancel(item, userId, isCancel);
        }
        
        /*public virtual List<TList> Get(string userId, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            sort = helperService.BuildSorting<TList, TCrm>(sort);
            filter = helperService.BuildFilter<TList, TCrm, TCrm>(filter);
            return basicRepository.Get(userId, paging, sort, filter);
        }*/

        public virtual Tuple<int, List<TList>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            //string sortXml = helperService.BuildSortingFetchXml<TList, TCrm>(sort);
            //filter = helperService.BuildFilter<TList, TCrm, TCrm>(filter);
            sort = helperService.BuildSorting<TList, TCrm>(sort);
            filter = helperService.BuildFilter<TList, TCrm, TCrm>(filter);
            return basicRepository.GetFetchXml(userId, false, paging, sort, filter);
        }

        




        /*public virtual List<TList> Get(string userId, Paging paging, List<Sort> sort, List<Filter> filter, List<Filter> additionalFilter)
        {
            sort = helperService.BuildSorting<TList, TCrm>(sort);
            filter = helperService.BuildFilter<TList, TCrm, TCrm>(filter);
            additionalFilter = helperService.BuildFilter<TList, TCrm, TCrm>(additionalFilter);
            return basicRepository.Get(userId, paging, sort, filter, additionalFilter);
        }*/

       
        public virtual Tuple<int, List<TList>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter, string type = "", bool getAllItems = false)
        {
            sort = helperService.BuildSorting<TList, TCrm>(sort);
            filter = helperService.BuildFilter<TList, TCrm, TCrm>(filter);
            return basicRepository.GetFetchXml(userId, false, paging, sort, filter);
        }


        public virtual decimal Count(string userId, List<Filter> filter)
        {
            filter = helperService.BuildFilter<TList, TCrm, TCrm>(filter);
            return basicRepository.Count(userId, filter);
        }

        /*public virtual TItem Get(string userId, string guid)
        {
            return basicRepository.Get(userId, guid);
        }*/

        public virtual TItem GetFetchXml(string userId, string guid)
        {
            return basicRepository.GetFetchXml(userId, guid);
        }
        public virtual TItem GetFetchXml(string userId, string guid, List<Filter> filters)
        {
            return basicRepository.GetFetchXml(userId, guid, filters);
        }


        public virtual void Delete(TItem item, string userId)
        {
            basicRepository.Delete(item, userId);
        }

        public virtual List<TList> GetFetchXmlTake1(string userId, string fetchXml)
        {
            return basicRepository.GetFetchXmlTake1(userId, fetchXml);
        }

        /*public virtual decimal CountFetchXml(string userId, string fetchXml)
        {
            return basicRepository.CountFetchXml(userId, fetchXml);
        }*/

        public virtual List<TList> GetGot(string userId, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            sort = helperService.BuildSorting<TList, TCrm>(sort);
            filter = helperService.BuildFilter<TList, TCrm, TCrm>(filter);
            return basicRepository.GetFetchXml(userId, false, paging, sort, filter).Item2;
        }

    }
}
