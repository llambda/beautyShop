using BeautyStudio.Data;
using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace BeautyStudio.ViewModels;

public partial class CustomerDetailsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
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
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private Appointment? selectedAppointment;
    [ObservableProperty] private DateTime appointmentDate = DateTime.Now;
    [ObservableProperty] private string? appointmentNotes;

    public ObservableCollection<Intervention> Interventions { get; } = new();
    public ObservableCollection<Appointment> Appointments { get; } = new();
    public ObservableCollection<InterventionLocation> Locations { get; } = new();
    public ObservableCollection<InterventionType> Types { get; } = new();
    public ObservableCollection<string> BeforePhotos { get; } = new();
    public ObservableCollection<string> AfterPhotos { get; } = new();

    public CustomerDetailsViewModel(Customer customer, MainViewModel mainViewModel)
    {
        Customer = customer;
        _mainViewModel = mainViewModel;
        _ = LoadAsync();
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
        AppointmentDate = DateTime.Now;
        AppointmentNotes = string.Empty;
    }

    [RelayCommand]
    private void EditAppointment(Appointment? item)
    {
        if (item == null) return;
        SelectedAppointment = item;
        AppointmentDate = item.Date;
        AppointmentNotes = item.Notes;
    }

    [RelayCommand]
    private async Task SaveAppointmentAsync()
    {
        try
        {
            var item = SelectedAppointment ?? new Appointment { CustomerId = Customer.Id };
            item.Date = AppointmentDate;
            item.Notes = AppointmentNotes?.Trim();
            await _appointmentService.SaveAsync(item);
            await ReloadAppointmentsAsync();
            NewAppointment();
            StatusMessage = "Appuntamento salvato.";
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
        NewAppointment();
        StatusMessage = "Appuntamento eliminato.";
    }

    [RelayCommand]
    private void NewIntervention()
    {
        SelectedIntervention = null;
        InterventionDate = DateTime.Today;
        SelectedLocation = Locations.FirstOrDefault();
        SelectedType = Types.FirstOrDefault();
        InterventionNotes = string.Empty;
        BeforePhotos.Clear();
        AfterPhotos.Clear();
        StatusMessage = null;
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
        SetPhotos(BeforePhotos, item.BeforePhotosJson);
        SetPhotos(AfterPhotos, item.AfterPhotosJson);
        StatusMessage = null;
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
            item.BeforePhotosJson = JsonSerializer.Serialize(BeforePhotos.ToArray());
            item.AfterPhotosJson = JsonSerializer.Serialize(AfterPhotos.ToArray());
            await _interventionService.SaveAsync(item);
            await ReloadInterventionsAsync();
            SelectedIntervention = null;
            StatusMessage = "Intervento salvato.";
            NewIntervention();
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
        NewIntervention();
        StatusMessage = "Intervento eliminato.";
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
    private void BackToCustomers() => _mainViewModel.ShowCustomers();
}