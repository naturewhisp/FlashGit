using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TurboGit.Infrastructure.Security
{
    /// <summary>
    /// Manages secure storage of authentication tokens.
    /// NOTE: This is a basic implementation for cross-platform compatibility.
    /// For production applications, consider using platform-specific credential managers
    /// like Windows Credential Manager (PasswordVault) or macOS KeyChain.
    /// </summary>
    public static class TokenManager
    {
        // A basic, hardcoded entropy is not ideal but serves for this example.
        // In a real-world scenario, this should be managed more securely,
        // perhaps derived from a machine-specific identifier.
        private static readonly byte[] s_entropy = Encoding.Unicode.GetBytes("TurboGitSaltValue");

        /// <summary>
        /// Gets the path to the file where the encrypted token will be stored.
        /// </summary>
        private static string GetTokenFilePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var turboGitFolder = Path.Combine(appDataPath, "TurboGit");
            Directory.CreateDirectory(turboGitFolder); // Ensure the directory exists
            return Path.Combine(turboGitFolder, "user.token");
        }

        /// <summary>
        /// Saves the authentication token securely to local storage.
        /// The token is encrypted using the Data Protection API (DPAPI) for the current user scope.
        /// </summary>
        /// <param name="token">The token to save.</param>
        public static void SaveToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                // If the token is null or empty, delete the existing token file.
                DeleteToken();
                return;
            }

            var tokenBytes = Encoding.UTF8.GetBytes(token);
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser.
                // This means only the current user on the current machine can decrypt the data.
                var encryptedData = ProtectedData.Protect(tokenBytes, s_entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(GetTokenFilePath(), encryptedData);
            }
            catch (Exception ex)
            {
                // Handle or log the exception appropriately.
                // For example, platform not supported, etc.
                Console.WriteLine($"[ERROR] Error saving token: { ex.Message }");
                // As a fallback, you might store it in a less secure way or notify the user.
            }
        }

        /// <summary>
        /// Retrieves the authentication token from local storage.
        /// </summary>
        /// <returns>The decrypted token, or null if it doesn't exist or fails to decrypt</returns>
        public static string? GetToken()
        {
            var tokenFilePath = GetTokenFilePath();
            if (!File.Exists(tokenFilePath))
            {
                return null;
            }

            try
            {
                var encryptedData = File.ReadAllBytes(tokenFilePath);
                // Decrypt the data using the same scope and entropy.
                var decryptedData = ProtectedData.Unprotect(encryptedData, s_entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedData);
            }
            catch (Exception ex)
            {
                // Handle or log the exception.
                // This could happen if the file is corrupted, or permissions changed.
                Console.WriteLine($"[ERROR] Error retrieving token: { ex.Message }");
                return null;
            }
        }

        /// <summary>
        /// Deletes the stored token.
        /// </summary>
        public static void DeleteToken()
        {
            var tokenFilePath = GetTokenFilePath();
            if (File.Exists(tokenFilePath))
            {
                try
                {
                    File.Delete(tokenFilePath);
                }
                catch (Exception ex)
                {
                    // Handle or log the exception.
                    Console.WriteLine($"[ERROR] Error deleting token: { ex.Message }");
                }
            }
        }
    }
}