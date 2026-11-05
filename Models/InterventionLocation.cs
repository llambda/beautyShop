using System.ComponentModel.DataAnnotations;

namespace BeautyStudio.Models;

public class InterventionLocation
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    public string? Notes { get; set; }
}
