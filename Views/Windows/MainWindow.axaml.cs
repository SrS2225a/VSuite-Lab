using Avalonia.Controls;
using Avalonia.Input;
using VSuiteLab.ViewModels;

namespace VSuiteLab.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        AddHandler(KeyDownEvent, OnGlobalKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    
    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var focused = topLevel?.FocusManager.GetFocusedElement();


        if (focused is TextBox ||
            focused is ComboBox ||
            focused is NumericUpDown ||
            focused is DatePicker)
        {
            return;
        }
        
        
        if (DataContext is not MainWindowViewModel vm)
            return;


        switch (e.Key)
        {
            case Key.Oem2:
                if (SearchButton?.Flyout is Flyout flyout)
                {
                    flyout.ShowAt(SearchButton);
                }
                break;

            case Key.D1:
            case Key.NumPad1:
                vm.SelectedTabIndex = 0;
                break;

            case Key.D2:
            case Key.NumPad2:
                vm.SelectedTabIndex = 1;
                break;

            case Key.D3:
            case Key.NumPad3:
                vm.SelectedTabIndex = 2;
                break;
            default:
                return;
        }
    }
}