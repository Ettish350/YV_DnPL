using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Data.Modals;
using Services.Services.Interfaces;

namespace Services.Services
{
    public class GoogleApiService : IGoogleApiService
    {
        public Decimal GetKmByLatLng(GoogleCordinations googleCordinations)
        {
            var c = googleCordinations;
            HttpClient client = new HttpClient();
            string urlString = ConfigurationManager.AppSettings["googleApiUrl"];
            string googleKey = ConfigurationManager.AppSettings["googleApiKey"];
            //<REDACTED-GOOGLE-API-KEY>
            //string readyUrl = "https://maps.googleapis.com/maps/api/distancematrix/json?avoid=tolls&origins=31.7679014802915,35.1795820802915&destinations=32.9950217,35.09832071970849&key=<REDACTED-GOOGLE-API-KEY>";
            string readyUrl = $"{urlString}avoid=tolls&origins={c.origLat},{c.origLng}&destinations={c.destLat},{c.destLng}&key={googleKey}";
            WebRequest request = WebRequest.Create(readyUrl);
            WebResponse response = request.GetResponse();
            //  var result=.ToString();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                var objText = reader.ReadToEnd();
                GoogleResponse googleResponse = (GoogleResponse)js.Deserialize(objText, typeof(GoogleResponse));

                try
                {
                    if (googleResponse.Rows.Count() > 0
                        && googleResponse.Rows[0].Elements.Count() > 0
                        && googleResponse.Rows[0].Elements[0].Distance != null)
                    {
                        string km = googleResponse.Rows[0].Elements[0].Distance.Text;
                        if (km.Contains("km"))
                        {
                            Decimal kmNumber = Decimal.Parse(km.Replace("km", "").Trim());
                            return kmNumber;
                        }
                        else if(km.Contains("m"))
                        {
                            return -999;
                        }

                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
                //error!!
                return -1;

            }
        }


            public Coordinate GetLatLngByName(string name)
            {
                if (name == null || name == "")
                   return null;
                HttpClient client = new HttpClient();
                //string urlString = ConfigurationManager.AppSettings["googleApiUrl"];
                string urlString = "https://maps.googleapis.com/maps/api/geocode/json";
                string googleKey = ConfigurationManager.AppSettings["googleApiKey"];

                //string readyUrl = "https://maps.googleapis.com/maps/api/distancematrix/json?avoid=tolls&origins=31.7679014802915,35.1795820802915&destinations=32.9950217,35.09832071970849&key=<REDACTED-GOOGLE-API-KEY>";
                string readyUrl = $"{urlString}?address={name}&key={googleKey}";
                WebRequest request = WebRequest.Create(readyUrl);
                WebResponse response = request.GetResponse();
                //  var result=.ToString();
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    var objText = reader.ReadToEnd();
                    GoogleGeocode googleGeocodeResponse = (GoogleGeocode)js.Deserialize(objText, typeof(GoogleGeocode));

                    try
                    {
                        if (googleGeocodeResponse.Results.Count() > 0
                            && googleGeocodeResponse.Results[0].Geometry.Location != null)
                        {
                            return new Coordinate
                            {
                                Id = "-1",
                                Value = name,
                                Lat = googleGeocodeResponse.Results[0].Geometry.Location.Lat.ToString(),
                                Lng = googleGeocodeResponse.Results[0].Geometry.Location.Lng.ToString()
                            };
                        }
                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }
                    //error!!
                    return null;

                }
            }

            
        }
}
