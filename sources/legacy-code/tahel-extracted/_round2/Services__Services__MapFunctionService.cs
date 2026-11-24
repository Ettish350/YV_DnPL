using AutoMapper;
using Data.Modals;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class MapFunctionService : IMapFunctionService
    {
        IMapper mapper;
        public MapFunctionService(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public string GetDestinationPropertyFor<TSrc, TDst>(string sourceProperty)
        {
            string prop = "";
            try
            {
                var map = mapper.ConfigurationProvider.FindTypeMapFor<TSrc, TDst>();
                var propertyMap = map.GetPropertyMaps().First(pm => pm.SourceMember == typeof(TSrc).GetProperty(sourceProperty));
                prop = propertyMap.DestinationProperty.Name;
            }
            catch (Exception ex)
            {
                //Trace.TraceError($"{DateTime.Now}: GetDestinationPropertyFor Failed to get property name of {sourceProperty}, error: {ex.Message}");
                throw;
            }
            return prop;
        }
    }
}
