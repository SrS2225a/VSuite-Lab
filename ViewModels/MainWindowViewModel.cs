using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using VSuiteLab.Models;
using VSuiteLab.Models.Contexts;
using VSuiteLab.Models.Helpers;
using VSuiteLab.Services;
using VSuiteLab.Services.Sync;
using VSuiteLab.Utils.Query;
using VSuiteLab.Views;
using VSuiteLab.Views.Windows;
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

        [ObservableProperty] private SearchQueryBuilder queryBuilder = new();

        public ObservableCollection<DavConfig> DavInstances { get; } = new();

        public ObservableCollection<SyncProgress> SyncResults { get; } = new();

        public bool HasSyncErrors => SyncResults.Any(x => !x.Success);

        [ObservableProperty] private SyncProgress? activeSync;

        [ObservableProperty] private bool isSyncing;

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
            if (IsSyncing) return;
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
        public async Task ShowAboutAsync()
        {
            var assembly = Assembly.GetEntryAssembly();
            var buildDate = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "BuildDate")?.Value;

            string version = Assembly.GetEntryAssembly().GetName().Version.ToString();

            var os = RuntimeInformation.OSDescription;
            var arch = RuntimeInformation.OSArchitecture;
            var framework = RuntimeInformation.FrameworkDescription;

            var message =
                $"""
                 ━━━━━━━━━━━━━━━━━━━━━━
                       V S U I T E   L A B
                 ━━━━━━━━━━━━━━━━━━━━━━

                 🧾 Version
                 {version}
                 Built: {buildDate:yyyy-MM-dd}

                 🖥 Environment
                 {framework}
                 {os} ({arch})

                 ℹ️ Frameworks
                 Built with Avalonia UI

                 ━━━━━━━━━━━━━━━━━━━━━━
                 © 2026 SrS2225a
                 """;

            var box = MessageBoxManager.GetMessageBoxStandard(
                "About VSuite Lab", message, ButtonEnum.Ok, Icon.Info);

            await box.ShowAsync();
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
            }, maxIndex: 1);
            WeakReferenceMessenger.Default.Send(new SyncCompletedMessage(config));
        }

        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            var settingsWindow = new SettingsWindow();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
                {
                    MainWindow: not null
                } desktop) await settingsWindow.ShowDialog(desktop.MainWindow);
        }

        [RelayCommand]
        private void OpenHelp()
            => OpenUrl("https://github.com/SrS2225a/VSuite-Lab/wiki");

        [RelayCommand]
        private void OpenSource()
            => OpenUrl("https://github.com/SrS2225a/VSuite-Lab");

        [RelayCommand]
        private void OpenIssues()
            => OpenUrl("https://github.com/SrS2225a/VSuite-Lab/issues");

        [RelayCommand]
        private void OpenDonate()
            => OpenUrl("https://github.com/SrS2225a/VSuite-Lab");

        public MainWindowViewModel()
        {
            _databaseService = new DatabaseService();
            _syncService = new SyncService();

            WeakReferenceMessenger.Default.Register<DavConfigChangedMessage>(this,
                (_, m) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        switch (m.ChangeType)
                        {
                            case DavConfigChangeType.Added:
                                if (!DavInstances.Any(x => x.Id == m.Value.Id))
                                    DavInstances.Add(m.Value);
                                break;

                            case DavConfigChangeType.Updated:
                                var existing = DavInstances.FirstOrDefault(x => x.Id == m.Value.Id);
                                if (existing != null)
                                {
                                    var index = DavInstances.IndexOf(existing);
                                    DavInstances[index] = m.Value;
                                }

                                break;

                            case DavConfigChangeType.Deleted:
                                var toRemove = DavInstances.FirstOrDefault(x => x.Id == m.Value.Id);
                                if (toRemove != null)
                                    DavInstances.Remove(toRemove);
                                break;
                        }
                    });
                });

            InitializationTask = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            JournalsViewModel = new JournalsViewModel(QueryBuilder);
            TasksViewModel = new TasksViewModel(QueryBuilder);
            NotesViewModel = new NotesViewModel(QueryBuilder);

            await LoadMains();

            SelectedTabIndex = 0;
            UpdateSelectedTabContent();

            _ = StartAutoSync();

            _ = CheckForUpdatesAsnyc();
        }

        private async Task LoadMains()
        {
            var instances = await _databaseService?.ReadAllAsync<DavConfig>()!;
            foreach (var instance in instances.Value!.OrderBy(i => i.Name))
            {
                DavInstances.Add(instance);
            }
        }

        private async Task CheckForUpdatesAsnyc()
        {
            var updateService = new UpdateService();

            var dbSettings = await _databaseService.ReadAllAsync<Settings>();
            var settings = dbSettings.Value!.FirstOrDefault();

            if (settings!.UpdateDoNotAsk)
                return;

            if (!await updateService!.CheckAsync())
                return;

            var release = updateService.GetLatestRelease();
            var version = release?.TagName;

            var box = MessageBoxManager.GetMessageBoxCustom(
                new MessageBoxCustomParams
                {
                    ContentTitle = "Update Available",
                    ContentMessage = $"A new VSuite Lav version {version} is available. Would you like to update now?",
                    ButtonDefinitions =
                    [
                        new ButtonDefinition { Name = "Update" },
                        new ButtonDefinition { Name = "No" },
                        new ButtonDefinition { Name = "Don't ask again" }
                    ],
                    Icon = Icon.Question
                });

            var result = await box.ShowAsync();

            switch (result)
            {
                case "Update":
                {
                    var download = await updateService.DownloadAsync(release);
                    if (download != null)
                        await updateService.InstallAsync(download);
                    break;
                }

                case "Don't ask again":
                {
                    settings.UpdateDoNotAsk = true;
                    await _databaseService.UpdateAsync(settings);
                    break;   
                }

                case "No":
                    break;
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
                () => { Avalonia.Threading.Dispatcher.UIThread.Post(() => { SyncResults.Clear(); }); });
        }

        private void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
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
                {
                    var view = new JournalsView
                    {
                        DataContext = JournalsViewModel
                    };

                    view.Loaded += (_, _) => { JournalsViewModel.CancelNoteSelection(); };

                    SelectedTabContent = view;
                    QueryBuilder.SetAvailableFields(QuerySchemaRegistry.Get<CalDavJournal>());
                    break;
                }
                case 1:
                {
                    var view = new NotesView
                    {
                        DataContext = NotesViewModel
                    };

                    view.Loaded += (_, _) => { NotesViewModel.CancelNoteSelection(); };

                    SelectedTabContent = view;

                    QueryBuilder.SetAvailableFields(QuerySchemaRegistry.Get<CalDavNote>());
                    break;
                }
                case 2:
                {
                    var view = new TasksView
                    {
                        DataContext = TasksViewModel
                    };

                    view.Loaded += (_, _) => { TasksViewModel.CancelNoteSelection(); };

                    SelectedTabContent = view;
                    QueryBuilder.SetAvailableFields(QuerySchemaRegistry.Get<CalDavTask>());
                    break;
                }
                default:
                    SelectedTabContent = null;
                    break;
            }
        }
    }
}