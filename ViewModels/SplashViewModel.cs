using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.ViewModels;


public partial class SplashViewModel : ObservableObject
{
    [ObservableProperty]
    private string status = "Preparing application...";
}