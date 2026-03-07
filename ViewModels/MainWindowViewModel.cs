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
        
        [ObservableProperty] 
        private BannerMessage _message = new BannerMessage();

        [RelayCommand]
        public async Task SyncCommand()
        {
            var syncService = new SyncService();

            await syncService.SyncAllAsync(_message);
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
        
    }
}