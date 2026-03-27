using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;

namespace VSuiteLab.Services.NotificationController;

public class WindowsNotificationController : INotificationService
{
    public Task ShowNotificationAsync(string title, string message)
    {
        ToastContentBuilder toastContentBuilder = new ToastContentBuilder()
            .AddText(title ?? "VSuiteLab")
            .AddText(message ?? "No message");
        #if WINDOWS
        toastContentBuilder.Show();
        #endif
        return Task.CompletedTask;
    }
}