using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Models;
using VSuiteLab.Models.Contexts;
using VSuiteLab.Utils;
using VSuiteLab.Utils.Query;
using VSuiteLab.ViewModels;
using VSuiteLab.Views;
using VSuiteLab.Views.Windows;
using MainWindow = VSuiteLab.Views.Windows.MainWindow;

namespace VSuiteLab;

public partial class App : Application
{
    public override void Initialize()
    {
        QuerySchemaRegistry.Register<CalDavJournal>(QueryValueResolver.JournalSchema.Fields);
        QuerySchemaRegistry.Register<CalDavNote>(QueryValueResolver.NoteSchema.Fields);
        QuerySchemaRegistry.Register<CalDavTask>(QueryValueResolver.TaskSchema.Fields);
        
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashVm = new SplashViewModel();
            var splash = new SplashWindow
            {
                DataContext = splashVm
            };

            desktop.MainWindow = splash;
            splash.Show();

            await Task.Run(() =>
            {
                using var db = new DatabaseContext();

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    splashVm.Status = "Applying database updates...");

                db.Database.Migrate();
            });

            var mainVm = new MainWindowViewModel();

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                splashVm.Status = "Loading your data...");

            await mainVm.InitializationTask;

            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            desktop.MainWindow = mainWindow;

            mainWindow.Show();
            splash.Close();
        }

        base.OnFrameworkInitializationCompleted();
    }
}