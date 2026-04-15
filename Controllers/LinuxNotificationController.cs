using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Tmds.DBus;
using VSuiteLab.Services;

namespace VSuiteLab.Controllers;

[DBusInterface("org.freedesktop.Notifications")]
public interface INotifications : IDBusObject
{
    Task<uint> NotifyAsync(
        string app_name,
        uint replaces_id,
        string app_icon,
        string summary,
        string body,
        string[] actions,
        IDictionary<string, object> hints,
        int expire_timeout);
}

public class LinuxNotificationController : INotificationService
{
    private static readonly Connection Connection = Connection.Session;

    private static readonly INotifications Proxy =
        Connection.CreateProxy<INotifications>(
            "org.freedesktop.Notifications",
            "/org/freedesktop/Notifications");

    public async Task ShowNotificationAsync(string title, string message)
    {
        var appIdentify = Assembly.GetExecutingAssembly().GetName().Name;

        var hints = new Dictionary<string, object>
        {
            ["desktop-entry"] = appIdentify,
            ["urgency"] = (byte)1
        };

        await Proxy.NotifyAsync(
            appIdentify,
            0,
            appIdentify,
            title,
            message,
            System.Array.Empty<string>(),
            hints,
            -1
        );
    }
}