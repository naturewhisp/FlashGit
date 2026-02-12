using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TurboGit.Core.Models;
using TurboGit.Services;
using System.Linq;

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window.
    /// It manages the list of repositories and the active view models for the content area.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IRepositoryService _repositoryService;

        [ObservableProperty]
        private ObservableCollection<LocalRepository> _repositoryList;

        [ObservableProperty]
        private LocalRepository _selectedRepository;

        [ObservableProperty]
        private HistoryViewModel _historyViewModel;

        [ObservableProperty]
        private StagingViewModel _stagingViewModel;

        public MainWindowViewModel() : this(new RepositoryService())
        {
        }

        public MainWindowViewModel(IRepositoryService repositoryService)
        {
            _repositoryService = repositoryService;

            RepositoryList = new ObservableCollection<LocalRepository>();

            // Instantiate the child ViewModels
            HistoryViewModel = new HistoryViewModel();
            StagingViewModel = new StagingViewModel();

            // Load repositories asynchronously
            _ = LoadRepositoriesAsync();
        }

        private async Task LoadRepositoriesAsync()
        {
            var repos = await _repositoryService.GetRepositoriesAsync();

            if (!repos.Any())
            {
                // If no repositories are saved, add the home directory as a default.
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var defaultRepo = new LocalRepository { Name = "Home Directory", Path = userProfile };
                await _repositoryService.AddRepositoryAsync(defaultRepo);
                repos = await _repositoryService.GetRepositoriesAsync();
            }

            RepositoryList.Clear();
            foreach (var repo in repos)
            {
                RepositoryList.Add(repo);
            }
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
