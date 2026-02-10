// ViewModels/CommitHistoryViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TurboGit.Services; // Assumendo che IGitService sia qui
using TurboGit.Models;   // Assumendo che CommitViewModel sia qui

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel per la vista della cronologia dei commit.
    /// Gestisce il caricamento asincrono dei commit e li espone alla View.
    /// </summary>
    public partial class CommitHistoryViewModel : ObservableObject
    {
        private readonly IGitService _gitService;
        private string _repositoryPath;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<CommitViewModel> Commits { get; } = new();

        public CommitHistoryViewModel(IGitService gitService)
        {
            _gitService = gitService;
        }

        /// <summary>
        /// Carica la cronologia dei commit per il repository specificato.
        /// L'operazione viene eseguita su un thread in background per non bloccare la UI.
        /// </summary>
        /// <param name="repoPath">Il percorso del repository locale.</param>
        public async Task LoadCommitsAsync(string repoPath)
        {
            _repositoryPath = repoPath;
            IsLoading = true;
            Commits.Clear();

            try
            {
                // Esegue l'operazione Git (potenzialmente lunga) in un thread separato.
                var commitData = await Task.Run(() => _gitService.GetCommitHistory(_repositoryPath));

                // Popola la collezione. Questo deve avvenire sul thread UI,
                // ma l'uso di ObservableCollection da un metodo async gestisce il marshalling.
                foreach (var commit in commitData)
                {
                    Commits.Add(new CommitViewModel(commit));
                }

                // TODO: Implementare l'algoritmo di layout del grafo qui.
                // Questo algoritmo dovrebbe calcolare le posizioni dei nodi e le connessioni
                // e popolare le proprietà grafiche in ogni CommitViewModel.
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
