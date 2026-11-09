using System;

namespace BeautyStudio.Models;

public class Appointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public int? InterventionTypeId { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Da confermare";
    public Customer? Customer { get; set; }
    public InterventionType? InterventionType { get; set; }
}