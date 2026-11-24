using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Models
{

    public class InvoiceData
    {
       

        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("address")]
        public string address { get; set; }

        [JsonProperty("telephone")]
        public string telephone { get; set; }

        [JsonProperty("relatedAccountName")]
        public string relatedAccountName { get; set; }

        [JsonProperty("relatedAccountAddress")]
        public string relatedAccountAddress { get; set; }

        [JsonProperty("status")]
        public string status { get; set; }


        [JsonProperty("semel")]
        public string semel { get; set; }
      
        [JsonProperty("invoiceNum")]
        public string invoiceNum { get; set; }

        [JsonProperty("currencyCode")]
        public string currencyCode { get; set; }



        //1=סכום
        //2=אחוז

        [JsonProperty("comments")]
        public string comments { get; set; }

        [JsonProperty("gefenType")]
        public string gefenType { get; set; }

        [JsonProperty("department")]
        public string department { get; set; }

        [JsonProperty("departmentEn")]
        public string departmentEn { get; set; }

        [JsonProperty("createdBy")]
        public string createdBy { get; set; }
        [JsonProperty("lang")]
        public string lang { get; set; }

        [JsonProperty("isCancel")]
        public bool isCancel { get; set; } = false;

        [JsonProperty("division")]
        public string division { get; set; }

        [JsonProperty("InvoiceTotalPrice")]
        public string InvoiceTotalPrice { get; set; }
        [JsonProperty("InvoiceTotalPriceAfterDiscount")]
        public string InvoiceTotalPriceAfterDiscount { get; set; }

        [JsonProperty("InvoiceTotalPriceAfterDiscountInILS")]
        public string InvoiceTotalPriceAfterDiscountInILS { get; set; }

        [JsonProperty("tables")]
        public List<TableDetails> tables { get; set; }
    }
    public class TableRowDetails
    {
        [JsonProperty("date")]
        public string date { get; set; }

        [JsonProperty("dateForDisplay")]
        public string dateForDisplay { get; set; }
        [JsonProperty("perut")]
        public string perut { get; set; }

        [JsonProperty("perutEn")]
        public string perutEn { get; set; }

        [JsonProperty("price")]
        public string price { get; set; }
        [JsonProperty("amount")]
        public string amount { get; set; }
        [JsonProperty("discount")]
        public string discount { get; set; }

        [JsonProperty("discountType")]
        public string discountType { get; set; }

        [JsonProperty("total")]
        public string total { get; set; }
        [JsonProperty("totalAfterDiscount")]
        public string totalAfterDiscount { get; set; }

        
    }

    public class TableDetails
    {
        [JsonProperty("contact")]
        public string contact { get; set; }

        [JsonProperty("reservationNum")]
        public string reservationNum { get; set; }

        [JsonProperty("groupName")]
        public string groupName { get; set; }

        [JsonProperty("tableRowDetails")]
        public List<TableRowDetails> tableRowDetails { get; set; }

        [JsonProperty("totalPrice")]
        public string totalPrice { get; set; }

        [JsonProperty("totalPriceAfterDiscount")]
        public string totalPriceAfterDiscount { get; set; }

        [JsonProperty("discount")]
        public string discount { get; set; }

        [JsonProperty("discountType")]
        public string discountType { get; set; }

    }

}


