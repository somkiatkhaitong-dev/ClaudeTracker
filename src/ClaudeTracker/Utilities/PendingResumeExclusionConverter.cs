using System.Globalization;
using System.Windows.Data;
using ClaudeTracker.Models;

namespace ClaudeTracker.Utilities;

/// <summary>Explains, for one PENDING row, why a captured session won't auto-resume — either the
/// user disabled that specific item, or its project isn't on a non-empty allowlist. Empty string
/// (nothing shown) when the item is eligible.</summary>
public class PendingResumeExclusionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not PendingResume pending) return string.Empty;

        if (!pending.IsEnabled) return "Manually disabled";

        var allowed = (values[1] as IEnumerable<string>)?.ToList() ?? new List<string>();
        if (!WatchdogLogic.IsProjectAllowed(pending.Cwd, allowed)) return "Not in allowed projects";

        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
