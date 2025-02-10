using Jose;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace WmsMskReferral.Helpers
{
    public static class TokenHelper
    {
        public static string CreateClientAuthJwt(string oAuthUrl, string clientSecret, string clientId)
        {
            var payload = new Dictionary<string, object>()
            {
                {"sub", clientId},
                {"aud", oAuthUrl + "token"},
                {"iss", clientId},
                {"exp", DateTimeOffset.Now.AddMinutes(10).ToUnixTimeSeconds() },
                {"jti", Guid.NewGuid()}
            };


            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clientSecret));

            return JWT.Encode(payload, securityKey, JwsAlgorithm.HS256);
        }

        //public static string CreateClientAuthJwtNative(string oAuthUrl, string clientSecret, string clientId)
        //{
            //var handler = new JwtSecurityTokenHandler();

            //var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clientSecret));

            //var sc = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            //var jwtSecurityToken = new SecurityTokenDescriptor
            //{
            //    Audience = oAuthUrl + "token",
            //    Issuer = clientId,
            //    Subject = new ClaimsIdentity(new List<Claim> { new Claim("sub", clientId) }),
            //    Expires = DateTimeOffset.Now.AddMinutes(10).UtcDateTime,  
            //    AdditionalHeaderClaims = new Dictionary<string, object>() { { "jti", Guid.NewGuid() }   }, //this doesnt get added to token???
            //    SigningCredentials = sc
            //};

            //return handler.CreateEncodedJwt(jwtSecurityToken);
        //}
    }
}
