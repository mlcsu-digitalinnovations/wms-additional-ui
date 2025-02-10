using Jose;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace WmsSelfReferral.Helpers
{
    public static class TokenHelper
    {
        public static string CreateClientAuthJwt(string nhsLoginUrl,string rsaKeyName, string nhsLoginClientId)
        {
            var payload = new Dictionary<string, object>()
            {
                {"sub", nhsLoginClientId},
                {"aud", nhsLoginUrl + "token"},
                {"iss", nhsLoginClientId},
                {"exp", DateTimeOffset.Now.AddMinutes(10).ToUnixTimeSeconds() },
                {"jti", Guid.NewGuid()}
            };

      
            var key = RSA.Create();
      
            rsaKeyName = rsaKeyName.Replace("\\n", Environment.NewLine);            
            key.ImportFromPem(rsaKeyName);

            return JWT.Encode(payload, key, JwsAlgorithm.RS512);           
        }

        public static string CreateClientAuthJwtNative(string nhsLoginUrl, string rsaKeyName, string nhsLoginClientId)
        {
            var handler = new JwtSecurityTokenHandler();

            var key = RSA.Create();
            rsaKeyName = rsaKeyName.Replace("\\n", Environment.NewLine);
            key.ImportFromPem(rsaKeyName);

            RsaSecurityKey securitykey = new RsaSecurityKey(key);

            var sc = new SigningCredentials(securitykey, SecurityAlgorithms.RsaSha512);
            
            var jwtSecurityToken = new SecurityTokenDescriptor
            {
                Audience = nhsLoginUrl + "token",
                Issuer = nhsLoginClientId,
                Subject = new ClaimsIdentity(new List<Claim> { new Claim("sub", nhsLoginClientId) }),
                Expires = DateTimeOffset.Now.AddMinutes(10).UtcDateTime,  
                AdditionalHeaderClaims = new Dictionary<string, object>() { { "jti", Guid.NewGuid() }   }, //this doesnt get added to token???
                SigningCredentials = sc 
            };

            return handler.CreateEncodedJwt(jwtSecurityToken);
        }
    }
}
