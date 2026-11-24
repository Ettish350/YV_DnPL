using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Models
{

    public class ReportData
    {
        public string fileName { get; set; } = "";
        public List<string> reportsPathList { get; set; }

    }
}