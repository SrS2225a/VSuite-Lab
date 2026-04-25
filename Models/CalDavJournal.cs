using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models;

public partial class CalDavJournal : CalDavItem
{
    [ObservableProperty] private JournalStatus status = JournalStatus.Draft;
    [ObservableProperty] private string? classification = string.Empty;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string contact = string.Empty;
    [ObservableProperty] private DateTimeOffset? publishedDate;
    

    public override bool HasDate => PublishedDate.HasValue;
}

public enum JournalStatus
{
    Draft,
    Final,
    Cancelled
}