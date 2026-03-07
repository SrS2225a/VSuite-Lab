using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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

    public async Task SyncAsync(DavConfig config, BannerMessage message)
    {
        string key = GetKey(config);

        if (_runningSyncs.ContainsKey(key))
            return;
        
        var task = RunSyncInteral(config, message);
        _runningSyncs.TryAdd(key, task);
        try
        {
            await task;
        }
        finally
        {
            _runningSyncs.TryRemove(key, out _);
        }
    }

    public async Task SyncAllAsync(BannerMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var accounts = await _databaseService.ReadAllAsync<DavConfig>();
        message.MaxIndex = accounts.Value.Count;
        
        foreach (var account in accounts.Value.Select((value, index) => new { value, index }))
        {
            message.CurrentIndex = account.index + 1;
            message.ServerName = account.value.Name;

            await SyncAsync(account.value, message);
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

    private async Task RunSyncInteral(DavConfig config, BannerMessage message)
    {
        await _syncWorker.ExecuteAsync(config, message, CancellationToken.None);
    }
    
    private string GetKey(DavConfig config) => $"{config.httpUrl}:{config.username}";
}