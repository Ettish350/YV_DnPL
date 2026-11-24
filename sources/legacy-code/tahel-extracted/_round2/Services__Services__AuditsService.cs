using Data.Repositories.Interfaces;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class AuditsService: IAuditsService
    {
        IAuditsRepository auditsRepository;
        public AuditsService(IAuditsRepository auditsRepository)
        {
            this.auditsRepository = auditsRepository;   
        }
        public string GetAudits(string userId, string guid)
        {
            return auditsRepository.GetAudits(userId, guid);
        }
    }
}
