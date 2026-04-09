using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSuiteLab.Models;

public class CalDavAttendee
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid? CalDavTaskId { get; set; }
    public CalDavTask? CalDavTask { get; set; }

    public Guid? CalDavJournalId { get; set; }
    public CalDavJournal? CalDavJournal { get; set; }

    public Guid? CalDavNoteId { get; set; }
    public CalDavNote? CalDavNote { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}