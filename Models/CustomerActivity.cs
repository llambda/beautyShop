using System;

namespace BeautyStudio.Models;

public sealed class CustomerActivity
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string Category { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
