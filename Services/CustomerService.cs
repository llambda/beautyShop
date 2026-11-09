
using BeautyStudio.Data;
using BeautyStudio.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

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
        db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = customer.Id,
            OccurredAt = DateTime.Now,
            Category = "Cliente",
            Detail = "Scheda cliente creata."
        });
        await db.SaveChangesAsync();

        return customer.Id;
    }

    public async Task UpdateAsync(Customer customer)
    {
        await using var db = new AppDbContext();

        var previous = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == customer.Id);
        db.Customers.Update(customer);
        await db.SaveChangesAsync();

        if (previous != null)
        {
            var changes = new List<string>();
            if (previous.FirstName != customer.FirstName || previous.LastName != customer.LastName) changes.Add("Nome");
            if (previous.BirthDate != customer.BirthDate) changes.Add("Data di nascita");
            if (previous.Phone != customer.Phone) changes.Add("Telefono");
            if (previous.Email != customer.Email) changes.Add("Email");
            if (previous.Address != customer.Address) changes.Add("Indirizzo");
            if (previous.Notes != customer.Notes) changes.Add("Note");
            if (previous.Allergies != customer.Allergies) changes.Add("Allergie");
            if (previous.Sensitivities != customer.Sensitivities) changes.Add("Sensibilità");
            if (previous.ConsentFormPath != customer.ConsentFormPath || previous.ConsentAcquiredAt != customer.ConsentAcquiredAt) changes.Add("Consenso informato");
            if (previous.PhotoPath != customer.PhotoPath) changes.Add("Fotografia profilo");
            if (changes.Count > 0)
            {
                db.CustomerActivities.Add(new CustomerActivity
                {
                    CustomerId = customer.Id,
                    OccurredAt = DateTime.Now,
                    Category = "Scheda cliente modificata",
                    Detail = "Campi aggiornati: " + string.Join(", ", changes) + "."
                });
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = new AppDbContext();

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
            return;

        db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = id,
            OccurredAt = DateTime.Now,
            Category = "Cliente eliminato",
            Detail = "La scheda cliente e i dati collegati sono stati eliminati."
        });
        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
    }
}
