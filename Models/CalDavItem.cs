using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VSuiteLab.Models;

public abstract partial class CalDavItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [ObservableProperty] private DateTimeOffset? lastModified = DateTimeOffset.Now;

    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    
    [ObservableProperty] private string? uri = string.Empty;
    [ObservableProperty] private string etag = string.Empty;
    [ObservableProperty] private string? uid = string.Empty;
    [ObservableProperty] private int sequence;

    public Guid DavConfigId { get; set; }
    public virtual DavConfig? DavConfig { get; set; }

    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private bool isDeleted;

    public virtual bool HasDate => false; // override in subclasses if needed
}