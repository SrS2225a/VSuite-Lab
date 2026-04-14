using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.ViewModels;

public partial class NotesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}