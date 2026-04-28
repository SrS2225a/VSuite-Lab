using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Models;
using VSuiteLab.ViewModels;

namespace VSuiteLab.Views;

public class GroupItemsCalDavNote : ObservableObject
{
    private object? _key;
    public object? Key { get => _key; set => SetProperty(ref _key, value); }

    private ObservableCollection<CalDavNote> _items = new();
    public ObservableCollection<CalDavNote> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }
}

public partial class NotesView : UserControl
{
    public NotesView()
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


        if (DataContext is NotesViewModel vm)
        {

            switch (e.Key)
            {
                case Key.S:
                    vm.SaveNewJournalCommand.Execute(null);
                    break;
                case Key.U:
                    vm.SaveJournalCommandCommand.Execute(null);
                    break;
                case Key.D:
                    vm.DeleteJournalCommandCommand.Execute(null);
                    break;
                default:
                    return;
            }
        }
    }
}