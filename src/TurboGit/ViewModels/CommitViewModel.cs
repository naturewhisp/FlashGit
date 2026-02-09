// ViewModels/CommitViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using LibGit2Sharp; // Per accedere all'oggetto Commit
using System;

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel che rappresenta un singolo commit nella UI.
    /// Contiene sia i dati del commit (messaggio, autore) sia le informazioni
    /// per il rendering del grafo (colonna, colore, connessioni).
    /// </summary>
    public partial class CommitViewModel : ObservableObject
    {
        private readonly Commit _commit;

        public string ShortMessage => _commit.MessageShort;
        public string Author => _commit.Author.Name;
        public DateTimeOffset CommitDate => _commit.Author.When;
        public string Sha => _commit.Sha.Substring(0, 7);

        // Proprietà per il rendering del grafo (da calcolare nel CommitHistoryViewModel)
        [ObservableProperty]
        private int _graphColumn;

        [ObservableProperty]
        private string _branchColor = "#FFFFFF"; // Colore default

        // TODO: Aggiungere una collezione di punti o linee per disegnare le connessioni del grafo.

        public CommitViewModel(Commit commit)
        {
            _commit = commit;
        }
    }
}
