using System.Text.Json;
using System.Text.Json.Serialization;

namespace LSL.Common.Utilities.Json;

internal class StrictObjectConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return true; // 处理所有类型
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(StrictConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    private class StrictConverter<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                // 使用默认的JsonSerializer进行反序列化
                return JsonSerializer.Deserialize<T>(ref reader, ConfigSerializerOptions.Base);
            }
            catch (JsonException ex)
            {
                // 转换失败时抛出异常
                throw new JsonException($"Strict deserialization failed: {ex.Message}", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, ConfigSerializerOptions.Base);
        }
    }
}