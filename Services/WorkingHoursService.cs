using BeautyStudio.Data;
using BeautyStudio.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BeautyStudio.Services;

public sealed class WorkingHoursService
{
    private static string SettingsPath => Path.Combine(AppDbContext.DataDirectory, "working-hours.json");

    public List<WorkingDaySetting> Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<List<WorkingDaySetting>>(File.ReadAllText(SettingsPath));
                if (loaded is { Count: 7 }) return loaded.OrderBy(x => ((int)x.Day + 6) % 7).ToList();
            }
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Lettura orari di lavoro", ex);
        }

        return Enum.GetValues<DayOfWeek>().Select(day => new WorkingDaySetting
        {
            Day = day,
            DisplayName = day switch
            {
                DayOfWeek.Monday => "Lunedì",
                DayOfWeek.Tuesday => "Martedì",
                DayOfWeek.Wednesday => "Mercoledì",
                DayOfWeek.Thursday => "Giovedì",
                DayOfWeek.Friday => "Venerdì",
                DayOfWeek.Saturday => "Sabato",
                _ => "Domenica"
            },
            IsWorkingDay = day != DayOfWeek.Sunday,
            StartTime = "09:00",
            EndTime = day == DayOfWeek.Saturday ? "13:00" : "18:00",
            HasBreak = day != DayOfWeek.Saturday,
            BreakStart = "13:00",
            BreakEnd = "14:00"
        }).OrderBy(x => ((int)x.Day + 6) % 7).ToList();
    }

    public void Save(IEnumerable<WorkingDaySetting> days)
    {
        var items = days.OrderBy(x => ((int)x.Day + 6) % 7).ToList();
        if (items.Count != 7 || items.Select(x => x.Day).Distinct().Count() != 7)
            throw new InvalidOperationException("Configura tutti i sette giorni della settimana.");

        foreach (var day in items.Where(x => x.IsWorkingDay))
        {
            if (!TimeSpan.TryParseExact(day.StartTime, @"hh\:mm", CultureInfo.InvariantCulture, out var start)
                || !TimeSpan.TryParseExact(day.EndTime, @"hh\:mm", CultureInfo.InvariantCulture, out var end)
                || start >= end)
                throw new InvalidOperationException($"Orario di apertura o chiusura non valido per {day.DisplayName}.");

            if (day.HasBreak)
            {
                if (!TimeSpan.TryParseExact(day.BreakStart, @"hh\:mm", CultureInfo.InvariantCulture, out var breakStart)
                    || !TimeSpan.TryParseExact(day.BreakEnd, @"hh\:mm", CultureInfo.InvariantCulture, out var breakEnd)
                    || breakStart >= breakEnd || breakStart < start || breakEnd > end)
                    throw new InvalidOperationException($"La pausa di {day.DisplayName} deve essere compresa tra apertura e chiusura.");
            }
        }

        Directory.CreateDirectory(AppDbContext.DataDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true }));
    }

    public bool IsAppointmentWithinWorkingHours(DateTime startDateTime, int durationMinutes, out string reason)
    {
        var day = Load().First(x => x.Day == startDateTime.DayOfWeek);
        if (!day.IsWorkingDay)
        {
            reason = $"Il {day.DisplayName} è impostato come giorno di chiusura.";
            return false;
        }

        if (!TimeSpan.TryParse(day.StartTime, out var open) || !TimeSpan.TryParse(day.EndTime, out var close))
        {
            reason = $"Gli orari di {day.DisplayName} non sono validi.";
            return false;
        }

        var start = startDateTime.TimeOfDay;
        var end = start.Add(TimeSpan.FromMinutes(durationMinutes));
        if (start < open || end > close)
        {
            reason = $"L'appuntamento deve rientrare nell'orario {open:hh\\:mm}–{close:hh\\:mm} di {day.DisplayName}.";
            return false;
        }

        if (day.HasBreak && TimeSpan.TryParse(day.BreakStart, out var breakStart)
            && TimeSpan.TryParse(day.BreakEnd, out var breakEnd)
            && start < breakEnd && end > breakStart)
        {
            reason = $"L'appuntamento si sovrappone alla pausa {breakStart:hh\\:mm}–{breakEnd:hh\\:mm}.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
