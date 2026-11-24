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
	public class ItineraryGroupService : BasicService<ItineraryGroup, ItineraryGroup, ItineraryGroupCRM>, IItineraryGroupService
	{
		private IHelperService helperService;
		private IItineraryGroupRepository itineraryGroupRepository;
		private IQueryHelperRepository queryHelperRepository;
		public ItineraryGroupService(IHelperService helperService,
			IItineraryGroupRepository itineraryGroupRepository,
			 IQueryHelperRepository queryHelperRepository,
		IMapFunctionRepository mapFunctionRepository) : base(helperService, itineraryGroupRepository, mapFunctionRepository)
		{
			this.helperService = helperService;
			this.itineraryGroupRepository = itineraryGroupRepository;
			this.queryHelperRepository = queryHelperRepository;
		}
	}
}
