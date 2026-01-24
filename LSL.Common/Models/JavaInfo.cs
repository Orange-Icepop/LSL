
using LSL.Common.Utilities;

namespace LSL.Common.Models;

/// <summary>
/// Contains a java executable file's information.
/// </summary>
public record JavaInfo
{
    public string Path { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Vendor { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;

    public JavaInfo(){}
    public JavaInfo(string path, string version, string vendor, string architecture)
    {
        Path = path;
        Version = version;
        Vendor = vendor;
        Architecture = architecture;
    }

    public async Task<bool> Validate()
    {
        if (!File.Exists(Path)) return false;
        return await JavaFinder.GetJavaInfo(Path) is null;
    }
}
