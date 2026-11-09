using BeautyStudio.Data;
using BeautyStudio.Models;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyStudio.Services;

public class AppointmentService
{
    public async Task<List<Appointment>> GetForCustomerAsync(int customerId)
    {
        await using var db = new AppDbContext();
        return await db.Set<Appointment>().AsNoTracking()
            .Include(x => x.InterventionType)
            .Include(x => x.Location)
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        await using var db = new AppDbContext();
        return await db.Set<Appointment>().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.InterventionType)
            .Include(x => x.Location)
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetUpcomingAsync()
    {
        await using var db = new AppDbContext();
        return await db.Set<Appointment>().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.InterventionType)
            .Include(x => x.Location)
            .Where(x => x.Date >= DateTime.Now && x.Status != "Annullato")
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task SaveAsync(Appointment appointment)
    {
        await using var db = new AppDbContext();
        if (appointment.DurationMinutes <= 0 || appointment.DurationMinutes > 720)
            throw new InvalidOperationException("La durata dell'appuntamento deve essere compresa tra 1 e 720 minuti.");

        bool activeAppointment = !string.Equals(appointment.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(appointment.Status, "Non presentato", StringComparison.OrdinalIgnoreCase);
        var candidates = activeAppointment
            ? await db.Appointments.AsNoTracking()
                .Where(x => x.Id != appointment.Id
                    && x.Status != "Annullato"
                    && x.Status != "Non presentato"
                    && x.Date >= appointment.Date.AddMinutes(-720)
                    && x.Date <= appointment.Date.AddMinutes(720))
                .Select(x => new { x.Date, x.DurationMinutes })
                .ToListAsync()
            : new List<dynamic>();

        bool slotTaken = activeAppointment && candidates.Any(x =>
            x.Date < appointment.Date.AddMinutes(appointment.DurationMinutes)
            && x.Date.AddMinutes(x.DurationMinutes) > appointment.Date);
        if (slotTaken)
            throw new InvalidOperationException("Questo orario si sovrappone a un altro appuntamento. Modifica l'orario o la durata.");
        if (appointment.Id == 0) db.Add(appointment);
        else db.Update(appointment);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Appointment appointment)
    {
        await using var db = new AppDbContext();
        db.Remove(appointment);
        await db.SaveChangesAsync();
    }
}