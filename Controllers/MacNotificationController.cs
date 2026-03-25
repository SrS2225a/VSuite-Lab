using System.Threading.Tasks;
#if MACOS
using System;
using Foundation;
using UserNotifications;
#endif

namespace VSuiteLab.Services.NotificationController;

public class MacNotificationController : INotificationService
{
    private static bool _initialized;

    private async Task EnsurePermissionsAsync()
    {
        #if MACOS
        if (_initialized) return;

        var center = UNUserNotificationCenter.Current;

        await center.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert |
            UNAuthorizationOptions.Sound |
            UNAuthorizationOptions.Badge);

        _initialized = true;
        #endif
    }

    public async Task ShowNotificationAsync(string title, string message)
    {
        #if MACOS
        await EnsurePermissionsAsync();

        var content = new UNMutableNotificationContent
        {
            Title = title,
            Body = message
        };

        var request = UNNotificationRequest.FromIdentifier(
            Guid.NewGuid().ToString(),
            content,
            null);

        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
        #endif
    }
}