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
    public class BudgetFrameworkService : IBudgetFrameworkService
    {

        private readonly IBudgetFrameworkRepository budgetFrameworkRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IDepartmentRepository departmentRepository;

        public BudgetFrameworkService(
            IBudgetFrameworkRepository budgetFrameworkRepository,
            IMapFunctionRepository mapFunctionRepository,
            IDepartmentRepository departmentRepository)
        {
            this.budgetFrameworkRepository = budgetFrameworkRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.departmentRepository = departmentRepository;
        }

       

        public List<BudgetFramework> GetAll(string userId, string department, string divisionRouter)
        {
            //string department = get department  from departments where businessUnit

            // IdValuePair department = LoginRepository.userInfo.Department;
            try
            {
                List<Filter> filterList = new List<Filter>();

                if (department != "" && department != null)
                {
                    filterList.Add(new Filter
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<BudgetFramework, BudgetFrameworkCRM>("Department"),
                        Value = department,
                        Type = "lookup"
                    });
                }
                if (divisionRouter != "" && divisionRouter != null)
                {
                    filterList.Add(new Filter
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<BudgetFramework, BudgetFrameworkCRM>("DivisionRouter"),
                        Value = divisionRouter,
                        Type = "optionset"
                    });
                }
                var sort = new Sort
                {
                    Active = mapFunctionRepository.GetDestinationPropertyFor<BudgetFramework, BudgetFrameworkCRM>("Name"),
                    Direction = "asc"
                };

                return budgetFrameworkRepository.GetFetchXml(userId, false, null, new List<Sort> { sort }, filterList).Item2;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        

        private List<BudgetFramework> InternalServerError(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
