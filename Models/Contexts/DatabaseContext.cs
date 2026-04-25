using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace VSuiteLab.Models.Contexts;

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

        // ------------------- TPT TABLES -------------------
        modelBuilder.Entity<CalDavItem>().ToTable("CalDavItems");
        modelBuilder.Entity<CalDavTask>().ToTable("CalDavTasks");
        modelBuilder.Entity<CalDavJournal>().ToTable("CalDavJournals");
        modelBuilder.Entity<CalDavNote>().ToTable("CalDavNotes");

        // ------------------- DAV CONFIG RELATION -------------------
        modelBuilder.Entity<CalDavItem>()
            .HasOne(x => x.DavConfig)
            .WithMany(d => d.Items)
            .HasForeignKey(x => x.DavConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        // ------------------- ALARMS -------------------
        modelBuilder.Entity<CalDavAlarm>()
            .HasOne(a => a.CalDavItem)
            .WithMany(i => i.Alarms)
            .HasForeignKey(a => a.CalDavItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ------------------- CATEGORIES -------------------
        modelBuilder.Entity<CalDavCategory>()
            .HasOne(a => a.CalDavItem)
            .WithMany(i => i.Categories)
            .HasForeignKey(c => c.CalDavItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ------------------- ATTENDEES -------------------
        modelBuilder.Entity<CalDavAttendee>()
            .HasOne(a => a.CalDavItem)
            .WithMany(i => i.Attendees)
            .HasForeignKey(a => a.CalDavItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ------------------- ATTACHMENTS -------------------
        modelBuilder.Entity<CalDavAttachment>()
            .HasOne(a => a.CalDavItem)
            .WithMany(i => i.Attachments)
            .HasForeignKey(a => a.CalDavItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // ------------------- COMMENTS -------------------
        modelBuilder.Entity<CalDavComment>()
            .HasOne(a => a.CalDavItem)
            .WithMany(i => i.Comments)
            .HasForeignKey(c => c.CalDavItemId)
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