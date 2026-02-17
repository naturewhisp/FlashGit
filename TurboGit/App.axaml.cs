// App.axaml.cs (Modified)

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TurboGit.ViewModels;
using TurboGit.Views;

namespace TurboGit;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new Services.RepositoryService(), new Infrastructure.Security.TokenManager())
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
