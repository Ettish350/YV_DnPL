using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace PdfCreator
{
    public class Program
    {
        public static void Main(string[] args)
        {

            CreateWebHostBuilder(args).UseUrls("http://localhost:5008").Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>().UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration));
    }
}
