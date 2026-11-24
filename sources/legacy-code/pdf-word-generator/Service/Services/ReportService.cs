
using Service.Services.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Data.Repositories.Interfaces;
using Data.Models;
using System.Reflection;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Diagnostics;

namespace Service.Services
{
    public class ReportService : IReportService
    {
        IReportRepository reportRepository;
        IHelperRepository helperRepository;
        public ReportService(IReportRepository reportRepository, IHelperRepository helperRepository)
        {
            this.reportRepository = reportRepository;
            this.helperRepository = helperRepository;
        }
        IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
   .Build();
  

        public AttachmentItem CreateCombinedFile(ReportData data)
        {
            Trace.WriteLine("in CreateDocument");
            var result = reportRepository.CreateCombinedFile(data);
            Trace.WriteLine("after CreateDocument");
            return result;
        }
    }
}
