using LSL.Common.Models.AppConfig;
using LSL.Common.Models.ServerConfig;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace LSL.Common.Utilities;

public static class NsJsonOptions
{
    static NsJsonOptions()
    {
        // common
        var types = new[]
        {
            typeof(ServerConfigV2),
            typeof(DesktopConfig),
            typeof(DaemonConfig),
            typeof(WebConfig),
            typeof(ForgeCoreConfigV1),
            typeof(CommonCoreConfigV1),
            typeof(string[]),
            typeof(bool?),
            typeof(uint?),
            typeof(ulong?),
            typeof(int?),
            typeof(long?),
            typeof(float?),
            typeof(double?),
        };
        foreach (var type in types) s_camelResolver.ResolveContract(type);
        DefaultOptions = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            FloatParseHandling = FloatParseHandling.Double,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = s_camelResolver,
            Error = (_, args) => args.ErrorContext.Handled = true,
            Converters = { s_enumConverter }
        };
        // snake
        s_snakeResolver.ResolveContract(typeof(ServerConfigV1));
        SnakeCaseOptions = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            FloatParseHandling = FloatParseHandling.Double,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = s_snakeResolver,
            Error = (_, args) => args.ErrorContext.Handled = true,
            Converters = { s_enumSnakeConverter }
        };
        
    }

    private static readonly StringEnumConverter s_enumConverter = new(new CamelCaseNamingStrategy());
    private static readonly StringEnumConverter s_enumSnakeConverter = new(new SnakeCaseNamingStrategy());
    private static readonly CamelCasePropertyNamesContractResolver s_camelResolver = new();

    private static readonly DefaultContractResolver s_snakeResolver = new()
    {
        NamingStrategy = new SnakeCaseNamingStrategy()
    };
    public static readonly JsonSerializerSettings DefaultOptions;
    public static readonly JsonSerializerSettings SnakeCaseOptions;
}