// Interfaccia per la gestione sicura delle credenziali.
// Astrae il meccanismo di salvataggio e recupero del token di accesso.
namespace TurboGit.Services
{
    public interface ICredentialStorage
    {
        /// <summary>
        /// Salva in modo sicuro il token di accesso di GitHub.
        /// </summary>
        /// <param name="token">Il token da salvare.</param>
        void SaveToken(string token);

        /// <summary>
        /// Recupera il token di accesso salvato.
        /// </summary>
        /// <returns>Il token salvato o null se non esiste.</returns>
        string? GetToken();

        /// <summary>
        /// Rimuove il token di accesso salvato.
        /// </summary>
        void ClearToken();
    }
}
