
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
using MigraDoc.DocumentObjectModel;

namespace Service.Services
{
    public class ScheduleService : IScheduleService
    {
        IScheduleRepository  scheduleRepository;
        IScheduleResourceRepository scheduleResourceRepository;
        IHelperRepository helperRepository;
        public ScheduleService(IScheduleRepository scheduleRepository,IHelperRepository helperRepository , IScheduleResourceRepository scheduleResourceRepository)
        {
            this.scheduleRepository = scheduleRepository;
            this.scheduleResourceRepository = scheduleResourceRepository;
            this.helperRepository = helperRepository;
        }
        IConfiguration configuration = new ConfigurationBuilder()
   .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
   .Build();
        public AttachmentItem CreateDocument(ScheduleReservationList data)
        {
            Trace.WriteLine("in CreateDocument");
            var result=scheduleRepository.createWord(data);

            Trace.WriteLine("after CreateDocument");
            return result;

        }

        public AttachmentItem createScheduleResourceDoc(ScheduleResourceItem data)
        {
            Trace.WriteLine("in CreateDocument");
            Document document = new Document();
            var createdFileName = scheduleResourceRepository.CreateDocument( document , data);
            Trace.WriteLine("after CreateDocument");
            return helperRepository.AttachDocument(createdFileName);

        }


    }
}
