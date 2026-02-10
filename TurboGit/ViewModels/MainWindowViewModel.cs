// TurboGit/ViewModels/MainWindowViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TurboGit.Models; // Assuming a Repository model exists

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window.
    /// It manages the list of repositories and the active view models for the content area.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Repository> _repositoryList;

        [ObservableProperty]
        private Repository _selectedRepository;

        [ObservableProperty]
        private HistoryViewModel _historyViewModel;

        [ObservableProperty]
        private StagingViewModel _stagingViewModel;

        public MainWindowViewModel()
        {
            // In a real application, this would be loaded from settings.
            RepositoryList = new ObservableCollection<Repository>
            {
                new Repository { Name = "TurboGit Project", Path = "/path/to/turbogit" },
                new Repository { Name = "Avalonia UI", Path = "/path/to/avalonia" }
            };

            // Instantiate the child ViewModels
            HistoryViewModel = new HistoryViewModel();
            StagingViewModel = new StagingViewModel();
        }

        // This method would be called when the selection changes in the UI.
        partial void OnSelectedRepositoryChanged(Repository value)
        {
            // When a repository is selected, we notify the child ViewModels
            // to load the data for that specific repository.
            // This is a crucial step for the application's reactivity.
            HistoryViewModel.LoadCommits(value.Path);
            StagingViewModel.LoadChanges(value.Path);
        }
    }

    // A simple model to represent a repository.
    public class Repository
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}