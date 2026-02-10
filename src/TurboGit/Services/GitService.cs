// Services/GitService.cs
using LibGit2Sharp;
using System.Collections.Generic;
using System.Linq;

namespace TurboGit.Services
{
    public interface IGitService
    {
        IEnumerable<Commit> GetCommitHistory(string repositoryPath);
    }

    public class GitService : IGitService
    {
        /// <summary>
        /// Recupera la cronologia dei commit utilizzando LibGit2Sharp.
        /// L'ordinamento topologico è essenziale per un corretto rendering del grafo.
        /// </summary>
        /// <param name="repositoryPath">Percorso del repository.</param>
        /// <returns>Una collezione di oggetti Commit.</returns>
        public IEnumerable<Commit> GetCommitHistory(string repositoryPath)
        {
            using (var repo = new Repository(repositoryPath))
            {
                // QueryBy con ordinamento topologico è fondamentale per il grafo
                return repo.Commits.QueryBy(new CommitFilter
                {
                    SortBy = CommitSortStrategies.Topological
                }).ToList();
            }
        }
    }
}
