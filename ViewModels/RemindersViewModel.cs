using BeautyStudio.Models;
using BeautyStudio.Data;
using Microsoft.EntityFrameworkCore;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace BeautyStudio.ViewModels;

public partial class RemindersViewModel : ObservableObject
{
    private readonly AppointmentService _appointmentService = new();
    private readonly InterventionService _interventionService = new();
    public ObservableCollection<Appointment> TodayAppointments { get; } = new();
    public ObservableCollection<Appointment> TomorrowAppointments { get; } = new();
    public ObservableCollection<Appointment> UnconfirmedAppointments { get; } = new();
    public ObservableCollection<Appointment> IncompleteAppointments { get; } = new();
    public ObservableCollection<Appointment> MissingConsentAppointments { get; } = new();
    public ObservableCollection<Appointment> OverdueAppointments { get; } = new();
    public ObservableCollection<Appointment> NoShowAppointments { get; } = new();
    public ObservableCollection<CustomerFollowUpItem> CustomersToReconnect { get; } = new();

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? statusMessage;

    public RemindersViewModel() { }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var all = await _appointmentService.GetAllAsync();
            var active = all.Where(x => x.Date.Date >= DateTime.Today
                                     && !string.Equals(x.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
                                     && !string.Equals(x.Status, "Non presentato", StringComparison.OrdinalIgnoreCase)).ToList();
            Replace(TodayAppointments, active.Where(x => x.Date.Date == DateTime.Today).OrderBy(x => x.Date));
            Replace(TomorrowAppointments, active.Where(x => x.Date.Date == DateTime.Today.AddDays(1)).OrderBy(x => x.Date));
            Replace(UnconfirmedAppointments, active.Where(x => string.Equals(x.Status, "Da confermare", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Date));
            Replace(IncompleteAppointments, active.Where(x => x.LocationId == null || x.InterventionTypeId == null).OrderBy(x => x.Date));
            Replace(MissingConsentAppointments, active.Where(HasMissingConsent).OrderBy(x => x.Date));
            Replace(OverdueAppointments, all.Where(x => x.Date < DateTime.Now
                && !string.Equals(x.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x.Status, "Non presentato", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Date));
            Replace(NoShowAppointments, all.Where(x => string.Equals(x.Status, "Non presentato", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Date).Take(30));

            var interventions = await _interventionService.GetAllAsync();
            List<Customer> customers;
            await using (var db = new AppDbContext())
                customers = await db.Customers.AsNoTracking().ToListAsync();

            var futureCustomerIds = active.Where(x => x.Date >= DateTime.Now).Select(x => x.CustomerId).ToHashSet();
            var followUps = customers
                .Where(customer => !futureCustomerIds.Contains(customer.Id))
                .Select(customer =>
                {
                    var last = interventions.Where(x => x.CustomerId == customer.Id)
                        .Select(x => (DateTime?)x.Date).Max();
                    var referenceDate = last ?? customer.CreatedAt;
                    return new CustomerFollowUpItem(customer, last, (DateTime.Today - referenceDate.Date).Days);
                })
                .Where(x => x.DaysSinceLastIntervention >= 90)
                .OrderByDescending(x => x.DaysSinceLastIntervention)
                .ToList();
            Replace(CustomersToReconnect, followUps);
            StatusMessage = "Promemoria aggiornati.";
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Promemoria", ex);
            StatusMessage = $"Errore nel caricamento dei promemoria: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private static bool HasMissingConsent(Appointment appointment)
    {
        string? path = appointment.Customer?.ConsentFormPath;
        return string.IsNullOrWhiteSpace(path) || !File.Exists(path);
    }

    private static void Replace(ObservableCollection<Appointment> target, System.Collections.Generic.IEnumerable<Appointment> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }
}


public sealed record CustomerFollowUpItem(Customer Customer, DateTime? LastInterventionDate, int DaysSinceLastIntervention)
{
    public string LastInterventionLabel => LastInterventionDate.HasValue
        ? LastInterventionDate.Value.ToString("dd/MM/yyyy")
        : "Nessun intervento registrato";
}
