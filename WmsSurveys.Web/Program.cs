using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WmsReferral.Business.Services;
using WmsSurveys.Web.Data;
using WmsSurveys.Web.Helpers;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

//Add DB context
builder.Services.AddDbContext<DatabaseContext>(options =>
                 options.UseSqlServer(configuration["WmsQuestionnaires:sessionCache"],
                 options => options.EnableRetryOnFailure()));

//Add DataProtection Keys to DB
builder.Services.AddDataProtection()
    .SetApplicationName("WmsQuestionnaires")
    .PersistKeysToDbContext<DatabaseContext>();

//Add WmsReferral service (business)
builder.Services.AddWmsReferralService();

//Add SQL Cache
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString =
        configuration["WmsQuestionnaires:sessionCache"];
    options.SchemaName = "dbo";
    options.TableName = "QuestionnairesSessionCache";
});

builder.Services.AddSession(options =>
{    
    options.IdleTimeout = TimeSpan.FromSeconds(600);
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".WmsQuestionnaire.Session";
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".WmsQuestionnaire.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IQuestionnaireData, QuestionnaireData>();

//Add services to the container.
builder.Services.AddControllersWithViews();
//.AddCookieTempDataProvider();
//builder.Services.AddResponseCaching();
builder.Services.AddMvc().AddRazorRuntimeCompilation();

//Add application insigts
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddApplicationInsightsTelemetryProcessor<FilterTelemetryProcessor>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

//Add health checks endpoint
builder.Services.AddHealthChecks();

//Add Hsts options
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(180);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/ErrorHandler/{0}");
//app.UseResponseCaching();

app.Use(async (context, next) => {
    //add CSP
    var nonce = Guid.NewGuid().ToString("N");
    context.Items["csp-nonce"] = nonce;
    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
        context.Response.Headers.Add("Content-Security-Policy", $"default-src 'none';connect-src 'self' uksouth-1.in.applicationinsights.azure.com; img-src 'self' data: assets.nhs.uk cdn.datatables.net; script-src 'self' assets.nhs.uk cdn.datatables.net js.monitor.azure.com 'nonce-{nonce}' ; style-src 'self' assets.nhs.uk cdn.datatables.net; font-src 'self' assets.nhs.uk; frame-ancestors 'none'; form-action 'self';");

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Questionnaire}/{action=Index}/{id?}");

app.MapHealthChecks("/health");

app.Run();
