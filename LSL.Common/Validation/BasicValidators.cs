using System.Numerics;
using System.Text.RegularExpressions;

namespace LSL.Common.Validation;

public static partial class BasicValidators
{
    extension<T>(T value) where T : INumber<T>
    {
        public bool IsInRange(T min, T max)
        {
            return value >= min && value <= max;
        }

        public bool IsInExclusiveRange(T min, T max)
        {
            return value > min && value < max;
        }
    }

    extension(string colorHex)
    {
        public bool IsValidRgbHex()
        {
            return RgbHexRegex().IsMatch(colorHex);
        }

        public bool IsValidArgbHex()
        {
            return ArgbHexRegex().IsMatch(colorHex);
        }
    }

    [GeneratedRegex("^#(?:[0-9A-Fa-f]{6})$")]
    private static partial Regex RgbHexRegex();

    [GeneratedRegex("^#(?:[0-9A-Fa-f]{8})$")]
    private static partial Regex ArgbHexRegex();

    public static bool IsValidUri(this string urlString, bool ignoreEmpty = false)
    {
        if (string.IsNullOrEmpty(urlString))
        {
            return ignoreEmpty;
        }
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrEmpty(uri.Host)) return false;
        return true;
    }
}