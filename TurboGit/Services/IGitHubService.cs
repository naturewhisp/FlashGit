// Services/IGitHubService.cs

using System.Threading.Tasks;
using Octokit;

namespace TurboGit.Services
{
    /// <summary>
    /// Defines the contract for a service that interacts with the GitHub API.
    /// </summary>
    public interface IGitHubService
    {
        /// <summary>
        /// Gets the URL to initiate the GitHub OAuth login flow.
        /// </summary>
        /// <param name="redirectUri">The optional redirect URI. If null, the default is used.</param>
        /// <returns>The GitHub OAuth authorization URL.</returns>
        string GetGitHubLoginUrl(string? redirectUri = null);

        /// <summary>
        /// Gets an access token from GitHub using the provided temporary code.
        /// </summary>
        /// <param name="code">The temporary code from the OAuth redirect.</param>
        /// <param name="redirectUri">The optional redirect URI used in the initial request.</param>
        /// <returns>An OAuth access token.</returns>
        Task<OauthToken> GetAccessToken(string code, string? redirectUri = null);

        /// <summary>
        /// Gets the authenticated GitHub client.
        /// </summary>
        /// <param name="token">The OAuth access token.</param>
        /// <returns>An authenticated GitHubClient instance.</returns>
        GitHubClient GetClient(string token);
    }
}
