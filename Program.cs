using Avalonia;
using System;
using Avalonia.Logging;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Models;
using VSuiteLab.Models.Contexts;

namespace VSuiteLab;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using (var db = new DatabaseContext())
        {
            db.Database.Migrate(); // creates DB + applies migrations
        }

        BuildAvaloniaApp()
            .WithDeveloperTools()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
