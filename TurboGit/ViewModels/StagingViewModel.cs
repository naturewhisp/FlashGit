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
        private ObservableCollection<DiffHunk> _diffHunks = new();

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
                DiffHunks = new ObservableCollection<DiffHunk>();
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
            await StageLinesInternal(lines);
        }

        [RelayCommand]
        private async Task UnstageSelectedLines(IList<object> selectedItems)
        {
            if (SelectedFile == null || _currentDiffModel == null || selectedItems == null || selectedItems.Count == 0) return;
            var lines = selectedItems.OfType<DiffLine>().Where(l => l.Type != DiffLineType.Context).ToList();
            if (lines.Count == 0) return;
            await UnstageLinesInternal(lines);
        }

        [RelayCommand]
        private async Task StageHunk(DiffHunk hunk)
        {
             if (SelectedFile == null || _currentDiffModel == null || hunk == null) return;
             // Stage all non-context lines in the hunk
             var lines = hunk.Lines.Where(l => l.Type != DiffLineType.Context).ToList();
             if (lines.Count == 0) return;
             await StageLinesInternal(lines);
        }

        [RelayCommand]
        private async Task UnstageHunk(DiffHunk hunk)
        {
             if (SelectedFile == null || _currentDiffModel == null || hunk == null) return;
             // Unstage all non-context lines in the hunk
             var lines = hunk.Lines.Where(l => l.Type != DiffLineType.Context).ToList();
             if (lines.Count == 0) return;
             await UnstageLinesInternal(lines);
        }

        private async Task StageLinesInternal(List<DiffLine> lines)
        {
            if (SelectedFile == null || _currentDiffModel == null) return;
            var filePath = SelectedFile.FilePath;
            await _gitService.StageLinesAsync(_currentRepoPath, filePath, lines, _currentDiffModel.Hunks);
            await RefreshStatus();
            ReselectFile(filePath, isStagedOperation: true);
        }

        private async Task UnstageLinesInternal(List<DiffLine> lines)
        {
            if (SelectedFile == null || _currentDiffModel == null) return;
            var filePath = SelectedFile.FilePath;
            await _gitService.UnstageLinesAsync(_currentRepoPath, filePath, lines, _currentDiffModel.Hunks);
            await RefreshStatus();
            ReselectFile(filePath, isStagedOperation: false);
        }

        private void ReselectFile(string filePath, bool isStagedOperation)
        {
             // Try to find the file in either list. 
             // If we just stared lines, prefer unstaged list if it's still there (partial), 
             // otherwise staged list. vice versa for unstaged.
             
             var inUnstaged = UnstagedFiles.FirstOrDefault(f => f.FilePath == filePath);
             var inStaged = StagedFiles.FirstOrDefault(f => f.FilePath == filePath);

             if (isStagedOperation)
             {
                 // We moved things to stage. If partially staged, keep selecting unstaged to allow more staging.
                 if (inUnstaged != null) SelectedUnstagedFile = inUnstaged;
                 else if (inStaged != null) SelectedStagedFile = inStaged;
             }
             else
             {
                 // We moved things to unstage. If partially staged, keep selecting staged to allow more unstaging.
                 if (inStaged != null) SelectedStagedFile = inStaged;
                 else if (inUnstaged != null) SelectedUnstagedFile = inUnstaged;
             }
        }

        private async Task LoadDiffForFile(GitFileStatus file)
        {
            _currentDiffModel = await _gitService.GetFileDiffModelAsync(_currentRepoPath, file.FilePath, file.IsStaged);
            DiffLines = new ObservableCollection<DiffLine>(_currentDiffModel.AllLines());
            DiffHunks = new ObservableCollection<DiffHunk>(_currentDiffModel.Hunks);
        }
    }
}
