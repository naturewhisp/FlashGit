// Interfaccia per il servizio di interazione con l'API di GitHub.
// Definisce i metodi per recuperare dati come repository e informazioni utente.
using TurboGit.Models;

namespace TurboGit.Services
{
    public interface IGitHubApiService
    {
        /// <summary>
        /// Recupera la lista dei repository dell'utente autenticato.
        /// </summary>
        /// <returns>Una collezione di oggetti Repository.</returns>
        Task<IEnumerable<Repository>> GetRepositoriesAsync();

        /// <summary>
        /// Recupera le informazioni del profilo dell'utente autenticato.
        /// </summary>
        /// <returns>Un oggetto UserProfile con i dati dell'utente.</returns>
        Task<UserProfile> GetUserAsync();
    }
}
