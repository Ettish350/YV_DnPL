using Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Interface
{
    public interface IInvoiceService
    {
        AttachmentItem CreateDocument(InvoiceData data);
        bool isDataValid(InvoiceData data);
        void Test();
    }
}
