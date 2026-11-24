using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Newtonsoft.Json;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Services.Services
{
    public class IdValueListService : IIdValueListService
    {
        IIdValueListRepository idValueListRepository;
        IMapFunctionRepository mapFunctionRepository;

        public IdValueListService(
            IIdValueListRepository idValueListRepository,
            IHelperService helperService,
            IMapFunctionRepository mapFunctionRepository)
        {
            this.idValueListRepository = idValueListRepository;
            this.mapFunctionRepository = mapFunctionRepository;
        }
        
        public List<IdValuePair> GetListByName(string listName)
        {
            var list = GetList(mapFunctionRepository.GetDestinationPropertyFor<Lists, ListsCRM>(listName));
            return list;
        }
        public List<IdValuePair> GetLookupListByName<TCrm>(string listName)
        {
            List<IdValuePair> list = GetLookupList<TCrm>(
                mapFunctionRepository.GetDestinationPropertyFor<Lists, ListsCRM>(listName), 
                mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Id"), 
                mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Value"));
           // list = list.OrderBy(x => x.Value);
            return list;
        }

        public List<IdValuePair> GetLookupListByNameAndSort<TCrm>(string listName, string sortField, string sortDir)
        {
            var list = GetLookupListWithSort<TCrm>(
                mapFunctionRepository.GetDestinationPropertyFor<Lists, ListsCRM>(listName),
                mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Id"),
                mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Value"),
                sortField,
                sortDir);

            return list;
        }

        public List<IdValuePair> GetLookupListWithFilterByNameAndSort<TCrm>(string listName, string filterField, string filterValue, string sortField, string sortDir)   
        {
            var list =
                GetLookupListWithFilterAndSort<TCrm>(
                    mapFunctionRepository.GetDestinationPropertyFor<Lists, ListsCRM>(listName),
                    mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Id"),
                    mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Value"),
                    filterField,
                    filterValue,
                    sortField,
                    sortDir);
            return list;
        }
        public List<IdValuePair> GetLookupListWithFilterByName<TCrm>(string listName, string filterField, string filterValue)
        {
            var list =
                GetLookupListWithFilter<TCrm>(
                    mapFunctionRepository.GetDestinationPropertyFor<Lists, ListsCRM>(listName),
                    mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Id"),
                    mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Value"),
                    filterField,
                    filterValue);
            return list;
        }
        public List<IdValuePair> GetLookupListWithFiltersByName<TCrm>(string listName, List<IdValuePair> filters)
        {
            var list =
                           GetLookupListWithFilters<TCrm>(
                               mapFunctionRepository.GetDestinationPropertyFor<Lists, ListsCRM>(listName),
                               mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Id"),
                               mapFunctionRepository.GetDestinationPropertyFor<IdValuePair, TCrm>("Value"),
                               filters);
            return list;
        }

        public int AddOptionsetValue(string picklistName, string label, string userId) {

            return idValueListRepository.AddOptionsetValue(picklistName, label, userId);
        }

        private List<IdValuePair> GetLookupList<Tsrc>(string listName, string key, string value)
        {
            return idValueListRepository.GetLookupList<Tsrc>(listName, key, value);
        }

        private List<IdValuePair> GetLookupListWithSort<Tsrc>(string listName, string key, string value, string sortField, string sortDir)
        {
            return idValueListRepository.GetLookupListWithSort<Tsrc>(listName, key, value, sortField, sortDir);
        }

        

        private List<IdValuePair> GetList(string listName)
        {
            return idValueListRepository.GetList(listName);
        }
        private List<IdValuePair> GetLookupListWithFilter<Tsrc>(string listName, string key, string value, string filterField, string filterValue)
        {
            return idValueListRepository.GetLookupListWithFilter<Tsrc>(listName, key, value, filterField, filterValue);
        }
        private List<IdValuePair> GetLookupListWithFilters<Tsrc>(string listName, string key, string value, List<IdValuePair> filters)
        {
            return idValueListRepository.GetLookupListWithFilters<Tsrc>(listName, key, value, filters);
        }

        private List<IdValuePair> GetLookupListWithFilterAndSort<Tsrc>(string listName, string key, string value, string filterField, string filterValue, string sortField, string sortDir)
        {
            return idValueListRepository.GetLookupListWithFilterAndSort<Tsrc>(listName, key, value, filterField, filterValue, sortField, sortDir);
        }

        public List<IdValuePair> GetLookupListWithFetchXml<Tsrc>(string listName, string fetchXml)
        {
            return idValueListRepository.GetLookupListWithFetchXml<Tsrc>(mapFunctionRepository.GetDestinationPropertyFor<Lists, ListsCRM>(listName), fetchXml);
        }


        public List<IdValuePair> GetGovCityList()
        {
            // deserialize JSON directly from a file
            string path = HttpRuntime.AppDomainAppPath + "/Files/gov_israel_cities.json";
            try
            {
                using (StreamReader file = File.OpenText(path))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    GovIsraelCity cities = (GovIsraelCity)serializer.Deserialize(file, typeof(GovIsraelCity));
                    return cities.Records.Select(cty => new IdValuePair { Id = cty[1].Integer.ToString(), Value = cty[2].String.Trim() }).ToList<IdValuePair>();
                }
            }
            catch (Exception ex)
            {
                var a = new List<IdValuePair>();
                a.Add(new IdValuePair("id", ex.Message));
                return a;
            }
        }
        public List<IdValuePair> GetConfigurationSettings(string key, string userId)
        {
            List<IdValuePair> list = new List<IdValuePair>();
            list = idValueListRepository.GetConfigurationSettings(key, userId);
            return list;
        }
    }
}
