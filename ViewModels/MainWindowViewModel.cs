using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VSuiteLab.Models;
using VSuiteLab.Services;
using VSuiteLab.Views;

namespace VSuiteLab.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public TasksViewModel TasksViewModel { get; } = new TasksViewModel();
        private SyncService? _syncService;

        public ObservableCollection<SyncProgress> SyncResults { get; } = new();

        [ObservableProperty]
        private SyncProgress? activeSync;

        [ObservableProperty]
        private bool isSyncing;
        

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
                    if (result.IsCompleted)
                    {
                        SyncResults.Add(result);
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

            await _syncService.SyncAsync(result.Config, r =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (r.IsCompleted)
                    {
                        SyncResults.Add(r);
                    }
                });
            });
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
            _syncService = new SyncService();
            _ = StartAutoSync();
        }

        private async Task StartAutoSync()
        {
            await _syncService.StatPerodic(result =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ActiveSync = result;

                    if (result.IsCompleted)
                        SyncResults.Add(result);
                });
            });
        }
    }
}