using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using VSuiteLab.Models;

namespace VSuiteLab.Services;

public class SyncService
{
    private readonly DatabaseService _databaseService;

    public SyncService()
    {
        _databaseService = new DatabaseService();
    }

    
    private readonly ConcurrentDictionary<string, Task> _runningSyncs = new();
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private readonly BaseSyncWorker _syncWorker = new();

    public async Task SyncAsync(
        DavConfig config,
        Action<SyncProgress> onResult)
    {
        string key = GetKey(config);

        if (!_runningSyncs.TryAdd(key, Task.CompletedTask))
            return;

        var progress = new SyncProgress
        {
            Config = config,
            ServerName = config.Name,
            Url = config.httpUrl
        };

        try
        {
            // 🔥 Notify UI immediately with the LIVE object
            onResult?.Invoke(progress);
            await _syncWorker.ExecuteAsync(config, progress, CancellationToken.None);
        }
        finally
        {
            _runningSyncs.TryRemove(key, out _);
        }
    }

    public async Task SyncAllAsync(Action<SyncProgress> onResult)
    {
        var accounts = await _databaseService.ReadAllAsync<DavConfig>();

        foreach (var account in accounts.Value)
        {
            await SyncAsync(account, onResult);
        }
    }

    // public void StatPerodic(TimeSpan interval)
    // {
    //     if(_timer != null)
    //         return;
    //
    //     _cts = new CancellationTokenSource();
    //     _timer = new PeriodicTimer(interval);
    //     
    //     _ = Task.Run(async () =>
    //     {
    //         while (await _timer.WaitForNextTickAsync(_cts.Token))
    //         {
    //             await SyncAllAsync(TODO);
    //         }
    //     });
    // }
    
    public void StopPeriodic()
    {
        _cts?.Cancel();
        _timer = null;
    }
    
    private string GetKey(DavConfig config) => $"{config.httpUrl}:{config.username}";
}