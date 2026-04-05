using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
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
    }
}