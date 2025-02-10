using Azure.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.Graph;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WmsElectiveCarePortal.Helpers;
using WmsElectiveCarePortal.Models;
using WmsReferral.Business.Services;

namespace WmsElectiveCarePortal.Services
{
    public class UserManager : IUserRepository
    {
        private readonly GraphServiceClient _graphClient;
        private readonly IODSLookupService _ODSLookupService;
        private TelemetryClient _telemetry;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _extclientIDconfiguration;
        private readonly string _aadTenantId = string.Empty;
        private readonly string _aadClientId = string.Empty;
        private readonly string _aadClientSecret = string.Empty;
        private readonly string _aadDomain = string.Empty;
        private readonly string _AppRole = string.Empty;
        private readonly string _UserOnboard = string.Empty;
        private readonly string _b2cAppRole = string.Empty;
        private readonly string _b2cOdsCode = string.Empty;
        private readonly string _b2cOrgName = string.Empty;
        private readonly string _b2cUserOnboard = string.Empty;
        private readonly string _b2cTenantId = string.Empty;
        private readonly string _b2cTenant = string.Empty;
        private readonly string _b2cClientId = string.Empty;
        private readonly string _b2cTokenSecret = string.Empty;
        private readonly string _b2cDomain = string.Empty;
        private readonly string _b2cAuthorizationUrl = string.Empty;
        private readonly string _b2cRedeemInvitePolicyId = string.Empty;
        private readonly string _b2cTokenIssuer = string.Empty;

        public UserManager(IConfiguration configuration, IODSLookupService odsLookupService, TelemetryClient telemetry, IHttpContextAccessor httpContextAccessor)
        {
            _telemetry = telemetry;
            _extclientIDconfiguration = configuration["AzureAdB2C:ExtensionClientId"];
            _AppRole = configuration["AzureAdB2C:AppRole"];
            _UserOnboard = configuration["AzureAdB2C:UserOnboard"];


            _aadTenantId = configuration["AzureGraphB2C:TenantId"];
            _aadDomain = configuration["AzureGraphB2C:Domain"];
            _aadClientId = configuration["AzureGraphB2C:ClientId"];
            _aadClientSecret = configuration["AzureGraphB2C:ClientSecret"];

            _b2cTenantId = configuration["AzureAdB2C:TenantId"];
            _b2cTenant = configuration["AzureAdB2C:Tenant"];
            _b2cDomain = configuration["AzureAdB2C:Domain"];
            _b2cClientId = configuration["AzureAdB2C:ClientId"];
            _b2cTokenSecret = configuration["AzureAdB2C:TokenSecret"];
            _b2cAuthorizationUrl = configuration["AzureAdB2C:B2CAuthorizationUrl"];
            _b2cRedeemInvitePolicyId = configuration["AzureAdB2C:RedeemInvitePolicyId"];
            _b2cTokenIssuer = configuration["AzureAdB2C:TokenIssuer"];

            _ODSLookupService = odsLookupService;

            B2cCustomAttributeHelper helper = new B2cCustomAttributeHelper(_extclientIDconfiguration);
            _b2cAppRole = helper.GetCompleteAttributeName(_AppRole);
            _b2cOdsCode = helper.GetCompleteAttributeName("ODS");
            _b2cOrgName = helper.GetCompleteAttributeName("OrgName");
            _b2cUserOnboard = helper.GetCompleteAttributeName(_UserOnboard);

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var clientSecretCredential = new ClientSecretCredential(
                _aadTenantId, _aadClientId, _aadClientSecret, options);

            // Set up the Microsoft Graph service client with client credentials
            _graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserModel> Get(string id)
        {
            var result = await _graphClient.Users[id]
                .Request()
                .Select($"id,identities,{_b2cAppRole},{_b2cUserOnboard},{_b2cOdsCode},{_b2cOrgName},displayName,givenname,surname,creationtype,usertype,LastPasswordChangeDateTime,AccountEnabled")
                .GetAsync();

            var arole = result.AdditionalData?.Where(e => e.Key == _b2cAppRole)?.FirstOrDefault().Value?.ToString();
            var ods = result.AdditionalData?.Where(e => e.Key == _b2cOdsCode)?.FirstOrDefault().Value?.ToString();
            var orgname = result.AdditionalData?.Where(e => e.Key == _b2cOrgName)?.FirstOrDefault().Value?.ToString();
            var authMethods = await _graphClient.Users[id].Authentication.Methods.Request().GetAsync();
            return new UserModel
            {
                Id = result.Id,
                AppRole = arole ?? "",
                OdsCode = ods ?? "",
                OrgName = orgname ?? "",
                Identities = result.Identities,
                AdditionalData = result.AdditionalData,
                GivenName = result.GivenName,
                Surname = result.Surname,
                DisplayName = result.DisplayName,
                CreationType = result.CreationType,
                LastPasswordChangeDateTime = result.LastPasswordChangeDateTime,
                AccountEnabled = result.AccountEnabled ?? false,
                UserType = result.UserType
            };
        }

        public async Task<IEnumerable<UserModel>> Get()
        {

            //Get all users including AppTest custom attribute
            var results = await _graphClient.Users
                .Request()
                .Select($"id,identities,{_b2cAppRole},{_b2cUserOnboard},{_b2cOdsCode},{_b2cOrgName},givenname,surname,creationtype,member")
                .GetAsync();

            //Filter users to only those that have the AppTestRole
            List<UserModel> users = new List<UserModel>();
            foreach (var user in results)
            {
                //user.AdditionalData != null && user.AdditionalData.Where(e => e.Key == b2cUserOnboard).First().Value.ToString().ToLower() == "true"
                if (user.CreationType == "LocalAccount")
                {
                    var arole = user.AdditionalData?.Where(e => e.Key == _b2cAppRole).FirstOrDefault().Value?.ToString();
                    var ods = user.AdditionalData?.Where(e => e.Key == _b2cOdsCode).FirstOrDefault().Value?.ToString();
                    var orgname = user.AdditionalData?.Where(e => e.Key == _b2cOrgName)?.FirstOrDefault().Value?.ToString();

                    users.Add(new UserModel
                    {
                        Id = user.Id,
                        AppRole = arole ?? "",
                        OdsCode = ods ?? "",
                        OrgName = orgname ?? "",
                        Identities = user.Identities,
                        AdditionalData = user.AdditionalData,
                        GivenName = user.GivenName,
                        Surname = user.Surname,
                        CreationType = user.CreationType

                    });

                }
            }


            return users;
        }
        public async Task<IEnumerable<UserModel>> GetAll()
        {

            //Get all users including AppTest custom attribute
            var results = await _graphClient.Users
                .Request()
                .Select($"id,identities,{_b2cAppRole},{_b2cUserOnboard},{_b2cOdsCode},{_b2cOrgName},givenname,surname,creationtype,member")
                .GetAsync();

            //Filter users to only those that have the AppTestRole
            List<UserModel> users = new List<UserModel>();
            foreach (var user in results)
            {
                if (user.AdditionalData != null && user.AdditionalData.Where(e => e.Key == _b2cUserOnboard).First().Value.ToString() != "true")
                {
                    users.Add(new UserModel
                    {
                        Id = user.Id,
                        AppRole = "",
                        Identities = user.Identities,
                        AdditionalData = user.AdditionalData,
                        GivenName = user.GivenName,
                        Surname = user.Surname,
                        CreationType = user.CreationType

                    });

                }
                else
                {
                    users.Add(new UserModel
                    {
                        Id = user.Id,
                        AppRole = "",
                        Identities = user.Identities,
                        AdditionalData = user.AdditionalData,
                        GivenName = user.GivenName,
                        Surname = user.Surname,
                        CreationType = user.CreationType

                    });
                }
            }


            return users;
        }

        public async Task<UserModel> Add(UserModel user)
        {
            //lookup ODS Code
            var odsorg = await _ODSLookupService.LookupODSCodeAsync(user.OdsCode);

            //create extensions/custom attributes
            IDictionary<string, object> extensionInstance = new Dictionary<string, object>
            {
                { _b2cAppRole, "User" },
                { _b2cUserOnboard, true },
                { _b2cOdsCode, user.OdsCode.ToUpper() },
                { _b2cOrgName, odsorg.Name ?? "Unknown"}
            };

            //create password
            var password = PasswordHelper.GenerateNewPassword(4, 8, 4);

            try
            {

                //add user
                var result = await _graphClient.Users
                   .Request()
                   .AddAsync(new Microsoft.Graph.User
                   {
                       GivenName = user.GivenName,
                       Surname = user.Surname,
                       DisplayName = user.DisplayName,
                       Identities = new List<ObjectIdentity>
                       {
                        new ObjectIdentity()
                        {
                            SignInType = "emailAddress",
                            Issuer = _aadDomain,
                            IssuerAssignedId = user.Mail
                        }
                       },
                       PasswordProfile = new PasswordProfile()
                       {
                           Password = password,
                           ForceChangePasswordNextSignIn = true
                       },
                       PasswordPolicies = "DisablePasswordExpiration",
                       AccountEnabled = user.AccountEnabled,
                       AdditionalData = extensionInstance
                   });
                user.UserUpdated = true;
                user.Password = password;
                user.UserUpdatedMessage = "Successfully added the user.";
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                user.UserUpdated = false;
                user.UserUpdatedMessage = ex.Message;
            }


            return user;
        }

        public async Task<bool> Delete(string id)
        {
            try
            {
                await _graphClient.Users[id]
                            .Request()
                            .DeleteAsync();
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                return false;
            }





            return true;
        }

        public async Task<UserModel> Update(UserModel user)
        {
            //lookup ODS Code
            var odsorg = await _ODSLookupService.LookupODSCodeAsync(user.OdsCode);

            IDictionary<string, object> extensionInstance = new Dictionary<string, object>
            {
                { _b2cAppRole, "User" },
                { _b2cUserOnboard, true },
                { _b2cOdsCode, user.OdsCode.ToUpper() },
                { _b2cOrgName, odsorg.Name ?? "Unknown"}
            };


            try
            {
                var result = await _graphClient.Users[user.Id]
               .Request()
               .UpdateAsync(new Microsoft.Graph.User
               {
                   GivenName = user.GivenName,
                   Surname = user.Surname,
                   DisplayName = user.DisplayName,
                   PasswordPolicies = "DisablePasswordExpiration",
                   AccountEnabled = user.AccountEnabled,
                   AdditionalData = extensionInstance
               });
                user.UserUpdated = true;
                user.UserUpdatedMessage = "Successfully updated the user.";
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                user.UserUpdated = false;
                user.UserUpdatedMessage = ex.Message;
            }

            return user;
        }

        public async Task<UserModel> InviteUser(UserModel user)
        {
            //lookup ODS Code
            var odsorg = await _ODSLookupService.LookupODSCodeAsync(user.OdsCode);
            user.OdsCode = user.OdsCode.ToUpper();
            user.OrgName = odsorg.Name;

            //build token
            string token = BuildIdToken(user);
            string inviteurl = BuildUrl(token);

            user.UserInviteUrl = inviteurl;
            return user;
        }

        private string BuildIdToken(UserModel model)
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context != null)
            {
                string issuer = _b2cTokenIssuer;

                // All parameters send to Azure AD B2C needs to be sent as claims
                IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", model.Mail, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("name", model.DisplayName, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("ODS", model.OdsCode, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("OrgName", model.OrgName, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("email", model.Mail, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("given_name", model.GivenName, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("family_name", model.Surname, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("tid", _b2cTenantId, System.Security.Claims.ClaimValueTypes.String, issuer)
            };

                // Note: This key phrase needs to be stored also in Azure B2C Keys for token validation
                var securityKey = Encoding.UTF8.GetBytes(_b2cTokenSecret);

                var signingKey = new SymmetricSecurityKey(securityKey);
                SigningCredentials signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

                //token expirydate. default is 1 day
                DateTime tokenExpiry = DateTime.Now.AddDays(1);
                if (model.UserInviteExpiry.HasValue)
                    tokenExpiry = model.UserInviteExpiry.Value.DateTime;


                // Create the token
                JwtSecurityToken token = new JwtSecurityToken(
                        issuer,
                        _b2cClientId,
                        claims,
                        DateTime.Now,
                        tokenExpiry,
                        signingCredentials);

                // Get the representation of the signed token
                JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

                return jwtHandler.WriteToken(token);
            }
            return "";
        }

        private string BuildUrl(string token)
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context != null)
            {
                string nonce = Guid.NewGuid().ToString("n");
                string redirecturi = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase.Value}/redeem";
                //redirecturi = $"https://jwt.ms";
                return string.Format(_b2cAuthorizationUrl,
                        _b2cTenant,
                        _b2cRedeemInvitePolicyId,
                        _b2cClientId,
                        Uri.EscapeDataString(redirecturi),
                        nonce) + "&id_token_hint=" + token;

            }
            return "";
        }
    }
}
