using BeautyStudio.Data;
using BeautyStudio.Models;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyStudio.Services;

public class InterventionService
{
    public async Task<List<Intervention>> GetForCustomerAsync(int customerId)
    {
        await using var db = new AppDbContext();
        return await db.Set<Intervention>()
            .Include(x => x.Location)
            .Include(x => x.Type)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<List<Intervention>> GetUpcomingAsync()
    {
        await using var db = new AppDbContext();
        return await db.Set<Intervention>()
            .Include(x => x.Customer)
            .Include(x => x.Location)
            .Include(x => x.Type)
            .Where(x => x.Date >= DateTime.Today)
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task SaveAsync(Intervention intervention)
    {
        await using var db = new AppDbContext();
        if (intervention.Id == 0)
            db.Add(intervention);
        else
            db.Update(intervention);
        await db.SaveChangesAsync();
    }

    public async Task SaveAndDeleteAppointmentAsync(Intervention intervention, int appointmentId)
    {
        await using var db = new AppDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (intervention.Id == 0) db.Interventions.Add(intervention);
        else db.Interventions.Update(intervention);

        var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId);
        if (appointment == null)
            throw new InvalidOperationException("L'appuntamento da archiviare non è più presente.");

        db.Appointments.Remove(appointment);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task DeleteAsync(Intervention intervention)
    {
        await using var db = new AppDbContext();
        db.Remove(intervention);
        await db.SaveChangesAsync();
    }
}