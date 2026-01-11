using System.Text.Json;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Validation;

namespace LSL.Common.Utilities.Json;

public delegate void HandleJsonProperty(JsonElement root, string propertyName, Action<string>? onFail = null);

public static class JsonPropertyValidationHelper
{
    public static HandleJsonProperty StringHandler(Func<string, string?>? validator = null,
        Action<string>? onSuccess = null, bool enableEmpty = false)
    {
        return (root, propertyName, onFail) =>
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
        };
    }

    public static HandleJsonProperty BoolHandler(Action<bool>? onSuccess = null)
    {
        return (root, propertyName, onFail) =>
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
                    onFail?.Invoke($"Property {propertyName} is not a string");
                    break;
            }
        };
    }

    public static HandleJsonProperty UIntHandler(
        uint? minValue = null,
        uint? maxValue = null, Action<uint>? onSuccess = null)
    {
        return (root, propertyName, onFail) =>
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

            if (value < minValue)
            {
                onFail?.Invoke($"Value must be at least {minValue.Value}");
                return;
            }

            if (value > maxValue)
            {
                onFail?.Invoke($"Value must be at most {maxValue.Value}");
                return;
            }

            onSuccess?.Invoke(value);
        };
    }

    public static HandleJsonProperty StringArrayHandler(Action<string[]>? onSuccess = null, bool ignoreWrongType = true,
        bool ignoreEmpty = false)
    {
        return (root, propertyName, onFail) =>
        {
            if (!root.TryGetProperty(propertyName, out var element))
            {
                onFail?.Invoke($"Property {propertyName} is missing");
                return;
            }
            if (element.ValueKind is not JsonValueKind.Array)
            {
                onFail?.Invoke($"Property {propertyName} is not a Array.");
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
        };
    }

    public static HandleJsonProperty JavaHandler(Action<string>? onSuccess = null)
    {
        return (root, propertyName, onFail) =>
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
        };
    }

    public static HandleJsonProperty FileHandler(Action<string>? onSuccess = null) =>
        StringHandler(validator: s => File.Exists(s) ? null : $"Invalid file path {s}", onSuccess: onSuccess);

    public static HandleJsonProperty EnumHandler<T>(Action<T>? onSuccess = null) where T : struct, Enum
    {
        return (root, propertyName, onFail) =>
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
                onFail?.Invoke("Invalid Enum type");
                return;
            }

            onSuccess?.Invoke(enumValue);
        };
    }
}