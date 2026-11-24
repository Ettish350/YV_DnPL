using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class CertificationCategoryService : ICertificationCategoryService
    {
        private readonly ICertificationCategoryRepository certificationCategoryRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;

        public CertificationCategoryService(
            ICertificationCategoryRepository certificationCategoryRepository,
            IMapFunctionRepository mapFunctionRepository)
        {
            this.certificationCategoryRepository = certificationCategoryRepository;
            this.mapFunctionRepository = mapFunctionRepository;
        }


        public List<CertificationCategory> GetAll(string userId)
        {
            try
            {
                List<Filter> filterList = new List<Filter>();

                List<Sort> sorts = new List<Sort> {
                //    new Sort {
                //    Active = mapFunctionRepository.GetDestinationPropertyFor<CertificationCategory, CertificationCategoryCRM>("DisplaySortOrder"),
                //    Direction = "asc"
                //}, 
                new Sort {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<CertificationCategory, CertificationCategoryCRM>("Name"),
                    Direction = "asc"
                }};

                return certificationCategoryRepository.GetFetchXml(userId, false, null, sorts, filterList).Item2
                .OrderBy(x => x.DisplaySortOrder == 0 ? int.MaxValue : x.DisplaySortOrder).ToList();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



        private List<CertificationCategory> InternalServerError(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
