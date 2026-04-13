using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using VSuiteLab.Models;
using VSuiteLab.ViewModels;

namespace VSuiteLab.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        this.AddHandler(KeyDownEvent, OnGlobalKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    
    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        
        if (DataContext is not MainWindowViewModel vm)
            return;


        if (!IsInputElementFocused())
        {
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

    private bool IsInputElementFocused()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var focused = topLevel?.FocusManager?.GetFocusedElement();


        if (focused is TextBox ||
            focused is ComboBox ||
            focused is NumericUpDown ||
            focused is DatePicker)
        {
            return true;
        }
        return false;
    }
}