using Data.Modals;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class HotelRateByMonthService : IHotelRateByMonthService
    {
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IHelperService helperService;
        private readonly IHotelRateByMonthRepository hotelRateByMonthRepository;

        public HotelRateByMonthService(
            IMapFunctionRepository mapFunctionRepository,
            IHelperService helperService,
            IHotelRateByMonthRepository hotelRateByMonthRepository)
        {
            this.mapFunctionRepository = mapFunctionRepository;
            this.helperService = helperService;
            this.hotelRateByMonthRepository = hotelRateByMonthRepository;
        }

        public List<HotelRateByMonth> GetHotelRateByMonth(string userId)
        {
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                 <entity name = 'new_hotel_by_month'>
                                   <attribute name = 'new_hotel_by_monthid'/>
                                   <attribute name = 'new_hotel'/>
                                   <attribute name = 'new_billing_rate'/>
                                   <attribute name = 'new_january'/>
                                   <attribute name = 'new_february'/>
                                   <attribute name = 'new_march'/>
                                   <attribute name = 'new_april'/>
                                   <attribute name = 'new_may'/>
                                   <attribute name = 'new_june'/>
                                   <attribute name = 'new_july'/>
                                   <attribute name = 'new_august'/>
                                   <attribute name = 'new_september'/>
                                   <attribute name = 'new_october'/>
                                   <attribute name = 'new_november'/>
                                   <attribute name = 'new_december'/>
                                   <filter type='and'>
                                       <condition attribute = 'statecode' operator= 'eq' value = '0'/>
                                   </filter>
                                 </entity>
                               </fetch>";
            return hotelRateByMonthRepository.GetFetchXmlOnly(userId, fetchXml).Item2;
        }

    }
}

