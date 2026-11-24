using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.Win32.SafeHandles;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using regeditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Linq;

namespace Data.Repositories
{
    public class HelperRepository : IHelperRepository
    {
        public HelperRepository()
        {

        }
        IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
        public string SaveDoc(Document document, string number,string prefix,string folder="Invoices")
        {
            Log.Information("in function save doc");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc1252 = Encoding.GetEncoding(1255);
            Log.Information("after encoding");

            string username = configuration.GetSection("app_settings:username").Get<string>();
            string password = configuration.GetSection("app_settings:password").Get<string>();

            string domain = configuration.GetSection("app_settings:domain").Get<string>();
            //using (Impersonator wi = new Impersonator("appadmin", "YADVASHEM", "<REDACTED-PASSWORD>"))
            Log.Information("in RunImpersonated");
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(true);

            //PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(false);

            Log.Information("after new render");
            pdfRenderer.Document = document;


            pdfRenderer.RenderDocument();
            pdfRenderer.PrepareRenderPages();
            Log.Information(" before save location");

            //string save_location = @"\\adm8240\c$\temp\";


            string save_location = configuration.GetSection("app_settings:save_location_"+folder).Get<string>();
            Log.Information("save location is: " + save_location);
            //@"\\172.24.50.200\e$\Files\Invoices\";
            //  string fileName =number + @"_" + DateTime.Now.ToString("mm-ss") + prefix+ ".pdf";
            string fileName = prefix + @"_" + number + ".pdf";

            string FullSavePath = save_location + fileName;


            using (Impersonator wi = new Impersonator(username, domain, password))
            {
                Log.Information("in imp");
                WindowsIdentity.RunImpersonated(wi.Identity.AccessToken, () =>
                {


                    try
                    {
                        Log.Information("going to save in: " + FullSavePath);
                        //do your stuff
                        //  pdfRenderer.PdfDocument.Save(FullSavePath);
#if DEBUG
                        //TEMP FILE NAME
                        // FullSavePath = FullSavePath + Guid.NewGuid() + ".pdf";
                        //Log.Error("in debug mode");

#endif
                        pdfRenderer.PdfDocument.Save(FullSavePath);
                        Log.Information("after save in: " + FullSavePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Information("Exception save in: " + FullSavePath);
                        throw ex;
                    }
                });
            }

            // Log.Error("before process start");

//#if DEBUG
//            var psi = new ProcessStartInfo
//            {
//                FileName = FullSavePath,
//                UseShellExecute = true
//            };
//            Process.Start(psi);
//#endif
            return fileName;
        }
        public string SavePdfDoc(PdfDocument document, string fileName, string folder = "reports")
        {
            Log.Error("in function save doc");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc1252 = Encoding.GetEncoding(1255);
            Log.Error("after encoding");

            string username = configuration.GetSection("app_settings:username").Get<string>();
            string password = configuration.GetSection("app_settings:password").Get<string>();
            string domain = configuration.GetSection("app_settings:domain").Get<string>();
            Log.Error("in RunImpersonated");
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(true);
            Log.Error("after new render");
            string save_location = configuration.GetSection("app_settings:save_location_" + folder).Get<string>();
            Log.Error("save location is: " + save_location);

            string FullSavePath = save_location + fileName;


            using (Impersonator wi = new Impersonator(username, domain, password))
            {
                Log.Error("in imp");
                WindowsIdentity.RunImpersonated(wi.Identity.AccessToken, () =>
                {


                    try
                    {
                        Log.Error("going to save in: " + FullSavePath);
                        document.Save(FullSavePath);
                        Log.Error("after save in: " + FullSavePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception save in: " + FullSavePath);
                        Log.Error("Exception details:: " + ex.Message);

                        throw new Exception("Exception save in: " + Path.GetFileName(FullSavePath));
                    }
                });
            }
            string linkToFilePrefix = configuration.GetSection("app_settings:linkToFilePrefix" + folder).Get<string>();

            return linkToFilePrefix+ fileName;
        }
        public string SaveQRcodeImage(System.Drawing.Bitmap image, string fileName, string folder = "QRcode")
        {
            Log.Error("in function SavePngImage");

            // רישום קידוד
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // קריאה לפרטי ההתחברות
            string username = configuration.GetSection("app_settings:username").Get<string>();
            string password = configuration.GetSection("app_settings:password").Get<string>();
            string domain = configuration.GetSection("app_settings:domain").Get<string>();

            // נתיב שמירת הקובץ
            string save_location = configuration.GetSection("app_settings:save_location_" + folder).Get<string>();
            string fullSavePath = Path.Combine(save_location, fileName);
            Log.Error("Save location is: " + fullSavePath);

            using (Impersonator wi = new Impersonator(username, domain, password))
            {
                Log.Error("in impersonation");
                WindowsIdentity.RunImpersonated(wi.Identity.AccessToken, () =>
                {
                    try
                    {
                        Log.Error("Saving image to: " + fullSavePath);
                        image.Save(fullSavePath, System.Drawing.Imaging.ImageFormat.Png);
                        Log.Error("Image saved successfully: " + fullSavePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception while saving image: " + ex.Message);
                        throw new Exception("Exception saving image to: " + Path.GetFileName(fullSavePath));
                    }
                });
            }

            string linkToFilePrefix = configuration.GetSection("app_settings:linkToFilePrefix" + folder).Get<string>();
            return linkToFilePrefix + fileName;
        }
        public string SaveWordDoc(Xceed.Document.NET.Document document, string FullSavePath)
        {
          
          

            string username = configuration.GetSection("app_settings:username").Get<string>();
            string password = configuration.GetSection("app_settings:password").Get<string>();

            string domain = configuration.GetSection("app_settings:domain").Get<string>();
         
            PdfDocumentRenderer pdfRenderer = new PdfDocumentRenderer(true);

            using (Impersonator wi = new Impersonator(username, domain, password))
            {
                WindowsIdentity.RunImpersonated(wi.Identity.AccessToken, () =>
                {
                    try
                    {
                        document.Save();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception save in: " + FullSavePath);
                        throw ex;
                    }
                });
            }



#if DEBUG
            using (new Impersonator("appadmin", "yadvashem", "<REDACTED-PASSWORD>"))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = FullSavePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
#endif
            return FullSavePath;
        }
        public string GetSaveLocation(string fileName)
        {
            string save_location = configuration.GetSection("app_settings:save_location_schedule").Get<string>();
            string FullSavePath = save_location + fileName;

            return FullSavePath;
        }
        public AttachmentItem AttachDocument(string createdFileName,string extention=".pdf",string folder= "Invoices")
        {
            var attachmentItem = new AttachmentItem();
            attachmentItem.Date = DateTime.Now.ToString();
            attachmentItem.Extention = extention;
            string linkToFilePrefix = configuration.GetSection("app_settings:linkToFilePrefix"+ folder).Get<string>();
            attachmentItem.FullPath = linkToFilePrefix + createdFileName;
            attachmentItem.Name = Path.GetFileName(createdFileName);
            Log.Information("end  CreateDocument service");
            return attachmentItem;
        }
        public string DiscountWithSymbol(string discount, string discountType,string currencyCode)
        {
            if (string.IsNullOrEmpty(discount) || string.IsNullOrEmpty(discountType))
                return string.Empty;
            string formatNumber;
            try
            {


                formatNumber = String.Format("{0:n2}", Convert.ToDecimal(discount));
                if (formatNumber.EndsWith("00"))
                {
                    formatNumber = String.Format("{0:n0}", Convert.ToDecimal(discount));
                }
            }
            catch (Exception)
            {
                formatNumber = discount;
            }
            switch (discountType)
            {
                case "1"://סכום
                case "3":
                    {
                        return WithCurrencySymbol(formatNumber, currencyCode);
                    }
                case "2":
                    {
                        return formatNumber + "%";
                    }
                
                default:
                    return "";
            }
        }

        public string ReverseHeb(string str)
        {
          
            try
            {

                str = str.Replace("(", "~~");
                str = str.Replace(")", "(");
                str = str.Replace("~~", ")");
                string[] arrSplit;
                if (str != null && str != "")
                {
                    arrSplit = Regex.Split(str, "( )|([א-ת]+)");
                    str = "";
                    int arrlenth = arrSplit.Length - 1;
                    for (int i = arrlenth; i >= 0; i--)
                    {
                        if (arrSplit[i] == " ")
                        {
                            str += " ";
                        }
                        else
                        {
                            if (arrSplit[i] != "")
                            {
                                int outInt;
                                //if (int.TryParse(arrSplit[i], out outInt))
                                //{
                                //    str += Convert.ToInt32(arrSplit[i]);
                                //}
                                //devora 30.8.21 fix bug when number has Parenthesis- it should't reverse the number
                                if (int.TryParse(arrSplit[i].Replace(")", "").Replace("(", ""), out outInt))
                                {
                                    string tempStr= arrSplit[i];
                                    tempStr = tempStr.Replace("(", "~~");
                                    tempStr = tempStr.Replace(")", "(");
                                    tempStr = tempStr.Replace("~~", ")");
                                    str += tempStr;
                                   
                                }
                                else
                                {
                                    arrSplit[i] = arrSplit[i].Trim();
                                    if (arrSplit[i].ToCharArray().Length > 0)
                                    {
                                        byte[] codes = System.Text.ASCIIEncoding.Default.GetBytes(arrSplit[i].ToCharArray(), 0, 1);
                                        if (codes[0] > 47 && codes[0] < 58 || codes[0] > 64 && codes[0] < 91 || codes[0] > 96 && codes[0] < 123)//EDIT 3.1 reverse just hebrew words                              
                                        {
                                            str += arrSplit[i].Trim();
                                        }
                                        else
                                        {
                                            str += ReverseInner(arrSplit[i]);
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
                return str;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public string Reverse(string dir, string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            List<string> listOfSTrings = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            var arr = Regex.Split(str, "(?<=[א-ת]+) (?=[^א-ת]+)|(?<=[^א-ת]+) (?=[א-ת]+)");//split the text to hebrew and english
        
            foreach (var item in arr)
            {
                //devora 25.5.21 added second if,because when has special caracters nneds reverse heb too.
                //  if (Regex.IsMatch(item, "[א-ת]") || Regex.IsMatch(item, @"[ `!@#$%^&*()_+\-=\[\]{};':\\|,.<>\/? ~]")) //if contains hebrew letters
                // devora 9.12.21 , if this is english it mixes up all the text
                if (Regex.IsMatch(item, "[א-ת]")) //if contains hebrew letters
                {
                    listOfSTrings.Add(ReverseHeb(item));
                }
                else
                {
                    listOfSTrings.Add(item);
                }
                listOfSTrings.Add(" ");
            }
            if (dir == "rtl")
            {
                for (int i = listOfSTrings.Count - 1; i >= 0; i--)
                {
                    stringBuilder.Append(listOfSTrings[i]);
                }
            }
            else {
                for (int i = 0; i < listOfSTrings.Count; i++)
                {
                    stringBuilder.Append(listOfSTrings[i]);
                }
            }
            return stringBuilder.ToString();
          

        }
        public string ReverseForWord( string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            List<string> listOfSTrings = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            var arr = Regex.Split(str, "(?<=[א-ת]+) (?=[^א-ת]+)|(?<=[^א-ת]+) (?=[א-ת]+)");//split the text to hebrew and english

            foreach (var item in arr)
            {
                
                
            listOfSTrings.Add(item);
                
            listOfSTrings.Add(" ");
            }
            
                for (int i = listOfSTrings.Count - 1; i >= 0; i--)
                {
                    stringBuilder.Append(listOfSTrings[i]);
                }
            
            return stringBuilder.ToString();


        }

        private static string ReverseInner(string s)
        {

          
            char[] charArray = s.ToCharArray();
          
            Array.Reverse(charArray);

            return new string(charArray);
        }

        public string WithCurrencySymbol(string number,string currencyCode)
        {
            string symbol = "₪";
            if(!string.IsNullOrEmpty(currencyCode))
            {
                symbol = GetSymbolByCurrencyCode(currencyCode);
            }
            string formatNumber;
            try
            {
                formatNumber = String.Format("{0:n2}", Convert.ToDecimal(number));
                if (formatNumber.EndsWith("00"))
                {
                    formatNumber = String.Format("{0:n0}", Convert.ToDecimal(number));
                }
            }
            catch (Exception)
            {
                formatNumber = number;
            }
            return symbol +" "+ formatNumber;
        }

        private string GetSymbolByCurrencyCode(string currencyCode)
        {
            string symbol = "₪";
            switch (currencyCode)
            {
                case "USD":
                case "AUD":
                case "CAD":
                    return "$";
                case "EUR":
                    return "€";
                case "GBP":
                    return "£";
                case "PLN":
                    return "zł";

                default:
                    return symbol;
            }
        }

        public void AddToTextSplitToLines(string dir, Paragraph paragraph, string stringToSplit, int maximumLineLength, Font font = null)
        {
            string withBreak = Regex.Replace(stringToSplit, @"(.{1," + maximumLineLength + @"})(?:\s|$)", "$1\n");
            string[] lines = withBreak.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {

                if (font != null)
                {
                    paragraph.AddFormattedText(Reverse(dir, lines[i]), font);
                }
                else
                {
                    paragraph.AddFormattedText(Reverse(dir, lines[i]));
                }
                if (i < lines.Length - 1)//don't add line breake to last line
                {
                    paragraph.AddLineBreak();
                }
            }
           

        }

        public void AddYVOrderNumTextFrame(HeaderFooter header, string orderNum, string direction)
        {
            var OrderNumFrame = new TextFrame();
            OrderNumFrame = header.AddTextFrame();
            OrderNumFrame.Width = 150;
            OrderNumFrame.RelativeHorizontal = RelativeHorizontal.Margin;


            OrderNumFrame.RelativeVertical = RelativeVertical.Page;
            switch (direction)
            {
                case "right":
                    OrderNumFrame.Left = ShapePosition.Left;
                    OrderNumFrame.WrapFormat.DistanceLeft = "14.8cm";
                    OrderNumFrame.WrapFormat.DistanceTop = "4.63cm";
                    break;
                case "left":
                    OrderNumFrame.Left = ShapePosition.Left;
                    OrderNumFrame.WrapFormat.DistanceLeft = "6cm";
                    OrderNumFrame.WrapFormat.DistanceTop = "4.75cm";
                    break;
                default:
                    OrderNumFrame.Left = ShapePosition.Left;
                    OrderNumFrame.WrapFormat.DistanceLeft = "14.8cm";
                    break;
            }


            var paragraph = OrderNumFrame.AddParagraph();
            Font font = new Font();
            font.Size = 12;
            font.Color = Color.Parse("#FFFFFF");
            font.Bold = true;
            paragraph.AddFormattedText(orderNum, font);
        }
        public Table AddActivityDayTable(Section section)
        {
            section.AddParagraph().Format.SpaceBefore = 2;//space before table
            var table = section.AddTable();
            table.TopPadding = 3;
            table.BottomPadding = 3;
            table.Rows.LeftIndent = 33;
            table.LeftPadding = 0;
            table.RightPadding = 0;
            table.RightPadding = 10;
            Column column;
            for (int i = 1; i <= 6; i++)
            {
                column = table.AddColumn();
                if (i == 6)
                {
                    column.Width = 130;
                }
                else
                {
                    column.Width = 80;
                }
            }

            var row = table.AddRow();

            row.HeadingFormat = true;
            row.Height = "0.4cm";
            row.Cells[0].Format.SpaceBefore = 3;
            row.Cells[0].Format.SpaceAfter = 3;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].MergeRight = 5;


            row.Format.Alignment = ParagraphAlignment.Right;
            row.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
            return table;

        }

        public void AddYVRows(Table table, int rowsCount, ParagraphAlignment paragraphAlignment)
        {
            for (int i = 1; i <= rowsCount; i++)
            {
                var row = table.AddRow();
                row.Cells[0].Format.Alignment = paragraphAlignment;
                row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                row.Cells[0].Format.SpaceBefore = 0;
                row.Cells[0].Format.SpaceAfter = 0;
                row.Height = "0.4cm";
                row.BottomPadding = 4;
            }
            table.SetEdge(0, 0, 1, rowsCount + 1, Edge.Box, BorderStyle.Single, 0.5, YVFonts.borderColor);


        }

        public Table AddYVTable(Section section, ParagraphAlignment paragraphAlignment)
        {
            var table = section.AddTable();
            table.BottomPadding = 4;
            table.Rows.LeftIndent = 33;
            table.LeftPadding = 0;
            table.RightPadding = 0;
            // table.Style = "Table";
            table.RightPadding = 10;

            var column = table.AddColumn();
            column.Width = 530;
            var row = table.AddRow();
            row.HeadingFormat = true;
            // row.Height = "0.2cm";
            row.Cells[0].Format.SpaceBefore = 3;
            row.Cells[0].Format.SpaceAfter = 2;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;

            row.Format.Alignment = paragraphAlignment;
            row.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
            return table;
        }

        public string WithCurrencyCodeIfNeed(string currencyCode)
        {
           if(!string.IsNullOrEmpty(currencyCode) && currencyCode!="ILS")
            {
                return " " + currencyCode;
            }
            return "";
        }
    }
}
