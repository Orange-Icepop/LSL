namespace LSL.Common.Models.ServerConfig;

public enum ServerCoreType
{
    Error,
    Unknown,
    Client,
    ForgeInstaller,
    ForgeShim,
    FabricInstaller,
    Forge,
    OldForge,
    Fabric,
    Akarin,
    Arclight,
    CatServer,
    CraftBukkit,
    Leaves,
    LightFall,
    Mohist,
    Paper,
    Vanilla,
    Velocity,
}

public static class ServerCoreTypeExtensions
{
    public static string Explain(this ServerCoreType coreType)
    {
        return coreType switch
        {
            ServerCoreType.Forge => "Forge (>=1.13)",
            ServerCoreType.OldForge => "Forge (<=1.13)",
            _ => coreType.ToString()
        };
    }
}