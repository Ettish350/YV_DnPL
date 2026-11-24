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
	public class WorkshopRegistrationService : BasicService<WorkshopConferenceRegistration, WorkshopConferenceRegistration, WorkshopConferenceRegistrationCRM>, IWorkshopRegistrationService
    {
		private IHelperService helperService;
		private IWorkshopRegistrationRepository workshopRegistrationRepository;
		private IQueryHelperRepository queryHelperRepository;
		public WorkshopRegistrationService(IHelperService helperService,
            IWorkshopRegistrationRepository workshopRegistrationRepository,
			 IQueryHelperRepository queryHelperRepository,
		IMapFunctionRepository mapFunctionRepository) : base(helperService, workshopRegistrationRepository, mapFunctionRepository)
		{
			this.helperService = helperService;
			this.workshopRegistrationRepository = workshopRegistrationRepository;
			this.queryHelperRepository = queryHelperRepository;
		}


    }
}
