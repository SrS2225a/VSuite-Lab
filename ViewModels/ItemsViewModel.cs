using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VSuiteLab.Models;
using VSuiteLab.Services;
using VSuiteLab.ViewModels;

public abstract partial class CalDavItemViewModel<T> : ViewModelBase where T : CalDavItem, new()
{
    protected readonly DatabaseService _databaseService = new();

    public ObservableCollection<T> Items { get; } = new();
    public ObservableCollection<DavConfig> DavInstances { get; } = new();

    [ObservableProperty] private DavConfig? selectedDavInstance;
    [ObservableProperty] private T? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;

    public CalDavItemViewModel()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var instances = await _databaseService.ReadAllAsync<DavConfig>();
        foreach (var instance in instances.Value)
            DavInstances.Add(instance);

        var items = await _databaseService.ReadAllAsync<T>();
        foreach (var item in items.Value)
            Items.Add(item);

        SelectedItem = new T();
    }

    [RelayCommand]
    public virtual async Task SaveItem()
    {
        if (SelectedItem == null || SelectedDavInstance == null) return;
        SelectedItem.IsDirty = true;
        SelectedItem.DavConfigId = SelectedDavInstance.Id;

        await _databaseService.UpdateAsync(SelectedItem);
        SelectedItem = new T();
    }

    [RelayCommand]
    public virtual async Task DeleteItem()
    {
        if (SelectedItem == null) return;
        SelectedItem.IsDeleted = true;
        SelectedItem.IsDirty = true;
        await _databaseService.UpdateAsync(SelectedItem);

        Items.Remove(SelectedItem);
        SelectedItem = new T();
    }
}