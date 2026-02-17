using System;
using System.Threading.Tasks;
using Xunit;
using TurboGit.Services;
using Octokit;

namespace TurboGit.Tests.Services
{
    public class GitHubServiceTests : IDisposable
    {
        private readonly string _originalClientId;
        private readonly string _originalClientSecret;

        public GitHubServiceTests()
        {
            _originalClientId = Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID");
            _originalClientSecret = Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET");
        }

        public void Dispose()
        {
            // Restore original environment variables
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", _originalClientId);
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET", _originalClientSecret);
        }

        [Fact]
        public void GetGitHubLoginUrl_ShouldThrow_WhenClientIdIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", null);
            var service = new GitHubService();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => service.GetGitHubLoginUrl("test-state"));
            Assert.Contains("TURBOGIT_GITHUB_CLIENT_ID", exception.Message);
        }

        [Fact]
        public async Task GetAccessToken_ShouldThrow_WhenClientSecretIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", "dummy-client-id");
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET", null);
            var service = new GitHubService();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAccessToken("dummy-code"));
            Assert.Contains("TURBOGIT_GITHUB_CLIENT_SECRET", exception.Message);
        }

        [Fact]
        public void GetGitHubLoginUrl_ShouldSucceed_WhenConfigured()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", "test-client-id");
            var service = new GitHubService();
            string state = "test-state-123";

            // Act
            var url = service.GetGitHubLoginUrl(state);

            // Assert
            Assert.Contains("client_id=test-client-id", url);
            Assert.Contains($"state={state}", url);
        }

        [Fact]
        public void GetClient_ShouldReturnClientWithCorrectCredentials()
        {
            // Arrange
            var service = new GitHubService();
            var token = "test-token";

            // Act
            var client = service.GetClient(token);

            // Assert
            var connection = (Connection)client.Connection;
            Assert.StartsWith("TurboGit", connection.UserAgent);
            Assert.Equal(token, client.Credentials.Password);
            Assert.Equal(AuthenticationType.Oauth, client.Credentials.AuthenticationType);
        }
    }
}
