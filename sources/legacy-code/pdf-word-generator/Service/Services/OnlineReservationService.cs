using Data.Models;
using Data.Repositories;
using Data.Repositories.Interfaces;
using MigraDoc.DocumentObjectModel;
using Serilog;
using Service.Services.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class OnlineReservationService : IOnlineReservationService
    {
        IOnlineReservationRepository onlineReservationRepository;
        IHelperRepository helperRepository;
        public OnlineReservationService(IOnlineReservationRepository onlineReservatiomRepository,
        IHelperRepository helperRepository)
        {
            this.onlineReservationRepository = onlineReservatiomRepository;
            this.helperRepository = helperRepository;
        }
        public AttachmentItem CreateQRcodeTicket(OnlineReservationTicket data)
        {
            var QRcodeUrl= onlineReservationRepository.GenerateQRCode(data.QRcodeItemList);
            Log.Error("start  CreateDocument service");
            Document document = new Document();
            data.QRcodeUrl = QRcodeUrl;
            Log.Error("data.QRcodeUrl", data.QRcodeUrl);
            //יצירת QRcode עבור כל פעילות
 //;          if(data.activitiesDetails?.Count > 0)
 //           {
 //               foreach(var activity in data.activitiesDetails)
 //               {
 //                   activity.QRcodeUrl = onlineReservationRepository.GenerateQRCode(activity.QRcodeDetails);
 //               }
 //           }
            var createdFileName = onlineReservationRepository.CreateDocument(document, data);
            var ticket = helperRepository.AttachDocument(createdFileName, ".pdf", "Ticket");
            return ticket;
            throw new NotImplementedException();
        }

    }
}
