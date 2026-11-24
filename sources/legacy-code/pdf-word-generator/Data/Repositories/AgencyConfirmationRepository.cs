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
using System.Globalization;

namespace Data.Repositories
{
    public class AgencyConfirmationRepository : IAgencyConfirmationRepository
    {
        private IHelperRepository helperRepository;
        Color borderColor = Color.Parse("#007dae");
        public AgencyConfirmationRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;
        }
        IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

        public string CreateDocument(Document document, AgencyConfirmationData data)
        {
            try
            {

                Log.Error("in invoice repository create document.  ");
                document.Styles[StyleNames.Normal].Font.Name = "Arial";

                document.Info.Title = "הצעת מחיר " + data.OrderNum;
                document.Info.Subject = "Confirmation 222";
                document.Info.Author = "Confirmation 333";
                if (data.DocLang == "he") {
                    CreatePageAndFillContentHe(document, data);
                }
                else//en
                {
                    CreatePageAndFillContentEn(document, data);
                }

                //switch (data.Lang)
                //{
                //    case "heb":
                //        {
                //            Log.Error("lang heb");
                //            CreatePageAndFillContentheb(document, data);
                //            break;
                //        }
                //    case "eng":
                //        {
                //                throw new Exception("todo english");
                //            }
                //    default:
                //        {
                //            Log.Error("lang is empty");
                //                string message = "exception in invoice repository create document- lang needs to be heb or eng";
                //                Log.Fatal(message);
                //                throw new Exception(message);
                //            }

                //}
                Log.Error("go to save doc");
                string file_path = helperRepository.SaveDoc(document, data.OrderNum + DateTime.Now.ToString("mm-ss"), "Confirmation");
                Log.Error("after save doc");
                return file_path;

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "exception in invoice repository create document");
                throw ex;
            }
        }


        private void CreatePageAndFillContentHe(Document document, AgencyConfirmationData data)
        {
            #region basic settings
            string dir = "rtl";
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(5.8);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(1.7);
            document.DefaultPageSetup.Orientation = Orientation.Portrait;

            var fontSmall = new Font();
            fontSmall.Size = 9;

            var font9 = new Font();
            font9.Size = 9;

            var font9Grey = new Font();
            font9Grey.Size = 9;
            font9Grey.Color = Color.Parse("#bcbdc0");


            var font9B = new Font();
            font9B.Size = 9;
            font9B.Bold = true;

            var font9BLink = new Font();
            font9BLink.Size = 9;
            font9BLink.Bold = true;
            font9BLink.Color = Color.Parse("#057cae");
            font9BLink.Underline = Underline.Single;

            var font10 = new Font();
            font10.Size = 10;

            var font10B = new Font();
            font10B.Size = 10;
            font10B.Bold = true;

            var font11 = new Font();
            font11.Size = 11;

            var fontBig = new Font();
            fontBig.Size = 11;
            fontBig.Bold = true;


            var font12B = new Font();
            font12B.Size = 12;
            font12B.Bold = true;

            var fontMedium = new Font();
            fontMedium.Size = 10;
            fontMedium.Bold = true;

            var fontTotal = new Font();
            fontTotal.Size = 15;
            fontTotal.Bold = true;

            var fontTotalWithDiscount = new Font();
            fontTotalWithDiscount.Size = 13;

            var fontTiny = new Font();
            fontTiny.Size = 8;

            var fontTinyB = new Font();
            fontTinyB.Size = 8;
            fontTinyB.Bold = true;

            var font7B = new Font();
            font7B.Size = 7;
            font7B.Bold = true;

            var font7HB = new Font();
            font7HB.Size = 7.5;
            font7HB.Bold = true;


            var font7 = new Font();
            font7.Size = 7;

            var font7Link = new Font();
            font7Link.Size = 7.2;
            font7Link.Color = Color.Parse("#057cae");
            font7Link.Underline = Underline.Single;

            var font7H = new Font();
            font7H.Size = 7.5;

            var font7HLink = new Font();
            font7HLink.Size = 7.7;
            font7HLink.Color = Color.Parse("#057cae");
            font7HLink.Underline = Underline.Single;

            var font8 = new Font();
            font8.Size = 8;

            var font8B = new Font();
            font8B.Size = 8;
            font8B.Bold = true;

            var font6 = new Font();
            font6.Size = 6;

            var font6H = new Font();
            font6H.Size = 6.5;

            var font6HB = new Font();
            font6HB.Size = 6.5;
            font6HB.Bold = true;

            var font6HLink = new Font();
            font6HLink.Size = 6.7;
            font6HLink.Color = Color.Parse("#057cae");
            font6HLink.Underline = Underline.Single;

            var fontTableColumnsHeader = new Font();
            fontTableColumnsHeader.Size = 9;
            fontTableColumnsHeader.Bold = true;

            var fontLine = new Font();
            fontLine.Color = Color.Parse("#057cae");//Color.Parse("#BDBEC1");
            fontLine.Size = 6;


            #endregion

            #region headers and date
            // Each MigraDoc document needs at least one section.
            Section section = document.AddSection();

            section.PageSetup.DifferentFirstPageHeaderFooter = true;
            section.PageSetup.HeaderDistance = 0;
            section.PageSetup.FooterDistance = 2;

            Paragraph paragraph1 = section.AddParagraph();

            section.Headers.FirstPage.AddImage(@"Images\agency_banner_he.jpg");
            Log.Error("AddImage " + @"Images\agency_banner_he.jpg");
            AddYVOrderNumTextFrame(section.Headers.FirstPage, data.OrderNum, data.DocLang);



            TextFrame bannerFrame = new TextFrame();
            bannerFrame = section.AddTextFrame();
            bannerFrame.Width = "7.0cm";
            bannerFrame.Left = ShapePosition.Left;
            bannerFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            bannerFrame.WrapFormat.DistanceTop = "4.1cm";
            bannerFrame.WrapFormat.DistanceLeft = "5.1cm";
            bannerFrame.RelativeVertical = RelativeVertical.Page;
            paragraph1 = bannerFrame.AddParagraph();
            paragraph1.Format.Alignment = ParagraphAlignment.Right;
            paragraph1.Format.LineSpacing = 20;
            paragraph1.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            //adina took off 14/9/2021 because when too long it looks bad
            //paragraph1.AddFormattedText(data.TourDate + helperRepository.Reverse(dir, " מועד הפעילות: "), fontBig);
            //paragraph1.AddLineBreak();
            //paragraph1.AddFormattedText(data.TourTime + helperRepository.Reverse(dir, " שעת הביקור: "), font11);
            //paragraph1.AddLineBreak();

            #endregion

            #region big table

            Paragraph paragraph = new Paragraph();

            Table bigTable = section.AddTable();
            bigTable.TopPadding= 15;
            bigTable.Rows.LeftIndent = 30;
            bigTable.AddColumn("9.5cm");
            bigTable.AddColumn("9.2cm");
            Row row = bigTable.AddRow();
            row.Height = "11.3cm";
            row.HeightRule = RowHeightRule.Exactly;
            //bigTable.Borders.Color = Colors.HotPink;
            //bigTable.Borders.Width = 2;

            TextFrame productListTF = bigTable[0, 0].AddTextFrame();
            Cell cell1 = MakeBox(productListTF, "בקשתכם כוללת", "9.2cm", "11.3cm", data.DocLang);
            SetProductList(cell1, data.ProductList, data.DocLang, data.Total, data.TotalPriceAfterDiscount);


            Cell cell2 = MakeBox(bigTable[0, 1].AddTextFrame(), "פרטי הביקור", "9.2cm", "8cm", data.DocLang);
            paragraph = cell2.AddParagraph();
            SetParagraph(paragraph, data.DocLang);
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "שם הקבוצה/ משפחה:"), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.GroupName), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.TourDate) + helperRepository.Reverse(dir, "תאריך: "), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.TourTime) + helperRepository.Reverse(dir, "שעה: "), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "לתשומת ליבך, פינוי המוזיאון ממבקרים יחל כ 40- דק’ טרם שעת סגירתו."), font8B);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "פעילות: "), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.ActivityType), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.Lang) + helperRepository.Reverse(dir, "שפה: "), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.NumberOfParticipants) + helperRepository.Reverse(dir, "מספר מבקרים: "), font10);   
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "אישור זה מהווה אסמכתא סופית להזמנתך."), font10B);

            TextFrame tf = bigTable[0, 1].AddTextFrame();
            tf.Top = "5.8cm";
            Cell cell3 = MakeBox(tf, "סוכן אחראי", "9.2cm", "2.95cm", data.DocLang);
            paragraph = cell3.AddParagraph();
            SetParagraph(paragraph, data.DocLang);
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.ContactName) + helperRepository.Reverse(dir, "שם מלא: "), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(data.ContactEmail + helperRepository.Reverse(dir, "דואר אלקטרוני: "), font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(data.ContactPhone + helperRepository.Reverse(dir, "טלפון: "), font10);

            row = bigTable.AddRow();
            row.Height = "3.3cm";
            row.HeightRule = RowHeightRule.Exactly;
            row.Cells[0].MergeRight = 1;
            tf = bigTable[1, 0].AddTextFrame();
            tf.Top = "0.35cm";
            Cell cell4 = MakeBox(tf, "הערות ובקשות מיוחדות", "18.7cm", "2.6cm", data.DocLang);
            paragraph = cell4.AddParagraph();


            SetParagraph(paragraph, data.DocLang);
            paragraph.Format.LineSpacing = 12;
            paragraph.Format.LineSpacingRule = LineSpacingRule.Exactly;
            //paragraph.AddFormattedText(helperRepository.Reverse(dir, data.Comments), font10);
            helperRepository.AddToTextSplitToLines("rtl", paragraph, data.Comments, 110, font10);

            #endregion

            #region health regulations

            paragraph = section.AddParagraph();
            paragraph.Format.LeftIndent = 32;
            paragraph.Format.RightIndent = 37;
            paragraph.Format.LineSpacing = 8;
            paragraph.Format.SpaceBefore = 10;

            paragraph.Format.Borders.Distance = 0;
            paragraph.Format.LineSpacingRule = LineSpacingRule.Exactly;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            /*paragraph.AddFormattedText(helperRepository.Reverse(dir, "הנחיות לביקור במוזיאון במסגרת התו הסגול:"), font7HB);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "הנחיות לביקור במוזיאון במסגרת התו הסגולעל מנת לשמור על בריאותכם, עליכם להקפיד על הנחיות משרד הבריאות ועל כללי ההיגיינה המפורטים: חובה לעטות מסיכה, שמירת מרחק 2 מטר, שמירה על היגיינה, מדידת חום."), font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "שימו לב — אין להיכנס למוזיאון במקרים הבאים:"), font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "• באם את/ ה משתעל / ת )למעט אלרגיה או אסטמה)."), font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "• באם חום גופך 38°C ומעלה או באם היה לך חום כאמור בשבוע האחרון."), font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "• באם היית במגע קרוב עם חולה קורונה בשבועיים האחרונים."), font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "ייתכן ונדרש בהמשך לשינוי שעות הפתיחה, שינוי בגודל הקבוצה, ובכפוף לכך שינוי בעלויות הביקור."), font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "לפיכך, הנכם נדרשים לוודא ולאשר הזמנתכם 48 שעות לפני מועד הביקור בטלפון: 02-6443802"), font7HB);
            paragraph.AddLineBreak();

           paragraph.AddFormattedText(helperRepository.Reverse(dir, "במידה ותתקבל בקשה מרשויות הבריאות במדינה להעביר מידע אודות המבקרים, המידע יועבר בהתאם."), font7HB);
           paragraph.AddLineBreak();
          */
            //paragraph.AddFormattedText(helperRepository.Reverse(dir, "בנסיבות מיוחדות, יד ושם שומר לעצמו את הזכות לבטל / לשנות את ההזמנה בהודעה מוקדמת ללקוח."), font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("visit@yadvashem.org.il", font7HLink);
            paragraph.AddFormattedText(helperRepository.Reverse(dir, " | טל‘: 02-6443802 | דוא“ל: "), font7H);
            paragraph.AddFormattedText("www.yadvashem.org/he/visiting", font7HLink);
            paragraph.AddFormattedText(helperRepository.Reverse(dir, "למידע נוסף: "), font7H);


            paragraph = section.AddParagraph();
            paragraph.Format.LeftIndent = 32;
            paragraph.Format.RightIndent = 37;
            paragraph.Format.SpaceBefore = 0;
            paragraph.Format.Borders.Bottom.Color = Colors.Black;
            paragraph.Format.Borders.Bottom.Width = 0.5;
            paragraph.Format.Borders.Distance = 0;

            #endregion

            #region rules

            Table tableRules = section.AddTable();
            tableRules.Format.SpaceBefore = 8;
            tableRules.Format.SpaceAfter = 12;
            tableRules.AddColumn("10.6cm");
            tableRules.AddColumn("9.3cm");
            Row rowRules = tableRules.AddRow();

            rowRules.Cells[0].Format.LeftIndent = 32;
            rowRules.Cells[0].Format.RightIndent = 6;
            rowRules.Format.Alignment = ParagraphAlignment.Right;
            rowRules.VerticalAlignment = VerticalAlignment.Top;
            Paragraph paragraphRules = tableRules[0, 0].AddParagraph();
            paragraphRules.Format.LineSpacing = 8;
            paragraphRules.Format.LineSpacingRule = LineSpacingRule.Exactly;
            paragraphRules.Format.Borders.Distance = 0;

            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "תנאים לשינוי וביטול:"), font6HB);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "לביטול או שינוי ההזמנה יש להיכנס לאתר יד ושם "), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("https://forms.yadvashem.org/he/reservations-edit", font6HLink);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "לא יינתן החזר עבור דמי הרשמה וטיפול להזמנה מבוטלת בכל שלב ומקרה."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "ביטול/ שינוי סיור יתאפשר עד 3 ימי עבודה לא כולל ימי שישי וערבי חג, לפני מועד הביקור המתוכנן, "), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "באמצעות אתר יד ושם."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "ביטול שלא יתבצע בהתאם לתנאים אלו יחוייב ב 100 % מגובה החיוב בגין השימוש בשירותים השונים."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "במקרה של ביטול ביום הפעילות מחמת כח עליון (מצב חירום עפ”י גורם ממשלתי מוסמך)"), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, " המזמין לא יחויב בתשלום."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "בנסיבות מיוחדות, יד ושם שומר לעצמו את הזכות לבטל / לשנות את ההזמנה בהודעה מוקדמת ללקוח."), font6H);

            paragraphRules = tableRules[0, 1].AddParagraph();
            //rowSig.Cells[1].Format.LeftIndent = 32;
            rowRules.Cells[1].Format.RightIndent = 0;
            paragraphRules.Format.LineSpacing = 8;
            paragraphRules.Format.LineSpacingRule = LineSpacingRule.Exactly;
            paragraphRules.Format.Borders.Left.Color = Colors.Black;
            paragraphRules.Format.Borders.Left.Width = 0.5;
            paragraphRules.Format.Borders.Distance = 0;
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "כללי התנהגות במוזיאון:"), font6HB);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "הכניסה ליד ושם בלבוש הולם ומכובד."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "אין להכניס מזון, שתיה, תיקים וכן אסור לצלם ולעשן."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "הכנסת מערכות שמע להדרכה שאינן של יד ושם בתחומי הקמפוס אסורה."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "עם ההגעה לאתר, יש להציג את"), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "טופס האישור הקבוצתי לנציג יד ושם."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "הכניסה למוזיאון לתולדות השואה תותר עד שעה לפני מועד סגירת האתר."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "הכניסה למוזיאון לתולדות השואה מגיל 10 ומעלה בהצגת תעודה מזהה."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "לא תתאפשר כניסת ילדים מתחת לגיל זה."), font6H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(helperRepository.Reverse(dir, "אין כניסה לתינוקות בעגלה או במנשא."), font6H);

            #endregion

            #region signature
            if (data.isNeedToshowSignatureParagraph)
            {
                Table tableSig = section.AddTable();
                tableSig.Rows.LeftIndent = 32;

                tableSig.AddColumn("6cm");
                tableSig.AddColumn("3.5cm");
                tableSig.AddColumn("6cm");
                tableSig.AddColumn("3cm");
                Row rowSig = tableSig.AddRow();
                rowSig.Format.Alignment = ParagraphAlignment.Right;
                rowSig.VerticalAlignment = VerticalAlignment.Top;
                rowSig.Cells[0].MergeRight = 3;
                tableSig[0, 0].Format.Borders.Distance = 8;
                tableSig[0, 0].Format.LeftIndent = 0;
                tableSig[0, 0].Format.RightIndent = 8;
                Paragraph paragraphSig = tableSig[0, 0].AddParagraph();
                paragraphSig.AddFormattedText("visit@yadvashem.org.il", font9BLink);
                paragraphSig.AddFormattedText(helperRepository.Reverse(dir, "אישור זה מחייב חתימה והחזרתו למייל "), font9B);

                rowSig = tableSig.AddRow();
                tableSig[1, 0].Format.LeftIndent = 0;
                tableSig[1, 0].Format.RightIndent = 0;
                tableSig[1, 1].Format.LeftIndent = 0;
                tableSig[1, 1].Format.RightIndent = 0;
                tableSig[1, 2].Format.LeftIndent = 0;
                tableSig[1, 2].Format.RightIndent = 0;
                tableSig[1, 3].Format.LeftIndent = 0;
                tableSig[1, 3].Format.RightIndent = 8;
                rowSig.Format.Alignment = ParagraphAlignment.Right;
                rowSig.VerticalAlignment = VerticalAlignment.Top;
                paragraphSig = tableSig[1, 0].AddParagraph();
                paragraphSig.Format.Borders.Width = 0;
                paragraphSig.Format.LineSpacing = 16;
                paragraphSig.Format.LineSpacingRule = LineSpacingRule.AtLeast;
                paragraphSig.Format.Borders.Distance = 0;


                paragraphSig.AddFormattedText("______________________________", font9Grey);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText("______________________________", font9Grey);

                paragraphSig = tableSig[1, 1].AddParagraph();
                paragraphSig.Format.LineSpacing = 16;
                paragraphSig.Format.LineSpacingRule = LineSpacingRule.AtLeast;

                paragraphSig.Format.Borders.Distance = 0;

                paragraphSig.AddFormattedText(helperRepository.Reverse(dir, "תפקיד: "), font9);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText(helperRepository.Reverse(dir, "חתימה וחותמת החברה: "), font9);

                paragraphSig = tableSig[1, 2].AddParagraph();
                paragraphSig.Format.Borders.Width = 0;
                paragraphSig.Format.LineSpacing = 16;
                paragraphSig.Format.LineSpacingRule = LineSpacingRule.AtLeast;
                paragraphSig.Format.Borders.Distance = 0;


                paragraphSig.AddFormattedText("______________________________", font9Grey);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText("______________________________", font9Grey);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText("______________________________", font9Grey);

                paragraphSig = tableSig[1, 3].AddParagraph();
                paragraphSig.Format.LineSpacing = 16;
                paragraphSig.Format.LineSpacingRule = LineSpacingRule.AtLeast;

                paragraphSig.Format.Borders.Distance = 0;

                paragraphSig.AddFormattedText(helperRepository.Reverse(dir, "שם המזמין: "), font9);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText(helperRepository.Reverse(dir, "טלפון ישיר: "), font9);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText(helperRepository.Reverse(dir, "תאריך: "), font9);

                tableSig.SetEdge(0, 0, 4, 2, Edge.Box, BorderStyle.Single, 0.5, Color.Parse("#057cae"));
            }
            #endregion

            #region footer

            AddYVFooterTextFrameHe(section.Footers.FirstPage, data, dir);//for firstPage

            #endregion
        }

        private void CreatePageAndFillContentEn(Document document, AgencyConfirmationData data)
        {
            #region basic settings
            string dir = "ltr";
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(5.8);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(1.7);
            document.DefaultPageSetup.Orientation = Orientation.Portrait;

            var fontSmall = new Font();
            fontSmall.Size = 9;

            var font9 = new Font();
            font9.Size = 9;

            var font9Grey = new Font();
            font9Grey.Size = 9;
            font9Grey.Color = Color.Parse("#bcbdc0");

            var font9B = new Font();
            font9B.Size = 9;
            font9B.Bold = true;

            var font9BLink = new Font();
            font9BLink.Size = 9;
            font9BLink.Bold = true;
            font9BLink.Color = Color.Parse("#057cae");
            font9BLink.Underline = Underline.Single;


            var font9HB = new Font();
            font9HB.Size = 9.8;
            font9HB.Bold = true;

            var font10 = new Font();
            font10.Size = 10;

            var font10B = new Font();
            font10B.Size = 10;
            font10B.Bold = true;

            var font11 = new Font();
            font11.Size = 11;

            var fontBig = new Font();
            fontBig.Size = 11;
            fontBig.Bold = true;


            var font12B = new Font();
            font12B.Size = 12;
            font12B.Bold = true;

            var fontMedium = new Font();
            fontMedium.Size = 10;
            fontMedium.Bold = true;

            var fontTotal = new Font();
            fontTotal.Size = 15;
            fontTotal.Bold = true;

            var fontTotalWithDiscount = new Font();
            fontTotalWithDiscount.Size = 13;

            var fontTiny = new Font();
            fontTiny.Size = 8;

            var fontTinyB = new Font();
            fontTinyB.Size = 8;
            fontTinyB.Bold = true;

            var font7B = new Font();
            font7B.Size = 7;
            font7B.Bold = true;

            var font7HB = new Font();
            font7HB.Size = 7.5;
            font7HB.Bold = true;

            var font7 = new Font();
            font7.Size = 7;

            var font7H = new Font();
            font7H.Size = 7.5;

            var font7HLink = new Font();
            font7HLink.Size = 7.5;
            font7HLink.Color = Color.Parse("#057cae");
            font7HLink.Underline = Underline.Single;

            var font8 = new Font();
            font8.Size = 8;

            var font8B = new Font();
            font8B.Size = 8;
            font8B.Bold = true;

            var font6 = new Font();
            font6.Size = 6;

            var font6B = new Font();
            font6B.Size = 6;
            font6B.Bold = true;

            var font5H = new Font();
            font5H.Size = 5.5;

            var font5HB = new Font();
            font5HB.Size = 5.5;
            font5HB.Bold = true;

            var font5HLink = new Font();
            font5HLink.Size = 5.5;
            font5HLink.Color = Color.Parse("#057cae");
            font5HLink.Underline = Underline.Single;

            var font6HB = new Font();
            font6HB.Size = 6.5;
            font6HB.Bold = true;

            var font6Link = new Font();
            font6Link.Size = 6;
            font6Link.Color = Color.Parse("#057cae");
            font6Link.Underline = Underline.Single;

            var fontTableColumnsHeader = new Font();
            fontTableColumnsHeader.Size = 9;
            fontTableColumnsHeader.Bold = true;

            var fontLine = new Font();
            fontLine.Color = Color.Parse("#057cae");//Color.Parse("#BDBEC1");
            fontLine.Size = 6;


            #endregion

            #region headers and date
            // Each MigraDoc document needs at least one section.
            Section section = document.AddSection();

            section.PageSetup.DifferentFirstPageHeaderFooter = true;
            section.PageSetup.HeaderDistance = 0;
            section.PageSetup.FooterDistance = 2;

            Paragraph paragraph1 = section.AddParagraph();

            section.Headers.FirstPage.AddImage(@"Images\agency_banner_en.jpg");
            Log.Error("AddImage " + @"Images\agency_banner_en.jpg");
            AddYVOrderNumTextFrame(section.Headers.FirstPage, data.OrderNum, data.DocLang);


            TextFrame bannerFrame = new TextFrame();
            bannerFrame = section.AddTextFrame();
            bannerFrame.Width = "7.0cm";
            bannerFrame.Left = ShapePosition.Left;
            bannerFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            bannerFrame.WrapFormat.DistanceTop = "4.1cm";
            bannerFrame.WrapFormat.DistanceLeft = "7.8cm";
            bannerFrame.RelativeVertical = RelativeVertical.Page;
            paragraph1 = bannerFrame.AddParagraph();
            paragraph1.Format.Alignment = ParagraphAlignment.Left;
            paragraph1.Format.LineSpacing = 20;
            paragraph1.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph1.AddFormattedText("Tour Date: " + data.TourDate, fontBig);
            paragraph1.AddLineBreak();
            paragraph1.AddFormattedText("Starting Time: " + data.TourTime, font11);
            paragraph1.AddLineBreak();

            #endregion

            #region big table

            Paragraph paragraph = new Paragraph();

            Table bigTable = section.AddTable();
            bigTable.TopPadding = 15;
            bigTable.Rows.LeftIndent = 30;
            bigTable.AddColumn("9.5cm");
            bigTable.AddColumn("9.2cm");
            Row row = bigTable.AddRow();
            row.Height = "10.9cm";
            row.HeightRule = RowHeightRule.Exactly;
            //bigTable.Borders.Color = Colors.HotPink;
            //bigTable.Borders.Width = 0.5;

            TextFrame productListTF = bigTable[0, 1].AddTextFrame();
            Cell cell1 = MakeBox(productListTF, "Your Reservation Includes", "9.2cm", "10.6cm", data.DocLang);
            SetProductList(cell1, data.ProductList, data.DocLang, data.Total, data.TotalPriceAfterDiscount);

            Cell cell2 = MakeBox(bigTable[0, 0].AddTextFrame(), "Tour Details", "9.2cm", "7.6cm", data.DocLang);
            paragraph = cell2.AddParagraph();
            SetParagraph(paragraph, data.DocLang);
            paragraph.AddFormattedText("Group/ Family Name: ", font10);
            paragraph.AddLineBreak();
            //adina 13.7.21 deleted helperRepository.Reverse
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.GroupName), font10);
           //devora- 2022-07-18 put it back
            // paragraph.AddFormattedText(data.GroupName, font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Date: " + data.TourDate, font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Time: " + data.TourTime, font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("The museum galleries are gradually cleared 40 minutes prior to closing.", font8B);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Activity: ", font10);
            paragraph.AddLineBreak();
            //adina 13.7.21 deleted helperRepository.Reverse
            //devora- 2022-07-18 put it back
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.ActivityType), font10);
           
            //paragraph.AddFormattedText(data.ActivityType, font10);
            paragraph.AddLineBreak();
            //adina 13.7.21 deleted helperRepository.Reverse
            //paragraph.AddFormattedText("Language: " + helperRepository.Reverse(dir, data.Lang) , font10);
            paragraph.AddFormattedText("Language: " + data.Lang, font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Number of Visitors: " + data.NumberOfParticipants, font10); 
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("This confirmation is a final reference for your order.", font9HB);

            TextFrame tf = bigTable[0, 0].AddTextFrame();
            tf.Top = "5.3cm";
            Cell cell3 = MakeBox(tf, "Responsible Agent", "9.2cm", "2.75cm", data.DocLang);
            paragraph = cell3.AddParagraph();
            SetParagraph(paragraph, data.DocLang);
            //adina 13.7.21 deleted helperRepository.Reverse
            //devora- 2022-07-18 put it back
            paragraph.AddFormattedText("Full Name: " + helperRepository.Reverse(dir, data.ContactName), font10);
          //  paragraph.AddFormattedText("Full Name: " + data.ContactName, font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Email: " + data.ContactEmail, font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Phone Number: " + data.ContactPhone, font10);

            row = bigTable.AddRow();
            row.Height = "2.9cm";
            row.HeightRule = RowHeightRule.Exactly;
            row.Cells[0].MergeRight = 1;
            tf = bigTable[1, 0].AddTextFrame();
            Cell cell4 = MakeBox(tf, "Comments and Special Requests", "18.7cm", "2.6cm", data.DocLang);
            paragraph = cell4.AddParagraph();
            SetParagraph(paragraph, data.DocLang);
            paragraph.Format.LineSpacing = 12;
            paragraph.Format.LineSpacingRule = LineSpacingRule.Exactly;
            //paragraph.AddFormattedText(helperRepository.Reverse(dir, data.Comments), font10);
            helperRepository.AddToTextSplitToLines("ltr", paragraph, data.Comments, 110, font10);






            #endregion

            #region Health Regulations

            paragraph = section.AddParagraph();
            paragraph.Format.LeftIndent = 30;
            paragraph.Format.RightIndent = 37;
            paragraph.Format.LineSpacing = 7;
            paragraph.Format.SpaceBefore = 17;

            paragraph.Format.Borders.Distance = 0;
            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.Alignment = ParagraphAlignment.Left;
            /*
            paragraph.AddFormattedText("Health Regulations Regarding Entry to the Museum:", font7HB);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("For your own safety visiting the Museum will require wearing a face mask, maintaining social distance(2 meters), maintaining personal hygiene and measuring body temperature.", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Please note: Entry will not be permitted in the following cases:", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("• Body temperature over 38°C at time of visit or within the week prior to visiting", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("• Cough not due to allergies or asthma", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("• Close contact with a confirmed COVID-19 case in the past two weeks", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Opening hours, group size and visiting costs are subject to alteration.", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Please reconfirm your reservation 48 hours prior to your scheduled visit: 02-6443802.", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Visitor information will be disclosed to the Israeli Health Ministry if this is required for contact tracing purposes.", font7H);
            paragraph.AddLineBreak();*/
            paragraph.AddFormattedText("In special circumstances, Yad Vashem reserves the right to cancel / change the reservation with advance notice to the client.", font7H);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("For further information: ", font7H);
            paragraph.AddFormattedText("www.yadvashem.org/visiting", font7HLink);
            paragraph.AddFormattedText(" | Tel: 02-6443802 | Email: ", font7H);
            paragraph.AddFormattedText("visit@yadvashem.org.il", font7HLink);

            paragraph = section.AddParagraph();
            paragraph.Format.RightIndent = 34;
            paragraph.Format.LeftIndent = 30;
            paragraph.Format.SpaceBefore = 0;
            paragraph.Format.Borders.Bottom.Color = Colors.Black;
            paragraph.Format.Borders.Bottom.Width = 0.5;
            paragraph.Format.Borders.Distance = 0;

            #endregion

            #region rules

            Table tableRules = section.AddTable();
            tableRules.Format.SpaceBefore = 6;
            tableRules.Format.SpaceAfter = 8;
            tableRules.AddColumn("9.8cm");
            tableRules.AddColumn("10.2cm");
            Row rowRules = tableRules.AddRow();

            rowRules.Cells[0].Format.LeftIndent = 30;
            //rowRules.Cells[0].Format.RightIndent = 6;
            rowRules.Format.Alignment = ParagraphAlignment.Left;
            rowRules.VerticalAlignment = VerticalAlignment.Top;

            Paragraph paragraphRules = tableRules[0, 0].AddParagraph();
            paragraphRules.Format.Borders.Right.Color = Colors.Black;
            paragraphRules.Format.Borders.Right.Width = 0.5;
            paragraphRules.Format.LineSpacing = 8;
            paragraphRules.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraphRules.Format.Borders.Distance = 0;
            paragraphRules.AddFormattedText("Rules of conduct inside the Holocaust History Museum:", font5HB);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Please dress respectfully when visiting Yad Vashem.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("No photography or smoking; Food, drinks and bags are not permitted.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Only Yad Vashem wireless headset systems may be used for tours of the Museum and grounds.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Upon arrival, please present the Group Permissions Form to a Yad Vashem representative.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Entrance to the Holocaust History Museum is permitted until one hour before closing.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Entrance to the Holocaust History Museum is permitted for children age 10 and over, on presentation of ID. Children under the age of ten, including babies in strollers or carriers, may not enter the Museum.", font5H);

            paragraphRules = tableRules[0, 1].AddParagraph();
            rowRules.Cells[1].Format.LeftIndent = 8;
            //rowRules.Cells[1].Format.RightIndent = 0;
            paragraphRules.Format.LineSpacing = 8;
            paragraphRules.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraphRules.Format.Borders.Distance = 0;

            paragraphRules.AddFormattedText("Alteration and Cancellation Policy:", font5HB);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Please cancel or alter your reservation via Yad Vashem's website:", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText(" https://forms.yadvashem.org/reservations-edit", font5HLink);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("The registration and handling fee for a cancelled registration is non-refundable under any circumstances.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Cancellation/alteration of a scheduled tour is possible up to 72 hours(3 business days, excluding Fridays and Jewish holiday eves) before the scheduled date and time, via Yad Vashem's website.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("Cancellation/alteration that does not fulfil these conditions will incur a charge of 100% of the price of the tour and services.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("In the case of cancellation on the day of the scheduled tour due to 'force majeure' (a state of emergency as declared by an authorized governmental agency), the cancellation fee will be waived.", font5H);
            paragraphRules.AddLineBreak();
            paragraphRules.AddFormattedText("In special circumstances, Yad Vashem reserves the right to cancel/alter a reservation with advance notice to the visitor.", font5H);


            #endregion

            #region signature
            if (data.isNeedToshowSignatureParagraph)
            {
                Paragraph paragraphSig = section.AddParagraph();
                paragraphSig.Format.LineSpacing = 22;
                paragraphSig.Format.LineSpacingRule = LineSpacingRule.AtLeast;
                paragraphSig.Format.Borders.Distance = 8;
                paragraphSig.Format.Borders.Color = Color.Parse("#057cae");
                paragraphSig.Format.Borders.Width = 0.5;
                paragraphSig.Format.RightIndent = 44;
                paragraphSig.Format.LeftIndent = 38;
                paragraphSig.Format.Alignment = ParagraphAlignment.Left;
                paragraphSig.AddFormattedText("This entry voucher must be signed and emailed to ", font9B);
                paragraphSig.AddFormattedText("visit@yadvashem.org.il", font9BLink);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText("Name: ", font9);
                paragraphSig.AddFormattedText("________________________________ ", font9Grey);
                paragraphSig.AddFormattedText(" Job title: ", font9);
                paragraphSig.AddFormattedText("________________________________ ", font9Grey);
                paragraphSig.AddFormattedText(" Date: ", font9);
                paragraphSig.AddFormattedText(" ___________________", font9Grey);
                paragraphSig.AddLineBreak();
                paragraphSig.AddFormattedText("Direct telephone number: ", font9);
                paragraphSig.AddFormattedText(" __________________________ ", font9Grey);
                paragraphSig.AddFormattedText(" Signature and stamp of company: ", font9);
                paragraphSig.AddFormattedText(" ___________________________", font9Grey);
            }
            #endregion

            #region footer

            AddYVFooterTextFrameEn(section.Footers.FirstPage, data, dir);//for firstPage

            #endregion
        }

        #region Help functions

        private void AddLine(Paragraph pLine)
        {
            pLine.Format.Alignment = ParagraphAlignment.Center;
            pLine.Format.Borders.Bottom.Color = Color.Parse("#057cae");
            pLine.Format.Borders.Bottom.Width = 0.5;
            pLine.Format.RightIndent = 8;
            pLine.Format.LeftIndent = 8;
            pLine.Format.SpaceBefore = 0;
            pLine.Format.SpaceAfter = 4;
            pLine.Format.LineSpacingRule = LineSpacingRule.Exactly;
            pLine.Format.LineSpacing = 0;
        }

        private void SetParagraph(Paragraph paragraph, string docLang) {
            if (docLang == "he")
            {
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.Format.RightIndent = 8;
                paragraph.Format.LineSpacing = 18;
            }
            else {
                paragraph.Format.Alignment = ParagraphAlignment.Left;
                paragraph.Format.LeftIndent = 8;
                paragraph.Format.LineSpacing = 16;
            }


            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;

        }

        private Cell MakeBox(TextFrame textFrame, string title, string width, string height, string docLang)
        {
            var font10B = new Font();
            font10B.Size = 10;
            font10B.Bold = true;

            textFrame.MarginLeft = 0;
            Table tableProd = textFrame.AddTable();
            tableProd.AddColumn(width); //"9.2cm"
            Row row = tableProd.AddRow();
            row.VerticalAlignment = VerticalAlignment.Top;
            tableProd.Borders.Color = Color.Parse("#057cae");
            tableProd.Borders.Width = 0.5;
            row.Height = height;//"8.85cm";
            row.HeightRule = RowHeightRule.Exactly;
            Paragraph paragraph = tableProd[0, 0].AddParagraph();

            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.LineSpacing = 14;
            paragraph.Format.SpaceBefore = 8;
            if (docLang == "he") {
                paragraph.Format.RightIndent = 8;
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", title), font10B);
            }
            else {
                paragraph.Format.LeftIndent = 8;
                paragraph.Format.Alignment = ParagraphAlignment.Left;
                paragraph.AddFormattedText(title, font10B);
            }
            AddLine(tableProd[0, 0].AddParagraph());
            return tableProd[0, 0];


        }

        private void AddYVFooterTextFrameHe(HeaderFooter footer, AgencyConfirmationData data, string dir)
        {
            var fontFooter = new Font();
            fontFooter.Size = 7.5;

            var fontFooterWeb = new Font();
            fontFooterWeb.Size = 10;

            var frame = footer.AddTextFrame();
            frame.MarginTop = "1.4cm";
            frame.MarginLeft = 20;
            frame.MarginBottom = 10;
            var table = frame.AddTable();

            table.AddColumn(122);
            table.AddColumn(100);
            table.AddColumn(350);

            var row = table.AddRow();
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].Format.LeftIndent = 12;

            row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

            row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[2].Format.SpaceBefore = 4;
            row.Cells[2].Format.RightIndent = 26;

            Paragraph paragraph = row.Cells[0].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Left;
            var image = paragraph.AddImage(@"Images\footer.jpg");

            paragraph = row.Cells[1].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Left;
            paragraph.Format.SpaceBefore = 6;
            paragraph.AddFormattedText("www.yadvashem.org", fontFooterWeb);

            paragraph = row.Cells[2].AddParagraph();

            paragraph.Format.Alignment = ParagraphAlignment.Right;
            //string date = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            paragraph.AddFormattedText(helperRepository.Reverse(dir, data.ApprovedBy) + helperRepository.Reverse(dir, "אושר על ידי:  ") + " | " + data.UpdatedOn + helperRepository.Reverse(dir, "מועד עדכון:  ") + " | " + data.CreatedOn + helperRepository.Reverse(dir, "מועד ביצוע ההזמנה:  "), fontFooter);


        }

        private void AddYVFooterTextFrameEn(HeaderFooter footer, AgencyConfirmationData data, string dir)
        {
            var fontFooter = new Font();
            fontFooter.Size = 7;

            var fontFooterWeb = new Font();
            fontFooterWeb.Size = 10;

            var frame = footer.AddTextFrame();
            frame.MarginTop = "1.4cm";
            frame.MarginRight = 20;
            frame.MarginBottom = 10;
            var table = frame.AddTable();

            table.AddColumn(360);
            table.AddColumn(100);
            table.AddColumn(108);

            var row = table.AddRow();
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].Format.LeftIndent = 30;
            row.Cells[1].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[1].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[1].Format.SpaceBefore = 4;
            row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[2].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[2].Format.SpaceBefore = 4;


            Paragraph paragraph = row.Cells[0].AddParagraph();
            paragraph.Format.SpaceBefore = 6;
            paragraph.Format.Alignment = ParagraphAlignment.Left;
            //string date = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            paragraph.AddFormattedText("Date of Reservation: " + data.CreatedOn + " | " + "Date of Update: " + data.UpdatedOn + " | " + "Approved by: " + helperRepository.Reverse(dir, data.ApprovedBy), fontFooter);
            paragraph = row.Cells[1].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddFormattedText("www.yadvashem.org", fontFooterWeb);
            paragraph = row.Cells[2].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            var image = paragraph.AddImage(@"Images\footer.jpg");

        }

        private void AddYVOrderNumTextFrame(HeaderFooter header, string orderNum, string docLang)
        {
            var OrderNumFrame = new TextFrame();
            OrderNumFrame = header.AddTextFrame();
            OrderNumFrame.Width = 150;
            OrderNumFrame.Left = ShapePosition.Left;
            OrderNumFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            OrderNumFrame.WrapFormat.DistanceTop = "4.37cm";
            if (docLang == "he")
            {
                OrderNumFrame.WrapFormat.DistanceLeft = "16.7cm";
            }
            else {
                OrderNumFrame.WrapFormat.DistanceLeft = "1cm";
            }
            OrderNumFrame.RelativeVertical = RelativeVertical.Page;

            var paragraph = OrderNumFrame.AddParagraph();
            Font font = new Font();
            font.Size = 14;
            font.Color = Color.Parse("#FFFFFF");
            font.Bold = true;
            paragraph.AddFormattedText(orderNum, font);
        }

        private void SetProductList(Cell cell, List<TourReservationProduct> productList, string docLang, string totalPrice, string totalPriceAfterDiscount)
        {
            TextFrame textFrame = cell.AddTextFrame();
            textFrame.MarginLeft = 10;
            textFrame.MarginRight = 10;
            Table tableProd = textFrame.AddTable();

            if (docLang == "he")
            {
                tableProd.AddColumn("2.5cm");
                tableProd.AddColumn("6cm");
            }
            else {
                tableProd.AddColumn("6cm");
                tableProd.AddColumn("2.5cm");
            }

            foreach (TourReservationProduct product in productList)
            {

                Row row = tableProd.AddRow();
                row.VerticalAlignment = VerticalAlignment.Top;
                row.Format.SpaceAfter = 6;
                row.Cells[0].Format.LineSpacing = 14;
                row.Cells[0].Format.LineSpacingRule = LineSpacingRule.Exactly;
                row.Cells[1].Format.LineSpacing = 14;
                row.Cells[1].Format.LineSpacingRule = LineSpacingRule.Exactly;

                string temp = product.Amount + " " + product.Name;
                if (product.Language != "")
                {
                    temp += " (" + product.Language + ")";
                }
                row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
                row.Cells[1].Format.Alignment = ParagraphAlignment.Right;

                int final = int.Parse(product.Price) * int.Parse(product.Amount);

                if (docLang == "he")
                {
                    row.Cells[0].AddParagraph(final.ToString());
                    row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", temp));
                }
                else
                {
                    //adina 13.7.21 deleted helperRepository.Reverse
                    //devora- 2022-07-18 put it back
                    row.Cells[0].AddParagraph(helperRepository.Reverse("ltr", temp));
                    //row.Cells[0].AddParagraph(temp);
                    row.Cells[1].AddParagraph(final.ToString());
                }

                if (product.DiscountType != "" && product.DiscountType != "0")
                {
                    //product.Discount
                    //product.DiscountType
                    row = tableProd.AddRow();
                    row.VerticalAlignment = VerticalAlignment.Top;
                    row.Format.SpaceAfter = 6;
                    row.Cells[0].Format.LineSpacing = 14;
                    row.Cells[0].Format.LineSpacingRule = LineSpacingRule.Exactly;
                    row.Cells[1].Format.LineSpacing = 14;
                    row.Cells[1].Format.LineSpacingRule = LineSpacingRule.Exactly;
                    row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
                    row.Cells[1].Format.Alignment = ParagraphAlignment.Right;

                    decimal discount = final - decimal.Parse(product.TotalPriceAfterDiscount);

                    if (docLang == "he")
                    {
                        row.Cells[0].AddParagraph("-" + discount.ToString());
                        row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "הנחה"));
                    }
                    else
                    {
                        row.Cells[0].AddParagraph("Discount");
                        row.Cells[1].AddParagraph("-" + discount.ToString());
                    }
                }

               
            }

            /*
             *   calcRowTotalPriceAfterDiscount(row: FormGroup) {
    const totalPrice = row.get('rowTotalPrice').value;
    const discountType = row.get('rowDiscountType').value;
    const discount = row.get('rowDiscount').value;
    row.get('rowTotalPriceAfterDiscount').setValue(
      totalPrice
    );
    if (+discountType.id === DiscountType.Sum || +discountType.id === DiscountType.MinistryOfEducation) {
      row.get('rowTotalPriceAfterDiscount').setValue(
        totalPrice - discount
      );
    } else if (+discountType.id === DiscountType.Percent) {
      row.get('rowTotalPriceAfterDiscount').setValue(
        totalPrice * ((100 - discount) / 100)
      );
    }

  }
             * */

            //TOTAL PRICE:
            Row rowT = tableProd.AddRow();
            rowT.VerticalAlignment = VerticalAlignment.Top;
            rowT.Format.SpaceBefore = 24;
            rowT.Borders.Top.Color = Color.Parse("#057cae");
            rowT.Borders.Top.Width = 0.5;

            rowT.Format.SpaceAfter = 6;
            rowT.Cells[0].Format.LineSpacing = 14;
            rowT.Cells[0].Format.LineSpacingRule = LineSpacingRule.Exactly;
            rowT.Cells[1].Format.LineSpacing = 14;
            rowT.Cells[1].Format.LineSpacingRule = LineSpacingRule.Exactly;

            rowT.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            rowT.Cells[1].Format.Alignment = ParagraphAlignment.Right;

            if (docLang == "he")
            {
                rowT.Cells[0].AddParagraph("₪" + totalPrice);
                rowT.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "סך הכל"));
            }
            else
            {
                rowT.Cells[0].AddParagraph(helperRepository.Reverse("ltr", "Total"));
                rowT.Cells[1].AddParagraph(totalPrice + "₪");
            }

            //Total After Discount:
            rowT = tableProd.AddRow();
            rowT.VerticalAlignment = VerticalAlignment.Top;
            rowT.Format.SpaceAfter = 6;
            rowT.Cells[0].Format.LineSpacing = 14;
            rowT.Cells[0].Format.LineSpacingRule = LineSpacingRule.Exactly;
            rowT.Cells[1].Format.LineSpacing = 14;
            rowT.Cells[1].Format.LineSpacingRule = LineSpacingRule.Exactly;

            rowT.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            rowT.Cells[1].Format.Alignment = ParagraphAlignment.Right;

            if (docLang == "he")
            {
                rowT.Cells[0].AddParagraph("₪" + totalPriceAfterDiscount);
                rowT.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "סך הכל לאחר הנחה"));
            }
            else
            {
                rowT.Cells[0].AddParagraph(helperRepository.Reverse("ltr", "Total After Discount"));
                rowT.Cells[1].AddParagraph(totalPriceAfterDiscount + "₪");
            }
            Table tableB = textFrame.AddTable();
            tableB.AddColumn("8.5cm");
            Row rowC = tableB.AddRow();

            rowC.VerticalAlignment = VerticalAlignment.Top;
            rowC.Format.SpaceBefore = 50;
            rowC.Borders.Top.Color = Color.Parse("#057cae");
            rowC.Borders.Top.Width = 0.5;

            rowC.Format.SpaceAfter = 6;
            rowC.Cells[0].Format.LineSpacing = 14;
            rowC.Cells[0].Format.LineSpacingRule = LineSpacingRule.Exactly;

            
            
            //if (docLang == "he")
            //{
            //    rowC.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            //    rowC.Cells[0].AddParagraph(helperRepository.Reverse("rtl", "כולל כניסה למיצב החדש - ספר שמות קרבנות השואה"));
            //}
            //else
            //{
            //    rowC.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            //    rowC.Cells[0].AddParagraph("Includes entrance to the new installation: The Book of Names of Holocaust Victims.");
            //}
        }

        #endregion

    }
}
