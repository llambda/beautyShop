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
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetUpcomingAsync()
    {
        await using var db = new AppDbContext();
        return await db.Set<Appointment>().AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.InterventionType)
            .Where(x => x.Date >= DateTime.Now && x.Status != "Annullato"
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task SaveAsync(Appointment appointment)
    {
        await using var db = new AppDbContext();
        bool slotTaken = await db.Appointments.AnyAsync(x => x.Id != appointment.Id && x.Status != "Annullato" && x.Date == appointment.Date);
        if (slotTaken)
            throw new InvalidOperationException("Esiste già un appuntamento in questo orario. Scegli un altro orario.");
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