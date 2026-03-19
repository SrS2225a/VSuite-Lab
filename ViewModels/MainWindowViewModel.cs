using System;
using System.Linq;
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
        
        public ObservableCollection<SyncProgress> SyncResults { get; } = new();

        [ObservableProperty]
        private SyncProgress? activeSync;
        

        [RelayCommand]
        public async Task SyncCommand()
        {
            var syncService = new SyncService();

            SyncResults.Clear();

            await syncService.SyncAllAsync(result =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Console.WriteLine(ActiveSync?.Message);
                    ActiveSync = result;
                    if (result.IsCompleted)
                    {
                        SyncResults.Add(result);
                    }
                });
            });
        }
        
        [RelayCommand]
        private async Task RetrySyncAsync(SyncProgress result)
        {
            if (result?.Config == null)
                return;

            SyncResults.Clear();

            var syncService = new SyncService();

            await syncService.SyncAsync(result.Config, r =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SyncResults.Add(r);
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
        
        
        private SyncProgress Clone(SyncProgress p)
        {
            return new SyncProgress
            {
                Message = p.Message,
                IsError = p.IsError,
                CurrentIndex = p.CurrentIndex,
                MaxIndex = p.MaxIndex,
                ServerName = p.ServerName,
                Config = p.Config,
                Url = p.Url,
                Success = p.Success,
                Timestamp = DateTime.Now
            };
        }
    }
}