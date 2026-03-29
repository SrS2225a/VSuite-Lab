using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Models;

namespace VSuiteLab.ViewModels;

public partial class NotesViewModel : ViewModelBase, IViewModelSearchableContext
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}