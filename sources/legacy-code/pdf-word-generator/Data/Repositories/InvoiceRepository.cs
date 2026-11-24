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

namespace Data.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private IHelperRepository helperRepository;
        Color shadingColor = Color.Parse("#E2EDF6");
        Font fontUnderline = new Font();
        Font font13b = new Font();
        Font red = new Font();
       
        public InvoiceRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;
            fontUnderline.Underline = Underline.Single;
            font13b.Bold = true;
            font13b.Size = 13;
            red.Bold = true;
            red.Color = Colors.Red;
            red.Size = 13;
        }
        IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
       

        public string CreateDocument(Document document, InvoiceData data)
        {
            try
            {

            Log.Error("in invoice repository create document.  ");
            document.Styles[StyleNames.Normal].Font.Name = "Arial";
            document.Info.Title = "Yad Vashem Invoice";
            document.Info.Subject = "Yad Vashem Invoice";
            document.Info.Author = "Yad Vashem";

            switch (data.lang)
            {
                case "עברית":
                    {
                        Log.Error("lang heb");
                        CreatePageAndFillContentheb(document, data);
                        break;
                    }
                case "אנגלית":
                    {
                        Log.Error("lang eng");
                            CreatePageAndFillContenteng(document, data);
                        break;
                    }
                default:
                    {
                            Log.Error("no lang ");

                            CreatePageAndFillContentheb(document, data);
                            break;
                        }
                  
            }
            Log.Error("go to save doc");
            string file_path = helperRepository.SaveDoc(document, data.invoiceNum, "invoice");
              
            Log.Error("after save doc");
            return file_path;

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "exception in invoice repository create document");
                throw ex;
            }
        }
        private void CreatePageAndFillContenteng(Document document, InvoiceData data)
        {
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(2);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(2);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(4.5);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(4.5);
            document.DefaultPageSetup.Orientation = Orientation.Portrait;


            //header
            Section section = document.AddSection();


            Paragraph paragraph = section.Headers.Primary.AddParagraph();
            Image image;
            if (IsHanzacha(data))
            {
                image = paragraph.AddImage("Images/logo.jpg");


            }
            else
            {
                image = paragraph.AddImage("Images/logo_school_eng.jpg");
            }



            #region footer
            Table table = section.Footers.Primary.AddTable();
            table.AddColumn(300);
            table.AddColumn();
            Row row = table.AddRow();

            // row.Shading.Color = shadingColor;
            row.Cells[0].VerticalAlignment = VerticalAlignment.Bottom;
            paragraph = row[0].AddParagraph();

            paragraph.AddFormattedText("Created by: " + helperRepository.Reverse("ltr", data.createdBy));

            if (IsHanzacha(data))
            {

                row[1].AddImage(@"Images\logo_eng.jpg");
                
            }
            else
            {
                row[1].AddImage(@"Images\icons_eng.jpg");

            }

            #endregion



            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddFormattedText(DateTime.Now.ToString("dd/MM/yyyy"));


            //to:
            paragraph = section.AddParagraph();
            paragraph.AddText("To: ");
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("ltr", data.name));
            paragraph.AddLineBreak();
            if (!string.IsNullOrEmpty(data.address))
            {
                paragraph.AddText(helperRepository.Reverse("ltr", data.address));
                paragraph.AddLineBreak();
            }
            if (!string.IsNullOrEmpty(data.telephone))
            {
                paragraph.AddText(helperRepository.Reverse("ltr", data.telephone));
                paragraph.AddLineBreak();
            }
            paragraph.AddText("I.D: " + data.semel);
            paragraph.AddLineBreak();

            if (!string.IsNullOrEmpty(data.relatedAccountName))
            {
                paragraph.AddText("For: " + helperRepository.Reverse("ltr", data.relatedAccountName));
                paragraph.AddLineBreak();
            }
            if (!string.IsNullOrEmpty(data.relatedAccountAddress))
            {
                paragraph.AddText(helperRepository.Reverse("ltr", data.relatedAccountAddress));
                paragraph.AddLineBreak();
            }
            if (data.tables.Count == 1)
            {
                if (!string.IsNullOrEmpty(data.tables[0].contact))
                {
                    paragraph.AddText("For: " + helperRepository.Reverse("ltr", data.tables[0].contact));
                }
                if (!string.IsNullOrEmpty(data.tables[0].groupName) || !string.IsNullOrEmpty(data.tables[0].reservationNum))
                {
                    paragraph.AddLineBreak();
                    paragraph.AddText("_______________________");
                    paragraph.AddLineBreak();
                    if (!string.IsNullOrEmpty(data.tables[0].groupName) && data.tables[0].groupName != "no-name")
                    {
                        paragraph.AddText("Group Name: " + helperRepository.Reverse("ltr", data.tables[0].groupName));
                        paragraph.AddLineBreak();
                    }
                    if (!string.IsNullOrEmpty(data.tables[0].groupName) || !string.IsNullOrEmpty(data.tables[0].reservationNum))
                    {
                        paragraph.AddText("Reservation Number: " + helperRepository.Reverse("ltr", data.tables[0].reservationNum));
                    }
                }
            }




            /********************************/
            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = 20;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            if (!string.IsNullOrEmpty(data.status))
            {
                //set first letter to upper case
                if (data.status.Length > 1)
                    data.status = char.ToUpper(data.status[0]) + data.status.Substring(1);
                paragraph.AddFormattedText(data.status, font13b);
                paragraph.AddLineBreak();
            }
            if (!data.isCancel)
            {
                paragraph.AddFormattedText(" INVOICE NO. " + data.invoiceNum + helperRepository.Reverse("rtl", " חשבון מספר "), font13b);
            }

            else // cancel
            {
                paragraph.AddFormattedText("CREDIT", red);
                paragraph.AddFormattedText(" INVOICE NO. " + data.invoiceNum, font13b);
                // paragraph.AddFormattedText(helperRepository.Reverse("rtl", " מספר "), font13b);

            }

            if (data.tables.Count > 1)//union
            {
                paragraph.AddLineBreak();
                //paragraph = section.AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("ltr", "Total For Payment: ")+ helperRepository.WithCurrencySymbol(data.InvoiceTotalPriceAfterDiscount,data.currencyCode), font13b);

                paragraph.AddLineBreak();
                paragraph = section.AddParagraph();
                paragraph.AddFormattedText(helperRepository.Reverse("ltr", "Payment Details: "));

            }

            paragraph.Format.SpaceAfter = 10;

            foreach (var tableValue in data.tables)
            {
                if (data.tables.Count > 1)//union
                {
                    paragraph.AddLineBreak();
                    paragraph = section.AddParagraph();
                    string rowHeader = "Reservation No. " + tableValue.reservationNum + " Group name:v" + tableValue.groupName + " Contact: " + tableValue.contact;
                    if (IsHanzacha(data))//without contact
                    {
                        rowHeader = "Reservation No. " + tableValue.reservationNum + " Group name:v" + tableValue.groupName;
                    }
                    paragraph.AddFormattedText(helperRepository.Reverse("ltr", rowHeader));
                    paragraph.Format.SpaceAfter = 10;
                }
                table = section.AddTable();
                table.Borders.Width = 0.25;
                table.Borders.Left.Width = 0.5;
                table.Borders.Right.Width = 0.5;
                //  if (!data.isCancel)
                {
                    // Before you can add a row, you must define the columns
                    Column column;
                    column = table.AddColumn("2.4cm");
                    column = table.AddColumn("4.3cm");
                    column = table.AddColumn("2.2cm");
                    column = table.AddColumn("1cm");
                    column = table.AddColumn("1.8cm");
                    column = table.AddColumn("2cm");
                    column = table.AddColumn("3.7cm");

                    foreach (Column col in table.Columns)
                    {
                        col.Format.Alignment = ParagraphAlignment.Center;
                    }

                    row = table.AddRow();
                    row.HeadingFormat = true;
                    row.Height = 27;
                    row.Format.Alignment = ParagraphAlignment.Center;
                    row.Format.Font.Bold = true;
                    row.Shading.Color = shadingColor;
                    row.Cells[0].AddParagraph("Activity date");
                    row.Cells[1].AddParagraph("Details");
                    row.Cells[2].AddParagraph("Unit price");
                    row.Cells[3].AddParagraph("Qty");
                    row.Cells[4].AddParagraph("Total");
                    row.Cells[5].AddParagraph("Discount");
                    row.Cells[6].AddParagraph("Total After Discount");

                    //fill content



                    int lenght = tableValue.tableRowDetails.Count;
                    for (int i = 0; i < lenght; i++)
                    {

                        row = table.AddRow();
                        row.Borders.Bottom.Width = 0.2;
                        row.Borders.Bottom.Color = Colors.Gray;
                        row.Cells[0].AddParagraph(tableValue.tableRowDetails[i].dateForDisplay);
                        row.Cells[1].AddParagraph(helperRepository.Reverse("ltr", tableValue.tableRowDetails[i].perutEn));
                        row.Cells[2].AddParagraph(helperRepository.WithCurrencySymbol(tableValue.tableRowDetails[i].price, data.currencyCode));
                        row.Cells[3].AddParagraph(tableValue.tableRowDetails[i].amount);
                        row.Cells[4].AddParagraph(helperRepository.WithCurrencySymbol(tableValue.tableRowDetails[i].total, data.currencyCode));
                        row.Cells[5].AddParagraph(helperRepository.DiscountWithSymbol(tableValue.tableRowDetails[i].discount, tableValue.tableRowDetails[i].discountType,data.currencyCode));
                        row.Cells[6].AddParagraph(helperRepository.WithCurrencySymbol(tableValue.tableRowDetails[i].totalAfterDiscount, data.currencyCode));
                        row.Height = 27;

                        if (i + 1 != lenght)//leave last border bottom
                        {
                            row.Borders.Bottom.Visible = false;
                        }


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
                }

            



            // Add the notes paragraph
            paragraph = section.AddParagraph();
            string totalToPay = tableValue.totalPriceAfterDiscount != string.Empty ? tableValue.totalPriceAfterDiscount : tableValue.totalPrice;
            if (tableValue.totalPriceAfterDiscount != "" && tableValue.discount != "" && tableValue.discount != "0")// there is a discount on the whole order
            {

                table = section.AddTable();
                table.Rows.LeftIndent = "7.8cm";
                table.LeftPadding = 0;
                table.RightPadding = 0;
                table.TopPadding = 0;
                table.BottomPadding = 0;

                table.AddColumn(200);
                table.AddColumn(10);
                table.AddColumn(300);
                row = table.AddRow();
                row.BottomPadding = 5;
                row = table.AddRow();
                row.BottomPadding = 5;
                row = table.AddRow();
                    row.BottomPadding = 5;



                    //first row
                    paragraph = table.Rows[0][0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText("Total Amount: ");
                paragraph = table.Rows[0][2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(tableValue.totalPrice,data.currencyCode)+ helperRepository.WithCurrencyCodeIfNeed(data.currencyCode));


                //second row
                paragraph = table.Rows[1][0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;

                paragraph.AddFormattedText("Discount: ");
                paragraph = table.Rows[1][2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.DiscountWithSymbol(tableValue.discount, tableValue.discountType ,data.currencyCode));



                //third row
                paragraph = table.Rows[2][0].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;

                paragraph.AddFormattedText("Total After Discount: ", TextFormat.Bold);
                paragraph = table.Rows[2][1].AddParagraph();

                paragraph.AddFormattedText(" ");//for space
                paragraph = table.Rows[2][2].AddParagraph();
                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(tableValue.totalPriceAfterDiscount, data.currencyCode) + helperRepository.WithCurrencyCodeIfNeed(data.currencyCode), TextFormat.Bold);


                    if ((!string.IsNullOrEmpty(data.currencyCode)) &&  data.currencyCode != "ILS")
                    {
                        row = table.AddRow();
                        paragraph = table.Rows[3][0].AddParagraph();
                        paragraph.Format.Alignment = ParagraphAlignment.Right;

                        paragraph.AddFormattedText("Total After Discount in ILS: ", TextFormat.Bold);
                        paragraph = table.Rows[3][1].AddParagraph();

                        paragraph.AddFormattedText(" ");//for space
                        paragraph = table.Rows[3][2].AddParagraph();
                        paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(data.InvoiceTotalPriceAfterDiscountInILS), TextFormat.Bold);

                    }


                }
            else
            {
                paragraph.Format.SpaceBefore = 10;
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                if (!data.isCancel)
                {
                    paragraph.AddFormattedText("Total Amount: " + helperRepository.WithCurrencySymbol(totalToPay,data.currencyCode) + helperRepository.WithCurrencyCodeIfNeed(data.currencyCode), TextFormat.Bold);
                }
                else //cancel
                {
                    paragraph.AddFormattedText("Total: " + helperRepository.WithCurrencySymbol(totalToPay,data.currencyCode) + helperRepository.WithCurrencyCodeIfNeed(data.currencyCode), TextFormat.Bold);
                }
                if ((!string.IsNullOrEmpty(data.currencyCode)) && data.currencyCode != "ILS")
                {
                    paragraph.AddLineBreak();

                    paragraph.AddLineBreak();
                    paragraph.AddFormattedText("Total Amount In ILS: " + helperRepository.WithCurrencySymbol(data.InvoiceTotalPriceAfterDiscountInILS), TextFormat.Bold);

                }
                }
           

            }
            if (!string.IsNullOrEmpty(data.comments))
            {
                paragraph = section.AddParagraph();
                paragraph.Format.SpaceBefore = 20;

                if (data.isCancel)
                {
                    paragraph.AddFormattedText("Credit for invoice No. "+ data.comments );
                }
                else
                {
                    helperRepository.AddToTextSplitToLines("ltr", paragraph, "Comments: " + data.comments, 110);

                }
                paragraph.AddLineBreak();
            }

            if (!string.IsNullOrEmpty(data.gefenType))
            {
                paragraph = section.AddParagraph();
                paragraph.Format.SpaceBefore = 20;
                helperRepository.AddToTextSplitToLines("ltr", paragraph, "Gefen Type: " + data.gefenType, 110);

                paragraph.AddLineBreak();
            }
            paragraph = section.AddParagraph();
      
            paragraph.Format.SpaceBefore = 17;
            paragraph.AddText("For more information please contact: "+helperRepository.Reverse("ltr", data.departmentEn) );


            paragraph = section.AddParagraph();
         

            paragraph.Format.SpaceBefore = 15;

            paragraph.AddFormattedText("Please note this invoice number upon payment", TextFormat.Bold);
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();
            if ((!string.IsNullOrEmpty(data.currencyCode)) && data.currencyCode != "ILS")
            {
                paragraph.Format.LineSpacing = 15;
                paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
                paragraph.AddText("Please transfer payment only in the currency noted on the invoice");
                paragraph.AddLineBreak();
                paragraph.AddText("SWIFT CHARGE CODE : 'OUR'");
                paragraph.AddLineBreak();
                paragraph.AddText("PAYMENT MUST BE RECEIVED IN FULL. ALL BANK CHARGES MUST BE BORNE BY THE PURCHASER");
                paragraph.AddLineBreak();
                paragraph.AddText("ACCOUNT NO. 20500/86");
                paragraph.AddLineBreak();
                paragraph.AddText("IBAN : IL55 0106 8500 0000 2050 086");
                paragraph.AddLineBreak();
                paragraph.AddText("BANK : LEUMI LE-ISRAEL B.M.");
                paragraph.AddLineBreak();
                paragraph.AddText("SWIFT CODE : L U M I I L I T X X X");
                paragraph.AddLineBreak();
                paragraph.AddText("BRANCH : DOMESTIC PRIVATE BANKING HAIFA 685");
                paragraph.AddLineBreak();
                paragraph.AddText("107 HANASSI AVE. HAIFA 34632");
                paragraph.AddLineBreak();
                paragraph.AddText("Tax id number 500500202");


            }
            else
            {


                paragraph.AddText("Beneficiary's Name: Yad Vashem");
                paragraph.AddLineBreak();
                paragraph.AddText("Leumi Bank 10");
                paragraph.AddLineBreak();
                paragraph.AddText("Branch 685");
                paragraph.AddLineBreak();
                paragraph.AddText("Account Number 20500/86");
                paragraph.AddLineBreak();
                paragraph.AddLineBreak();
                paragraph.AddText("Tax id number 500500202");
            }


            table = section.AddTable();
            table.AddColumn(300);
            table.AddColumn(300);
            row = table.AddRow();

            paragraph = row[1].AddParagraph();
         
            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.LineSpacing = 15;
            paragraph.AddLineBreak();
            paragraph.AddText("Sincerely,");
            if (!IsHanzacha(data))
            {
                paragraph.AddLineBreak();
                paragraph.AddText("The International School of Holocaust Studies");
            }
            paragraph.AddLineBreak();
            paragraph.AddText("Yad Vashem");
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();
            //if (IsHanzacha(data))
            //{
            //    paragraph.AddText("Signed: "+ helperRepository.Reverse("ltr", data.createdBy)); 
            //}
            //else
            //{
            //    paragraph.AddText("Signed______________________________");
            //}



        }
        private void CreatePageAndFillContentheb(Document document, InvoiceData data)
        {
            document.DefaultPageSetup.LeftMargin = Unit.FromCentimeter(2);
            document.DefaultPageSetup.RightMargin = Unit.FromCentimeter(2);
            document.DefaultPageSetup.TopMargin = Unit.FromCentimeter(4.5);
            document.DefaultPageSetup.BottomMargin = Unit.FromCentimeter(4.5);
            document.DefaultPageSetup.Orientation = Orientation.Portrait;



            //header
            Section section = document.AddSection();
           // section.PageSetup.HeaderDistance = 50;
           // section.PageSetup.FooterDistance = 50;

            Paragraph paragraph = section.Headers.Primary.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            Image image;
            if(IsHanzacha(data))
            {
                image = paragraph.AddImage("Images/commemoration_header_heb.jpg");
                image.Height = 80;
                image.Width = 120;

            }
            else
            {
                image = paragraph.AddImage("Images/logo_school_heb.jpg");

            }



            #region footer
            Table table = section.Footers.Primary.AddTable();
            table.AddColumn(250);
            table.AddColumn(230);
            Row row=table.AddRow();

            //row.Shading.Color = shadingColor;
            if (IsHanzacha(data))
            {

                row[0].AddImage(@"Images\logo_heb.jpg");

            }
            else
            {
                row[0].AddImage(@"Images\icons_heb.jpg");

            }
            paragraph = row[1].AddParagraph();
                row.Cells[1].VerticalAlignment = VerticalAlignment.Bottom;
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", data.createdBy) + helperRepository.Reverse("rtl", "המסמך נוצר ע”י :"));
            #endregion



            paragraph = section.AddParagraph();
      
            paragraph.AddFormattedText(DateTime.Now.ToString("dd/MM/yyyy"));

        
          //to:
            paragraph = section.AddParagraph();
             paragraph.Format.SpaceBefore = 15;
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.AddText(helperRepository.Reverse("rtl", " לכבוד:"));
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", data.name));
            paragraph.AddLineBreak();
            if (!string.IsNullOrEmpty(data.address))
            {
                paragraph.AddText(helperRepository.Reverse("rtl", data.address));
                paragraph.AddLineBreak();
            }

            if (!string.IsNullOrEmpty(data.telephone))
            {
                paragraph.AddText(helperRepository.Reverse("rtl", data.telephone));
                paragraph.AddLineBreak();
            }
            paragraph.AddText(data.semel + helperRepository.Reverse("rtl", "ח.פ./סמל מוסד: "));
            paragraph.AddLineBreak();

            if (!string.IsNullOrEmpty(data.relatedAccountName))
            {
                paragraph.AddText(helperRepository.Reverse("rtl", data.relatedAccountName) + helperRepository.Reverse("rtl", "עבור: "));
                paragraph.AddLineBreak();
            }
            if (!string.IsNullOrEmpty(data.relatedAccountAddress))
            {
                paragraph.AddText(helperRepository.Reverse("rtl", data.relatedAccountAddress));
                paragraph.AddLineBreak();
            }
            if (data.tables.Count == 1)
            {
                if (!string.IsNullOrEmpty(data.tables[0].contact))
                {
                    paragraph.AddText(helperRepository.Reverse("rtl", data.tables[0].contact) + helperRepository.Reverse("rtl", "לידי: "));
                }
                if (!string.IsNullOrEmpty(data.tables[0].groupName) || !string.IsNullOrEmpty(data.tables[0].reservationNum))
                {
                    paragraph.AddLineBreak();
                    paragraph.AddText("_______________________");
                    paragraph.AddLineBreak();
                    if (!string.IsNullOrEmpty(data.tables[0].groupName) && data.tables[0].groupName != "no-name")
                    {
                        paragraph.AddText(helperRepository.Reverse("rtl", data.tables[0].groupName) + helperRepository.Reverse("rtl", "שם הקבוצה: "));
                        paragraph.AddLineBreak();
                    }
                    if (!string.IsNullOrEmpty(data.tables[0].groupName) || !string.IsNullOrEmpty(data.tables[0].reservationNum))
                    {
                        paragraph.AddText(helperRepository.Reverse("rtl", data.tables[0].reservationNum) + helperRepository.Reverse("rtl", "מספר הזמנה: "));
                    }
                }
            }

         


            /********************************/
            paragraph = section.AddParagraph();
            paragraph.Format.SpaceBefore = 20;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            if(!string.IsNullOrEmpty(data.status))
            {
                string status_heb;
                switch (data.status.ToLower())
                {
                    case "original":
                        status_heb = "מקור";
                        break;
                    case "copy":
                        status_heb = "העתק";
                        break;
                    default:
                        status_heb = "";
                        break;
                }
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", status_heb), font13b);
                paragraph.AddLineBreak();
            }
             if (!data.isCancel)
            {
                paragraph.AddFormattedText(" INVOICE NO. " + data.invoiceNum + helperRepository.Reverse("rtl", " חשבון מספר "), font13b);
            }

            else // cancel
            {

                paragraph.AddFormattedText("CREDIT", red);
                paragraph.AddFormattedText(" INVOICE NO. " + data.invoiceNum, font13b);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", " מספר "), font13b);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", " זיכוי "), red);
                paragraph.AddFormattedText(helperRepository.Reverse("rtl", " חשבון "), font13b);

            }
          
             if(data.tables.Count>1)//union
            {
                paragraph.AddLineBreak();
                //paragraph = section.AddParagraph();
                paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(data.InvoiceTotalPriceAfterDiscount) + helperRepository.Reverse("rtl", " סה\"כ לתשלום "), font13b);

                paragraph.AddLineBreak();
                paragraph = section.AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText( helperRepository.Reverse("rtl", " פירוט תשלומים: "));

            }
            paragraph.Format.SpaceAfter = 10;


            foreach (var tableValue in data.tables)
            {
                if (data.tables.Count > 1)//union
                {
                    paragraph.AddLineBreak();
                    paragraph = section.AddParagraph();
                    paragraph.Format.Alignment = ParagraphAlignment.Right;
                    string rowHeader = " מספר הזמנה: " + tableValue.reservationNum + " שם הקבוצה: " + tableValue.groupName + " איש קשר: " + tableValue.contact;
                    if(IsHanzacha(data))//without contact
                    {
                        rowHeader = " מספר הזמנה: " + tableValue.reservationNum + " שם הקבוצה: " + tableValue.groupName;
                    }
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", rowHeader));
                    paragraph.Format.SpaceAfter = 10;
                }
                table = section.AddTable();
            table.Borders.Width = 0.25;
            table.Borders.Left.Width = 0.5;
            table.Borders.Right.Width = 0.5;
            //if (!data.isCancel)
            
                // Before you can add a row, you must define the columns
                Column column = table.AddColumn("2.6cm");
                column = table.AddColumn("2.2cm");
                column = table.AddColumn("2.8cm");
                column = table.AddColumn("1.3cm");
                column = table.AddColumn("2cm");
                column = table.AddColumn("3.8cm");
                column = table.AddColumn("2.3cm");
              
                foreach (Column col in table.Columns)
                {
                    col.Format.Alignment = ParagraphAlignment.Center;
                }
           


                row = table.AddRow();
                row.HeadingFormat = true;
                row.Height = 27;
                row.Format.Alignment = ParagraphAlignment.Center;
                row.Format.Font.Bold = true;
                row.Shading.Color = shadingColor;
                row.Cells[6].AddParagraph(helperRepository.Reverse("rtl", "תאריך פעילות"));
                row.Cells[5].AddParagraph(helperRepository.Reverse("rtl", "פרטים"));
                row.Cells[4].AddParagraph(helperRepository.Reverse("rtl", "מחיר"));
                row.Cells[3].AddParagraph(helperRepository.Reverse("rtl", "כמות"));
                row.Cells[2].AddParagraph(helperRepository.Reverse("rtl", "סה\"כ לפני הנחה"));
                row.Cells[1].AddParagraph(helperRepository.Reverse("rtl", "הנחה"));
                row.Cells[0].AddParagraph(helperRepository.Reverse("rtl", "סה\"כ"));

                //fill content

                int lenght = tableValue.tableRowDetails.Count;
                for (int i = 0; i < lenght; i++)
                {

                    Row row1 = table.AddRow();
                    row1.Borders.Bottom.Width = 0.2;
                    row1.Borders.Bottom.Color = Colors.Gray;
                    row1.Cells[6].AddParagraph(tableValue.tableRowDetails[i].dateForDisplay);
                    paragraph = row1.Cells[5].AddParagraph();
                    helperRepository.AddToTextSplitToLines("rtl", paragraph, tableValue.tableRowDetails[i].perut, 15);
                    row1.Cells[4].AddParagraph(helperRepository.WithCurrencySymbol(tableValue.tableRowDetails[i].price));
                    row1.Cells[3].AddParagraph(tableValue.tableRowDetails[i].amount);
                    row1.Cells[2].AddParagraph(helperRepository.WithCurrencySymbol(tableValue.tableRowDetails[i].total));
                    row1.Cells[1].AddParagraph(helperRepository.DiscountWithSymbol(tableValue.tableRowDetails[i].discount, tableValue.tableRowDetails[i].discountType));
                    row1.Cells[0].AddParagraph(helperRepository.WithCurrencySymbol(tableValue.tableRowDetails[i].totalAfterDiscount));
                    row1.Height = 27;

                    if (i + 1 != lenght)//leave last border bottom
                    {
                        row1.Borders.Bottom.Visible = false;
                    }


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



                // Add the notes paragraph
                paragraph = section.AddParagraph();
                string totalToPay = tableValue.totalPriceAfterDiscount != string.Empty ? tableValue.totalPriceAfterDiscount : tableValue.totalPrice;
                if (tableValue.totalPriceAfterDiscount != "" && tableValue.discount != "" && tableValue.discount != "0")// there is a discount on the whole order
                {

                    var tableTotal = section.AddTable();
                    // table.Rows.LeftIndent = 33;
                    tableTotal.LeftPadding = 0;
                    tableTotal.RightPadding = 0;
                    tableTotal.TopPadding = 0;
                    tableTotal.BottomPadding = 0;

                    tableTotal.AddColumn(50);
                    tableTotal.AddColumn(30);
                    tableTotal.AddColumn(150);
                    row = tableTotal.AddRow();
                    row.BottomPadding = 5;
                    row = tableTotal.AddRow();
                    row.BottomPadding = 5;
                    row = tableTotal.AddRow();



                    //first row
                    paragraph = tableTotal.Rows[0][0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(tableValue.totalPrice));
                    //  paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph = tableTotal.Rows[0][2].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ עלות:"));

                    //second row
                    paragraph = tableTotal.Rows[1][0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.DiscountWithSymbol(tableValue.discount, tableValue.discountType));
                    // paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph = tableTotal.Rows[1][2].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "הנחה:"));

                    //third row
                    paragraph = tableTotal.Rows[2][0].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(tableValue.totalPriceAfterDiscount), TextFormat.Bold);
                    // paragraph.Format.Alignment = ParagraphAlignment.Right;
                    paragraph = tableTotal.Rows[2][1].AddParagraph();
                    paragraph.AddFormattedText(" ");//for space
                    paragraph = tableTotal.Rows[2][2].AddParagraph();
                    paragraph.AddFormattedText(helperRepository.Reverse("rtl", "סה\"כ לאחר הנחה:"), TextFormat.Bold);

                }
                else
                {
                    paragraph.Format.SpaceBefore = 10;
                    if (!data.isCancel)
                    {
                        paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(totalToPay) + helperRepository.Reverse("rtl", "סה\"כ לתשלום: "), TextFormat.Bold);
                    }
                    else //cancel
                    {
                        paragraph.AddFormattedText(helperRepository.WithCurrencySymbol(totalToPay) + helperRepository.Reverse("rtl", "סה\"כ: "), TextFormat.Bold);

                    }
                }

            }
            if (!string.IsNullOrEmpty(data.comments))
            {
                paragraph = section.AddParagraph();
                paragraph.Format.SpaceBefore = 20;
                paragraph.Format.Alignment = ParagraphAlignment.Right;

                if (data.isCancel)
                {
                    paragraph.AddFormattedText(data.comments+helperRepository.Reverse("rtl", "זיכוי עבור חשבון מספר: ") );
                }
                else
                {
                    helperRepository.AddToTextSplitToLines("rtl", paragraph, "הערות: " + data.comments, 110);

                }
                paragraph.AddLineBreak();
            }


            if (!string.IsNullOrEmpty(data.gefenType))
            {
                paragraph = section.AddParagraph();
                paragraph.Format.SpaceBefore = 20;
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                helperRepository.AddToTextSplitToLines("rtl", paragraph, data.gefenType, 110);

                paragraph.AddLineBreak();
            }

            if (!string.IsNullOrEmpty(data.department))
            {
                paragraph = section.AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.Format.SpaceBefore = 15;
                if (IsHanzacha(data))
                {
                    paragraph.AddText("visit.billing@yadvashem.org.il" + helperRepository.Reverse("rtl", "לבירורים/שאלות ניתן לפנות למייל  "));

                }
                else
                {
                    //  paragraph.AddText(helperRepository.Reverse("rtl", data.department) + helperRepository.Reverse("rtl", "לבירורים/שאלות ניתן לפנות למחלקת "));
                    //change text 6/6/23 devora
                    paragraph.AddFormattedText("school.invoices@yadvashem.org.il" + helperRepository.Reverse("rtl", "  לבירורים או שאלות על החשבון בלבד, נא לפנות במייל ל   "), TextFormat.Bold);

                  
                }
            }

            paragraph = section.AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.SpaceBefore = 15;

            paragraph.AddFormattedText(helperRepository.Reverse("rtl", "לתשלום בהעברה בנקאית:"), fontUnderline);
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "חובה לציין את מס‘ החשבון בעת התשלום"));
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "שם המוטב: יד ושם"));
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "בנק לאומי 10"));
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "סניף 685"));
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "חשבון מס' 20500/86"));
            paragraph.AddLineBreak();
            paragraph.AddLineBreak();
            paragraph.AddText(helperRepository.Reverse("rtl", "מס‘ תיק במס הכנסה 500500202"));




            /*************************/
            table = section.AddTable();
            table.AddColumn(180);
            table.AddColumn(10);
           row= table.AddRow();
            
            paragraph = row[0].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.LineSpacingRule = LineSpacingRule.AtLeast;
            paragraph.Format.LineSpacing = 15;
            paragraph.AddText(helperRepository.Reverse("rtl", "בברכה,"));
            paragraph.AddLineBreak();
          if(IsHanzacha(data))
            {
                paragraph.AddText(helperRepository.Reverse("rtl", "יד ושם, רשות הזיכרון לשואה ולגבורה."));

            }
            else
            {
                paragraph.AddText(helperRepository.Reverse("rtl", "ביה“ס הבין־לאומי להוראת השואה, יד ושם."));

            }
         
            paragraph.AddLineBreak();
            //if (IsHanzacha(data))
            //{
            //    paragraph.AddText(helperRepository.Reverse("rtl", "חתימה: "+ data.createdBy));
            //}
            //else
            //{
            //    paragraph.AddText(helperRepository.Reverse("rtl", "חתימה"));
            //}

        



        }

        private bool IsHanzacha(InvoiceData data)
        {

            if (!string.IsNullOrEmpty(data.division) && data.division == "2")
                return true;
            if (!string.IsNullOrEmpty(data.department) && data.department.Contains("הנצחה"))
                return true;
            return false;
        }





   




    }
}
