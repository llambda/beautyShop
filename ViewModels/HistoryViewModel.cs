using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyStudio.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly InterventionService _interventionService = new();
    private readonly CustomerService _customerService = new();
    private readonly InterventionTypeService _typeService = new();
    private readonly InterventionLocationService _locationService = new();

    public ObservableCollection<Intervention> Interventions { get; } = new();
    public ObservableCollection<Intervention> FilteredInterventions { get; } = new();
    public ObservableCollection<Customer> Customers { get; } = new();
    public ObservableCollection<InterventionType> Types { get; } = new();
    public ObservableCollection<InterventionLocation> Locations { get; } = new();

    [ObservableProperty] private Customer? selectedCustomer;
    [ObservableProperty] private InterventionType? selectedType;
    [ObservableProperty] private InterventionLocation? selectedLocation;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool isLoading;

    partial void OnSelectedCustomerChanged(Customer? value) => ApplyFilters();
    partial void OnSelectedTypeChanged(InterventionType? value) => ApplyFilters();
    partial void OnSelectedLocationChanged(InterventionLocation? value) => ApplyFilters();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var customers = await _customerService.GetAllAsync();
            var types = await _typeService.GetAllAsync();
            var locations = await _locationService.GetAllAsync();
            var interventions = await _interventionService.GetAllAsync();
            Replace(Customers, customers);
            Replace(Types, types);
            Replace(Locations, locations);
            Replace(Interventions, interventions);
            ApplyFilters();
            StatusMessage = $"{FilteredInterventions.Count} interventi trovati.";
        }
        catch (Exception ex) { StatusMessage = $"Errore caricamento storico: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedCustomer = null;
        SelectedType = null;
        SelectedLocation = null;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var query = Interventions.AsEnumerable();
        if (SelectedCustomer != null) query = query.Where(x => x.CustomerId == SelectedCustomer.Id);
        if (SelectedType != null) query = query.Where(x => x.TypeId == SelectedType.Id);
        if (SelectedLocation != null) query = query.Where(x => x.LocationId == SelectedLocation.Id);
        var result = query.OrderByDescending(x => x.Date).ToList();
        Replace(FilteredInterventions, result);
        StatusMessage = $"{result.Count} interventi trovati.";
    }

    private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }
}
