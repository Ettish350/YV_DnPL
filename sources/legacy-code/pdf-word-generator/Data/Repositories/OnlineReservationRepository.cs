using Data.Models;
using Data.Repositories.Interfaces;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

using ZXing;
using System.Globalization;
using ZXing.Windows.Compatibility;
using ZXing.QrCode;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using System.Xml.Linq;
using System.Diagnostics;
using iTextSharp.text.pdf;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using Microsoft.OpenApi.Writers;


namespace Data.Repositories
{
    public class OnlineReservationRepository : IOnlineReservationRepository
    {
        private IHelperRepository helperRepository;

        public OnlineReservationRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;
        }
        public string GenerateQRCode(List<QRCodeItem> qRcodeDetails)
        {
            try
            {
                Log.Error("start GenerateQRCode details: " + @qRcodeDetails);
                var json = JsonConvert.SerializeObject(qRcodeDetails);
                // Create a QR code writer
                BarcodeWriter writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = 200,
                        Width = 200
                    }
                };

                // Encode the JSON string as a QR code
                System.Drawing.Bitmap qrCode = writer.Write(json);

                var urlToSave = qRcodeDetails[0].Guid + "_qrcode.png";
                Log.Error("go to save doc");
                string file_path = helperRepository.SaveQRcodeImage(qrCode, urlToSave);
                Log.Error("after save doc");
                // Save the QR code as an image file 
                Log.Error($"QRCODE URLTOSAVE: {urlToSave}");

                var attachment = helperRepository.AttachDocument(urlToSave, ".png", "QRcode");

                Log.Error("end GenerateQRCode url is: " + attachment.FullPath);

                return attachment.FullPath;

            }
            catch (Exception ex)
            {


                Log.Error("Error!!!faild to generateQRCode: " + ex.Message);

                return null;

            }
            throw new NotImplementedException();
        }
        public string CreateDocument(Document document, OnlineReservationTicket data)
        {
            try
            {

                Log.Information("in OnlineReservation repository create document.  ");
                document.Styles[StyleNames.Normal].Font.Name = "Arial";
                document.Info.Title = "Yad Vashem OnlineReservation";
                document.Info.Subject = "Yad Vashem OnlineReservation";
                document.Info.Author = "Yad Vashem";
                document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(1);
                document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(1);
                document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(1);
                document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(1);
                switch (data.Lang)
                {
                    case "עברית":
                        {
                            Log.Error("lang heb");
                            CreatePageAndFillContentHeb(document, data);
                            break;
                        }
                    case "אנגלית":
                        {
                            Log.Error("lang eng");
                            CreatePageAndFillContentEng(document, data);
                            break;
                        }
                    default:
                        {
                            Log.Error("no lang ");
                            CreatePageAndFillContentHeb(document, data);
                            break;
                        }

                }
                Log.Error("go to save doc");
                string file_path = helperRepository.SaveDoc(document, data.ReservationGuid, "Ticket", "ticket");

                Log.Error("after save doc");
                return file_path;

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "exception in invoice repository create document");
                throw ex;
            }
        }
        //private void CreatePageAndFillContentHeb(Document document, OnlineReservationTicket data)
        //{


        //    ////header
        //    Section section = document.AddSection();
        //    section.PageSetup.RightMargin = Unit.FromCentimeter(1); // שוליים ימניים קטנים

        //    // הגדרת טבלה כללית
        //    Table table;
        //    Row Row;
        //    Paragraph paragraph = section.Headers.Primary.AddParagraph();
        //    paragraph.Format.Alignment = ParagraphAlignment.Right;
        //    Image logo = paragraph.AddImage("Images/commemoration_header_heb.jpg");
        //    logo.Width = "3cm";

        //    logo.LockAspectRatio = true;
        //    table = section.AddTable();
        //    //table.RightPadding = 0;
        //    //table.LeftPadding = 10;
        //    table.Format.Alignment = ParagraphAlignment.Right;
        //    table.AddColumn("5cm");
        //    table.AddColumn("3cm");
        //    //table.AddRow();//שורת רווח
        //    Row = table.AddRow();

        //    Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "  תאריך רכישה: ")).Format.Alignment = ParagraphAlignment.Right;

        //    if (!string.IsNullOrEmpty(data.PaymentMethod))
        //    {
        //        Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", "אמצעי תשלום:")).Format.Alignment = ParagraphAlignment.Right;
        //    }
        //    Row = table.AddRow();
        //    Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", data.PurchaseDate)).Format.Alignment = ParagraphAlignment.Right;
        //    Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", data.PaymentMethod)).Format.Alignment = ParagraphAlignment.Right;



        //    paragraph = section.AddParagraph();
        //    paragraph.AddLineBreak();
        //    paragraph.AddText(helperRepository.Reverse("rtl", "הכרטיס שלך " + data.FullName));
        //    paragraph.Format.Alignment = ParagraphAlignment.Center;
        //    paragraph.Format.Font.Bold = true;
        //    paragraph.Format.Font.Size = 12;
        //    Paragraph lineParagraph = section.AddParagraph();
        //    lineParagraph.Format.Borders.Top.Width = Unit.FromCentimeter(0.01); // עובי הקו: 3 ס"מ
        //    lineParagraph.Format.Borders.Top.Color = Colors.Black;          // צבע הקו: שחור
        //    lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);      // מרחק בין הקו לטקסט
        //    paragraph.AddLineBreak();




        //    paragraph = section.AddParagraph();
        //    paragraph.AddLineBreak();


        //    //lineParagraph = section.AddParagraph();
        //    //lineParagraph.Format.Borders.Top.Width = Unit.FromCentimeter(0.08); 
        //    //lineParagraph.Format.Borders.Top.Color = Colors.Black;         
        //    //lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);   


        //    Table mainTable = section.AddTable();
        //    mainTable.AddColumn("4cm"); // עמודה לתמונה
        //    mainTable.AddColumn("10cm");  // עמודה לטבלה
        //    Row mainRow = mainTable.AddRow();
        //    //טבלה לתוכן
        //    table = new Table();
        //    table.Format.Alignment = ParagraphAlignment.Right;
        //    // הוספת עמודות
        //    table.AddColumn("3cm");
        //    table.AddColumn("1.5cm");
        //    table.AddColumn("1.5cm");
        //    table.AddColumn("1.9cm");
        //    table.AddColumn("3cm");
        //    table.AddColumn("1.9cm");

        //    //כותרת
        //    Row header = table.AddRow();
        //    header.Cells[0].MergeRight = table.Columns.Count - 1; // מיזוג כל התאים בשורה הראשונה
        //    Paragraph headerParagraph = header.Cells[0].AddParagraph(helperRepository.Reverse("rtl", data.ActivityName));
        //    headerParagraph.Format.Font.Size = 16; // גודל גופן
        //    headerParagraph.Format.Font.Bold = true; // גופן מודגש
        //    //headerParagraph.Format.Alignment = ParagraphAlignment.Right; // יישור לימין
        //    headerParagraph.AddLineBreak();
        //    headerParagraph.AddLineBreak();

        //    // שורה ראשונה
        //    Row = table.AddRow();
        //    Row.Format.Alignment = ParagraphAlignment.Right;
        //    Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", "תאריך:")).Format.Font.Bold = true;
        //    Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", data.VisitDate));
        //    Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "שעה:")).Format.Font.Bold = true;
        //    Row.Cells[2].AddParagraph(helperRepository.Reverse("rtl", data.VisitStartTime));
        //    if (!string.IsNullOrEmpty(data.hall))
        //    {
        //        Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "אולם:")).Format.Font.Bold = true;
        //        Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", data.hall));
        //        Row = table.AddRow();
        //    }
        //    //סוג כרטיסים
        //    Row = table.AddRow();
        //    Row.Format.Alignment = ParagraphAlignment.Right;
        //    Row.Cells[4].MergeRight = 1;
        //    if (data.visitorsDetails.Count>0&&!string.IsNullOrEmpty(data.visitorsDetails[0].Name))
        //    {
        //        Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "סוג וכמות כרטיסים:")).Format.Font.Bold = true;
        //        Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
        //        foreach (var visitorType in data.visitorsDetails)
        //        {
        //            Row = table.AddRow();
        //            Row.Format.Alignment = ParagraphAlignment.Right;
        //            Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", visitorType.Name + visitorType.Amount + "X"));
        //            Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));

        //        }
        //    }
        //    else
        //    {
        //        Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "כמות כרטיסים:")).Format.Font.Bold = true;
        //        //Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
        //        //foreach (var visitorType in data.visitorsDetails)
        //        //{
        //            Row = table.AddRow();
        //            Row.Format.Alignment = ParagraphAlignment.Right;
        //            Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", data.visitorsDetails[0].Amount));
        //            //Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));

        //        //}
        //    }



        //    mainRow.Cells[1].Elements.Add(table);

        //    //ברקוד
        //    Image image = mainRow.Cells[0].AddImage(data.QRcodeUrl);
        //    image.LockAspectRatio = true;
        //    image.Width = "5cm"; // גודל התמונה
        //    image.Height = "5cm"; // גודל התמונה

        //    Row = table.AddRow();

        //    // קו אופקי אחרון
        //    lineParagraph = section.AddParagraph();
        //    lineParagraph.Format.Borders.Top.Width = Unit.FromCentimeter(0.01); // עובי הקו: 3 ס"מ
        //    lineParagraph.Format.Borders.Top.Color = Colors.Black;          // צבע הקו: שחור
        //    lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);
        //    //עבור כל אחת מהפעיליות בהזמנה
        //    if (data.activitiesDetails != null && data.activitiesDetails.Count > 0) {
        //        data.activitiesDetails.ForEach(activity =>
        //        {
        //            mainTable = section.AddTable();
        //            mainTable.AddColumn("4cm"); // עמודה לתמונה
        //            mainTable.AddColumn("10cm");  // עמודה לטבלה
        //            mainRow = mainTable.AddRow();
        //            //טבלה לתוכן
        //            table = new Table();
        //            table.Format.Alignment = ParagraphAlignment.Right;
        //            // הוספת עמודות
        //            table.AddColumn("3cm");
        //            table.AddColumn("1.5cm");
        //            table.AddColumn("1.5cm");
        //            table.AddColumn("1.9cm");
        //            table.AddColumn("3cm");
        //            table.AddColumn("1.9cm");

        //            //כותרת
        //            header = table.AddRow();
        //            header.Cells[0].MergeRight = table.Columns.Count - 1; // מיזוג כל התאים בשורה הראשונה
        //            headerParagraph = header.Cells[0].AddParagraph(helperRepository.Reverse("rtl", activity.ActivityName));
        //            headerParagraph.Format.Font.Size = 16; // גודל גופן
        //            headerParagraph.Format.Font.Bold = true; // גופן מודגש
        //                                                     //headerParagraph.Format.Alignment = ParagraphAlignment.Right; // יישור לימין
        //            headerParagraph.AddLineBreak();
        //            headerParagraph.AddLineBreak();

        //            // שורה ראשונה
        //            Row = table.AddRow();
        //            Row.Format.Alignment = ParagraphAlignment.Right;
        //            Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", "תאריך:")).Format.Font.Bold = true;
        //            Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", activity.VisitDate));
        //            Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "שעה:")).Format.Font.Bold = true;
        //            Row.Cells[2].AddParagraph(helperRepository.Reverse("rtl", activity.VisitStartTime));
        //            if (!string.IsNullOrEmpty(activity.hall))
        //            {
        //                Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "אולם:")).Format.Font.Bold = true;
        //                Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", activity.hall));
        //                Row = table.AddRow();
        //            }
        //            //סוג וכמות כרטיסים
        //            Row = table.AddRow();
        //            Row.Format.Alignment = ParagraphAlignment.Right;
        //            Row.Cells[4].MergeRight = 1;
        //            if (activity.visitorsDetails.Count > 0 && !string.IsNullOrEmpty(activity.visitorsDetails[0].Name))
        //            {
        //                Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "סוג וכמות כרטיסים:")).Format.Font.Bold = true;
        //                Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
        //                foreach (var visitorType in activity.visitorsDetails)
        //                {
        //                    Row = table.AddRow();
        //                    Row.Format.Alignment = ParagraphAlignment.Right;
        //                    Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", visitorType.Name + visitorType.Amount + "X"));
        //                    Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));

        //                }
        //            }
        //            else
        //            {
        //                Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "כמות כרטיסים:")).Format.Font.Bold = true;
        //                //Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
        //                //foreach (var visitorType in data.visitorsDetails)
        //                //{
        //                Row = table.AddRow();
        //                Row.Format.Alignment = ParagraphAlignment.Right;
        //                Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", activity.visitorsDetails[0].Amount));
        //                //Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));

        //                //}
        //            }



        //            mainRow.Cells[1].Elements.Add(table);

        //            ////ברקוד
        //            //Image image = mainRow.Cells[0].AddImage(data.QRcodeUrl);
        //            //image.LockAspectRatio = true;
        //            //image.Width = "2.5cm"; // גודל התמונה
        //            Row = table.AddRow();

        //            // קו אופקי אחרון
        //            lineParagraph = section.AddParagraph();
        //            lineParagraph.Format.Borders.Top.Width = Unit.FromCentimeter(0.01); // עובי הקו: 3 ס"מ
        //            lineParagraph.Format.Borders.Top.Color = Colors.Black;          // צבע הקו: שחור
        //            lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);
        //        }
        //        );
        //    }



        // }
        private void CreatePageAndFillContentHeb(Document document, OnlineReservationTicket data)
        {
            ////header
            Section section = document.AddSection();
            section.PageSetup.RightMargin = Unit.FromPoint(28.35); // שוליים ימניים קטנים (1 ס"מ = 28.35 נקודות)

            // הגדרת טבלה כללית
            Table table;
            Row Row;
            Paragraph paragraph = section.Headers.Primary.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            Image logo = paragraph.AddImage("Images/commemoration_header_heb.jpg");
            logo.Width = Unit.FromPoint(85); // 3 ס"מ = 85 נקודות

            logo.LockAspectRatio = true;
            table = section.AddTable();
            table.Format.Alignment = ParagraphAlignment.Right;
            table.AddColumn(Unit.FromPoint(142.75)); // 5 ס"מ = 142.75 נקודות
            table.AddColumn(Unit.FromPoint(85)); // 3 ס"מ = 85 נקודות

            Row = table.AddRow();
            Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "  תאריך רכישה: ")).Format.Alignment = ParagraphAlignment.Right;

            if (!string.IsNullOrEmpty(data.PaymentMethod))
            {
                Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", "אמצעי תשלום:")).Format.Alignment = ParagraphAlignment.Right;
            }
            Row = table.AddRow();
            Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", data.PurchaseDate)).Format.Alignment = ParagraphAlignment.Right;
            Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", data.PaymentMethod)).Format.Alignment = ParagraphAlignment.Right;

            paragraph = section.AddParagraph();
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "הכרטיס שלך " + data.FullName));
            paragraph.AddTab();
            paragraph.AddText(helperRepository.Reverse("rtl", "מספר הזמנה: " + data.ReservationNumber));

            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Size = 12;
            Paragraph lineParagraph = section.AddParagraph();
            lineParagraph.Format.Borders.Top.Width = Unit.FromPoint(2.85); // 0.1 ס"מ = 2.85 נקודות
            lineParagraph.Format.Borders.Top.Color = Colors.Black;
            lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);
            paragraph.AddLineBreak();

            paragraph = section.AddParagraph();
            paragraph.AddLineBreak();

            Table mainTable = section.AddTable();
            mainTable.AddColumn(Unit.FromPoint(113.4)); // 4 ס"מ = 113.4 נקודות
            mainTable.AddColumn(Unit.FromPoint(283.5)); // 10 ס"מ = 283.5 נקודות
            Row mainRow = mainTable.AddRow();

            table = new Table();
            table.Format.Alignment = ParagraphAlignment.Right;
            table.AddColumn(Unit.FromPoint(85.05)); // 3 ס"מ = 85.05 נקודות
            table.AddColumn(Unit.FromPoint(42.75)); // 1.5 ס"מ = 42.75 נקודות
            table.AddColumn(Unit.FromPoint(42.75)); // 1.5 ס"מ = 42.75 נקודות
            table.AddColumn(Unit.FromPoint(53.65)); // 1.9 ס"מ = 53.65 נקודות
            table.AddColumn(Unit.FromPoint(85.05)); // 3 ס"מ = 85.05 נקודות
            table.AddColumn(Unit.FromPoint(60)); // 1.9 ס"מ = 53.65 נקודות

            //כותרת
            Row header = table.AddRow();
            header.Cells[0].MergeRight = table.Columns.Count - 1;
            Paragraph headerParagraph = header.Cells[0].AddParagraph(helperRepository.Reverse("rtl", data.ActivityName));
            headerParagraph.Format.Font.Size = 16;
            headerParagraph.Format.Font.Bold = true;
            headerParagraph.AddLineBreak();

            if (data.TourNote != null)
            {
                Row note = table.AddRow();
                note.Cells[0].MergeRight = table.Columns.Count - 1;
                Paragraph headerParagraphNote = note.Cells[0].AddParagraph(helperRepository.Reverse("rtl", data.TourNote));
                headerParagraphNote.AddLineBreak();
                headerParagraphNote.AddLineBreak();
            }
            else
            {
                headerParagraph.AddLineBreak();
            }

            // שורה ראשונה
            Row = table.AddRow();
            Row.Format.Alignment = ParagraphAlignment.Right;
            Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", "תאריך:")).Format.Font.Bold = true;
            Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", data.VisitDate));
            Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "שעה:")).Format.Font.Bold = true;
            Row.Cells[2].AddParagraph(helperRepository.Reverse("rtl", data.VisitStartTime));
            if (!string.IsNullOrEmpty(data.hall))
            {
                Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "אולם:")).Format.Font.Bold = true;
                Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", data.hall));
                Row = table.AddRow();
            }

            Row = table.AddRow();
            Row = table.AddRow();

            Row.Format.Alignment = ParagraphAlignment.Right;
            Row.Cells[4].MergeRight = 1;
            if (data.visitorsDetails.Count > 0 && !string.IsNullOrEmpty(data.visitorsDetails[0].Name))
            {
                Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "סוג וכמות כרטיסים:")).Format.Font.Bold = true;
                Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
                foreach (var visitorType in data.visitorsDetails)
                {
                    Row = table.AddRow();
                    Row = table.AddRow();

                    Row.Format.Alignment = ParagraphAlignment.Right;
                    Row.Cells[4].MergeRight = 1;
                    Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", visitorType.Name + visitorType.Amount + "X"));
                    Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));
                }
            }

            else
            {
                Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "כמות כרטיסים:")).Format.Font.Bold = true;
                Row = table.AddRow();
                Row = table.AddRow();

                Row.Format.Alignment = ParagraphAlignment.Right;
                Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", data.visitorsDetails[0].Amount));
            }
            if (data.products.Count > 0 && !string.IsNullOrEmpty(data.products[0].Name))
            {
                //Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "סוג וכמות כרטיסים:")).Format.Font.Bold = true;
                //Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
                foreach (var visitorType in data.products)
                {
                    Row = table.AddRow();
                    Row = table.AddRow();
                    Row.Cells[4].MergeRight = 1;
                    Row.Format.Alignment = ParagraphAlignment.Right;
                    Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", visitorType.Name + visitorType.Amount + "X"));
                    Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));
                }
            }

            mainRow.Cells[1].Elements.Add(table);

            // ברקוד
            Image image = mainRow.Cells[0].AddImage(data.QRcodeUrl);
            image.LockAspectRatio = true;
            image.Width = Unit.FromPoint(142.75); // 5 ס"מ = 142.75 נקודות
            image.Height = Unit.FromPoint(142.75); // 5 ס"מ = 142.75 נקודות

            Row = table.AddRow();

            // קו אופקי אחרון
            lineParagraph = section.AddParagraph();
            lineParagraph.Format.Borders.Top.Width = Unit.FromPoint(2.85); // 0.1 ס"מ = 2.85 נקודות
            lineParagraph.Format.Borders.Top.Color = Colors.Black;
            lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);

            if (data.activitiesDetails != null && data.activitiesDetails.Count > 0)
            {
                data.activitiesDetails.ForEach(activity =>
                {
                    mainTable = section.AddTable();
                    mainTable.AddColumn(Unit.FromPoint(113.4)); // 4 ס"מ = 113.4 נקודות
                    mainTable.AddColumn(Unit.FromPoint(283.5)); // 10 ס"מ = 283.5 נקודות
                    mainRow = mainTable.AddRow();

                    table = new Table();
                    table.Format.Alignment = ParagraphAlignment.Right;
                    table.AddColumn(Unit.FromPoint(85.05)); // 3 ס"מ = 85.05 נקודות
                    table.AddColumn(Unit.FromPoint(42.75)); // 1.5 ס"מ = 42.75 נקודות
                    table.AddColumn(Unit.FromPoint(42.75)); // 1.5 ס"מ = 42.75 נקודות
                    table.AddColumn(Unit.FromPoint(53.65)); // 1.9 ס"מ = 53.65 נקודות
                    table.AddColumn(Unit.FromPoint(85.05)); // 3 ס"מ = 85.05 נקודות
                    table.AddColumn(Unit.FromPoint(53.65)); // 1.9 ס"מ = 53.65 נקודות

                    // כותרת
                    header = table.AddRow();
                    header.Cells[0].MergeRight = table.Columns.Count - 1;
                    headerParagraph = header.Cells[0].AddParagraph(helperRepository.Reverse("rtl", activity.ActivityName));
                    headerParagraph.Format.Font.Size = 16;
                    headerParagraph.Format.Font.Bold = true;
                    headerParagraph.AddLineBreak();
                    headerParagraph.AddLineBreak();

                    // שורה ראשונה
                    Row = table.AddRow();
                    Row.Format.Alignment = ParagraphAlignment.Right;
                    Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", "תאריך:")).Format.Font.Bold = true;
                    Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", activity.VisitDate));
                    Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "שעה:")).Format.Font.Bold = true;
                    Row.Cells[2].AddParagraph(helperRepository.Reverse("rtl", activity.VisitStartTime));
                    if (!string.IsNullOrEmpty(activity.hall))
                    {
                        Row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "אולם:")).Format.Font.Bold = true;
                        Row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", activity.hall));
                        Row = table.AddRow();
                    }

                    Row = table.AddRow();
                    Row.Format.Alignment = ParagraphAlignment.Right;
                    Row.Cells[4].MergeRight = 1;
                    if (activity.visitorsDetails.Count > 0 && !string.IsNullOrEmpty(activity.visitorsDetails[0].Name))
                    {
                        Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "סוג וכמות כרטיסים:")).Format.Font.Bold = true;
                        Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
                        foreach (var visitorType in activity.visitorsDetails)
                        {
                            Row = table.AddRow();
                            Row = table.AddRow();
                            Row.Format.Alignment = ParagraphAlignment.Right;
                            Row.Cells[4].MergeRight = 1;
                            Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", visitorType.Name + visitorType.Amount + "X"));
                            Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));
                        }
                    }
                    else

                    {


                        Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "כמות כרטיסים:")).Format.Font.Bold = true;
                        Row = table.AddRow();
                        Row = table.AddRow();
                        Row.Format.Alignment = ParagraphAlignment.Right;
                        Row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", activity.visitorsDetails[0].Amount));


                    }
                    if (activity.products.Count > 0 && !string.IsNullOrEmpty(activity.products[0].Name))
                    {
                        // Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "סוג וכמות כרטיסים:")).Format.Font.Bold = true;
                        // Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "מחיר:")).Format.Font.Bold = true;
                        foreach (var visitorType in activity.products)
                        {
                            Row = table.AddRow();
                            Row = table.AddRow();
                            Row.Format.Alignment = ParagraphAlignment.Right;
                            Row.Cells[4].MergeRight = 1;
                            Row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", visitorType.Name + visitorType.Amount + "X"));
                            Row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", visitorType.Price + " שח"));
                        }
                    }

                    mainRow.Cells[1].Elements.Add(table);

                    lineParagraph = section.AddParagraph();
                    lineParagraph.Format.Borders.Top.Width = Unit.FromPoint(2.85); // 0.1 ס"מ = 2.85 נקודות
                    lineParagraph.Format.Borders.Top.Color = Colors.Black;
                    lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);
                });
            }
        }
        private void CreatePageAndFillContentEng(Document document, OnlineReservationTicket data)
        {
            ////header
            Section section = document.AddSection();
            section.PageSetup.RightMargin = Unit.FromPoint(28.35); // שוליים ימניים קטנים

            Table table;
            Row Row;
            Paragraph paragraph = section.Headers.Primary.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            Image logo = paragraph.AddImage("Images/commemoration_header_heb.jpg");
            logo.Width = Unit.FromPoint(85);
            logo.LockAspectRatio = true;

            table = section.AddTable();
            table.AddColumn(Unit.FromPoint(142));
            table.AddColumn(Unit.FromPoint(85));

            Row = table.AddRow();
            Row.Cells[0].AddParagraph("Date of purchase:");

            if (!string.IsNullOrEmpty(data.PaymentMethod))
            {
                Row.Cells[1].AddParagraph("Payment method:");
            }
            Row = table.AddRow();
            Row.Cells[0].AddParagraph(data.PurchaseDate);
            if (!string.IsNullOrEmpty(data.PaymentMethod))
            {
                Row.Cells[1].AddParagraph(data.PaymentMethod);
            }

            paragraph = section.AddParagraph();
            paragraph.AddLineBreak();
            paragraph.AddText("your card: " + data.FullName);
            paragraph.AddTab();
            paragraph.AddText("Reservation number: " + data.ReservationNumber);
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Size = 12;
            Paragraph lineParagraph = section.AddParagraph();
            lineParagraph.Format.Borders.Top.Width = Unit.FromPoint(0.28);
            lineParagraph.Format.Borders.Top.Color = Colors.Black;
            lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);

            paragraph.AddLineBreak();
            paragraph = section.AddParagraph();
            paragraph.AddLineBreak();

            Table mainTable = section.AddTable();
            mainTable.AddColumn(Unit.FromPoint(368));
            mainTable.AddColumn(Unit.FromPoint(198));
            Row mainRow = mainTable.AddRow();

            table = new Table();
            table.AddColumn(Unit.FromPoint(54));
            table.AddColumn(Unit.FromPoint(100));
            table.AddColumn(Unit.FromPoint(54));
            table.AddColumn(Unit.FromPoint(250));


            Row header = table.AddRow();
            header.Cells[0].MergeRight = table.Columns.Count - 1;
            Paragraph headerParagraph = header.Cells[0].AddParagraph(data.ActivityName);
            headerParagraph.Format.Font.Size = 16;
            headerParagraph.Format.Font.Bold = true;
            headerParagraph.AddLineBreak();
            if (data.TourNote != null)
            {
                Row note = table.AddRow();
                note.Cells[0].MergeRight = table.Columns.Count - 1;
                Paragraph headerParagraphNote = note.Cells[0].AddParagraph(data.TourNote);
                headerParagraphNote.AddLineBreak();
                headerParagraphNote.AddLineBreak();
            }
            else
            {
                headerParagraph.AddLineBreak();
            }

            Row = table.AddRow();
            Row.Cells[0].AddParagraph("date:").Format.Font.Bold = true;
            Row.Cells[1].AddParagraph(data.VisitDate);
            Row.Cells[2].AddParagraph("time:").Format.Font.Bold = true;
            Row.Cells[3].AddParagraph(data.VisitStartTime);
            if (!string.IsNullOrEmpty(data.hall))
            {
                Row.Cells[0].AddParagraph("hall:").Format.Font.Bold = true;
                Row.Cells[1].AddParagraph(data.hall);
                Row = table.AddRow();
            }
            Row = table.AddRow();
            Row = table.AddRow();
            Row.Cells[0].MergeRight = 1;
            if (data.visitorsDetails.Count > 0 && !string.IsNullOrEmpty(data.visitorsDetails[0].Name))
            {
                Row.Cells[0].AddParagraph("Type and quantity of tickets:").Format.Font.Bold = true;
                Row.Cells[2].AddParagraph("price:").Format.Font.Bold = true;
                foreach (var visitorType in data.visitorsDetails)
                {
                    Row = table.AddRow();
                    Row = table.AddRow();
                    Row.Cells[0].MergeRight = 1;
                    Row.Cells[0].AddParagraph(visitorType.Name + " X" + visitorType.Amount);
                    Row.Cells[2].AddParagraph(visitorType.Price + "₪");
                }
            }
            else
            {

                Row.Cells[0].AddParagraph("quantity of tickets:").Format.Font.Bold = true;
                Row = table.AddRow();
                Row = table.AddRow();
                Row.Cells[0].AddParagraph(data.visitorsDetails[0].Amount);


            }
            if (data.products.Count > 0 && !string.IsNullOrEmpty(data.products[0].Name))
            {
                //Row.Cells[0].AddParagraph("Type and quantity of tickets:").Format.Font.Bold = true;
                // Row.Cells[2].AddParagraph("price:").Format.Font.Bold = true;
                foreach (var visitorType in data.products)
                {
                    Row = table.AddRow();
                    Row = table.AddRow();
                    Row.Cells[0].MergeRight = 1;
                    Row.Cells[0].AddParagraph(visitorType.Name + " X" + visitorType.Amount);
                    Row.Cells[2].AddParagraph(visitorType.Price + "₪");
                }
            }


                mainRow.Cells[0].Elements.Add(table);

                Image image = mainRow.Cells[1].AddImage(data.QRcodeUrl);
                image.LockAspectRatio = true;
                image.Width = Unit.FromPoint(142);
                image.Height = Unit.FromPoint(142);

                Row = table.AddRow();

                lineParagraph = section.AddParagraph();
                lineParagraph.Format.Borders.Top.Width = Unit.FromPoint(0.28);
                lineParagraph.Format.Borders.Top.Color = Colors.Black;
                lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);
                if (data.activitiesDetails != null && data.activitiesDetails.Count > 0)
                {
                    data.activitiesDetails.ForEach(activity =>
                    {
                        mainTable = section.AddTable();
                        mainTable.AddColumn(Unit.FromPoint(368));
                        mainTable.AddColumn(Unit.FromPoint(198));
                        mainRow = mainTable.AddRow();
                        table = new Table();
                        table.AddColumn(Unit.FromPoint(54));
                        table.AddColumn(Unit.FromPoint(100));
                        table.AddColumn(Unit.FromPoint(54));
                        table.AddColumn(Unit.FromPoint(250));


                        header = table.AddRow();
                        header.Cells[0].MergeRight = table.Columns.Count - 1;
                        headerParagraph = header.Cells[0].AddParagraph(activity.ActivityName);
                        headerParagraph.Format.Font.Size = 16;
                        headerParagraph.Format.Font.Bold = true;
                        headerParagraph.AddLineBreak();
                        headerParagraph.AddLineBreak();

                        Row = table.AddRow();
                        Row.Cells[0].AddParagraph("date:").Format.Font.Bold = true;
                        Row.Cells[1].AddParagraph(activity.VisitDate);
                        Row.Cells[2].AddParagraph("time:").Format.Font.Bold = true;
                        Row.Cells[3].AddParagraph(activity.VisitStartTime);
                        if (!string.IsNullOrEmpty(activity.hall))
                        {
                            Row.Cells[0].AddParagraph("hall:").Format.Font.Bold = true;
                            Row.Cells[1].AddParagraph(activity.hall);
                            Row = table.AddRow();
                        }
                        Row = table.AddRow();
                        Row = table.AddRow();

                        Row.Cells[0].MergeRight = 1;
                        if (activity.visitorsDetails.Count > 0 && !string.IsNullOrEmpty(activity.visitorsDetails[0].Name))
                        {
                            Row.Cells[0].AddParagraph("Type and quantity of tickets:").Format.Font.Bold = true;
                            Row.Cells[2].AddParagraph("price:").Format.Font.Bold = true;
                            foreach (var visitorType in activity.visitorsDetails)
                            {
                                Row = table.AddRow();
                                Row = table.AddRow();
                                Row.Cells[0].MergeRight = 1;
                                Row.Cells[0].AddParagraph(visitorType.Name + " X" + visitorType.Amount);
                                Row.Cells[2].AddParagraph(visitorType.Price + "₪");
                            }
                        }
                        else
                        {


                            Row.Cells[0].AddParagraph("quantity of tickets:").Format.Font.Bold = true;
                            Row = table.AddRow();
                            Row = table.AddRow();
                            Row.Cells[0].AddParagraph(activity.visitorsDetails[0].Amount);


                        }
                        if (activity.products.Count > 0 && !string.IsNullOrEmpty(activity.products[0].Name))
                        {
                            Row.Cells[0].AddParagraph("Type and quantity of tickets:").Format.Font.Bold = true;
                            Row.Cells[2].AddParagraph("price:").Format.Font.Bold = true;
                            foreach (var visitorType in activity.products)
                            {
                                Row = table.AddRow();
                                Row = table.AddRow();
                                Row.Cells[0].MergeRight = 1;
                                Row.Cells[0].AddParagraph(visitorType.Name + " X" + visitorType.Amount);
                                Row.Cells[2].AddParagraph(visitorType.Price + "₪");
                            }
                        }

                        mainRow.Cells[0].Elements.Add(table);



                        Row = table.AddRow();

                        lineParagraph = section.AddParagraph();
                        lineParagraph.Format.Borders.Top.Width = Unit.FromPoint(0.28);
                        lineParagraph.Format.Borders.Top.Color = Colors.Black;
                        lineParagraph.Format.Borders.Distance = Unit.FromPoint(0);
                    });
                }


            }


        
    }
}

