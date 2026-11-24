using Data.Models;
using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface ISchoolConfirmationRepository
    {
         string CreateDocument(Document document, SchoolConfirmationData data);
    }
}
