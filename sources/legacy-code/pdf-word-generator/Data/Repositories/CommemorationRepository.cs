using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;



namespace Data.Repositories
{
    public class CommemorationRepository : ICommemorationRepository
    {
        private IHelperRepository helperRepository;

        public CommemorationRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;

        }
        IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

        public string CreateDocument(SchoolConfirmationData data)
        {


            Document document = new Document();
            try
                {

                    Log.Error("in invoice repository create document.  ");
                    document.Styles[StyleNames.Normal].Font.Name = "Arial";

                    document.Info.Title = "הצעת מחיר " + data.OrderNum;
                    document.Info.Subject = "Confirmation 222";
                    document.Info.Author = "Confirmation 333";



                    switch (data.DocLang)
                    {
                        case "עברית":
                            {
                                Log.Error("lang heb");
                                CreatePageAndFillContentheb(document, data);
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

        private void CreatePageAndFillContentheb(Document document, SchoolConfirmationData data)
        {
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(0);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(6.5);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(2.8);
            document.DefaultPageSetup.Orientation = Orientation.Portrait;
            string totalToPay = data.TotalPriceAfterDiscount != string.Empty ? data.TotalPriceAfterDiscount : data.Total;
            SetResponsibleAgentDetails(data);

            #region headers and date
            // Each MigraDoc document needs at least one section.
            Section section = document.AddSection();


            section.PageSetup.DifferentFirstPageHeaderFooter = true;
            section.PageSetup.HeaderDistance = 0;
            section.PageSetup.FooterDistance = 12;

            Paragraph paragraph = section.AddParagraph();
            Row row;

            //first page header
            section.Headers.FirstPage.AddImage(@"Images\commemoration_banner_heb.jpg");
            helperRepository.AddYVOrderNumTextFrame(section.Headers.FirstPage, data.OrderNum, "right");

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

            helperRepository.AddYVOrderNumTextFrame(section.Headers.Primary, data.OrderNum, "right");
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

            //TextFrame departmentFrame = new TextFrame();
            //departmentFrame = section.AddTextFrame();
            //departmentFrame.Left = ShapePosition.Left;
            //departmentFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            //departmentFrame.WrapFormat.DistanceTop = "6cm";
            //departmentFrame.WrapFormat.DistanceLeft = "17cm";
            //departmentFrame.RelativeVertical = RelativeVertical.Page;
            //paragraph = departmentFrame.AddParagraph();
            //paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.DepartmentName), YVFonts.font10);



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
            if (data.GroupName.Trim() != string.Empty)
            {
                paragraph.AddLineBreak();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.GroupName), YVFonts.font10);

            }

            if (data.InstitutionAddress.Trim() != string.Empty)
            {
                paragraph.AddLineBreak();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionAddress), YVFonts.font10);

            }
            if (data.InstitutionCity.Trim() != string.Empty)
            {
                paragraph.AddLineBreak();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionCity), YVFonts.font10);

            }
            if (data.Fax.Trim() != string.Empty)
            {
                paragraph.AddLineBreak();
                paragraph.AddFormattedText(data.Fax + helperRepository.Reverse("rtl", "פקס "), YVFonts.font10);
            }
            paragraph.AddLineBreak();


            #endregion

            #region first part
            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = "0.2cm";
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.LineSpacing = 15;
            paragraph.Format.RightIndent = 34;
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "שלום רב,"), YVFonts.fontBig);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "להלן טופס התקשרות להזמנת פעילות:"), YVFonts.fontBig);
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();




            // char charToPad = ' ';//THIS IS NOT A SPACE. THIS IS ALT+255

            table = AddOneRowTable(section,100,120,260, 50);//total width 530

            paragraph = table[0, 3].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "שם הארגון:"), YVFonts.font11);
            paragraph = table[0, 2].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionNameOnly), YVFonts.font11);
            paragraph = table[0, 1].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "מספר עוסק מורשה / ח.פ:"), YVFonts.font11);
            paragraph = table[0, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionCompanyNumberOrSymbol), YVFonts.font11);





            table = AddOneRowTable(section, 445, 85);

            paragraph = table[0,0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.InstitutionAddress + " " + data.InstitutionCity), YVFonts.font11);
            paragraph = table[0, 1].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כתובת ארגון מלאה:"), YVFonts.font11);


            table = AddOneRowTable(section, 380, 150);
            paragraph = table[0, 0].AddParagraph();
            paragraph.AddFormattedText("", YVFonts.font11);
            paragraph = table[0, 1].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כתובת למשלוח חשבונית לתשלום:"), YVFonts.font11);


            table = AddOneRowTable(section, 230, 30, 220, 50);

            paragraph = table[0, 0].AddParagraph();

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.ContactEmail), YVFonts.font11);
            paragraph = table[0,1].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "מייל:"), YVFonts.font11);
            paragraph = table[0, 2].AddParagraph();

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.ContactName), YVFonts.font11);
            paragraph = table[0, 3].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "איש קשר:"), YVFonts.font11);

            table = AddOneRowTable(section, 200, 60, 220, 50);
            paragraph = table[0, 0].AddParagraph();
            paragraph.AddFormattedText("", YVFonts.font11);
            paragraph = table[0, 1].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "טלפון נוסף:"), YVFonts.font11);
            paragraph = table[0, 2].AddParagraph();
             paragraph.AddFormattedText(helperRepository.Reverse("rtl",data.ContactPhone), YVFonts.font11);
            paragraph = table[0, 3].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "טלפון נייד:"), YVFonts.font11);




            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddLineBreak();
            paragraph.Format.RightIndent = 33;

            //if (data.ActivityDayList.Count == 1)
            //{
            //    paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.ActivityDayList[0].Location) + helperRepository.Reverse("rtl", "מקום הפעילות: "), YVFonts.font11);
            //    paragraph.AddTab();
            //    paragraph.AddTab();
            //    paragraph.AddFormattedText(data.ActivityDayList[0].HouresFromTo + helperRepository.Reverse("rtl", "שעות הפעילות: "), YVFonts.font11);
            //    paragraph.AddTab();
            //    paragraph.AddTab();
            //    paragraph.AddFormattedText(data.ActivityDayList[0].Date + helperRepository.Reverse("rtl", "תאריך הביקור: "), YVFonts.font11);
            //    paragraph.AddLineBreak();
            //}
            //else
            //{
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "תאריך פעילות:"), YVFonts.fontBig);
            paragraph.AddLineBreak();
            int counter = 1;
            table = section.AddTable();

            table.AddColumn(160);
            table.AddColumn(160);
            table.AddColumn(150);
            table.AddColumn(100);
            table.TopPadding = 4;

            foreach (var activityDay in data.ActivityDayList)
            {
                row = table.AddRow();
                row.VerticalAlignment = VerticalAlignment.Bottom;
                row.Format.Alignment = ParagraphAlignment.Right;
                paragraph = row.Cells[0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", activityDay.ParticipantsNum) + helperRepository.Reverse("rtl", "מספר משתתפים: "), YVFonts.font11);

                if(!string.IsNullOrEmpty(activityDay.Location))
                {
                    paragraph = row.Cells[1].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", activityDay.Location) + helperRepository.Reverse("rtl", "מקום הפעילות: "), YVFonts.font11);
                }

                paragraph = row.Cells[2].AddParagraph();
                paragraph.AddFormattedText(activityDay.HouresFromTo + helperRepository.Reverse("rtl", "שעות הפעילות: "), YVFonts.font11);

                paragraph = row.Cells[3].AddParagraph();
                paragraph.AddFormattedText(activityDay.Date + " ." + counter.ToString(), YVFonts.font11);

                counter++;
            }



            //}


            paragraph = section.AddParagraph();
            paragraph.Format.RightIndent = 33;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.SpaceBefore = "0.2cm";
            paragraph.AddFormattedText(data.DepartmentEmail + helperRepository.Reverse("rtl", "לידיעתכם, ההזמנה תקלט ותאושר במערכת רק לאחר חתימה על הטופס והחזרתו למייל "), YVFonts.font9B);
            paragraph.Format.SpaceAfter = "0.5cm";
            #endregion

            #region schedule
            if (data.ShowSchedule)
            {
                paragraph = section.AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.Format.SpaceBefore = 15;
                paragraph.Format.RightIndent = 33;
                paragraph.AddFormattedText("________________________________________________", YVFonts.fontBlueLine);


                paragraph = section.AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.Format.SpaceBefore = 15;
                paragraph.Format.RightIndent = 33;

                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "לו\"ז ותוכנית פעילות מבוקשת:"), YVFonts.fontBig);
                foreach (var day in data.ActivityDayList)
                {
                    if (day.scheduleList != null)
                    {

                        foreach (var item in day.scheduleList)
                        {

                            paragraph = section.AddParagraph();
                            paragraph.Format.Alignment = ParagraphAlignment.Right;
                            paragraph.Format.SpaceBefore = 8;
                            paragraph.Format.RightIndent = 33;
                            paragraph.AddFormattedText(helperRepository.Reverse("rtl", item.From + "-" + item.To + " " + item.Descr), YVFonts.font11);

                        }
                        if (HasComment(data, day))
                        {
                            paragraph = section.AddParagraph();
                            paragraph.Format.Alignment = ParagraphAlignment.Right;
                            paragraph.Format.SpaceBefore = 15;
                            paragraph.Format.RightIndent = 33;

                            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "לתשומת לבכם:"), YVFonts.font11);
                        }

                        //if (day.CommentFood && data.ShowCommentFood)
                        //{
                        //    paragraph = section.AddParagraph();
                        //    paragraph.Format.Alignment = ParagraphAlignment.Right;
                        //    paragraph.Format.SpaceBefore = 8;
                        //    paragraph.Format.RightIndent = 33;

                        //    paragraph.AddFormattedText(helperRepository.Reverse("rtl", " כיבוד וארוחות יתואמו ישירות מול מאיר קיסלסי מנהל הקפיטריה 02-6410582 , <REDACTED-PHONE>"), YVFonts.font11);
                        //}
                        if (day.CommentLecture)
                        {
                            paragraph = section.AddParagraph();
                            paragraph.Format.Alignment = ParagraphAlignment.Right;
                            paragraph.Format.SpaceBefore = 8;
                            paragraph.Format.RightIndent = 33;

                            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "על הגוף המזמין לוודא הימצאות מחשב, מקרן, מסך, רמקולים, מיקרופון תקינים לשימוש המרצה."), YVFonts.font11);
                        }
                        if (day.CommentTestimony)
                        {
                            paragraph = section.AddParagraph();
                            paragraph.Format.Alignment = ParagraphAlignment.Right;
                            paragraph.Format.SpaceBefore = 8;
                            paragraph.Format.RightIndent = 33;

                            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הקבוצה מודעת ומסכימה לכך שבמידה ולא תוכל להתקיים עדות בהתראה קצרה, תתקיים במקומה צפייה מונחית בסרט עדות."), YVFonts.font11);
                        }
                    }

                }
                paragraph = section.AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.Format.SpaceBefore = 15;
                paragraph.Format.RightIndent = 33;
                paragraph.AddFormattedText("________________________________________________", YVFonts.fontBlueLine);


            }

            #endregion

            #region activity day tables

            if (data.ShowDetails)
            {
                bool isFirstTable = true;
                //firt table should have a header row for title
                bool productsHaveDiscount = IsProductsHaveDiscount(data);
                foreach (var activityDay in data.ActivityDayList)
                {
                    //if products has discount-BuildTable
                    //if products do not have discount-BuildTableWithoutDiscountColumns
                    if (productsHaveDiscount)
                    {
                        BuildTable(section, isFirstTable, data, activityDay);
                    }
                    else
                    {
                        BuildTableWithoutDiscountColumns(section, isFirstTable, data, activityDay);
                    }
                    isFirstTable = false;
                }

                #endregion

                #region Comment Food


                if (!string.IsNullOrEmpty(data.FoodDetails))
                {
                    string[] strings = data.FoodDetails.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables
                    table = helperRepository.AddYVTable(section, ParagraphAlignment.Right);
                    helperRepository.AddYVRows(table, strings.Length + 1, ParagraphAlignment.Right);
                    row = table.Rows[0];
                    row.BottomPadding = 10;
                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", " פירוט כיבוד: "), YVFonts.fontBig);


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

                #region Total



                string totalLine;
                if (data.TotalPriceAfterDiscount != "" && data.Discount != "")// there is a discount on the whole order
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
                    row.Format.Shading.Color = YVFonts.shadingColor;

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
                    paragraph.Format.Shading.Color = YVFonts.shadingColor;
                    totalLine = helperRepository.WithCurrencySymbol(totalToPay) + " : " + data.OrderNum + helperRepository.Reverse("rtl", " סה\"כ עלות הזמנה מספר ") + "                                    ";
                    paragraph.Format.RightIndent = 33;
                    paragraph.AddFormattedText(totalLine, YVFonts.fontTotal);
                }
            }
            #endregion

            #region Customer comments


            if (!string.IsNullOrEmpty(data.CustomerComments))
            {
                string[] strings = data.CustomerComments.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables
                table = helperRepository.AddYVTable(section, ParagraphAlignment.Right);
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

            #region cancel
            section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables

            table = helperRepository.AddYVTable(section, ParagraphAlignment.Right);


            helperRepository.AddYVRows(table, 3, ParagraphAlignment.Right);
            table.Rows[0].KeepWith = 2;
            paragraph = table[0, 0].AddParagraph();

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "(ביטול / שינוי-כל שינוי בפרטי ההזמנה שהתקבלה)"), YVFonts.fontSmall);
            paragraph.AddSpace(2);

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "נוהלי ביטול / שינוי הזמנה:"), YVFonts.fontMedium);



            paragraph = table[1, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "ביטול או שינוי פעילות יתאפשר עד 48 שעות לפני שעת הפעילות המתוכננת באמצעות מייל."), YVFonts.fontSmall);
            paragraph = table[2, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "ביטול שלא יתבצע בהתאם לתנאים אלו יחוייב ב 100% מגובה החיוב .") + " .2", YVFonts.fontSmall);


            #endregion

            #region second table

            section.AddParagraph().Format.SpaceBefore = "0.1cm";//for spacing between the tables


            table = helperRepository.AddYVTable(section, ParagraphAlignment.Right);

            helperRepository.AddYVRows(table, 9, ParagraphAlignment.Right);
            table.Rows[0].KeepWith = 8;
            paragraph = table[0, 0].AddParagraph();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "אישור והתחייבות המזמין:"), YVFonts.fontMedium);

            paragraph = table[1, 0].AddParagraph();
            if (data.ShowDetails == false)
            {
                paragraph = table[2, 0].AddParagraph();

                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "קראתי וידועים לי תנאי התשלום, נהלי הביטול וההנחיות המופיעות בהזמנה זו:"), YVFonts.fontSmall);

            }
            else
            {
                paragraph.AddSpace(2);
                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(totalToPay) + helperRepository.Reverse("rtl", "סה“כ עלות הפעילות שאנו מזמינים ומתחייבים לשלם:  "), YVFonts.font12B);

                paragraph = table[2, 0].AddParagraph();

                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כמו כן, קראתי וידועים לי תנאי התשלום, נהלי הביטול וההנחיות המופיעות בהזמנה זו:"), YVFonts.fontSmall);
            }
            paragraph = table[4, 0].AddParagraph();

            paragraph.AddFormattedText("_______________________________________________________________________", YVFonts.fontLine);
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "חתימת המזמין וחותמת הארגון:"), YVFonts.fontSmall);

            paragraph = table[6, 0].AddParagraph();

            paragraph.AddFormattedText("_________________________________________________________________________________", YVFonts.fontLine);
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הערות המזמין:"), YVFonts.fontSmall);
            paragraph = table[8, 0].AddParagraph();

            paragraph.AddFormattedText("___________________________________________________________________________________________", YVFonts.fontLine);







            #endregion



            #region footer

            Footer(section.Footers.FirstPage, data);
            Footer(section.Footers.Primary, data);



            #endregion
        }

        private void SetResponsibleAgentDetails(SchoolConfirmationData data)
        {
           if(data.ResponsibleAgent== "ביקורים רשמיים - הנצחה")
            {
                data.DepartmentEmail = "Official.visit@yadvashem.org.il";
                data.DepartmentPhone = "02-644-3567/484";
                data.Department = "המדור לביקורים רשמיים ואורחי יד ושם";
               // data.DepartmentName = "ביקורים רשמיים";

            }
            else if (data.ResponsibleAgent == "הדרכה - הנצחה")
            {
                //  data.DepartmentEmail = "guiding@yadvashem.org.il";
                data.DepartmentEmail = "visit@yadvashem.org.il";
                data.DepartmentPhone = " 02-644-3033";
                data.Department = "מחלקת הדרכה";
              //  data.DepartmentName = "מחלקת הדרכה";

            }
            //else if (data.ResponsibleAgent == "רכזת תוכן ופרויקטים")
            //{
            //    data.DepartmentEmail = "Naama.galil@yadvashem.org.il";
            //    data.DepartmentPhone = "02-6443146";
            //    data.Department = "מחלקת הדרכה";

            //}

        }

        private bool HasComment(SchoolConfirmationData data,ActivityDay day)
        {
            if (day.CommentLecture ||
                 day.CommentTestimony ||
                (day.CommentFood && data.ShowCommentFood))
                return true;
            return false;
        }

        private bool IsProductsHaveDiscount(SchoolConfirmationData data)
        {
            foreach (var day in data.ActivityDayList)
            {
                foreach (var details in day.detailsList)
                {
                    if (!string.IsNullOrEmpty(details.Discount)&& details.Discount!="0")
                        return true;
                }
              
            }
          
            return false;
        }

        private void BuildTable(Section section,bool isFirstTable,SchoolConfirmationData data,ActivityDay activityDay)
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
            Row row;
            Paragraph paragraph;
            if (isFirstTable)
            {

                 row = table.AddRow();
                row.Cells[0].MergeRight = 5;
                row.BottomPadding = 10;
                paragraph = row.Cells[0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText(data.OrderNum + helperRepository.Reverse("rtl", " פירוט עלות כספית להזמנה מס: "), YVFonts.fontBig);
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
                    table.Rows[0].KeepWith = 2 + activityDay.detailsList.Count;
                }
                else
                {
                    table.Rows[0].KeepWith = 1 + activityDay.detailsList.Count;

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
                    paragraph.AddFormattedText(helperRepository.DiscountWithSymbol(details.Discount, details.DiscountType), YVFonts.fontSmall);

                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.TotalAfterDiscount), YVFonts.fontSmall);


                }
                ////last row
                //row = table.AddRow();
                //row.Format.Alignment = ParagraphAlignment.Right;
                //row.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
                //paragraph = row.Cells[0].AddParagraph();
                //paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.TotalAfterDiscount), YVFonts.fontTableColumnsHeader);

                ////  row.Cells[1].MergeRight = 1;
                //paragraph = row.Cells[2].AddParagraph();
                //paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.Total), YVFonts.fontTableColumnsHeader);

                //// row.Cells[3].MergeRight = 2;
                //paragraph = row.Cells[5].AddParagraph();
                //paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ"), YVFonts.fontTableColumnsHeader);




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

                table.SetEdge(0, innerBorderFromRowNumber, table.Columns.Count, table.Rows.Count - 3, Edge.Horizontal, BorderStyle.Single, 0.5, YVFonts.tableEdgeColor);
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
        }

        private void BuildTableWithoutDiscountColumns(Section section, bool isFirstTable, SchoolConfirmationData data, ActivityDay activityDay)
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
            for (int i = 1; i <= 4; i++)
            {
                column = table.AddColumn();
                if (i == 4)
                {
                    column.Width = 200;
                }
                else
                {
                    column.Width = 110;
                }
            }
            Row row;
            Paragraph paragraph;
            if (isFirstTable)
            {

                row = table.AddRow();
                row.Cells[0].MergeRight = 3;
                row.BottomPadding = 10;
                paragraph = row.Cells[0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText(data.OrderNum + helperRepository.Reverse("rtl", " פירוט עלות כספית להזמנה מס: "), YVFonts.fontBig);
            }

            row = table.AddRow();

            row.HeadingFormat = true;
            row.Height = "0.4cm";
            row.Cells[0].Format.SpaceBefore = 3;
            row.Cells[0].Format.SpaceAfter = 3;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[0].MergeRight =3;


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
                    table.Rows[0].KeepWith = 2 + activityDay.detailsList.Count;
                }
                else
                {
                    table.Rows[0].KeepWith = 1 + activityDay.detailsList.Count;

                }


                //add table headers
                row = table.AddRow();

                paragraph = row.Cells[3].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הפריט"), YVFonts.fontTableColumnsHeader);
                paragraph = row.Cells[2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "עלות ליח'"), YVFonts.fontTableColumnsHeader);

                paragraph = row.Cells[1].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "כמות"), YVFonts.fontTableColumnsHeader);
                paragraph = row.Cells[0].AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ"), YVFonts.fontTableColumnsHeader);
               
                foreach (var details in activityDay.detailsList)
                {
                    row = table.AddRow();

                    paragraph = row.Cells[3].AddParagraph();
                    //אם יש ערך בשדה הערה- אז לשים אותו, אחרת לשים את שם הפריט
                    if (details.Comments.Trim() != string.Empty)
                    {
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.Comments), YVFonts.fontSmall);
                    }
                    else
                    {
                        paragraph.AddFormattedText(helperRepository.Reverse("rtl", details.ItemName), YVFonts.fontSmall);
                    }

                    paragraph = row.Cells[2].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.PricePerUnit), YVFonts.fontSmall);

                    paragraph = row.Cells[1].AddParagraph();
                    paragraph.AddFormattedText(details.Amount, YVFonts.fontSmall);

                    paragraph = row.Cells[0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(details.Total), YVFonts.fontSmall);

                   

                }
                //last row
                //row = table.AddRow();
                //row.Format.Alignment = ParagraphAlignment.Right;
                //row.Shading.Color = YVFonts.shadingColor; //Color.Parse("#E0E8F3");
                //paragraph = row.Cells[0].AddParagraph();
                //paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.TotalAfterDiscount), YVFonts.fontTableColumnsHeader);

                ////  row.Cells[1].MergeRight = 1;
                //paragraph = row.Cells[1].AddParagraph();
                //paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(activityDay.Total), YVFonts.fontTableColumnsHeader);

                //// row.Cells[3].MergeRight = 2;
                //paragraph = row.Cells[3].AddParagraph();
                //paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ"), YVFonts.fontTableColumnsHeader);




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

                table.SetEdge(0, innerBorderFromRowNumber, table.Columns.Count, table.Rows.Count - 3, Edge.Horizontal, BorderStyle.Single, 0.5, YVFonts.tableEdgeColor);
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
        }
        

        private Table AddOneRowTable(Section section,int width1, int width2, int width3=0, int width4=0)
        {
            var table=section.AddTable();
            table.TopPadding = 5;
            table.BottomPadding = 8;
            table.Rows.LeftIndent = 33;
            table.LeftPadding = 0;
            table.RightPadding = 0;
          

            table.Format.Alignment = ParagraphAlignment.Right;
            table.AddColumn(width1);
            table.AddColumn(width2);
            if(width3!=0)
            {
                table.AddColumn(width3);
            }
            if (width4 != 0)
            {
                table.AddColumn(width4);
            }

            var  row = table.AddRow();
           
            row.VerticalAlignment = VerticalAlignment.Bottom;
            row.Format.Alignment = ParagraphAlignment.Right;
            row[0].Format.Borders.Bottom.Visible = true;
            if (table.Columns.Count > 2)
            {
                row[2].Format.Borders.Bottom.Visible = true;
                row[1].Format.RightIndent = 5;
            }
           
            //paragraph.Format.LeftIndent = 15;
            //paragraph.Format.RightIndent = 15;


            return table;
        }

        private void Footer(HeaderFooter footer, SchoolConfirmationData data)
        {
            var frame = footer.AddTextFrame();
            frame.MarginTop = "0.2cm";
            frame.MarginLeft = 20;
            frame.MarginBottom = 10;
            var table = frame.AddTable();

            table.AddColumn(180);
            table.AddColumn(10);


            if (footer.IsFirstPage)
            {
                table.AddColumn(450);
            }

            var row = table.AddRow();
            row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
            var paragraph = row.Cells[0].AddParagraph();


           
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.Department), YVFonts.fontFooter);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "אגף הנצחה וקשרי קהילה"), YVFonts.fontFooter);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText( data.DepartmentPhone + helperRepository.Reverse("rtl", "טל‘ "), YVFonts.fontFooter);
            paragraph.AddLineBreak();
            paragraph.AddFormattedText(data.DepartmentEmail + helperRepository.Reverse("rtl", "מייל ") , YVFonts.fontFooter);


            paragraph = row.Cells[0].AddParagraph();
            paragraph.Format.SpaceBefore = 5;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            var image = paragraph.AddImage(@"Images\footer.jpg");
            // image.WrapFormat.DistanceTop = 50;
            //image.WrapFormat.DistanceLeft = 50;
            paragraph = row.Cells[1].AddParagraph();
            paragraph.AddImage(@"Images\line.jpg");

            //if (footer.IsFirstPage)
            //{
            //    paragraph = row.Cells[2].AddParagraph();
            //    row.Cells[2].Format.RightIndent = 80;
            //    row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
            //    row.Cells[2].VerticalAlignment = VerticalAlignment.Top;
            //    paragraph.Format.Alignment = ParagraphAlignment.Right;
            //    paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.CoordinatorName) + helperRepository.Reverse("rtl", "* לצורך תיאום תכנית הביקור יש לפנות, לרכז/ת "), YVFonts.fontFooter);
            //    paragraph.AddLineBreak();
            //    paragraph.AddSpace(20);
            //    paragraph.AddFormattedText(data.CoordinatorEmail + helperRepository.Reverse("rtl", "מייל ") + " | " + data.CoordinatorPhone + helperRepository.Reverse("rtl", "טלפון ") + "   ", YVFonts.fontFooter);// i created the prefix spaces with alt+255
            //}
        }

        public static void AddHorizontalRuleYV(Section section, double percentWidth, Color? color = null)
        {
             Paragraph paragraph = section.AddParagraph();
            paragraph.Format.LeftIndent = 15;
            paragraph.Format.RightIndent = 15;
            paragraph.Format.Borders.Top.Visible = true;
            paragraph.Format.Borders.Left.Visible = false;
            paragraph.Format.Borders.Right.Visible = false;
            paragraph.Format.Borders.Bottom.Visible = false;
          
        }

        public static void AddHorizontalRule(Section section, double percentWidth, Color? color = null)
        {
            double percent = (percentWidth < 0.0 || percentWidth > 1.0) ? 1.0 : percentWidth;
            Color hrColor = color ?? new Color(96, 96, 96); // Lt Grey default

            Unit contentWidth = section.PageSetup.PageWidth - section.PageSetup.LeftMargin - section.PageSetup.RightMargin;
            Unit indentSize = (contentWidth - (percent * contentWidth)) / 2.0;
            Paragraph paragraph = section.AddParagraph();
            paragraph.Format.LeftIndent = indentSize;
            paragraph.Format.RightIndent = indentSize;
            paragraph.Format.Borders.Top.Visible = true;
            paragraph.Format.Borders.Left.Visible = false;
            paragraph.Format.Borders.Right.Visible = false;
            paragraph.Format.Borders.Bottom.Visible = false;
            paragraph.Format.Borders.Top.Color = hrColor;
        }
    }

}