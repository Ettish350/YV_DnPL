using Data.Modals;
using Data.Repositories.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class HelperService: IHelperService
    {
        private readonly IMapFunctionRepository mapService;
        private readonly IQueryHelperRepository queryHelperRepository;

        public HelperService(
            IMapFunctionRepository mapService,
            IQueryHelperRepository queryHelperRepositor)
        {
            this.mapService = mapService;
            this.queryHelperRepository = queryHelperRepositor;
        }
        public List<Sort> BuildSorting<TSrc, TDest>(List<Sort> sort)
        {
            if (sort != null)
            {
                foreach (var item in sort)
                {
                    item.Active = mapService.GetDestinationPropertyFor<TSrc, TDest>(queryHelperRepository.FirstCharToUpper(item.Active));
                }
            }
            else
                sort = new List<Sort>();
            return sort;
        }


        public List<Filter> BuildFilter<TSrc, TDest, LinkEntityDest>(List<Filter> filter)
        {
            if (filter != null)
            {
                foreach (Filter item in filter)
                {
                    if (item.Type != "global")
                    {
                        item.Field = mapService.GetDestinationPropertyFor<TSrc, TDest>(queryHelperRepository.FirstCharToUpper(item.Field));
                       
                    }
                }
            }
            else
                filter = new List<Filter>();
            return filter;
        }




        public string ObjConvertToJson(object value)
        {
            //Trace.WriteLine($"{DateTime.Now}: start ObjConvertToJson, value: {value.ToString()} ");
            try
            {
                string json = JsonConvert.SerializeObject(
                value,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                // Trace.WriteLine($"{DateTime.Now}: ObjConvertToJson, before return json: {json}");

                return json;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{DateTime.Now}: failed ObjConvertToJson, exception: {ex.Message}");
                throw ex;
            }
        }


        public T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public DateTime ParseDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return DateTime.MinValue;

            DateTime parsed;

            // dd/MM/yyyy + שעות
            if (DateTime.TryParseExact(
                input,
                new[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
            {
                return parsed;
            }

            // fallback — CRM לפעמים מחזיר MM/dd/yyyy
            if (DateTime.TryParse(input, out parsed))
                return parsed;

            return DateTime.MinValue;
        }


    }
}
