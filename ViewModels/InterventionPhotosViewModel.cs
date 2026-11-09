using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics;
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
    public ObservableCollection<string> MissingPhotoPaths { get; } = new();
    public string PhotoIntegrityMessage => MissingPhotoPaths.Count == 0 ? "Tutte le fotografie collegate sono disponibili." : "Attenzione: " + MissingPhotoPaths.Count + " fotografie non sono state trovate nei percorsi salvati.";
    public string CustomerName => $"{_parent.Customer.FirstName} {_parent.Customer.LastName}".Trim();
    public string NotesDisplay => string.IsNullOrWhiteSpace(Intervention.Notes) ? "Nessuna nota presente" : Intervention.Notes;

    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private int beforeIndex;
    [ObservableProperty] private int afterIndex;

    public InterventionPhotoItem? CurrentBeforePhoto => BeforePhotos.Count == 0 ? null : BeforePhotos[Math.Clamp(BeforeIndex, 0, BeforePhotos.Count - 1)];
    public InterventionPhotoItem? CurrentAfterPhoto => AfterPhotos.Count == 0 ? null : AfterPhotos[Math.Clamp(AfterIndex, 0, AfterPhotos.Count - 1)];
    public string BeforeCounter => BeforePhotos.Count == 0 ? "0 / 0" : $"{BeforeIndex + 1} / {BeforePhotos.Count}";
    public string AfterCounter => AfterPhotos.Count == 0 ? "0 / 0" : $"{AfterIndex + 1} / {AfterPhotos.Count}";
    partial void OnBeforeIndexChanged(int value) { OnPropertyChanged(nameof(CurrentBeforePhoto)); OnPropertyChanged(nameof(BeforeCounter)); }
    partial void OnAfterIndexChanged(int value) { OnPropertyChanged(nameof(CurrentAfterPhoto)); OnPropertyChanged(nameof(AfterCounter)); }

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
        OnPropertyChanged(nameof(PhotoIntegrityMessage));
        OnPropertyChanged(nameof(CurrentBeforePhoto));
        OnPropertyChanged(nameof(CurrentAfterPhoto));
        OnPropertyChanged(nameof(BeforeCounter));
        OnPropertyChanged(nameof(AfterCounter));
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
                if (!File.Exists(path))
                    MissingPhotoPaths.Add(path);
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
                i < BeforePhotos.Count ? BeforePhotos[i] : null,
                i < AfterPhotos.Count ? AfterPhotos[i] : null));
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
    private void PreviousBefore() { if (BeforePhotos.Count > 0) BeforeIndex = (BeforeIndex - 1 + BeforePhotos.Count) % BeforePhotos.Count; }

    [RelayCommand]
    private void NextBefore() { if (BeforePhotos.Count > 0) BeforeIndex = (BeforeIndex + 1) % BeforePhotos.Count; }

    [RelayCommand]
    private void PreviousAfter() { if (AfterPhotos.Count > 0) AfterIndex = (AfterIndex - 1 + AfterPhotos.Count) % AfterPhotos.Count; }

    [RelayCommand]
    private void NextAfter() { if (AfterPhotos.Count > 0) AfterIndex = (AfterIndex + 1) % AfterPhotos.Count; }

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
    private void ExportComparison()
    {
        try
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var file = Path.Combine(folder, $"BeautyStudio_Confronto_{Intervention.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            var html = new StringBuilder();
            html.AppendLine("<!doctype html><html lang='it'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>");
            html.AppendLine("<title>Confronto prima e dopo - Beauty Studio</title><style>body{font-family:Segoe UI,Arial,sans-serif;margin:28px;color:#222}h1{font-size:24px}.meta{color:#555;margin-bottom:22px}.grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}.photo{border:1px solid #ddd;padding:12px;break-inside:avoid}.photo img{width:100%;height:auto;max-height:650px;object-fit:contain}.caption{margin-top:8px;color:#555}@media(max-width:700px){.grid{grid-template-columns:1fr}}@media print{body{margin:10mm}}</style></head><body>");
            html.AppendLine("<h1>Confronto fotografico prima e dopo</h1>");
            html.AppendLine("<div class='meta'>" + WebUtility.HtmlEncode(CustomerName) + " · " + Intervention.Date.ToString("dd/MM/yyyy") + " · " + WebUtility.HtmlEncode(Intervention.Type?.Name ?? "Trattamento") + "</div>");
            html.AppendLine("<div class='grid'><section><h2>Prima</h2>" + BuildExportPhotos(BeforePhotos) + "</section><section><h2>Dopo</h2>" + BuildExportPhotos(AfterPhotos) + "</section></div>");
            html.AppendLine("<h2>Note</h2><p>" + WebUtility.HtmlEncode(NotesDisplay) + "</p></body></html>");
            File.WriteAllText(file, html.ToString(), Encoding.UTF8);
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            StatusMessage = "Confronto esportato: " + file;
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Esportazione confronto fotografico", ex);
            StatusMessage = "Esportazione del confronto non riuscita: " + ex.Message;
        }
    }

    private static string BuildExportPhotos(IEnumerable<InterventionPhotoItem> photos)
    {
        var html = new StringBuilder();
        foreach (var photo in photos)
        {
            if (!File.Exists(photo.Path)) continue;
            var extension = Path.GetExtension(photo.Path).ToLowerInvariant();
            var mime = extension switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", ".bmp" => "image/bmp", _ => "image/jpeg" };
            var data = Convert.ToBase64String(File.ReadAllBytes(photo.Path));
            html.Append("<figure class='photo'><img src='data:").Append(mime).Append(";base64,").Append(data).Append("'><figcaption class='caption'>")
                .Append(WebUtility.HtmlEncode(photo.Date?.ToString("dd/MM/yyyy") ?? string.Empty)).Append(" ")
                .Append(WebUtility.HtmlEncode(photo.Description)).Append("</figcaption></figure>");
        }
        return html.Length == 0 ? "<p>Nessuna fotografia disponibile.</p>" : html.ToString();
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
    public InterventionPhotoItem? BeforePhoto { get; }
    public InterventionPhotoItem? AfterPhoto { get; }
    public PhotoComparisonPair(InterventionPhotoItem? beforePhoto, InterventionPhotoItem? afterPhoto)
    {
        BeforePhoto = beforePhoto;
        AfterPhoto = afterPhoto;
    }
}
