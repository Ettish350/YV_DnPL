using Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interface
{
    public interface IConfirmationService
    {
        AttachmentItem CreateSchoolDocument(SchoolConfirmationData confirmationData);
        AttachmentItem CreateAgencyDocument(AgencyConfirmationData confirmationData);
        bool isDataValid(SchoolConfirmationData data);
        AttachmentItem CreateCommemorationDocument(SchoolConfirmationData data);
    }
}
