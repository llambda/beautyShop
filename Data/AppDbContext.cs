using BeautyStudio.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace BeautyStudio.Data;

public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InterventionLocation> InterventionLocations => Set<InterventionLocation>();
    public DbSet<InterventionType> InterventionTypes => Set<InterventionType>();
    public DbSet<Intervention> Interventions => Set<Intervention>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<InterventionHistoryEntry> InterventionHistory => Set<InterventionHistoryEntry>();

    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BeautyStudio");
    public static string DatabasePath => Path.Combine(DataDirectory, "Database", "BeautyStudio.db");
    public static string PhotosDirectory => Path.Combine(DataDirectory, "Photos");
    public static string ConsentsDirectory => Path.Combine(DataDirectory, "Documents", "ConsensiInformati");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        Directory.CreateDirectory(Path.Combine(PhotosDirectory, "Customers"));
        Directory.CreateDirectory(Path.Combine(PhotosDirectory, "Treatments"));
        Directory.CreateDirectory(ConsentsDirectory);
        Directory.CreateDirectory(Path.Combine(DataDirectory, "Backups"));
        optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Customer>().HasIndex(c => new { c.LastName, c.FirstName });
        modelBuilder.Entity<InterventionLocation>().ToTable("InterventionLocations");
        modelBuilder.Entity<InterventionType>().ToTable("InterventionTypes");
        modelBuilder.Entity<Intervention>().ToTable("Interventions");
        modelBuilder.Entity<Appointment>().ToTable("Appointments");
        modelBuilder.Entity<InterventionHistoryEntry>().ToTable("InterventionHistory");
        modelBuilder.Entity<InterventionHistoryEntry>()
            .HasOne<Intervention>()
            .WithMany()
            .HasForeignKey(x => x.InterventionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InterventionHistoryEntry>()
            .HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}