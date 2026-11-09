using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BeautyStudio.ViewModels;

public partial class InterventionPhotosViewModel : ObservableObject
{
    private readonly CustomerDetailsViewModel _parent;
    private readonly MainViewModel _mainViewModel;
    private readonly InterventionService _interventionService = new();
    private readonly Dictionary<string, PhotoMetadata> _metadata = new(StringComparer.OrdinalIgnoreCase);

    public Intervention Intervention { get; }
    public ObservableCollection<InterventionPhotoItem> BeforePhotos { get; } = new();
    public ObservableCollection<InterventionPhotoItem> AfterPhotos { get; } = new();
    public ObservableCollection<PhotoComparisonPair> PhotoPairs { get; } = new();
    public ObservableCollection<InterventionHistoryEntry> History { get; } = new();
    public string CustomerName => $"{_parent.Customer.FirstName} {_parent.Customer.LastName}".Trim();

    [ObservableProperty] private string? statusMessage;

    public InterventionPhotosViewModel(CustomerDetailsViewModel parent, Intervention intervention, MainViewModel mainViewModel)
    {
        _parent = parent;
        _mainViewModel = mainViewModel;
        Intervention = intervention;
        try
        {
            var stored = JsonSerializer.Deserialize<Dictionary<string, PhotoMetadata>>(intervention.PhotoMetadataJson ?? "{}");
            if (stored != null)
                foreach (var pair in stored) _metadata[pair.Key] = pair.Value;
        }
        catch { }
        LoadPhotos(BeforePhotos, intervention.BeforePhotosJson);
        LoadPhotos(AfterPhotos, intervention.AfterPhotosJson);
        BuildPhotoPairs();
        _ = LoadHistoryAsync();
    }

    private void LoadPhotos(ObservableCollection<InterventionPhotoItem> target, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            foreach (var path in JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>())
            {
                _metadata.TryGetValue(path, out var info);
                target.Add(new InterventionPhotoItem(path, info?.Date ?? SafeFileDate(path), info?.Description ?? string.Empty));
            }
        }
        catch { }
    }

    private void BuildPhotoPairs()
    {
        PhotoPairs.Clear();
        int count = Math.Max(BeforePhotos.Count, AfterPhotos.Count);
        for (int i = 0; i < count; i++)
            PhotoPairs.Add(new PhotoComparisonPair(
                i < BeforePhotos.Count ? BeforePhotos[i].Path : null,
                i < AfterPhotos.Count ? AfterPhotos[i].Path : null));
    }

    private static DateTime? SafeFileDate(string path)
    {
        try { return File.Exists(path) ? File.GetCreationTime(path) : null; }
        catch { return null; }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var items = await _interventionService.GetHistoryAsync(Intervention.Id);
            History.Clear();
            foreach (var item in items) History.Add(item);
        }
        catch (Exception ex) { StatusMessage = $"Impossibile caricare lo storico: {ex.Message}"; }
    }

    [RelayCommand]
    private void Back() => _mainViewModel.ShowCustomerDetails(_parent);

    [RelayCommand]
    private void Edit() => _parent.EditInterventionFromDetails(Intervention);

    [RelayCommand]
    private void OpenPhoto(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        try
        {
            var window = new Window
            {
                Title = "Fotografia intervento",
                Width = 1100,
                Height = 850,
                MinWidth = 500,
                MinHeight = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Background = Brushes.Black,
                Content = new Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.GetFullPath(path))),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(12)
                }
            };
            window.ShowDialog();
        }
        catch (Exception ex) { StatusMessage = $"Impossibile aprire la fotografia: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task SavePhotoMetadataAsync()
    {
        try
        {
            _metadata.Clear();
            foreach (var photo in BeforePhotos.Concat(AfterPhotos))
                _metadata[photo.Path] = new PhotoMetadata { Date = photo.Date, Description = photo.Description ?? string.Empty };
            Intervention.PhotoMetadataJson = JsonSerializer.Serialize(_metadata);
            await _interventionService.SaveAsync(Intervention);
            await LoadHistoryAsync();
            StatusMessage = "Date e descrizioni delle fotografie salvate.";
        }
        catch (Exception ex) { StatusMessage = $"Salvataggio metadati non riuscito: {ex.Message}"; }
    }

    private sealed class PhotoMetadata
    {
        public PhotoMetadata() { }
        public DateTime? Date { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}

public partial class InterventionPhotoItem : ObservableObject
{
    public string Path { get; }
    [ObservableProperty] private DateTime? date;
    [ObservableProperty] private string description;

    public InterventionPhotoItem(string path, DateTime? date, string description)
    {
        Path = path;
        this.date = date;
        this.description = description;
    }
}

public sealed class PhotoComparisonPair
{
    public string? BeforePath { get; }
    public string? AfterPath { get; }
    public PhotoComparisonPair(string? beforePath, string? afterPath)
    {
        BeforePath = beforePath;
        AfterPath = afterPath;
    }
}
