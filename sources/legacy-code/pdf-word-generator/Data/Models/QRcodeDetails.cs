using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Data.Models
{
    public class QRCodeItem
    {
        [JsonPropertyName("guid")]
        public string Guid { get; set; }
        [JsonPropertyName("isEventReservation")]

        public bool IsEventReservation { get; set; }
        //public string ActivityDayGuid { get; set; }
        [JsonPropertyName("eventCode")]

        public string EventCode { get; set; }


    }
}


