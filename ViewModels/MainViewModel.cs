
using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace BeautyStudio.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CustomerService _customerService;
    private readonly CustomerDialogService _dialogService;
    private readonly SettingsViewModel _settingsViewModel;
    private CalendarViewModel? _calendarViewModel;
    private UpcomingInterventionsViewModel? _upcomingInterventionsViewModel;

    public ObservableCollection<Customer> Customers { get; } = new();

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

        // All'avvio mostriamo la lista clienti.
        CurrentViewModel = this;
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
