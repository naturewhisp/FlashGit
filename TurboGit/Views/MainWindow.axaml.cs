using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using TurboGit.ViewModels;

namespace TurboGit.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.RequestFolderSelection = async () =>
                {
                    if (StorageProvider != null)
                    {
                        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                        {
                            Title = "Select Repository Folder",
                            AllowMultiple = false
                        });

                        var folder = result.FirstOrDefault();
                        return folder?.Path.LocalPath;
                    }
                    return null;
                };
            }
        }
    }
}
