// ViewModels/LoginViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using TurboGit.Infrastructure;
using TurboGit.Infrastructure.Security;
using TurboGit.Services;

namespace TurboGit.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IGitHubService _gitHubService;

        [ObservableProperty]
        private string _statusMessage;

        public LoginViewModel(IGitHubService gitHubService)
        {
            _gitHubService = gitHubService;
            StatusMessage = "Please log in with GitHub to continue.";
        }

        [RelayCommand]
        private async Task Login()
        {
            StatusMessage = "Waiting for GitHub authentication...";
            // Start a local listener to catch the redirect
            await HandleOAuthCallback();
        }

        private async Task HandleOAuthCallback()
        {
            try
            {
                int port = GetFreePort();
                string redirectUri = $"http://127.0.0.1:{port}/callback/";

                using (var listener = new HttpListener())
                {
                    listener.Prefixes.Add(redirectUri);
                    listener.Start();

                    string state = OAuthStateGenerator.GenerateState();
                    string authUrl = _gitHubService.GetGitHubLoginUrl(state, redirectUri);

                    // Open the user's default browser to the GitHub authorization page
                    Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

                    // Asynchronously wait for one request
                    HttpListenerContext context = await listener.GetContextAsync();
                    var request = context.Request;

                    // The 'code' is the temporary authorization code from GitHub
                    string code = request.QueryString.Get("code");
                    string? returnedState = request.QueryString.Get("state");

                    if (string.IsNullOrEmpty(returnedState) || returnedState != state)
                    {
                        StatusMessage = "Security Error: Invalid OAuth state.";
                        Console.WriteLine($"[Security] OAuth state mismatch. Expected: {state}, Received: {returnedState}");

                        var errResponse = context.Response;
                        string errResponseString = "<html><head><title>TurboGit Auth Error</title></head><body>Authentication failed. Security check (CSRF) failed.</body></html>";
                        byte[] errBuffer = System.Text.Encoding.UTF8.GetBytes(errResponseString);
                        errResponse.ContentLength64 = errBuffer.Length;
                        await errResponse.OutputStream.WriteAsync(errBuffer, 0, errBuffer.Length);
                        errResponse.OutputStream.Close();
                        listener.Stop();
                        return;
                    }

                    // Respond to the browser to close the page
                    var response = context.Response;
                    string responseString = "<html><head><title>TurboGit Auth</title></head><body>Authentication successful! You can now close this window and return to TurboGit.</body></html>";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    var output = response.OutputStream;
                    await output.WriteAsync(buffer, 0, buffer.Length);
                    output.Close();
                    listener.Stop();

                    if (!string.IsNullOrEmpty(code))
                    {
                        StatusMessage = "Exchanging code for access token...";
                        var token = await _gitHubService.GetAccessToken(code, redirectUri);

                        if (token != null && !string.IsNullOrEmpty(token.AccessToken))
                        {
                            // TODO: Securely store the token and navigate to the main app view
                            StatusMessage = $"Login successful! Token: {token.AccessToken.Substring(0, 8)}...";
                        }
                        else
                        {
                            StatusMessage = "Failed to retrieve access token.";
                        }
                    }
                    else
                    {
                        StatusMessage = "Authentication failed. No code received.";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"An error occurred: {ex.Message}";
            }
        }

        private int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
