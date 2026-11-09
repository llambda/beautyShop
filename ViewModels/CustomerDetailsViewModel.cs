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
    [ObservableProperty] private bool isConvertingAppointment;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private Appointment? selectedAppointment;
    [ObservableProperty] private DateTime appointmentDate = DateTime.Today;
    [ObservableProperty] private string appointmentTime = DateTime.Now.ToString("HH:mm");
    [ObservableProperty] private string? appointmentNotes;
    [ObservableProperty] private string selectedAppointmentStatus = "Da confermare";
    public ObservableCollection<string> AppointmentStatuses { get; } = new() { "Da confermare", "Confermato", "Annullato", "Non presentato" };
    [ObservableProperty] private InterventionType? selectedAppointmentType;
    [ObservableProperty] private InterventionLocation? selectedAppointmentLocation;

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

    partial void OnSelectedInterventionChanged(Intervention? value)
    {
        if (value == null)
        {
            BeforePhotos.Clear();
            AfterPhotos.Clear();
            return;
        }
        SetPhotos(BeforePhotos, value.BeforePhotosJson);
        SetPhotos(AfterPhotos, value.AfterPhotosJson);
    }

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
        SelectedLocation = Locations.FirstOrDefault(x => x.Id == appointment.LocationId);
        SelectedType = Types.FirstOrDefault(x => x.Id == appointment.InterventionTypeId);
        InterventionNotes = appointment.Notes;
        BeforePhotos.Clear();
        AfterPhotos.Clear();
        if (appointment.LocationId == null || appointment.InterventionTypeId == null)
        {
            StatusMessage = "Questo appuntamento non contiene luogo e tipo di intervento. Modificalo prima di confermare lo svolgimento.";
            MessageBox.Show(StatusMessage, "Dati appuntamento mancanti", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedLocation = Locations.FirstOrDefault(x => x.Id == appointment.LocationId);
        SelectedType = Types.FirstOrDefault(x => x.Id == appointment.InterventionTypeId);
        if (SelectedLocation == null || SelectedType == null)
        {
            StatusMessage = "Il luogo o il tipo associato all'appuntamento non è più disponibile nelle Impostazioni.";
            MessageBox.Show(StatusMessage, "Dati appuntamento non disponibili", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _convertingAppointmentToIntervention = true;
        IsConvertingAppointment = true;
        StatusMessage = "Luogo e tipo sono già stati recuperati dall'appuntamento. Puoi completare note, prezzo e fotografie.";
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
        SelectedAppointmentStatus = "Da confermare";
        SelectedAppointmentType = Types.FirstOrDefault();
        SelectedAppointmentLocation = Locations.FirstOrDefault();
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
        SelectedAppointmentStatus = string.IsNullOrWhiteSpace(item.Status) ? "Da confermare" : item.Status;
        SelectedAppointmentType = Types.FirstOrDefault(x => x.Id == item.InterventionTypeId);
        SelectedAppointmentLocation = Locations.FirstOrDefault(x => x.Id == item.LocationId);
        StatusMessage = null;
        _mainViewModel.ShowAppointmentEditor(this);
    }

    [RelayCommand]
    private async Task SaveAppointmentAsync()
    {
        if (SelectedAppointmentType == null || SelectedAppointmentLocation == null)
        {
            StatusMessage = "Seleziona il luogo e il tipo di intervento per l'appuntamento.";
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
            item.Status = SelectedAppointmentStatus;
            item.InterventionTypeId = SelectedAppointmentType.Id;
            item.LocationId = SelectedAppointmentLocation.Id;
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
    private void OpenInterventionDetails(Intervention? item)
    {
        if (item == null) return;
        _mainViewModel.ShowInterventionPhotos(this, item);
    }

    public void EditInterventionFromDetails(Intervention item) => EditIntervention(item);

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
        if (_convertingAppointmentToIntervention && SelectedAppointment != null)
        {
            SelectedLocation = Locations.FirstOrDefault(x => x.Id == SelectedAppointment.LocationId);
            SelectedType = Types.FirstOrDefault(x => x.Id == SelectedAppointment.InterventionTypeId);
        }

        if (SelectedLocation == null || SelectedType == null)
        {
            StatusMessage = "Il luogo o il tipo di intervento associato all'appuntamento non è disponibile. Verifica le Impostazioni.";
            return;
        }
        if (InterventionPrice < 0)
        {
            StatusMessage = "Il prezzo non può essere negativo.";
            return;
        }

        try
        {
            var item = SelectedIntervention ?? new Intervention { CustomerId = Customer.Id };
            item.CustomerId = Customer.Id;
            var oldBeforePhotos = SelectedIntervention == null ? Array.Empty<string>() : DeserializePhotos(SelectedIntervention.BeforePhotosJson);
            var oldAfterPhotos = SelectedIntervention == null ? Array.Empty<string>() : DeserializePhotos(SelectedIntervention.AfterPhotosJson);
            item.Date = InterventionDate;
            item.LocationId = SelectedLocation.Id;
            item.TypeId = SelectedType.Id;
            item.Notes = InterventionNotes?.Trim();
            item.Price = InterventionPrice;
            item.IsPaid = InterventionIsPaid;
            item.PaidAt = InterventionIsPaid ? (item.PaidAt ?? DateTime.Now) : null;
            item.BeforePhotosJson = JsonSerializer.Serialize(BeforePhotos.ToArray());
            item.AfterPhotosJson = JsonSerializer.Serialize(AfterPhotos.ToArray());
            bool convertedAppointment = _convertingAppointmentToIntervention;
            if (convertedAppointment)
            {
                if (SelectedAppointment == null)
                    throw new InvalidOperationException("Non è stato possibile recuperare l'appuntamento da confermare.");

                await _interventionService.SaveAndDeleteAppointmentAsync(item, SelectedAppointment.Id);
                await ReloadAppointmentsAsync();
            }
            else
            {
                await _interventionService.SaveAsync(item);
            }
            await ReloadInterventionsAsync();
            await _mainViewModel.RefreshCustomersAsync();
            DeleteRemovedPhotos(oldBeforePhotos, BeforePhotos);
            DeleteRemovedPhotos(oldAfterPhotos, AfterPhotos);

            SelectedIntervention = null;
            SelectedAppointment = null;
            _convertingAppointmentToIntervention = false;
            IsConvertingAppointment = false;
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
        var photosToDelete = DeserializePhotos(SelectedIntervention.BeforePhotosJson)
            .Concat(DeserializePhotos(SelectedIntervention.AfterPhotosJson)).ToArray();
        await _interventionService.DeleteAsync(SelectedIntervention);
        DeleteRemovedPhotos(photosToDelete, Array.Empty<string>());
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

    private static string[] DeserializePhotos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }

    private static void DeleteRemovedPhotos(System.Collections.Generic.IEnumerable<string> oldPaths, System.Collections.Generic.IEnumerable<string> retainedPaths)
    {
        var retained = new System.Collections.Generic.HashSet<string>(retainedPaths, StringComparer.OrdinalIgnoreCase);
        string root = Path.GetFullPath(Path.Combine(AppDbContext.PhotosDirectory, "Treatments")) + Path.DirectorySeparatorChar;
        foreach (var path in oldPaths.Where(p => !retained.Contains(p)))
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch { }
        }
    }

    [RelayCommand]
    private void OpenPhoto(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { StatusMessage = "Impossibile aprire la fotografia: " + ex.Message; }
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
            IsConvertingAppointment = false;
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
    private async Task EditCustomerAsync()
    {
        if (_mainViewModel.ShowEditCustomerDialog(Customer) == true)
        {
            var updated = await new CustomerService().GetByIdAsync(Customer.Id);
            if (updated != null) Customer = updated;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private void BackToCustomers() => _mainViewModel.ShowCustomers();
}