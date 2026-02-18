using Avalonia.VisualTree;
using Avalonia.Controls;
using System.Linq;
using System.Collections.Generic;
using TurboGit.ViewModels;
using Avalonia.Markup.Xaml;

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
            var diffItemsControl = this.FindControl<ItemsControl>("DiffItemsControl");

            if (stageBtn != null)
            {
                stageBtn.Click += async (_, _) =>
                {
                    if (DataContext is StagingViewModel vm && diffItemsControl != null)
                    {
                        var selected = new List<object>();
                        foreach (var listBox in diffItemsControl.GetVisualDescendants().OfType<ListBox>())
                        {
                            if (listBox.SelectedItems != null)
                            {
                                selected.AddRange(listBox.SelectedItems.Cast<object>());
                            }
                        }
                        await vm.StageSelectedLinesCommand.ExecuteAsync(selected);
                    }
                };
            }

            if (unstageBtn != null)
            {
                unstageBtn.Click += async (_, _) =>
                {
                    if (DataContext is StagingViewModel vm && diffItemsControl != null)
                    {
                        var selected = new List<object>();
                        foreach (var listBox in diffItemsControl.GetVisualDescendants().OfType<ListBox>())
                        {
                             if (listBox.SelectedItems != null)
                             {
                                 selected.AddRange(listBox.SelectedItems.Cast<object>());
                             }
                        }
                        await vm.UnstageSelectedLinesCommand.ExecuteAsync(selected);
                    }
                };
            }
        }
    }
}
