// TurboGit/Controls/VirtualizedCommitGraphControl.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using TurboGit.ViewModels; // Using GitCommit model

namespace TurboGit.Controls
{
    /// <summary>
    /// A high-performance, virtualized control for rendering a Git commit graph.
    /// This control uses low-level drawing commands to render only the visible commits,
    /// ensuring the UI remains responsive even with tens of thousands of commits.
    /// This is a conceptual implementation focusing on the drawing logic.
    /// </summary>
    public class VirtualizedCommitGraphControl : Control
    {
        // Property to hold the collection of commits to be rendered.
        public static readonly StyledProperty<IEnumerable<GitCommit>> ItemsProperty =
            AvaloniaProperty.Register<VirtualizedCommitGraphControl, IEnumerable<GitCommit>>(nameof(Items));

        public IEnumerable<GitCommit> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        static VirtualizedCommitGraphControl()
        {
            // We need to re-render whenever the Items property changes.
            AffectsRender<VirtualizedCommitGraphControl>(ItemsProperty);
        }

        /// <summary>
        /// The core rendering method. This is where the magic happens.
        /// </summary>
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Items == null) return;

            // Define some drawing parameters.
            double rowHeight = 25;
            double dotRadius = 4;
            double graphColumnWidth = 100;

            // Pens and Brushes for drawing the graph.
            var pen = new Pen(Brushes.Gray, 1);
            var dotFill = Brushes.DodgerBlue;
            var textBrush = Brushes.White; // This should be theme-aware in a real app.

            // This is the virtualization part: we only iterate over items
            // that are currently visible within the control's bounds.
            int firstVisibleIndex = Math.Max(0, (int)(Bounds.Top / rowHeight));
            int lastVisibleIndex = Math.Min(((int)(Bounds.Bottom / rowHeight)) + 1, GetItemsCount());

            // Helper to draw a single item
            void DrawItem(GitCommit item, int index)
            {
                double y = (index * rowHeight) + (rowHeight / 2);

                // --- 1. Draw Graph Lines (Conceptual) ---
                // In a real implementation, we'd need to know parent-child relationships
                // to draw the lines correctly. Here, we'll just draw a vertical line for simplicity.
                // The logic would involve mapping SHAs to vertical "lanes".
                double lineX = graphColumnWidth / 2;
                context.DrawLine(pen, new Point(lineX, y - rowHeight / 2), new Point(lineX, y + rowHeight / 2));

                // --- 2. Draw Commit Dot ---
                var dotCenter = new Point(lineX, y);
                context.DrawEllipse(dotFill, pen, dotCenter, dotRadius, dotRadius);

                // --- 3. Draw Commit Message and Author ---
                // Corrected FormattedText constructor for Avalonia 11
                var formattedText = new FormattedText(
                    $"{item.Sha.Substring(0, 7)} - {item.Message} ({item.Author})",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    12,
                    textBrush
                );

                context.DrawText(formattedText, new Point(graphColumnWidth + 10, y - formattedText.Height / 2));
            }

            if (Items is IList<GitCommit> listItems)
            {
                for (int i = firstVisibleIndex; i <= lastVisibleIndex && i < listItems.Count; i++)
                {
                    DrawItem(listItems[i], i);
                }
            }
            else
            {
                int currentIndex = 0;
                foreach (var item in Items)
                {
                    if (currentIndex < firstVisibleIndex)
                    {
                        currentIndex++;
                        continue;
                    }
                    if (currentIndex > lastVisibleIndex) break;

                    DrawItem(item, currentIndex);

                    currentIndex++;
                }
            }
        }

        private int GetItemsCount()
        {
            if (Items is ICollection<GitCommit> collection)
            {
                return collection.Count;
            }
            // Fallback for IEnumerable if it's not a collection
            int count = 0;
            foreach (var _ in Items) count++;
            return count;
        }
    }
}
