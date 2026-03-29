using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Models;

namespace VSuiteLab.ViewModels;

public partial class JournalsViewModel : ViewModelBase, IViewModelSearchableContext
{
    [ObservableProperty]
    private string _searchText = string.Empty;
}