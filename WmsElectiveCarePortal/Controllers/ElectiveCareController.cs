using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using WmsElectiveCarePortal.Helpers;
using WmsElectiveCarePortal.Models;
using WmsReferral.Business.Helpers;
using WmsReferral.Business.Services;
using WmsReferral.Business.Models;
using Newtonsoft.Json.Linq;
using SendGrid.Helpers.Mail;
using Microsoft.Graph.ExternalConnectors;

namespace WmsElectiveCarePortal.Controllers
{
    [Authorize(AuthenticationSchemes = "AB2C")]
    public class ElectiveCareController : Controller
    {
        private readonly ILogger<ElectiveCareController> _logger;
        private TelemetryClient _telemetry;
        private readonly IWmsReferralService _WmsReferralService;
        private readonly string[] _permittedExtensions = { ".xls", ".xlsx", ".csv" };
        private readonly long _fileSizeLimit = 3145728;

        public ElectiveCareController(ILogger<ElectiveCareController> logger, TelemetryClient telemetry, IWmsReferralService wmsReferralService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _WmsReferralService = wmsReferralService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await ClaimProfile());
        }

        public IActionResult Upload()
        {
            return View();
        }
        public IActionResult Complete()
        {
            return View();
        }
        [Route("{controller}/my-account")]
        public IActionResult MyAccount()
        {
            return View();
        }
        [Route("{controller}/help")]
        public IActionResult Help()
        {
            return View();
        }


        [Route("{controller}/edit-profile")]
        public async Task<IActionResult> EditProfile()
        {
            var redirectUrl = Url.Content("~/ElectiveCare");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items["policy"] = "B2C_1_wmp_electivecare_profile";

            await HttpContext.ChallengeAsync("AB2C", properties);
            return new EmptyResult();
        }
        [Route("{controller}/change-password")]
        public async Task<IActionResult> ChangePassword()
        {
            var redirectUrl = Url.Content("~/ElectiveCare");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items["policy"] = "B2C_1_wmp_electivecare_passwordreset";            

            await HttpContext.ChallengeAsync("AB2C", properties);
            return new EmptyResult();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(UploadModel model)
        {
            ModelState.Remove("Result");
            ModelState.Remove("RowErrors");
            try
            {
                //check file is really an excel file (not 100% but its good enough)
                var streamedFileContent = Array.Empty<byte>();
                streamedFileContent =
                   await FileHelper.ProcessFormFile<ElectiveCareController>(
                   formFile: model.FormFile,
                   modelState: ModelState,
                   permittedExtensions: _permittedExtensions,
                   sizeLimit: _fileSizeLimit);
                
                //return errors
                if (!ModelState.IsValid)
                {
                    _logger.LogInformation("Upload failed, not excel file.");
                    model.Result = false;
                    return View(model);
                }

                //submit to api
                var claims = await ClaimProfile();
                using (var memoryStream = new MemoryStream())
                {
                    //copy file to stream
                    await model.FormFile.CopyToAsync(memoryStream);

                    //submit to api
                    var result = await _WmsReferralService.UploadElectiveCareFileAsync(
                        memoryStream, 
                        Path.GetExtension(model.FormFile.FileName), 
                        claims.Id, 
                        claims.OdsCode);

                    //results from api
                    if (result != null)
                    {
                        //success
                        if (result.IsSuccessStatusCode)
                        {
                            model.Result = true;
                            return View("Complete");
                        } else
                        {
                            //problem
                            var telemErrors = StaticReferralHelper.GetAPIErrorDictionary(await result.Content.ReadAsStringAsync());
                            telemErrors.TryAdd("User", claims.Mail);
                            telemErrors.TryAdd("ODS", claims.OdsCode);
                            telemErrors.TryAdd("UserID", claims.Id);
                            telemErrors.TryAdd("StatusCode", result.StatusCode.ToString());

                            _telemetry.TrackEvent("GoneWrong:UploadIssue", telemErrors);
                            telemErrors.TryGetValue("Detail", out string? detailerror);
                            switch (result.StatusCode)
                            {
                                case HttpStatusCode.BadRequest:
                                    
                                    //extract errors                                    
                                    ModelState.AddModelError(model.FormFile.Name, detailerror??"Unknown error");
                                    var jObj = JObject.Parse(await result.Content.ReadAsStringAsync());
                                    var rowerrors = jObj.SelectToken("$.rowErrors")?.ToObject<Dictionary<int, List<string>>>();

                                    if (rowerrors != null)
                                        model.RowErrors = rowerrors;
                                    model.Result = false;
                                    
                                    return View(model);                                
                                case HttpStatusCode.UnprocessableEntity:
                                    //quota exceeded, too many referrals
                                    return View("GoneWrong", GetErrorModel("Quota exceeded, too many referrals."));
                                case HttpStatusCode.Forbidden:
                                    //odscode doest match upload
                                    return View("GoneWrong", GetErrorModel("Your registered ODS code doesn't match the contents of your upload."));
                                case HttpStatusCode.Unauthorized:
                                    return View("GoneWrong", GetErrorModel("An error has occurred (not authorised)"));
                                case HttpStatusCode.InternalServerError:
                                    return View("GoneWrong", GetErrorModel("An error has occurred (500)"));
                                default:
                                    return View("GoneWrong", GetErrorModel("An error has occurred"));
                            }


                        }




                    } 
                }


                
                return View("GoneWrong", GetErrorModel("An unknown error has occurred"));

            }
            catch (Exception e)
            {
                _telemetry.TrackException(e);
                model.Result = false;
                return View("GoneWrong", GetErrorModel("An error has occurred"));
            }

        }

        [Route("{controller}/sign-out")]
        public async Task<IActionResult> Signout()
        {
            await HttpContext.SignOutAsync("AB2C");
            await HttpContext.SignOutAsync();

            var callbackUrl = Url.Content($"~/");
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                "AB2C", "cookiesb2c");
        }

        private async Task<UserModel> ClaimProfile()
        {
            var claims = User.Claims;
            UserModel model = new UserModel
            {
                Id = claims.Where(w=>w.Type.ToLower()== "http://schemas.microsoft.com/identity/claims/objectidentifier").FirstOrDefault()?.Value ?? "Unknown",
                Mail = claims.Where(w => w.Type.ToLower() == "emails").FirstOrDefault()?.Value ?? "Unknown",
                OrgName = claims.Where(w => w.Type.ToLower() == "extension_orgname").FirstOrDefault()?.Value ?? "Unknown",
                OdsCode = claims.Where(w => w.Type.ToLower() == "extension_ods").FirstOrDefault()?.Value ?? "Unknown",
                DisplayName = claims.Where(w => w.Type.ToLower() == "name").FirstOrDefault()?.Value ?? "Unknown"
            };

            if (model.OdsCode!= null)
            {
                var quota = await _WmsReferralService.ElectiveCareQuota(model.OdsCode);
                if (quota.Status == HttpStatusCode.OK)
                {
                    model.ReferralQuotaTotal = quota.Result.QuotaTotal;
                    model.ReferralQuotaRemaining = quota.Result.QuotaRemaining;
                } 
            }
            return model;
        }

        private ErrorViewModel GetErrorModel(string message, string traceid = "")
        {
            return new ErrorViewModel()
            {
                RequestId = "",
                TraceId = traceid,
                Message = $"Error: {message}."
            };
        }

    }
}
