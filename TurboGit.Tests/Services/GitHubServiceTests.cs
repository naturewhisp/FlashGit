using System;
using System.Linq;
using Moq;
using Octokit;
using TurboGit.Infrastructure;
using TurboGit.Services;
using Xunit;

namespace TurboGit.Tests.Services
{
    public class GitHubServiceTests
    {
        private readonly Mock<IGitHubClient> _gitHubClientMock;
        private readonly string _testClientId = "test-client-id";
        private readonly string _testClientSecret = "test-client-secret";

        public GitHubServiceTests()
        {
            _gitHubClientMock = new Mock<IGitHubClient>();
        }

        [Fact]
        public void GetGitHubLoginUrl_WithDefaultRedirectUri_ReturnsCorrectUrl()
        {
            // Arrange
            var expectedUrl = new Uri("https://github.com/login/oauth/authorize?client_id=test-client-id&scope=repo%20user&redirect_uri=http%3A%2F%2Flocalhost%3A8989%2Fcallback%2F");

            _gitHubClientMock.Setup(c => c.Oauth.GetGitHubLoginUrl(It.IsAny<OauthLoginRequest>()))
                .Returns(expectedUrl);

            var service = new GitHubService(_gitHubClientMock.Object, _testClientId, _testClientSecret);

            // Act
            var result = service.GetGitHubLoginUrl();

            // Assert
            Assert.Equal(expectedUrl.ToString(), result);
            _gitHubClientMock.Verify(c => c.Oauth.GetGitHubLoginUrl(It.Is<OauthLoginRequest>(r =>
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

            _gitHubClientMock.Setup(c => c.Oauth.GetGitHubLoginUrl(It.IsAny<OauthLoginRequest>()))
                .Returns(expectedUrl);

            var service = new GitHubService(_gitHubClientMock.Object, _testClientId, _testClientSecret);

            // Act
            var result = service.GetGitHubLoginUrl(customRedirectUri);

            // Assert
            Assert.Equal(expectedUrl.ToString(), result);
            _gitHubClientMock.Verify(c => c.Oauth.GetGitHubLoginUrl(It.Is<OauthLoginRequest>(r =>
                r.ClientId == _testClientId &&
                r.Scopes.Contains("repo") &&
                r.Scopes.Contains("user") &&
                r.RedirectUri.ToString() == customRedirectUri
            )), Times.Once);
        }
    }
}
