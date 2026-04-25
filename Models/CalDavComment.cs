using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace VSuiteLab.Models;

public class CalDavComment
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    
    public Guid CalDavItemId { get; set; }
    public CalDavItem CalDavItem { get; set; }
}