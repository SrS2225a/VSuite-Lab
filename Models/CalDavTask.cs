using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;


namespace VSuiteLab.Models;

public partial class CalDavTask : CalDavItem
{   
    [ObservableProperty] private TodoStatus status = TodoStatus.NeedsAction;
    [ObservableProperty] private string location = string.Empty;
    [ObservableProperty] private int priority;
    [ObservableProperty] private DateTimeOffset? dueDate;
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? completedDate;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string contact = string.Empty;
    [ObservableProperty] private string? classification = string.Empty;

    public ObservableCollection<CalDavAlarm> Alarms { get; set; } = new();
    public ObservableCollection<CalDavCategory> Categories { get; set; } = new();
    public ObservableCollection<CalDavAttendee> Attendees { get; set; } = new();
    public ObservableCollection<CalDavAttachment> Attachments { get; set; } = new();
    public ObservableCollection<CalDavComment> Comments { get; set; } = new();

    public override bool HasDate => StartDate.HasValue || DueDate.HasValue;
}

public enum TodoStatus
{
    NeedsAction,
    InProgress,
    Completed,
    Cancelled
}