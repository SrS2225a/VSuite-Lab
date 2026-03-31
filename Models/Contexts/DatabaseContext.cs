using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace VSuiteLab.Models;

public class DatabaseContext : DbContext
{
    public DbSet<DavConfig> DavConfigs { get; set; }
    public DbSet<CalDavTask> Tasks { get; set; }
    public DbSet<CalDavJournal> Journals { get; set; }
    public DbSet<CalDavNote> Notes { get; set; }
    public DbSet<Settings> Settings { get; set; }

    public DbSet<CalDavAlarm> Alarms { get; set; }
    public DbSet<CalDavCategory> Categories { get; set; }
    public DbSet<CalDavAttendee> Attendees { get; set; }
    public DbSet<CalDavAttachment> Attachments { get; set; }
    public DbSet<CalDavComment> Comments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ------------------- TASK -------------------
    modelBuilder.Entity<CalDavTask>()
        .HasOne(n => n.DavConfig)
        .WithMany(d => d.Tasks)
        .HasForeignKey(n => n.DavConfigId)
        .OnDelete(DeleteBehavior.Cascade);

    // Alarm (TASK ONLY)
    modelBuilder.Entity<CalDavAlarm>()
        .HasOne(a => a.CalDavTask)
        .WithMany(t => t.Alarms)
        .HasForeignKey(a => a.CalDavTaskId)
        .OnDelete(DeleteBehavior.Cascade);

    // Category
    modelBuilder.Entity<CalDavCategory>()
        .HasOne(c => c.CalDavTask)
        .WithMany(t => t.Categories)
        .HasForeignKey(c => c.CalDavTaskId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavCategory>()
        .HasOne(c => c.CalDavJournal)
        .WithMany(j => j.Categories)
        .HasForeignKey(c => c.CalDavJournalId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavCategory>()
        .HasOne(c => c.CalDavNote)
        .WithMany(n => n.Categories)
        .HasForeignKey(c => c.CalDavNoteId)
        .OnDelete(DeleteBehavior.Cascade);

    // Attendee (already fixed)
    modelBuilder.Entity<CalDavAttendee>()
        .HasOne(a => a.CalDavTask)
        .WithMany(t => t.Attendees)
        .HasForeignKey(a => a.CalDavTaskId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavAttendee>()
        .HasOne(a => a.CalDavJournal)
        .WithMany(j => j.Attendees)
        .HasForeignKey(a => a.CalDavJournalId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavAttendee>()
        .HasOne(a => a.CalDavNote)
        .WithMany(n => n.Attendees)
        .HasForeignKey(a => a.CalDavNoteId)
        .OnDelete(DeleteBehavior.Cascade);

    // Attachment
    modelBuilder.Entity<CalDavAttachment>()
        .HasOne(a => a.CalDavTask)
        .WithMany(t => t.Attachments)
        .HasForeignKey(a => a.CalDavTaskId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavAttachment>()
        .HasOne(a => a.CalDavJournal)
        .WithMany(j => j.Attachments)
        .HasForeignKey(a => a.CalDavJournalId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavAttachment>()
        .HasOne(a => a.CalDavNote)
        .WithMany(n => n.Attachments)
        .HasForeignKey(a => a.CalDavNoteId)
        .OnDelete(DeleteBehavior.Cascade);

    // Comment
    modelBuilder.Entity<CalDavComment>()
        .HasOne(c => c.CalDavTask)
        .WithMany(t => t.Comments)
        .HasForeignKey(c => c.CalDavTaskId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavComment>()
        .HasOne(c => c.CalDavJournal)
        .WithMany(j => j.Comments)
        .HasForeignKey(c => c.CalDavJournalId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<CalDavComment>()
        .HasOne(c => c.CalDavNote)
        .WithMany(n => n.Comments)
        .HasForeignKey(c => c.CalDavNoteId)
        .OnDelete(DeleteBehavior.Cascade);

    // ------------------- JOURNAL -------------------
    modelBuilder.Entity<CalDavJournal>()
        .HasOne(n => n.DavConfig)
        .WithMany(d => d.Journals)
        .HasForeignKey(n => n.DavConfigId)
        .OnDelete(DeleteBehavior.Cascade);

    // ------------------- NOTE -------------------
    modelBuilder.Entity<CalDavNote>()
        .HasOne(n => n.DavConfig)
        .WithMany(d => d.Notes)
        .HasForeignKey(n => n.DavConfigId)
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