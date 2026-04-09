using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VSuiteLab.Models;
using VSuiteLab.Services;
using VSuiteLab.Views;

namespace VSuiteLab.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public JournalsViewModel JournalsViewModel { get; } = new JournalsViewModel();
        public TasksViewModel TasksViewModel { get; } = new TasksViewModel();
        public NotesViewModel NotesViewModel { get; } = new NotesViewModel();
        
        private SyncService? _syncService;
        private DatabaseService? _databaseService;
        
        public ObservableCollection<DavConfig> DavInstances { get; } = new();

        public ObservableCollection<SyncProgress> SyncResults { get; } = new();
        
        public bool HasSyncErrors => SyncResults.Any(x => !x.Success);
        
        [ObservableProperty]
        private SyncProgress? activeSync;

        [ObservableProperty]
        private bool isSyncing;
        
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();

                    if (CurrentSearchContext != null)
                        CurrentSearchContext.SearchText = value;
                }
            }
        }

        [RelayCommand]
        public async Task SyncCommand()
        {
            if(IsSyncing) return;
            IsSyncing = true;

            SyncResults.Clear();

            await _syncService.SyncAllAsync(result =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ActiveSync = result;
                    if (result is { IsCompleted: true, Success: false })
                    {
                        SyncResults.Add(result);
                        OnPropertyChanged(nameof(HasSyncErrors));
                    }
                });
            });
            
            IsSyncing = false;
        }
        
        [RelayCommand]
        private async Task RetrySyncAsync(SyncProgress result)
        {
            if (result?.Config == null)
                return;

            SyncResults.Clear();

            await _syncService.SyncAsync(result.Config, e =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ActiveSync = e;
                    if (e is { IsCompleted: true, Success: false })
                    {
                        SyncResults.Add(e);
                        OnPropertyChanged(nameof(HasSyncErrors));
                    }
                });
            });
            WeakReferenceMessenger.Default.Send(new SyncCompletedMessage(result.Config));
        }

        [RelayCommand]
        public async Task SyncDavItem(DavConfig config)
        {
            SyncResults.Clear();

            await _syncService.SyncAsync(config, e =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ActiveSync = e;
                    if (e is { IsCompleted: true, Success: false })
                    {
                        SyncResults.Add(e);
                        OnPropertyChanged(nameof(HasSyncErrors));
                    }
                });
            }, MaxIndex:1);
            WeakReferenceMessenger.Default.Send(new SyncCompletedMessage(config));
        }

        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            var settingsWindow = new SettingsWindow();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await settingsWindow.ShowDialog(desktop.MainWindow);
            }
        }
        
        public MainWindowViewModel()
        {
            _databaseService = new DatabaseService();
            _syncService = new SyncService();

            _ = LoadMains();
            
            SelectedTabIndex = 0;
            UpdateSelectedTabContent();
            
            _ = StartAutoSync();
        }

        private async Task LoadMains()
        {
            var instances = await _databaseService.ReadAllAsync<DavConfig>();
            foreach(var instance in instances.Value.OrderBy(i => i.Name))
            {
                DavInstances.Add(instance);
            }
        }

        private async Task StartAutoSync()
        {
            await _syncService.StatPerodic(
                result =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        ActiveSync = result;

                        if (result is { IsCompleted: true, Success: false })
                        {
                            SyncResults.Add(result);
                            OnPropertyChanged(nameof(HasSyncErrors));
                        }
                    });
                },
                () =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        SyncResults.Clear();
                    });
                });
        }
        
        
        public IViewModelSearchableContext? CurrentSearchContext
        {
            get
            {
                return SelectedTabIndex switch
                {
                    0 => JournalsViewModel as IViewModelSearchableContext,
                    1 => NotesViewModel as IViewModelSearchableContext,
                    2 => TasksViewModel as IViewModelSearchableContext,
                    _ => null
                };
            }
        }
        
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentSearchContext));
                    
                    if (CurrentSearchContext != null)
                        CurrentSearchContext.SearchText = SearchText;
                    
                    UpdateSelectedTabContent();
                }
            }
        }

        private object? _selectedTabContent;
        public object? SelectedTabContent
        {
            get => _selectedTabContent;
            set
            {
                _selectedTabContent = value;
                OnPropertyChanged();
            }
        }

        public void UpdateSelectedTabContent()
        {
            switch (SelectedTabIndex)
            {
                case 0:
                    SelectedTabContent = new JournalsView() {DataContext = JournalsViewModel};
                    break;
                case 1:
                    SelectedTabContent = new NotesView();
                    break;
                case 2:
                    SelectedTabContent = new TasksView() { DataContext = TasksViewModel };
                    break;
                default:
                    SelectedTabContent = null;
                    break;
            }
        }
    }
}