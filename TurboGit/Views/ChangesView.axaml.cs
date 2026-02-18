using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TurboGit.ViewModels;

namespace TurboGit.Views
{
    public partial class ChangesView : UserControl
    {
        public ChangesView()
        {
            InitializeComponent();
            WireUpButtons();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void WireUpButtons()
        {
            var stageBtn = this.FindControl<Button>("StageSelectedBtn");
            var unstageBtn = this.FindControl<Button>("UnstageSelectedBtn");
            var diffListBox = this.FindControl<ListBox>("DiffListBox");

            if (stageBtn != null && diffListBox != null)
            {
                stageBtn.Click += async (_, _) =>
                {
                    if (DataContext is StagingViewModel vm)
                    {
                        var selected = diffListBox.SelectedItems?.Cast<object>().ToList() ?? new List<object>();
                        await vm.StageSelectedLinesCommand.ExecuteAsync(selected);
                    }
                };
            }

            if (unstageBtn != null && diffListBox != null)
            {
                unstageBtn.Click += async (_, _) =>
                {
                    if (DataContext is StagingViewModel vm)
                    {
                        var selected = diffListBox.SelectedItems?.Cast<object>().ToList() ?? new List<object>();
                        await vm.UnstageSelectedLinesCommand.ExecuteAsync(selected);
                    }
                };
            }
        }
    }
}
