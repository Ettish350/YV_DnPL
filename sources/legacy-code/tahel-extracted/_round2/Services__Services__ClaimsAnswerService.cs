using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Services.Services
{
    public class ClaimsAnswerService : IClaimsAnswerService
    {

        private readonly IClaimsAnswerRepository claimsAnswerRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;

        public ClaimsAnswerService(
            IClaimsAnswerRepository claimsAnswerRepository,
            IMapFunctionRepository mapFunctionRepository)
        {
            this.claimsAnswerRepository = claimsAnswerRepository;
            this.mapFunctionRepository = mapFunctionRepository;
        }

        
        public List<ClaimsAnswer> GetAll(string userId)
        {            
            try
            {
                List<Filter> filterList = new List<Filter>();

                var sort = new Sort
                {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<ClaimsAnswer, ClaimsAnswerCRM>("Answer"),
                    Direction = "asc"
                };

                return claimsAnswerRepository.GetFetchXml(userId, false, null, new List<Sort> { sort }, filterList).Item2;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }



        private List<ClaimsAnswer> InternalServerError(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
