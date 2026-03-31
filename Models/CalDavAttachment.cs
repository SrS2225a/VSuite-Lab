using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSuiteLab.Models;

public class CalDavAttachment
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public byte[] Uri { get; set; }
    public string Title { get; set; }
    public string ContentType { get; set; } = string.Empty;

    public Guid? CalDavTaskId { get; set; }
    public CalDavTask? CalDavTask { get; set; }

    public Guid? CalDavJournalId { get; set; }
    public CalDavJournal? CalDavJournal { get; set; }

    public Guid? CalDavNoteId { get; set; }
    public CalDavNote? CalDavNote { get; set; }
}