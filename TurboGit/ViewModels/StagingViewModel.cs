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
        private DiffModel? _currentDiffModel;
        private bool _isUpdatingSelection = false;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _unstagedFiles;

        [ObservableProperty]
        private ObservableCollection<GitFileStatus> _stagedFiles;

        // Separate selection properties for each list
        [ObservableProperty]
        private GitFileStatus? _selectedUnstagedFile;

        [ObservableProperty]
        private GitFileStatus? _selectedStagedFile;

        // The "active" file driving the diff view
        [ObservableProperty]
        private GitFileStatus? _selectedFile;

        [ObservableProperty]
        private string _diffContent = string.Empty;

        [ObservableProperty]
        private ObservableCollection<DiffLine> _diffLines = new();

        [ObservableProperty]
        private bool _isSelectedFileUnstaged;

        [ObservableProperty]
        private bool _isSelectedFileStaged;

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

        // Called when user clicks a file in the Unstaged list
        partial void OnSelectedUnstagedFileChanged(GitFileStatus? value)
        {
            if (_isUpdatingSelection) return;
            if (value == null) return;

            _isUpdatingSelection = true;
            SelectedStagedFile = null;   // deselect staged list
            _isUpdatingSelection = false;

            SelectedFile = value;
        }

        // Called when user clicks a file in the Staged list
        partial void OnSelectedStagedFileChanged(GitFileStatus? value)
        {
            if (_isUpdatingSelection) return;
            if (value == null) return;

            _isUpdatingSelection = true;
            SelectedUnstagedFile = null;  // deselect unstaged list
            _isUpdatingSelection = false;

            SelectedFile = value;
        }

        // Called when SelectedFile changes — loads the diff
        async partial void OnSelectedFileChanged(GitFileStatus? value)
        {
            if (value == null || string.IsNullOrEmpty(_currentRepoPath))
            {
                DiffContent = string.Empty;
                DiffLines = new ObservableCollection<DiffLine>();
                _currentDiffModel = null;
                IsSelectedFileStaged = false;
                IsSelectedFileUnstaged = false;
                return;
            }

            IsSelectedFileStaged = value.IsStaged;
            IsSelectedFileUnstaged = !value.IsStaged;
            await LoadDiffForFile(value);
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

        [RelayCommand]
        private async Task StageSelectedLines(IList<object> selectedItems)
        {
            if (SelectedFile == null || _currentDiffModel == null || selectedItems == null || selectedItems.Count == 0) return;
            var lines = selectedItems.OfType<DiffLine>().Where(l => l.Type != DiffLineType.Context).ToList();
            if (lines.Count == 0) return;
            var filePath = SelectedFile.FilePath;
            await _gitService.StageLinesAsync(_currentRepoPath, filePath, lines, _currentDiffModel.Hunks);
            await RefreshStatus();
            // Re-select the same file (as unstaged, since we just staged some lines — more may remain)
            var reselected = UnstagedFiles.FirstOrDefault(f => f.FilePath == filePath)
                          ?? StagedFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (reselected != null)
            {
                if (!reselected.IsStaged)
                    SelectedUnstagedFile = reselected;
                else
                    SelectedStagedFile = reselected;
            }
        }

        [RelayCommand]
        private async Task UnstageSelectedLines(IList<object> selectedItems)
        {
            if (SelectedFile == null || _currentDiffModel == null || selectedItems == null || selectedItems.Count == 0) return;
            var lines = selectedItems.OfType<DiffLine>().Where(l => l.Type != DiffLineType.Context).ToList();
            if (lines.Count == 0) return;
            var filePath = SelectedFile.FilePath;
            await _gitService.UnstageLinesAsync(_currentRepoPath, filePath, lines, _currentDiffModel.Hunks);
            await RefreshStatus();
            // Re-select the same file (as staged, since we just unstaged some lines — more may remain)
            var reselected = StagedFiles.FirstOrDefault(f => f.FilePath == filePath)
                          ?? UnstagedFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (reselected != null)
            {
                if (reselected.IsStaged)
                    SelectedStagedFile = reselected;
                else
                    SelectedUnstagedFile = reselected;
            }
        }

        private async Task LoadDiffForFile(GitFileStatus file)
        {
            _currentDiffModel = await _gitService.GetFileDiffModelAsync(_currentRepoPath, file.FilePath, file.IsStaged);
            DiffLines = new ObservableCollection<DiffLine>(_currentDiffModel.AllLines());
        }
    }
}
