using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VSuiteLab.Controllers;

namespace VSuiteLab.Services;

public interface INotificationService
{
    Task ShowNotificationAsync(string title, string message);
}

public class NotificationService : INotificationService
{
    private readonly INotificationService _impl;

    public NotificationService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            _impl = new LinuxNotificationController();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _impl = new WindowsNotificationController();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _impl = new MacNotificationController();
        else
            throw new PlatformNotSupportedException();
    }
    
    
    /// <summary>
    ///  Shows a notification on the current platform
    /// </summary>
    /// <param name="title">The title of the notification</param>
    /// <param name="message">The message of the notification</param>
    public Task ShowNotificationAsync(string title, string message) => _impl.ShowNotificationAsync(title, message);
}