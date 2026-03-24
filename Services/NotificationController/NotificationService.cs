using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VSuiteLab.Services.NotificationController;

public class NotificationService : INotificationService
{
    private readonly INotificationService _impl;

    public NotificationService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            _impl = new LinuxNotificationService();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _impl = new WindowsNotificationService();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _impl = new MacNotificationService();
        else
            throw new PlatformNotSupportedException();
    }
    
    public Task ShowNotificationAsync(string title, string message) => _impl.ShowNotificationAsync(title, message);
}