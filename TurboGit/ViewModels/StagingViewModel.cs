// TurboGit/ViewModels/StagingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TurboGit.Services;

namespace TurboGit.ViewModels
{
    public partial class StagingViewModel : ObservableObject
    {
        private readonly IGitService _gitService;
        private string _currentRepoPath;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _unstagedFiles;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _stagedFiles;
        
        [ObservableProperty]
        private GitFileStatus _selectedFile;

        [ObservableProperty]
        private string _diffContent;

        public StagingViewModel()
        {
            _gitService = new GitService();
            UnstagedFiles = new ObservableCollection<GitFileStatus>();
            StagedFiles = new ObservableCollection<GitFileStatus>();
        }

        public async void LoadChanges(string repoPath)
        {
            _currentRepoPath = repoPath;
            if (string.IsNullOrEmpty(_currentRepoPath)) return;

            await RefreshStatus();
        }

        private async Task RefreshStatus()
        {
            UnstagedFiles.Clear();
            StagedFiles.Clear();

            var statuses = await _gitService.GetFileStatusAsync(_currentRepoPath);
            foreach (var status in statuses)
            {
                if (status.IsStaged)
                    StagedFiles.Add(status);
                else
                    UnstagedFiles.Add(status);
            }
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