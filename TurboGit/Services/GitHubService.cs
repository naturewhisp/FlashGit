// Services/GitHubService.cs

using System;
using System.Threading.Tasks;
using Octokit;
using TurboGit.Infrastructure;
using TurboGit.Infrastructure.Security;

namespace TurboGit.Services
{
    /// <summary>
    /// Service to handle interactions with the GitHub API using Octokit.
    /// </summary>
    public class GitHubService : IGitHubService
    {
        // IMPORTANT: In a real application, these should be stored securely
        // and not hardcoded. For this example, we'll retrieve them from environment variables.
        private static string ClientId => Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID") ?? "YOUR_CLIENT_ID";
        private static string ClientSecret => Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET") ?? "YOUR_CLIENT_SECRET";

        private readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("TurboGit"));

        /// <summary>
        /// Gets the URL to initiate the GitHub OAuth login flow.
        /// This URL directs the user to GitHub to authorize the application.
        /// </summary>
        /// <returns>The GitHub OAuth authorization URL.</returns>
        public string GetGitHubLoginUrl()
        {
            var request = new OauthLoginRequest(ClientId)
            {
                Scopes = { "repo", "user" }, // Request access to repositories and user profile
                RedirectUri = new Uri(Constants.GitHubOAuthCallbackUrl) // Local listener for the callback
            };
            return _client.Oauth.GetGitHubLoginUrl(request).ToString();
        }

        /// <summary>
        /// Exchanges the temporary code received from GitHub for a permanent access token.
        /// </summary>
        /// <param name="code">The temporary code from the OAuth redirect.</param>
        /// <returns>An OAuth access token.</returns>
        public async Task<OauthToken> GetAccessToken(string code)
        {
            var request = new OauthTokenRequest(ClientId, ClientSecret, code);
            var token = await _client.Oauth.CreateAccessToken(request);

            if (!string.IsNullOrEmpty(token?.AccessToken))
            {
                TokenManager.SaveToken(token.AccessToken);
            }

            return token;
        }

        /// <summary>
        /// Creates and returns an authenticated GitHub client using the provided token.
        /// </summary>
        /// <param name="token">The user's OAuth access token.</param>
        /// <returns>An authenticated GitHubClient instance.</returns>
        public GitHubClient GetClient(string token)
        {
            var authenticatedClient = new GitHubClient(new ProductHeaderValue("TurboGit"))
            {
                Credentials = new Credentials(token)
            };
            return authenticatedClient;
        }
    }
}
