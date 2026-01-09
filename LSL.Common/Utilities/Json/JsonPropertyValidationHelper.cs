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
                onFail?.Invoke($"Property {propertyName} is not a number or is missing");
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
            }

            onSuccess?.Invoke(value);
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
                onFail?.Invoke($"Property {propertyName} is not a string or is missing");
                return;
            }

            if (CheckComponents.IsValidJava(value))
            {
                onFail?.Invoke("Invalid Java.");
                return;
            }

            onSuccess?.Invoke(value);
        };
    }
}