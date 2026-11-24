using Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interface
{
    public interface IReportService
    {
        AttachmentItem CreateCombinedFile(ReportData data);
       
    }
}
