using BeautyStudio.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeautyStudio.Services;

public static class AppointmentOverlapValidator
{
    public static bool HasOverlap(DateTime start, int durationMinutes, IEnumerable<Appointment> existingAppointments)
    {
        if (durationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        var end = start.AddMinutes(durationMinutes);
        return existingAppointments.Any(existing =>
            !string.Equals(existing.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(existing.Status, "Non presentato", StringComparison.OrdinalIgnoreCase)
            && existing.Date < end
            && existing.Date.AddMinutes(Math.Max(1, existing.DurationMinutes)) > start);
    }
}
