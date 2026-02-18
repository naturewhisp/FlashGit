using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using TurboGit.Core.Models;

namespace TurboGit.ViewModels
{
    /// <summary>
    /// Converts a <see cref="DiffLineType"/> to a background brush for the diff viewer.
    /// </summary>
    public class DiffLineColorConverter : IValueConverter
    {
        public static readonly DiffLineColorConverter Instance = new();

        private static readonly IBrush AdditionBrush = new SolidColorBrush(Color.FromArgb(60, 0, 200, 80));
        private static readonly IBrush DeletionBrush = new SolidColorBrush(Color.FromArgb(60, 220, 50, 50));
        private static readonly IBrush ContextBrush = Brushes.Transparent;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DiffLineType type)
            {
                return type switch
                {
                    DiffLineType.Addition => AdditionBrush,
                    DiffLineType.Deletion => DeletionBrush,
                    _ => ContextBrush
                };
            }
            return ContextBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
