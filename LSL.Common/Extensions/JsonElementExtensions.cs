using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace LSL.Common.Extensions;

[Obsolete]
public static class JsonElementExtensions
{
    extension(JsonElement property)
    {
        public bool TryGetString([NotNullWhen(true)] out string? value)
        {
            if (property.ValueKind == JsonValueKind.String)
            {
                value = property.GetString();
                return value is not null;
            }

            value = null;
            return false;
        }

        public bool TryGetNullableString(out string? value)
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
}