using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LibGit2Sharp;

namespace TurboGit.ViewModels
{
    /// <summary>
    /// Represents a single line in a diff view.
    /// </summary>
    public class DiffLineViewModel : INotifyPropertyChanged
    {
        public string Text { get; set; }
        public ChangeKind Type { get; set; }
        public bool IsSelected { get; set; } // For line-by-line staging (future)
        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Represents a "hunk" or a contiguous block of changes in a diff.
    /// </summary>
    public class DiffHunkViewModel : INotifyPropertyChanged
    {
        public List<DiffLineViewModel> Lines { get; } = new List<DiffLineViewModel>();
        public string Header { get; set; } // e.g., "@@ -1,5 +1,6 @@"
        public ICommand StageHunkCommand { get; }
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Repository _repo;
        private readonly PatchEntryChanges _patchChanges;

        public DiffHunkViewModel(Repository repo, PatchEntryChanges patchChanges, string header, IEnumerable<DiffLineViewModel> lines)
        {
            _repo = repo;
            _patchChanges = patchChanges;
            Header = header;
            Lines.AddRange(lines);
            StageHunkCommand = new AsyncRelayCommand(StageHunk);
        }

        private async Task StageHunk()
        {
            // This is a complex operation. libgit2sharp does not directly support
            // staging hunks. The strategy is to create a patch from this hunk
            // and apply it to the index.
            
            // NOTE: The following is a conceptual implementation.
            // A more robust solution requires careful patch generation.
            
            var patchContent = new StringBuilder();
            patchContent.AppendLine($"--- a/{_patchChanges.Path}");
            patchContent.AppendLine($"+++ b/{_patchChanges.Path}");
            patchContent.AppendLine(Header);
            foreach (var line in Lines)
            {
                char prefix = ' ';
                if (line.Type == ChangeKind.Added) prefix = '+';
                if (line.Type == ChangeKind.Deleted) prefix = '-';
                patchContent.AppendLine($"{prefix}{line.Text}");
            }

            // Create a patch object in memory
            var patch = Patch.From(patchContent.ToString(), PatchOptions.None);

            // Apply this patch to the index
            _repo.Apply(patch, ApplyLocation.Index);

            // TODO: Refresh the UI to show the staged changes.
            System.Diagnostics.Debug.WriteLine($"Staging hunk for file: {_patchChanges.Path}");
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Manages the view for staging files and hunks.
    /// </summary>
    public class StagingViewModel : INotifyPropertyChanged
    {
        private readonly Repository _repo;
        public List<DiffHunkViewModel> Hunks { get; } = new List<DiffHunkViewModel>();
        public event PropertyChangedEventHandler PropertyChanged;

        public StagingViewModel(Repository repo)
        {
            _repo = repo;
        }

        public void LoadChanges(TreeChanges changes)
        {
            Hunks.Clear();
            
            // This example focuses on a single file's changes for simplicity.
            // A real UI would show a list of changed files.
            var firstChange = changes.FirstOrDefault(c => c.Status != ChangeKind.Unmodified);
            if (firstChange == null) return;

            var patch = _repo.Diff.Compare<Patch>(new[] { firstChange.Path });
            var patchEntry = patch[firstChange.Path];

            if (patchEntry != null)
            {
                // LibGit2Sharp's Patch object gives us hunks directly
                // However, for more control, we can parse the patch text.
                // Here, we simulate hunk creation from the patch lines.
                // A real implementation would parse this more intelligently.
                
                var lines = patchEntry.Patch.Split('\n');
                var currentHunkLines = new List<DiffLineViewModel>();
                string currentHeader = "";

                foreach (var line in lines)
                {
                    if (line.StartsWith("@@"))
                    {
                        if (currentHunkLines.Any())
                        {
                            Hunks.Add(new DiffHunkViewModel(_repo, patchEntry, currentHeader, currentHunkLines));
                            currentHunkLines = new List<DiffLineViewModel>();
                        }
                        currentHeader = line;
                    }
                    else if (line.StartsWith("+") || line.StartsWith("-") || line.StartsWith(" "))
                    {
                        ChangeKind type = ChangeKind.Unmodified;
                        if (line.StartsWith("+")) type = ChangeKind.Added;
                        if (line.StartsWith("-")) type = ChangeKind.Deleted;
                        
                        currentHunkLines.Add(new DiffLineViewModel { Text = line.Substring(1), Type = type });
                    }
                }

                if (currentHunkLines.Any())
                {
                    Hunks.Add(new DiffHunkViewModel(_repo, patchEntry, currentHeader, currentHunkLines));
                }
            }
            
            // Notify UI of changes
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hunks)));
        }
    }

    // Helper class for Commands
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public async void Execute(object parameter) => await _execute();
        public AsyncRelayCommand(Func<Task> execute) { _execute = execute; }
    }
}
