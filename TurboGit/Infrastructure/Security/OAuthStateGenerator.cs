using System;
using System.Security.Cryptography;

namespace TurboGit.Infrastructure.Security
{
    public static class OAuthStateGenerator
    {
        /// <summary>
        /// Generates a cryptographically secure random state string for OAuth flows.
        /// </summary>
        /// <returns>A hex string representing 32 random bytes.</returns>
        public static string GenerateState()
        {
            byte[] randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToHexString(randomBytes);
        }
    }
}
