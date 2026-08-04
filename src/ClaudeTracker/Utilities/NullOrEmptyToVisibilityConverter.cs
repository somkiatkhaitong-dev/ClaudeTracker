using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ClaudeTracker.Utilities;

/// <summary>Collapsed when the bound string is null/empty, Visible otherwise — used for inline
/// validation messages that should only take up space when there's actually something to say.</summary>
public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
