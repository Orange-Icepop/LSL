namespace LSL.Common.Models;
/// <summary>
/// A record of unvalidated or unregistered server's config.
/// </summary>
/// <param name="serverName">The server's name.</param>
/// <param name="corePath">The server's core file path. Meaningless in editing config.</param>
/// <param name="minMem">The minimum JVM allocated RAM.</param>
/// <param name="maxMem">The maximum JVM allocated RAM.</param>
/// <param name="javaPath">The executable java file path.</param>
/// <param name="extJvm">The extent JVM parameters.</param>
public class FormedServerConfig(
    string serverName,
    string corePath,
    string minMem,
    string maxMem,
    string javaPath,
    string extJvm)
{
    public string ServerName => serverName;
    public string CorePath => corePath;
    public string MinMem => minMem;
    public string MaxMem => maxMem;
    public string JavaPath => javaPath;
    public string ExtJvm => extJvm;
}