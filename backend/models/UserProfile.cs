using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.models;

public enum Occupation
{
    Student,
    Developer,
    Founder,
    Athlete,
    Employee
}

public class UserProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }  // Foreign key (not the whole User object)

    [Required]
    public virtual User User { get; set; } = null!;  // Navigation property

    [Required]
    public TimeOnly WakeUpTime { get; set; }

    [Required]
    public TimeOnly SleepTime { get; set; }

    // Store TimeZone as string ID (e.g., "America/New_York")
    [Required]
    public string TimeZoneId { get; set; } = "UTC";

    [Required]
    public Occupation Occupation { get; set; }

    // Computed property to get TimeZoneInfo from TimeZoneId
    [NotMapped]
    public TimeZoneInfo TimeZone
    {
        get => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        set => TimeZoneId = value.Id;
    }
    
    // Optional: Add these useful properties
    public DateTime? LastUpdated { get; set; }
    
    public string? Bio { get; set; }
    
    public string? AvatarUrl { get; set; }
}