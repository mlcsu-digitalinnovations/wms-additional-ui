using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                    
                });

            services.AddSingleton<IWmsCalculations, WmsCalculations>();
            services.AddControllersWithViews();

            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 404)
                {
                    context.Request.Path = "/";
                    await next();
                }
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
