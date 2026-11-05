using BeautyStudio.Data;
using BeautyStudio.Models;
using Microsoft.EntityFrameworkCore;

namespace BeautyStudio.Services;

public class InterventionTypeService
{
    public async Task<List<InterventionType>> GetAllAsync()
    {
        await using var db = new AppDbContext();
        return await db.InterventionTypes.AsNoTracking()
            .OrderBy(x => x.Name).ToListAsync();
    }

    public async Task SaveAsync(InterventionType type)
    {
        await using var db = new AppDbContext();
        if (type.Id == 0) db.InterventionTypes.Add(type);
        else db.InterventionTypes.Update(type);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = new AppDbContext();
        var type = await db.InterventionTypes.FindAsync(id);
        if (type is null) return;
        db.InterventionTypes.Remove(type);
        await db.SaveChangesAsync();
    }
}
