using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
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
        options.LoginPath = "/OTPAuth/login";
        options.LogoutPath = "/";
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

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".WmsMskReferral.Antiforgery";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddSingleton<IWmsCalculations, WmsCalculations>();
builder.Services.AddControllersWithViews();
builder.Services.AddResponseCaching();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddApplicationInsightsTelemetryProcessor<FilterTelemetryProcessor>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHealthChecks();

//Add Hsts options
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(180);
});

//builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
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
app.UseRouting();

app.UseAuthorization();

app.Use(async (context, next) => {
    //add CSP
    var nonce = Guid.NewGuid().ToString("N");
    context.Items["csp-nonce"] = nonce;    
    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
        context.Response.Headers.Add("Content-Security-Policy", $"default-src 'none';connect-src 'self' uksouth-1.in.applicationinsights.azure.com; img-src 'self' data: assets.nhs.uk cdn.datatables.net; script-src 'self' assets.nhs.uk cdn.datatables.net js.monitor.azure.com 'nonce-{nonce}' ; style-src 'self' assets.nhs.uk cdn.datatables.net; font-src 'self' assets.nhs.uk; frame-ancestors 'none'; form-action 'self' fs.nhs.net;");
    
    context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        MustRevalidate = true,
                        NoStore = true,
                        NoCache = true,

                    };
    if (context.Request.Path.StartsWithSegments("/robots.txt"))
    {
        var robotsTxtPath = Path.Combine(app.Environment.ContentRootPath, $"robots.{app.Environment.EnvironmentName}.txt");
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
