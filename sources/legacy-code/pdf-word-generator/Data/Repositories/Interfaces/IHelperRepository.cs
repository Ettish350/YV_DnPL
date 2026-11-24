using Data.Models;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Text;
using Xceed.Words.NET;

namespace Data.Repositories.Interfaces
{
    public interface IHelperRepository
    {
        string SaveDoc(Document document, string number, string prefix, string folder = "Invoices");
        string SaveQRcodeImage(System.Drawing.Bitmap image, string fileName, string folder = "QRcode");
        string SavePdfDoc(PdfDocument document, string fileName, string folder = "reports");
        string SaveWordDoc(Xceed.Document.NET.Document document, string fileName);
        string GetSaveLocation(string fileName);

        AttachmentItem AttachDocument(string createdFileName, string extention = ".pdf", string folder = "Invoices");
        string DiscountWithSymbol(string discount, string discountType,string currencyCode="");
        string Reverse(string docLang, string str);
       string ReverseForWord( string str);

        string WithCurrencySymbol(string number,string currencyCode="");
        string WithCurrencyCodeIfNeed(string currencyCode);
        void AddToTextSplitToLines(string dir, Paragraph paragraph, string stringToSplit, int maximumLineLength, Font font = null);

         void AddYVOrderNumTextFrame(HeaderFooter header, string orderNum, string direction);
         Table AddActivityDayTable(Section section);
         void AddYVRows(Table table, int rowsCount, ParagraphAlignment paragraphAlignment);
        Table AddYVTable(Section section, ParagraphAlignment paragraphAlignment);
    }
       
}

