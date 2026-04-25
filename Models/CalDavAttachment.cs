using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSuiteLab.Models;

public class CalDavAttachment
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public byte[] Uri { get; set; }
    public string Title { get; set; }
    public string ContentType { get; set; } = string.Empty;
    
    public Guid CalDavItemId { get; set; }
    public CalDavItem CalDavItem { get; set; }
}