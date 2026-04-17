using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VSuiteLab.Models;
using VSuiteLab.Models.Contexts;
using VSuiteLab.Models.Helpers;
using VSuiteLab.Services;
using VSuiteLab.Services.Sync;
using VSuiteLab.Utils.Query;
using VSuiteLab.Views;
using QueryFilterVm = VSuiteLab.Models.Contexts.QueryFilterVm;
using QueryGroupVm = VSuiteLab.Models.Contexts.QueryGroupVm;
using QuerySortVm = VSuiteLab.Models.Contexts.QuerySortVm;

namespace VSuiteLab.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private SyncService? _syncService;
        private DatabaseService? _databaseService;
        
        public Task InitializationTask { get; }
        
        [ObservableProperty]
        private SearchQueryBuilder queryBuilder = new();
        
        public ObservableCollection<DavConfig> DavInstances { get; } = new();

        public ObservableCollection<SyncProgress> SyncResults { get; } = new();
        
        public bool HasSyncErrors => SyncResults.Any(x => !x.Success);
        
        [ObservableProperty]
        private SyncProgress? activeSync;

        [ObservableProperty]
        private bool isSyncing;

        public JournalsViewModel JournalsViewModel { get; set; }
        public TasksViewModel TasksViewModel { get; set; }
        public NotesViewModel NotesViewModel { get; set; }

        [RelayCommand]
        private void AddFilter()
        {
            QueryBuilder.Filters.Add(new QueryFilterVm(QueryBuilder));
        }

        [RelayCommand]
        private void RemoveFilter(QueryFilterVm filter)
        {
            QueryBuilder.Filters.Remove(filter);
        }
        
        [RelayCommand]
        private void AddSort()
        {
            QueryBuilder.Sorts.Add(new QuerySortVm(QueryBuilder));
        }

        [RelayCommand]
        private void RemoveSort(QuerySortVm sort)
        {
            QueryBuilder.Sorts.Remove(sort);
        }

        [RelayCommand]
        private void AddGroup()
        {
            QueryBuilder.Groups.Add(new QueryGroupVm(QueryBuilder));
        }

        [RelayCommand]
        private void RemoveGroup(QueryGroupVm group)
        {
            QueryBuilder.Groups.Remove(group);
        }

        [RelayCommand]
        public async Task SyncCommand()
        {
            if(IsSyncing) return;
            IsSyncing = true;

            SyncResults.Clear();

            await _syncService?.SyncAllAsync(result =>
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
            })!;
            
            IsSyncing = false;
        }
        
        [RelayCommand]
        private async Task RetrySyncAsync(SyncProgress result)
        {
            SyncResults.Clear();

            await _syncService!.SyncAsync(result.Config, e =>
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

            await _syncService!.SyncAsync(config, e =>
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
            }, maxIndex:1);
            WeakReferenceMessenger.Default.Send(new SyncCompletedMessage(config));
        }

        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            var settingsWindow = new SettingsWindow();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop) await settingsWindow.ShowDialog(desktop.MainWindow);
        }
        
        public MainWindowViewModel()
        {
            _databaseService = new DatabaseService();
            _syncService = new SyncService();

            SelectedTabIndex = 0;
            UpdateSelectedTabContent();

            InitializationTask = Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            JournalsViewModel = new JournalsViewModel(QueryBuilder);
            TasksViewModel = new TasksViewModel(QueryBuilder);
            NotesViewModel = new NotesViewModel(QueryBuilder);
            
            await LoadMains();
            _ = StartAutoSync();
        }

        private async Task LoadMains()
        {
            var instances = await _databaseService?.ReadAllAsync<DavConfig>()!;
            foreach(var instance in instances.Value!.OrderBy(i => i.Name))
            {
                DavInstances.Add(instance);
            }
        }

        private Task StartAutoSync()
        {
            return _syncService!.StatPerodic(
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

        private void UpdateSelectedTabContent()
        {
            switch (SelectedTabIndex)
            {
                case 0:
                    SelectedTabContent = new JournalsView() {DataContext = JournalsViewModel};
                    QueryBuilder.SetAvailableFields(QuerySchemaRegistry.Get<CalDavJournal>());
                    break;
                case 1:
                    SelectedTabContent = new NotesView() { DataContext = NotesViewModel };
                    QueryBuilder.SetAvailableFields(QuerySchemaRegistry.Get<CalDavNote>());
                    break;
                case 2:
                    SelectedTabContent = new TasksView() { DataContext = TasksViewModel };
                    QueryBuilder.SetAvailableFields(QuerySchemaRegistry.Get<CalDavTask>());
                    break;
                default:
                    SelectedTabContent = null;
                    break;
            }
        }
    }
}