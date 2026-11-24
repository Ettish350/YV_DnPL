using Data.Modals;
using Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace Services.Services
{
    public class ScheduleEventsHelperService : IScheduleEventsHelperService
    {

        private readonly IGoogleApiService googleApiService;
        IProductService productService;
        public ScheduleEventsHelperService(IGoogleApiService googleApiService,
              IProductService productService)
        {
            this.googleApiService = googleApiService;
            this.productService = productService;
        }

        readonly HttpClient client = new HttpClient();

        public List<Coordinate> IsraelCitiesList()
        {
            List<Coordinate> coordinatesList = new List<Coordinate>();
            try
            {
                // Read from local JSON file instead of calling the GOV API
                string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "IsraelCities.json");
                string jsonContent = File.ReadAllText(jsonFilePath);
                JavaScriptSerializer js = new JavaScriptSerializer();
                js.MaxJsonLength = int.MaxValue;
                var cities = (object[])js.DeserializeObject(jsonContent);

                foreach (Dictionary<string, object> city in cities)
                {
                    coordinatesList.Add(new Coordinate
                    {
                        Id = city["Id"].ToString(),
                        Value = city["Value"].ToString().Trim(),
                        Lat = city["Lat"].ToString(),
                        Lng = city["Lng"].ToString()
                    });
                }
            }
            catch (Exception)
            {
                // Fallback hardcoded list in case file read fails
            }

            // Add custom locations that don't exist in the GOV data
            coordinatesList.Add(new Coordinate { Id = "1111", Value = "בית שמש", Lat = "31.746853753317108", Lng = "34.990335452608754" });
            coordinatesList.Add(new Coordinate { Id = "1112", Value = "אבן יהודה", Lat = "32.27012254593142", Lng = "34.87821648704401" });
            coordinatesList.Add(new Coordinate { Id = "1113", Value = "צומת בני דרור", Lat = "32.2579718580552", Lng = "34.90117557977772" });
            coordinatesList.Add(new Coordinate { Id = "1114", Value = "יד ושם", Lat = "31.77283380013671", Lng = "35.18150389888085" });
            coordinatesList.Add(new Coordinate { Id = "1115", Value = "עיר הבה\"דים", Lat = "31.058813750065113", Lng = "34.83849054531375" });
            coordinatesList.Add(new Coordinate { Id = "1116", Value = "שדות מיכה", Lat = "31.7217196", Lng = "34.9260959" });
            coordinatesList.Add(new Coordinate { Id = "1117", Value = "ביסל\"א", Lat = "30.9811697", Lng = "34.7664898" });
            coordinatesList.Add(new Coordinate { Id = "1118", Value = "יד לשריון, לטרון", Lat = "31.8380778", Lng = "34.9823878" });
            coordinatesList.Add(new Coordinate { Id = "1119", Value = "מחנה עופר", Lat = "31.8730516", Lng = "35.18155745" });
            coordinatesList.Add(new Coordinate { Id = "1120", Value = "בית ווהלין", Lat = "32.06164549212693", Lng = "34.816731130053576" });
            coordinatesList.Add(new Coordinate { Id = "1121", Value = "מעאר", Lat = "32.8907712", Lng = "35.4396294" });
            coordinatesList.Add(new Coordinate { Id = "1122", Value = "מתחם בתי הכלא באר שבע ", Lat = "31.207103168632386", Lng = "34.8107493524241" });
            coordinatesList.Add(new Coordinate { Id = "1123", Value = "בית כלא נפחא", Lat = "30.7264586", Lng = "34.7757137" });
            coordinatesList.Add(new Coordinate { Id = "1124", Value = "מחנה עמנואל-בסיס ג'וליס", Lat = "31.692884", Lng = "34.6780397" });
            coordinatesList.Add(new Coordinate { Id = "1125", Value = "מחנה פלס", Lat = "32.2571645", Lng = "35.5137437" });
            coordinatesList.Add(new Coordinate { Id = "1126", Value = "מחנה שבטה", Lat = "30.9246579", Lng = "34.6594354" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "מחנה צריפין", Lat = "31.9573272", Lng = "34.8493208" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "בסיס תל השומר שער צפון", Lat = "32.050105674506874", Lng = "34.850165044542" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "ג'למה", Lat = "32.5088815", Lng = "35.3479754" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "בה\"ד 1 בית ספר לקצינים", Lat = "30.651070194560035", Lng = "34.797492231085364" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "מחנה נחשונים", Lat = "32.04545898661605", Lng = "34.98357846562144" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "המכללה הלאומית לשוטרים-בית שמש", Lat = "31.738834191407797", Lng = "34.94967708867878" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "תעש שער שיא", Lat = "32.131290876096124", Lng = "34.871282402163835" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "בית סוהר קציעות", Lat = "30.97080775791253", Lng = "34.49826503926471" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "מחנה רעים", Lat = "31.362783238253797", Lng = "34.449303273639615" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "בסיס ביסל\"ח", Lat = "31.1165882", Lng = "34.9549255" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "מחנה משואה", Lat = "31.0322596", Lng = "34.9751652" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "יישוב בית גבריאל", Lat = "32.2242701", Lng = "34.7470149" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "מתקן אדם", Lat = "31.9503965", Lng = "35.0001334" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "מחנה בירנית", Lat = "32.4283943", Lng = "35.5993025" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "בסיס סיירים", Lat = "29.916650284196407", Lng = "34.84574034047702" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "צוקי עובדה", Lat = "29.954760774617263", Lng = "34.89145219417095" });
            coordinatesList.Add(new Coordinate { Id = "1127", Value = "מחנה שזפון", Lat = "30.55229114061747", Lng = "34.965763807686656" });

            return coordinatesList;
        }

        /// <summary>
        /// One-time utility method to regenerate the IsraelCities.json from the GOV API.
        /// Run manually if the data needs to be refreshed.
        /// </summary>
        public void RegenerateIsraelCitiesJson()
        {
            string readyUrl = "https://data.gov.il/api/3/action/datastore_search?resource_id=b6ca0817-187e-470f-be34-4561c2e404b2&limit=32000";
            WebRequest request = WebRequest.Create(readyUrl);
            WebResponse response = request.GetResponse();

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var rawJson = reader.ReadToEnd();
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                // Save raw response for reference
                File.WriteAllText(Path.Combine(outputDir, "IsraelCitiesRaw.json"), rawJson);
                // After saving, run the PowerShell script GenerateIsraelCities.ps1 to convert ITM to WGS84
            }
        }

        private static readonly string[] CityPrefixes = { "מושב ", "קיבוץ ", "כפר ", "ישוב ", "יישוב ", "עיר ", "שכונת " };

        private Coordinate FindCityInList(List<Coordinate> israelCityList, string cityName)
        {
            if (string.IsNullOrEmpty(cityName)) return null;
            
            string trimmed = cityName.Trim();
            // Try exact match first
            Coordinate result = israelCityList.Find(n => n.Value == trimmed);
            if (result != null) return result;

            // Try removing common prefixes
            foreach (string prefix in CityPrefixes)
            {
                if (trimmed.StartsWith(prefix))
                {
                    string withoutPrefix = trimmed.Substring(prefix.Length).Trim();
                    result = israelCityList.Find(n => n.Value == withoutPrefix);
                    if (result != null) return result;
                }
            }

            return null;
        }

        public List<EventItemForExcel> CalculateKm(List<EventItemForExcel> finalList)
        {
            List<Coordinate> israelCityList = IsraelCitiesList();
            int i = 0;
            foreach (EventItemForExcel item in finalList)
            {
                i++;
                item.Km = -1;

                if(item.IsVirtual)
                {
                    item.Km = 0;
                    item.KmForActivityName = 0;
                    continue;
                }

                Coordinate activityCity = FindCityInList(israelCityList, item.Location.Value);

                if (activityCity != null && item.Location.Value == LocationTypeHelper.YadVashem)
                {
                    activityCity = new Coordinate(activityCity);
                    activityCity.Value = "ירושלים";
                    //item.ActivityLocationCity = "ירושלים";
                }
                else if (activityCity != null && item.Location.Value == LocationTypeHelper.WahlinHouse)
                {
                    activityCity = new Coordinate(activityCity);
                    activityCity.Value = "גבעתיים";
                    //item.ActivityLocationCity = "גבעתיים";
                }
                else if (item.Location.Value == LocationTypeHelper.Customer)
                {
                    if (item.IsAddressDifferentFromInstitution)
                    {
                        activityCity = FindCityInList(israelCityList, item.ActivityLocationCity);
                        if (activityCity == null) //Try get lng&lat by google
                        {
                            activityCity = this.googleApiService.GetLatLngByName(item.ActivityLocationCity);
                        }
                    }
                    else
                    {
                        if (item.Institution != null && item.Institution.AddressInfo != null)
                        {
                            activityCity = FindCityInList(israelCityList, item.Institution.AddressInfo.City);
                            if (activityCity == null) //Try get lng&lat by google
                            {
                                activityCity = this.googleApiService.GetLatLngByName(item.Institution.AddressInfo.City);
                            }
                        }
                    }
                }
                else if(activityCity == null)
                { 
                    activityCity = this.googleApiService.GetLatLngByName(item.Location.Value);
                }
                //!אל תחשב את הקילומטר אם אתה ביד ושם! וגם אתה פעולת הדרכה
                //, מקווים שכל המוצרים של פעולות הדרכה מפורטים כאן, אם זה נכנס אפילו שמדובר בפעולה של הדרכה- כנראה ששם המוצר לא נכלל כאן בתנאי
                
                Coordinate serviceProviderCity = FindCityInList(israelCityList, item.ServiceProviderCityLocation);
                if (serviceProviderCity == null) //Try get lng&lat by google
                {
                    serviceProviderCity = this.googleApiService.GetLatLngByName(item.ServiceProviderCityLocation);
                }

                if ( 
                    !item.ActivityType.Value.Contains("הצגה") && 
                    !item.ActivityType.Value.Contains("עדות") && 
                    !item.ActivityType.Value.Contains("בית מדרש") &&
                    !item.ActivityType.Value.Contains("סיור מורחב") &&
                    !item.ActivityType.Value.Contains("סיור במוזיאון") &&
                    !item.ActivityType.Value.Contains("סיור מודרך באתר") &&
                    !item.ActivityType.Value.Contains("תרגום סימולטני אישי") &&
                    !item.ActivityType.Value.Contains("מנחה") &&
                    !item.ActivityType.Value.Contains("חזיון אורקולי") &&
                    !((item.ActivityType.Value.Contains("הדרכה") || item.ActivityType.Value.Contains("מדריך") || item.ActivityType.Value.Contains("מדריכים")) && 
                        (item.Location.Value == LocationTypeHelper.YadVashem || item.Location.Value == LocationTypeHelper.WahlinHouse || item.Location.Value == LocationTypeHelper.Virtual))
                   )

                {//תח"צ או ללא נסיעות
                    if (activityCity != null && serviceProviderCity != null && activityCity.Value == serviceProviderCity.Value)
                    {// same city
                        item.Km = 10;

                    }
                    else
                    {
                        if (activityCity != null && serviceProviderCity != null)
                        {
                            item.Km = this.googleApiService.GetKmByLatLng(new GoogleCordinations
                            {
                                origLat = serviceProviderCity.Lat,
                                origLng = serviceProviderCity.Lng,
                                destLat = activityCity.Lat,
                                destLng = activityCity.Lng
                            });
                        }
                    }

                }
                else
                {
                    item.Km = 0;

                }
                item.KmForActivityName = item.Km;
                if (item.PaymentForTravelType == (int)TravelType.NOTHING || item.PaymentForTravelType == (int)TravelType.NO)
                {
                    item.Km = 0;
                }
                if (item.Km >0 && item.PaymentForTravelType == (int)TravelType.ONESIDE)
                {
                    item.Km = item.Km / 2;
                }

                /*if (!item.IsPaymentForTravel)// if has taxi or ! IsPaymentForTravel
                {
                    item.Km = 0;
                }*/


                // DateTime.ParseExact(e.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() == DateTime.ParseExact(item.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() &&
                //devora- if has 2 activites on the same activity day- put 0 on all km excep for the first
                //shiraD-add another condition -  if has 2 activites in the BahadisCity ActivityLocation
                //(one is in the Customer and one is in the BahadisCity) in the same date and with the same service provider - put 0 on all km excep for the first
                List<EventItemForExcel> moreThanOneList = finalList.FindAll(e =>
                     (e.Location.Value == item.Location.Value &&
                    (item.Location.Value == LocationTypeHelper.BahadisCity ||
                    (item.Location.Value == LocationTypeHelper.Customer &&
                    (item.IsAddressDifferentFromInstitution == true && item.ActivityLocationAddress == e.ActivityLocationAddress ||
                    item.IsAddressDifferentFromInstitution == false && item.Institution.AddressInfo!=null && item.Institution.AddressInfo.Address == e.Institution.AddressInfo.Address)))
                    ||
                    item.Location.Value == LocationTypeHelper.BahadisCity && e.Location.Value == LocationTypeHelper.Customer && item.Location.Value == e.ActivityLocationCity
                    ||
                    item.Location.Value == LocationTypeHelper.Customer && e.Location.Value == LocationTypeHelper.BahadisCity && item.ActivityLocationCity == e.Location.Value)
                    &&
                    e.ServiceProviderId == item.ServiceProviderId &&
                    e.IsForPay == item.IsForPay &&
                    DateTime.Parse(e.Start).ToShortDateString() == DateTime.Parse(item.Start).ToShortDateString())
                    .OrderBy(e => e.ServiceProviderFullName).ToList();
                
                if (moreThanOneList.Count > 1)
                {
                    for (int j = 1; j < moreThanOneList.Count; j++)//leave km of first item
                    {
                        moreThanOneList[j].Km = 0;
                        moreThanOneList[j].KmForActivityName = 0;
                    }


                }

               
            }
            foreach (EventItemForExcel item in finalList)
            {
                //devora 29-11-22
                //אם יש 2 הרצאות לאותו נותן שירות באותו אירוע או הזמנה, רק ההרצאה הראשונה נשארת אותו מרחק וכל השאר קרוב, ולכן שמים -1 כדי שבקלינט יצא הרצאה קרוב
                List<EventItemForExcel> lecturerList = finalList.FindAll(e =>
                       e.ReservationNum == item.ReservationNum &&
                       e.ServiceProviderId == item.ServiceProviderId &&
                       e.IsForPay == item.IsForPay &&
                       item.IsForPay==true &&
                       e.ActivityType.Id == item.ActivityType.Id &&
                       item.GuideType == e.GuideType &&
                       DateTime.Parse(e.Start).ToShortDateString() == DateTime.Parse(item.Start).ToShortDateString() &&
                       item.ActivityType.Value.Contains("הרצאה") &&
                       e.Location.Value == item.Location.Value &&
                       item.KmForActivityName != -1
                       );
                if (lecturerList.Count > 1)
                {
                    for (int j = 1; j < lecturerList.Count; j++)
                    {
                        lecturerList[j].KmForActivityName = -1;
                    }

                }
            }






                return finalList;
        }
       

      
        public List<EventItemForExcel> UnionScheduleEvents(string userId, List<EventItemForExcel> eventList, List<EventItemForExcel> cascadedList)
        {

            List<EventItemForExcel> unionList = new List<EventItemForExcel>();
            FillGuideTypeInScheduleItems(userId, eventList, cascadedList);
           
            var temp = eventList.Select(x => x.ServiceProviderId != string.Empty);
            temp.ToList();
            foreach (EventItemForExcel item in eventList)
            {
                List<EventItemForExcel> innerList = eventList.FindAll(e =>
                    e.ServiceProviderId == item.ServiceProviderId &&
                    e.IsForPay == item.IsForPay &&
                    DateTime.Parse(e.Start).ToShortDateString() == DateTime.Parse(item.Start).ToShortDateString() &&
                    (e.ActivityType.Value.Contains("הדרכה") || e.ActivityType.Value.Contains("ניידת")) &&
                    e.Location.Value == item.Location.Value &&
                    //compare activity Address(without Bahadis location)-> institusion | is diffrent
                    (item.Location.Value == LocationTypeHelper.BahadisCity ||
                    (item.Location.Value == LocationTypeHelper.Customer &&
                    (item.IsAddressDifferentFromInstitution == true && item.ActivityLocationAddress == e.ActivityLocationAddress ||
                    item.IsAddressDifferentFromInstitution == false && item.Institution.AddressInfo!=null &&item.Institution.AddressInfo.Address == e.Institution.AddressInfo.Address))));
                if (innerList.Count > 1)
                {
                    eventList = eventList.Except(innerList).ToList();
                    EventItemForExcel newItem = innerList[0];
                    execUnionEvents(innerList, cascadedList, newItem);
                    unionList.Add(newItem);
                }

                //כאשר מדובר בארועים - לצרף את כל השורות של נותני השירות של אותו יום באותו הזמנה
                //רק את המוצרים שסוג המדריך שלהם הוא הדרכה

                List<EventItemForExcel> eventReservationGuideInnerList = eventList.FindAll(e =>
                   e.IsEventReservation == true &&
                     e.ReservationNum==item.ReservationNum &&// 28.5.23 בשביל שלא יתאחד הדרכה מ2 אירועים שונים
                    e.ServiceProviderId == item.ServiceProviderId &&
                    e.IsForPay == item.IsForPay &&
                    e.GuideType == "1" &&//רק מוצרים עם סוג מדריך: הדרכה
                    !e.ActivityType.Value.Contains("מלחמה") && //כדי שלא יאחד מוצר שמכיל " מלחמה" 
                    DateTime.Parse(e.Start).ToShortDateString() == DateTime.Parse(item.Start).ToShortDateString() &&
                    //  !e.ActivityType.Value.Contains("הרצאה") &&
                    e.Location.Value == item.Location.Value );
                //  &&   (item.Location.Value == LocationTypeHelper.YadVashem)
                if (eventReservationGuideInnerList.Count > 1)
                {
                    eventList = eventList.Except(eventReservationGuideInnerList).ToList();
                    List<EventItemForExcel> notGuideInnerList = eventList.FindAll(e =>
                  item.IsEventReservation == true &&
                   e.ServiceProviderId == item.ServiceProviderId &&
                   e.IsForPay == item.IsForPay &&
                   e.GuideType != "1" &&//רק מוצרים עם סוג מדריך שונה  מהדרכה
                   DateTime.Parse(e.Start).ToShortDateString() == DateTime.Parse(item.Start).ToShortDateString() &&
                   //  !e.ActivityType.Value.Contains("הרצאה") &&
                   e.Location.Value == item.Location.Value );
                 


                    EventItemForExcel newItem = UnionEventReservationEvents(eventReservationGuideInnerList, notGuideInnerList);
                    unionList.Add(newItem);
                }
                else if(eventReservationGuideInnerList.Count==1)//גם  נותן שירות שקיים רק פעם אחת - צריך להחליף לו את הסוג פעילות להדרכה לפי מספר השעות
                {
                   UnionEventReservationEvents(eventReservationGuideInnerList, null);
                }


                //כאשר מדובר בארועים - לצרף את כל השורות של נותני השירות מסוג מורי דרך של אותו יום באותו הזמנה
                //לצרף רק אם זה אותו מוצר של מורה דרך

                List<EventItemForExcel> trourGuideInnerList = eventList.FindAll(e =>
                   e.IsEventReservation == true &&
                    e.ReservationNum == item.ReservationNum &&
                    e.ServiceProviderId == item.ServiceProviderId &&
                    e.IsForPay == item.IsForPay &&
                    e.ActivityType.Id == item.ActivityType.Id &&
                    item.GuideType == "9" &&//רק מוצרים עם סוג מורה דרך
                    item.GuideType == e.GuideType &&
                    DateTime.Parse(e.Start).ToShortDateString() == DateTime.Parse(item.Start).ToShortDateString() &&
                    //  !e.ActivityType.Value.Contains("הרצאה") &&
                    e.Location.Value == item.Location.Value &&
                    (item.Location.Value == LocationTypeHelper.YadVashem));
                if (trourGuideInnerList.Count > 1)
                {
                    eventList = eventList.Except(trourGuideInnerList).ToList();
                    EventItemForExcel newItem = UnionEventReservationTimeOnly(trourGuideInnerList);
                    unionList.Add(newItem);
                }
                if (item.ActivityType.Value.Contains("ניידת"))
                {
                    DateTime minStartDate = DateTime.Parse(item.StartEvent);
                    DateTime maxEndDate = DateTime.Parse(item.EndEvent);
                    var hours = (maxEndDate - minStartDate).TotalHours;
                    var minutes = (maxEndDate - minStartDate).TotalMinutes % hours;

                    if (hours > 6 || (hours == 6 && minutes > 0))
                    {
                        item.ActivityType.Value = "הדרכה עד 8 שעות";
                    }
                    else if (hours > 3 || (hours == 3 && minutes > 0))
                    {
                        item.ActivityType.Value = "הדרכה עד 6 שעות";
                    }
                    else
                    {
                        item.ActivityType.Value = "הדרכה עד 3 שעות";
                    }
                }


            }

            //var res1 = eventList
            //    .GroupBy(x => new
            //    {
            //        x.ReservationNum,
            //        x.ServiceProviderId,
            //        x.IsForPay,
            //        x.ActivityType.Id,
            //        x.GuideType,
            //        x.Location
            //    });
            //foreach (var xx in res1)
            //{
            //    var a = xx;
            //}
            // var grouped = unionList.GroupBy(x => new { x.ReservationNum, x.ServiceProviderId });
            //var result = unionList
            //    .GroupBy(x => x.ReservationNum && x.ServiceProviderId)        // Group by name
            //    .Where(g => g.Count() > 1 )   // Select only groups having duplicates
            //    .SelectMany(g => g);


            eventList.AddRange(unionList);
            return eventList;
        }

      
        private void FillGuideTypeInScheduleItems(string userId, List<EventItemForExcel> eventList, List<EventItemForExcel> cascadedList)
        {
            Filter filter = new Filter
            {
                Field = "Id",
                Type = "lookup",
                Operator = "multiple",
                Alias = "Product",
            };
            //List<EventItemForExcel> joinedList = new List<EventItemForExcel>(eventList);
            //joinedList.AddRange(cascadedList);
            var activityDayTypes= eventList.Select(x => x.ActivityType.Id).Distinct();

            filter.Value =String.Join(";", activityDayTypes);
            //foreach (var item in eventList)
            //{

            //    if (filter.Value != null)
            //    {
            //        filter.Value += ";" + item.ActivityType.Id;
            //    }
            //    else
            //    {
            //        filter.Value = item.ActivityType.Id;
            //    }
            //}

            //get all products for guide type;
            List<Product> productList = productService.GetAll(userId, "", filter);
            Product pro;
            foreach (var item in eventList)
            {
                pro = productList.Find(p => p.Id == item.ActivityType.Id);
                if (pro != null)
                {
                    item.GuideType = pro.ServiceProviderType.ToString();
                }
            }
            foreach (var item in cascadedList)
            {
                pro = productList.Find(p => p.Id == item.ActivityType.Id);
                if (pro != null)
                {
                    item.GuideType = pro.ServiceProviderType.ToString();
                }
            }

        }

        public void execUnionEvents(List<EventItemForExcel> list, List<EventItemForExcel> cascadedList, EventItemForExcel mainItem)
        {
            DateTime minStartDate;
            DateTime maxEndDate;
            string keepName= mainItem.ActivityType.Value;
            if(mainItem.ActivityType.Value.Contains("ניידת"))//טיפול עבור נידות- האם יש דרך יותר נכונה לעשות את זה<???
            {
                cascadedList = list;
            }
            List<EventItemForExcel> innerCascadedList = new List<EventItemForExcel>();
            if (cascadedList!=null && cascadedList.Count > 0)
            {
                foreach (EventItemForExcel item in list)
                {
                    innerCascadedList.AddRange(
                        cascadedList.FindAll(e => e.ReservationId == item.ReservationId &&
                                                  e.ServiceProviderId == item.ServiceProviderId &&
                                                  DateTime.Parse(e.Start).ToShortDateString() == DateTime.Parse(item.Start).ToShortDateString() &&
                                                  e.Location.Value == item.Location.Value &&
                                                  (item.Location.Value == LocationTypeHelper.BahadisCity ||
                                                  (item.Location.Value == LocationTypeHelper.Customer &&
                                                  (item.IsAddressDifferentFromInstitution == true && item.ActivityLocationAddress == e.ActivityLocationAddress ||
                                                  item.IsAddressDifferentFromInstitution == false && item.Institution.AddressInfo.Address == e.Institution.AddressInfo.Address)))));

                    /*List<EventItemForExcel> itemCascadedList = cascadedList.FindAll(e => e.ReservationId == item.ReservationId &&
                                                  e.ServiceProviderId == item.ServiceProviderId &&
                                                  DateTime.ParseExact(e.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() == DateTime.ParseExact(item.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() &&
                                                  e.Location.Value == item.Location.Value &&
                                                  (item.Location.Value == BahaisCity ||
                                                  (item.Location.Value == Customer &&
                                                  (item.IsAddressDifferentFromInstitution == true && item.ActivityLocationAddress == e.ActivityLocationAddress ||
                                                  item.IsAddressDifferentFromInstitution == false && item.Institution.AddressInfo.Address == e.Institution.AddressInfo.Address))));
                    if(itemCascadedList.Count < 1)
                    {
                        itemCascadedList.Add(item);
                    }
                    innerCascadedList.AddRange(itemCascadedList);*/
                }
            }
            //אם יש באיחוד גם אירוע- צריך לקחת את השיבוץ עצמו ולא לבדוק אם הוא קיים בשיבוצים המחולחלים 30.8.22
            innerCascadedList.AddRange(list.FindAll(x => x.IsEventReservation == true));
          
            if (innerCascadedList.Count > 0)
            {
                minStartDate = DateTime.Parse(innerCascadedList[0].StartEvent);
                maxEndDate = DateTime.Parse(innerCascadedList[0].EndEvent);
            }
            else
            {
                minStartDate = DateTime.Parse(mainItem.StartEvent);
                maxEndDate = DateTime.Parse(mainItem.EndEvent);
            }
            if (innerCascadedList.Count > 0)
            {
                foreach (EventItemForExcel item in innerCascadedList)
                {
                    if (DateTime.Parse(item.StartEvent) < minStartDate)
                    {
                        minStartDate = DateTime.Parse(item.StartEvent);
                    }
                    if (DateTime.Parse(item.EndEvent) > maxEndDate)
                    {
                        maxEndDate = DateTime.Parse(item.EndEvent);
                    }
                }

                mainItem.StartEvent = minStartDate.ToString();
                mainItem.EndEvent = maxEndDate.ToString();
                var hours = (maxEndDate - minStartDate).TotalHours;
                var minutes = (maxEndDate - minStartDate).TotalMinutes % hours;
                if (hours > 6 || (hours == 6 && minutes > 0))
                {
                    mainItem.ActivityType.Value = "הדרכה עד 8 שעות";//change it?
                                                                    //mainItem.ActivityType.Id = "-1";
                }
                else if (hours > 3 || (hours == 3 && minutes > 0))
                {
                    mainItem.ActivityType.Value = "הדרכה עד 6 שעות";//change it?
                                                                    //mainItem.ActivityType.Id = "-1";
                }
                else
                {
                    mainItem.ActivityType.Value = "הדרכה עד 3 שעות";//change it?
                                                                    //mainItem.ActivityType.Id = "-1";
                }
                if (keepName.Contains("ניידת"))
                {
                    mainItem.ActivityType.Value = keepName;
                }
                //אם באיחוד שיבוצים יש לאחד מהשיבוצים מסגרת תקציבית של צהל ירידה לדרום שזה עיר הבהדים אז צריך שזה יהיה המסגרת התקציבית זה נועד בשביל איחוד של שיבוץ של סמינר
                if (innerCascadedList.FindAll(x => x.BudgetFramework.Name == "צהל- ירידה לדרום").Count > 0)
                {
                    mainItem.BudgetFramework = innerCascadedList.FindAll(x => x.BudgetFramework.Name == "צהל- ירידה לדרום")[0].BudgetFramework;
                }
            }

        }
        public EventItemForExcel UnionEventReservationEvents(List<EventItemForExcel> listWithGuide, List<EventItemForExcel> listWithOutGuide)
        {
            //with guide - עם סוג מדריך הדרכה
            EventItemForExcel mainItem = listWithGuide[0];
            DateTime minStartDate = DateTime.Parse(listWithGuide[0].StartEvent);
            DateTime maxEndDate = DateTime.Parse(listWithGuide[0].EndEvent);
            foreach (EventItemForExcel item in listWithGuide)
            {
                if (DateTime.Parse(item.StartEvent) < minStartDate)
                {
                    minStartDate = DateTime.Parse(item.StartEvent);
                }
                if (DateTime.Parse(item.EndEvent) > maxEndDate)
                {
                    maxEndDate = DateTime.Parse(item.EndEvent);
                }
            }
            mainItem.StartEvent = minStartDate.ToString();
            mainItem.EndEvent = maxEndDate.ToString();

            //without guide - עם סוג מדריך שונה מהדרכה 
            // צריך לקחת רק את השיבוצים שהזמן שלהם הוא בין השיבוצים של ההרכה
            double totalHouresWithoutGuide = 0;

            if (listWithOutGuide != null && listWithOutGuide.Count > 0)
            {
                foreach (EventItemForExcel item in listWithOutGuide)
                {
                    if (DateTime.Parse(item.StartEvent) > minStartDate && DateTime.Parse(item.StartEvent) < maxEndDate)
                    {
                        totalHouresWithoutGuide += (DateTime.Parse(item.EndEvent) - DateTime.Parse(item.StartEvent)).TotalHours;

                    }

                }
            }
            // mainItem.StartEvent = minStartDate.ToString();
            // mainItem.EndEvent = maxEndDate.ToString();

            var hours = (maxEndDate - minStartDate).TotalHours;
            var minutes = (maxEndDate - minStartDate).TotalMinutes % hours;
            if (totalHouresWithoutGuide != 0)
            {
                hours -= totalHouresWithoutGuide;//מסך שעות ההדרכה צריך להוריד את השעות שהם עם מדריך מסוג אחר 
            }
            ChangeActivityTypeTahelToHadracha(mainItem, hours, minutes);
            return mainItem;
        }


        public void execUnionEventsOld(List<EventItemForExcel> list, List<EventItemForExcel> cascadedList, EventItemForExcel mainItem)
        {
            DateTime minStartDate;
            DateTime maxEndDate;

            List<EventItemForExcel> innerCascadedList = new List<EventItemForExcel>();
            if (cascadedList.Count > 0)
            {
                foreach (EventItemForExcel item in list)
                {
                    innerCascadedList.AddRange(
                        cascadedList.FindAll(e => e.ReservationId == item.ReservationId &&
                                                  e.ServiceProviderId == item.ServiceProviderId &&
                                                  DateTime.ParseExact(e.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() == DateTime.ParseExact(item.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() &&
                                                  e.Location.Value == item.Location.Value &&
                                                  (item.Location.Value == LocationTypeHelper.BahadisCity ||
                                                  (item.Location.Value == LocationTypeHelper.Customer &&
                                                  (item.IsAddressDifferentFromInstitution == true && item.ActivityLocationAddress == e.ActivityLocationAddress ||
                                                  item.IsAddressDifferentFromInstitution == false && item.Institution.AddressInfo.Address == e.Institution.AddressInfo.Address)))));

                    /*List<EventItemForExcel> itemCascadedList = cascadedList.FindAll(e => e.ReservationId == item.ReservationId &&
                                                  e.ServiceProviderId == item.ServiceProviderId &&
                                                  DateTime.ParseExact(e.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() == DateTime.ParseExact(item.Start, "dd/MM/yyyy HH:mm:ss", null).ToShortDateString() &&
                                                  e.Location.Value == item.Location.Value &&
                                                  (item.Location.Value == BahaisCity ||
                                                  (item.Location.Value == Customer &&
                                                  (item.IsAddressDifferentFromInstitution == true && item.ActivityLocationAddress == e.ActivityLocationAddress ||
                                                  item.IsAddressDifferentFromInstitution == false && item.Institution.AddressInfo.Address == e.Institution.AddressInfo.Address))));
                    if(itemCascadedList.Count < 1)
                    {
                        itemCascadedList.Add(item);
                    }
                    innerCascadedList.AddRange(itemCascadedList);*/
                }
            }
            if (innerCascadedList.Count > 0)
            {
                minStartDate = DateTime.ParseExact(innerCascadedList[0].StartEvent, "MM/dd/yyyy HH:mm:ss", null);
                maxEndDate = DateTime.ParseExact(innerCascadedList[0].EndEvent, "MM/dd/yyyy HH:mm:ss", null);
            }
            else
            {
                minStartDate = DateTime.ParseExact(mainItem.StartEvent, "MM/dd/yyyy HH:mm:ss", null);
                maxEndDate = DateTime.ParseExact(mainItem.EndEvent, "MM/dd/yyyy HH:mm:ss", null);
            }
            if (innerCascadedList.Count > 0)
            {
                foreach (EventItemForExcel item in innerCascadedList)
                {
                    if (DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null) < minStartDate)
                    {
                        minStartDate = DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null);
                    }
                    if (DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null) > maxEndDate)
                    {
                        maxEndDate = DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null);
                    }
                }

                mainItem.StartEvent = minStartDate.ToString();
                mainItem.EndEvent = maxEndDate.ToString();
                var hours = (maxEndDate - minStartDate).TotalHours;
                var minutes = (maxEndDate - minStartDate).TotalMinutes % hours;
                if (hours > 6 || (hours == 6 && minutes > 0))
                {
                    mainItem.ActivityType.Value = "הדרכה עד 8 שעות";//change it?
                                                                    //mainItem.ActivityType.Id = "-1";
                }
                else if (hours > 3 || (hours == 3 && minutes > 0))
                {
                    mainItem.ActivityType.Value = "הדרכה עד 6 שעות";//change it?
                                                                    //mainItem.ActivityType.Id = "-1";
                }
                else
                {
                    mainItem.ActivityType.Value = "הדרכה עד 3 שעות";//change it?
                                                                    //mainItem.ActivityType.Id = "-1";
                }
            }

        }
        public EventItemForExcel UnionEventReservationEventsOld(List<EventItemForExcel> listWithGuide, List<EventItemForExcel> listWithOutGuide)
        {
            //with guide - עם סוג מדריך הדרכה
            EventItemForExcel mainItem = listWithGuide[0];
            DateTime minStartDate = DateTime.ParseExact(listWithGuide[0].StartEvent, "MM/dd/yyyy HH:mm:ss", null);
            DateTime maxEndDate = DateTime.ParseExact(listWithGuide[0].EndEvent, "MM/dd/yyyy HH:mm:ss", null);
            foreach (EventItemForExcel item in listWithGuide)
            {
                if (DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null) < minStartDate)
                {
                    minStartDate = DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null);
                }
                if (DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null) > maxEndDate)
                {
                    maxEndDate = DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null);
                }
            }
            mainItem.StartEvent = minStartDate.ToString();
            mainItem.EndEvent = maxEndDate.ToString();

            //without guide - עם סוג מדריך שונה מהדרכה 
            // צריך לקחת רק את השיבוצים שהזמן שלהם הוא בין השיבוצים של ההרכה
            double totalHouresWithoutGuide = 0;

            if (listWithOutGuide != null && listWithOutGuide.Count > 0)
            {
                foreach (EventItemForExcel item in listWithOutGuide)
                {
                    if (DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null) > minStartDate && DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null) < maxEndDate)
                    {
                        totalHouresWithoutGuide += (DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null) - DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null)).TotalHours;

                    }

                }
            }
            // mainItem.StartEvent = minStartDate.ToString();
            // mainItem.EndEvent = maxEndDate.ToString();

            var hours = (maxEndDate - minStartDate).TotalHours;
            var minutes = (maxEndDate - minStartDate).TotalMinutes % hours;
            if (totalHouresWithoutGuide != 0)
            {
                hours -= totalHouresWithoutGuide;//מסך שעות ההדרכה צריך להוריד את השעות שהם עם מדריך מסוג אחר 
            }
            if (hours > 6 || (hours == 6 && minutes > 0))
            {
                mainItem.ActivityType.Value = "הדרכה עד 8 שעות";
            }
            else if (hours > 3 || (hours == 3 && minutes > 0))
            {
                mainItem.ActivityType.Value = "הדרכה עד 6 שעות";
            }
            else
            {
                mainItem.ActivityType.Value = "הדרכה עד 3 שעות";
            }
            return mainItem;
        }

        public void ChangeActivityTypeTahelToHadracha(EventItemForExcel eventItemForExcel, double hours,double minutes)
        {
            if (eventItemForExcel.ActivityType.Value.Contains("בית מדרש"))
            {
                return;
            }

            if (hours > 6 || (hours == 6 && minutes > 0))
            {
                eventItemForExcel.ActivityType.Value = "הדרכה עד 8 שעות";
            }
            else if (hours > 3 || (hours == 3 && minutes > 0))
            {
                eventItemForExcel.ActivityType.Value = "הדרכה עד 6 שעות";
            }
            else
            {
                eventItemForExcel.ActivityType.Value = "הדרכה עד 3 שעות";
            }
        }

        private Tuple<DateTime, DateTime> UnoinEventReservationEvents(List<EventItemForExcel> list)
        {
            DateTime minStartDate = DateTime.ParseExact(list[0].StartEvent, "MM/dd/yyyy HH:mm:ss", null);
            DateTime maxEndDate = DateTime.ParseExact(list[0].EndEvent, "MM/dd/yyyy HH:mm:ss", null);

            foreach (EventItemForExcel item in list)
            {
                if (DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null) < minStartDate)
                {
                    minStartDate = DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null);
                }
                if (DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null) > maxEndDate)
                {
                    maxEndDate = DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null);
                }
            }

            return Tuple.Create<DateTime, DateTime>(minStartDate, maxEndDate);
        }

        private EventItemForExcel UnionEventReservationTimeOnlyOld(List<EventItemForExcel> list)
        {
            EventItemForExcel mainItem = list[0];
            DateTime minStartDate = DateTime.ParseExact(list[0].StartEvent, "MM/dd/yyyy HH:mm:ss", null);
            DateTime maxEndDate = DateTime.ParseExact(list[0].EndEvent, "MM/dd/yyyy HH:mm:ss", null);
            foreach (EventItemForExcel item in list)
            {
                if (DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null) < minStartDate)
                {
                    minStartDate = DateTime.ParseExact(item.StartEvent, "MM/dd/yyyy HH:mm:ss", null);
                }
                if (DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null) > maxEndDate)
                {
                    maxEndDate = DateTime.ParseExact(item.EndEvent, "MM/dd/yyyy HH:mm:ss", null);
                }
            }
            mainItem.StartEvent = minStartDate.ToString();
            mainItem.EndEvent = maxEndDate.ToString();
            return mainItem;
        }
        private EventItemForExcel UnionEventReservationTimeOnly(List<EventItemForExcel> list)
        {
            EventItemForExcel mainItem = list[0];
            DateTime minStartDate = DateTime.Parse(list[0].StartEvent);
            DateTime maxEndDate = DateTime.Parse(list[0].EndEvent);
            foreach (EventItemForExcel item in list)
            {
                if (DateTime.Parse(item.StartEvent) < minStartDate)
                {
                    minStartDate = DateTime.Parse(item.StartEvent);
                }
                if (DateTime.Parse(item.EndEvent) > maxEndDate)
                {
                    maxEndDate = DateTime.Parse(item.EndEvent);
                }
            }
            mainItem.StartEvent = minStartDate.ToString();
            mainItem.EndEvent = maxEndDate.ToString();
            return mainItem;
        }
    }
}
