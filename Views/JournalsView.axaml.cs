using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using VSuiteLab.Models;

namespace VSuiteLab.Views;

public class GroupItemsCalDavJournal : ObservableObject
{
    private object? _key;
    public object? Key { get => _key; set => SetProperty(ref _key, value); }

    private ObservableCollection<CalDavJournal> _items = new();
    public ObservableCollection<CalDavJournal> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }
}

public partial class JournalsView : UserControl
{
    public JournalsView()
    {
        InitializeComponent();
    }
}