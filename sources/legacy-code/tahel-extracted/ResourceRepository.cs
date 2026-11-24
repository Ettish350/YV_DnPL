using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repositories
{
    public class ResourceRepository : BasicRepository<Resource, Resource>, IResourceRepository
    {
        private IMapFunctionRepository mapFunctionRepository;
        //private IEventItemRepository eventItemRepository;
        public override string EntityName { get; set; }
        public override string EntityId { get; set; }
        public ResourceRepository(IQueryHelperRepository queryHelperService,
                                         ICRMConnection crmConnection,
                                         //IEventItemRepository eventItemRepository,
                                         IMapFunctionRepository mapFunctionRepository
                                         ) : base(queryHelperService, crmConnection)
        {
            this.mapFunctionRepository = mapFunctionRepository;
            //this.eventItemRepository = eventItemRepository;
            EntityName = mapFunctionRepository.GetDestinationPropertyFor<Objects, ObjectsCRM>(Objects.Resource);
            EntityId = mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Id");
        }
        protected override IdValuePair SetEntityToIdValueList(string userId, Entity entity)
        {
            throw new NotImplementedException();
        }

        public Resource GetMuseumResource(string userId, string guid)
        {
            try
            {
                Filter f = new Filter
                {
                    Type = "lookup",
                    Value = guid,
                    Field = EntityId
                };

                var basicQuery = crmConnection.CreateQuery(EntityName, userId);
                basicQuery = queryHelperRepository.SingleFilterBy(basicQuery, f);

                var query = basicQuery.Select(x => SetEntityToItemMuseum(x, guid));

                var itemToReturn = query.FirstOrDefault();
                return itemToReturn;
            }
            catch (Exception ex)
            {
                //Trace.TraceError($"{DateTime.Now}: Get Failed to retrtieve specific properties {ex.Message} - {guid}");
                throw;
            }


        }

        public Resource SetEntityToItemMuseum(Entity entity, string guid)
        {
            var item = new Resource
            {
                Id = guid,
                Value = queryHelperRepository.GetValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Value")),
                Capacity = queryHelperRepository.GetIntValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Capacity")),
                Status = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "Status"),
                Arrangement = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "Arrangement").Value,
                CanSplit = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "CanSplit").Value,
                IsConsolidable = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "IsConsolidable").Value,
                IsForFrontDesk = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsForFrontDesk")),
                IsBusy = false,
                IsNeedConfirmed = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsNeedConfirmed")),
                IsCanHaveRefreshments = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsCanHaveRefreshments")),
                IsModifyArrangementAvailable = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsModifyArrangementAvailable")),
                Location = queryHelperRepository.GetIdValueForEntityReferenceByName<Resource, ResourceCRM>(entity, "Location"),
                HasMicrophone = queryHelperRepository.GetValueBoolByAttributeName<Resource, ResourceCRM>(entity, "HasMicrophone"),
                AirconditionerAfterSix = queryHelperRepository.GetValueBoolByAttributeName<Resource, ResourceCRM>(entity, "AirconditionerAfterSix"),
            };

            item.OpeningTimes = GetTimes(entity, "Open");
            item.ClosingTimes = GetTimes(entity, "Close");

            return item;

        }
        protected override Resource SetEntityToItem(string userId, Entity entity, string guid, List<Filter> additionalFilter = null)
        {
            return SetEntityToListItem(userId, entity);
        }
        protected override Resource SetEntityToListItem(string userId, Entity entity, List<Filter> additionalFilter = null)
        {
            List<Filter> filters = new List<Filter>();
            var rId = queryHelperRepository.GetValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Id"));
            if (additionalFilter != null)
            {
                filters = new List<Filter>(additionalFilter);
            }
            filters.Add(new Filter() { Field = "new_resource", Value = rId, Type = "lookup" });

            var item = new Resource
            {
                Id = rId,
                Value = queryHelperRepository.GetValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Value")),
                Capacity = queryHelperRepository.GetIntValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Capacity")),
                Status = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "Status"),
                Arrangement = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "Arrangement").Value,
                CanSplit = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "CanSplit").Value,
                IsConsolidable = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "IsConsolidable").Value,
                // IsBusy = eventItemRepository.GetCountEvent(filters, "new_resource", userId) > 0,
                IsNeedConfirmed = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsNeedConfirmed")),
                IsCanHaveRefreshments = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsCanHaveRefreshments")),
                IsModifyArrangementAvailable = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsModifyArrangementAvailable")),
                IsForFrontDesk = queryHelperRepository.GetBoolValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("IsForFrontDesk")),
                Location = queryHelperRepository.GetIdValueForEntityReferenceByName<Resource, ResourceCRM>(entity, "Location"),
                HasMicrophone = queryHelperRepository.GetValueBoolByAttributeName<Resource, ResourceCRM>(entity, "HasMicrophone"),
                AirconditionerAfterSix = queryHelperRepository.GetValueBoolByAttributeName<Resource, ResourceCRM>(entity, "AirconditionerAfterSix"),
            };

            item.OpeningTimes = GetTimes(entity, "Open");
            item.ClosingTimes = GetTimes(entity, "Close");

            return item;
        }

        public Resource SetEntityToListShortItem(Entity entity)
        {
            List<Filter> filters = new List<Filter>();
            var rId = queryHelperRepository.GetValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Id"));

            filters.Add(new Filter() { Field = "new_resource", Value = rId, Type = "lookup" });

            var item = new Resource
            {
                Id = rId,
                Value = queryHelperRepository.GetValueByAttribute(entity, mapFunctionRepository.GetDestinationPropertyFor<Resource, ResourceCRM>("Value")),
                OpeningTimes = GetTimes(entity, "Open"),
                ClosingTimes = GetTimes(entity, "Close"),
                Location = queryHelperRepository.GetIdValueForEntityReferenceByName<Resource, ResourceCRM>(entity, "Location"),
                IsNeedConfirmed = queryHelperRepository.GetValueBoolByAttributeName<Resource, ResourceCRM>(entity, "IsNeedConfirmed"),
                Floor = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "Floor"),
                Building = queryHelperRepository.GetIdValueForOptionSetByName<Resource, ResourceCRM>(entity, "Building")
            };


            return item;
        }




        public List<Resource> GetResourceList(string userId)
        {

            List<Resource> resourceList = new List<Resource>();

            try
            {
                var basicQuery = crmConnection.CreateQuery(EntityName, userId);
                // Get only Active record
                basicQuery = basicQuery.Where(a => a.GetAttributeValue<OptionSetValue>("statecode").Value.Equals(0));

                var query = basicQuery
                     .Select(x => SetEntityToListShortItem(x));

                resourceList = query.ToList();

            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: get list {EntityName} Failed. error: {ex.Message}");

                throw;
            }

            return resourceList;

        }

        protected override void SetItemToEntity(Resource item, Resource oldItem, Entity entity, string userId)
        {
            throw new NotImplementedException();
        }

        public override string GetLinkedEntity(bool isAggregate, List<Filter> filters, List<Sort> sort, string linkType, string additionalType)
        {
            if (linkType == "")
                linkType = "outer";
            string fetchXml = @"<link-entity name='new_resource' from='new_resourceid' to='new_resource' alias='Resource'  link-type='" + linkType + @"'>";
            fetchXml += @"<all-attributes/>";
            if (filters != null && filters.Count > 0)
            {
                fetchXml += FilterFetchXML(filters.FindAll(x => x.Alias == "Resource"));
            }

            fetchXml += @"</link-entity>";

            return fetchXml;
        }


        private Time[] GetTimes(Entity entity, string type)
        {
            Time[] timeArr = new Time[6];

            timeArr[0] = queryHelperRepository.GetValueTimeFromStringByAttributeName<TempResource, ResourceCRM>(entity, "Sunday" + type);
            timeArr[1] = queryHelperRepository.GetValueTimeFromStringByAttributeName<TempResource, ResourceCRM>(entity, "Monday" + type);
            timeArr[2] = queryHelperRepository.GetValueTimeFromStringByAttributeName<TempResource, ResourceCRM>(entity, "Tuesday" + type);
            timeArr[3] = queryHelperRepository.GetValueTimeFromStringByAttributeName<TempResource, ResourceCRM>(entity, "Wednesday" + type);
            timeArr[4] = queryHelperRepository.GetValueTimeFromStringByAttributeName<TempResource, ResourceCRM>(entity, "Thursday" + type);
            timeArr[5] = queryHelperRepository.GetValueTimeFromStringByAttributeName<TempResource, ResourceCRM>(entity, "Friday" + type);

            return timeArr;
        }
        public bool IsUnionResource(string resourceId, string userId)
        {
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='new_resource'>
                                    <attribute name='new_resourceid' />
                                    <attribute name='new_name' />
                                    <attribute name='createdon' />
                                    <order attribute='new_name' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='new_resourceid' operator='eq' uitype='new_resource' value='" + resourceId + @"' />
                                      <condition attribute='new_is_can_be_split' operator='eq' value='1' />
                                    </filter>
                                  </entity>
                                </fetch>";
            Tuple<int, List<Resource>> resource = GetFetchXmlOnly(userId, fetchXml);
            if (resource.Item2.Count > 0)
                return true;
            return false;
        }
    }

}
