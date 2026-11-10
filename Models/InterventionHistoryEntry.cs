using System;

namespace BeautyStudio.Models;

public class InterventionHistoryEntry
{
    public int Id { get; set; }
    public int InterventionId { get; set; }
    public int CustomerId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
