using Octokit;
using TurboGit.Models;

namespace TurboGit.Services
{
    /// <summary>
    /// Implementazione del servizio per interagire con l'API di GitHub.
    /// Utilizza il token salvato per effettuare chiamate autenticate.
    /// </summary>
    public class GitHubApiService : IGitHubApiService
    {
        private readonly ICredentialStorage _credentialStorage;
        private GitHubClient _client;

        public GitHubApiService(ICredentialStorage credentialStorage)
        {
            _credentialStorage = credentialStorage;
            _client = new GitHubClient(new ProductHeaderValue("TurboGit"));
        }

        private void EnsureAuthenticatedClient()
        {
            var token = _credentialStorage.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("User is not authenticated.");
            }
            _client.Credentials = new Credentials(token);
        }

        public async Task<UserProfile> GetUserAsync()
        {
            EnsureAuthenticatedClient();
            var user = await _client.User.Current();
            return new UserProfile
            {
                Login = user.Login,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl
            };
        }

        public async Task<IEnumerable<Repository>> GetRepositoriesAsync()
        {
            EnsureAuthenticatedClient();
            // Utilizza opzioni di paginazione per non caricare migliaia di repo in una sola chiamata
            var options = new ApiOptions
            {
                PageSize = 100,
                PageCount = 1
            };

            var repos = await _client.Repository.GetAllForCurrent(options);

            // Mappatura dal modello di Octokit al nostro modello di dominio
            return repos.Select(r => new Models.Repository
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description ?? string.Empty,
                CloneUrl = r.CloneUrl,
                IsPrivate = r.Private,
                IsFork = r.Fork
            }).ToList();
        }
    }
}
