using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models;

public partial class CalDavNote : CalDavItem
{
    [ObservableProperty] private JournalStatus status = JournalStatus.Draft;
    [ObservableProperty] private string classification = string.Empty;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string contact = string.Empty;

    public ObservableCollection<CalDavCategory> Categories { get; set; } = new();
    public ObservableCollection<CalDavAttachment> Attachments { get; set; } = new();
    public ObservableCollection<CalDavComment> Comments { get; set; } = new();
    public ObservableCollection<CalDavAttendee> Attendees { get; set; } = new();
    public ObservableCollection<CalDavAlarm> Alarms { get; set; } = new();

    public override bool HasDate => false;
}