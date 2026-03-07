using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.ViewModels;

public class SettingsWindowViewModel : ObservableObject
{
    public SyncSettingsViewModel SyncSettings { get; }
    public DavSettingsViewModel DavSettings { get; }
    
    public int SelectedTabIndex { get; set; } = 0;

    public SettingsWindowViewModel(SyncSettingsViewModel sync, DavSettingsViewModel dav) =>
        (SyncSettings, DavSettings) = (sync, dav);
}