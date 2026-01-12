using System.Text.Json;
using LSL.Common.Validation;

namespace LSL.Common.Extensions;

public static class JsonPropertyExtensions
{
    public static void ParseStringProperty(this JsonElement root, string propertyName,
        Action<string>? onSuccess = null,
        Action<string>? onFail = null,
        Func<string, string?>? validator = null,
        bool enableEmpty = false)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            onFail?.Invoke($"Property {propertyName} is missing");
            return;
        }

        if (!element.TryGetString(out var value))
        {
            onFail?.Invoke($"Property {propertyName} is not a string");
            return;
        }

        if (!enableEmpty && string.IsNullOrWhiteSpace(value))
        {
            onFail?.Invoke($"Property {propertyName} is empty");
            return;
        }

        var validateEx = validator?.Invoke(value);
        if (validateEx != null)
        {
            onFail?.Invoke($"Property {propertyName} validation failed: {validateEx}");
            return;
        }

        onSuccess?.Invoke(value);
    }

    public static void ParseBoolProperty(this JsonElement root, string propertyName,
        Action<bool>? onSuccess = null,
        Action<string>? onFail = null)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            onFail?.Invoke($"Property {propertyName} is missing");
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                onSuccess?.Invoke(true);
                return;
            case JsonValueKind.False:
                onSuccess?.Invoke(false);
                return;
            default:
                onFail?.Invoke($"Property {propertyName} is not a boolean");
                break;
        }
    }

    public static void ParseUIntProperty(this JsonElement root, string propertyName,
        Action<uint>? onSuccess = null,
        Action<string>? onFail = null,
        uint? minValue = null,
        uint? maxValue = null)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            onFail?.Invoke($"Property {propertyName} is missing");
            return;
        }

        if (element.ValueKind is not JsonValueKind.Number ||
            !element.TryGetUInt32(out var value))
        {
            onFail?.Invoke($"Property {propertyName} is not a number");
            return;
        }

        if (minValue.HasValue && value < minValue.Value)
        {
            onFail?.Invoke($"Value must be at least {minValue.Value}");
            return;
        }

        if (maxValue.HasValue && value > maxValue.Value)
        {
            onFail?.Invoke($"Value must be at most {maxValue.Value}");
            return;
        }

        onSuccess?.Invoke(value);
    }

    public static void ParseStringArrayProperty(this JsonElement root, string propertyName,
        Action<string[]>? onSuccess = null,
        Action<string>? onFail = null,
        bool ignoreWrongType = true,
        bool ignoreEmpty = false)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            onFail?.Invoke($"Property {propertyName} is missing");
            return;
        }

        if (element.ValueKind is not JsonValueKind.Array)
        {
            onFail?.Invoke($"Property {propertyName} is not an array");
            return;
        }

        List<string> result = [];
        foreach (var item in element.EnumerateArray())
        {
            if (item.TryGetNullableString(out var value))
            {
                if (ignoreEmpty && string.IsNullOrWhiteSpace(value)) continue;
                result.Add(value ?? string.Empty);
            }
            else if (!ignoreWrongType)
            {
                onFail?.Invoke($"Property {propertyName} has non-string element");
                return;
            }
        }

        onSuccess?.Invoke(result.ToArray());
    }

    public static void ParseJavaProperty(this JsonElement root, string propertyName,
        Action<string>? onSuccess = null,
        Action<string>? onFail = null)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            onFail?.Invoke($"Property {propertyName} is missing");
            return;
        }

        if (!element.TryGetString(out var value))
        {
            onFail?.Invoke($"Property {propertyName} is not a string");
            return;
        }

        if (!CheckComponents.IsValidJava(value))
        {
            onFail?.Invoke("Invalid Java");
            return;
        }

        onSuccess?.Invoke(value);
    }

    public static void ParseFileProperty(this JsonElement root, string propertyName,
        Action<string>? onSuccess = null,
        Action<string>? onFail = null) =>
        root.ParseStringProperty(propertyName, onSuccess, onFail,
            validator: s => File.Exists(s) ? null : $"Invalid file path {s}");

    public static void ParseEnumProperty<T>(this JsonElement root, string propertyName,
        Action<T>? onSuccess = null,
        Action<string>? onFail = null) where T : struct, Enum
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            onFail?.Invoke($"Property {propertyName} is missing");
            return;
        }

        if (!element.TryGetString(out var value))
        {
            onFail?.Invoke($"Property {propertyName} is not a string");
            return;
        }

        if (!Enum.TryParse<T>(value, out var enumValue))
        {
            onFail?.Invoke($"Invalid enum value for type {typeof(T).Name}");
            return;
        }

        onSuccess?.Invoke(enumValue);
    }
}