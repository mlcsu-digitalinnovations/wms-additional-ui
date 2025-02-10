using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;
using WmsElectiveCarePortal.Models;
using WmsElectiveCarePortal.Services;

namespace WmsElectiveCarePortal.Controllers
{
    [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme, Policy = "ElectiveCareAdmin")]
    public class ManageController : Controller
    {
        private readonly ILogger<ManageController> _logger;
        private readonly IUserRepository _userRepository;
        
        public ManageController(ILogger<ManageController> logger, IUserRepository userRepository, IConfiguration configuration)
        {
            _logger = logger;
            _userRepository = userRepository;
            
        }
        [AllowAnonymous]
        [Route("{controller}/sign-out")]
        public IActionResult Signout()
        {
            var properties = new AuthenticationProperties { RedirectUri = "/" };
            return SignOut(
                properties,
                OpenIdConnectDefaults.AuthenticationScheme, "Cookies");
        }

        public async Task<IActionResult> Index()
        {
            IEnumerable<UserModel> users = await _userRepository.Get();

            return View(users);
        }

        public async Task<IActionResult> Edit(string id)
        {
            UserModel user = await _userRepository.Get(id);

            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(UserModel model)
        {
            var result = await _userRepository.Update(model);
            model.UserUpdatedMessage = "Success";

            if (result.UserUpdated == false)
                model.UserUpdatedMessage = "Error, something went wrong.";

            return View(model);
        }

        public IActionResult Create()
        {


            return View(new UserModel());
        }
        [HttpPost]
        public async Task<IActionResult> Create(UserModel model)
        {
            var result = await _userRepository.Add(model);

            //pass random password back
            model.Password = result.Password;

            if (result.UserUpdated == false)
            {
                model.UserUpdatedMessage += "<br />Error, something went wrong.";
            }
            else
            {
                model.UserUpdatedMessage = "Successfully added the user.<br /><br />Username: " + model.Mail + "<br />Password: " + model.Password + "</p>";
            }


            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            UserModel user = await _userRepository.Get(id);
            return View(user);
        }

        public async Task<JsonResult> DeleteUser(string id)
        {
            if (id == null)
            {
                return Json("No user ID specified.");
            }

            bool result = await _userRepository.Delete(id);
            if (!result)
                return Json("User could not be deleted.");

            return Json("Success");
        }

        public IActionResult InviteUser()
        {


            return View(new UserModel() { UserInviteExpiry = DateTime.Now.AddDays(1) });
        }
        [HttpPost]
        public async Task<IActionResult> InviteUser(UserModel model)
        {
            if (model.UserInviteExpiry.HasValue)
                if (model.UserInviteExpiry.Value.DateTime < DateTime.Now)
                {
                    ModelState.TryAddModelError("UserInviteExpiry", "Token expiry can't be in the past");
                    return View(model);
                } else if (model.UserInviteExpiry.Value.DateTime > DateTime.Now.AddDays(3))
                {
                    ModelState.TryAddModelError("UserInviteExpiry", "Token expiry can't be more than 3 days in the future.");
                    return View(model);
                }



            UserModel user = await _userRepository.InviteUser(model);

            user.UserUpdated = true;
            user.UserUpdatedMessage = $"Successfully created the user invite.<br /><br />Username: {user.Mail}<br /> <a href=\"{ user.UserInviteUrl}\">Invite Link</a></p>";
            
            return View(user);
        }

    }

}
