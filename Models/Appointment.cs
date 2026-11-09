using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyStudio.Models;

public class Appointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public int DurationMinutes { get; set; } = 60;
    public int? InterventionTypeId { get; set; }
    public int? LocationId { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Da confermare";
    public Customer? Customer { get; set; }
    public InterventionType? InterventionType { get; set; }
    public InterventionLocation? Location { get; set; }
    [NotMapped] public bool IsOutsideWorkingHours { get; set; }
    [NotMapped] public string WorkingHoursWarning { get; set; } = string.Empty;
}