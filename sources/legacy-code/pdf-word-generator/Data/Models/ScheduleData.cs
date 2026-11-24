using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Models
{
   
    public class ScheduleReservationList
    {
        public string serviceProvider { get; set; } = "";
        public bool isHideServiceProviderName { get; set; } = false;

        public List<ScheduleReservation> reservations { get; set; }
       
    }
    public class ScheduleReservation
    {
        public bool isEventReservation { get; set; }
        public string reservationNum { get; set; }
        public string division { get; set; }
        public string instName { get; set; }
        public string instCity { get; set; }
        public string groupName { get; set; }
        public string language { get; set; }
        public string departmentName { get; set; }
        public string departmentTelephone { get; set; }
        public string departmentFax { get; set; }
        public string ageGroup { get; set; }
        public string coordinator { get; set; }
      
        

        public List<ScheduleActivityDay> days { get; set; }
    }
    public class ScheduleActivityDay
    {
      
       
        public string date { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string dayOfWeek { get; set; }
        public string  subject { get; set; }
        public int numberOfGroups { get; set; }
        public string location { get; set; }

        public List<ScheduleGroup> groups { get; set; }



    }
    public class ScheduleGroup
    {
        public string groupNumber { get; set; }
        public string groupName { get; set; }
        public string participantsNumber { get; set; }
        public string groupServiceProvider { get; set; }
        public string groupResource { get; set; }
        public List<ScheduleGroupDetails> details { get; set; }
    }
    public class ScheduleGroupDetails
    {
        public string startHour { get; set; }
        public string endHour { get; set; }
        public string product { get; set; }
        public string subject { get; set; }
        public string serviceProvider { get; set; }
        public string resource { get; set; }
        public string comments { get; set; }
    }
}
   




