using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Repositories;
using Data.Repositories.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Service.Services;
using Service.Services.Interface;

namespace PdfCreator
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IOnlineReservationService, OnlineReservationService>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IConfirmationService, ConfirmationService>();
            services.AddScoped<ISchoolConfirmationRepository, SchoolConfirmationRepository>();
            services.AddScoped<IOnlineReservationRepository, OnlineReservationRepository>();

            services.AddScoped<IAgencyConfirmationRepository, AgencyConfirmationRepository>();
            services.AddScoped<IHelperRepository,HelperRepository>();
            services.AddScoped<IScheduleRepository, ScheduleRepository>();
            services.AddScoped<IScheduleService, ScheduleService>();
            services.AddScoped<ICommemorationRepository, CommemorationRepository>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IScheduleResourceRepository, ScheduleResourceRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {


            app.UseCors(builder => builder
                   .WithOrigins("http://localhost:4200")
                   .AllowAnyMethod()
                   .AllowCredentials()
                   .WithHeaders("Accept", "Content-Type", "Origin", "X-My-Header"));
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
