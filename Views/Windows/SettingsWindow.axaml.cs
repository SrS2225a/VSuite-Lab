using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VSuiteLab.ViewModels;

namespace VSuiteLab.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new SettingsWindowViewModel(
            new SyncSettingsViewModel(),
            new DavSettingsViewModel()
        );
    }
}