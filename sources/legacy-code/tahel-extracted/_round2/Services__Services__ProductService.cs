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
    public class ProductService : IProductService
    {

        private readonly IFullProductRepository fullProductRepository;
        private readonly IProductRepository productRepository;
        private readonly IMapFunctionRepository mapFunctionRepository;
        private readonly IHelperService helperService;

        public ProductService(
            IProductRepository productRepository,
            IFullProductRepository fullProductRepository,
            IMapFunctionRepository mapFunctionRepository,
            IHelperService helperService)
        {

            this.fullProductRepository = fullProductRepository;
            this.productRepository = productRepository;
            this.mapFunctionRepository = mapFunctionRepository;
            this.helperService = helperService;
        }

        public List<Product> GetGuideProducts(string userId, string divisionRouter)
        {
            var filterList = new List<Filter>();

            filterList.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>("ProductClassification"),
                Value = "1",//הדרכה
                Type = "optionset"
            });
            filterList.Add(new Filter
            {
                Field = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>("DivisionRouter"),
                Value = divisionRouter,
                Type = "optionset"
            });

            var sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>("Name"),
                Direction = "asc"
            };

            return fullProductRepository.GetFetchXml(userId, false, new List<Sort> { sort }, filterList).Item2;

        }


        public List<Product> GetAll(string userId, string divisionRouter, Filter filter = null, string additionalFetchXml = null)
        {
            if (filter != null)
            {
                filter.Field = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>(filter.Field);

            }

            if (divisionRouter != "" && divisionRouter != null)
            {

                filter = new Filter
                {
                    Field = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>("DivisionRouter"),
                    Value = divisionRouter,
                    Type = "optionset"
                };
            }

            var sort = new Sort
            {
                Active = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>("Name"),
                Direction = "asc"
            };

            return fullProductRepository.GetFetchXml(userId, false, filter, additionalFetchXml).Item2;
        }

    public Dictionary<string, List<Pricelist>> GetAllByFilter(string divisionRouter, string userId)
    {
        var filterList = new List<Filter>();

        filterList.Add(new Filter
        {
            Field = mapFunctionRepository.GetDestinationPropertyFor<Product, ProductCRM>("DivisionRouter"),
            Value = divisionRouter,
            Type = "optionset"
        });

        Dictionary<string, List<Pricelist>> productlist = new Dictionary<string, List<Pricelist>>();
        List<Product> tempProductlist = fullProductRepository.GetFetchXml(userId, false, filterList).Item2;
        foreach (var product in tempProductlist)
        {
            productlist.Add(product.Id, product.PriceList);
        }
        return productlist;

    }
}
}
