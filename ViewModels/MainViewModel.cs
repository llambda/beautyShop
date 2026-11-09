
using BeautyStudio.Models;
using BeautyStudio.Data;
using Microsoft.EntityFrameworkCore;
using System;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace BeautyStudio.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CustomerService _customerService;
    private readonly CustomerDialogService _dialogService;
    private readonly SettingsViewModel _settingsViewModel;
    private CalendarViewModel? _calendarViewModel;
    private UpcomingInterventionsViewModel? _upcomingInterventionsViewModel;
    private readonly DashboardViewModel _dashboardViewModel;

    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<Customer> FilteredCustomers { get; } = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty] private string treatmentFilter = string.Empty;
    [ObservableProperty] private string selectedSortOption = "Cognome A-Z";
    [ObservableProperty] private string selectedConsentFilter = "Tutti";
    [ObservableProperty] private DateTime? lastInterventionFrom;
    [ObservableProperty] private DateTime? lastInterventionTo;

    public ObservableCollection<string> SortOptions { get; } = new() { "Cognome A-Z", "Nome A-Z", "Ultimo intervento recente", "Ultimo intervento meno recente" };
    public ObservableCollection<string> ConsentFilters { get; } = new() { "Tutti", "Consenso presente", "Consenso mancante" };

    private bool _isMenuExpanded = true;

    public bool IsMenuExpanded
    {
        get => _isMenuExpanded;
        set
        {
            if (SetProperty(ref _isMenuExpanded, value))
                MenuWidth = value
                    ? new GridLength(220)
                    : new GridLength(72);
        }
    }

    private GridLength _menuWidth = new(220);

    public GridLength MenuWidth
    {
        get => _menuWidth;
        set => SetProperty(ref _menuWidth, value);
    }

    private string _selectedSection = "Clienti";

    public string SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private object? _currentViewModel;

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public MainViewModel(
        CustomerService customerService,
        CustomerDialogService dialogService,
        SettingsViewModel settingsViewModel)
    {
        _customerService = customerService;
        _dialogService = dialogService;
        _settingsViewModel = settingsViewModel;
        _dashboardViewModel = new DashboardViewModel();
        Customers.CollectionChanged += (_, _) => ApplyCustomerFilter();

        // All'avvio mostriamo direttamente la lista clienti.
        CurrentViewModel = this;
    }

    partial void OnSearchTextChanged(string value) => ApplyCustomerFilter();
    partial void OnTreatmentFilterChanged(string value) => ApplyCustomerFilter();
    partial void OnSelectedSortOptionChanged(string value) => ApplyCustomerFilter();
    partial void OnSelectedConsentFilterChanged(string value) => ApplyCustomerFilter();
    partial void OnLastInterventionFromChanged(DateTime? value) => ApplyCustomerFilter();
    partial void OnLastInterventionToChanged(DateTime? value) => ApplyCustomerFilter();

    private void ApplyCustomerFilter()
    {
        string term = SearchText?.Trim() ?? string.Empty;
        string treatment = TreatmentFilter?.Trim() ?? string.Empty;
        var filtered = Customers.Where(customer =>
            (string.IsNullOrWhiteSpace(term)
             || $"{customer.FirstName} {customer.LastName}".Contains(term, StringComparison.CurrentCultureIgnoreCase)
             || (customer.Phone?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)
             || (customer.Email?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false)
             || (customer.Address?.Contains(term, StringComparison.CurrentCultureIgnoreCase) ?? false))
            && (string.IsNullOrWhiteSpace(treatment) || customer.TreatmentNames.Contains(treatment, StringComparison.CurrentCultureIgnoreCase))
            && (SelectedConsentFilter == "Tutti"
                || (SelectedConsentFilter == "Consenso presente" && !string.IsNullOrWhiteSpace(customer.ConsentFormPath))
                || (SelectedConsentFilter == "Consenso mancante" && string.IsNullOrWhiteSpace(customer.ConsentFormPath)))
            && (!LastInterventionFrom.HasValue || (customer.LastInterventionDate.HasValue && customer.LastInterventionDate.Value.Date >= LastInterventionFrom.Value.Date))
            && (!LastInterventionTo.HasValue || (customer.LastInterventionDate.HasValue && customer.LastInterventionDate.Value.Date <= LastInterventionTo.Value.Date)))
            .ToList();

        filtered = SelectedSortOption switch
        {
            "Nome A-Z" => filtered.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToList(),
            "Ultimo intervento recente" => filtered.OrderByDescending(x => x.LastInterventionDate ?? DateTime.MinValue).ThenBy(x => x.LastName).ToList(),
            "Ultimo intervento meno recente" => filtered.OrderBy(x => x.LastInterventionDate ?? DateTime.MaxValue).ThenBy(x => x.LastName).ToList(),
            _ => filtered.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList()
        };

        FilteredCustomers.Clear();
        foreach (var customer in filtered)
            FilteredCustomers.Add(customer);
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuExpanded = !IsMenuExpanded;
    }

    [RelayCommand]
    private void Navigate(string section)
    {
        SelectedSection = section;

        switch (section)
        {
            case "Panoramica":
                CurrentViewModel = _dashboardViewModel;
                _ = _dashboardViewModel.RefreshCommand.ExecuteAsync(null);
                break;
            case "Clienti":
                ShowCustomers();
                break;

            case "Impostazioni":
                CurrentViewModel = _settingsViewModel;
                _ = _settingsViewModel.LoadLocationsCommand.ExecuteAsync(null);
                _ = _settingsViewModel.LoadInterventionTypesCommand.ExecuteAsync(null);
                break;

            case "Appuntamenti":
            case "Calendario":
                _calendarViewModel ??= new CalendarViewModel();
                CurrentViewModel = _calendarViewModel;
                _ = _calendarViewModel.RefreshCommand.ExecuteAsync(null);
                break;

            case "Interventi":
                _upcomingInterventionsViewModel ??= new UpcomingInterventionsViewModel(this);
                CurrentViewModel = _upcomingInterventionsViewModel;
                _ = _upcomingInterventionsViewModel.RefreshCommand.ExecuteAsync(null);
                break;
        }
    }

    public void ShowUpcomingAppointments()
    {
        if (_upcomingInterventionsViewModel == null) return;
        SelectedSection = "Interventi";
        CurrentViewModel = _upcomingInterventionsViewModel;
        _ = _upcomingInterventionsViewModel.RefreshCommand.ExecuteAsync(null);
    }

    public void ShowCustomerDetails(CustomerDetailsViewModel viewModel)
    {
        CurrentViewModel = viewModel;
    }

    public bool? ShowEditCustomerDialog(Customer customer) => _dialogService.ShowDialog(customer);

    public void ShowInterventionEditor(CustomerDetailsViewModel viewModel)
    {
        CurrentViewModel = new InterventionEditorViewModel(viewModel);
    }

    public void ShowAppointmentEditor(CustomerDetailsViewModel viewModel)
    {
        CurrentViewModel = new AppointmentEditorViewModel(viewModel);
    }

    public void ShowCustomers()
    {
        SelectedSection = "Clienti";
        CurrentViewModel = this;
    }

    public async Task RefreshCustomersAsync()
    {
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        IsLoading = true;

        try
        {
            var customers = await _customerService.GetAllAsync();
            await using (var db = new AppDbContext())
            {
                var interventions = await db.Interventions.Include(x => x.Type).AsNoTracking().ToListAsync();
                foreach (var customer in customers)
                {
                    var history = interventions.Where(x => x.CustomerId == customer.Id).ToList();
                    customer.LastInterventionDate = history.Count == 0 ? null : history.Max(x => x.Date);
                    customer.TreatmentNames = string.Join(" ", history.Select(x => x.Type?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
                }
            }

            Customers.Clear();

            foreach (var customer in customers)
                Customers.Add(customer);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCustomerAsync()
    {
        bool? result = _dialogService.ShowDialog();

        if (result == true)
            await LoadCustomersAsync();
    }

    [RelayCommand]
    private void ExportCustomers()
    {
        try
        {
            var path = new ExportService().ExportCustomersCsv(Customers);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Esportazione clienti non riuscita: {ex.Message}", "Esportazione CSV", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Ora il clic sulla card apre i dettagli
    // nella finestra principale.

    [RelayCommand]
    private void EditCustomer(Customer? customer)
    {
        if (customer == null)
            return;

        CurrentViewModel = new CustomerDetailsViewModel(
            customer,
            this);
    }
}
