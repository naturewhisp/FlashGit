using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using TurboGit.Core.Models;

namespace TurboGit.ViewModels;

public class MainViewModel : ViewModelBase
{
    private LocalRepository? _selectedRepository;

    // A collection to hold the repositories listed in the sidebar.
    public ObservableCollection<LocalRepository> Repositories { get; }

    // Command to handle adding a new repository.
    // In a real app, this would open a file dialog.
    public ReactiveCommand<Unit, Unit> AddRepositoryCommand { get; }

    // The currently selected repository in the list.
    public LocalRepository? SelectedRepository
    {
        get => _selectedRepository;
        set => this.RaiseAndSetIfChanged(ref _selectedRepository, value);
    }

    // A helper property to determine if a repository is selected.
    public bool IsRepositorySelected => SelectedRepository != null;

    public MainViewModel()
    {
        Repositories = new ObservableCollection<LocalRepository>();

        // Initialize the command. For now, it just adds a dummy repository.
        AddRepositoryCommand = ReactiveCommand.Create(AddRepository);

        // For demonstration, let's add some sample data.
        LoadRepositories();
    }

    private void AddRepository()
    {
        // TODO: Implement US1 - Open a folder browser to select a repository.
        // For now, add a placeholder.
        var newRepo = new LocalRepository
        {
            Name = "New Dummy Repo",
            Path = "/path/to/new/repo"
        };
        Repositories.Add(newRepo);
    }

    private void LoadRepositories()
    {
        // TODO: Implement US1 - Load saved repositories from config.
        Repositories.Add(new LocalRepository { Name = "TurboGit", Path = "C:/dev/TurboGit" });
        Repositories.Add(new LocalRepository { Name = "AvaloniaUI", Path = "C:/dev/AvaloniaUI" });
    }
}