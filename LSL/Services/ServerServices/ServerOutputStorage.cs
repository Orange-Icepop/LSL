using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LSL.Common.DTOs;
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
    private readonly Channel<IStorageArgs> _storageQueue;
    private readonly CancellationTokenSource _storageCts = new();
    private readonly ILogger<ServerOutputStorage> _logger;
    public ServerOutputStorage(ILogger<ServerOutputStorage> logger)
    {
        _logger = logger;
        _storageQueue = Channel.CreateUnbounded<IStorageArgs>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
        Task.Run(() => ProcessStorage(_storageCts.Token));
        EventBus.Instance.Subscribe<IStorageArgs>(arg => TrySendLine(arg));
        _logger.LogInformation("ServerOutputStorage Launched");
    }

    #region 排队处理
    private bool TrySendLine(IStorageArgs args)
    {
        if (_storageQueue.Writer.TryWrite(args))
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
            await foreach (var args in _storageQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    await StorageProcessor(args);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured in output storage processing.");
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    private Task StorageProcessor(IStorageArgs args)
    {
        return Task.Run(() =>
        {
            switch (args)
            {
                case ColorOutputArgs coa:
                    OutputDict.AddOrUpdate(coa.ServerId, [new ColorOutputLine(coa.Output, coa.ColorHex)],
                        (_, value) =>
                        {
                            value.Add(new ColorOutputLine(coa.Output, coa.ColorHex));
                            return value;
                        });
                    break;
                case ServerStatusArgs ssa:
                    StatusDict.AddOrUpdate(ssa.ServerId, (ssa.IsRunning, ssa.IsOnline), (_, value) =>
                    {
                        value.IsRunning = ssa.IsRunning;
                        value.IsOnline = ssa.IsOnline;
                        return value;
                    });
                    break;
                case PlayerUpdateArgs pua:
                    PlayerDict.AddOrUpdate((pua.ServerId, pua.PlayerName), pua.UUID, (key, value) =>
                    {
                        if (pua.Entering)
                        {
                            return pua.UUID;
                        }
                        else
                        {
                            PlayerDict.TryRemove(key, out _);
                            return string.Empty;
                        }
                    });
                    break;
                case PlayerMessageArgs pma:
                    MessageDict.AddOrUpdate(pma.ServerId, _ => [pma.Message], (_, value) =>
                    {
                        value.Add(pma.Message);
                        return value;
                    });
                    break;
            }
        });
    }
    public void Dispose()
    {
        _storageCts.Cancel();
        _storageQueue.Writer.TryComplete();
        _storageCts.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion

}