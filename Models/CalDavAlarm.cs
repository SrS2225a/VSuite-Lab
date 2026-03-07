using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSuiteLab.Models;

public class CalDavAlarm
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Action { get; set; } = Ical.Net.AlarmAction.Display;
    public string? Description { get; set; }
    public string? Summary { get; set; }
    
    public DateTimeOffset SelectedDate { get; set; } = DateTimeOffset.Now;
    public TimeSpan SelectedTime { get; set; }

    public int? Repeat { get; set; }

    public Guid CalDavNoteId { get; set; }

    public bool HasRan { get; set; } = false;
}