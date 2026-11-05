using BeautyStudio.Data;
using BeautyStudio.Models;
using Microsoft.EntityFrameworkCore;

namespace BeautyStudio.Services;

public class InterventionLocationService
{
    public async Task<List<InterventionLocation>> GetAllAsync()
    {
        await using var db = new AppDbContext();
        return await db.InterventionLocations.AsNoTracking()
            .OrderBy(x => x.Name).ToListAsync();
    }

    public async Task SaveAsync(InterventionLocation location)
    {
        await using var db = new AppDbContext();
        if (location.Id == 0)
            db.InterventionLocations.Add(location);
        else
            db.InterventionLocations.Update(location);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = new AppDbContext();
        var location = await db.InterventionLocations.FindAsync(id);
        if (location is null) return;
        db.InterventionLocations.Remove(location);
        await db.SaveChangesAsync();
    }
}
