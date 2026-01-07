using System.Text.Json;
using System.Text.Json.Serialization;

namespace LSL.Common.Utilities.Json;

internal class LooseObjectConverter : JsonConverterFactory
{
    private JsonSerializerOptions _defaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { }
    };
    public override bool CanConvert(Type typeToConvert)
    {
        return true;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(LooseConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    private class LooseConverter<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                // 使用默认的JsonSerializer进行反序列化
                return JsonSerializer.Deserialize<T>(ref reader, ConfigSerializerOptions.Base);
            }
            catch (JsonException)
            {
                // 转换失败时返回默认值
                return default;
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, ConfigSerializerOptions.Base);
        }
    }
}