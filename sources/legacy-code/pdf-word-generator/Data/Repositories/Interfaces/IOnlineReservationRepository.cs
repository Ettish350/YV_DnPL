using Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using MigraDoc.DocumentObjectModel;

namespace Data.Repositories.Interfaces
{
    public interface IOnlineReservationRepository
    {
        string GenerateQRCode(List<QRCodeItem> data);
        string CreateDocument(Document document, OnlineReservationTicket data);

    }
}
