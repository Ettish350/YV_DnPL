using Data.Models;

using System;
using System.Collections.Generic;
using System.Text;
using Xceed.Document.NET;

namespace Data.Repositories.Interfaces
{
    public interface IScheduleRepository
    {
        AttachmentItem createWord(ScheduleReservationList data);
    }
}
