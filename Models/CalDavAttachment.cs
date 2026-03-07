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

    public Guid CalDavNoteId { get; set; }
}