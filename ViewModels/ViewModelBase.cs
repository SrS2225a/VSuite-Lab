using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VSuiteLab.Models;
using System.Windows;

namespace VSuiteLab.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
    }

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
}