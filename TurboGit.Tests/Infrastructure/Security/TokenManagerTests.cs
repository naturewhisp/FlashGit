using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using TurboGit.Infrastructure.Security;
using Xunit;

namespace TurboGit.Tests.Infrastructure.Security
{
    // Use [Collection("TokenManager")] to prevent parallel execution if other tests use TokenManager
    [Collection("TokenManager")]
    public class TokenManagerTests : IDisposable
    {
        private readonly string _tokenFilePath;
        private readonly byte[] _legacyEntropy;

        public TokenManagerTests()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _tokenFilePath = Path.Combine(appDataPath, "TurboGit", "user.token");
            _legacyEntropy = Encoding.Unicode.GetBytes("TurboGitSaltValue");

            // Cleanup before test starts to ensure clean state
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }
        }

        public void Dispose()
        {
            // Cleanup after test
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }
        }

        [Fact]
        public void SaveAndGetToken_ShouldWork_Correctly()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            // Arrange
            string expectedToken = "test_token_123";

            // Act
            TokenManager.SaveToken(expectedToken);
            string? actualToken = TokenManager.GetToken();

            // Assert
            Assert.Equal(expectedToken, actualToken);
        }

        [Fact]
        public void DeleteToken_ShouldRemoveFile()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            // Arrange
            TokenManager.SaveToken("temp_token");
            Assert.True(File.Exists(_tokenFilePath));

            // Act
            TokenManager.DeleteToken();

            // Assert
            Assert.False(File.Exists(_tokenFilePath));
            Assert.Null(TokenManager.GetToken());
        }

        [Fact]
        public void GetToken_ShouldReturnNull_WhenFileMissing()
        {
            // This test doesn't use ProtectedData directly so it might pass on Linux if GetToken handles it well
            // But GetToken calls ProtectedData inside... so it depends on if the file exists.
            // Here file does NOT exist, so GetToken returns null immediately without calling ProtectedData.
            // So this test CAN run on Linux.

            // Arrange
            if (File.Exists(_tokenFilePath)) File.Delete(_tokenFilePath);

            // Act
            var token = TokenManager.GetToken();

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public void GetToken_ShouldGracefullyFail_WithLegacyEntropy()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            // This test simulates a user upgrading from the vulnerable version (with hardcoded entropy)
            // to the fixed version (without entropy). The fixed version should fail to decrypt
            // the legacy token and return null, forcing re-login, instead of crashing.

            // Arrange: create a file encrypted with legacy entropy
            string legacyToken = "legacy_token_secret";
            byte[] tokenBytes = Encoding.UTF8.GetBytes(legacyToken);

            // Encrypt using the legacy entropy (hardcoded value from the vulnerability report)
            // NOTE: This assumes we are testing the FIX. If run against the original code,
            // this test will FAIL because it will successfully decrypt it.
            // So we expect GetToken() to return null ONLY after the fix is applied.
            // Before the fix, it would return the token.

            byte[] encryptedData = ProtectedData.Protect(tokenBytes, _legacyEntropy, DataProtectionScope.CurrentUser);

            Directory.CreateDirectory(Path.GetDirectoryName(_tokenFilePath)!);
            File.WriteAllBytes(_tokenFilePath, encryptedData);

            // Act
            string? result = TokenManager.GetToken();

            // Assert
            // If the fix works (removing entropy usage), this should return null because
            // Unprotect(..., null, ...) cannot decrypt data protected with entropy.
            // If the fix is NOT applied, this will return the token.

            // To make this test pass BEFORE the fix (for TDD/verification), I can assert based on current behavior?
            // But the goal is to implement the fix. So I will write the assertion for the FIXED behavior.
            Assert.Null(result);
        }
    }
}
