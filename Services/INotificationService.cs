using System.Threading.Tasks;

namespace VSuiteLab.Services.NotificationController;

public interface INotificationService
{
    public const string NotificationServiceKey = "vsuitelab";
    Task ShowNotificationAsync(string title, string message);
}