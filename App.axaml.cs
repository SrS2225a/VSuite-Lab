using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using VSuiteLab.Models;
using VSuiteLab.Utils;
using VSuiteLab.Utils.Query;
using VSuiteLab.ViewModels;
using VSuiteLab.Views;
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

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            desktop.MainWindow = new MainWindow()
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}