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
}