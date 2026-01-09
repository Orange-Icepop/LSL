using System.Text.Json;
using LSL.Common.Extensions;
using LSL.Common.Models;
using LSL.Common.Validation;

namespace LSL.Common.Utilities.Json;

public delegate void HandleProperty<out T>(JsonElement element, string propertyName, Action<T>? onSuccess = null,
    Action<string>? onFail = null);

public static class JsonPropertyValidationHelper
{
    public static HandleProperty<string> StringHandler(Func<string, string?>? validator = null)
    {
        return (element, propertyName, onSuccess, onFail) =>
        {
            if (!element.TryGetString(out var value))
            {
                onFail?.Invoke($"Property {propertyName} is not a string or is missing");
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

    public static HandleProperty<uint> UIntHandler(
        uint? minValue = null,
        uint? maxValue = null)
    {
        return (element, propertyName, onSuccess, onFail) =>
        {
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

    public static HandleProperty<string> JavaHandler()
    {
        return (element, propertyName, onSuccess, onFail) =>
        {
            if (!element.TryGetString(out var value))
            {
                onFail?.Invoke($"Property {propertyName} is not a string or is missing");
                return;
            }

            if (CheckComponents.IsValidJava(value))
            {
                onFail?.Invoke($"Invalid Java.");
                return;
            }

            onSuccess?.Invoke(value);
        };
    }
}