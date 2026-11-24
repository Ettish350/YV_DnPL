using Data.Models;
using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IScheduleResourceRepository
    {
        string CreateDocument(Document document, ScheduleResourceItem data);
    }
}
