// Services/GitHubService.cs

using System;
using System.Threading.Tasks;
using Octokit;

namespace TurboGit.Services
{
    /// <summary>
    /// Service to handle interactions with the GitHub API using Octokit.
    /// </summary>
    public class GitHubService : IGitHubService
    {
        // IMPORTANT: These are placeholders. In a real application, these should be stored securely
        // and not hardcoded. For this example, we'll use placeholders.
        private const string ClientId = "YOUR_CLIENT_ID"; // Replace with your GitHub OAuth App Client ID
        private const string ClientSecret = "YOUR_CLIENT_SECRET"; // Replace with your GitHub OAuth App Client Secret

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
                RedirectUri = new Uri("http://localhost:8989/callback") // Local listener for the callback
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
