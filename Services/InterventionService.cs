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

    public async Task<List<Intervention>> GetAllAsync()
    {
        await using var db = new AppDbContext();
        return await db.Interventions.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Location)
            .Include(x => x.Type)
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
        await using var transaction = await db.Database.BeginTransactionAsync();
        var previous = intervention.Id == 0
            ? null
            : await db.Interventions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == intervention.Id);

        if (intervention.Id == 0)
            db.Add(intervention);
        else
            db.Update(intervention);

        await db.SaveChangesAsync();

        string action = previous == null ? "Intervento creato" : "Intervento modificato";
        string details = previous == null
            ? $"Intervento creato. Prezzo: {intervention.Price:C2}. Pagato: {(intervention.IsPaid ? "sì" : "no")}."
            : BuildChangeDetails(previous, intervention);
        db.InterventionHistory.Add(new InterventionHistoryEntry
        {
            InterventionId = intervention.Id,
            CustomerId = intervention.CustomerId,
            ChangedAt = DateTime.Now,
            Action = action,
            Details = details
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<List<InterventionHistoryEntry>> GetHistoryForCustomerAsync(int customerId)
    {
        await using var db = new AppDbContext();
        return await db.InterventionHistory.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    public async Task<List<InterventionHistoryEntry>> GetHistoryAsync(int interventionId)
    {
        await using var db = new AppDbContext();
        return await db.InterventionHistory.AsNoTracking()
            .Where(x => x.InterventionId == interventionId)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    private static string BuildChangeDetails(Intervention oldItem, Intervention newItem)
    {
        var changes = new List<string>();
        if (oldItem.Price != newItem.Price) changes.Add($"Prezzo: {oldItem.Price:C2} → {newItem.Price:C2}");
        if (oldItem.IsPaid != newItem.IsPaid)
            changes.Add(newItem.IsPaid ? "Pagamento registrato" : "Pagamento revocato");
        if (oldItem.Date != newItem.Date) changes.Add($"Data modificata: {newItem.Date:dd/MM/yyyy}");
        if (oldItem.LocationId != newItem.LocationId) changes.Add("Luogo modificato");
        if (oldItem.TypeId != newItem.TypeId) changes.Add("Tipo di intervento modificato");
        if (oldItem.Notes != newItem.Notes) changes.Add("Note aggiornate");
        if (oldItem.BeforePhotosJson != newItem.BeforePhotosJson || oldItem.AfterPhotosJson != newItem.AfterPhotosJson)
            changes.Add("Fotografie aggiornate");
        if (oldItem.PhotoMetadataJson != newItem.PhotoMetadataJson)
            changes.Add("Date o descrizioni delle fotografie aggiornate");
        return changes.Count == 0 ? "Dati intervento salvati." : string.Join("; ", changes);
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

        // Mantiene i dati dell'appuntamento quando presenti; in assenza usa le scelte fatte nella schermata di conferma.
        intervention.CustomerId = appointment.CustomerId;
        if (appointment.LocationId.HasValue)
            intervention.LocationId = appointment.LocationId.Value;
        else if (intervention.LocationId <= 0)
            throw new InvalidOperationException("Aggiungi un luogo prima di confermare lo svolgimento.");

        if (appointment.InterventionTypeId.HasValue)
            intervention.TypeId = appointment.InterventionTypeId.Value;
        else if (intervention.TypeId <= 0)
            throw new InvalidOperationException("Seleziona un tipo di intervento prima di confermare lo svolgimento.");

        db.Appointments.Remove(appointment);
        await db.SaveChangesAsync();
        db.InterventionHistory.Add(new InterventionHistoryEntry
        {
            InterventionId = intervention.Id,
            CustomerId = intervention.CustomerId,
            ChangedAt = DateTime.Now,
            Action = "Intervento creato da appuntamento",
            Details = $"Appuntamento del {intervention.Date:dd/MM/yyyy HH:mm} registrato come intervento. Prezzo: {intervention.Price:C2}. Pagato: {(intervention.IsPaid ? "sì" : "no")}."
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task DeleteAsync(Intervention intervention)
    {
        await using var db = new AppDbContext();
        var existing = await db.Interventions.FirstOrDefaultAsync(x => x.Id == intervention.Id);
        if (existing == null) return;
        // Lo storico collegato viene eliminato automaticamente insieme all'intervento (FK cascade).
        db.Interventions.Remove(existing);
        await db.SaveChangesAsync();
    }
}