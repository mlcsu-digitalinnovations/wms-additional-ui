using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.ExternalConnectors;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using Microsoft.Identity.Web.TokenCacheProviders.Session;
using Microsoft.Identity.Web.UI;
using System.Globalization;
using System.Security.Claims;
using WmsElectiveCarePortal.Data;
using WmsElectiveCarePortal.Helpers;
using WmsElectiveCarePortal.Services;
using WmsReferral.Business.Services;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddDbContext<DatabaseContext>(options =>
                 options.UseSqlServer(builder.Configuration["WmsElectiveCare:sessionCache"],
                 options => options.EnableRetryOnFailure()));

builder.Services.AddDataProtection()
    .SetApplicationName("WmsElectiveCare")
    .PersistKeysToDbContext<DatabaseContext>();


builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddTransient<OpenIdEventsHelper>();

// Add services to the container.

//add MLCSU admin auth
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(options =>
        {

            builder.Configuration.Bind("AzureAdMLCSU", options);
            options.Events ??= new OpenIdConnectEvents();
            options.Events.OnRedirectToIdentityProvider += OnRedirectToIdentityProviderFunc;
            options.Events.OnRemoteSignOut += OnRemoteSignOutFunc;
            options.Events.OnSignedOutCallbackRedirect += OnSignedOutFunc;
            options.Events.OnRedirectToIdentityProviderForSignOut += OnRedirectToIdentityProviderForSignOutFunc;
            options.SaveTokens = true;
            options.ResponseMode = "form_post";
            options.UseTokenLifetime = false;
            

        }, options =>
        {
            options.Cookie.Name = ".WmsElectiveCare.Login";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            options.SlidingExpiration = true;
            options.Events ??= new CookieAuthenticationEvents();
            options.Events.OnSigningOut += OnCookieSigningOutFunc;

        }, OpenIdConnectDefaults.AuthenticationScheme, "Cookies");
        


//Add b2c auth
builder.Services.AddAuthentication("AB2C")
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);        
        options.EventsType = typeof(OpenIdEventsHelper);

    },options =>
    {
        options.Cookie.Name = ".WmsElectiveCare.B2cLogin";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = true;
        
    }, cookieScheme: "cookiesb2c", openIdConnectScheme: "AB2C");

builder.Services.AddAntiforgery(options => options.Cookie.Name = ".WmsElectiveCare.Antiforgery");

builder.Services.AddTransient<IUserRepository, UserManager>();
builder.Services.AddWmsReferralService();
builder.Services.AddODSLookupService();

//restrict /Manage to MLCSU security group
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ElectiveCareAdmin", policy => {
        policy.AddAuthenticationSchemes(new[] { "OpenIdConnect" });
        policy.RequireClaim("groups", builder.Configuration["WmsElectiveCare:adminGroup"]);
    });    
});

builder.Services.Configure<MvcViewOptions>(options =>
{
    options.HtmlHelperOptions.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.None;
   
});

builder.Services.AddControllersWithViews(options =>
{    
}).AddNewtonsoftJson();

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();



//Add application insigts
builder.Services.AddApplicationInsightsTelemetry();
//builder.Services.AddSingleton<ITelemetryInitializer, FilterTelemetryInitializer>();
builder.Services.AddApplicationInsightsTelemetryProcessor<FilterTelemetryProcessor>();

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
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/ErrorHandler/{0}");

app.UseRewriter(
    new RewriteOptions().Add(
        context => {
            if (context.HttpContext.Request.Path == "/MicrosoftIdentity/Account/SignedOut")
            { context.HttpContext.Response.Redirect("/"); }
        })


);

app.Use(async (context, next) => {
    //add CSP
    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
        context.Response.Headers.Add("Content-Security-Policy", "default-src 'none';connect-src 'self'; img-src 'self' data: assets.nhs.uk cdn.datatables.net; script-src 'self' assets.nhs.uk cdn.datatables.net; style-src 'self' assets.nhs.uk cdn.datatables.net; font-src 'self' assets.nhs.uk;");

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
//app.UseSession(); // Before UseMvc()
app.UseAuthentication();
app.UseAuthorization();

//set localization
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
app.MapRazorPages();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

async Task OnRedirectToIdentityProviderFunc(RedirectContext context)
{
    // Custom code here
    var id_token_hint = context.Properties.Items.FirstOrDefault(x => x.Key == "id_token_hint").Value;
    if (id_token_hint != null)
    {
        // Send parameter to authentication request
        //context.ProtocolMessage.SetParameter("id_token_hint", id_token_hint);
    }
    // Don't remove this line
    await Task.CompletedTask.ConfigureAwait(false);
}
async Task OnRedirectToIdentityProviderForSignOutFunc(RedirectContext context)
{
    
    // Don't remove this line
    await Task.CompletedTask.ConfigureAwait(false);
}
async Task OnRemoteSignOutFunc(RemoteSignOutContext context)
{
    // Custom code here

    // Don't remove this line
    await Task.CompletedTask.ConfigureAwait(false);
}
async Task OnSignedOutFunc(RemoteSignOutContext context)
{
    // Custom code here

    // Don't remove this line
    await Task.CompletedTask.ConfigureAwait(false);
}
async Task OnCookieSigningOutFunc(CookieSigningOutContext context)
{
    // Custom code here

    // Don't remove this line
    await Task.CompletedTask.ConfigureAwait(false);
}

