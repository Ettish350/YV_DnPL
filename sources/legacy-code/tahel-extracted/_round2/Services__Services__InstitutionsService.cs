using Data.Modals;
using Data.Modals.CRMModals;
using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Services.Services
{
    public class InstitutionsService : BasicService<InstitutionItem, Institution, InstitutionCRM>, IInstitutionsService
    {
        private IInstitutionsRepository institutionsRepository;
        private ILoginService loginService;
        private IReservationsService reservationsService;
        private IMapFunctionRepository mapFunctionRepository;

        public InstitutionsService(
            IInstitutionsRepository institutionsRepository,
            ILoginService loginService,
            IReservationsService reservationsService,
            IMapFunctionRepository mapFunctionRepository,
            IHelperService helperService) : base(helperService, institutionsRepository, mapFunctionRepository)
        {
            this.institutionsRepository = institutionsRepository;
            this.loginService = loginService;
            this.reservationsService = reservationsService;
            this.mapFunctionRepository = mapFunctionRepository;
        }
        /*public override List<Institution> Get(string userId, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            sort = helperService.BuildSorting<Institution, InstitutionCRM>(sort);
            filter = helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filter);
            SetAlmogCodeFilter(userId, filter);
            return institutionsRepository.GetFetchXml(userId, false, paging, sort, filter).Item2;
        }*/

        public override decimal Count(string userId, List<Filter> filter)
        {
            filter = helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filter);
            SetAlmogCodeFilter(userId, filter);
            return institutionsRepository.Count(userId, filter);
        }


        public override Tuple<int, List<Institution>> GetFetchXml(string userId, bool isAggregate, Paging paging, List<Sort> sort, List<Filter> filter)
        {
            string fetchXml = "";
            List<Sort> institutionSort = new List<Sort>();

            if (sort != null && !isAggregate)
            {
                sort.ForEach(x =>
                {
                    if (x.Active.Contains(":"))
                    {
                        x.Alias = x.Active.Split(':')[1];
                        x.Active = x.Active.Split(':')[0];
                    }
                });

                institutionSort = helperService.BuildSorting<Institution, InstitutionCRM>(sort.FindAll(x => x.Alias == "" || x.Alias == null || x.Alias == "Institution"));
            }

            List<Filter> institutionFilters = new List<Filter>();
            if (filter != null)
            {
                List<Filter> almogCodeNotEmpty = filter.FindAll(x => x.Field == "AlmogCode" && x.Operator == "not-empty");
                //almogCodeNotEmpty = helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(almogCodeNotEmpty.Li);
                if (almogCodeNotEmpty.Count > 0) {
                    fetchXml += "<filter type='or'>";
                        fetchXml += "<filter type='and'>";
                        fetchXml += "<condition attribute='new_almog_code' operator='not-null'/>";
                        fetchXml += "<condition attribute='new_is_not_with_code' operator='eq' value='false'/>";
                        fetchXml += "</filter>";
                        //fetchXml += "<filter type='and'>";
                        //fetchXml += "<condition attribute='new_almog_code' operator='not-null'/>";
                        fetchXml += "<condition attribute='new_is_not_with_code' operator='eq' value='true'/>";
                        //fetchXml += "</filter>";
                    fetchXml += "</filter>";
                }

                institutionFilters = helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filter.FindAll(x => (x.Alias == "" || x.Alias == null || x.Alias == "Institution" ) && x.Field != "AlmogCode" && x.Operator != "not-empty"));
                //additionalFilters.AddRange(helperService.BuildFilter<BudgetFramework, BudgetFrameworkCRM, BudgetFrameworkCRM>(filter.FindAll(x => x.Alias == "BudgetFramework")));
            }

            return institutionsRepository.GetFetchXml(userId, false, paging, institutionSort, institutionFilters, fetchXml);

        }


        private void SetAlmogCodeFilter(string userId, List<Filter> filter)
        {
            var user = loginService.GetUserInfo();
            if (user.BusinessUnit.Value == "בית ספר" || user.ParentBusinessUnit.Value == "בית ספר")
                filter.Add(new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<InstitutionItem, InstitutionItemCRM>("AlmogCode"),
                    Value = "empty",
                    Type = "string",
                    Operator = "not-empty"
                });

        }

        public InstitutionForReservation GetInstitutionForReservation(string userId, string guid)
        {
            return institutionsRepository.GetInstitutionForReservation(userId, guid);
        }
        public InstitutionForReservation GetInstitutionForReservationByAlmogData(string userId, string licensNumber, string Symbol, string almogCode,bool isRelatedAccount= false)
        {
            List<Filter> filterList = new List<Filter>();
            string guid = string.Empty;
            List<Institution> result;

            //אם זה חשבון קשור- תחפש רק לפי קוד אלמוג
            if (isRelatedAccount)
            {
                filterList.Add(new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<InstitutionItem, InstitutionItemCRM>("AlmogCode"),
                    Value = almogCode,
                    Type = "string"
                });
                result = institutionsRepository.GetFetchXml(userId, false, filterList).Item2;
            }
            else
            {
                // almogCode+licensNumber מחפש מוסד לפי
                filterList.Add(new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<InstitutionItem, InstitutionItemCRM>("AlmogCode"),
                    Value = almogCode,
                    Type = "string"
                });
                filterList.Add(new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<InstitutionItem, InstitutionItemCRM>("CompanyNumber"),
                    Value = licensNumber,
                    Type = "string"

                });
                result = institutionsRepository.GetFetchXml(userId, false, filterList).Item2;
                if (result.Count == 0)  // almogCode+Symbol מחפש מוסד לפי
                {

                    filterList.Clear();
                    filterList.Add(new Filter
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<InstitutionItem, InstitutionItemCRM>("AlmogCode"),
                        Value = almogCode,
                        Type = "string"
                    });
                    filterList.Add(new Filter
                    {
                        Field = mapFunctionRepository.GetDestinationPropertyFor<InstitutionItem, InstitutionItemCRM>("InstitutionSymbol"),
                        Value = Symbol,
                        Type = "string"
                    });
                    result = institutionsRepository.GetFetchXml(userId, false, filterList).Item2;
                }
            }
            if (result.Count == 1)// חזר מוסד אחד- זה מעולה
            {
                guid = result[0].Id;
                return institutionsRepository.GetInstitutionForReservation(userId, guid);
            }
            if (result.Count > 1 && !isRelatedAccount)//חזר יותר ממוסד אחד , תיקח את המוסד שהוא מוסד משלם
            {
                foreach (var item in result)
                {
                    if (item.IsPayingInstitution)
                    {
                        guid = result[0].Id;
                        return institutionsRepository.GetInstitutionForReservation(userId, guid);
                    }
                }

            }
           
                Trace.WriteLine(" error getting institution for this data:");
                Trace.WriteLine("licensNumber: " + licensNumber + " Symbol: " + Symbol + " almogCode: " + almogCode);
                throw new System.Exception("more or less than one institution returned for the data you sent");
            



        }
        

        public InstitutionForPayingInstitution GetInstitutionForPayingInstitution(string userId, string guid)
        {
            return institutionsRepository.GetInstitutionForPayingInstitution(userId, guid);
        }

        
        public List<IdValuePair> GetList(string userId, Paging paging, List<Filter> filters)
        {
            string fetchXml = "";

            if (filters != null)
            {                
                List<Filter> almogCodeNotEmpty = filters.FindAll(x => x.Field == "AlmogCode" && x.Operator == "not-empty");
                if (almogCodeNotEmpty.Count > 0)
                {
                    fetchXml += "<filter type='or'>";
                    fetchXml += "<filter type='and'>";
                    fetchXml += "<condition attribute='new_almog_code' operator='not-null'/>";
                    fetchXml += "<condition attribute='new_is_not_with_code' operator='eq' value='false'/>";
                    fetchXml += "</filter>";
                    fetchXml += "<condition attribute='new_is_not_with_code' operator='eq' value='true'/>";
                    fetchXml += "</filter>";
                }

                filters = filters.FindAll(x => x.Field != "AlmogCode" && x.Operator != "not-empty");
                //filters = helperService.BuildFilter<Institution, InstitutionCRM, InstitutionCRM>(filters.FindAll(x => (x.Alias == "" || x.Alias == null || x.Alias == "Institution") && x.Field != "AlmogCode" && x.Operator != "not-empty"));
            }

            return institutionsRepository.GetList(paging, filters, userId, fetchXml);

        }
        
    }
}
