using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class PaymentService : IPaymentService 
    {
       private readonly IPaymentRepository _paymentRepository;
       private readonly ICrmRepository  _crmRepository;
       private readonly IConfiguration _config;
       private readonly INewReservationService _newReservationService;
       private readonly IHelperService _helperService;
       private readonly string pelecardUser ;
       private readonly string pelecardPassword ;
       private readonly string pelecardTerminal;

        private readonly string iframePelecardUser    ;
        private readonly string iframePelecardPassword;
        private readonly string iframePelecardTerminal;

        public PaymentService(IPaymentRepository paymentRepository,
            ICrmRepository crmRepository
            , IConfiguration config ,INewReservationService newReservationService, IHelperService helperService)
        {
            _paymentRepository = paymentRepository;
            _crmRepository = crmRepository;
            _config = config;
            _newReservationService = newReservationService;
            _helperService = helperService;
            pelecardUser = _config.GetValue<string>("pelecardUser")!;
            pelecardPassword= _config.GetValue<string>("pelecardPassword")!;
            pelecardTerminal= _config.GetValue<string>("pelecardTerminal")!;

            iframePelecardUser = _config.GetValue<string>("iframePelecardUser")!;
            iframePelecardPassword = _config.GetValue<string>("iframePelecardPassword")!;
            iframePelecardTerminal = _config.GetValue<string>("iframePelecardTerminal")!;

        }

        public async Task<PelecardIframeResponse> GetPaymentUrl(double total, string currency, string paramX, string lang,string guid,string type,string tourCode, string? couponGuid=null)
        {
            Log.Information($"GetPaymentUrl totoal :{total}, currency :{currency}, paramX :{paramX}, lang: {lang}, couponGuid: {couponGuid}");
            /* string cssLink = "https://forms.yadvashem.org/sites/all/modules/yv_pelecard/css/yv_pelecard_iframe_rtl.css";
             if (lang != "he")
             {
                 cssLink = "https://forms.yadvashem.org/sites/all/modules/yv_pelecard/css/yv_pelecard_iframe.css";
             }
            */

            string goodUrl = _config.GetValue<string>("clientSideGoodUrl").Replace("{lang}", lang)+ guid+"/"+ type;
            string ErrorURL = _config.GetValue<string>("clientSideErrorURL").Replace("{lang}", lang).Replace("{tourCode}",tourCode)!;
            string serverSideUrl = _config.GetValue<string>("serverSideUrl")+guid+"&type="+type+"&total="+ total+ "&couponGuid=" + couponGuid;

            PelecardIframeRequest payment = new PelecardIframeRequest();
            payment.paramX = paramX;
            payment.user = iframePelecardUser;
            payment.password = iframePelecardPassword;
            payment.terminal = iframePelecardTerminal;
            payment.total = (total * 100);
            payment.language = lang;
            //payment.cssURL = cssLink;
            payment.goodURL = goodUrl;
            payment.ErrorURL = ErrorURL;
            payment.serverSideGoodFeedbackURL = serverSideUrl;
            //payment.CustomerIdField = "hide";
            payment.FeedbackOnTop = true;

            var response = await _paymentRepository.GetPaymentUrl(payment);

            return response;//.URL;
        }

        public async Task<PelecardResponse> Pay(BasicReservationItem rsv, string kiosk)
        {
            var rsvNum = rsv is Reservation ? ((Reservation)rsv).ReservationNumber : ((EventRegistration)rsv).EventRegistrationNumber;
            Log.Information($"Pay start, for rsv number:{rsvNum}, guid:{rsvNum}, kiosk:{kiosk}");
            PelecardRequest payment = new PelecardRequest();
            payment.shopNumber = _config.GetValue<string>("shopNumber_" + kiosk)!;
            payment.paramX = rsv.PaymentDetails.ExternalReference;
            payment.user = pelecardUser;
            payment.password = pelecardPassword; 
            payment.terminalNumber = pelecardTerminal;
            payment.total = (rsv.AdditionalTourDetails.totalPayment * 100 )+ (rsv.PaymentDetails.DonationItem.TotalDonationSum*100);
            payment.FeedbackOnTop = true;
            var response = await _paymentRepository.Pay(payment, kiosk);

            //for developing - result success - not use in prod!!!
            //PelecardResponse response = new PelecardResponse();
            //if (kiosk != "null")
            //{
            //    response = await _paymentRepository.Pay(payment, kiosk);
            //}
            //if (_config.GetValue<string>("mode") == "test")
            //{
            //    var res = "{\"StatusCode\":\"000\",\"ErrorMessage\":\"operation success\",\"ResultData\":{\"PelecardTransactionId\":\"2061820931\",\"VoucherId\":\"37-022-001\",\"ShvaResult\":\"000\",\"ShvaResultEmv\":\"000\",\"ShvaResultEmvMessage\":\"העסקה אושרה\",\"ShvaFileNumber\":\"37\",\"StationNumber\":\"22\",\"Reciept\":\"1\",\"JParam\":\"4\",\"CreditCardNumber\":\"552350******5282\",\"CreditCardExpDate\":\"0127\",\"CreditCardCompanyClearer\":\"1\",\"CreditCardCompanyIssuer\":\"0\",\"CreditCardStarsDiscountTotal\":\"0\",\"CreditType\":\"1\",\"CreditCardAbroadCard\":\"0\",\"DebitType\":\"1\",\"DebitCode\":\"5\",\"DebitTotal\":\"3000\",\"DebitApproveNumber\":\"0096012\",\"DebitCurrency\":\"1\",\"TotalPayments\":\"1\",\"FirstPaymentTotal\":\"0\",\"FixedPaymentTotal\":\"0\",\"AdditionalDetailsParamX\":\"rsv-0183177\",\"shvaOutput\":\"0000000552350******528211000401270000003000        0000000000110105001009601200000000000000000037022001     CBA Credit0        rsv-0183177\",\"CardHebName\":\"CBA Credit\",\"CreditCardBrand\":\"1\",\"ApprovedBy\":\"1\",\"CallReason\":\"0\",\"Token\":\"2143731264\",\"Tz\":\"\",\"Uid\":\"24080110024257922402224\",\"eci\":\"\",\"xid\":\"\",\"cavv\":\"\",\"Terminal\":\"\"}}";
            //    response = JsonConvert.DeserializeObject<PelecardResponse>(res);
            //    if (response != null)
            //    {
            //        response.ResultData.DebitTotal = payment.total.ToString();
            //        //response.StatusCode = "013";//test fail
            //    }


            //}

            if (response!=null && response.StatusCode == "000")//success
            {
                try
                {
                    Log.Information("Payment successfully completed in pelecard for rsv:" + rsvNum);
                    var productList = rsv is EventRegistration ? ((EventRegistration)rsv).TourType.ProductList : _newReservationService.GetProductsByTourType("103", DateTime.ParseExact(rsv.BasicTourDetails.Date + rsv.BasicTourDetails.StartTime, "dd/MM/yyyyHH:mm", null).ToString("yyyy-MM-dd"), _config.GetValue<string>("YadVashemLocationGuid")!);
                    //if there is a payment in the rsv:
                    if (rsv.AdditionalTourDetails.IsWithAudioGuide || rsv is EventRegistration)
                    {
                        var paymentGuid = _crmRepository.CreatePaymentInCRM(response, rsv, productList, kiosk);
                        _newReservationService.UpdateReservationAfterPayment(rsv, paymentGuid);
                    }

                    //if there is a donation in the rsv:
                    if (rsv.PaymentDetails.DonationItem.TotalDonationSum > 0)
                    {
                        var donationGuid = _crmRepository.CreateDonationInCRM(response, rsv, kiosk);
                    }

                }
                catch (Exception ex)
                {
                    Log.Error("KIOSK CreatePaymentInCRM error, ex: " + ex.Message);
                    //send mail that CRM payment failed! important!
                    MailInfo mailInfo = new MailInfo();
                    mailInfo.Message = $"CreatePaymentInCRM error: {ex.Message}.<br>" +
                                        $"Reservation num: {rsvNum},<br>" +
                                        $"Reservation guid:{rsv.Guid}.<br>" +
                                        $"Payment in pelecard was successfully finished, but there was an error while creating payment in crm!<br><br>" +
                                        $"response from pelecard is: {JsonConvert.SerializeObject(response)}";
                    mailInfo.Subject = $"CreatePaymentInCRM error for reservation: {rsvNum}";
                    mailInfo.To = new List<string>() { "yv.dev.logs@yadvashem.org.il" };
                    mailInfo.Cc = new List<string>() { "esther.fuxbrumer@yadvashem.org.il", "devora.weyl@yadvashem.org.il", "feigy.cirota@yadvashem.org.il" };
                    mailInfo.Bcc = new List<string>();
                    mailInfo.From = "no-reply@yadvashem.org";
                    mailInfo.Files = new List<string>();
                    mailInfo.Direction = Direction.LTR;
                    Log.Information($"Before send email error: {@mailInfo}.");
                    _helperService.CreateRequest("http://reservations.yadvashem/sendmail/mail", "POST", JsonConvert.SerializeObject(mailInfo));
                    Log.Information("After send email error");
                }

               
            }
            return response;
        }

        
    }
}
