using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TurboGit.Core.Models;
using TurboGit.Services;
using TurboGit.Infrastructure.Security;
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
        private readonly ITokenManager _tokenManager;

        [ObservableProperty]
        private ObservableCollection<LocalRepository>? _repositoryList;

        [ObservableProperty]
        private LocalRepository? _selectedRepository;

        [ObservableProperty]
        private HistoryViewModel? _historyViewModel;

        [ObservableProperty]
        private StagingViewModel? _stagingViewModel;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoggedIn))]
        private bool _isLoggedIn;

        public bool IsNotLoggedIn => !IsLoggedIn;

        [ObservableProperty]
        private LoginViewModel? _loginViewModel;

        /// <summary>
        /// Delegate to request folder selection from the View.
        /// </summary>
        public Func<Task<string?>>? RequestFolderSelection { get; set; }

        public MainWindowViewModel() : this(new RepositoryService(), new TokenManager())
        {
        }

        public MainWindowViewModel(IRepositoryService repositoryService, ITokenManager tokenManager)
        {
            _repositoryService = repositoryService;
            _tokenManager = tokenManager;

            var token = _tokenManager.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                IsLoggedIn = true;
                InitializeDashboard();
            }
            else
            {
                IsLoggedIn = false;
                LoginViewModel = new LoginViewModel(new GitHubService(), _tokenManager, OnLoginSuccess);
            }
        }

        private void OnLoginSuccess()
        {
            IsLoggedIn = true;
            InitializeDashboard();
        }

        private void InitializeDashboard()
        {
            RepositoryList = new ObservableCollection<LocalRepository>();

            // Instantiate the child ViewModels
            HistoryViewModel = new HistoryViewModel();
            StagingViewModel = new StagingViewModel();

            _ = LoadRepositoriesAsync();
        }

        [ObservableProperty]
        private bool _isDashboardActive = true;

        [RelayCommand]
        private void GoToDashboard()
        {
            SelectedRepository = null;
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
                        RepositoryList?.Add(newRepo);
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

                // Remove legacy "Home Directory" if present
                var homeDir = repos.FirstOrDefault(r => r.Name == "Home Directory");
                if (homeDir != null)
                {
                   await _repositoryService.RemoveRepositoryAsync(homeDir);
                   repos.Remove(homeDir);
                }

                RepositoryList?.Clear();
                foreach (var repo in repos)
                {
                    RepositoryList?.Add(repo);
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
            if (value == null)
            {
                IsDashboardActive = true;
                return;
            }

            IsDashboardActive = false;
            try
            {
                if (HistoryViewModel == null || StagingViewModel == null)
                    return;

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
