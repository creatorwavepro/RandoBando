using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;


    public class SecurityHashPasswordWithSalt
    {
        public static (string hashedPassword, string salt) HashPasswordWithSalt(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return (hashed, Convert.ToBase64String(salt));
        }
    }

