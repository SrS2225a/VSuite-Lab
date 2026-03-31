using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VSuiteLab.Models;

public class CalDavCategory
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid? CalDavTaskId { get; set; }
    public CalDavTask? CalDavTask { get; set; }

    public Guid? CalDavJournalId { get; set; }
    public CalDavJournal? CalDavJournal { get; set; }

    public Guid? CalDavNoteId { get; set; }
    public CalDavNote? CalDavNote { get; set; }

    public string Value { get; set; } = string.Empty;
}