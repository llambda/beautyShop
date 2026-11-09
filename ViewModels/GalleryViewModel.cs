using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyStudio.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly InterventionService _interventionService = new();
    public ObservableCollection<GalleryInterventionItem> AllItems { get; } = new();
    public ObservableCollection<GalleryInterventionItem> FilteredItems { get; } = new();
    public ObservableCollection<string> TreatmentTypes { get; } = new() { "Tutti i trattamenti" };

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedTreatment = "Tutti i trattamenti";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? statusMessage;

    public GalleryViewModel(MainViewModel mainViewModel) => _mainViewModel = mainViewModel;

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedTreatmentChanged(string value) => ApplyFilters();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var interventions = await _interventionService.GetAllAsync();
            AllItems.Clear();
            foreach (var item in interventions)
                AllItems.Add(new GalleryInterventionItem(item));

            TreatmentTypes.Clear();
            TreatmentTypes.Add("Tutti i trattamenti");
            foreach (var type in interventions.Select(x => x.Type?.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x))
                TreatmentTypes.Add(type!);

            if (!TreatmentTypes.Contains(SelectedTreatment))
                SelectedTreatment = "Tutti i trattamenti";
            ApplyFilters();
            StatusMessage = AllItems.Count == 0 ? "Non ci sono ancora fotografie di interventi." : $"{FilteredItems.Count} interventi nella galleria.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Impossibile caricare la galleria: " + ex.Message;
            ApplicationLog.Write("Galleria", ex);
        }
        finally { IsLoading = false; }
    }

    private void ApplyFilters()
    {
        var term = SearchText?.Trim() ?? string.Empty;
        var results = AllItems.Where(item =>
            (string.IsNullOrWhiteSpace(term)
             || item.CustomerName.Contains(term, StringComparison.CurrentCultureIgnoreCase)
             || item.TreatmentName.Contains(term, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(SelectedTreatment)
                || SelectedTreatment == "Tutti i trattamenti"
                || item.TreatmentName.Equals(SelectedTreatment, StringComparison.CurrentCultureIgnoreCase)))
            .OrderByDescending(x => x.Intervention.Date)
            .ToList();

        FilteredItems.Clear();
        foreach (var item in results) FilteredItems.Add(item);
        StatusMessage = $"{FilteredItems.Count} interventi visualizzati.";
    }

    [RelayCommand]
    private void OpenDetails(GalleryInterventionItem? item)
    {
        if (item?.Intervention.Customer == null) return;
        _mainViewModel.OpenGalleryIntervention(item.Intervention);
    }
}

public sealed class GalleryInterventionItem
{
    public Intervention Intervention { get; }
    public string CustomerName => $"{Intervention.Customer?.FirstName} {Intervention.Customer?.LastName}".Trim();
    public string TreatmentName => Intervention.Type?.Name ?? "Trattamento non specificato";
    public string LocationName => Intervention.Location?.Name ?? "Luogo non specificato";
    public string PreviewPhotoPath { get; }
    public int BeforePhotoCount { get; }
    public int AfterPhotoCount { get; }
    public string PhotoCountLabel => $"Prima: {BeforePhotoCount} · Dopo: {AfterPhotoCount}";

    public GalleryInterventionItem(Intervention intervention)
    {
        Intervention = intervention;
        var before = ReadPaths(intervention.BeforePhotosJson);
        var after = ReadPaths(intervention.AfterPhotosJson);
        BeforePhotoCount = before.Length;
        AfterPhotoCount = after.Length;
        PreviewPhotoPath = after.FirstOrDefault(System.IO.File.Exists)
            ?? before.FirstOrDefault(System.IO.File.Exists)
            ?? string.Empty;
    }

    private static string[] ReadPaths(string? json)
    {
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json ?? "[]") ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }
}
