using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyStudio.ViewModels;

public partial class RemindersViewModel : ObservableObject
{
    private readonly AppointmentService _appointmentService = new();
    public ObservableCollection<Appointment> TodayAppointments { get; } = new();
    public ObservableCollection<Appointment> TomorrowAppointments { get; } = new();
    public ObservableCollection<Appointment> UnconfirmedAppointments { get; } = new();
    public ObservableCollection<Appointment> IncompleteAppointments { get; } = new();
    public ObservableCollection<Appointment> MissingConsentAppointments { get; } = new();

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
            Replace(MissingConsentAppointments, active.Where(x => string.IsNullOrWhiteSpace(x.Customer?.ConsentFormPath)).OrderBy(x => x.Date));
            StatusMessage = "Promemoria aggiornati.";
        }
        catch (Exception ex) { StatusMessage = $"Errore nel caricamento dei promemoria: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static void Replace(ObservableCollection<Appointment> target, System.Collections.Generic.IEnumerable<Appointment> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }
}
