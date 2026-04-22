using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VSuiteLab.Models;
using VSuiteLab.Models.Helpers;
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
    public StatusResponse<string>? StatusResponse
    {
        get => _statusResponse;
        set => SetProperty(ref _statusResponse, value);
    }

    public ICommand AddCommand { get; set; }
    public ICommand UpdateCommand { get; set; }
    public ICommand RemoveCommand { get; set; }

    private readonly CalDavService _davService = new();

    public DavSettingsViewModel()
    {
        _databaseService = new DatabaseService();
        
        DavConfigs = new ObservableCollection<DavConfig>();

        SelectedDavConfig = new DavConfig();
        
        AddCommand = new AsyncRelayCommand(AddAsync);
        UpdateCommand = new AsyncRelayCommand(UpdateAsync);
        RemoveCommand = new AsyncRelayCommand(RemoveAsync);
        
        _ = LoadAsync();
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
        if(string.IsNullOrEmpty(SelectedDavConfig?.httpUrl) || string.IsNullOrEmpty(SelectedDavConfig.Name))
            return;

        
        IsAddSaving = true;
        
        StatusResponse = await _davService.addMount(SelectedDavConfig);
        if (StatusResponse.Success)
        {
            DavConfigs.Add(SelectedDavConfig);
            SelectedDavConfig = new DavConfig();
            StatusResponse = new();
            
            WeakReferenceMessenger.Default.Send(new DavConfigChangedMessage(SelectedDavConfig));
        }
        
        IsAddSaving = false;
    }

    private async Task UpdateAsync()
    {
        if(string.IsNullOrEmpty(SelectedDavConfig?.httpUrl) || string.IsNullOrEmpty(SelectedDavConfig.Name))
            return;
        
        IsUpdateSaving = true;
        
        var davClient = DavMiddlewareService.GetDavClient(SelectedDavConfig);
        var hasCalDav = await DavMiddlewareService.HasCalDav(davClient, SelectedDavConfig);
        if (hasCalDav.Success)
        {
            await _databaseService.UpdateAsync( SelectedDavConfig);

            // update list
            var found = DavConfigs.FirstOrDefault(c => c.Id == SelectedDavConfig.Id);
            if (found != null)
            {
                DavConfigs.Add(SelectedDavConfig);
                DavConfigs.Remove(found);
                
                WeakReferenceMessenger.Default.Send(new DavConfigChangedMessage(SelectedDavConfig));
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
        if(string.IsNullOrEmpty(SelectedDavConfig?.httpUrl) || string.IsNullOrEmpty(SelectedDavConfig.Name))
            return;

        
        await _databaseService.DeleteAsync(SelectedDavConfig);
        
        DavConfigs.Remove(SelectedDavConfig);
        
        WeakReferenceMessenger.Default.Send(new DavConfigChangedMessage(SelectedDavConfig));
        
        SelectedDavConfig = new DavConfig();
        StatusResponse = new();
    }
}