using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;
using WmsSelfReferral.Data;
using WmsSelfReferral.Helpers;
namespace WmsSelfReferral
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
                 options.UseSqlServer(Configuration["WmsSelfReferral:sessionCache"],
                 options => options.EnableRetryOnFailure()));

            services.AddDataProtection()
                .SetApplicationName("WmsPublicReferral")
                .PersistKeysToDbContext<DatabaseContext>();

            services.AddWmsReferralService();
            services.AddPostcodesioService(Configuration);
            services.AddGetAddressioService(Configuration);
            services.AddNhsLoginService();
            services.AddOpenIdRefreshTokenService();
            services.AddTransient<IReferralSessionData, ReferralSessionData>();
            services.AddDistributedSqlServerCache(options =>
            {
                options.ConnectionString =
                    Configuration["WmsSelfReferral:sessionCache"];
                options.SchemaName = "dbo";
                options.TableName = "GeneralReferralsSessionCache";
            });

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(600);
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".WmsSelfReferral.Session";
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            services.AddAntiforgery(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".WmsSelfReferral.Antiforgery";
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.Secure = CookieSecurePolicy.Always;
            });

            services.AddTransient<CookieAuthenticationEventsHelper>();
            services.AddTransient<OpenIdEventsHelper>();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

            })
                .AddCookie(options =>
                {                    
                    options.EventsType = typeof(CookieAuthenticationEventsHelper);
                    options.Cookie.Name = ".WmsSelfReferral.Login";
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(600);
                    options.SlidingExpiration = true;                    
                })
                .AddOpenIdConnect(options =>
                {
                    options.ClientId = Configuration["WmsSelfReferral:NhsLoginClientId"];
                    options.Authority = Configuration["WmsSelfReferral:NhsLoginUrl"];
                    options.ResponseType = "code";
                    options.ResponseMode = "form_post";
                    options.Scope.Add("email");
                    options.Scope.Add("phone");
                    options.Scope.Add("profile_extended");
                    options.Scope.Add("gp_registration_details");
                    options.SaveTokens = true;
                    options.UseTokenLifetime = false;
                    options.CallbackPath = "/signin-oidc";
                    options.AccessDeniedPath = "/access-denied";
                    options.EventsType = typeof(OpenIdEventsHelper);
                    options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                    options.CorrelationCookie.Name = ".WmsSelfReferral.Correlation";
                    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(30);

                });

            services.AddSingleton<IWmsCalculations, WmsCalculations>();
            services.AddControllersWithViews();
            services.AddResponseCaching();

            //for FrontDoor
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();

                // Put your front door FQDN here and any other hosts that will send headers you want respected
                options.AllowedHosts = new List<string>() 
                { 
                    "app-nhseiwmsselfreferral-uks-pre-1.azurewebsites.net",
                    "app-nhseiwmsselfreferral-uks-prd-1.azurewebsites.net",
                    "app-nhseiwmsselfreferral-ukw-prd-1.azurewebsites.net",
                    "public.wms-pre.mlcsu.org",
                    "public.wmp.nhs.uk",
                };
            });

            services.AddApplicationInsightsTelemetry();
            services.AddApplicationInsightsTelemetryProcessor<FilterTelemetryProcessor>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHealthChecks();

            //Add Hsts options
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(180);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                
            }
            app.UseStatusCodePagesWithReExecute("/ErrorHandler/{0}");
            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseResponseCaching();
            app.Use(async (context, next) =>
            {
                //add CSP
                var nonce = Guid.NewGuid().ToString("N");
                context.Items["csp-nonce"] = nonce;
                if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                    context.Response.Headers.Add("Content-Security-Policy", $"default-src 'none';connect-src 'self' https://*.google-analytics.com https://*.analytics.google.com https://*.googletagmanager.com uksouth-1.in.applicationinsights.azure.com; img-src 'self' data: assets.nhs.uk https://*.google-analytics.com https://*.googletagmanager.com cdn.datatables.net; script-src 'self' assets.nhs.uk cdn.datatables.net js.monitor.azure.com https://*.googletagmanager.com 'nonce-{nonce}' ; style-src 'self' assets.nhs.uk cdn.datatables.net; font-src 'self' assets.nhs.uk; frame-ancestors 'none'; form-action 'self';");
                
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

            var cultures = new List<CultureInfo> {
                new CultureInfo("en-GB")                
            };
            app.UseRequestLocalization(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-GB");
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
            });

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
