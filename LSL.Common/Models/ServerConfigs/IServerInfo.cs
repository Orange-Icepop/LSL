namespace LSL.Common.Models.ServerConfigs;

public interface IServerInfo
{
    string ServerPath { get; }
    string ServerName { get; }
    string UsingJavaPath { get; }
    string CoreName { get; }
    uint MinMemory { get; }
    uint MaxMemory { get; }
    string ExtJvm { get; }
}