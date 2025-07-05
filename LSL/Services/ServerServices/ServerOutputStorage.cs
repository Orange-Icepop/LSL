using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace LSL.Services.ServerServices;

/// <summary>
/// A storage in daemon program to provide server information histories for clients.
/// </summary>
public class ServerOutputStorage : IDisposable
{
    public readonly ConcurrentDictionary<int, ObservableCollection<ColorOutputLine>> OutputDict = new();
    public readonly ConcurrentDictionary<int, (bool IsRunning, bool IsOnline)> StatusDict = new();
    public readonly ConcurrentDictionary<(int ServerId, string PlayerName), string> PlayerDict = new();
    public readonly ConcurrentDictionary<int, ObservableCollection<string>> MessageDict = new();
    private readonly Channel<IStorageArgs> StorageQueue;
    private readonly CancellationTokenSource StorageCTS = new();
    private ILogger<ServerOutputStorage> _logger { get; }
    public ServerOutputStorage(ILogger<ServerOutputStorage> logger)
    {
        _logger = logger;
        StorageQueue = Channel.CreateUnbounded<IStorageArgs>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
        Task.Run(() => ProcessStorage(StorageCTS.Token));
        EventBus.Instance.Subscribe<IStorageArgs>(arg => TrySendLine(arg));
        _logger.LogInformation("ServerOutputStorage Launched");
    }

    #region 排队处理
    private bool TrySendLine(IStorageArgs args)
    {
        if (StorageQueue.Writer.TryWrite(args))
        {
            return true;
        }
        else
        {
            _logger.LogError("StorageQueue Writer is full");
            return false;
        }
    }
    private async Task ProcessStorage(CancellationToken ct)
    {
        try
        {
            await foreach (var args in StorageQueue.Reader.ReadAllAsync(ct))
            {
                try { await StorageProcessor(args); }
                catch { }
            }
        }
        catch (OperationCanceledException) { }
    }
    private async Task StorageProcessor(IStorageArgs args)
    {
        switch (args)
        {
            case ColorOutputArgs COA:
                OutputDict.AddOrUpdate(COA.ServerId, [new ColorOutputLine(COA.Output, COA.ColorHex)], (key, value) =>
                {
                    value.Add(new ColorOutputLine(COA.Output, COA.ColorHex));
                    return value;
                });
                break;
            case ServerStatusArgs SSA:
                StatusDict.AddOrUpdate(SSA.ServerId, (SSA.IsRunning, SSA.IsOnline), (key, value) =>
                {
                    value.IsRunning = SSA.IsRunning;
                    value.IsOnline = SSA.IsOnline;
                    return value;
                });
                break;
            case PlayerUpdateArgs PUA:
                PlayerDict.AddOrUpdate((PUA.ServerId, PUA.PlayerName), PUA.UUID, (key, value) =>
                {
                    if (PUA.Entering)
                    {
                        return PUA.UUID;
                    }
                    else
                    {
                        PlayerDict.TryRemove(key, out _);
                        return string.Empty;
                    }
                });
                break;
            case PlayerMessageArgs PMA:
                MessageDict.AddOrUpdate(PMA.ServerId, _ => [PMA.Message], (key, value) =>
                {
                    value.Add(PMA.Message);
                    return value;
                });
                break;
            default:
                break;
        }
    }
    public void Dispose()
    {
        StorageCTS.Cancel();
        StorageQueue.Writer.TryComplete();
        StorageCTS.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion

}