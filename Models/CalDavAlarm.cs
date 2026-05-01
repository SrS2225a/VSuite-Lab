using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using VSuiteLab.Converters;

namespace VSuiteLab.Models;

public class CalDavAlarm
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string? Action { get; set; } = Ical.Net.AlarmAction.Display;
    public string? Description { get; set; }
    public string? Summary { get; set; }
    
    public DateTimeOffset? SelectedDate { get; set; } = null;
    
    
    [NotMapped]
    public DateTimeOffset? AlarmDateOnly
    {
        get => TimeConverter.GetDateOnly(SelectedDate);
        set
        {
            SelectedDate = TimeConverter.SetDateOnly(SelectedDate, value);
            ResetIfMovedForward();
        }
    }

    [NotMapped]
    public TimeSpan? AlarmTimeOnly
    {
        get => TimeConverter.GetTimeOnly(SelectedDate);
        set
        {
            SelectedDate = TimeConverter.SetTimeOnly(SelectedDate, value);
            ResetIfMovedForward();
        }
    }

    public int? Repeat { get; set; }
    public bool HasRan { get; set; } = false;
    public Guid CalDavItemId { get; set; }
    public CalDavItem CalDavItem { get; set; }
    
    private void ResetIfMovedForward()
    {
        if (HasRan && SelectedDate > DateTimeOffset.UtcNow)
        {
            HasRan = false;
        }
    }
}