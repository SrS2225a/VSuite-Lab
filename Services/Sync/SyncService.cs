using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using VSuiteLab.Models;

namespace VSuiteLab.Services;

public class SyncService
{
    private Settings _settings;
    private readonly DatabaseService _databaseService;
    private readonly AlarmService _alarmService;

    public SyncService()
    {
        _databaseService = new DatabaseService();
        _alarmService = new AlarmService();
        
        _ = _alarmService.InitializeAsync();
    }
    
    private readonly ConcurrentDictionary<string, Task> _runningSyncs = new();
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private readonly BaseSyncWorker _syncWorker = new();

    public async Task SyncAsync(
        DavConfig config,
        Action<SyncProgress> onResult, int CurrentIndex = 0, int MaxIndex = 0)
    {
        string key = GetKey(config);

        if (!_runningSyncs.TryAdd(key, Task.CompletedTask))
            return;

        var progress = new SyncProgress
        {
            Config = config,
            ServerName = config.Name,
            Url = config.httpUrl,
            CurrentIndex = CurrentIndex + 1,
            MaxIndex = MaxIndex
        };

        try
        {
            onResult?.Invoke(progress);
            await _syncWorker.ExecuteAsync(config, progress, CancellationToken.None);
        }
        finally
        {
            if (progress.IsCompleted)
            {
                WeakReferenceMessenger.Default.Send(new SyncCompletedMessage(config));


                var alarmsResult =
                    await _databaseService.ReadAllAsync<CalDavAlarm>(
                        q => q.Where(a => a.SelectedDate != null && !a.HasRan), true);
                await _alarmService.SyncAlarmsAsync(alarmsResult.Value);
            }

            _runningSyncs.TryRemove(key, out _);
        }
    }

    public async Task SyncAllAsync(Action<SyncProgress> onResult)
    {
        var accounts = await _databaseService.ReadAllAsync<DavConfig>();
        foreach (var account in accounts.Value.Select((value, index) => new { value, index }))
        {
            await SyncAsync(account.value, onResult, account.index, accounts.Value.Count);
        }
    }

    public async Task StatPerodic(Action<SyncProgress> onResult)
    {
        if (_timer == null)
        {
            _settings = await GetSettingsAsync();

            if (_settings.SyncOnChange)
            {
                await SyncAllAsync(onResult);
            }

            var interval = TimeSpan.FromSeconds(_settings.SyncAuto);
            
            _cts = new CancellationTokenSource(); 
            _timer = new PeriodicTimer(interval);
            
            _ = Task.Run(async () =>
            {
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    await SyncAllAsync(onResult);
                }
            });
        }
    }
    
    public void StopPeriodic()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _timer = null;
    }

    private async Task<Settings> GetSettingsAsync()
    {
        var result = await _databaseService.ReadAllAsync<Settings>();
        return result.Value.FirstOrDefault() ?? new Settings();
    }
    
    private string GetKey(DavConfig config) => $"{config.httpUrl}:{config.username}";
}