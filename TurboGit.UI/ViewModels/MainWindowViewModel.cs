using System.Collections.ObjectModel;
using ReactiveUI;
using TurboGit.Core.Models;

namespace TurboGit.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<RepositoryInfo> _recentRepos = new();
    private RepositoryInfo? _selectedRepo;

    public MainWindowViewModel()
    {
        // Dummy data for design purposes
        RecentRepos.Add(new RepositoryInfo { Name = "TurboGit Project", Path = "/path/to/turbogit" });
        RecentRepos.Add(new RepositoryInfo { Name = "Avalonia UI", Path = "/path/to/avalonia" });
    }

    public ObservableCollection<RepositoryInfo> RecentRepos
    {
        get => _recentRepos;
        set => this.RaiseAndSetIfChanged(ref _recentRepos, value);
    }

    public RepositoryInfo? SelectedRepo
    {
        get => _selectedRepo;
        set => this.RaiseAndSetIfChanged(ref _selectedRepo, value);
    }
}

// Base ViewModel for ReactiveUI
public class ViewModelBase : ReactiveObject
{
}
