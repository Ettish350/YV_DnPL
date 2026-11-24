
using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;

namespace Data.Models
{
    public class ScheduleResourceItem
    {
        public List<ScheduleResourcesTableLine> ScheduleResourcesTable { get; set; } 
        public List<ScheduleResourcesTableLine> Comments { get; set; }
        public List<List<string>> UnusualActivityHoursComments { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }

    }

    public class ScheduleResourcesTableLine
    {
        public string Title { get; set; }
        public List<SchedulerEventItem> ScheduleEvents { get; set; }
    }

    public class SchedulerEventItem
    {
        public string ReservationId { get; set; }
        public string ReservationName { get; set; }
        public string ResourceId { get; set; }
        public string ResourceName { get; set; }
        public DateTime Date { get; set; }
        public string CreatedBy { get; set; }
        public string Conetnt { get; set; }
        public string Comments { get; set; }
        public string ClassType { get; set; }
        public bool IsNeedRefreshments { get; set; }
        public bool IsNeedModifyArrangement { get; set; }
    }
    public static class SchedulerEventClassType
    {
        public static Dictionary<string, Color> ClassType { get; set; } = new Dictionary<string, Color>();
        static SchedulerEventClassType()
        {
            ClassType.Add("guidance", new Color(255, 255, 163));
            ClassType.Add("teachers", new Color(187, 217, 246));
            ClassType.Add("save-spot-no-reservation", new Color(225, 255, 180));
            ClassType.Add("abroad", new Color(249, 197, 214));
            ClassType.Add("army", new Color(177, 242, 204));
            ClassType.Add("not-recognized", new Color(237, 196, 254));
            ClassType.Add("not-approved", new Color(184, 184, 184));
        }
    }
}
