using System;

namespace VSuiteLab.Models;

public class Settings
{
    public Guid Id  { get; set; }
    public float SyncAuto { get; set; } = 600;
    public bool SyncOnChange { get; set; } = false;
    public bool? DebugEnabled { get; set; } = false;
    public bool UpdateDoNotAsk { get; set; } = false;
    
    public ConflictStrategy ConflictStrategy { get; set; } = ConflictStrategy.Ignore;
}

public enum ConflictStrategy
{
    PreferServer,
    PreferClient,
    Ignore
}