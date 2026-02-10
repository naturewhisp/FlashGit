using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using TurboGit.Infrastructure.Security;

namespace TurboGit.Infrastructure.Services
{
    /// <summary>
    /// Service to handle GitHub OAuth 2.0 authentication flow.
    /// </summary>
    public class GitHubAuthService
    {
        // IMPORTANT: In a real application, these should NOT be hardcoded.
        // They should be stored in a secure configuration file or retrieved from a server.
        private readonly string _clientId = "YOUR_CLIENT_ID"; // Replace with your GitHub App Client ID
        private readonly string _clientSecret = "YOUR_CLIENT_SECRET"; // Replace with your GitHub App Client Secret
        private readonly HttpClient _httpClient;

        public GitHubAuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TurboGit");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Gets the URL to which the user should be redirected to authorize the application.
        /// </summary>
        /// <returns>The GitHub authorization URL.</returns>
        public string GetGitHubAuthorizationUrl()
        {
            // The "repo" scope requests full control of private and public repositories.
            // Adjust scopes as needed for the application's functionality.
            const string scopes = "repo,user";
            return $"https://github.com/login/oauth/authorize?client_id={_clientId}&scope={Uri.EscapeDataString(scopes)}";
        }

        /// <summary>
        /// Exchanges the temporary code received from GitHub for a permanent access token.
        /// This method now includes improved error handling.
        /// </summary>
        /// <param name="code">The temporary code from the GitHub callback.</param>
        /// <returns>A tuple indicating success and the access token or an error message.</returns>
        public async Task<(bool Success, string? Token, string? ErrorMessage)> GetAccessTokenAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return (false, null, "Authorization code is missing or empty.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "client_id", _clientId },
                        { "client_secret", _clientSecret },
                        { "code", code }
                    })
                };

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    // Improved error logging
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"GitHub API error: {response.StatusCode} - {errorContent}");
                    return (false, null, $"FAILED Failed to get access token. GitHub returned: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseContent);

                if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var errorDescription = jsonDoc.RootElement.TryGetProperty("error_description", out var descElement)
                                          ? descElement.GetString()
                                         : "No description provided.";
                    return (false, null, $"An error occurred during authentication: {errorElement.GetString()} - {errorDescription}");
                }

                if (jsonDoc.RootElement.TryGetProperty("access_token", out var tokenElement))
                {
                    var accessToken = tokenElement.GetString();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        // Securely save the token
                        TokenManager.SaveToken(accessToken);
                        return (true, accessToken, null);
                    }
                }

                return (false, null, "Access token was not found in the response from GitHub.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ERROR] Network error during token exchange: { ex.Message }");
                return (false, null, "A network error occurred. Please check your connection and try again.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error during token exchange: { ex.Message }");
                return (false, null, "Received an invalid response from GitHub.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: { ex.Message }");
                return (false, null, "An unexpected error occurred during authentication.");
            }
        }

        /// <summary>
        /// Logs the user out by deleting the stored token.
        /// </summary>
        public void Logout()
        {
            TokenManager.DeleteToken();
        }

        /// <summary>
        /// Retrieves the currently stored access token.
        /// </summary>
        /// <returns>The access token, or null if not authenticated.</returns>
        public string? GetCurrentToken()
        {
            return TokenManager.GetToken();
        }
    }
}