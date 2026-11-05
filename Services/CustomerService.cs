
using BeautyStudio.Data;
using BeautyStudio.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BeautyStudio.Services;

public class CustomerService
{
    public async Task<List<Customer>> GetAllAsync()
    {
        await using var db = new AppDbContext();

        return await db.Customers
            .AsNoTracking()
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await using var db = new AppDbContext();

        return await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<int> AddAsync(Customer customer)
    {
        await using var db = new AppDbContext();

        db.Customers.Add(customer);

        await db.SaveChangesAsync();

        return customer.Id;
    }

    public async Task UpdateAsync(Customer customer)
    {
        await using var db = new AppDbContext();

        db.Customers.Update(customer);

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = new AppDbContext();

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
            return;

        db.Customers.Remove(customer);

        await db.SaveChangesAsync();
    }
}
