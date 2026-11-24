using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Models
{
    public class OnlineReservationTicket
    {
        public string FullName { get; set; }
        public string VisitDate { get; set; }
        public string VisitStartTime { get; set; }
        public string PurchaseDate {get;set;}
        public string PaymentMethod { get; set;}

        public string TotalPrice { get; set; }
        public string QRcodeUrl { get; set; }
        public string Lang { get; set; }
        public string ReservationGuid { get; set; }
        public string ReservationNumber { get; set; }

        public string ActivityName { get; set; }
        public string hall { get; set; }
        public List<QRCodeItem> QRcodeItemList { get; set; }
        public List<VisitorsDetails> visitorsDetails { get; set; }
        public List<VisitorsDetails> products { get; set; }
        public List<ActivityDetails> activitiesDetails { get; set; }
        public string TourNote { get; set; }
    }
    public class ActivityDetails
    {
        public string ActivityName { get; set; }
        public string VisitDate { get; set; }
        public string VisitStartTime { get; set; }
        public string hall { get; set; }
        public List<VisitorsDetails> visitorsDetails { get; set; }
        public List<VisitorsDetails> products { get; set; }
        //public QRcodeDetails QRcodeDetails { get; set; }
        //public string QRcodeUrl { get; set; }

    }
    public class VisitorsDetails
    {
        public string Price { get; set; }
        public string Amount { get; set; }
        public string Name { get; set; }

    }
}
