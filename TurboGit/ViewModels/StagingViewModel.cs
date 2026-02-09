// TurboGit/ViewModels/StagingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TurboGit.Services; // Assuming a GitService exists for git operations

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel for the staging area (ChangesView).
    /// Manages unstaged and staged files, and handles staging operations.
    /// </summary>
    public partial class StagingViewModel : ObservableObject
    {
        private readonly IGitService _gitService;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _unstagedFiles;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _stagedFiles;

        public StagingViewModel()
        {
            // In a real DI scenario, IGitService would be injected.
            _gitService = new GitService();
            UnstagedFiles = new ObservableCollection<GitFileStatus>();
            StagedFiles = new ObservableCollection<GitFileStatus>();
        }

        /// <summary>
        /// Loads the file status for the given repository path.
        /// This is designed to be called when the user selects a repository.
        /// </summary>
        /// <param name="repoPath">The file system path to the repository.</param>
        public async void LoadChanges(string repoPath)
        {
            if (string.IsNullOrEmpty(repoPath)) return;

            // Clear previous state
            UnstagedFiles.Clear();
            StagedFiles.Clear();

            // All Git operations should be asynchronous to prevent UI freezes.
            var statuses = await _gitService.GetFileStatusAsync(repoPath);
            foreach (var status in statuses)
            {
                if (status.IsStaged)
                {
                    StagedFiles.Add(status);
                }
                else
                {
                    UnstagedFiles.Add(status);
                }
            }
        }
    }

    // A model representing the status of a file.
    public class GitFileStatus
    {
        public string FilePath { get; set; }
        public bool IsStaged { get; set; }
        public string Status { get; set; } // e.g., "Modified", "New", "Deleted"
    }
}