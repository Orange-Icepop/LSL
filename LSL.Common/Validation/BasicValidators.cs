using System.Numerics;
using System.Text.RegularExpressions;

namespace LSL.Common.Validation;

public static partial class BasicValidators
{
    public static bool IsInRange<T>(this T value, T min, T max) where T : INumber<T>
    {
        return value >= min && value <= max;
    }
    public static bool IsInExclusiveRange<T>(this T value, T min, T max) where T : INumber<T>
    {
        return value > min && value < max;
    }
    public static bool IsValidRgbHex(this string colorHex)
    {
        return RgbHexRegex().IsMatch(colorHex);
    }
    public static bool IsValidArgbHex(this string colorHex)
    {
        return ArgbHexRegex().IsMatch(colorHex);
    }

    [GeneratedRegex("^#(?:[0-9A-Fa-f]{6})$")]
    private static partial Regex RgbHexRegex();
    [GeneratedRegex("^#(?:[0-9A-Fa-f]{8})$")]
    private static partial Regex ArgbHexRegex();

    public static bool IsValidUri(this string urlString, bool ignoreEmpty = false)
    {
        if (string.IsNullOrEmpty(urlString) && !ignoreEmpty) return false;
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrEmpty(uri.Host)) return false;
        return true;
    }
}