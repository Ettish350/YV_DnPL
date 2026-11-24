using Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services.Interface
{
    public interface IOnlineReservationService
    {
        AttachmentItem CreateQRcodeTicket(OnlineReservationTicket data);

    }
}
