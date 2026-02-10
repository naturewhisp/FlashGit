// Views/LoginView.axaml.cs

using Avalonia.Controls;
using TurboGit.Services;
using TurboGit.ViewModels;

namespace TurboGit.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            // In a real app with a DI container, you'd inject this.
            // For simplicity, we new it up here.
            DataContext = new LoginViewModel(new GitHubService());
        }
    }
}
