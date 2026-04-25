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



    public override bool HasDate => false;
}