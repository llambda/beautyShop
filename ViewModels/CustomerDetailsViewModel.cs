using BeautyStudio.Data;
using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace BeautyStudio.ViewModels;

public partial class CustomerDetailsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly bool _returnToUpcomingAppointments;
    private bool _convertingAppointmentToIntervention;
    private readonly InterventionService _interventionService = new();
    private readonly AppointmentService _appointmentService = new();
    private readonly InterventionLocationService _locationService = new();
    private readonly InterventionTypeService _typeService = new();

    [ObservableProperty] private Customer customer;
    [ObservableProperty] private Intervention? selectedIntervention;
    [ObservableProperty] private DateTime interventionDate = DateTime.Today;
    [ObservableProperty] private InterventionLocation? selectedLocation;
    [ObservableProperty] private InterventionType? selectedType;
    [ObservableProperty] private string? interventionNotes;
    [ObservableProperty] private decimal interventionPrice;
    [ObservableProperty] private bool interventionIsPaid;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private Appointment? selectedAppointment;
    [ObservableProperty] private DateTime appointmentDate = DateTime.Today;
    [ObservableProperty] private string appointmentTime = DateTime.Now.ToString("HH:mm");
    [ObservableProperty] private string? appointmentNotes;
    [ObservableProperty] private InterventionType? selectedAppointmentType;

    public ObservableCollection<Intervention> Interventions { get; } = new();
    public decimal TotalRevenue => Interventions.Sum(x => x.Price);
    public decimal TotalPaid => Interventions.Where(x => x.IsPaid).Sum(x => x.Price);
    public decimal TotalUnpaid => Interventions.Where(x => !x.IsPaid).Sum(x => x.Price);
    public int InterventionCount => Interventions.Count;
    public DateTime? LastInterventionDate => Interventions.Count == 0 ? null : Interventions.Max(x => x.Date);
    public ObservableCollection<Appointment> Appointments { get; } = new();
    public ObservableCollection<InterventionLocation> Locations { get; } = new();
    public ObservableCollection<InterventionType> Types { get; } = new();
    public ObservableCollection<string> BeforePhotos { get; } = new();
    public ObservableCollection<string> AfterPhotos { get; } = new();

    public CustomerDetailsViewModel(Customer customer, MainViewModel mainViewModel, bool loadImmediately = true, bool returnToUpcomingAppointments = false)
    {
        Customer = customer;
        Interventions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TotalRevenue));
            OnPropertyChanged(nameof(TotalPaid));
            OnPropertyChanged(nameof(TotalUnpaid));
            OnPropertyChanged(nameof(InterventionCount));
            OnPropertyChanged(nameof(LastInterventionDate));
        };
        _mainViewModel = mainViewModel;
        _returnToUpcomingAppointments = returnToUpcomingAppointments;
        if (loadImmediately) _ = LoadAsync();
    }

    public async Task LoadAndEditAppointmentAsync(Appointment appointment)
    {
        await LoadAsync();
        EditAppointment(appointment);
    }

    public async Task LoadAndMarkAppointmentAsExecutedAsync(Appointment appointment)
    {
        await LoadAsync();
        SelectedAppointment = appointment;
        SelectedIntervention = null;
        InterventionDate = appointment.Date.Date;
        SelectedLocation = null;
        SelectedType = Types.FirstOrDefault(x => x.Id == appointment.InterventionTypeId);
        InterventionNotes = appointment.Notes;
        BeforePhotos.Clear();
        AfterPhotos.Clear();
        _convertingAppointmentToIntervention = true;
        StatusMessage = Locations.Count == 0
            ? "Prima di registrare l'intervento, aggiungi un luogo nelle Impostazioni."
            : "Completa il luogo e, se necessario, aggiungi le foto prima e dopo. Salvando, l'appuntamento verrà archiviato come intervento eseguito.";
        _mainViewModel.ShowInterventionEditor(this);
    }

    private void ReturnAfterAppointmentEdit()
    {
        if (_returnToUpcomingAppointments)
            _mainViewModel.ShowUpcomingAppointments();
        else
            _mainViewModel.ShowCustomerDetails(this);
    }

    private async Task LoadAsync()
    {
        try
        {
            var locations = await _locationService.GetAllAsync();
            var types = await _typeService.GetAllAsync();
            Locations.Clear();
            Types.Clear();
            foreach (var item in locations) Locations.Add(item);
            foreach (var item in types) Types.Add(item);
            await ReloadInterventionsAsync();
            await ReloadAppointmentsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Errore caricamento: {ex.Message}"; }
    }

    private async Task ReloadInterventionsAsync()
    {
        var items = await _interventionService.GetForCustomerAsync(Customer.Id);
        Interventions.Clear();
        foreach (var item in items) Interventions.Add(item);
    }

    private async Task ReloadAppointmentsAsync()
    {
        var items = await _appointmentService.GetForCustomerAsync(Customer.Id);
        Appointments.Clear();
        foreach (var item in items) Appointments.Add(item);
    }

    [RelayCommand]
    private void NewAppointment()
    {
        SelectedAppointment = null;
        AppointmentDate = DateTime.Today;
        AppointmentTime = DateTime.Now.ToString("HH:mm");
        AppointmentNotes = string.Empty;
        SelectedAppointmentType = Types.FirstOrDefault();
        StatusMessage = null;
        _mainViewModel.ShowAppointmentEditor(this);
    }

    [RelayCommand]
    private void EditAppointment(Appointment? item)
    {
        if (item == null) return;
        SelectedAppointment = item;
        AppointmentDate = item.Date.Date;
        AppointmentTime = item.Date.ToString("HH:mm");
        AppointmentNotes = item.Notes;
        SelectedAppointmentType = Types.FirstOrDefault(x => x.Id == item.InterventionTypeId);
        StatusMessage = null;
        _mainViewModel.ShowAppointmentEditor(this);
    }

    [RelayCommand]
    private async Task SaveAppointmentAsync()
    {
        if (SelectedAppointmentType == null)
        {
            StatusMessage = "Seleziona il tipo di intervento per l'appuntamento.";
            return;
        }

        try
        {
            var item = SelectedAppointment ?? new Appointment { CustomerId = Customer.Id };
            if (!TimeSpan.TryParse(AppointmentTime, out var time))
            {
                StatusMessage = "Inserisci un orario valido (HH:mm).";
                return;
            }
            item.Date = AppointmentDate.Date.Add(time);
            item.Notes = AppointmentNotes?.Trim();
            item.InterventionTypeId = SelectedAppointmentType?.Id;
            await _appointmentService.SaveAsync(item);
            await ReloadAppointmentsAsync();
            SelectedAppointment = null;
            StatusMessage = "Appuntamento salvato.";
            ReturnAfterAppointmentEdit();
        }
        catch (Exception ex) { StatusMessage = $"Salvataggio appuntamento non riuscito: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task DeleteAppointmentAsync()
    {
        if (SelectedAppointment == null) return;
        if (MessageBox.Show("Eliminare questo appuntamento?", "Conferma", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        await _appointmentService.DeleteAsync(SelectedAppointment);
        await ReloadAppointmentsAsync();
        SelectedAppointment = null;
        StatusMessage = "Appuntamento eliminato.";
        ReturnAfterAppointmentEdit();
    }

    [RelayCommand]
    private void NewIntervention()
    {
        SelectedIntervention = null;
        InterventionDate = DateTime.Today;
        SelectedLocation = Locations.FirstOrDefault();
        SelectedType = Types.FirstOrDefault();
        InterventionNotes = string.Empty;
        InterventionPrice = 0;
        InterventionIsPaid = false;
        BeforePhotos.Clear();
        AfterPhotos.Clear();
        StatusMessage = null;
        _mainViewModel.ShowInterventionEditor(this);
    }

    [RelayCommand]
    private void EditIntervention(Intervention? item)
    {
        if (item == null) return;
        SelectedIntervention = item;
        InterventionDate = item.Date;
        SelectedLocation = Locations.FirstOrDefault(x => x.Id == item.LocationId);
        SelectedType = Types.FirstOrDefault(x => x.Id == item.TypeId);
        InterventionNotes = item.Notes;
        InterventionPrice = item.Price;
        InterventionIsPaid = item.IsPaid;
        SetPhotos(BeforePhotos, item.BeforePhotosJson);
        SetPhotos(AfterPhotos, item.AfterPhotosJson);
        StatusMessage = null;
        _mainViewModel.ShowInterventionEditor(this);
    }

    [RelayCommand]
    private async Task SaveInterventionAsync()
    {
        if (SelectedLocation == null || SelectedType == null)
        {
            StatusMessage = "Seleziona luogo e tipo di intervento nelle Impostazioni.";
            return;
        }

        try
        {
            var item = SelectedIntervention ?? new Intervention { CustomerId = Customer.Id };
            item.Date = InterventionDate;
            item.LocationId = SelectedLocation.Id;
            item.TypeId = SelectedType.Id;
            item.Notes = InterventionNotes?.Trim();
            item.Price = InterventionPrice;
            item.IsPaid = InterventionIsPaid;
            item.PaidAt = InterventionIsPaid ? (item.PaidAt ?? DateTime.Now) : null;
            item.BeforePhotosJson = JsonSerializer.Serialize(BeforePhotos.ToArray());
            item.AfterPhotosJson = JsonSerializer.Serialize(AfterPhotos.ToArray());
            await _interventionService.SaveAsync(item);
            await ReloadInterventionsAsync();

            bool convertedAppointment = _convertingAppointmentToIntervention;
            if (convertedAppointment && SelectedAppointment != null)
            {
                await _appointmentService.DeleteAsync(SelectedAppointment);
                await ReloadAppointmentsAsync();
            }

            SelectedIntervention = null;
            SelectedAppointment = null;
            _convertingAppointmentToIntervention = false;
            StatusMessage = convertedAppointment ? "Appuntamento registrato nello storico degli interventi." : "Intervento salvato.";
            if (_returnToUpcomingAppointments)
                _mainViewModel.ShowUpcomingAppointments();
            else
                _mainViewModel.ShowCustomerDetails(this);
        }
        catch (Exception ex) { StatusMessage = $"Salvataggio non riuscito: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task DeleteInterventionAsync()
    {
        if (SelectedIntervention == null) return;
        if (MessageBox.Show("Eliminare questo intervento?", "Conferma", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        await _interventionService.DeleteAsync(SelectedIntervention);
        await ReloadInterventionsAsync();
        SelectedIntervention = null;
        StatusMessage = "Intervento eliminato.";
        _mainViewModel.ShowCustomerDetails(this);
    }

    [RelayCommand]
    private void AddBeforePhotos() => AddPhotos(BeforePhotos);

    [RelayCommand]
    private void AddAfterPhotos() => AddPhotos(AfterPhotos);

    [RelayCommand]
    private void RemoveBeforePhoto(string? path) { if (path != null) BeforePhotos.Remove(path); }

    [RelayCommand]
    private void RemoveAfterPhoto(string? path) { if (path != null) AfterPhotos.Remove(path); }

    private void AddPhotos(ObservableCollection<string> destination)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona fotografie",
            Filter = "Immagini|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;

        string folder = Path.Combine(AppDbContext.PhotosDirectory, "Treatments", Customer.Id.ToString());
        Directory.CreateDirectory(folder);
        foreach (string source in dialog.FileNames)
        {
            string target = Path.Combine(folder, $"{Guid.NewGuid()}{Path.GetExtension(source)}");
            File.Copy(source, target, true);
            destination.Add(target);
        }
    }

    private static void SetPhotos(ObservableCollection<string> target, string? json)
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            foreach (var path in JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>())
                target.Add(path);
        }
        catch { }
    }

    [RelayCommand]
    private void CancelEditor()
    {
        if (_convertingAppointmentToIntervention)
        {
            _convertingAppointmentToIntervention = false;
            SelectedAppointment = null;
        }
        ReturnAfterAppointmentEdit();
    }


    [RelayCommand]
    private void ExportCustomerCsv()
    {
        try
        {
            var path = new ExportService().ExportCustomerCsv(Customer, Interventions);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            StatusMessage = "Storico esportato in CSV.";
        }
        catch (Exception ex) { StatusMessage = $"Esportazione CSV non riuscita: {ex.Message}"; }
    }

    [RelayCommand]
    private void ExportCustomerPdf()
    {
        try
        {
            var path = new ExportService().ExportCustomerPdf(Customer, Interventions);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            StatusMessage = "Scheda esportata in PDF.";
        }
        catch (Exception ex) { StatusMessage = $"Esportazione PDF non riuscita: {ex.Message}"; }
    }

    [RelayCommand]
    private void OpenConsentForm()
    {
        if (string.IsNullOrWhiteSpace(Customer.ConsentFormPath) || !File.Exists(Customer.ConsentFormPath))
        {
            MessageBox.Show("Per questo cliente non è disponibile un PDF del consenso informato.", "Consenso informato", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(Customer.ConsentFormPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossibile aprire il PDF: {ex.Message}", "Consenso informato", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void BackToCustomers() => _mainViewModel.ShowCustomers();
}