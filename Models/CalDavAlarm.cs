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
        set => SelectedDate = TimeConverter.SetDateOnly(SelectedDate, value);
    }

    [NotMapped]
    public TimeSpan? AlarmTimeOnly
    {
        get => TimeConverter.GetTimeOnly(SelectedDate);
        set => SelectedDate = TimeConverter.SetTimeOnly(SelectedDate, value);
    }

    public int? Repeat { get; set; }

    public Guid? CalDavTaskId { get; set; }
    public CalDavTask? CalDavTask { get; set; }
    
    public Guid? CalDavJournalId { get; set; }
    public CalDavJournal? CalDavJournal { get; set; }
    
    public Guid? CalDavNoteId { get; set; }
    public CalDavNote? CalDavNote { get; set; }

    public bool HasRan { get; set; } = false;
}