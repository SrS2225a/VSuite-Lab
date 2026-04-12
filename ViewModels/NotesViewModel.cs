using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Models;

namespace VSuiteLab.ViewModels;

public partial class NotesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}