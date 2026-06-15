using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WorldPainterUO.App.Converters;

/// <summary>Converts a bool to a highlighted or transparent button background.</summary>
public sealed class BoolToActiveButtonConverter : IValueConverter
{
    public static readonly BoolToActiveButtonConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromArgb(180, 80, 140, 200))
            : new SolidColorBrush(Colors.Transparent);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts an Avalonia Color to a SolidColorBrush.</summary>
public sealed class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Color c ? new SolidColorBrush(c) : new SolidColorBrush(Colors.Gray);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns a highlighted brush when a biome button's name matches the active biome name.
/// ConverterParameter is the active biome name string.
/// </summary>
public sealed class BiomeActiveConverter : IValueConverter
{
    public static readonly BiomeActiveConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name   = value as string;
        var active = parameter as string;
        return name == active
            ? new SolidColorBrush(Color.FromRgb(100, 180, 255))
            : new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts bool to a checkmark string for menu items.</summary>
public sealed class BoolToCheckConverter : IValueConverter
{
    public static readonly BoolToCheckConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "\u2713" : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
