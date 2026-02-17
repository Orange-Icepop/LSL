using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FluentResults;
using FluentResults.Extensions;
using LSL.Common.Utilities.Minecraft;
using LSL.Common.Validation;
using Mutty;

namespace LSL.Common.Models.ServerConfig;

[MutableGeneration]
public record LocatedServerConfig
{
    internal LocatedServerConfig(string serverPath,
        string serverName,
        ServerCoreType serverType,
        CommonCoreConfigV1? commonInfo,
        ForgeCoreConfigV1? forgeInfo,
        string javaPath,
        uint minMemory,
        uint maxMemory,
        IEnumerable<string> extJvm,
        bool enablePreLaunchProtection)
    {
        ServerPath = serverPath;
        ServerName = serverName;
        ServerType = serverType;
        CommonCoreInfo = commonInfo;
        ForgeCoreInfo = forgeInfo;
        JavaPath = javaPath;
        MinMemory = minMemory;
        MaxMemory = maxMemory;
        ExtraJvmArgs = extJvm.ToImmutableList();
        EnablePreLaunchProtection = enablePreLaunchProtection;
    }

    public string ServerPath { get; init; }
    public string ServerName { get; init; }
    public ServerCoreType ServerType { get; init; }
    public CommonCoreConfigV1? CommonCoreInfo { get; init; }
    public ForgeCoreConfigV1? ForgeCoreInfo { get; init; }
    public string JavaPath { get; init; }
    public uint MinMemory { get; init; }
    public uint MaxMemory { get; init; }
    public ImmutableList<string> ExtraJvmArgs { get; init; }
    public bool EnablePreLaunchProtection { get; init; }

    public static LocatedServerConfig Empty =>
        new(string.Empty, string.Empty, ServerCoreType.Unknown, null, null, string.Empty, 1024, 4096, [], true);

    #region 获取启动信息

    public async Task<Result<ProcessStartInfo>> GetStartInfo()
    {
        if (!await CheckComponents.IsValidJava(JavaPath))
            return Result.Fail<ProcessStartInfo>(new Error($"Java at {JavaPath} is not valid"));
        if (ServerType is not ServerCoreType.Forge && File.Exists(CommonCoreInfo?.JarName))
            return Result.Ok(new ProcessStartInfo
            {
                FileName = JavaPath,
                Arguments =
                    $"-server -Xms{MinMemory}M -Xmx{MaxMemory} {new StringBuilder().AppendJoin(' ', ExtraJvmArgs)} -jar {Path.Combine(ServerPath, CommonCoreInfo.JarName)} nogui",
                WorkingDirectory = ServerPath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = null,
                StandardOutputEncoding = null,
                StandardErrorEncoding = null
            });

        if (ServerType is ServerCoreType.Forge && ForgeCoreInfo is not null)
        {
            if (OperatingSystem.IsWindows() && File.Exists(ForgeCoreInfo.WinLibraryArgsPath))
                return Result.Ok(new ProcessStartInfo
                {
                    FileName = JavaPath,
                    Arguments =
                        $"-server -Xms{MinMemory}M -Xmx{MaxMemory} {new StringBuilder().AppendJoin(' ', ExtraJvmArgs)} @{ForgeCoreInfo.WinLibraryArgsPath} nogui",
                    WorkingDirectory = ServerPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardInputEncoding = null,
                    StandardOutputEncoding = null,
                    StandardErrorEncoding = null
                });
            if (File.Exists(ForgeCoreInfo.UnixLibraryArgsPath))
                return Result.Ok(new ProcessStartInfo
                {
                    FileName = JavaPath,
                    Arguments =
                        $"-server -Xms{MinMemory}M -Xmx{MaxMemory} {new StringBuilder().AppendJoin(' ', ExtraJvmArgs)} @{ForgeCoreInfo.UnixLibraryArgsPath} nogui",
                    WorkingDirectory = ServerPath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardInputEncoding = null,
                    StandardOutputEncoding = null,
                    StandardErrorEncoding = null
                });
        }

        return Result.Fail<ProcessStartInfo>("Server configuration is not valid to create a Minecraft server process");
    }

    #endregion

    #region 创建与转换

    public IndexedServerConfig AsIndexed(int serverId)
    {
        return new IndexedServerConfig(serverId, this);
    }

    public Task<Result<ServerConfigV2>> ToLatestConfig()
    {
        return Validate().Bind(() => Task.FromResult(Result.Ok(new ServerConfigV2
        {
            Name = ServerName,
            ServerType = ServerType,
            CommonCoreInfo = CommonCoreInfo,
            ForgeCoreInfo = ForgeCoreInfo,
            JavaPath = JavaPath,
            MinMemory = MinMemory,
            MaxMemory = MaxMemory,
            ExtraJvmArgs = ExtraJvmArgs,
            EnablePreLaunchProtection = EnablePreLaunchProtection
        })));
    }

    public static Task<Result<LocatedServerConfig>> CreateAsync(string serverPath,
        string? serverName,
        ServerCoreType? serverType,
        CommonCoreConfigV1? commonInfo,
        ForgeCoreConfigV1? forgeInfo,
        string? javaPath,
        uint? minMemory,
        uint? maxMemory,
        IEnumerable<string>? extJvm,
        bool? enablePreLaunchProtection)
    {
        if (string.IsNullOrEmpty(serverName))
            return Task.FromResult(Result.Fail<LocatedServerConfig>("This server doesn't have a name"));
        if (minMemory is null) return Task.FromResult(Result.Fail<LocatedServerConfig>("Minimum memory is missing"));
        if (maxMemory is null) return Task.FromResult(Result.Fail<LocatedServerConfig>("Maximum memory is missing"));
        return new LocatedServerConfig(serverPath, serverName, serverType ?? ServerCoreType.Error, commonInfo,
            forgeInfo,
            javaPath ?? string.Empty, minMemory.Value, maxMemory.Value, extJvm ?? [],
            enablePreLaunchProtection ?? true).CheckAndFixAsync();
    }

    #endregion

    #region 检查与修复

    public Task<Result<LocatedServerConfig>> CheckAndFixAsync(bool skipCoreCheck = false) =>
        this.CreateDraft().CheckAndFixAsync(skipCoreCheck);

    // PS.这样会多造成一次堆分配但是有效减少了代码重复......等之后可以用Interface把Validate方法包出去
    //TODO:合并逻辑
    public Task<Result> Validate(bool skipPathCheck = false) => this.CreateDraft().Validate(skipPathCheck);

    #endregion
}

public partial class MutableLocatedServerConfig : INotifyPropertyChanged
{
    public async Task<Result<LocatedServerConfig>> CheckAndFixAsync(bool skipCoreCheck = false, int tries = 1)
    {
        if (tries > 2)
            return Result.Fail<LocatedServerConfig>("Failed to check server configuration after multiple attempts");

        var validationResult = await Validate(skipCoreCheck);
        if (validationResult.IsFailed) return Result.Fail<LocatedServerConfig>(validationResult.Errors);
        if (skipCoreCheck) return Result.Ok(this.FinishDraft());
        switch (ServerType)
        {
            case ServerCoreType.Forge or ServerCoreType.ForgeInstaller or ServerCoreType.ForgeShim:
            {
                if (ForgeCoreInfo is null || ForgeCoreInfo.Validate(ServerPath).IsFailed)
                {
                    var detectResult = await ForgeConfigHelper.GetForgeConfig(ServerPath);
                    if (detectResult.IsFailed)
                        return Result.Fail<LocatedServerConfig>(
                            "Cannot get the correct core info of the forge server");
                    ForgeCoreInfo = detectResult.Value;
                }

                break;
            }
            case ServerCoreType.Error when CommonCoreInfo is not null && File.Exists(CommonCoreInfo.JarName):
            {
                var detectResult = await CoreTypeHelper.GetCoreType(CommonCoreInfo.JarName);
                if (detectResult.IsFailed) return Result.Fail<LocatedServerConfig>("Cannot get the core type");
                ServerType = detectResult.Value;
                return await CheckAndFixAsync(skipCoreCheck, tries + 1);
            }
            case ServerCoreType.Error when ForgeCoreInfo is not null && ForgeCoreInfo.Validate(ServerPath).IsSuccess:
                ServerType = ServerCoreType.Forge;
                break;
            case ServerCoreType.Error:
                return Result.Fail<LocatedServerConfig>("Neither core file nor forge arguments is valid");
            default:
            {
                if (CommonCoreInfo is null || !File.Exists(CommonCoreInfo.JarName))
                    return Result.Fail<LocatedServerConfig>("The jar info of the server is invalid");
                break;
            }
        }

        return Result.Ok(this.FinishDraft());
    }
    public async Task<Result> Validate(bool skipPathCheck = false)
    {
        List<string> warnings = [];
        if (!skipPathCheck)
        {
            // if server path doesn't exist, or neither CoreInfo is valid, then warn
            if (!Path.Exists(ServerPath) ||
                !(CommonCoreInfo?.Validate(ServerPath).IsSuccess is true ||
                  ForgeCoreInfo?.Validate(ServerPath).IsSuccess is true))
            {
                warnings.Add($"Cannot get the core information of server at {ServerPath}");
            }
        }

        if (MinMemory > MaxMemory) warnings.Add("Minimum memory shouldn't be greater than maximum memory");
        if (!await CheckComponents.IsValidJava(JavaPath)) warnings.Add("The configured Java is not valid");
        warnings.AddRange(from arg in ExtraJvmArgs
            where !CheckComponents.ExtraJvmArg(arg).Passed
            select $"Invalid extra JVM argument {arg}");
        if (warnings.Count != 0) return Result.Fail(new StringBuilder().AppendJoin('\n', warnings).ToString());
        return Result.Ok();
    }
}