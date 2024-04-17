using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


    public interface IJwtTokenServices
    {
        Task<bool> IsValidJWTAsync(string jwtToken);

    }
    public class JwtTokenServices:IJwtTokenServices
    {
        private readonly ISecretsConfiguration _secretsConfiguration;
       

        public JwtTokenServices(ISecretsConfiguration secretsConfiguration)
        {
            _secretsConfiguration = secretsConfiguration;
        }

   


        public async Task<bool> IsValidJWTAsync(string jwtToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // Retrieve the key asynchronously
            var key = _secretsConfiguration.MainJWTTokenKey;
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero // Immediate expiration check
            };

            try
            {
                tokenHandler.ValidateToken(jwtToken, validationParameters, out SecurityToken validatedToken);
                return true; // Token is valid
            }
            catch
            {
                return false; // Token is not valid
            }
        }


        public static byte[] GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                return aes.Key;
            }
        }

      


       

    }

