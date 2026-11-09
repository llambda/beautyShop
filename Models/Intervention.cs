using System;

namespace BeautyStudio.Models;

public class Intervention
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int LocationId { get; set; }
    public int TypeId { get; set; }
    public string? Notes { get; set; }
    public string BeforePhotosJson { get; set; } = "[]";
    public string AfterPhotosJson { get; set; } = "[]";
    public string PhotoMetadataJson { get; set; } = "{}";
    public decimal Price { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public Customer? Customer { get; set; }
    public InterventionLocation? Location { get; set; }
    public InterventionType? Type { get; set; }
}