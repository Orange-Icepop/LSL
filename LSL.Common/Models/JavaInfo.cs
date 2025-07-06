namespace LSL.Common.Models;

/// <summary>
/// Contains a java executable file's information.
/// </summary>
public class JavaInfo
{
    public string Path { get; init; }
    public string Version { get; init; }
    public string Vendor { get; init; }
    public string Architecture { get; init; }

    public JavaInfo(string path, string version, string vendor, string architecture)
    {
        Path = path;
        Version = version;
        Vendor = vendor;
        Architecture = architecture;
    }
}
