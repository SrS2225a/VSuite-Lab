using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models;

public partial class CalDavJournal : CalDavItem
{
    [ObservableProperty] private string classification = string.Empty;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string contact = string.Empty;
    
    [ObservableProperty] private DateTimeOffset? publishedDate;
    
    public ObservableCollection<CalDavCategory> Categories { get; set; } = new();
    public ObservableCollection<CalDavAttachment> Attachments { get; set; } = new();
    public ObservableCollection<CalDavComment> Comments { get; set; } = new();
    public ObservableCollection<CalDavAttendee> Attendees { get; set; } = new();

    public override bool HasDate => PublishedDate.HasValue;
}