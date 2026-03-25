using System.Threading.Tasks;
#if WINDOWS
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Notifications;
using Windows.UI.Notifications;
#endif

namespace VSuiteLab.Services.NotificationController;

public class WindowsNotificationController : INotificationService
{
    public Task ShowNotificationAsync(string title, string message)
    {
        #if WINDOWS
        var content = new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .GetToastContent();

        var toast = new ToastNotification(content.GetXml());

        ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
        #endif
        return Task.CompletedTask;
    }
}