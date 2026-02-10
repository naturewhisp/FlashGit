// TurboGit/ViewModels/HistoryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System;
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

        [ObservableProperty]
        private ObservableCollection<GitCommit> _commits;

        public HistoryViewModel()
        {
            // In a real DI scenario, IGitService would be injected.
            _gitService = new GitService();
            Commits = new ObservableCollection<GitCommit>();
        }

        /// <summary>
        /// Loads the commit history for the given repository path.
        /// This should be called when the user selects a repository.
        /// </summary>
        /// <param name="repoPath">The file system path to the repository.</param>
        public async void LoadCommits(string repoPath)
        {
            if (string.IsNullOrEmpty(repoPath)) return;

            Commits.Clear();

            // Asynchronously load commits to keep the UI responsive.
            var commitLog = await _gitService.GetCommitHistoryAsync(repoPath);
            foreach (var commit in commitLog)
            {
                Commits.Add(commit);
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