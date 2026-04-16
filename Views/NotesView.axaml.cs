using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
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
    }
}