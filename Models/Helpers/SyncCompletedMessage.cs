using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VSuiteLab.Models;

public class SyncCompletedMessage(DavConfig value) : ValueChangedMessage<DavConfig>(value);