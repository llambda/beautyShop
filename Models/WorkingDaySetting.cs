using System;

namespace BeautyStudio.Models;

public sealed class WorkingDaySetting
{
    public DayOfWeek Day { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsWorkingDay { get; set; }
    public string StartTime { get; set; } = "09:00";
    public string EndTime { get; set; } = "18:00";
    public bool HasBreak { get; set; }
    public string BreakStart { get; set; } = "13:00";
    public string BreakEnd { get; set; } = "14:00";
}
