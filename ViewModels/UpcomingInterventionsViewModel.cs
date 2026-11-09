using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BeautyStudio.ViewModels;

public partial class UpcomingInterventionsViewModel : ObservableObject
{
    private readonly AppointmentService _appointmentService = new();
    private readonly MainViewModel _mainViewModel;

    public ObservableCollection<Appointment> Appointments { get; } = new();

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? statusMessage;

    public bool HasAppointments => Appointments.Count > 0;

    public UpcomingInterventionsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        Appointments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAppointments));
    }

    [RelayCommand]
    private async Task EditAppointmentAsync(Appointment? appointment)
    {
        if (appointment?.Customer == null) return;
        var details = new CustomerDetailsViewModel(appointment.Customer, _mainViewModel, loadImmediately: false, returnToUpcomingAppointments: true);
        await details.LoadAndEditAppointmentAsync(appointment);
    }

    [RelayCommand]
    private async Task MarkAppointmentAsExecutedAsync(Appointment? appointment)
    {
        if (appointment?.Customer == null) return;
        // Dopo la conferma mostriamo la scheda cliente con l'intervento appena registrato.
        var details = new CustomerDetailsViewModel(appointment.Customer, _mainViewModel, loadImmediately: false, returnToUpcomingAppointments: false);
        await details.LoadAndMarkAppointmentAsExecutedAsync(appointment);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var items = await _appointmentService.GetUpcomingAsync();
            Appointments.Clear();
            foreach (var item in items)
                Appointments.Add(item);

            if (Appointments.Count == 0)
                StatusMessage = "Non ci sono appuntamenti programmati a partire da adesso.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore durante il caricamento degli appuntamenti: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}