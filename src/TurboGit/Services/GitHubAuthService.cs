using Octokit;
using System.Diagnostics;
using System.Net;

namespace TurboGit.Services
{
    /// <summary>
    /// Implementazione del servizio di autenticazione OAuth 2.0 per GitHub.
    /// </summary>
    public class GitHubAuthService : IGitHubAuthService
    {
        // ATTENZIONE: Questi valori devono essere gestiti tramite configurazione sicura,
        // non hardcoded nel codice sorgente in produzione.
        private const string ClientId = "YOUR_CLIENT_ID"; // Sostituire con il proprio Client ID
        private const string ClientSecret = "YOUR_CLIENT_SECRET"; // Sostituire con il proprio Client Secret

        private readonly GitHubClient _client;
        private readonly ICredentialStorage _credentialStorage;

        public GitHubAuthService(ICredentialStorage credentialStorage)
        {
            _credentialStorage = credentialStorage;
            _client = new GitHubClient(new ProductHeaderValue("TurboGit"));
        }

        public async Task<bool> AuthenticateAsync()
        {
            if (string.IsNullOrEmpty(ClientId) || ClientId == "YOUR_CLIENT_ID")
            {
                // Blocco per evitare l'esecuzione con valori placeholder.
                // In un'app reale, si leggerebbe da un file di config e si lancerebbe un'eccezione.
                Debug.WriteLine("ERRORE: ClientId e ClientSecret non sono configurati.");
                return false;
            }

            var request = new OauthLoginRequest(ClientId)
            {
                Scopes = { "repo", "read:user" },
            };

            var loginUrl = _client.Oauth.GetGitHubLoginUrl(request);
            var redirectUri = new Uri("http://localhost:8765/"); // Porta di ascolto locale

            try
            {
                // 1. Apri il browser per l'autorizzazione
                Process.Start(new ProcessStartInfo(loginUrl.ToString()) { UseShellExecute = true });

                // 2. Avvia un listener HTTP locale per catturare il callback
                using var listener = new HttpListener();
                listener.Prefixes.Add(redirectUri.ToString());
                listener.Start();

                var context = await listener.GetContextAsync();
                var code = context.Request.QueryString.Get("code");

                // Rispondi al browser per dare un feedback all'utente
                var responseBytes = System.Text.Encoding.UTF8.GetBytes("<html><body><h1>Authentication successful!</h1><p>You can now close this window and return to TurboGit.</p></body></html>");
                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = responseBytes.Length;
                await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                context.Response.Close();
                listener.Stop();

                if (string.IsNullOrEmpty(code))
                {
                    return false;
                }

                // 3. Scambia il codice per un access token
                var tokenRequest = new OauthTokenRequest(ClientId, ClientSecret, code);
                var token = await _client.Oauth.CreateAccessToken(tokenRequest);

                // 4. Salva il token in modo sicuro
                _credentialStorage.SaveToken(token.AccessToken);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Autenticazione fallita: {ex.Message}");
                return false;
            }
        }

        public void Logout()
        {
            _credentialStorage.ClearToken();
        }
    }
}
