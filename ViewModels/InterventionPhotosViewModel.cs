using BeautyStudio.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace BeautyStudio.ViewModels;

public partial class InterventionPhotosViewModel : ObservableObject
{
    private readonly CustomerDetailsViewModel _parent;
    private readonly MainViewModel _mainViewModel;

    public Intervention Intervention { get; }
    public ObservableCollection<string> BeforePhotos { get; } = new();
    public ObservableCollection<string> AfterPhotos { get; } = new();

    public string CustomerName => $"{_parent.Customer.FirstName} {_parent.Customer.LastName}".Trim();

    public InterventionPhotosViewModel(CustomerDetailsViewModel parent, Intervention intervention, MainViewModel mainViewModel)
    {
        _parent = parent;
        _mainViewModel = mainViewModel;
        Intervention = intervention;
        LoadPhotos(BeforePhotos, intervention.BeforePhotosJson);
        LoadPhotos(AfterPhotos, intervention.AfterPhotosJson);
    }

    private static void LoadPhotos(ObservableCollection<string> target, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            foreach (var path in JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>())
                target.Add(path);
        }
        catch { }
    }

    [RelayCommand]
    private void Back() => _mainViewModel.ShowCustomerDetails(_parent);

    [RelayCommand]
    private void Edit() => _parent.EditInterventionFromDetails(Intervention);
}
