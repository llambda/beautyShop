using MessageBox = BeautyStudio.Services.StyledMessageBox;
using BeautyStudio.Data;
using BeautyStudio.Models;
using BeautyStudio.Services;
using BeautyStudio.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
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
    private readonly CustomerService _customerService = new();
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
    [ObservableProperty] private int appointmentDurationMinutes = 60;
    public ObservableCollection<int> AppointmentDurationOptions { get; } = new() { 15, 30, 45, 60, 90, 120, 180 };
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
    public DateTime? NextAppointmentDate => Appointments
        .Where(x => x.Date >= DateTime.Now
            && !string.Equals(x.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(x.Status, "Non presentato", StringComparison.OrdinalIgnoreCase))
        .Select(x => (DateTime?)x.Date)
        .OrderBy(x => x)
        .FirstOrDefault();
    public ObservableCollection<Appointment> Appointments { get; } = new();
    public ObservableCollection<InterventionLocation> Locations { get; } = new();
    public ObservableCollection<InterventionType> Types { get; } = new();
    public ObservableCollection<string> BeforePhotos { get; } = new();
    public ObservableCollection<string> AfterPhotos { get; } = new();
    public ObservableCollection<CustomerTimelineEntry> Timeline { get; } = new();
    private System.Collections.Generic.List<CustomerActivity> _customerActivities = new();
    private System.Collections.Generic.List<InterventionHistoryEntry> _interventionHistory = new();
    public ObservableCollection<Intervention> FilteredInterventions { get; } = new();
    [ObservableProperty] private InterventionType? historyFilterType;
    [ObservableProperty] private InterventionLocation? historyFilterLocation;

    partial void OnHistoryFilterTypeChanged(InterventionType? value) => ApplyHistoryFilter();
    partial void OnHistoryFilterLocationChanged(InterventionLocation? value) => ApplyHistoryFilter();

    [RelayCommand]
    private void ClearHistoryFilters()
    {
        HistoryFilterType = null;
        HistoryFilterLocation = null;
        ApplyHistoryFilter();
    }

    private void ApplyHistoryFilter()
    {
        var filtered = Interventions.Where(item =>
            (HistoryFilterType == null || item.TypeId == HistoryFilterType.Id)
            && (HistoryFilterLocation == null || item.LocationId == HistoryFilterLocation.Id)
)
            .OrderByDescending(item => item.Date).ToList();
        FilteredInterventions.Clear();
        foreach (var item in filtered) FilteredInterventions.Add(item);
    }

    private void RebuildTimeline()
    {
        Timeline.Clear();
        foreach (var item in Appointments)
            Timeline.Add(new CustomerTimelineEntry(item.Date, "Appuntamento", item.Status,
                item.InterventionType?.Name ?? "Tipo non specificato", item.Location?.Name ?? "Luogo non specificato"));
        foreach (var item in Interventions)
            Timeline.Add(new CustomerTimelineEntry(item.Date, "Intervento", item.Type?.Name ?? "Tipo non specificato",
                item.Location?.Name ?? "Luogo non specificato", item.IsPaid ? "Pagato" : "Non pagato"));
        if (!_customerActivities.Any(x => x.Category == "Cliente"))
            Timeline.Add(new CustomerTimelineEntry(Customer.CreatedAt, "Cliente inserito", "Scheda cliente presente in Beauty Studio.", "Anagrafica cliente", string.Empty));
        foreach (var item in _customerActivities)
            Timeline.Add(new CustomerTimelineEntry(item.OccurredAt, item.Category, item.Detail, "Attività scheda cliente", string.Empty));
        foreach (var item in _interventionHistory)
            Timeline.Add(new CustomerTimelineEntry(item.ChangedAt, item.Action, item.Details, "Storico intervento", string.Empty));
        var ordered = Timeline.OrderByDescending(item => item.Date).ToList();
        Timeline.Clear();
        foreach (var item in ordered) Timeline.Add(item);
    }


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
        Appointments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(NextAppointmentDate));
        Interventions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TotalRevenue));
            OnPropertyChanged(nameof(TotalPaid));
            OnPropertyChanged(nameof(TotalUnpaid));
            OnPropertyChanged(nameof(InterventionCount));
            OnPropertyChanged(nameof(LastInterventionDate));
            ApplyHistoryFilter();
            RebuildTimeline();
        };
        Appointments.CollectionChanged += (_, _) => RebuildTimeline();
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
        InterventionPrice = 0m;
        InterventionIsPaid = false;
        BeforePhotos.Clear();
        AfterPhotos.Clear();
        SelectedType = appointment.InterventionTypeId.HasValue
            ? Types.FirstOrDefault(x => x.Id == appointment.InterventionTypeId.Value)
            : null;

        // Se l'appuntamento non ha un luogo, permette di crearlo subito senza interrompere la conferma.
        if (!appointment.LocationId.HasValue)
        {
            if (MessageBox.Show("Questo appuntamento non ha un luogo. Vuoi aggiungerlo adesso?",
                "Luogo mancante", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var locationDialog = new LocationEditorWindow(null) { Owner = Application.Current.MainWindow };
            if (locationDialog.ShowDialog() != true)
                return;

            var newLocation = new InterventionLocation
            {
                Name = locationDialog.LocationName,
                Address = locationDialog.LocationAddress,
                Notes = locationDialog.LocationNotes
            };
            await _locationService.SaveAsync(newLocation);
            await LoadLocationsOnlyAsync();
            SelectedLocation = Locations.FirstOrDefault(x => x.Id == newLocation.Id)
                ?? Locations.FirstOrDefault(x => string.Equals(x.Name, newLocation.Name, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            SelectedLocation = Locations.FirstOrDefault(x => x.Id == appointment.LocationId.Value);
        }

        if (SelectedLocation == null)
        {
            StatusMessage = "Seleziona o aggiungi un luogo prima di confermare lo svolgimento.";
            MessageBox.Show(StatusMessage, "Luogo mancante", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _convertingAppointmentToIntervention = true;
        IsConvertingAppointment = true;
        StatusMessage = SelectedType == null
            ? "Seleziona il tipo di intervento per completare la conferma. Puoi anche inserire prezzo, pagamento e fotografie."
            : appointment.LocationId.HasValue
                ? "Luogo e tipo sono stati recuperati dall'appuntamento. Puoi completare note, prezzo e fotografie."
                : "Nuovo luogo aggiunto e selezionato. Puoi completare prezzo, pagamento e fotografie.";
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
            await ReloadAuditHistoryAsync();
        }
        catch (Exception ex) { StatusMessage = $"Errore caricamento: {ex.Message}"; }
    }

    private async Task LoadLocationsOnlyAsync()
    {
        var items = await _locationService.GetAllAsync();
        Locations.Clear();
        foreach (var item in items) Locations.Add(item);
    }

    private async Task ReloadInterventionsAsync()
    {
        var items = await _interventionService.GetForCustomerAsync(Customer.Id);
        Interventions.Clear();
        foreach (var item in items) Interventions.Add(item);
        ApplyHistoryFilter();
        await ReloadAuditHistoryAsync();
    }

    private async Task ReloadAuditHistoryAsync()
    {
        await using var db = new AppDbContext();
        _customerActivities = await db.CustomerActivities.AsNoTracking()
            .Where(x => x.CustomerId == Customer.Id)
            .OrderByDescending(x => x.OccurredAt)
            .ToListAsync();
        _interventionHistory = await _interventionService.GetHistoryForCustomerAsync(Customer.Id);
        RebuildTimeline();
    }

    private async Task ReloadAppointmentsAsync()
    {
        var items = await _appointmentService.GetForCustomerAsync(Customer.Id);
        Appointments.Clear();
        foreach (var item in items) Appointments.Add(item);
        OnPropertyChanged(nameof(NextAppointmentDate));
        await ReloadAuditHistoryAsync();
    }

    [RelayCommand]
    private void NewAppointment()
    {
        SelectedAppointment = null;
        AppointmentDate = DateTime.Today;
        AppointmentDurationMinutes = 60;
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
        AppointmentDurationMinutes = item.DurationMinutes <= 0 ? 60 : item.DurationMinutes;
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
            item.DurationMinutes = AppointmentDurationMinutes;
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
    private async Task ConfirmAppointmentExecutedAsync(Appointment? appointment)
    {
        if (appointment == null) return;

        if (string.Equals(appointment.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
            || string.Equals(appointment.Status, "Non presentato", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Non puoi registrare lo svolgimento di un appuntamento annullato o con stato Non presentato.",
                "Appuntamento non confermabile", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Apre la schermata dedicata di registrazione: data, luogo, tipo e note sono precompilati;
        // prezzo e stato del pagamento vengono richiesti prima di creare l'intervento.
        await LoadAndMarkAppointmentAsExecutedAsync(appointment);
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync()
    {
        int appointmentCount = Appointments.Count;
        int interventionCount = Interventions.Count;
        string impact = $"Stai per eliminare {Customer.FirstName} {Customer.LastName}.\n\n"
            + $"Saranno eliminati definitivamente anche {appointmentCount} appuntamenti e {interventionCount} interventi, "
            + "le fotografie archiviate nell'app e lo storico collegato. Questa operazione non può essere annullata.\n\nContinuare?";
        if (MessageBox.Show(impact, "Eliminazione cliente e dati collegati", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var treatmentPhotos = Interventions
            .SelectMany(x => DeserializePhotos(x.BeforePhotosJson).Concat(DeserializePhotos(x.AfterPhotosJson)))
            .ToArray();
        string? customerPhoto = Customer.PhotoPath;
        string? consentPdf = Customer.ConsentFormPath;

        try
        {
            await _customerService.DeleteAsync(Customer.Id);
            DeleteRemovedPhotos(treatmentPhotos, Array.Empty<string>());
            TryDeleteManagedFile(customerPhoto, Path.Combine(AppDbContext.PhotosDirectory, "Customers"));
            TryDeleteManagedFile(consentPdf, AppDbContext.ConsentsDirectory);
            await _mainViewModel.RefreshCustomersAsync();
            _mainViewModel.ShowCustomers();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Eliminazione cliente non riuscita: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteAppointmentAsync(Appointment? appointment)
    {
        if (appointment == null) return;
        SelectedAppointment = appointment;
        if (MessageBox.Show($"Eliminare l'appuntamento del {SelectedAppointment.Date:dd/MM/yyyy} alle {SelectedAppointment.Date:HH:mm}?\nNon verrà creato alcun intervento nello storico.", "Elimina appuntamento", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
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
            if (SelectedAppointment.LocationId.HasValue)
                SelectedLocation = Locations.FirstOrDefault(x => x.Id == SelectedAppointment.LocationId.Value);
            if (SelectedAppointment.InterventionTypeId.HasValue)
                SelectedType = Types.FirstOrDefault(x => x.Id == SelectedAppointment.InterventionTypeId.Value);
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
            if (_returnToUpcomingAppointments && !convertedAppointment)
                _mainViewModel.ShowUpcomingAppointments();
            else
                _mainViewModel.ShowCustomerDetails(this);
        }
        catch (Exception ex) { StatusMessage = $"Salvataggio non riuscito: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task DeleteInterventionAsync(Intervention? intervention)
    {
        if (intervention == null) return;
        if (MessageBox.Show($"Eliminare l'intervento del {intervention.Date:dd/MM/yyyy}? Verranno eliminate anche le fotografie prima/dopo e lo storico delle modifiche collegato.", "Elimina intervento", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        var photosToDelete = DeserializePhotos(intervention.BeforePhotosJson)
            .Concat(DeserializePhotos(intervention.AfterPhotosJson)).ToArray();
        await _interventionService.DeleteAsync(intervention);
        DeleteRemovedPhotos(photosToDelete, Array.Empty<string>());
        await ReloadInterventionsAsync();
        if (SelectedIntervention?.Id == intervention.Id) SelectedIntervention = null;
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

    private static string[] DeserializePhotos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }

    private static void TryDeleteManagedFile(string? path, string managedRoot)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            string root = Path.GetFullPath(managedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch { }
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
public sealed class CustomerTimelineEntry
{
    public DateTime Date { get; }
    public string Category { get; }
    public string Detail { get; }
    public string PrimaryInfo { get; }
    public string SecondaryInfo { get; }

    public CustomerTimelineEntry(DateTime date, string category, string detail, string primaryInfo, string secondaryInfo)
    {
        Date = date;
        Category = category;
        Detail = detail;
        PrimaryInfo = primaryInfo;
        SecondaryInfo = secondaryInfo;
    }
}
