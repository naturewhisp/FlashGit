// TurboGit/ViewModels/StagingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TurboGit.Core.Models;
using TurboGit.Services;

namespace TurboGit.ViewModels
{
    public partial class StagingViewModel : ObservableObject
    {
        private readonly IGitService _gitService;
        private string _currentRepoPath = string.Empty;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _unstagedFiles;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _stagedFiles;

        [ObservableProperty]
        private GitFileStatus _selectedFile;

        [ObservableProperty]
        private string _diffContent = string.Empty;

        public StagingViewModel(IGitService? gitService = null)
        {
            _gitService = gitService ?? new GitService();
            UnstagedFiles = new ObservableCollection<GitFileStatus>();
            StagedFiles = new ObservableCollection<GitFileStatus>();
        }

        public virtual async Task LoadChanges(string repoPath)
        {
            _currentRepoPath = repoPath;
            if (string.IsNullOrEmpty(_currentRepoPath)) return;

            await RefreshStatus();
        }

        private async Task RefreshStatus()
        {
            var statuses = await _gitService.GetFileStatusAsync(_currentRepoPath);
            var statusList = statuses.ToList();

            StagedFiles = new ObservableCollection<GitFileStatus>(statusList.Where(s => s.IsStaged));
            UnstagedFiles = new ObservableCollection<GitFileStatus>(statusList.Where(s => !s.IsStaged));
        }

        [RelayCommand]
        private async Task StageFile(GitFileStatus file)
        {
            if (file == null || string.IsNullOrEmpty(_currentRepoPath)) return;
            await _gitService.StageFileAsync(_currentRepoPath, file.FilePath);
            await RefreshStatus();
        }

        [RelayCommand]
        private async Task UnstageFile(GitFileStatus file)
        {
            if (file == null || string.IsNullOrEmpty(_currentRepoPath)) return;
            await _gitService.UnstageFileAsync(_currentRepoPath, file.FilePath);
            await RefreshStatus();
        }

        // This method is called when the selected file changes.
        async partial void OnSelectedFileChanged(GitFileStatus value)
        {
            if (value == null || string.IsNullOrEmpty(_currentRepoPath))
            {
                DiffContent = string.Empty;
                return;
            }

            // Load the diff for the selected file.
            DiffContent = await _gitService.GetFileDiffAsync(_currentRepoPath, value.FilePath, value.IsStaged);
        }
    }
}
