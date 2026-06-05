using backend.models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class Database : DbContext
{
    public Database(DbContextOptions<Database> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<CalendarAccount> CalendarAccounts { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure table name
        modelBuilder.Entity<User>().ToTable("Users");
        // Add unique indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        // Configure property constraints
        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);
        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);
        // Configure UserProfile
        modelBuilder.Entity<UserProfile>()
            .HasOne(up => up.User)
            .WithOne(u => u.UserProfile)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        // Store TimeZone as string
        modelBuilder.Entity<UserProfile>()
            .Property(up => up.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100);
        // Store Occupation as string (enum conversion)
        modelBuilder.Entity<UserProfile>()
            .Property(up => up.Occupation)
            .HasConversion<string>()
            .HasMaxLength(30);
    }
}