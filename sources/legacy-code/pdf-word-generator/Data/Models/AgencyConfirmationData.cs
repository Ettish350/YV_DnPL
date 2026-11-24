using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Models
{

    public class AgencyConfirmationData
    {
        [JsonProperty("docLang")]
        public string DocLang { get; set; }

        [JsonProperty("tourDate")]
        public string TourDate { get; set; }

        [JsonProperty("tourTime")]
        public string TourTime { get; set; }

        [JsonProperty("approvedBy")]
        public string ApprovedBy { get; set; }

        [JsonProperty("updatedOn")]
        public string UpdatedOn { get; set; }

        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("contactPhone")]
        public string ContactPhone { get; set; }

        [JsonProperty("activityType")]
        public string ActivityType { get; set; }

        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; }
        
        [JsonProperty("orderNum")]
        public string OrderNum { get; set; }

        [JsonProperty("contactName")]
        public string ContactName { get; set; }

        [JsonProperty("groupName")]
        public string GroupName { get; set; }
        
        [JsonProperty("numberOfParticipants")]
        public string NumberOfParticipants { get; set; }
     
        [JsonProperty("productList")]
        public List<TourReservationProduct> ProductList { get; set; }
      
        [JsonProperty("comments")]
        public string Comments { get; set; }

        [JsonProperty("total")]
        public string Total { get; set; }

        [JsonProperty("totalPriceAfterDiscount")]
        public string TotalPriceAfterDiscount { get; set; }

        [JsonProperty("isNeedToshowSignatureParagraph")]
        public bool isNeedToshowSignatureParagraph { get; set; } = false;


    }

    public class TourReservationProduct
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("language")]
        public string Language { set; get; }

        [JsonProperty("totalPriceAfterDiscount")]
        public string TotalPriceAfterDiscount { set; get; }

        [JsonProperty("discountType")]
        public string DiscountType { set; get; }
        
    }

    enum DiscountType
    {
        None = 0,
        Sum = 1,
        Percent = 2,
        MinistryOfEducation = 3,
    }



}