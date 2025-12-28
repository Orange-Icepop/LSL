using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace LSL.Common.Models.ServerConfigs;

public interface IServerConfig
{
    PathedServerConfig Standardize(string path);
    string Serialize();
}

public interface IServerConfig<TConfig> : IServerConfig where TConfig : IServerConfig<TConfig>
{
    static abstract bool TryDeserialize(JsonElement configRoot, bool ignoreWarnings,
        [NotNullWhen(true)] out TConfig? result);
}