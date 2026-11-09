using System;
using System.ComponentModel.DataAnnotations;

namespace BeautyStudio.Models;

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(150)] public string? Email { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    public string? PhotoPath { get; set; }
    public string? ConsentFormPath { get; set; }
    public string? Notes { get; set; }
    [MaxLength(2000)] public string? Allergies { get; set; }
    [MaxLength(2000)] public string? Sensitivities { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}