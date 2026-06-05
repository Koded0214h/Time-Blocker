using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.models;
public enum CalendarProvider
{
    Google,
    Outlook,
    Apple
}

public class CalendarAccount
{
    [Key]
    public int Id { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    public User User { get; set; } = null!;
    [Required]
    public CalendarProvider Provider { get; set; }
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
    [Required]
    public DateTime TokenExpiresAt { get; set; }

    public bool IsConnected { get; set; } = true;
}


public class CalendarEvent
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsExternal { get; set; }

    public string? ExternalEventId { get; set; }

    public string? Description { get; set; }

    public string? Location { get; set; }
}