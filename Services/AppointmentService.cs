using BeautyStudio.Data;
using BeautyStudio.Models;
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
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task SaveAsync(Appointment appointment)
    {
        await using var db = new AppDbContext();
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
