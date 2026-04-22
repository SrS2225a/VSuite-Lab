using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VSuiteLab.Models.Helpers;

public class SyncCompletedMessage(DavConfig value) : ValueChangedMessage<DavConfig>(value);

public class DavConfigChangedMessage : ValueChangedMessage<DavConfig?>
{
    public DavConfigChangedMessage(DavConfig? value) : base(value) { }
}