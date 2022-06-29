using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WmsMskReferral.Data;
using WmsMskReferral.Helpers;
using WmsReferral.Business.Services;
using WmsReferral.Business.Shared;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;
string oidauthority = "https://fs.nhs.net/adfs/.well-known/openid-configuration";


// Add services to the container.

builder.Services.AddDbContext<DatabaseContext>(options =>
                 options.UseSqlServer(configuration["WmsMskReferral:sessionCache"],
                 options => options.EnableRetryOnFailure()));

builder.Services.AddDataProtection()
    .SetApplicationName("WmsMskReferral")
    .PersistKeysToDbContext<DatabaseContext>();

builder.Services.AddWmsReferralService();
builder.Services.AddPostcodesioService(configuration);
builder.Services.AddGetAddressioService(configuration);
builder.Services.AddODSLookupService();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.Configure<AuthMessageSenderOptions>(configuration.GetSection("SendGridConfig"));
builder.Services.AddOpenIdRefreshTokenService();
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString =
        configuration["WmsMskReferral:sessionCache"];
    options.SchemaName = "dbo";
    options.TableName = "MskReferralsSessionCache";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(600);
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".WmsMskReferral.Session";
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddTransient<CookieAuthenticationEventsHelper>();
builder.Services.AddTransient<OpenIdEventsHelper>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

})
    .AddCookie(options =>
    {
        options.EventsType = typeof(CookieAuthenticationEventsHelper);
        options.Cookie.Name = ".WmsMskReferral.Login";
        options.ExpireTimeSpan = TimeSpan.FromSeconds(600);
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(options =>
    {
        options.ClientId = configuration["WmsMskReferral:NhsMailClientId"];
        options.ClientSecret = configuration["WmsMskReferral:NhsMailSecret"];        
        options.MetadataAddress = oidauthority;
        options.ResponseType = "code";
        options.ResponseMode = "form_post";
        options.Scope.Add("Display Name");
        options.Scope.Add("UPN");
        options.Scope.Add("Primary Mail Address");
        options.Scope.Add("ODS Code");
        options.SaveTokens = true;
        options.UseTokenLifetime = false;
        options.CallbackPath = "/signin-oidc";
        options.AccessDeniedPath = "/access-denied";        
        options.EventsType = typeof(OpenIdEventsHelper);
        

    });

builder.Services.AddSingleton<IWmsCalculations, WmsCalculations>();
builder.Services.AddControllersWithViews();

builder.Services.AddApplicationInsightsTelemetry(configuration["APPINSIGHTS_CONNECTIONSTRING"]);
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHealthChecks();


//builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

app.Run();
