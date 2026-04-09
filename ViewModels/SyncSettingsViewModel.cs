using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Models;
using VSuiteLab.Services;
using VSuiteLab.Converters;

namespace VSuiteLab.ViewModels;

public class SyncSettingsViewModel : ObservableObject
{
    private Settings _settings = new();
    private double _syncIntervalDisplay;
    private readonly DatabaseService _databaseService;
    private TimeConverter.TimeUnit _selectedUnit = TimeConverter.TimeUnit.Minutes;
    public IEnumerable<EnumOption<ConflictStrategy>> ConflictStrategies =>
        new[]
        {
            new EnumOption<ConflictStrategy>(ConflictStrategy.PreferServer, "Prefer server"),
            new EnumOption<ConflictStrategy>(ConflictStrategy.PreferClient, "Prefer local"),
            new EnumOption<ConflictStrategy>(ConflictStrategy.Ignore, "Let the application decide"),
        };
    public Array TimeUnits => Enum.GetValues(typeof(TimeConverter.TimeUnit));

    public SyncSettingsViewModel()
    {
        _databaseService = new DatabaseService();
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        var result = await _databaseService.ReadAllAsync<Settings>();

        _settings = result.Success && result.Value?.Count > 0
            ? result.Value.First()
            : await CreateDefaultSettingsAsync();

        _selectedUnit = _settings.SyncAuto >= 3600 ? TimeConverter.TimeUnit.Hours : TimeConverter.TimeUnit.Minutes;
        _syncIntervalDisplay = TimeConverter.ConvertToDisplay(_settings.SyncAuto, _selectedUnit);

        OnPropertyChanged(string.Empty); 
    }
    
    private async Task<Settings> CreateDefaultSettingsAsync()
    {
        var settings = new Settings();
        await _databaseService.CreateAsync(settings);
        return settings;
    }
    
    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == string.Empty) return;
        
        await _databaseService.UpdateAsync(_settings);
    }

    /* ---------------------------
     * Auto sync enable/disable
     * --------------------------- */

    public bool IsAutoSyncEnabled
    {
        get => _settings.SyncAuto > 0;
        set
        {
            if (value)
            {
                if (_settings.SyncAuto <= 0)
                    _settings.SyncAuto = 600; // default 10 min
            }
            else
            {
                _settings.SyncAuto = 0;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(SyncIntervalDisplay));
        }
    }

    public double SyncIntervalDisplay
    {
        get => _syncIntervalDisplay;
        set
        {
            if (SetProperty(ref _syncIntervalDisplay, value))
            {
                _settings.SyncAuto = TimeConverter.ConvertToSeconds(value, SelectedUnit);
                OnPropertyChanged();
            }
        }
    }

    public TimeConverter.TimeUnit SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value))
            {
                OnPropertyChanged(nameof(SyncIntervalDisplay));
            }
        }
    }

    /* ---------------------------
     * Immediate sync
     * --------------------------- */

    public bool SyncOnChange
    {
        get => _settings.SyncOnChange;
        set
        {
            _settings.SyncOnChange = value;
            OnPropertyChanged();
        }
    }

    public bool DebugEnabled
    {
        get => _settings.DebugEnabled ?? false;
        set
        {
            _settings.DebugEnabled = value;
            OnPropertyChanged();
        }
    }
    
    public ConflictStrategy SelectedConflictStrategy
    {
        get => _settings.ConflictStrategy;
        set
        {
            if (_settings.ConflictStrategy != value)
            {
                _settings.ConflictStrategy = value;
                OnPropertyChanged();
            }
        }
    }
}