using System;
using System.Threading.Tasks;
using TurboGit.Services;
using Xunit;

namespace TurboGit.Tests.Services
{
    [Collection("Sequential")]
    public class GitHubServiceTests : IDisposable
    {
        private readonly string? _originalClientId;
        private readonly string? _originalClientSecret;

        public GitHubServiceTests()
        {
            _originalClientId = Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID");
            _originalClientSecret = Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", _originalClientId);
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET", _originalClientSecret);
        }

        [Fact]
        public void GetGitHubLoginUrl_ThrowsInvalidOperationException_WhenClientIdIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", null);
            var service = new GitHubService();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => service.GetGitHubLoginUrl());
            Assert.Contains("GitHub Client ID is not configured", exception.Message);
        }

        [Fact]
        public async Task GetAccessToken_ThrowsInvalidOperationException_WhenClientIdIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET", "some_secret");
            var service = new GitHubService();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccessToken("code"));
            Assert.Contains("GitHub Client ID is not configured", exception.Message);
        }

        [Fact]
        public async Task GetAccessToken_ThrowsInvalidOperationException_WhenClientSecretIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", "some_id");
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET", null);
            var service = new GitHubService();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccessToken("code"));
            Assert.Contains("GitHub Client Secret is not configured", exception.Message);
        }

        [Fact]
        public void GetGitHubLoginUrl_ReturnsUrl_WhenClientIdIsPresent()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", "test_id");
            var service = new GitHubService();

            // Act
            var url = service.GetGitHubLoginUrl();

            // Assert
            Assert.Contains("client_id=test_id", url);
        }
    }
}
