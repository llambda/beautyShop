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
    private readonly InterventionService _interventionService = new();

    public ObservableCollection<Intervention> Interventions { get; } = new();

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? statusMessage;

    public bool HasInterventions => Interventions.Count > 0;

    public UpcomingInterventionsViewModel()
    {
        Interventions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasInterventions));
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var items = await _interventionService.GetUpcomingAsync();
            Interventions.Clear();
            foreach (var item in items)
                Interventions.Add(item);

            if (Interventions.Count == 0)
                StatusMessage = "Non ci sono interventi programmati a partire da oggi.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore durante il caricamento degli interventi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}