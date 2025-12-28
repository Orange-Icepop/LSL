using Newtonsoft.Json;

namespace LSL.Common.Models.ServerConfigs;

public class PathedServerConfig
{
    
    public string ServerPath { get; set; }
    public string ServerName { get; set; }
    public string UsingJava { get; set; }
    public string CoreName { get; set; }
    public uint MinMemory { get; set; }
    public uint MaxMemory { get; set; }
    public string ExtJvm { get; set; }

    public PathedServerConfig(string serverPath, string serverName, string usingJava, string coreName, uint minMemory, uint maxMemory, string extJvm)
    {
        ServerPath = serverPath;
        ServerName = serverName;
        UsingJava = usingJava;
        CoreName = coreName;
        MinMemory = minMemory;
        MaxMemory = maxMemory;
        ExtJvm = extJvm;
    }
    
    public static PathedServerConfig Empty => new(string.Empty, string.Empty,string.Empty,string.Empty,1024,4096,string.Empty);

    public IndexedServerConfig ToServerConfig(int serverId) => new(serverId, this);
}