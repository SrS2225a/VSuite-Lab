using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;


namespace VSuiteLab.Models;

public partial class CalDavTask : ObservableObject
{   
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? uriUrl {get; set;}
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private DateTimeOffset? lastModified = DateTimeOffset.Now;
    [ObservableProperty] private TodoStatus status = TodoStatus.NeedsAction;
    [ObservableProperty] private string location = string.Empty;
    [ObservableProperty] private int priority = 0;
    [ObservableProperty] private DateTimeOffset? dueDate = null;
    [ObservableProperty] private DateTimeOffset? startDate = null;
    [ObservableProperty] private DateTimeOffset? completedDate = null;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string contact = string.Empty;
    [ObservableProperty] private string classification = string.Empty;
    
    [ObservableProperty] private string etag = string.Empty;
    [ObservableProperty] private string? uid = string.Empty;
    [ObservableProperty] private int sequence = 0;

    public Guid DavConfigId { get; set; }
    public virtual DavConfig DavConfig { get; set; }

    public bool IsDirty { get; set; }
    public bool IsDeleted { get; set; }
    
    public ObservableCollection<CalDavAlarm> Alarms { get; set; } = new();
    public ObservableCollection<CalDavCategory> Categories { get; set; } = new();
    public virtual ObservableCollection<CalDavAttendee> Attendees { get; set; } = new();
    public virtual ObservableCollection<CalDavAttachment> Attachments { get; set; } = new();
    public virtual ObservableCollection<CalDavComment> Comments { get; set; } = new();
}

public enum TodoStatus
{
    NeedsAction,
    InProgress,
    Completed,
    Cancelled
}