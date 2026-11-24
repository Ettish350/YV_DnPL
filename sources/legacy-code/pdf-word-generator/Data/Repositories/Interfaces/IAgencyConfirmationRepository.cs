using Data.Models;
using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repositories.Interfaces
{
    public interface IAgencyConfirmationRepository
    {
         string CreateDocument(Document document, AgencyConfirmationData data);
    }
}
