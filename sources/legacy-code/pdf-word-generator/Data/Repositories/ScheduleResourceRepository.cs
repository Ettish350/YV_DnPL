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
using System.Linq;

namespace Data.Repositories
{
    public class ScheduleResourceRepository : IScheduleResourceRepository
    {
        private IHelperRepository helperRepository;
        Color shadingColor = Color.Parse("#E2EDF6");
        public ScheduleResourceRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;

        }
        IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

        public string CreateDocument(Document document, ScheduleResourceItem data)
        {
            try
            {
                string fileName = DateTime.UtcNow.ToString("yyyy-dd-M--HH-mm-ss");
                Log.Error("in scheduleResourc repository create document.  ");
                document.Styles[StyleNames.Normal].Font.Name = "Arial";
                document.Info.Title = "לוז משאבים לתאריכים " + data.EndDate + " - " + data.StartDate;
                PageSetup pageSetup = document.DefaultPageSetup.Clone();
                CreatePageAndFillContentheb(document, data);
                Log.Error("go to save doc");
                string file_path = helperRepository.SaveDoc(document, fileName, "לוז_משאבים");
                Log.Error("after save doc");
                return file_path;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "exception in invoice repository create document");
                throw ex;
            }
        }


        private void CreatePageAndFillContentheb(Document document, ScheduleResourceItem data)
        {
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(1);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(1);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(1);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(1);
            document.DefaultPageSetup.Orientation = Orientation.Landscape;

            Section section = document.AddSection();
            Paragraph paragraph;
            Table table;
            Row row;

            paragraph = section.AddParagraph();
            paragraph.AddText(data.StartDate + " - " + data.EndDate + helperRepository.Reverse("rtl", " לוז משאבים לתאריכים "));
            paragraph.Format.Font.Underline = Underline.Single;
            paragraph.Format.Font.Bold =true;
            paragraph.Format.Font.Size = 15;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph = section.AddParagraph();

            #region part one:
            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            Color eventsColor = Color.FromRgb(255, 224, 234);
            table = section.AddTable();
            table.Borders.Width = 0.25;
            table.Borders.Left.Width = 0.2;
            table.Borders.Right.Width = 0.2;

            // Before you can add a row, you must define the columns
            Column column = table.AddColumn("4.3cm");
            column = table.AddColumn("4.3cm");
            column = table.AddColumn("4.3cm");
            column = table.AddColumn("4.3cm");
            column = table.AddColumn("4.3cm");
            column = table.AddColumn("4.3cm");
            column = table.AddColumn("2cm");

            foreach (Column col in table.Columns)
            {
                col.Format.Alignment = ParagraphAlignment.Center;
            }
            DateTime StartDate = DateTime.Parse(data.StartDate, new CultureInfo("he-IL"));
            row = table.AddRow();
            row.HeadingFormat = true;
            row.Height = 27;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Format.Font.Bold = true;
            row.Shading.Color = shadingColor;
            row.Cells[6].AddParagraph(helperRepository.Reverse("rtl", "משאב"));
            row.Cells[5].AddParagraph(StartDate.ToString("dd/MM") + helperRepository.Reverse("rtl", " יום ראשון "));
            row.Cells[4].AddParagraph(StartDate.AddDays(1).ToString("dd/MM") + helperRepository.Reverse("rtl", " יום שני "));
            row.Cells[3].AddParagraph(StartDate.AddDays(2).ToString("dd/MM") + helperRepository.Reverse("rtl", " יום שלישי "));
            row.Cells[2].AddParagraph(StartDate.AddDays(3).ToString("dd/MM") + helperRepository.Reverse("rtl", " יום רביעי "));
            row.Cells[1].AddParagraph(StartDate.AddDays(4).ToString("dd/MM") + helperRepository.Reverse("rtl", " יום חמישי "));
            row.Cells[0].AddParagraph(StartDate.AddDays(5).ToString("dd/MM") + helperRepository.Reverse("rtl", " יום שישי "));

            //fill content

            foreach (var item in data.ScheduleResourcesTable)
            {
                Row row1 = table.AddRow();
                row1.Borders.Bottom.Width = 0.2;
                row1.Borders.Bottom.Color = Colors.Gray;
                paragraph = row1.Cells[6].AddParagraph();
                paragraph.Format.Font.Bold = true;
                helperRepository.AddToTextSplitToLines("rtl", paragraph, item.Title + " ", 5);

                for (int i = 1; i < 7; i++)
                {
                    var todayEvents = item.ScheduleEvents.Where(eventItem => (int)eventItem.Date.DayOfWeek == i -1).ToList();
                    if (todayEvents.Count > 0)
                    {
                        int j = 0;
                        foreach (var scheduleEvent in todayEvents)
                        {
                            paragraph = row1.Cells[6 - i].AddParagraph();
                            helperRepository.AddToTextSplitToLines("rtl", paragraph, scheduleEvent.Conetnt + " ", 20);
                            if(scheduleEvent.ClassType.Contains("not-approved"))
                            {
                                paragraph.Format.Shading.Color = SchedulerEventClassType.ClassType["not-approved"];
                            }
                            else
                            {
                                paragraph.Format.Shading.Color = SchedulerEventClassType.ClassType[scheduleEvent.ClassType];
                            }
                            j++;
                            if (!string.IsNullOrEmpty(scheduleEvent.Comments))
                            {
                                //paragraph = row1.Cells[6 - i].AddParagraph();
                                var img = paragraph.AddImage("Images/comment.png");
                                img.Height = 10;
                            }
                            if (todayEvents.Count > 1 && j != todayEvents.Count)
                            {
                                paragraph = row1.Cells[6 - i].AddParagraph();
                                paragraph.AddChar('-', 8);
                                paragraph.Format.Font.Size = 5;
                            }
                        }
                    }
                }
            }
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    for (int j = 1; j < table.Rows.Count; j++)//start from second row
                    {
                        Cell cell = table[j, i];
                        cell.Format.Alignment = ParagraphAlignment.Center;
                        cell.VerticalAlignment = VerticalAlignment.Top;
                    }
                }
            }
            #endregion
            #region part tow:
            section.AddPageBreak();
            paragraph = section.AddParagraph();
            paragraph.Format.Borders.Visible = false;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.SpaceBefore = 0;
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", " הערות כיבוד/סידור לתאריכים " + data.StartDate + " - " + data.EndDate));
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Underline = Underline.Single;
            paragraph.Format.Font.Size = 12;
            foreach (var item in data.Comments)
            {

                paragraph = section.AddParagraph();
                paragraph.Format.Borders.Visible = false;
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.Format.SpaceBefore = 0;
                paragraph.AddLineBreak();
                paragraph.AddText(helperRepository.Reverse("rtl", item.Title));
                paragraph.Format.Font.Bold = true;
                paragraph.Format.Font.Underline = Underline.Single;
                paragraph.AddLineBreak();
                paragraph.AddLineBreak();

                table = section.AddTable();
                table.Borders.Visible = true;
                // Before you can add a row, you must define the columns
                column = table.AddColumn("3cm");
                column = table.AddColumn("3cm");
                column = table.AddColumn("1.5cm");
                column = table.AddColumn("8.8cm");
                column = table.AddColumn("8.8cm");
                column = table.AddColumn("3cm");

                foreach (Column col in table.Columns)
                {
                    col.Format.Alignment = ParagraphAlignment.Right;
                }


                //fill content
                foreach (var scheduleEvent in item.ScheduleEvents)
                {
                    Row row1 = table.AddRow();

                    paragraph = row1.Cells[5].AddParagraph();
                    helperRepository.AddToTextSplitToLines("rtl", paragraph, scheduleEvent.ResourceName, 15);

                    paragraph = row1.Cells[4].AddParagraph();
                    helperRepository.AddToTextSplitToLines("rtl", paragraph, scheduleEvent.Conetnt, 45);

                    paragraph = row1.Cells[3].AddParagraph();
                    helperRepository.AddToTextSplitToLines("rtl", paragraph, scheduleEvent.Comments, 45);
                    paragraph.AddLineBreak();

                    paragraph = row1.Cells[2].AddParagraph();
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    if (scheduleEvent.IsNeedModifyArrangement)
                    {
                        var img = paragraph.AddImage("Images/chair.png");
                        img.Height = 15;
                    }
                    if (scheduleEvent.IsNeedRefreshments)
                    {
                        var img = paragraph.AddImage("Images/coffee.png");
                        img.Height = 15;
                    }

                    paragraph = row1.Cells[1].AddParagraph();
                    helperRepository.AddToTextSplitToLines("rtl", paragraph, scheduleEvent.ReservationName, 15);

                    paragraph = row1.Cells[0].AddParagraph();
                    helperRepository.AddToTextSplitToLines("rtl", paragraph, "נוצר על ידי: " + scheduleEvent.CreatedBy, 15);
                }
            }
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    for (int j = 0; j < table.Rows.Count; j++)//start from second row
                    {
                        Cell cell = table[j, i];
                        cell.Format.Alignment = ParagraphAlignment.Right;
                        cell.VerticalAlignment = VerticalAlignment.Center;


                    }
                }
            }
            #endregion
            #region part three:
            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.SpaceBefore = 0;
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "שעות פעילות בית הספר:"));
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Underline = Underline.Single;
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();

            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddText(helperRepository.Reverse("rtl", "18:00 - 07:00"));
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();

            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddText(helperRepository.Reverse("rtl", "שעות פעילות חריגות:"));
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Font.Underline = Underline.Single;
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();

            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            table = section.AddTable();
            table.Borders.Visible = true;
            table.Format.Alignment = ParagraphAlignment.Center;
            // Before you can add a row, you must define the columns
            column = table.AddColumn("10cm");
            column = table.AddColumn("10cm");
            column = table.AddColumn("8cm");


            foreach (Column col in table.Columns)
            {
                col.Format.Alignment = ParagraphAlignment.Center;
            }

            row = table.AddRow();
            row.HeadingFormat = true;
            row.Format.Alignment = ParagraphAlignment.Center;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Format.Font.Bold = true;
            row.Height = 15;
            row.Shading.Color = shadingColor;
            row.Cells[2].AddParagraph(helperRepository.Reverse("rtl", "תאריך"));
            row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "משאב"));
            row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", "שעות"));

            //fill content
            foreach (var item in data.UnusualActivityHoursComments)
            {
                Row row1 = table.AddRow();
                //row1.Height = 20;

                paragraph = row1.Cells[2].AddParagraph(helperRepository.Reverse("rtl",item[0] ));

                paragraph = row1.Cells[1].AddParagraph(helperRepository.Reverse("rtl",item[1] ));

                paragraph = row1.Cells[0].AddParagraph( item[2]);
        
            }

            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    for (int j = 0; j < table.Rows.Count; j++)//start from second row
                    {
                        Cell cell = table[j, i];
                        cell.Format.Alignment = ParagraphAlignment.Center;
                        cell.VerticalAlignment = VerticalAlignment.Center;


                    }
                }
            }
            #endregion
        }

    }
}