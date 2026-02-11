using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TurboGit.Core.Models;

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window.
    /// It manages the list of repositories and the active view models for the content area.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<LocalRepository> _repositoryList;

        [ObservableProperty]
        private LocalRepository _selectedRepository;

        [ObservableProperty]
        private HistoryViewModel _historyViewModel;

        [ObservableProperty]
        private StagingViewModel _stagingViewModel;

        public MainWindowViewModel()
        {
            // In a real application, this would be loaded from settings.
            // Using Environment.GetFolderPath to provide a valid path structure on Windows.
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            RepositoryList = new ObservableCollection<LocalRepository>
            {
                new LocalRepository { Name = "Home Directory", Path = userProfile }
            };

            // Instantiate the child ViewModels
            HistoryViewModel = new HistoryViewModel();
            StagingViewModel = new StagingViewModel();
        }

        // This method would be called when the selection changes in the UI.
        async partial void OnSelectedRepositoryChanged(LocalRepository value)
        {
            // When a repository is selected, we notify the child ViewModels
            // to load the data for that specific repository.
            // This is a crucial step for the application's reactivity.
            if (value != null)
            {
                try
                {
                    // Await the async loading tasks to ensure they complete and handle exceptions.
                    // Load both History and Staging concurrently for better performance.
                    var loadHistoryTask = HistoryViewModel.LoadCommits(value.Path);
                    var loadChangesTask = StagingViewModel.LoadChanges(value.Path);

                    await Task.WhenAll(loadHistoryTask, loadChangesTask);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading repository: {ex.Message}");
                }
            }
        }
    }
}
