using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSuiteLab.Models;

public class CalDavAttendee
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    
    public Guid CalDavItemId { get; set; }
    public CalDavItem CalDavItem { get; set; }
}