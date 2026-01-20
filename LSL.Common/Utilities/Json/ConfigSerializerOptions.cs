using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace LSL.Common.Utilities.Json;

public static class ConfigSerializerOptions
{
    public static readonly JsonSerializerSettings Default = new()
    {
        Formatting = Formatting.Indented,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        DateParseHandling = DateParseHandling.DateTimeOffset,
        FloatParseHandling = FloatParseHandling.Double,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Error = (_, args) => args.ErrorContext.Handled = true,
        Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) }
    };
}