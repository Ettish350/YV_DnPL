using Data.Models;
using Data.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Serilog;
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
    public class ReportRepository : IReportRepository
    {
        private IHelperRepository helperRepository;

        public ReportRepository(IHelperRepository helperRepository)
        {
            this.helperRepository = helperRepository;

        }
        IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

        public AttachmentItem CreateCombinedFile(ReportData data)
        {


            Console.WriteLine(data.fileName);
            var attachmentItem = new AttachmentItem();
            attachmentItem.FullPath = MergeMultiplePDFIntoSinglePDF(data.fileName, data.reportsPathList);
            //  attachmentItem.FullPath = MergeMultiplePDFIntoSinglePDF3(helperRepository.GetSaveLocation(data.fileName), data.reportsPathList);

            attachmentItem.Name = Path.GetFileName(attachmentItem.FullPath);
            return attachmentItem;
        }

        private string MergeMultiplePDFIntoSinglePDF(string fileName, List<string> pdfFiles)
        {
            Log.Error($" going to open {pdfFiles.Count} filse");

            PdfDocument document = new PdfDocument();
            foreach (string pdfFile in pdfFiles)
            {
                try
                {
                    Log.Error($"going to open file: {pdfFile} in document:{document.FileSize}");

                    PdfDocument inputPDFDocument = PdfReader.Open(pdfFile, PdfDocumentOpenMode.Import);
                    document.Version = inputPDFDocument.Version;
                    foreach (PdfPage page in inputPDFDocument.Pages)
                    {
                        try
                        {
                            document.AddPage(page);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($" error, exception: {ex.Message}");
                        }
                    }
                    System.IO.File.Delete(pdfFile);
                }
                catch (Exception ex)
                {
                    Log.Error($" error, exception: {ex.Message}");
                }
            }
            string file_path = helperRepository.SavePdfDoc(document, $"{fileName}@{DateTime.Now.ToString("mm-ss")}.pdf");

            // In the final stage, all documents are merged and save in your output file path.  
            return file_path;
        }

    }
}