using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
namespace BasicPlugIn
{
    class WriteToLog
    {
        internal static void WriteLineToLog(string sMsg, int iIndex)
        {
           
            string[] lines = { sMsg };
            try
            {

                System.IO.File.AppendAllLines(@"C:\CrmAdditional\Logs\PlugIn\Log" + DateTime.Now.ToString("yyyyMMdd") + "-" + iIndex + ".txt", lines);
            }
            catch (Exception ex)
            { WriteLineToLog(sMsg, iIndex + 1); }
        }


        internal static void LogToFileInit(Microsoft.Xrm.Sdk.IOrganizationService systemService, string sMsg)
        {
            WriteLineToLog(sMsg,0);
        }

        internal static void LogToFile(Microsoft.Xrm.Sdk.IOrganizationService systemService, string sMsg, string sLogBaseData)
        {
            WriteLineToLog(sLogBaseData+sMsg,0);
        }

        internal static void LogToFile(string sMsg, string sLogBaseData)
        {
            WriteLineToLog(sLogBaseData+sMsg,0);
        }

        internal static void LogToFileError(string sMsg, string sLogBaseData)
        {
            WriteLineToLog("Error " + sLogBaseData + sMsg, 0);
            SendEmailWithError(sLogBaseData, sMsg);
        }
        private static void SendEmailWithError(string sLogBaseData, string sMsg)
        {
            
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential("anat.yadvashem", "Ohad15*%");

            try
            {
                // Create instance of message
                MailMessage message = new MailMessage();

                // Add receiver
                message.To.Add("<REDACTED-EMAIL>");

                // Set sender
                // In this case the same as the username
                message.From = new MailAddress("<REDACTED-EMAIL>");

                string sSubject = sMsg;
                if (sSubject.Length > 100)
                {
                    sSubject = sSubject.Substring(0, 99);
                }
                // Set subject
                message.Subject = sSubject;

                // Set body of message
                message.Body = sLogBaseData + "  " + sMsg;

                // Send the message
                client.Send(message);

                // Clean up
                message = null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not send e-mail. Exception caught: " + e);
            }
        }
    }
}
