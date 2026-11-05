using System;
using System.ComponentModel.DataAnnotations;

namespace BeautyStudio.Models;

public class Appointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
    public Customer? Customer { get; set; }
}
