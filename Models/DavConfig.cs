using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models;

public class DavConfig : ObservableObject
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Color { get; set; }
    public string httpUrl { get; set; }
    public string? username { get; set; }
    public string? password { get; set; }
    public bool SupportsVtodo { get; set; }
    public bool SupportsVjournal { get; set; }
    public string? LastSyncToken { get; set; }

    public virtual ICollection<CalDavTask> Tasks { get; set; } = new List<CalDavTask>();
    public virtual ICollection<CalDavJournal> Journals { get; set; } = new List<CalDavJournal>();
    public virtual ICollection<CalDavNote> Notes { get; set; } = new List<CalDavNote>();
}