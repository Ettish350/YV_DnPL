using Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interface
{
    public interface IScheduleService
    {
        AttachmentItem CreateDocument(ScheduleReservationList data);
        AttachmentItem createScheduleResourceDoc(ScheduleResourceItem data);
    }
}
