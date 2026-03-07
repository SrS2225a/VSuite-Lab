using System.Collections.Generic;

namespace VSuiteLab.Models;

public class SyncCollectionResult
{
    public string? SyncToken { get; set; }
    public List<SyncItem> ChangedResources { get; set; } = new();
    public List<string> DeletedResources { get; set; } = new();
}

public class SyncItem
{
    public string? Uri { get; set; }
    public string? Etag { get; set; }
    public string? ContentType { get; set; }
}