using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Models;
using VSuiteLab.ViewModels;

namespace VSuiteLab.Views;

public class GroupItemsCalDavTask : ObservableObject
{
    private object? _key;
    public object? Key { get => _key; set => SetProperty(ref _key, value); }

    private ObservableCollection<CalDavTask> _items = new();
    public ObservableCollection<CalDavTask> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }
}

public partial class TasksView : UserControl
{
    
    public TasksView()
    {
        InitializeComponent();
        
        AddHandler(KeyDownEvent, OnGlobalKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }
    
    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var focused = topLevel?.FocusManager.GetFocusedElement();


        if (focused is TextBox ||
            focused is ComboBox ||
            focused is NumericUpDown ||
            focused is DatePicker)
        {
            return;
        }


        if (DataContext is TasksViewModel vm)
        {

            switch (e.Key)
            {
                case Key.S:
                    vm.SaveNewNoteCommand.Execute(null);
                    break;
                case Key.U:
                    vm.SaveNoteCommand.Execute(null);
                    break;
                case Key.D:
                    vm.DeleteNoteCommand.Execute(null);
                    break;
                default:
                    return;
            }
        }
    }
}