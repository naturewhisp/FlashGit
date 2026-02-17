using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Octokit;
using TurboGit.Infrastructure;
using TurboGit.Services;
using Xunit;

namespace TurboGit.Tests.Services
{
    [Collection("Sequential")]
    public class GitHubServiceTests : IDisposable
    {
        private readonly Mock<IGitHubClient> _gitHubClientMock;
        private readonly Mock<IOauthOperations> _oauthOperationsMock;
        private readonly string _testClientId = "test-client-id";
        private readonly string _testClientSecret = "test-client-secret";
        private readonly string? _originalClientId;
        private readonly string? _originalClientSecret;

        public GitHubServiceTests()
        {
            _gitHubClientMock = new Mock<IGitHubClient>();
            _oauthOperationsMock = new Mock<IOauthOperations>();
            _gitHubClientMock.Setup(c => c.Oauth).Returns(_oauthOperationsMock.Object);

            _originalClientId = Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID");
            _originalClientSecret = Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", _originalClientId);
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET", _originalClientSecret);
        }

        [Fact]
        public void Constructor_ThrowsInvalidOperationException_WhenClientIdIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new GitHubService());
            Assert.Contains("GitHub Client ID is not configured", exception.Message);
        }

        [Fact]
        public void Constructor_ThrowsInvalidOperationException_WhenClientSecretIsMissing()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID", "some_id");
            Environment.SetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET", null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new GitHubService());
            Assert.Contains("GitHub Client Secret is not configured", exception.Message);
        }

        [Fact]
        public void GetGitHubLoginUrl_WithDefaultRedirectUri_ReturnsCorrectUrl()
        {
            // Arrange
            var expectedUrl = new Uri("https://github.com/login/oauth/authorize?client_id=test-client-id&scope=repo%20user&redirect_uri=http%3A%2F%2Flocalhost%3A8989%2Fcallback%2F");

            _oauthOperationsMock.Setup(o => o.GetGitHubLoginUrl(It.IsAny<OauthLoginRequest>()))
                .Returns(expectedUrl);

            var service = new GitHubService(_gitHubClientMock.Object, _testClientId, _testClientSecret);

            // Act
            var result = service.GetGitHubLoginUrl();

            // Assert
            Assert.Equal(expectedUrl.ToString(), result);
            _oauthOperationsMock.Verify(o => o.GetGitHubLoginUrl(It.Is<OauthLoginRequest>(r =>
                r.ClientId == _testClientId &&
                r.Scopes.Contains("repo") &&
                r.Scopes.Contains("user") &&
                r.RedirectUri.ToString() == Constants.GitHubOAuthCallbackUrl
            )), Times.Once);
        }

        [Fact]
        public void GetGitHubLoginUrl_WithCustomRedirectUri_ReturnsCorrectUrl()
        {
            // Arrange
            var customRedirectUri = "http://127.0.0.1:1234/callback/";
            var expectedUrl = new Uri($"https://github.com/login/oauth/authorize?client_id=test-client-id&scope=repo%20user&redirect_uri={Uri.EscapeDataString(customRedirectUri)}");

            _oauthOperationsMock.Setup(o => o.GetGitHubLoginUrl(It.IsAny<OauthLoginRequest>()))
                .Returns(expectedUrl);

            var service = new GitHubService(_gitHubClientMock.Object, _testClientId, _testClientSecret);

            // Act
            var result = service.GetGitHubLoginUrl(customRedirectUri);

            // Assert
            Assert.Equal(expectedUrl.ToString(), result);
            _oauthOperationsMock.Verify(o => o.GetGitHubLoginUrl(It.Is<OauthLoginRequest>(r =>
                r.ClientId == _testClientId &&
                r.Scopes.Contains("repo") &&
                r.Scopes.Contains("user") &&
                r.RedirectUri.ToString() == customRedirectUri
            )), Times.Once);
        }
    }
}
