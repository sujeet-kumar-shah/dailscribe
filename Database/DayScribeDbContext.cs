using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using DayScribe.Database.Models;

namespace DayScribe.Database;

public class DayScribeDbContext : DbContext
{
    public DbSet<ActivityLogEntry> ActivityLogs => Set<ActivityLogEntry>();
    public DbSet<BrowserEvent> BrowserEvents => Set<BrowserEvent>();
    public DbSet<ArticleSummary> ArticleSummaries => Set<ArticleSummary>();

    public DayScribeDbContext()
    {
    }

    public DayScribeDbContext(DbContextOptions<DayScribeDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbFolder = Path.Combine(path, "DayScribe");
            Directory.CreateDirectory(dbFolder);
            var dbPath = Path.Combine(dbFolder, "dayscribe.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
