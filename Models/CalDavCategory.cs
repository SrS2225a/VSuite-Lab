using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VSuiteLab.Models;

public class CalDavCategory
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    
    public Guid CalDavItemId { get; set; }
    public CalDavItem CalDavItem { get; set; }
}