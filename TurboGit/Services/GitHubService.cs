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
        // and not hardcoded. We'll retrieve them from environment variables.
        private static string ClientId => Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_ID")
            ?? throw new InvalidOperationException("GitHub Client ID is not configured. Please set the TURBOGIT_GITHUB_CLIENT_ID environment variable.");

        private static string ClientSecret => Environment.GetEnvironmentVariable("TURBOGIT_GITHUB_CLIENT_SECRET")
            ?? throw new InvalidOperationException("GitHub Client Secret is not configured. Please set the TURBOGIT_GITHUB_CLIENT_SECRET environment variable.");

        private readonly GitHubClient _client = new GitHubClient(new ProductHeaderValue("TurboGit"));

        /// <summary>
        /// Gets the URL to initiate the GitHub OAuth login flow.
        /// This URL directs the user to GitHub to authorize the application.
        /// </summary>
        /// <param name="redirectUri">The optional redirect URI. If null, the default is used.</param>
        /// <returns>The GitHub OAuth authorization URL.</returns>
        public string GetGitHubLoginUrl(string? redirectUri = null)
        {
            var request = new OauthLoginRequest(ClientId)
            {
                Scopes = { "repo", "user" }, // Request access to repositories and user profile
                RedirectUri = new Uri(redirectUri ?? Constants.GitHubOAuthCallbackUrl) // Local listener for the callback
            };
            return _client.Oauth.GetGitHubLoginUrl(request).ToString();
        }

        /// <summary>
        /// Exchanges the temporary code received from GitHub for a permanent access token.
        /// </summary>
        /// <param name="code">The temporary code from the OAuth redirect.</param>
        /// <param name="redirectUri">The optional redirect URI used in the initial request.</param>
        /// <returns>An OAuth access token.</returns>
        public async Task<OauthToken> GetAccessToken(string code, string? redirectUri = null)
        {
            var request = new OauthTokenRequest(ClientId, ClientSecret, code);
            if (!string.IsNullOrEmpty(redirectUri))
            {
                request.RedirectUri = new Uri(redirectUri);
            }
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
