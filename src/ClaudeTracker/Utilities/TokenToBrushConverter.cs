using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ClaudeTracker.Utilities;

/// <summary>Converts a theme color token name (e.g. "AccentBlue") into a pastel body brush,
/// or a darker outline/feet brush with ConverterParameter="dark".</summary>
public class TokenToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var token = value as string ?? "AccentBlue";
        var baseColor = ThemeColors.Get(token);

        var color = (parameter as string) == "dark"
            ? Darken(baseColor, 0.30)
            : Pastelize(baseColor);

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Color Pastelize(Color c)
    {
        // Lerp toward white — dark-theme accents are already soft, so lighten less
        var amount = ThemeColors.IsDark ? 0.20 : 0.40;
        return Lerp(c, Colors.White, amount);
    }

    private static Color Darken(Color c, double amount) => Lerp(c, Colors.Black, amount);

    private static Color Lerp(Color from, Color to, double t)
    {
        return Color.FromRgb(
            (byte)(from.R + (to.R - from.R) * t),
            (byte)(from.G + (to.G - from.G) * t),
            (byte)(from.B + (to.B - from.B) * t));
    }
}
