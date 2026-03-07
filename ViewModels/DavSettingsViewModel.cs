using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VSuiteLab.Models;
using VSuiteLab.Services;

namespace VSuiteLab.ViewModels;

public partial class DavSettingsViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    
    [ObservableProperty] private bool _isAddSaving;
    [ObservableProperty] private bool _isUpdateSaving;
    
    public ObservableCollection<DavConfig> DavConfigs { get; set; }
    private DavConfig _selectedDavConfig;
    public DavConfig SelectedDavConfig
    {
        get => _selectedDavConfig;
        set => SetProperty(ref _selectedDavConfig, value);
    }

    private StatusResponse<string>? _statusResponse;
    public StatusResponse<string> StatusResponse
    {
        get => _statusResponse;
        set => SetProperty(ref _statusResponse, value);
    }

    public ICommand AddCommand { get; set; }
    public ICommand UpdateCommand { get; set; }
    public ICommand RemoveCommand { get; set; }

    private readonly CalDAVService _davService = new();

    public DavSettingsViewModel()
    {
        _databaseService = new DatabaseService();
        
        DavConfigs = new ObservableCollection<DavConfig>();

        SelectedDavConfig = new DavConfig();
        
        AddCommand = new AsyncRelayCommand(AddAsync);
        UpdateCommand = new AsyncRelayCommand(UpdateAsync);
        RemoveCommand = new AsyncRelayCommand(RemoveAsync);
        
        LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _databaseService.ReadAllAsync<DavConfig>();
        if(!result.Success || result.Value == null)
            return;
        
        DavConfigs.Clear();
        foreach(var davConfig in result.Value)
        {
            DavConfigs.Add(davConfig);
        }
    }

    private async Task AddAsync()
    {
        if (SelectedDavConfig == null)
            return;
        
        IsAddSaving = true;
        
        StatusResponse = await _davService.addMount(SelectedDavConfig);
        if (StatusResponse.Success)
        {
            DavConfigs.Add(SelectedDavConfig);
            SelectedDavConfig = new DavConfig();
            StatusResponse = new();
        }
        
        IsAddSaving = false;
    }

    private async Task UpdateAsync()
    {
        if(SelectedDavConfig == null)
            return;
        
        IsUpdateSaving = true;
        
        var davClient = DavMiddlewareService.getDavClient(SelectedDavConfig);
        var hasCalDav = await DavMiddlewareService.hasCalDav(davClient, SelectedDavConfig);
        if (hasCalDav.Success)
        {
            await _databaseService.UpdateAsync( SelectedDavConfig);

            // update list
            var found = DavConfigs.FirstOrDefault(c => c.Id == SelectedDavConfig.Id);
            if (found != null)
            {
                DavConfigs.Add(SelectedDavConfig);
                DavConfigs.Remove(found);
            }
        }
        else
        {
            StatusResponse = hasCalDav;
        }
        
        IsUpdateSaving = false;
    }

    private async Task RemoveAsync()
    {
        if(SelectedDavConfig == null)
            return;
        
        await _databaseService.DeleteAsync(SelectedDavConfig);
        
        DavConfigs.Remove(SelectedDavConfig);
        
        SelectedDavConfig = new DavConfig();
        StatusResponse = new();
    }
}