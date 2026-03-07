using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace VSuiteLab.Models;

public class DatabaseContext : DbContext
{
    //public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { } 
    
    public DbSet<DavConfig> DavConfigs { get; set; }
    public DbSet<CalDavTask> Notes { get; set; }
    public DbSet<Settings> Settings { get; set; }
    public DbSet<CalDavAlarm> Alarms { get; set; }
    public DbSet<CalDavCategory> Categories { get; set; }
    public DbSet<CalDavAttendee> Attendees { get; set; }
    public DbSet<CalDavAttachment> Attachments { get; set; }
    public DbSet<CalDavComment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<CalDavTask>()
            .HasOne(n => n.DavConfig)
            .WithMany(d => d.Notes)
            .HasForeignKey(n => n.DavConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CalDavAlarm>()
            .HasOne<CalDavTask>()
            .WithMany(n => n.Alarms)
            .HasForeignKey("CalDavNoteId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CalDavCategory>()
            .HasOne<CalDavTask>()
            .WithMany(n => n.Categories)
            .HasForeignKey("CalDavNoteId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CalDavAttendee>()
            .HasOne<CalDavTask>()
            .WithMany(n => n.Attendees)
            .HasForeignKey("CalDavNoteId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CalDavAttachment>()
            .HasOne<CalDavTask>()
            .WithMany(n => n.Attachments)
            .HasForeignKey("CalDavNoteId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CalDavComment>()
            .HasOne<CalDavTask>()
            .WithMany(n => n.Comments)
            .HasForeignKey("CalDavNoteId")
            .OnDelete(DeleteBehavior.Cascade);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VSuiteLab"
        );

        Directory.CreateDirectory(folder);

        var dbPath = Path.Combine(folder, "vsuitelab.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}")
            .EnableSensitiveDataLogging();
    }
}