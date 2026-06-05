using System;
using System.ComponentModel.DataAnnotations;

namespace backend.models;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = "";
    [Required]
    [StringLength(50)]
    public string Firstname { get; set; } = "";
    [Required]
    [StringLength(50)]
    public string Lastname { get; set; } = "";
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";
    [Required]
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public string FullName => $"{Firstname} {Lastname}";
    public virtual UserProfile? UserProfile { get; set; }
}