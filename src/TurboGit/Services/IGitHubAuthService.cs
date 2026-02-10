// Interfaccia per il servizio di autenticazione GitHub.
// Definisce il contratto per avviare il flusso OAuth e gestire il token.
namespace TurboGit.Services
{
    public interface IGitHubAuthService
    {
        /// <summary>
        /// Avvia il processo di autenticazione OAuth 2.0.
        /// Apre il browser per l'autorizzazione dell'utente e attende il callback.
        /// </summary>
        /// <returns>True se l'autenticazione ha avuto successo, altrimenti false.</returns>
        Task<bool> AuthenticateAsync();

        /// <summary>
        /// Esegue il logout eliminando il token salvato.
        /// </summary>
        void Logout();
    }
}
