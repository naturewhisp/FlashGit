using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TurboGit.Core.Models;
using TurboGit.Services;
using System.Linq;
using System.Collections.Generic;
using System.IO;

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
        private LocalRepository? _selectedRepository;

        [ObservableProperty]
        private HistoryViewModel _historyViewModel;

        [ObservableProperty]
        private StagingViewModel _stagingViewModel;

        /// <summary>
        /// Delegate to request folder selection from the View.
        /// </summary>
        public Func<Task<string?>>? RequestFolderSelection { get; set; }

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

        [RelayCommand]
        private async Task AddRepository()
        {
            if (RequestFolderSelection != null)
            {
                try
                {
                    var path = await RequestFolderSelection();
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Use the folder name as the repository name
                        var name = new DirectoryInfo(path).Name;
                        var newRepo = new LocalRepository { Name = name, Path = path };

                        await _repositoryService.AddRepositoryAsync(newRepo);
                        RepositoryList.Add(newRepo);
                        SelectedRepository = newRepo;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to add repository: {ex.Message}");
                }
            }
        }

        private async Task LoadRepositoriesAsync()
        {
            try
            {
                var repos = (await _repositoryService.GetRepositoriesAsync()).ToList();

                if (!repos.Any())
                {
                    // If no repositories are saved, add the home directory as a default.
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var defaultRepo = new LocalRepository { Name = "Home Directory", Path = userProfile };

                    // Add to service (persist)
                    await _repositoryService.AddRepositoryAsync(defaultRepo);

                    // Add to local list directly to avoid re-fetching
                    repos.Add(defaultRepo);
                }

                RepositoryList.Clear();
                foreach (var repo in repos)
                {
                    RepositoryList.Add(repo);
                }
            }
            catch (Exception ex)
            {
                // In a real app, this would be a user-facing error message (Toast, Dialog, etc.)
                Console.WriteLine($"Failed to load repositories: {ex.Message}");
            }
        }

        // This method would be called when the selection changes in the UI.
        async partial void OnSelectedRepositoryChanged(LocalRepository? value)
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
