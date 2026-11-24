using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using Xceed.Document.NET;
using Xceed.Words.NET;


namespace Data.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private IHelperRepository helperRepository;
  
        public ScheduleRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;

        }
        IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

        public AttachmentItem createWord(ScheduleReservationList data)
        {
            SetAllData(data);
            //  string fileNameDate="todo";
            //string fileNameReservationNumber = "todo";
            //if(data.schedulesDaysList.Count>1)//for a few days 
            //{
            //    fileNameDate = "All_Days";
            //}
            //else//only 1 day
            //{
            //    fileNameDate = data.schedulesDaysList[0].date.Replace("/", "_");
            //}
            //string fileName = "Schedule_" + data.schedulesDaysList[0].reservationNum + "_" + fileNameDate + "_" + Guid.NewGuid() + ".docx";
            string fileName = ((data.serviceProvider == "Schedule" || string.IsNullOrEmpty(data.serviceProvider))  ? "Schedule_" : data.serviceProvider.Replace(" ","_").Replace("/", "_") + "_") + Guid.NewGuid() + ".docx";
            string fullFilePath = helperRepository.GetSaveLocation(fileName);
            using (var document = DocX.Create(fullFilePath, DocumentTypes.Document))
            {
                #region header footer
                document.SetDirection(Direction.RightToLeft);
                document.MarginTop = 0;
                document.MarginBottom = 0;
                document.MarginRight = 40;
                document.MarginLeft = 40;
                document.AddFooters();
                document.AddHeaders();

                Xceed.Document.NET.Image image;
                Picture picture;
                Paragraph p;
                if (data.reservations != null && data.reservations.Count > 0 && data.reservations.Find(reservation=>reservation.division=="2")  != null)//for commemoration
                {
                    image = document.AddImage(@"Images/footer_with_link.jpg");
                    picture = image.CreatePicture(25, 280);
                }
                else
                {
                    image = document.AddImage(@"Images/schedule_footer.jpg");
                    picture = image.CreatePicture(55,200);
                }

               
                p = document.Footers.First.InsertParagraph();
                p.AppendPicture(picture);
                p = document.Footers.Even.InsertParagraph();
                p.AppendPicture(picture);
                p = document.Footers.Odd.InsertParagraph();
                p.AppendPicture(picture);








                Paragraph footerParagraph = document.Footers.Odd.InsertParagraph();
                footerParagraph.Direction = Direction.RightToLeft;

                footerParagraph.Append(" עמוד ");
                footerParagraph.AppendPageNumber(PageNumberFormat.normal);
                footerParagraph.Append(" מתוך ");
                footerParagraph.AppendPageCount(PageNumberFormat.normal);

                image = document.AddImage(@"Images/schedule_header.jpg");
                picture = image.CreatePicture(80, 140);
                var header = document.Headers;
                document.MarginHeader = 0;
                p = header.Odd.InsertParagraph();
                p.Direction = Direction.RightToLeft;
                p.AppendPicture(picture);

                document.InsertParagraph("");
                if (data.serviceProvider!="") {
                    ;

                    p = document.InsertParagraph(data.serviceProvider!= "Schedule"? data.serviceProvider + " -שיבוצים": "שיבוצים" /*+ data.reservations[0].days[0].date +"-"+ data.reservations[data.reservations.Count-1].days[data.reservations[data.reservations.Count - 1].days.Count-1].date*/).Bold().FontSize(14);
                    p.UnderlineColor(Color.Black);
                    p.Direction = Direction.RightToLeft;
                    p.SpacingAfter(10);
                }
                #endregion
                for (int i = 0; i < data.reservations.Count; i++)
                {
                    bool ismultiReservation = data.reservations.Count > 1 ? true : false;
                    BuildReservationInDocx(document, data.reservations[i], ismultiReservation, i, data.serviceProvider,data.isHideServiceProviderName);
                    if (i < data.reservations.Count - 1 && data.serviceProvider == "")//not last page
                    {
                        document.InsertParagraph().InsertPageBreakAfterSelf();

                    }
                    else if (data.serviceProvider != "") {
                        p = document.InsertParagraph().AppendLine("*********************************");
                        p.Direction = Direction.RightToLeft;

                    }
                }

                p = document.InsertParagraph("בהצלחה והדרכה נעימה").Bold().FontSize(13);
                p.Alignment = Alignment.center;


                foreach (var par in document.Paragraphs)
                {
                    par.Font("Arial");
                }
                helperRepository.SaveWordDoc(document, fullFilePath);
                return helperRepository.AttachDocument(fileName, ".docx", "schedule");
            }


        }

        //private void BuildFooter(DocX document, ScheduleReservationList reservations)
        //{
        //    Table t = document.AddTable(1, 1);
        //    t.SetColumnWidth(0, 200);
        //    t.SetDirection(Direction.RightToLeft);
        //    Color color = Color.Black;
        //    Border border = new Border(BorderStyle.Tcbs_single, BorderSize.one, 2, color);
        //    Border borderNone = new Border(BorderStyle.Tcbs_none, BorderSize.one, 1, color);
        //    t.SetBorder(TableBorderType.Top, borderNone);
        //    t.SetBorder(TableBorderType.Bottom, borderNone);
        //    t.SetBorder(TableBorderType.Right, borderNone);
        //    t.SetBorder(TableBorderType.Left, border);
        //    t.Alignment = Alignment.right;
           
        //    var row = t.Rows[0];
        //   // row.TableHeader = true;
           
        //    var cell = row.Cells[0];
        //    var p = cell.Paragraphs[0];
        //     char charToPad = ' ';//THIS IS NOT A SPACE. THIS IS ALT+255
        //    p.Append("בית הספר הבין לאומי להוראת השואה").Bold().FontSize(11).SpacingLine(16);

        //    //p.AppendLine("מחלקת " + reservations.departmentName).Bold().FontSize(11).SpacingLine(16);
        //    //p.AppendLine("טל " + reservations.departmentTelephone + " | " + charToPad+"פקס " + reservations.departmentFax).Bold().FontSize(11).SpacingLine(16);

           


        //    var image = document.AddImage(@"Images/signs.jpg");
        //    var picture = image.CreatePicture(25, 150);
          

        //    p = document.Footers.First.InsertParagraph();
        //    p.IndentationBefore = 45f;
        //    p.AppendPicture(picture);
        //    p.InsertTableBeforeSelf(t);
        //    p = document.Footers.Even.InsertParagraph();
        //    p.IndentationBefore =45f;
        //    p.AppendPicture(picture);
        //    p.InsertTableBeforeSelf(t);
        //    p = document.Footers.Odd.InsertParagraph();
        //    p.IndentationBefore = 45f;
        //    p.AppendPicture(picture);
        //    p.InsertTableBeforeSelf(t);
        //}

        private void BuildReservationInDocx(Document document,ScheduleReservation reservation,bool isMultiReservation,int counter,string serviceProvider,bool isHideServiceProviderName =false)
        {
            Paragraph p;
            if (isMultiReservation || (!isMultiReservation&& counter==0))
            {
                if (reservation.isEventReservation)
                {
                    p = document.InsertParagraph(reservation.reservationNum + " הכשרה/ השתלמות מספר").Bold().FontSize(13);
                }
                else {
                    p = document.InsertParagraph(reservation.reservationNum + " הזמנה מספר").Bold().FontSize(13);
                }
                p.SpacingBefore(20);
                p.Direction = Direction.RightToLeft;
            }
            if (serviceProvider == "") {
                p = document.InsertParagraph("מחלקת " + reservation.departmentName).FontSize(14);
                p.Direction = Direction.RightToLeft;
                p = document.InsertParagraph("טל " + reservation.departmentTelephone).FontSize(14);
                p.Direction = Direction.RightToLeft;
                p = document.InsertParagraph("פקס " + reservation.departmentFax).FontSize(14);
                p.Direction = Direction.RightToLeft;
            }
          




            int dayCounter = 0;
            foreach (var day in reservation.days)
            {
                if (serviceProvider == "")
                {
                    if (!isMultiReservation && dayCounter > 0)
                    {
                        document.InsertParagraph().InsertPageBreakAfterSelf();
                    }
                    var activityDayType = "יום עיון";
                    if (reservation.isEventReservation) {
                        activityDayType = "הכשרה/ השתלמות";
                    }
                    p = document.InsertParagraph(activityDayType + " " + reservation.instName).Bold().FontSize(14);
                    p.Direction = Direction.RightToLeft;
                    p.Alignment = Alignment.center;
                    p.SpacingAfter(10);
                    p.SpacingBefore(10);
                    if (!string.IsNullOrEmpty(reservation.groupName))
                    {
                        p = document.InsertParagraph(reservation.groupName).Bold().FontSize(14);
                        p.Direction = Direction.RightToLeft;
                        p.Alignment = Alignment.center;
                        p.SpacingAfter(10);
                        p.SpacingBefore(10);
                    }

                    p = document.InsertParagraph("יום "+day.dayOfWeek+" "+HebrewDate.GetHebDateByDate(day.date) + " " + day.date).FontSize(14);
                    p.Direction = Direction.RightToLeft;
                    p.Alignment = Alignment.center; 
                    // p.IndentationBefore = 33;
                    p.SpacingAfter(10);
                    if (!string.IsNullOrEmpty(day.subject))
                    {
                        p = document.InsertParagraph("נושא יום העיון").Bold().FontSize(14);

                        p.Direction = Direction.RightToLeft;
                        p.Alignment = Alignment.center;

                        p = document.InsertParagraph(day.subject).Bold().FontSize(14);
                        p.Direction = Direction.RightToLeft;
                        p.SpacingBefore(10);
                    }
                    p.SpacingAfter(10);
                    if (!reservation.isEventReservation)
                    {
                        p = document.InsertParagraph("מספר הקבוצות " + day.numberOfGroups).FontSize(14);
                        p.Direction = Direction.RightToLeft;
                    }
                    p = document.InsertParagraph("שפת הדרכה " + reservation.language).FontSize(14);
                    p.Direction = Direction.RightToLeft;


                    p = document.InsertParagraph("תחילת הפעילות " + day.startTime).FontSize(14);
                    p.Direction = Direction.RightToLeft;


                    p = document.InsertParagraph("סיום הפעילות " + day.endTime).FontSize(14);
                    p.Direction = Direction.RightToLeft;

                   

                    if (!reservation.isEventReservation)
                    {
                       p = document.InsertParagraph("שכבת גיל " + reservation.ageGroup).FontSize(14);
                       p.Direction = Direction.RightToLeft;
                    }

                    if(!string.IsNullOrEmpty(day.location))
                    {
                        p = document.InsertParagraph("מיקום הפעילות " + day.location).FontSize(14);
                        p.Direction = Direction.RightToLeft;
                    }
                    
                    p = document.InsertParagraph("שם הרכז " + reservation.coordinator).FontSize(14);
                    p.Direction = Direction.RightToLeft;

                    if (!reservation.isEventReservation && !string.IsNullOrEmpty(reservation.instCity))
                    {
                        p = document.InsertParagraph("יישוב המזמין " + reservation.instCity).FontSize(14);
                        p.Direction = Direction.RightToLeft;
                    }
                    p.AppendLine();
                    p.AppendLine();
                    p = document.InsertParagraph("תכנית היום").FontSize(14);
                    p.UnderlineColor(Color.Black);
                    p.Direction = Direction.RightToLeft;
                    p.SpacingAfter(10);
                }
                else 
                {
                    if (reservation.isEventReservation)
                    {
                        p = document.InsertParagraph("(" + reservation.reservationNum + ") פירוט עבודתך בהכשרה/ השתלמות").Bold().FontSize(11);
                    }
                    else {
                        p = document.InsertParagraph("(" + reservation.reservationNum + ") פירוט עבודתך בהזמנה").Bold().FontSize(11);

                    }
                    addReservationDetailsTable(document, reservation, day);
                    p.UnderlineColor(Color.Black);
                    p.Direction = Direction.RightToLeft;
                    p.SpacingBefore(10);
                }
                foreach (var group in day.groups)
                {

                    addGroupDetailsTable(document, group, reservation.isEventReservation, isHideServiceProviderName);

                }
                dayCounter++;
            }
        }
        private void SetAllData(ScheduleReservationList dataList)
        {
            foreach (var reservation in dataList.reservations)
            {

                foreach (var data in reservation.days)
                {
                 
                    foreach (var group in data.groups)
                    {
                        if (group.details == null)
                        {
                            group.details = new List<ScheduleGroupDetails>();
                        }
                        var goodDetails = new List<ScheduleGroupDetails>();
                        foreach (var de in group.details)
                        {
                            if ((de.product == "סיור במוזיאון" || de.product == "חדר השאלות הגדולות") && string.IsNullOrEmpty(de.resource))
                                continue;

                            goodDetails.Add(de);
                        }
                        group.details = goodDetails;
                    }
                }
            }
        }

        //private string FixText(string text)
        //{
        //    return text;
        //    //text = text.Replace("(", "~~");
        //   // text = text.Replace(")", "(");
        //    //text = text.Replace("~~", ")");
        //    // text = text.Replace(" '", "' ");


        //}
        private void addReservationDetailsTable(Document document, ScheduleReservation reservation, ScheduleActivityDay day) {
            Table t;
            if (reservation.isEventReservation)
            {
                t = document.AddTable(4, 6);
            }
            else {
                t = document.AddTable(5, 6);
            }
            
            Paragraph p;
            Color shadingColor = Color.FromArgb(226, 237, 246);
            float[] width = new float[] { 20, 16, 14, 15, 14, 14 };
            t.SetWidthsPercentage(width, 570);
            // t.Design = TableDesign.ColorfulShadingAccent5;

            t.Alignment = Alignment.center;
            //t.IndentFromLeft = 80;
            t.SetDirection(Direction.RightToLeft);
            var row = t.Rows[0];
            var cell = row.Cells[0];
            cell.Paragraphs[0].Append("תאריך").Bold();
            cell.FillColor = shadingColor;
            cell = row.Cells[1];
            cell.Paragraphs[0].Append(day.date);
            cell = row.Cells[2];
            cell.Paragraphs[0].Append("תאריך עברי").Bold();
            cell.FillColor = shadingColor;
            cell = row.Cells[3];
            cell.Paragraphs[0].Append(HebrewDate.GetHebDateByDate(day.date));
            cell = row.Cells[4];
            cell.Paragraphs[0].Append("מחלקה מזמינה").Bold();
            cell.FillColor = shadingColor;
            cell = row.Cells[5];
            cell.Paragraphs[0].Append(reservation.departmentName);
            row = t.Rows[1];
            cell = row.Cells[0];
            if (reservation.isEventReservation) {
                cell.Paragraphs[0].Append("מספר הכשרה/ השתלמות").Bold();
            }
            else
            {
                cell.Paragraphs[0].Append("מספר הזמנה").Bold();
            }
            cell.FillColor = shadingColor;
            cell = row.Cells[1];
            cell.Paragraphs[0].Append(reservation.reservationNum);
            cell = row.Cells[2];
            cell.Paragraphs[0].Append("שעות הפעילות").Bold(); 
            cell.FillColor = shadingColor;
            cell = row.Cells[3];
            cell.Paragraphs[0].Append(day.startTime+"-"+day.endTime);
            cell = row.Cells[4];
            cell.Paragraphs[0].Append("טלפון המחלקה").Bold();
            cell.FillColor = shadingColor;
            cell = row.Cells[5];
            cell.Paragraphs[0].Append(reservation.departmentTelephone);
            row = t.Rows[2];
            cell = row.Cells[0];
            if (reservation.isEventReservation)
            {
                cell.Paragraphs[0].Append("יום בשבוע").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[1];
                cell.Paragraphs[0].Append("יום " + day.dayOfWeek);
            }
            else
            {
                cell.Paragraphs[0].Append("שם המוסד").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[1];
                cell.Paragraphs[0].Append(reservation.instName);
            }

            cell = row.Cells[2];
            if (reservation.isEventReservation)
            {
                cell.Paragraphs[0].Append("נושא הארוע").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[3];
                cell.Paragraphs[0].Append(reservation.groupName);
                cell = row.Cells[4];
                cell.Paragraphs[0].Append("רכז").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[5];
                cell.Paragraphs[0].Append(reservation.coordinator);

                if(!string.IsNullOrEmpty(day.location))
                {
                    row = t.Rows[3];
                    cell = row.Cells[0];
                    cell.Paragraphs[0].Append("מיקום פעילות").Bold();
                    cell.FillColor = shadingColor;
                    cell = row.Cells[1];
                    cell.Paragraphs[0].Append(day.location);
                }
                
            }



            if (!reservation.isEventReservation)
            {
                cell.Paragraphs[0].Append("שם ההזמנה").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[3];
                cell.Paragraphs[0].Append(reservation.groupName);
                cell = row.Cells[4];
                cell.Paragraphs[0].Append("שפת ההדרכה").Bold();
                cell.FillColor = shadingColor;   
                cell = row.Cells[5];
                cell.Paragraphs[0].Append(reservation.language);
                row = t.Rows[3];
                cell = row.Cells[0];
                cell.Paragraphs[0].Append("מספר קבוצות בהזמנה").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[1];
                cell.Paragraphs[0].Append(day.numberOfGroups.ToString());
                cell = row.Cells[2];
                cell.Paragraphs[0].Append("יום בשבוע").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[3];
                cell.Paragraphs[0].Append("יום " + day.dayOfWeek);
                cell = row.Cells[4];
                cell.Paragraphs[0].Append("שכבת גיל").Bold();
                cell.FillColor = shadingColor;
                cell = row.Cells[5];
                cell.Paragraphs[0].Append(reservation.ageGroup);
                if (!string.IsNullOrEmpty(day.location))
                {
                    row = t.Rows[4];
                    cell = row.Cells[0];
                    cell.Paragraphs[0].Append("מיקום פעילות").Bold();
                    cell.FillColor = shadingColor;
                    cell = row.Cells[1];
                    cell.Paragraphs[0].Append(day.location);
                }
              
            }
            p = document.InsertParagraph();
            p.InsertTableAfterSelf(t);
        }

        private void addGroupDetailsTable(Document document, ScheduleGroup group, bool isEventReservation=false, bool isHideServiceProviderName=false)
        {
            Table t;
            if (group.details.Count>0)
            {
                 t = document.AddTable(group.details.Count + 2, 6);

            }
            else
            {
                 t = document.AddTable(1, 6);

            }
            Paragraph p;
           // t.Design = TableDesign.ColorfulShadingAccent5;

            t.Alignment = Alignment.center;
            //t.IndentFromLeft = 80;
            t.SetDirection(Direction.RightToLeft);
            var row = t.Rows[0];
            row.TableHeader = true;
            var cell =row.Cells[0];
            //cell.Paragraphs[0].Append("קבוצה " + group.groupNumber + " מספר משתתפים " + group.participantsNumber+" נותן שירות: " +group.groupServiceProvider+" משאב: "+);

            if (!isEventReservation)
            {
                if (group.groupName.Trim() != "")
                {
                    cell.Paragraphs[0].Append(group.groupName);
                }
                else
                {
                    cell.Paragraphs[0].Append("קבוצה ").Bold();
                    cell.Paragraphs[0].Append(group.groupNumber);
                }
            }
            cell.Paragraphs[0].Append(" ");
            cell.Paragraphs[0].Append(" מספר משתתפים ").Bold();
            cell.Paragraphs[0].Append(group.participantsNumber);
            cell.Paragraphs[0].Append(" ");
            if (!string.IsNullOrEmpty(group.groupServiceProvider)&&!isHideServiceProviderName)
            {
                cell.Paragraphs[0].Append(" מדריך/כה  ").Bold();
                cell.Paragraphs[0].Append(group.groupServiceProvider);
            }
            if (!string.IsNullOrEmpty(group.groupResource))
            {
               

                cell.Paragraphs[0].Append(","+group.groupResource);
            }
           
            Color shadingColor = Color.FromArgb(226, 237, 246);
            cell.FillColor = shadingColor;
            t.Rows[0].MergeCells(0, 5);
            // t.AutoFit = AutoFit.Contents;
            float[] width = new float[] { 14, 14, 18, 15, 15, 14 };
            t.SetWidthsPercentage(width, 570);
            if (group.details!=null&&group.details.Count > 0)
            {
               
                cell = t.Rows[1].Cells[0];
                cell.Paragraphs[0].Append("שעות");
                cell.Width = 20;
                p = t.Rows[1].Cells[1].Paragraphs[0].Append("מוצר");
                cell = t.Rows[1].Cells[2];
                // cell.Width = 140;
                cell.Paragraphs[0].Append("נושא");

                p = t.Rows[1].Cells[3].Paragraphs[0].Append("נותן שרות");
                p = t.Rows[1].Cells[4].Paragraphs[0].Append("מיקום");
                p = t.Rows[1].Cells[5].Paragraphs[0].Append("הערות");

                int rowNum = 2;
                foreach (var cur in group.details)
                {
                    p = t.Rows[rowNum].Cells[0].Paragraphs[0].Append(cur.startHour).Append("-").Append(cur.endHour);
                    p = t.Rows[rowNum].Cells[1].Paragraphs[0].Append(cur.product);
                    p = t.Rows[rowNum].Cells[2].Paragraphs[0].Append(cur.subject);
                    p = t.Rows[rowNum].Cells[3].Paragraphs[0].Append(isHideServiceProviderName?"":cur.serviceProvider);
                    p = t.Rows[rowNum].Cells[4].Paragraphs[0].Append(cur.resource);
                    p = t.Rows[rowNum].Cells[5].Paragraphs[0].Append(cur.comments);
                    rowNum++;
                }
                //Color color = Color.FromArgb(5, 124, 174);
                //Border border = new Border(BorderStyle.Tcbs_thinThickThinSmallGap, BorderSize.one,2, color);

                //t.SetBorder(TableBorderType.Bottom, border);
                //t.SetBorder(TableBorderType.Top, border);
                //t.SetBorder(TableBorderType.Left, border);
                //t.SetBorder(TableBorderType.Right, border);

                int rowNumber = 0;
                foreach (var item in t.Rows)
                {
                    item.SetDirection(Direction.RightToLeft);
                    if (rowNumber == 0)//first row
                    {
                        item.Height = 30;
                    }
                    else
                    {
                        item.MinHeight = 20;
                    }
                    foreach (var cel in item.Cells)
                    {
                        cel.SetDirection(Direction.RightToLeft);
                        cel.TextDirection = TextDirection.right;
                        cel.VerticalAlignment = VerticalAlignment.Center;
                        foreach (var par in cel.Paragraphs)
                        {
                            par.Direction = Direction.RightToLeft;
                            if (rowNumber == 0 || rowNumber == 1)
                            {
                                par.Bold();
                                par.FontSize(11);
                            }
                            else
                            {
                                par.FontSize(10);
                            }
                        }


                    }

                    rowNumber++;
                }
            }
           
                p = document.InsertParagraph();
                p.InsertTableAfterSelf(t);
             
            
            
        }


       
    }
}