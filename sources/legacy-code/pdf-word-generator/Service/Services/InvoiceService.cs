using MigraDoc.DocumentObjectModel;
using Service.Services.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Data.Repositories.Interfaces;
using Data.Models;
using System.Reflection;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Service.Services
{
    public class InvoiceService: IInvoiceService
    {
        IInvoiceRepository invoiceRepository;
        IHelperRepository helperRepository;
        public InvoiceService(IInvoiceRepository repository,IHelperRepository helperRepository)
        {
            invoiceRepository = repository;
            this.helperRepository = helperRepository;
        }
        IConfiguration configuration = new ConfigurationBuilder()
       .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
       .Build();
        public AttachmentItem CreateDocument(InvoiceData data)
        {
            Log.Error("start  CreateDocument service");
            Document document = new Document();
            var createdFileName = invoiceRepository.CreateDocument(document,data);
            return helperRepository.AttachDocument(createdFileName);
        }

        public bool isDataValid(InvoiceData data)
        {
            List<bool> results = new List<bool>();
            results.Add(checkData(data));
            foreach (var table in data.tables)
            {


                if (table.tableRowDetails != null)
                {
                    foreach (TableRowDetails item in table.tableRowDetails)
                    {
                        results.Add(checkData(item));
                    }
                }
            }
           
            return !results.Contains(false);
        }
        private bool checkData( object data)
        {
            bool res = true;
            //all values must be full
            foreach (PropertyInfo prop in data.GetType().GetProperties())
            {
                var temp = prop.GetValue(data, null);
                if (temp == null)
                {
                    Log.Error("missing value: " + prop.Name);
                    res = false;
                }
                else
                {
                    Log.Error(" value of: " + prop.Name + " is: " + temp);
                }
            }
            Log.Error(" before return res1: " + res);
            return res;
        }

        public void Test()
        {
            string res = helperRepository.Reverse("he", "this is devoras test");
             res = helperRepository.Reverse("he", "this is a test זה בעברית this is english זה שוב עברית");
            res = helperRepository.Reverse("he", "this is a test זה בעברית this is english זה שוב עברית");

            res = helperRepository.Reverse("he", "ביטול או שינוי - משבועיים ועד שבוע ממועד הביקור, יחויב המזמין ב 50%- מעלות הביקור.");
             res = helperRepository.Reverse("he", "זה טקסט רק בעברית");
             res = helperRepository.Reverse("he", "זה מתחיל בעברית  and ends in english");

            string a = res;

        }
    }
}
