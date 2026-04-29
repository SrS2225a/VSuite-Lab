using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VSuiteLab.Models.Helpers;

public class SyncCompletedMessage(DavConfig value) : ValueChangedMessage<DavConfig>(value);

public enum DavConfigChangeType
{
    Added,
    Updated,
    Deleted
}

public class DavConfigChangedMessage
{
    public DavConfigChangedMessage(DavConfig config, DavConfigChangeType changeType)
    {
        Value = config;
        ChangeType = changeType;
    }

    public DavConfig Value { get; }
    public DavConfigChangeType ChangeType { get; }
}