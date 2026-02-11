// TurboGit/ViewModels/HistoryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.IO;
using System.Threading.Tasks;
using TurboGit.Services; // Assuming a GitService exists

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel for the HistoryView.
    /// Manages loading and displaying the commit history for a repository.
    /// </summary>
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly IGitService _gitService;
        private readonly IZipExportService _zipExportService;
        private string _currentRepoPath;

        [ObservableProperty]
        private ObservableCollection<GitCommit> _commits;

        public HistoryViewModel()
        {
            // In a real DI scenario, services would be injected.
            _gitService = new GitService();
            _zipExportService = new ZipExportService();
            Commits = new ObservableCollection<GitCommit>();
        }

        /// <summary>
        /// Loads the commit history for the given repository path.
        /// This should be called when the user selects a repository.
        /// </summary>
        /// <param name="repoPath">The file system path to the repository.</param>
        public virtual async void LoadCommits(string repoPath)
        {
            if (string.IsNullOrEmpty(repoPath)) return;

            _currentRepoPath = repoPath;
            Commits.Clear();

            // Asynchronously load commits to keep the UI responsive.
            var commitLog = await _gitService.GetCommitHistoryAsync(repoPath);
            foreach (var commit in commitLog)
            {
                Commits.Add(commit);
            }
        }

        /// <summary>
        /// Exports the selected commit as a Zip archive.
        /// </summary>
        /// <param name="commit">The commit to export.</param>
        [RelayCommand]
        private async Task ExportCommit(GitCommit commit)
        {
            if (commit == null || string.IsNullOrEmpty(_currentRepoPath)) return;

            // In a real application, we would use an IDialogService (e.g., via Avalonia.StorageProvider)
            // to ask the user where to save the file.
            // For this implementation, we default to the User's Downloads folder with a generated name.
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string fileName = $"TurboGit_Export_{commit.Sha.Substring(0, 7)}.zip";
            string fullPath = Path.Combine(downloadsPath, fileName);

            try
            {
                // Ensure the directory exists (it should, but safety first)
                Directory.CreateDirectory(downloadsPath);

                await _zipExportService.ExportCommitAsZipAsync(_currentRepoPath, commit.Sha, fullPath);

                // In a real app, we would show a toast notification or message box.
                Console.WriteLine($"Successfully exported commit {commit.Sha} to {fullPath}");
            }
            catch (Exception ex)
            {
                // Handle errors (e.g. log, show message)
                Console.WriteLine($"Failed to export zip: {ex.Message}");
            }
        }
    }

    // A model representing a single Git commit for the view.
    public class GitCommit
    {
        public string Sha { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
        public DateTimeOffset CommitDate { get; set; }
    }
}
