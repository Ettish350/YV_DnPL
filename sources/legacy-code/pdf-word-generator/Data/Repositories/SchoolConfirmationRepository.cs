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
    public class SchoolConfirmationRepository : ISchoolConfirmationRepository
    {
        private IHelperRepository helperRepository;

        public SchoolConfirmationRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;
           
        }
        IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();


        public string CreateDocument(Document document, SchoolConfirmationData data)
        {
            try
            {

                Log.Error("in Confirmation repository create document.  ");
                document.Styles[StyleNames.Normal].Font.Name = "Arial";

                document.Info.Title = "הצעת מחיר " + data.OrderNum;
                document.Info.Subject = "Confirmation 222";
                document.Info.Author = "Confirmation 333";



                switch (data.DocLang)
                {
                    case "עברית":
                    case "he":
                        {
                            Log.Error("lang heb");
                            CreatePageAndFillContentheb(document, data);
                            break;
                        }
                    case "אנגלית":
                    case "en":
                        {
                            Log.Error("lang eng");
                            CreatePageAndFillContenteng(document, data);
                            break;
                        }
                    default:
                        {
                            Log.Error("no lang");
                            CreatePageAndFillContentheb(document, data);
                            break;
                        }

                }
                Log.Error("go to save doc");
                string file_path = helperRepository.SaveDoc(document, data.OrderNum + DateTime.Now.ToString("mm-ss"), "Confirmation", "Confirmation");
                Log.Error("after save doc");
                return file_path;

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "exception in invoice repository create document");
                throw ex;
            }
        }

        private void CreatePageAndFillContentheb(Document document, SchoolConfirmationData data)
        {
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(6.5);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(2.7);
            document.DefaultPageSetup.Orientation = Orientation.Portrait;

            #region headers and date
            // Each MigraDoc document needs at least one section.
            Section section = document.AddSection();

            section.PageSetup.DifferentFirstPageHeaderFooter = true;
            section.PageSetup.HeaderDistance = 0;
            section.PageSetup.FooterDistance = 12;

            Paragraph paragraph = section.AddParagraph();
            Row row;

            //first page header
            section.Headers.FirstPage.AddImage(@"Images\confirmation_banner_heb.jpg");
            AddYVOrderNumTextFrame(section.Headers.FirstPage, data.OrderNum, "right");

            //other pages header
            var frameHeader = section.Headers.Primary.AddTextFrame();
            frameHeader.AddImage(@"Images\confirmation_banner_next_page_heb.jpg");


            frameHeader = section.Headers.Primary.AddTextFrame();
            frameHeader.MarginLeft = 35;
            frameHeader.Width = 150;
            paragraph = frameHeader.AddParagraph();
            paragraph.AddNumPagesField();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", " מתוך "), YVFonts.fontSmall);
            paragraph.AddPageField();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "המשך - דף "), YVFonts.fontSmall);

            AddYVOrderNumTextFrame(section.Headers.Primary, data.OrderNum, "right");
            frameHeader.MarginBottom = 20;

            TextFrame dateFrame = new TextFrame();
            dateFrame = section.AddTextFrame();
            dateFrame.Width = "5.0cm";
            dateFrame.Left = ShapePosition.Left;
            dateFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            dateFrame.Top = "2.0cm";
            dateFrame.RelativeVertical = RelativeVertical.Page;
            string date = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            paragraph = dateFrame.AddParagraph();
            paragraph.AddFormattedText(date + helperRepository.Reverse("rtl", "תאריך הזמנה:   "), YVFonts.font10);
            paragraph.Format.Alignment = ParagraphAlignment.Right;


            TextFrame toFrame = new TextFrame();
            toFrame = section.AddTextFrame();
            toFrame.Left = ShapePosition.Left;
            toFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            toFrame.WrapFormat.DistanceTop = "4cm";
            toFrame.WrapFormat.DistanceLeft = "3.8cm";
            toFrame.RelativeVertical = RelativeVertical.Page;

            Table table = toFrame.AddTable();
            table.AddColumn(200);
            table.AddColumn(30);
            table.AddRow();
            table.LeftPadding = 0;
            table.RightPadding = 0;


            paragraph = table[0, 1].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", " לכבוד:  "), YVFonts.font10);


            paragraph = table[0, 0].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.ContactName), YVFonts.font10);
            paragraph.AddLineBreak();
            helperRepository.AddToTextSplitToLines("rtl", paragraph, data.InstitutionName, 45, YVFonts.font10);

            //paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionName), YVFonts.font10);
            paragraph.AddLineBreak();

            if (data.InstitutionAddress.Trim() != string.Empty)
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionAddress), YVFonts.font10);

            }
            paragraph.AddLineBreak();

            if (data.InstitutionCity.Trim() != string.Empty)
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionCity), YVFonts.font10);

            }
            paragraph.AddLineBreak();
            if (data.Fax.Trim() != string.Empty)
            {
                paragraph.AddFormattedText(data.Fax + helperRepository.Reverse("rtl", "פקס "), YVFonts.font10);
            }
            //paragraph.AddLineBreak();


            table = toFrame.AddTable();
            table.AddColumn(500);
            table.AddRow();
            table.TopPadding =0;
            table.RightPadding = 130;
            paragraph = table[0,0].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "אישור זה מהווה אסמכתא סופית להזמנתך."), YVFonts.font12B);



            #endregion

            #region first part
            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = "0.2cm";
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.LineSpacing = 10;
            paragraph.Format.RightIndent = 34;
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "שלום רב,"), YVFonts.fontBig);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הרינו מאשרים את הזמנתכם:"), YVFonts.fontBig);
            paragraph.AddLineBreak();
            if (data.tourName != "")
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.tourName) + helperRepository.Reverse("rtl", "סוג סיור: "), YVFonts.font11);
            }
            paragraph.AddLineBreak();




            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = "0.08cm";
            paragraph.Format.RightIndent = 33;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.LineSpacing = 15;

            table = section.AddTable();
            table.TopPadding = 2;
            table.Format.Alignment = ParagraphAlignment.Right;
            table.AddColumn(225);
            table.AddColumn(345);
            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Bottom;
            row.Format.Alignment = ParagraphAlignment.Right;
            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Bottom;
            row.Format.Alignment = ParagraphAlignment.Right;

            paragraph = table[0, 0].AddParagraph();
            if (!string.IsNullOrEmpty(data.GroupName))
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.GroupName) + helperRepository.Reverse("rtl", "שם ההזמנה: "), YVFonts.font11);

            }
            paragraph = table[0, 1].AddParagraph();
            if (!string.IsNullOrEmpty(data.InstitutionName)){
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionName) + helperRepository.Reverse("rtl", "שם המזמין: "), YVFonts.font11);
            }
            else
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.ContactName) + helperRepository.Reverse("rtl", "שם המזמין: "), YVFonts.font11);

            }
            row=table.AddRow();
            paragraph = row.Cells[1].AddParagraph();

            if(!string.IsNullOrEmpty(data.GefenType))
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.GefenType) + helperRepository.Reverse("rtl", "סוג תוכנית גפן: "), YVFonts.font11);

            }
            //paragraph = table[0, 1].AddParagraph();
            //paragraph.Format.Shading.Color = Color.Parse("#E5E8F3");
            //paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.GroupName) + helperRepository.Reverse("rtl", "שם ההזמנה: "), YVFonts.font11);
            paragraph = table[1, 0].AddParagraph();
            if (!string.IsNullOrEmpty(data.VisitLang))
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.VisitLang) + helperRepository.Reverse("rtl", "שפה: "), YVFonts.font11);

            }
            paragraph = table[1, 1].AddParagraph();
            paragraph.AddFormattedText(data.NumberOfParticipants + helperRepository.Reverse("rtl", "מס משתתפים: "), YVFonts.font11);
            paragraph.AddSpace(6);
            if (data.NumberOfGroups!=null&&data.NumberOfGroups != ""&& data.NumberOfGroups!="0")
            {
                paragraph.AddFormattedText(data.NumberOfGroups + helperRepository.Reverse("rtl", "מס קבוצות: "), YVFonts.font11);
               
            }
            if (!string.IsNullOrEmpty(data.Grade) && !string.IsNullOrEmpty(data.AgeGroup))
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.Grade) + helperRepository.Reverse("rtl", "כיתה: "), YVFonts.font11);
                paragraph.AddSpace(6);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.AgeGroup) + helperRepository.Reverse("rtl", "שכבת גיל: "), YVFonts.font11);
            
            }

           


            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddLineBreak();
            paragraph.Format.RightIndent = 33;

            int counter = 1;
            table = section.AddTable();
            if (data.ActivityDayList.Count > 1)
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "תאריכי הביקור:"), YVFonts.font11);
                paragraph.AddLineBreak();
                table.AddColumn(190);
                table.AddColumn(130);
                table.AddColumn(150);
                table.AddColumn(100);
            }
            else
            {

                table.AddColumn(230);
                table.AddColumn(160);
                table.AddColumn(180);
                
            }
            table.TopPadding = 4;
            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Bottom;
            row.Format.Alignment = ParagraphAlignment.Right;

            if (data.ActivityDayList.Count ==1)
            {
                var tempDay = data.ActivityDayList[0];
                paragraph = row.Cells[0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", tempDay.Location) + helperRepository.Reverse("rtl", "מקום הפעילות: "), YVFonts.font11);
                paragraph = row.Cells[1].AddParagraph();
                paragraph.AddFormattedText(tempDay.HouresFromTo + helperRepository.Reverse("rtl", "שעות הפעילות: "), YVFonts.font11);
                //if (!string.IsNullOrEmpty(tempDay.MuseumEntryTime))
                //{
                //    paragraph = row.Cells[2].AddParagraph();
                //    paragraph.AddFormattedText(helperRepository.Reverse("rtl", tempDay.MuseumEntryTime) + helperRepository.Reverse("rtl", "שעת כניסה למוזיאון: "), YVFonts.font11);
                //}
                paragraph = row.Cells[2].AddParagraph();
                paragraph.AddFormattedText(tempDay.Date + helperRepository.Reverse("rtl", "תאריך הביקור: "), YVFonts.font11);
            }
            else
            {
                foreach (var activityDay in data.ActivityDayList)
                {
                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", activityDay.NumberOfGroups) + helperRepository.Reverse("rtl", "מספר קבוצות: "), YVFonts.font11);
                    paragraph = row.Cells[1].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", activityDay.Location) + helperRepository.Reverse("rtl", "מקום הפעילות: "), YVFonts.font11);
                    paragraph = row.Cells[2].AddParagraph();
                    paragraph.AddFormattedText(activityDay.HouresFromTo + helperRepository.Reverse("rtl", "שעות הפעילות: "), YVFonts.font11);
                    paragraph = row.Cells[3].AddParagraph();
                    paragraph.AddFormattedText(activityDay.Date + " ." + counter.ToString(), YVFonts.font11);
                    counter++;
                }
            }



            paragraph = section.AddParagraph();
            paragraph.Format.RightIndent = 33;
            //paragraph.Format.Alignment = ParagraphAlignment.Right;
            //paragraph.Format.SpaceBefore = "0.2cm";
            //paragraph.AddFormattedText(data.DepartmentEmail + helperRepository.Reverse("rtl", " או למייל ") + data.DepartmentFax + helperRepository.Reverse("rtl", "לידיעתכם, ההזמנה תקלט ותאושר במערכת רק לאחר חתימה על הטופס והחזרתו לפקס מס "), YVFonts.fontSmall);
            //paragraph.Format.SpaceAfter = "0.5cm";
            #endregion

            #region first table

            table = AddYVTable(section, ParagraphAlignment.Right);
            AddYVRows(table, 7, ParagraphAlignment.Right);

            paragraph = table[0, 0].AddParagraph();

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כללי התנהגות במוזיאון:"), YVFonts.fontMedium);

            paragraph = table[1, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הכניסה ליד ושם בלבוש הולם ומכובד."), YVFonts.fontSmall);
            paragraph = table[2, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "אין להכניס מזון, שתיה, תיקים וכן אסור לצלם ולעשן."), YVFonts.fontSmall);
            paragraph = table[3, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הכנסת מערכות שמע להדרכה שאינן של יד ושם בתחומי הקמפוס אסורה."), YVFonts.fontSmall);
            paragraph = table[4, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "עם ההגעה לאתר, יש להציג את טופס האישור הקבוצתי לנציג יד ושם."), YVFonts.fontSmall);
            paragraph = table[5, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הכניסה למוזיאון לתולדות השואה תותר עד שעה לפני מועד סגירת האתר."), YVFonts.fontSmall);
            paragraph = table[6, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הכניסה למוזיאון לתולדות השואה מגיל 10 ומעלה בהצגת תעודה מזהה. לא תתאפשר כניסת ילדים מתחת לגיל זה."), YVFonts.fontSmall);
            paragraph = table[7, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "אין כניסה לתינוקות בעגלה או במנשא."), YVFonts.fontSmall);

            #endregion

            #region second table
            section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables

            table = AddYVTable(section, ParagraphAlignment.Right);
            AddYVRows(table, 7, ParagraphAlignment.Right);

            paragraph = table[0, 0].AddParagraph();

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "תנאים לשינוי וביטול:"), YVFonts.fontMedium);


            paragraph = table[1, 0].AddParagraph();
            paragraph.AddFormattedText("https://forms.yadvashem.org/he/reservations-edit", YVFonts.fontSmall);
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "לביטול או שינוי ההזמנה יש להיכנס לאתר יד ושם "), YVFonts.fontSmall);
            paragraph = table[2, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "לא יינתן החזר עבור דמי הרשמה וטיפול להזמנה מבוטלת בכל שלב ומקרה."), YVFonts.fontSmall);
            paragraph = table[3, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "ניתן לשנות תאריך הזמנה פעם אחת ללא תשלום נוסף עבור דמי הרשמה וטיפול."), YVFonts.fontSmall);
            paragraph = table[3, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "ביטול/ שינוי סיור יתאפשר עד 3 ימי עבודה לא כולל ימי שישי וערבי חג, לפני מועד הביקור המתוכנן, באמצעות אתר יד ושם .") , YVFonts.fontSmall);
            paragraph = table[4, 0].AddParagraph();
            paragraph.AddFormattedText(data.DepartmentEmail, YVFonts.fontSmall);
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", " או באמצעות כתובת המייל "), YVFonts.fontSmall);

            paragraph = table[5, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "ביטול/ שינוי שלא יתבצע בהתאם לתנאים אלו יחוייב ב % 100 מגובה החיוב בגין השימוש בשירותים השונים."), YVFonts.fontSmall);
            paragraph = table[6, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "במקרה של ביטול ביום הפעילות מחמת כח עליון (מצב חירום עפ“י גורם ממשלתי מוסמך) המזמין לא יחויב בתשלום."), YVFonts.fontSmall);
            paragraph = table[7, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "בנסיבות מיוחדות, יד ושם שומר לעצמו את הזכות לבטל / לשנות את ההזמנה בהודעה מוקדמת ללקוח."), YVFonts.fontSmall);

            #endregion
            string totalToPay = data.TotalPriceAfterDiscount != string.Empty ? data.TotalPriceAfterDiscount : data.Total;

            #region third table
            if (data.isNeedToShowSignatureParagraph)
            {
                section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables


                table = AddYVTable(section, ParagraphAlignment.Right);
                AddYVRows(table, 9, ParagraphAlignment.Right);

                paragraph = table[0, 0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "אישור והתחייבות המזמין:"), YVFonts.fontMedium);
                if (data.ShowDetails == false)
                {
                    paragraph = table[2, 0].AddParagraph();

                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "קראתי וידועים לי תנאי התשלום, נהלי הביטול וההנחיות המופיעות בהזמנה זו:"), YVFonts.fontSmall);

                }
                else
                {
                    paragraph = table[1, 0].AddParagraph();

                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "(מצ“ב פירוט כספי)"), YVFonts.fontSmall);

                    paragraph.AddSpace(2);
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(totalToPay) + helperRepository.Reverse("rtl", "סה“כ עלות הפעילות שאנו מזמינים ומתחייבים לשלם:  "), YVFonts.font12B);

                    paragraph = table[2, 0].AddParagraph();

                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כמו כן, קראתי וידועים לי תנאי התשלום, נהלי הביטול וההנחיות המופיעות בהזמנה זו:"), YVFonts.fontSmall);
                }
                paragraph = table[3, 0].AddParagraph();





                paragraph.AddFormattedText("_____________________________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "תפקיד המאשר:"), YVFonts.fontSmall);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("_____________________________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "שם המאשר:"), YVFonts.fontSmall);

                paragraph = table[4, 0].AddParagraph();

                paragraph.AddFormattedText("____________________________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "חתימת המאשר:"), YVFonts.fontSmall);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("____________________________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "טלפון המאשר:"), YVFonts.fontSmall);

                paragraph = table[5, 0].AddParagraph();
                paragraph.AddFormattedText("____________________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "מס' הזמנת רכש (במידה ויש):"), YVFonts.fontSmall);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("_____________________________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כתובת מייל:"), YVFonts.fontSmall);

                paragraph = table[6, 0].AddParagraph();
                paragraph.Format.SpaceBefore = "0.3cm";
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", " *במידה וגורם אחר שאינו המזמין ישלם עבור הזמנתכם, יש להחתימו על מסמך זה."), YVFonts.fontSmall);

                paragraph = table[7, 0].AddParagraph();
                paragraph.AddFormattedText("__________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "טלפון") + " .3", YVFonts.fontSmall);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("__________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "ח.פ ") + " .2", YVFonts.fontSmall);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("____________________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "שם הגורם המשלם") + " .1", YVFonts.fontSmall);

                paragraph = table[8, 0].AddParagraph();
                paragraph.AddFormattedText("_________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "חתימה") + " .6", YVFonts.fontSmall);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("____________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "תפקיד המאשר") + " .5", YVFonts.fontSmall);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("_______________________", YVFonts.fontLine);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "שם המאשר") + " .4", YVFonts.fontSmall);

                paragraph = table[9, 0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", " - רק לאחר קבלת הזמנת רכש נוכל לאפשר פעילות"), YVFonts.fontSmall);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "*כוחות הביטחון"), YVFonts.font9B);
            }
            #endregion

            #region some text

            //paragraph = section.AddParagraph();
            //paragraph.Format.Alignment = ParagraphAlignment.Right;
            //paragraph.Format.SpaceBefore = "0.3cm";
            //paragraph.Format.RightIndent = 33;
            //paragraph.AddFormattedText(data.OrderNum+helperRepository.Reverse("rtl", " פירוט עלות כספית להזמנה מס: "), YVFonts.fontBig);

            #endregion

          
            #region activity day tables


            bool isFirstTable = true;
            //firt table should have a header row for title

            if (data.ShowDetails)
            {
                foreach (var activityDay in data.ActivityDayList)
                {

                    //
                    section.AddParagraph().Format.SpaceBefore = 1;//space before table
                    table = section.AddTable();
                    table.TopPadding = 1;
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

                    if (isFirstTable)
                    {

                        row = table.AddRow();
                        row.Cells[0].MergeRight = 5;
                        row.BottomPadding = 3;
                        paragraph = row.Cells[0].AddParagraph();
                        paragraph.Format.Alignment = ParagraphAlignment.Right;
                        paragraph.AddFormattedText(data.OrderNum + helperRepository.Reverse("rtl", " פירוט עלות כספית להזמנה מס: "), YVFonts.fontBig);
                        if (data.creditCard4Digit != "")
                        {
                            paragraph.AddFormattedText(data.creditCardBrand + " ************" + data.creditCard4Digit + helperRepository.Reverse("rtl", "שולם עם: "), YVFonts.fontBig);
                        }


                        //if (data.IsFromOnlineReservation)
                        //{
                        //    paragraph = row.Cells[0].AddParagraph();
                        //    paragraph.Format.Alignment = ParagraphAlignment.Left;
                        //    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "המחיר עבור שירות ההדרכה בלבד ולא כולל פעולות נוספות בתשלום שיתואמו ע''י הרכז."), YVFonts.fontRed);
                        //}

                    }

                    row = table.AddRow();

                    row.HeadingFormat = true;
                    row.Height = "0.4cm";
                    row.Cells[0].Format.SpaceBefore = 3;
                    row.Cells[0].Format.SpaceAfter = 3;
                    row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                    row.Cells[0].MergeRight = 5;


                    row.Format.Alignment = ParagraphAlignment.Right;
                    row.Shading.Color = YVFonts.shadingColor;// Color.Parse("#E0E8F3");
                                                             //
                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.AddFormattedText(activityDay.Date + helperRepository.Reverse("rtl", "פירוט לתאריך ביקור "), YVFonts.fontMedium);

                    if (activityDay.detailsList != null && activityDay.detailsList.Count > 0)
                    {
                        //this line makes sure that the table will not split in the middle, but will all be in one page
                        if (isFirstTable)
                        {
                            table.Rows[0].KeepWith = 3 + activityDay.detailsList.Count;
                        }
                        else
                        {
                            table.Rows[0].KeepWith = 2 + activityDay.detailsList.Count;

                        }


                        //add table headers
                        row = table.AddRow();

                        paragraph = row.Cells[5].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הפריט"), YVFonts.fontTableColumnsHeader);
                        paragraph = row.Cells[4].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", "עלות ליח'"), YVFonts.fontTableColumnsHeader);

                        paragraph = row.Cells[3].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כמות"), YVFonts.fontTableColumnsHeader);
                        paragraph = row.Cells[2].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ"), YVFonts.fontTableColumnsHeader);
                        paragraph = row.Cells[1].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הנחה"), YVFonts.fontTableColumnsHeader);
                        paragraph = row.Cells[0].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ לאחר הנחה"), YVFonts.fontTableColumnsHeader);
                        foreach (var details in activityDay.detailsList)
                        {
                            row = table.AddRow();

                            paragraph = row.Cells[5].AddParagraph();
                            //אם יש ערך בשדה הערה- אז לשים אותו, אחרת לשים את שם הפריט
                            if (details.Comments.Trim() != string.Empty)
                            {
                                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.Comments), YVFonts.fontSmall);
                            }
                            else
                            {
                                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.ItemName), YVFonts.fontSmall);
                            }

                            paragraph = row.Cells[4].AddParagraph();
                            paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.PricePerUnit), YVFonts.fontSmall);

                            paragraph = row.Cells[3].AddParagraph();
                            paragraph.AddFormattedText(details.Amount, YVFonts.fontSmall);

                            paragraph = row.Cells[2].AddParagraph();
                            paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.Total), YVFonts.fontSmall);

                            paragraph = row.Cells[1].AddParagraph();
                            if (!string.IsNullOrEmpty(details.Discount) && details.Discount != "0")
                            {
                                paragraph.AddFormattedText(helperRepository.DiscountWithSymbol(details.Discount, details.DiscountType), YVFonts.fontSmall);

                            }
                            paragraph = row.Cells[0].AddParagraph();
                            paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.TotalAfterDiscount), YVFonts.fontSmall);


                        }
                        //last row
                        row = table.AddRow();
                        row.Format.Alignment = ParagraphAlignment.Right;
                        row.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
                        paragraph = row.Cells[0].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.TotalAfterDiscount), YVFonts.fontTableColumnsHeader);

                        //  row.Cells[1].MergeRight = 1;
                        paragraph = row.Cells[2].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.Total), YVFonts.fontTableColumnsHeader);

                        // row.Cells[3].MergeRight = 2;
                        paragraph = row.Cells[5].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ"), YVFonts.fontTableColumnsHeader);




                        int innerBorderFromRowNumber = 2;
                        int centerTextFromRowNumber = 1;

                        if (isFirstTable)
                        {
                            innerBorderFromRowNumber = 3;
                            centerTextFromRowNumber = 2;
                            table.SetEdge(0, 1, table.Columns.Count, table.Rows.Count - 1, Edge.Box, BorderStyle.Single, 0.5, YVFonts.borderColor);


                        }
                        else
                        {
                            table.SetEdge(0, 0, table.Columns.Count, table.Rows.Count, Edge.Box, BorderStyle.Single, 0.5, YVFonts.borderColor);

                        }
                        table.SetEdge(0, innerBorderFromRowNumber, table.Columns.Count, table.Rows.Count - 3, Edge.Horizontal, BorderStyle.Single, 0.5, Color.Parse("#BDBEC1"));
                        table.LeftPadding = 5;
                        table.RightPadding = 5;
                        table.TopPadding = 5;

                        //loop all table
                        {
                            for (int i = 0; i < table.Columns.Count; i++)
                            {
                                for (int j = centerTextFromRowNumber; j < table.Rows.Count; j++)//start from second row
                                {
                                    Cell cell = table[j, i];
                                    cell.Format.Alignment = ParagraphAlignment.Right;
                                    cell.VerticalAlignment = VerticalAlignment.Center;


                                }
                            }
                        }

                    }
                    isFirstTable = false;
                }
            }
            #endregion

            if (data.ShowSchedule || data.ShowScheduleFirstGroup)
            {

                for (int i = 0; i < data.ScheduleData.reservations.Count; i++)
                {
                    bool ismultiReservation = data.ScheduleData.reservations.Count > 1 ? true : false;
                    BuildReservationInDocx(document, data.ScheduleData.reservations[i], ismultiReservation, i, data.ScheduleData.serviceProvider, data.ShowScheduleFirstGroup);
                    if (i < data.ScheduleData.reservations.Count - 1 && data.ScheduleData.serviceProvider == "")//not last page
                    {
                        document.AddSection().AddParagraph().AddLineBreak();

                    }
                    else if (data.ScheduleData.serviceProvider != "")
                    {
                        document.AddSection().AddParagraph().AddFormattedText(helperRepository.Reverse("rtl", "*********************************"), YVFonts.fontSmall);

                        //document.AddSection().AddParagraph().AddFormattedText("*********************************");
                        //p.Direction = Xceed.Document.NET.Direction.RightToLeft;

                    }
                }

            }


            #region Total


            if (data.ShowDetails)
            {
                string totalLine;
                if (data.TotalPriceAfterDiscount != "" && data.Discount != "" && data.Discount != "0")// there is a discount on the whole order
                {

                    table = section.AddTable();
                    table.Rows.LeftIndent = 33;
                    table.LeftPadding = 0;
                    table.RightPadding = 0;
                    table.Format.Borders.DistanceFromBottom = 3;
                    table.Format.Borders.DistanceFromTop = 3;

                    table.AddColumn(77);
                    table.AddColumn(8);
                    table.AddColumn(445);

                    row = table.AddRow();
                    row.TopPadding = 15;

                    row = table.AddRow();
                    row.BottomPadding = 5;

                    row = table.AddRow();
                    row.Format.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");

                    //first row
                    paragraph = table.Rows[0][0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(data.Total), YVFonts.font10);
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph = table.Rows[0][2].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ עלות הזמנה :  "), YVFonts.font10);

                    //second row
                    paragraph = table.Rows[1][0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.DiscountWithSymbol(data.Discount, data.DiscountType), YVFonts.font10);
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph = table.Rows[1][2].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", " הנחה: "), YVFonts.font10);

                    //third row
                    paragraph = table.Rows[2][0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(data.TotalPriceAfterDiscount), YVFonts.fontTotalWithDiscount);
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph = table.Rows[2][1].AddParagraph();
                    paragraph.AddFormattedText(" ", YVFonts.fontTotalWithDiscount);
                    paragraph = table.Rows[2][2].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", " סה\"כ לאחר הנחה: "), YVFonts.fontTotalWithDiscount);

                }
                else
                {
                    paragraph = section.AddParagraph();
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph.Format.SpaceBefore = 15;
                    paragraph.Format.LeftIndent = 33;
                    paragraph.Format.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
                    totalLine = helperRepository.WithCurrencySymbol(totalToPay) + " : " + data.OrderNum + helperRepository.Reverse("rtl", " סה\"כ עלות הזמנה מספר ") + "                                    ";
                    paragraph.Format.RightIndent = 33;
                    paragraph.AddFormattedText(totalLine, YVFonts.fontTotal);
                }
            }

            #endregion

            #region Customer comments


            if (data.ShowCustomerComments && !string.IsNullOrEmpty(data.CustomerComments))
            {
                string[] strings = data.CustomerComments.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables
                table = helperRepository.AddYVTable(section, ParagraphAlignment.Right);
                table.RightPadding = "0.5cm";
                helperRepository.AddYVRows(table, strings.Length + 1, ParagraphAlignment.Right);
                row = table.Rows[0];
                row.BottomPadding = 10;
                paragraph = row.Cells[0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הערות:"), YVFonts.fontBig);


                int rowNum = 1;
                foreach (string str in strings)
                {
                    row = table.Rows[rowNum];
                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", str), YVFonts.font11);
                    rowNum++;
                }
                table.Rows[0].KeepWith = table.Rows.Count - 1;
            }




            #endregion

            #region food comment

            //section.AddParagraph().Format.SpaceBefore = 2;//space before table
            //paragraph = section.AddParagraph();

            //string food_comment_1 = "במידה והנך מעוניין בכיבוד/אוכל או שירותי הסעדה ומזון, ניתן לצור קשר עם";
            //string food_comment_2 = "מאיר קיסלסי <REDACTED-PHONE> / אריה קיסלסי <REDACTED-PHONE> / קפיטריה 026410582.מייל <REDACTED-EMAIL>";
            //paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            //paragraph.Format.LineSpacing = 15;
            //paragraph.Format.Alignment = ParagraphAlignment.Right;
            //paragraph.Format.RightIndent = 34;
            //paragraph.AddFormattedText(helperRepository.Reverse("rtl", food_comment_1) , YVFonts.font11);
            //paragraph.AddLineBreak();
            //paragraph.AddFormattedText(helperRepository.Reverse("rtl", food_comment_2), YVFonts.font11);

            #endregion

            #region footer

            AddYVFooterTextFrame(section.Footers.FirstPage, data);//for firstPage


            AddYVFooterTextFrame(section.Footers.Primary, data);//for all other pages



            #endregion
        }

        private void CreatePageAndFillContenteng(Document document, SchoolConfirmationData data)
        {
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(6.5);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(2.7);
            document.DefaultPageSetup.Orientation = Orientation.Portrait;


            #region headers and date
            // Each MigraDoc document needs at least one section.
            Section section = document.AddSection();



            section.PageSetup.DifferentFirstPageHeaderFooter = true;
            section.PageSetup.HeaderDistance = 0;
            section.PageSetup.FooterDistance = 12;

            Paragraph paragraph = section.AddParagraph();
            Row row;

            //first page header

            section.Headers.FirstPage.AddImage(@"Images\confirmation_banner_eng.jpg");
            AddYVOrderNumTextFrame(section.Headers.FirstPage, data.OrderNum,"left");

            //other pages header
            var frameHeader = section.Headers.Primary.AddTextFrame();
            frameHeader.AddImage(@"Images\confirmation_banner_next_page_eng.jpg");


            frameHeader = section.Headers.Primary.AddTextFrame();
            frameHeader.MarginLeft = "17cm";
            frameHeader.Width = "23cm";
            paragraph = frameHeader.AddParagraph();


            paragraph.AddFormattedText("Page ", YVFonts.fontSmall);
            paragraph.AddPageField();
            paragraph.AddFormattedText(" of ", YVFonts.fontSmall);
            paragraph.AddNumPagesField();

            AddYVOrderNumTextFrame(section.Headers.Primary, data.OrderNum,"left");
            frameHeader.MarginBottom = 20;

            TextFrame dateFrame = new TextFrame();
            dateFrame = section.AddTextFrame();
            dateFrame.Width = "6.2cm";
            dateFrame.Left = ShapePosition.Right;
            dateFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            dateFrame.Top = "2.0cm";
            //dateFrame.MarginRight = 33;
            dateFrame.RelativeVertical = RelativeVertical.Page;
            string date = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            paragraph = dateFrame.AddParagraph();
            paragraph.AddFormattedText("Date of Reservation:" + date, YVFonts.font10);



            TextFrame toFrame = new TextFrame();
            toFrame = section.AddTextFrame();
            toFrame.Left = ShapePosition.Left;
            toFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            toFrame.WrapFormat.DistanceTop = "3.5cm";
            toFrame.WrapFormat.DistanceLeft = "10cm";
            toFrame.RelativeVertical = RelativeVertical.Page;

            Table table = toFrame.AddTable();

            table.AddColumn(15);
            table.AddColumn(200);
            table.AddRow();
            table.LeftPadding = 0;
            table.RightPadding = 0;


            paragraph = table[0, 0].AddParagraph();

            paragraph.AddFormattedText("To: ", YVFonts.font10);


            paragraph = table[0, 1].AddParagraph();

            paragraph.AddFormattedText(helperRepository.Reverse("ltr", data.ContactName), YVFonts.font10);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse("ltr", data.InstitutionName), YVFonts.font10);



            paragraph.AddLineBreak();

            if (data.InstitutionAddress.Trim() != string.Empty)
            {
                paragraph.AddFormattedText(helperRepository.Reverse("ltr", data.InstitutionAddress), YVFonts.font10);

            }
            paragraph.AddLineBreak();

            if (data.InstitutionCity.Trim() != string.Empty)
            {
                paragraph.AddFormattedText(helperRepository.Reverse("ltr", data.InstitutionCity), YVFonts.font10);

            }
            paragraph.AddLineBreak();

            if (data.Fax.Trim() != string.Empty)
            {
                paragraph.AddFormattedText("Fax: " + data.Fax, YVFonts.font10);
            }
            paragraph.AddLineBreak();

            toFrame = section.AddTextFrame();
            toFrame.Left = ShapePosition.Center;
            toFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            toFrame.WrapFormat.DistanceTop = "5.85cm";
            toFrame.WrapFormat.DistanceLeft = "5cm";
            toFrame.RelativeVertical = RelativeVertical.Page;
            toFrame.Width = 500;
            paragraph = toFrame.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddFormattedText("This confirmation is a final reference for your order.", YVFonts.font12B);



            #endregion

            #region first part

            paragraph = section.AddParagraph();
            // paragraph.AddSectionPagesField().Section.PageSetup.LeftMargin = Unit.FromCentimeter(2.7); 
            //paragraph.Format.SpaceBefore = "3cm";

            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.LineSpacing = 15;
            paragraph.Format.LeftIndent = 34;
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("To whom it may concern,", YVFonts.fontBig);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("We confirm the following reservation:", YVFonts.fontBig);
            paragraph.AddLineBreak();
            if (data.tourName != "")
            {
                paragraph.AddFormattedText("Tour type: " + helperRepository.Reverse("ltr", data.tourName), YVFonts.font10);
            }
            paragraph.AddLineBreak();

            //paragraph.Format.SpaceBefore = "0cm";


            table = section.AddTable();

            table.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            table.Format.LineSpacing = 15;
            table.AddColumn(370);
            table.AddColumn(230);
            table.LeftPadding = 0;
            table.RightPadding = 0;
            table.TopPadding = 4;
            table.BottomPadding = 0;

            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Bottom;
            row[0].Format.LeftIndent = 33;

            row = table.AddRow();
            row.VerticalAlignment = VerticalAlignment.Bottom;
            row[0].Format.LeftIndent = 33;


            paragraph = table[0, 0].AddParagraph();
            // paragraph.Format.Shading.Color = Color.Parse("#D4E8F4");
            if (!string.IsNullOrEmpty(data.InstitutionName))
            {
                paragraph.AddFormattedText("Name of Client: " + helperRepository.Reverse("ltr", data.InstitutionName), YVFonts.font10);
            }
            else
            {
                paragraph.AddFormattedText("Name of Client: " + helperRepository.Reverse("ltr", data.ContactName), YVFonts.font10);

            }
            paragraph = table[0, 1].AddParagraph();
            //paragraph.Format.Shading.Color = Color.Parse("#E5E8F3");
            if (!string.IsNullOrEmpty(data.GroupName))
            {
                paragraph.AddFormattedText("Name of reservation: " + helperRepository.Reverse("ltr", data.GroupName), YVFonts.font10);

            }

            //helperRepository.AddToTextSplitToLines("ltr", paragraph, "Name of group: " + data.GroupName, 50, YVFonts.font10);

            paragraph = table[1, 0].AddParagraph();
            // paragraph.Format.Shading.Color = Color.Parse("#E0C3F2");
            if (!string.IsNullOrEmpty(data.AgeGroup)&&!string.IsNullOrEmpty(data.Grade)){
                paragraph.AddFormattedText("Age: " + helperRepository.Reverse("ltr", data.AgeGroup), YVFonts.font10);
                paragraph.AddSpace(3);
                paragraph.AddFormattedText("Class: " + helperRepository.Reverse("ltr", data.Grade), YVFonts.font10);
                paragraph.AddSpace(3);
            }
            if (data.NumberOfGroups != "")
            {
                paragraph.AddFormattedText("Num. of groups: " + data.NumberOfGroups, YVFonts.font10);
                paragraph.AddSpace(3);
            }
            paragraph.AddFormattedText("Num. of Participants: " + data.NumberOfParticipants, YVFonts.font10);



            paragraph = table[1, 1].AddParagraph();
            //  paragraph.Format.Shading.Color = Color.Parse("#E8E8D8");

            paragraph.AddFormattedText("Language: " + helperRepository.Reverse("ltr", data.VisitLangEn), YVFonts.font10);
            if (!string.IsNullOrEmpty(data.GefenType))
            {
                paragraph = section.AddParagraph();
                paragraph.Format.SpaceBefore = "0.2cm";
                paragraph.Format.LeftIndent = 33;
                paragraph.AddFormattedText("Gefen Type: " + data.GefenType, YVFonts.font10);
            }
       



            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = "0.2cm";
            paragraph.Format.LeftIndent = 33;

            if (data.ActivityDayList.Count == 1)
            {
                paragraph.AddFormattedText("Place of Activity: " + helperRepository.Reverse("ltr", data.ActivityDayList[0].LocationEn), YVFonts.font10);
                paragraph.AddTab();
                paragraph.AddTab();
                paragraph.AddFormattedText("Hours of activity: " + data.ActivityDayList[0].HouresFromTo, YVFonts.font10);
                paragraph.AddTab();
                paragraph.AddTab();
                //if (!string.IsNullOrEmpty(data.ActivityDayList[0].MuseumEntryTime))
                //{
                //    paragraph.AddFormattedText("Museum entry time: " + data.ActivityDayList[0].MuseumEntryTime, YVFonts.font10);
                //    paragraph.AddTab();
                //    paragraph.AddTab();
                //}
                paragraph.AddFormattedText("Date of Activity: " + data.ActivityDayList[0].Date, YVFonts.font10);
                paragraph.AddLineBreak();
            }
            else
            {
                paragraph.AddFormattedText("Date of Activity: ", YVFonts.font10);
                paragraph.AddLineBreak();
                int counter = 1;
                table = section.AddTable();
                table.AddColumn(100);
                table.AddColumn(160);
                table.AddColumn(220);
                table.AddColumn(75);
                table.TopPadding = 4;

                foreach (var activityDay in data.ActivityDayList)
                {
                    row = table.AddRow();
                    row.Format.LeftIndent = 33;
                    row.VerticalAlignment = VerticalAlignment.Bottom;
                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.AddFormattedText(counter.ToString() + ". " + activityDay.Date, YVFonts.font9_5);
                    paragraph = row.Cells[1].AddParagraph();
                    paragraph.AddFormattedText("Hours of activity: " + activityDay.HouresFromTo, YVFonts.font9_5);
                    paragraph = row.Cells[2].AddParagraph();
                    paragraph.AddFormattedText("Place of activity: " + activityDay.LocationEn, YVFonts.font9_5);
                    paragraph = row.Cells[3].AddParagraph();
                    paragraph.AddFormattedText("#Gro: " + activityDay.NumberOfGroups, YVFonts.font9_5);
                    counter++;
                }



            }
            /* string textOrCounter = helperRepository.Reverse("rtl","תאריך הביקור: ");

            int counter = 1;
            table = section.AddTable();
            if (data.ActivityDayList.Count > 1)
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "תאריכי הביקור:"), YVFonts.font11);
                paragraph.AddLineBreak();
                table.AddColumn(190);
                table.AddColumn(130);
                table.AddColumn(150);
                table.AddColumn(100);
            }
            else
            {

                table.AddColumn(160);
                table.AddColumn(130);
                table.AddColumn(150);
                table.AddColumn(130);
            }
                table.TopPadding = 4;

            foreach (var activityDay in data.ActivityDayList)
            {
                row = table.AddRow();
                row.VerticalAlignment = VerticalAlignment.Bottom;
                row.Format.Alignment = ParagraphAlignment.Right;
                paragraph = row.Cells[0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", activityDay.NumberOfGroups) + helperRepository.Reverse("rtl", "מספר קבוצות: "), YVFonts.font11);

                paragraph = row.Cells[1].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", activityDay.Location) + helperRepository.Reverse("rtl", "מקום הפעילות: "), YVFonts.font11);

                paragraph = row.Cells[2].AddParagraph();
                paragraph.AddFormattedText(activityDay.HouresFromTo + helperRepository.Reverse("rtl", "שעות הפעילות: "), YVFonts.font11);

                paragraph = row.Cells[3].AddParagraph();
                if (data.ActivityDayList.Count > 1)
                {
                    textOrCounter = " ." + counter.ToString();
                }
                paragraph.AddFormattedText(activityDay.Date + textOrCounter, YVFonts.font11);

                counter++;
            }

*/


            paragraph = section.AddParagraph();
            paragraph.Format.LeftIndent = 33;
            //paragraph.Format.SpaceBefore = "0.2cm";
            //paragraph.AddFormattedText("Please note, the reservation will only be confirmed in our system once it is returned and signed to: " + data.DepartmentEmail + " or " + data.DepartmentFax, YVFonts.font8_5);
            //paragraph.Format.SpaceAfter = "0.5cm";
            #endregion

            #region first table
            table = AddYVTable(section, ParagraphAlignment.Left);
            AddYVRows(table, 6, ParagraphAlignment.Left);
            table.LeftPadding = "0.5cm";
            paragraph = table[0, 0].AddParagraph();

            paragraph.AddFormattedText("Rules of conduct inside the Holocaust History Museum:", YVFonts.fontMedium);
            paragraph = table[1, 0].AddParagraph();
            paragraph.AddFormattedText("Please dress respectfully when visiting Yad Vashem. No photography or smoking; Food, drinks and bags are not permitted.", YVFonts.fontTiny);
            paragraph = table[2, 0].AddParagraph();
            paragraph.AddFormattedText("Only Yad Vashem wireless headset systems may be used for tours of the Museum and grounds.", YVFonts.fontTiny);
            paragraph = table[3, 0].AddParagraph();
            paragraph.AddFormattedText("Upon arrival, please present the Group Permissions Form to a Yad Vashem representative.", YVFonts.fontTiny);
            paragraph = table[4, 0].AddParagraph();
            paragraph.AddFormattedText("Entrance to the Holocaust History Museum is permitted until one hour before closing.", YVFonts.fontTiny);
            paragraph = table[5, 0].AddParagraph();
            paragraph.AddFormattedText("Entrance to the Holocaust History Museum is permitted for children age 10 and over, on presentation of ID. Children under the age of ten, including babies in strollers or carriers, may not enter the Museum", YVFonts.fontTiny);

            #endregion

            #region second table
            section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables

            table = AddYVTable(section, ParagraphAlignment.Left);
            AddYVRows(table, 6, ParagraphAlignment.Left);
            table.LeftPadding = "0.5cm";
            paragraph = table[0, 0].AddParagraph();

            paragraph.AddFormattedText("Alteration and Cancellation Policy:", YVFonts.fontMedium);


            paragraph = table[1, 0].AddParagraph();
            paragraph.AddFormattedText("Please cancel or alter your reservation via Yad Vashem's website: https://forms.yadvashem.org/reservations-edit", YVFonts.fontSmall);
            paragraph = table[2, 0].AddParagraph();
            paragraph.AddFormattedText("The registration and handling fee for a cancelled registration is non-refundable under any circumstances.", YVFonts.fontSmall);
            paragraph = table[3, 0].AddParagraph();
            paragraph.AddFormattedText( "Cancellation/alteration of a scheduled tour is possible up to 72 hours (3 business days, excluding Fridays and Jewish holiday eves) before the scheduled date and time, via Yad Vashem's website", YVFonts.fontSmall);
            paragraph.AddFormattedText("or via the email ", YVFonts.fontSmall);
            paragraph.AddFormattedText(data.DepartmentEmail, YVFonts.fontSmall);
            paragraph = table[4, 0].AddParagraph();
            paragraph.AddFormattedText( "Cancellation/alteration that does not fulfil these conditions will incur a charge of 100% of the price of the tour and services.", YVFonts.fontSmall);
            paragraph = table[5, 0].AddParagraph();
            paragraph.AddFormattedText("In the case of cancellation on the day of the scheduled tour due to 'force majeure' (a state of emergency as declared by an authorized governmental agency), the cancellation fee will be waived.", YVFonts.fontSmall);
            paragraph = table[6, 0].AddParagraph();
            paragraph.AddFormattedText("In special circumstances, Yad Vashem reserves the right to cancel/alter a reservation with advance notice to the visitor.", YVFonts.fontSmall);

            #endregion
            string totalToPay = data.TotalPriceAfterDiscount != string.Empty ? data.TotalPriceAfterDiscount : data.Total;
            #region third table
            if (data.isNeedToShowSignatureParagraph)
            {

                section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables


                table = AddYVTable(section, ParagraphAlignment.Left);
                table.LeftPadding = "0.5cm";
                AddYVRows(table, 9, ParagraphAlignment.Left);

                paragraph = table[0, 0].AddParagraph();
                paragraph.AddFormattedText("Confirmation from the Client: ", YVFonts.fontMedium);

                paragraph = table[1, 0].AddParagraph();

                paragraph.AddFormattedText("The total price owed by the client for the day’s activities:", YVFonts.fontBig);

                paragraph.AddSpace(2);
                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(totalToPay), YVFonts.fontBig);

                paragraph = table[2, 0].AddParagraph();

                paragraph.AddFormattedText("By signing the following, I understand the conditions of payment, and rules of the cancellation policy:", YVFonts.fontSmall);

                paragraph = table[3, 0].AddParagraph();




                paragraph.AddFormattedText("Name of the Client: ", YVFonts.fontSmall);
                paragraph.AddFormattedText("______________________________", YVFonts.fontLine);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("Position: ", YVFonts.fontSmall);

                paragraph.AddFormattedText("______________________________________", YVFonts.fontLine);

                paragraph = table[4, 0].AddParagraph();

                paragraph.AddFormattedText("Telephone: ", YVFonts.fontSmall);
                paragraph.AddFormattedText("____________________________________", YVFonts.fontLine);


                paragraph.AddSpace(2);
                paragraph.AddFormattedText("Signature: ", YVFonts.fontSmall);
                paragraph.AddFormattedText("____________________________________", YVFonts.fontLine);


                paragraph = table[5, 0].AddParagraph();
                paragraph.AddFormattedText("E-mail:", YVFonts.fontSmall);
                paragraph.AddFormattedText("_______________________________________", YVFonts.fontLine);
                paragraph.AddSpace(2);

                paragraph.AddFormattedText("Number of Acquisition (if relevant only):", YVFonts.fontSmall);
                paragraph.AddFormattedText("________________", YVFonts.fontLine);


                paragraph = table[6, 0].AddParagraph();
                paragraph.Format.SpaceBefore = "0.3cm";
                paragraph.AddFormattedText("*In the case that another party, other than the client, is paying for the activity, please sign the follow-ing:", YVFonts.fontSmall);

                paragraph = table[7, 0].AddParagraph();
                paragraph.AddFormattedText("1. Name of payee:", YVFonts.fontSmall);

                paragraph.AddFormattedText("__________________", YVFonts.fontLine);
                paragraph.AddSpace(2);
                paragraph.AddFormattedText("2. Identification Number:", YVFonts.fontSmall);
                paragraph.AddFormattedText("_______________", YVFonts.fontLine);

                paragraph.AddSpace(2);
                paragraph.AddFormattedText("3. Telephone:", YVFonts.fontSmall);
                paragraph.AddFormattedText("_______________", YVFonts.fontLine);


                paragraph = table[8, 0].AddParagraph();
                paragraph.AddFormattedText("4.Name of Confirmer:", YVFonts.fontSmall);
                paragraph.AddFormattedText("________________", YVFonts.fontLine);

                paragraph.AddSpace(2);
                paragraph.AddFormattedText("5. Position:", YVFonts.fontSmall);
                paragraph.AddFormattedText("________________________", YVFonts.fontLine);

                paragraph.AddSpace(2);
                paragraph.AddFormattedText("6. Signature:", YVFonts.fontSmall);
                paragraph.AddFormattedText("________________", YVFonts.fontLine);


                paragraph = table[9, 0].AddParagraph();
            }

            #endregion

           
            #region activity day tables


                   bool isFirstTable = true;
                   //firt table should have a header row for title

                   foreach (var activityDay in data.ActivityDayList)
                   {

                       section.AddParagraph().Format.SpaceBefore = 1;//space before table
                       table = section.AddTable();
                       table.TopPadding = 1;
                       table.BottomPadding = 2;
                       table.Rows.LeftIndent = 33;
                       table.RightPadding = 0;
                       table.LeftPadding = 10;
              
                       Column column;
                       for (int i = 1; i <= 6; i++)
                       {
                           column = table.AddColumn();
                           if (i == 1|| i==6)
                           {
                               column.Width = 130;
                           }
                           else
                           {
                               column.Width = 70;
                           }
                       }

                       if (isFirstTable)
                       {

                           row = table.AddRow();
                           row.Cells[0].MergeRight = 5;
                           row.BottomPadding = 5;
                           paragraph = row.Cells[0].AddParagraph();
                           paragraph.Format.Alignment = ParagraphAlignment.Left;
                           paragraph.AddFormattedText("Payment details for Reservation Number: " + data.OrderNum , YVFonts.fontBig);
                            if (data.creditCard4Digit != "")
                            {
                                paragraph.AddFormattedText("Paid with: " + data.creditCardBrand + " ************" + data.creditCard4Digit, YVFonts.fontBig);
                            }

                    //if (data.IsFromOnlineReservation)
                    //{
                    //    paragraph = row.Cells[0].AddParagraph();
                    //    paragraph.Format.Alignment = ParagraphAlignment.Left;
                    //    paragraph.AddFormattedText("The price is for the educational guided tour on the Yad Vashem campus only. ", YVFonts.fontRed);
                    //    paragraph.AddFormattedText("Costs of additional activities arranged by the group coordinator are not included. ", YVFonts.fontRed);
                    //}
                }

                row = table.AddRow();

                       row.HeadingFormat = true;
                       row.Height = "0.4cm";
                       row.Cells[0].Format.SpaceBefore = 2;
                       row.Cells[0].Format.SpaceAfter = 2;
                       row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                       row.Cells[0].MergeRight = 5;
                
                       row.Format.Alignment = ParagraphAlignment.Left;
                       row.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
                       //
                       paragraph = row.Cells[0].AddParagraph();
                       paragraph.AddFormattedText("Details for visit date: "+activityDay.Date , YVFonts.fontMedium);

                       if (activityDay.detailsList != null && activityDay.detailsList.Count > 0)
                       {
                           //this line makes sure that the table will not split in the middle, but will all be in one page
                           if (isFirstTable)
                           {
                               table.Rows[0].KeepWith = 3 + activityDay.detailsList.Count;
                           }
                           else
                           {
                               table.Rows[0].KeepWith = 2 + activityDay.detailsList.Count;

                           }


                           //add table headers
                           row = table.AddRow();
                   
                           paragraph = row.Cells[0].AddParagraph();
                           paragraph.AddFormattedText("Activity", YVFonts.fontTableColumnsHeader);
                    paragraph = row.Cells[1].AddParagraph();
                    paragraph.AddFormattedText("Cost", YVFonts.fontTableColumnsHeader);
                    paragraph = row.Cells[2].AddParagraph();
                    paragraph.AddFormattedText("Amount", YVFonts.fontTableColumnsHeader);
                    paragraph = row.Cells[3].AddParagraph();
                    paragraph.AddFormattedText("Total", YVFonts.fontTableColumnsHeader);
                    paragraph = row.Cells[4].AddParagraph();
                    paragraph.AddFormattedText("Discount", YVFonts.fontTableColumnsHeader);
                    paragraph = row.Cells[5].AddParagraph();
                    paragraph.AddFormattedText("Total after discount", YVFonts.fontTableColumnsHeader);


                    foreach (var details in activityDay.detailsList)
                           {
                               row = table.AddRow();

                               paragraph = row.Cells[0].AddParagraph();
                               //אם יש ערך בשדה הערה- אז לשים אותו, אחרת לשים את שם הפריט
                               if (details.Comments.Trim() != string.Empty)
                               {
                                   paragraph.AddFormattedText(helperRepository.Reverse("ltr", details.Comments), YVFonts.fontSmall);
                               }
                               else
                               {
                                   paragraph.AddFormattedText(helperRepository.Reverse("ltr", details.itemNameEn), YVFonts.fontSmall);
                               }

                               paragraph = row.Cells[1].AddParagraph();
                               paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.PricePerUnit), YVFonts.fontSmall);

                               paragraph = row.Cells[2].AddParagraph();
                               paragraph.AddFormattedText(details.Amount, YVFonts.fontSmall);

                               paragraph = row.Cells[3].AddParagraph();
                               paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.Total), YVFonts.fontSmall);

                               paragraph = row.Cells[4].AddParagraph();
                               if (!string.IsNullOrEmpty(details.Discount) && details.Discount != "0")
                               {
                                   paragraph.AddFormattedText(helperRepository.DiscountWithSymbol(details.Discount, details.DiscountType), YVFonts.fontSmall);
                               }
                               paragraph = row.Cells[5].AddParagraph();
                               paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.TotalAfterDiscount), YVFonts.fontSmall);


                           }
                           //last row
                           row = table.AddRow();
                           row.Format.Alignment = ParagraphAlignment.Left;
                           row.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
                           paragraph = row.Cells[5].AddParagraph();
                           paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.TotalAfterDiscount), YVFonts.fontTableColumnsHeader);

                           //  row.Cells[1].MergeRight = 1;
                           paragraph = row.Cells[3].AddParagraph();
                           paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.Total), YVFonts.fontTableColumnsHeader);

                           // row.Cells[3].MergeRight = 2;
                           paragraph = row.Cells[0].AddParagraph();
                           paragraph.AddFormattedText("Total:", YVFonts.fontTableColumnsHeader);




                           int innerBorderFromRowNumber = 2;
                           int centerTextFromRowNumber = 1;

                           if (isFirstTable)
                           {
                               innerBorderFromRowNumber = 3;
                               centerTextFromRowNumber = 2;
                               table.SetEdge(0, 1, table.Columns.Count, table.Rows.Count - 1, Edge.Box, BorderStyle.Single, 0.5, YVFonts.borderColor);


                           }
                           else
                           {
                               table.SetEdge(0, 0, table.Columns.Count, table.Rows.Count, Edge.Box, BorderStyle.Single, 0.5, YVFonts.borderColor);

                           }
                           table.SetEdge(0, innerBorderFromRowNumber, table.Columns.Count, table.Rows.Count - 3, Edge.Horizontal, BorderStyle.Single, 0.5, Color.Parse("#BDBEC1"));
                           table.LeftPadding = 5;
                           table.RightPadding = 5;
                           table.TopPadding = 5;

                           //loop all table
                           {
                               for (int i = 0; i < table.Columns.Count; i++)
                               {
                                   for (int j = centerTextFromRowNumber; j < table.Rows.Count; j++)//start from second row
                                   {
                                       Cell cell = table[j, i];
                                       cell.Format.Alignment = ParagraphAlignment.Left;
                                       cell.VerticalAlignment = VerticalAlignment.Center;


                                   }
                               }
                           }

                       }
                       isFirstTable = false;
                   }
                   #endregion




                   #region Total
                   
                   // if (data.ActivityDayList.Count>1|| data.Discount != string.Empty)
                   // {

                   string totalLine;
                   if (data.TotalPriceAfterDiscount != "" && data.Discount != "" && data.Discount!="0")// there is a discount on the whole order
                   {

                       table = section.AddTable();
                       table.Rows.LeftIndent = 33;
                       table.LeftPadding = 0;
                       table.RightPadding = 0;
                       table.Format.Borders.DistanceFromBottom = 3;
                       table.Format.Borders.DistanceFromTop = 3;

                table.AddColumn(445);
                       table.AddColumn(8);
                table.AddColumn(77);


                      row = table.AddRow();
                       row.TopPadding = 15;

                       row = table.AddRow();
                       row.BottomPadding = 5;

                       row = table.AddRow();
                       row.Format.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");

                       //first row
                       paragraph = table.Rows[0][0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText("Total Cost for Reservation: " , YVFonts.font10);
                paragraph = table.Rows[0][2].AddParagraph();

                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(data.Total), YVFonts.font10);
                      

                //second row
                paragraph = table.Rows[1][0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;

                paragraph.AddFormattedText("Discount: ", YVFonts.font10);
                       paragraph = table.Rows[1][2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.DiscountWithSymbol(data.Discount, data.DiscountType), YVFonts.font10);



                //third row
                paragraph = table.Rows[2][0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;

                paragraph.AddFormattedText("Total After Discount: ", YVFonts.fontTotalWithDiscount);

                paragraph = table.Rows[2][1].AddParagraph();
                paragraph.AddFormattedText(" ", YVFonts.fontTotalWithDiscount);
                paragraph = table.Rows[2][2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(data.TotalPriceAfterDiscount), YVFonts.fontTotalWithDiscount);

                

            }
            else
                   {
                       paragraph = section.AddParagraph();
                       paragraph.Format.SpaceBefore = 15;
                       paragraph.Format.LeftIndent = 33;
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                       paragraph.Format.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
                       totalLine = "Total Cost for Reservation Number " + data.OrderNum + " : " + helperRepository.WithCurrencySymbol(totalToPay);
                       paragraph.Format.RightIndent = 20;
                       paragraph.AddFormattedText(totalLine, YVFonts.fontTotal);
                   }

            #endregion


            #region Customer comments

            if (data.IsFromOnlineReservation)
            {
                //third row
                /*paragraph = table.Rows[3][0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;

                paragraph.AddFormattedText("WARNING!");

                paragraph = table.Rows[2][1].AddParagraph();
                paragraph.AddFormattedText(" ", YVFonts.fontTotalWithDiscount);
                paragraph = table.Rows[2][2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(data.TotalPriceAfterDiscount), YVFonts.fontTotalWithDiscount);
                */

                section.AddParagraph().Format.SpaceBefore = 2;//space before table
                paragraph = section.AddParagraph();
                
                paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
                paragraph.Format.LineSpacing = 15;
                paragraph.Format.LeftIndent = 34;
                Font font = new Font();
                font.Size = 12;
                font.Color = Color.Parse("#ff0000");

                //paragraph.AddFormattedText("ONLY GUIDE", font);
                


            }



            if (!string.IsNullOrEmpty(data.CustomerComments))
            {
                string[] strings = data.CustomerComments.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables
                table = helperRepository.AddYVTable(section, ParagraphAlignment.Left);
                table.LeftPadding = "0.5cm";
                helperRepository.AddYVRows(table, strings.Length + 1, ParagraphAlignment.Left);
                row = table.Rows[0];
                row.BottomPadding = 10;
               
                paragraph = row.Cells[0].AddParagraph();
                paragraph.AddFormattedText("Comments:", YVFonts.fontBig);


                int rowNum = 1;
                foreach (string str in strings)
                {
                    row = table.Rows[rowNum];
                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("ltr", str), YVFonts.font11);
                    rowNum++;
                }
                table.Rows[0].KeepWith = table.Rows.Count - 1;
            }




            #endregion

            #region food comment

            //section.AddParagraph().Format.SpaceBefore = 2;//space before table
            //paragraph = section.AddParagraph();

            //string food_comment_1 = "In the event that you are interested in refreshments, food, or restaurant service, please contact the Yad Vashem";
            //string food_comment_2 = "Cafeteria: Meir Kislasi <REDACTED-PHONE>,Aryeh Kislasi <REDACTED-PHONE>,Cafeteria Office: 026410582 , <REDACTED-EMAIL>";

            //paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            //paragraph.Format.LineSpacing = 15;
            //paragraph.Format.LeftIndent = 34;


            //paragraph.AddFormattedText(food_comment_1, YVFonts.font10);
            //paragraph.AddLineBreak();
            //paragraph.AddFormattedText(food_comment_2, YVFonts.font10);

            #endregion

            #region footer

            AddYVFooterTextFrameEng(section.Footers.FirstPage, data);//for firstPage


                   AddYVFooterTextFrameEng(section.Footers.Primary, data);//for all other pages


                  
            #endregion

        }


        //   ----------------------------------------
        #region Help functions
        private void AddYVFooterTextFrame(HeaderFooter footer,SchoolConfirmationData data)
        {
            var frame = footer.AddTextFrame();
            frame.MarginTop = "0.6cm";
            frame.MarginLeft = 20;
            frame.MarginBottom = 10;
            var table = frame.AddTable();
           
            table.AddColumn(180);
            table.AddColumn(10);


            if (footer.IsFirstPage)
            {
                table.AddColumn(450);
            }
           
            var row= table.AddRow();
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            var paragraph=row.Cells[0].AddParagraph();
           

            var fontFooter = new Font();
            fontFooter.Size = 9;
            fontFooter.Bold = true;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            //paragraph.AddFormattedText(helperRepository.Reverse("rtl", "בית ספר הבין-לאומי להוראת השואה,"), fontFooter);
            //paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.Department), fontFooter);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(data.DepartmentFax + helperRepository.Reverse("rtl", "פקס ") + " | " + data.DepartmentPhone + helperRepository.Reverse("rtl", "טל‘ "), fontFooter);

            
            paragraph = row.Cells[0].AddParagraph();
            paragraph.Format.SpaceBefore = 5;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            var image = paragraph.AddImage(@"Images\footer.jpg");
           // image.WrapFormat.DistanceTop = 50;
            //image.WrapFormat.DistanceLeft = 50;
            paragraph = row.Cells[1].AddParagraph();
            paragraph.AddImage(@"Images\line.jpg");

            if (footer.IsFirstPage && !data.IsFromOnlineReservation)
            {
                paragraph = row.Cells[2].AddParagraph();
                row.Cells[2].Format.RightIndent = 80;
                row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[2].VerticalAlignment = VerticalAlignment.Top;
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.CoordinatorName)+helperRepository.Reverse("rtl", "* לצורך תיאום תכנית הביקור יש לפנות, לרכז/ת "), fontFooter);
                paragraph.AddLineBreak();
                paragraph.AddSpace(20);
                paragraph.AddFormattedText(data.CoordinatorEmail + helperRepository.Reverse("rtl", "מייל ") + " | " + data.CoordinatorPhone + helperRepository.Reverse("rtl", "טלפון ") + "   ", fontFooter);// i created the prefix spaces with alt+255
            }
        }

        private void AddYVOrderNumTextFrame(HeaderFooter header, string orderNum,string direction)
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
                    OrderNumFrame.WrapFormat.DistanceTop = "4.72cm";
                    break;
                case "left":
                    OrderNumFrame.Left = ShapePosition.Left;
                    OrderNumFrame.WrapFormat.DistanceLeft = "6cm";
                    OrderNumFrame.WrapFormat.DistanceTop = "4.8cm";
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



        private void AddYVFooterTextFrameEng(HeaderFooter footer, SchoolConfirmationData data)
        {
            var frame = footer.AddTextFrame();
            frame.MarginTop = "0.6cm";
            frame.MarginLeft = 33;
            frame.MarginBottom = 10;
            var table = frame.AddTable();



            
            table.AddColumn(320);
            table.AddColumn(10);
            table.AddColumn(300);

            Paragraph paragraph;
            var row = table.AddRow();
            if (footer.IsFirstPage && !data.IsFromOnlineReservation)
            {
                paragraph = row.Cells[0].AddParagraph();
               
                row.Cells[2].VerticalAlignment = VerticalAlignment.Top;
                paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
                paragraph.Format.LineSpacing = 15;
                paragraph.AddFormattedText("In order to coordinate the visit,", YVFonts.font9B);
                paragraph.AddLineBreak();
                paragraph.AddFormattedText("please contact the content coordinator " + helperRepository.Reverse("ltr", data.CoordinatorNameEn), YVFonts.font9B);
                paragraph.AddLineBreak();
                paragraph.AddFormattedText("Tel: " + data.CoordinatorPhone + " | " + "Mail: " + data.CoordinatorEmail, YVFonts.font9B);// i created the prefix spaces with alt+255
            }


            paragraph = row.Cells[1].AddParagraph();
            paragraph.AddImage(@"Images\line.jpg");

            paragraph = row.Cells[2].AddParagraph();


            
            //paragraph.AddFormattedText("The International School for Holocaust Studies", YVFonts.font9B);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Department: "+helperRepository.Reverse("ltr", data.DepartmentEn), YVFonts.font9B);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText("Tel: "+data.DepartmentPhone +  " | " +"Fax: " +data.DepartmentFax , YVFonts.font9B);
            paragraph.AddLineBreak();

            //paragraph = row.Cells[1].AddParagraph();
            //paragraph.Format.SpaceBefore = 5;
            var image = paragraph.AddImage(@"Images\footer.jpg");
            // image.WrapFormat.DistanceTop = 50;
            //image.WrapFormat.DistanceLeft = 50;
          

           
        }

        private void AddYVOrderNumTextFrameEng(HeaderFooter header, string orderNum)
        {
            var OrderNumFrame = new TextFrame();
            OrderNumFrame = header.AddTextFrame();
            OrderNumFrame.Width = 150;
            OrderNumFrame.Left = ShapePosition.Left;
            OrderNumFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            OrderNumFrame.WrapFormat.DistanceTop = "4.63cm";
            OrderNumFrame.WrapFormat.DistanceLeft = "14.8cm";
            OrderNumFrame.RelativeVertical = RelativeVertical.Page;

            var paragraph = OrderNumFrame.AddParagraph();
            Font font = new Font();
            font.Size = 12;
            font.Color = Color.Parse("#FFFFFF");
            font.Bold = true;
            paragraph.AddFormattedText(orderNum, font);
        }

        private Table AddActivityDayTable(Section section)
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
                column=table.AddColumn();
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

        private void AddYVRows(Table table,int rowsCount, ParagraphAlignment paragraphAlignment)
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
            table.SetEdge(0, 0, 1, rowsCount+1, Edge.Box, BorderStyle.Single,0.5, YVFonts.borderColor);


        }

        private Table AddYVTable(Section section, ParagraphAlignment paragraphAlignment)
        {
            var table= section.AddTable();
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


        private void BuildReservationInDocx(Document document, ScheduleReservation reservation, bool isMultiReservation, int counter, string serviceProvider, bool showScheduleFirstGroup)
        {
            Section section = document.AddSection();

            Paragraph p;
            p = section.AddParagraph();
            p.Format.Alignment = ParagraphAlignment.Right;

            //if (serviceProvider == "")
            //{
            //    Font font = new Font();
            //    font.Size = 14;
            //    p.Format.Alignment = ParagraphAlignment.Right;
            //    p.AddFormattedText(helperRepository.Reverse("rtl", "מחלקת " + reservation.departmentName), font);
            //    p.AddLineBreak();
            //    p.AddFormattedText(helperRepository.Reverse("rtl", "טל " + reservation.departmentTelephone), font);
            //    p.AddLineBreak();
            //    p.AddFormattedText(helperRepository.Reverse("rtl", "פקס " + reservation.departmentFax), font);
            //}

            int dayCounter = 0;
            foreach (var day in reservation.days)
            {
                if (serviceProvider == "")
                {
                    if (!isMultiReservation && dayCounter > 0)
                    {
                        section.AddParagraph().AddLineBreak();
                    }
                    var activityDayType = "יום עיון";
                    if (reservation.isEventReservation)
                    {
                        activityDayType = "הכשרה/ השתלמות";
                    }
                    Font font = new Font();
                    font.Size = 14;
                    font.Bold = true;
                    p = section.AddParagraph();
                    p.AddFormattedText(helperRepository.Reverse("rtl", activityDayType + " " + reservation.instName), font);
                    p.AddLineBreak();
                    p.Format.Alignment = ParagraphAlignment.Center;
                    p.AddSpace(30);
                    if (!string.IsNullOrEmpty(reservation.groupName))
                    {
                        p = section.AddParagraph();
                        p.AddFormattedText(helperRepository.Reverse("rtl", reservation.groupName), font);
                        p.AddLineBreak();
                        p.Format.Alignment = ParagraphAlignment.Center;
                        p.AddSpace(30);
                    }
                    font.Bold = false;
                    p = section.AddParagraph();
                    p.AddFormattedText(helperRepository.Reverse("rtl", "יום " + day.dayOfWeek + " " + HebrewDate.GetHebDateByDate(day.date) + " " + day.date), font);
                    p.AddLineBreak();
                    p.Format.Alignment = ParagraphAlignment.Center;
                    p.AddSpace(10);

                    if (!string.IsNullOrEmpty(day.subject))
                    {

                        font.Bold = true;
                        p = section.AddParagraph();
                        p.AddFormattedText(helperRepository.Reverse("rtl", "נושא יום העיון"), font);
                        p.AddLineBreak();
                        p.Format.Alignment = ParagraphAlignment.Center;

                        p = section.AddParagraph();
                        p.Format.RightIndent = 64;
                        p.Format.SpaceBefore = "0.2cm";

                        p.AddFormattedText(helperRepository.Reverse("rtl", day.subject), font);
                        p.AddLineBreak();
                        p.Format.Alignment = ParagraphAlignment.Right;
                        p.AddSpace(10);

                    }
                    p.AddSpace(10);
                    font.Bold = false;
                    if (!reservation.isEventReservation)
                    {
                        p = section.AddParagraph();
                        p.Format.SpaceBefore = "0.2cm";
                        p.Format.Alignment = ParagraphAlignment.Right;
                        //p.Format.LineSpacingRule = LineSpacingRule.AtLeast;
                        //p.Format.LineSpacing = 15;
                        p.Format.RightIndent = 34;
                        p.AddFormattedText(helperRepository.Reverse("rtl", "מספר הקבוצות " + day.numberOfGroups), font);

                        p.AddLineBreak();
                    }

                    p.AddFormattedText(helperRepository.Reverse("rtl", "שפת הדרכה " + reservation.language), font);
                    p.AddLineBreak();

                    p.AddFormattedText(helperRepository.Reverse("rtl", "תחילת הפעילות " + day.startTime), font);
                    p.AddLineBreak();

                    p.AddFormattedText(helperRepository.Reverse("rtl", "סיום הפעילות " + day.endTime), font);
                    p.AddLineBreak();

                    if (!reservation.isEventReservation)
                    {
                        p.AddFormattedText(helperRepository.Reverse("rtl", "שכבת גיל " + reservation.ageGroup), font);
                        p.AddLineBreak();
                    }

                    font.Underline = Underline.Single;
                    p.AddFormattedText(helperRepository.Reverse("rtl", "תכנית היום"), font);
                    p.AddLineBreak();
                    p.AddSpace(10);
                }
                else
                {
                    Font font = new Font();
                    font.Size = 11;
                    font.Bold = true;
                    font.Underline = Underline.Single;

                    if (reservation.isEventReservation)
                    {
                        p = section.AddParagraph();
                        p.AddFormattedText(helperRepository.Reverse("rtl", "(" + reservation.reservationNum + ") פירוט עבודתך בהכשרה/ השתלמות"), font);
                        p.AddLineBreak();
                    }
                    else
                    {
                        p = section.AddParagraph();
                        p.AddFormattedText(helperRepository.Reverse("rtl", "(" + reservation.reservationNum + ") פירוט עבודתך בהזמנה"), font);
                        p.AddLineBreak();
                    }
                    addReservationDetailsTable(section, reservation, day);
                    p.AddSpace(10);
                }

                if (showScheduleFirstGroup && day.groups.Count > 0)
                {
                    addGroupDetailsTable(section, day.groups[0], reservation.isEventReservation);
                }
                else
                {
                    foreach (var group in day.groups)
                    {
                        addGroupDetailsTable(section, group, reservation.isEventReservation);
                    }
                }
               
                dayCounter++;
            }
        }

        private void addReservationDetailsTable(Section section, ScheduleReservation reservation, ScheduleActivityDay day)
        {

            Table t;
            if (reservation.isEventReservation)
            {
                t = section.AddTable(); // row 3 c 6
                for (int i = 0; i < 6; i++)
                {
                    t.AddColumn();
                }
                for (int i = 0; i < 3; i++)
                {
                    t.AddRow();
                }
            }
            else
            {
                t = section.AddTable(); // 4 6
                for (int i = 0; i < 6; i++)
                {
                    t.AddColumn();
                }
                for (int i = 0; i < 4; i++)
                {
                    t.AddRow();
                }
            }

            Paragraph p;
            Color shadingColor = Color.FromArgb(0, 226, 237, 246);
            float[] width = new float[] { 20, 16, 14, 15, 14, 14 };
            for (int i = 0; i < t.Columns.Count; i++)
            {
                t.Columns[i].Width = width[i];
            }
            //t.SetWidthsPercentage(width, 570);
            t.Format.Alignment = ParagraphAlignment.Center;
            //t.SetDirection(Direction.RightToLeft);
            
            Paragraph paragraph;
            Font font = new Font();
            font.Bold = true;

            var row = t.Rows[0];
            var cell = row.Cells[0];

            cell.AddParagraph("תאריך");
            cell.Format.Shading.Color = shadingColor;
            cell.Format.Font = font;

            cell = row.Cells[1];
            cell.AddParagraph(day.date);
            cell.Format.Font = font;

            cell = row.Cells[2];
            cell.AddParagraph("תאריך עברי");
            cell.Format.Shading.Color = shadingColor;
            cell.Format.Font = font;

            cell = row.Cells[3];
            cell.AddParagraph(HebrewDate.GetHebDateByDate(day.date));

            cell = row.Cells[4];
            cell.AddParagraph("מחלקה מזמינה");
            cell.Format.Shading.Color = shadingColor;
            cell.Format.Font = font;

            cell = row.Cells[5];
            cell.AddParagraph(reservation.departmentName);
            
            row = t.Rows[1];
            cell = row.Cells[0];
            if (reservation.isEventReservation)
            {
                cell.AddParagraph("מספר הכשרה/ השתלמות");
                cell.Format.Font = font;
            }
            else
            {
                cell.AddParagraph("מספר הזמנה");
                cell.Format.Font = font;
            }
            cell.Format.Shading.Color = shadingColor;

            cell = row.Cells[1];
            cell.AddParagraph(reservation.reservationNum);

            cell = row.Cells[2];
            cell.AddParagraph("שעות הפעילות");
            cell.Format.Font = font;
            cell.Format.Shading.Color = shadingColor;

            cell = row.Cells[3];
            cell.AddParagraph(day.startTime + "-" + day.endTime);

            cell = row.Cells[4];
            cell.AddParagraph("טלפון המחלקה");
            cell.Format.Font = font;

            cell = row.Cells[5];
            cell.AddParagraph(reservation.departmentTelephone);

            row = t.Rows[2];
            cell = row.Cells[0];
            if (reservation.isEventReservation)
            {
                cell.AddParagraph("יום בשבוע");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;
                
                cell = row.Cells[1];
                cell.AddParagraph("יום " + day.dayOfWeek);
            }
            else
            {
                cell.AddParagraph("שם המוסד");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;

                cell = row.Cells[1];
                cell.AddParagraph(reservation.instName);
            }

            cell = row.Cells[2];
            if (reservation.isEventReservation)
            {
                cell.AddParagraph("נושא הארוע");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;
                
                cell = row.Cells[3];
                cell.AddParagraph(reservation.groupName);

                cell = row.Cells[4];
                cell.AddParagraph("רכז");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;
                
                cell = row.Cells[5];
                cell.AddParagraph(reservation.coordinator);
            }



            if (!reservation.isEventReservation)
            {
                cell.AddParagraph("שם ההזמנה");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;

                cell = row.Cells[3];
                cell.AddParagraph(reservation.groupName);

                cell = row.Cells[4];
                cell.AddParagraph("שפת ההדרכה");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;

                cell = row.Cells[5];
                cell.AddParagraph(reservation.language);

                row = t.Rows[3];
                cell = row.Cells[0];
                cell.AddParagraph("מספר קבוצות בהזמנה");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;

                cell = row.Cells[1];
                cell.AddParagraph(day.numberOfGroups.ToString());

                cell = row.Cells[2];
                cell.AddParagraph("יום בשבוע");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;

                cell = row.Cells[3];
                cell.AddParagraph("יום " + day.dayOfWeek);

                cell = row.Cells[4];
                cell.AddParagraph("שכבת גיל");
                cell.Format.Font = font;
                cell.Format.Shading.Color = shadingColor;

                cell = row.Cells[5];
                cell.AddParagraph(reservation.ageGroup);

            }
            //p = section.AddParagraph();
            //p.InsertTableAfterSelf(t);
        }

        private void addGroupDetailsTable(Section section, ScheduleGroup group, bool isEventReservation = false)
        {
            Table table;


            table = section.AddTable();
            table.BottomPadding = 3;
            table.Rows.LeftIndent = 77;

            Column column;
            Row row;
            Paragraph paragraph;

            for (int i = 1; i <= 6; i++)
            {
                column = table.AddColumn();
                column.Width = 80;
                column.Format.Alignment = ParagraphAlignment.Right;
            }

            row = table.AddRow();

            row.HeadingFormat = true;
            row.Height = "0.4cm";
            row.Cells[0].Format.SpaceBefore = 3;
            row.Cells[0].Format.SpaceAfter = 3;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].MergeRight = 5;


            row.Format.Alignment = ParagraphAlignment.Right;
            row.Shading.Color = YVFonts.shadingColor;// Color.Parse("#E0E8F3");
                                                     //
            paragraph = row.Cells[0].AddParagraph();

            if (!isEventReservation)
            {
                if (group.groupName.Trim() != "")
                {
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", group.groupName + " מספר משתתפים " + group.participantsNumber), YVFonts.fontMedium);
                }
                else
                {
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "קבוצה " + group.groupNumber + " מספר משתתפים " + group.participantsNumber), YVFonts.fontMedium);
                }
            }
            if (!string.IsNullOrEmpty(group.groupServiceProvider))
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", " מדריך/כה " + group.groupServiceProvider), YVFonts.fontMedium);
            }
            if (!string.IsNullOrEmpty(group.groupResource))
            {
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "," + group.groupResource), YVFonts.fontMedium);
            }

            if (group.details != null && group.details.Count > 0)
            {
                row = table.AddRow();

                paragraph = row.Cells[5].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "שעות"), YVFonts.fontTableColumnsHeader);
                paragraph = row.Cells[4].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "מוצר"), YVFonts.fontTableColumnsHeader);

                paragraph = row.Cells[3].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "נושא"), YVFonts.fontTableColumnsHeader);
                paragraph = row.Cells[2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "נותן שרות"), YVFonts.fontTableColumnsHeader);
                paragraph = row.Cells[1].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "מיקום"), YVFonts.fontTableColumnsHeader);
                paragraph = row.Cells[0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הערות"), YVFonts.fontTableColumnsHeader);

                Borders borders = new Borders();
                borders.Style = BorderStyle.Single;
                row.Borders = borders;
            }

            foreach (var details in group.details)
            {
                row = table.AddRow();

                paragraph = row.Cells[5].AddParagraph();

                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.startHour), YVFonts.fontSmall);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "-"), YVFonts.fontSmall);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.endHour), YVFonts.fontSmall);

                paragraph = row.Cells[4].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.product), YVFonts.fontSmall);

                paragraph = row.Cells[3].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.subject), YVFonts.fontSmall);

                paragraph = row.Cells[2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.serviceProvider), YVFonts.fontSmall);

                paragraph = row.Cells[1].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.resource), YVFonts.fontSmall);

                paragraph = row.Cells[0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.comments), YVFonts.fontSmall);

                Borders borders = new Borders();
                borders.Style = BorderStyle.Single;
                row.Borders = borders;

            }
            int innerBorderFromRowNumber = 2;
            int centerTextFromRowNumber = 1;
            table.SetEdge(0, 0, table.Columns.Count, table.Rows.Count, Edge.Box, BorderStyle.Single, 0.5, YVFonts.borderColor);

            table.LeftPadding = 5;
            table.RightPadding = 5;
            table.TopPadding = 5;

            for (int i = 0; i < table.Columns.Count; i++)
            {
                for (int j = centerTextFromRowNumber; j < table.Rows.Count; j++)//start from second row
                {
                    Cell cell = table[j, i];
                    cell.Format.Alignment = ParagraphAlignment.Right;
                    cell.VerticalAlignment = VerticalAlignment.Center;

                }
            }

            }

        #endregion

    }
}
