// TurboGit/Views/ConflictResolutionView.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TurboGit.Views
{
    public partial class ConflictResolutionView : Window
    {
        public ConflictResolutionView()
        {
            InitializeComponent();
        }

        // A simple way to close the dialog from the view.
        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
