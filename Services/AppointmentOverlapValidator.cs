using BeautyStudio.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeautyStudio.Services;

/// <summary>
/// Regole pure per verificare che gli appuntamenti attivi non occupino intervalli sovrapposti.
/// Gli intervalli sono semiaperti: un appuntamento può iniziare esattamente quando termina il precedente.
/// </summary>
public static class AppointmentOverlapValidator
{
    public static bool HasOverlap(DateTime start, int durationMinutes, IEnumerable<Appointment> existingAppointments)
    {
        ArgumentNullException.ThrowIfNull(existingAppointments);
        if (durationMinutes <= 0 || durationMinutes > 720)
            throw new ArgumentOutOfRangeException(nameof(durationMinutes), "La durata deve essere compresa tra 1 e 720 minuti.");

        var end = start.AddMinutes(durationMinutes);
        return existingAppointments.Any(existing =>
        {
            if (existing == null
                || string.Equals(existing.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
                || string.Equals(existing.Status, "Non presentato", StringComparison.OrdinalIgnoreCase))
                return false;

            if (existing.DurationMinutes <= 0) return false;
            var existingEnd = existing.Date.AddMinutes(existing.DurationMinutes);
            return existing.Date < end && existingEnd > start;
        });
    }
}
