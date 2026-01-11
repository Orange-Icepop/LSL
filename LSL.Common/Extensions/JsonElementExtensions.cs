using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace LSL.Common.Extensions;

public static class JsonElementExtensions
{
    public static bool TryGetString(this JsonElement property, [NotNullWhen(true)]out string? value)
    {
        if (property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return value is not null;
        }
        else
        {
            value = null;
            return false;
        }
    }

    public static bool TryGetNullableString(this JsonElement property, out string? value)
    {
        switch (property.ValueKind)
        {
            case JsonValueKind.String:
                value = property.GetString();
                return value is not null;
            case JsonValueKind.Null:
                value = null;
                return true;
            default:
                value = null;
                return false;
        }
    }
}