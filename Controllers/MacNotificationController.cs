using System.Diagnostics;
using System.Threading.Tasks;

namespace VSuiteLab.Services.NotificationController;

public class MacNotificationController : INotificationService
{
    public Task ShowNotificationAsync(string title, string message)
    {
        var appIdentify = INotificationService.NotificationServiceKey;
        
        Process.Start("osascript",
            $"-e 'display notification \"{message}\" with title \"{title}\" subtitle \"{appIdentify}\"'");

        return Task.CompletedTask;
    }
}