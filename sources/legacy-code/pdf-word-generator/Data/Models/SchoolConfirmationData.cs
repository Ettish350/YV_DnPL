using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using static iTextSharp.text.pdf.events.IndexEvents;

namespace Data.Models
{

   
        public class SchoolConfirmationData
    {

        public string Division { get; set; }
        public bool ShowDetails { get; set; } = true;
        public string GefenType { get; set; }
        public bool ShowSchedule { get; set; } = false;
        public bool ShowScheduleFirstGroup { get; set; } = false;
        public bool isNeedToShowSignatureParagraph { get; set; } = false;
        public bool IsFromOnlineReservation { get; set; } = false;
        public string creditCard4Digit { get; set; } = "";
        public string creditCardBrand { get; set; } = "";
        public string tourName { get; set; } = "";
        public bool ShowCommentFood { get; set; } = true;
        public bool ShowCustomerComments { get; set; } = true;
       


        public string VisitLang { get; set; }

        public string VisitLangEn { get; set; }

       
        public string DocLang { get; set; }

        public string Total { get; set; }
        public string TotalPriceAfterDiscount { get; set; }

        public string OrderNum { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        

        public string GroupName { get; set; }

        public string InstitutionName { get; set; }
        public string InstitutionNameOnly { get; set; }
        public string InstitutionCompanyNumberOrSymbol { get; set; }

       


        public string InstitutionAddress { get; set; }

        public string InstitutionCity { get; set; }

        public string Fax { get; set; }


        public string NumberOfParticipants { get; set; }

        public string NumberOfGroups { get; set; }

        public string Grade { get; set; }

        public string AgeGroup { get; set; }
        


        public string Department { get; set; }

        public string DepartmentEn { get; set; }
        //public string DepartmentName { get; set; }

        public string DepartmentFax { get; set; }

        public string DepartmentPhone { get; set; }

        public string DepartmentEmail { get; set; }

        public List<ActivityDay> ActivityDayList { get; set; }
        public string Discount { get; set; }

        //1=סכום
        //2=אחוז
        public string DiscountType { get; set; }

        public string CoordinatorName { get; set; }

        public string CoordinatorNameEn { get; set; }

        public string CoordinatorPhone { get; set; }

        public string CoordinatorEmail { get; set; }
        public string ResponsibleAgent { get; set; }
        public string CustomerComments { get; set; }
        public string FoodDetails { get; set; }
        public ScheduleReservationList ScheduleData { get; set; }
    }

    public class ActivityDay
    {
        
        public string Date { get; set; }

        public string ParticipantsNum { get; set; }
        public string NumberOfGroups { get; set; }

        public string Location { get; set; }

        public string LocationEn { get; set; }

        public string HouresFromTo { get; set; }
        public string MuseumEntryTime { get; set; }

        public List<Details> detailsList { get; set; }

        public List<CommemorationSchedule> scheduleList { get; set; }
        public string Total { get; set; }
        public string TotalAfterDiscount { get; set; }
        public bool CommentFood { get; set; }
        public bool CommentTestimony { get; set; }
        public bool CommentLecture { get; set; }

       

    }

    public class Details
    {
        
        public string Amount { get; set; }

        public string Total { get; set; }

        public string ItemName { get; set; }

        public string itemNameEn { get; set; }

        public string Comments { get; set; }
        public string PricePerUnit { get; set; }

        public string Discount { get; set; }

      
        public string DiscountType { get; set; }

        public string TotalAfterDiscount { get; set; }
        

    }

    public class CommemorationSchedule
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Descr { get; set; }
     


    }
}