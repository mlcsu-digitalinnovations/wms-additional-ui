using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WmsPharmacyReferral.Helpers;
using WmsPharmacyReferral.Data;
using WmsReferral.Business;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;

namespace WmsPharmacyReferral
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
            services.AddDbContext<DatabaseContext>(options =>
                 options.UseSqlServer(Configuration["WmsPharmacyReferral:sessionCache"],
                 options => options.EnableRetryOnFailure()));

            services.AddDataProtection()
                .SetApplicationName("WmsPharmacyReferral")
                .PersistKeysToDbContext<DatabaseContext>();
            services.AddWmsReferralService();
            services.AddPostcodesioService(Configuration);
            services.AddGetAddressioService(Configuration);
            services.AddODSLookupService();
            services.AddTransient<IEmailSender, EmailSender>();
            services.Configure<AuthMessageSenderOptions>(Configuration.GetSection("SendGridConfig"));
            services.AddDistributedSqlServerCache(options =>
            {
                options.ConnectionString =
                    Configuration["WmsPharmacyReferral:sessionCache"];
                options.SchemaName = "dbo";
                options.TableName = "PharmacyReferralsSessionCache";
            });
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(600);
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".WmsPharmacyReferral.Session";
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            services.AddSingleton<IWmsCalculations, WmsCalculations>();
            services.AddTransient<IPharmacyData, PharmacyData>();
            services.AddControllersWithViews();

            services.AddResponseCaching();

            services.AddApplicationInsightsTelemetry();
            services.AddApplicationInsightsTelemetryProcessor<FilterTelemetryProcessor>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //services.AddSingleton<ITelemetryInitializer, TelemetryEnrichment>();

            //Add Hsts options
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(180);
            });

            services.AddAntiforgery(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".WmsPharmacyReferral.Antiforgery";
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePagesWithReExecute("/ErrorHandler/{0}");
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithReExecute("/ErrorHandler/{0}");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseResponseCaching();
            app.Use(async (context, next) =>
            {
                //add CSP
                var nonce = Guid.NewGuid().ToString("N");
                context.Items["csp-nonce"] = nonce;
                if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                    context.Response.Headers.Add("Content-Security-Policy", $"default-src 'none';connect-src 'self' uksouth-1.in.applicationinsights.azure.com; img-src 'self' data: assets.nhs.uk cdn.datatables.net; script-src 'self' assets.nhs.uk cdn.datatables.net js.monitor.azure.com 'nonce-{nonce}' ; style-src 'self' assets.nhs.uk cdn.datatables.net; font-src 'self' assets.nhs.uk;frame-ancestors 'none'; form-action 'self';");

                context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        MustRevalidate = true,
                        NoStore = true,
                        NoCache = true,
                        
                    };

                if (context.Request.Path.StartsWithSegments("/robots.txt"))
                {
                    var robotsTxtPath = Path.Combine(env.ContentRootPath, $"robots.{env.EnvironmentName}.txt");
                    string output = "User-agent: *  \nDisallow: /";
                    if (System.IO.File.Exists(robotsTxtPath))
                    {
                        output = await System.IO.File.ReadAllTextAsync(robotsTxtPath);
                    }
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(output);
                }
                else await next();
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
