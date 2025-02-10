using Microsoft.Graph;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace WmsElectiveCarePortal.Models
{
    public class UserModel : User
    {
        [JsonProperty(PropertyName = "password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "approle", NullValueHandling = NullValueHandling.Ignore)]
        public string AppRole { get; set; }
        [JsonProperty(PropertyName = "odscode", NullValueHandling = NullValueHandling.Ignore)]
        public string OdsCode { get; set; }
        [JsonProperty(PropertyName = "orgname", NullValueHandling = NullValueHandling.Ignore)]
        public string OrgName { get; set; }
        public bool AccountEnabled { get; set; }
        public void SetB2CProfile(string TenantName)
        {
            this.PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = false,
                Password = this.Password,
                ODataType = null
            };
            this.PasswordPolicies = "DisablePasswordExpiration,DisableStrongPassword";
            this.Password = null;
            this.ODataType = null;

            foreach (var item in this.Identities)
            {
                if (item.SignInType == "emailAddress" || item.SignInType == "userName")
                {
                    item.Issuer = TenantName;
                }
            }
        }
        public bool? UserUpdated { get; set; }
        public string UserUpdatedMessage { get; set; } = "";
        public string UserInviteUrl { get; set; } = "";
        [Required]        
        public DateTimeOffset? UserInviteExpiry { get; set; }
        public int ReferralQuotaTotal { get; set; } = -1;
        public int ReferralQuotaRemaining { get; set; } = -1;
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
