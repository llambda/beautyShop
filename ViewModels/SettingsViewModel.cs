using MessageBox = BeautyStudio.Services.StyledMessageBox;
using BeautyStudio.Models;
using BeautyStudio.Services;
using BeautyStudio.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;

namespace BeautyStudio.ViewModels;

public sealed record PaletteOption(string Name, string Hex, string SecondaryHex, string PrimaryText, string SecondaryText);

public partial class SettingsViewModel : ObservableObject
{
    private readonly InterventionLocationService _locationService;
    private readonly InterventionTypeService _typeService;
    private readonly CustomerService _customerService;
    private readonly BackupService _backupService = new();
    private readonly UpdateCheckService _updateCheckService = new();
    private readonly ThemeSettingsService _themeService = new();
    private bool _loadingTheme;

    private static ObservableCollection<PaletteOption> BuildPalettes() => new()
    {
        new("Yellow", "#FFFFEB3B", "#FFFFC107", "#000000", "#000000"),
        new("Amber", "#FFFFC107", "#FFFF9800", "#000000", "#000000"),
        new("Purple", "#FF9C27B0", "#FFE040FB", "#FFFFFFFF", "#FFFFFFFF"),
        new("Lime", "#FFCDDC39", "#FFAEEA00", "#000000", "#000000"),
        new("LightBlue", "#FF03A9F4", "#FF00B8D4", "#000000", "#000000"),
        new("Cyan", "#FF00BCD4", "#FF00E5FF", "#000000", "#000000"),
        new("Teal", "#FF009688", "#FF1DE9B6", "#FFFFFFFF", "#000000"),
        new("Indigo", "#FF3F51B5", "#FF536DFE", "#FFFFFFFF", "#FFFFFFFF"),
        new("Orange", "#FFFF9800", "#FFFFAB00", "#000000", "#000000"),
        new("Green", "#FF4CAF50", "#FF00C853", "#FFFFFFFF", "#000000"),
        new("DeepOrange", "#FFFF5722", "#FFFF3D00", "#FFFFFFFF", "#FFFFFFFF"),
        new("DeepPurple", "#FF673AB7", "#FF651FFF", "#FFFFFFFF", "#FFFFFFFF"),
        new("LightGreen", "#FF8BC34A", "#FFAEEA00", "#000000", "#000000"),
        new("Blue", "#FF2196F3", "#FF448AFF", "#FFFFFFFF", "#FFFFFFFF"),
        new("Red", "#FFF44336", "#FFFF1744", "#FFFFFFFF", "#FFFFFFFF"),
        new("Pink", "#FFE91E63", "#FFFF4081", "#FFFFFFFF", "#FFFFFFFF"),
        new("BlueGrey", "#FF607D8B", "#FF78909C", "#FFFFFFFF", "#FFFFFFFF"),
        new("Grey", "#FF9E9E9E", "#FFBDBDBD", "#000000", "#000000"),
    };
    public ObservableCollection<PaletteOption> PrimaryPalettes { get; } = BuildPalettes();
    public ObservableCollection<PaletteOption> SecondaryPalettes { get; } = BuildPalettes();

    [ObservableProperty] private PaletteOption? selectedPrimaryPalette;
    [ObservableProperty] private PaletteOption? selectedSecondaryPalette;
    [ObservableProperty] private bool isDarkTheme;
    [ObservableProperty] private ObservableCollection<InterventionLocation> locations = new();
    [ObservableProperty] private InterventionLocation? selectedLocation;
    [ObservableProperty] private ObservableCollection<InterventionType> interventionTypes = new();
    [ObservableProperty] private InterventionType? selectedInterventionType;
    [ObservableProperty] private string? dataManagementStatus;
    [ObservableProperty] private string updateStatus = "Non hai ancora controllato gli aggiornamenti.";
    [ObservableProperty] private bool isCheckingUpdates;
    [ObservableProperty] private string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
    private string? _latestReleaseUrl;

    public SettingsViewModel(InterventionLocationService locationService, InterventionTypeService typeService, CustomerService customerService)
    {
        _locationService = locationService;
        _typeService = typeService;
        _customerService = customerService;

        var settings = _themeService.Load();
        _loadingTheme = true;
        SelectedPrimaryPalette = PrimaryPalettes.FirstOrDefault(x => x.Hex.Equals(settings.PrimaryColor, StringComparison.OrdinalIgnoreCase)) ?? PrimaryPalettes[0];
        SelectedSecondaryPalette = SecondaryPalettes.FirstOrDefault(x => x.SecondaryHex.Equals(settings.SecondaryColor, StringComparison.OrdinalIgnoreCase)) ?? SecondaryPalettes[0];
        IsDarkTheme = settings.IsDark;
        _loadingTheme = false;
        ApplyTheme();
    }

    partial void OnSelectedPrimaryPaletteChanged(PaletteOption? value) => ApplyTheme();
    partial void OnSelectedSecondaryPaletteChanged(PaletteOption? value) => ApplyTheme();
    partial void OnIsDarkThemeChanged(bool value) => ApplyTheme();

    private void ApplyTheme()
    {
        if (_loadingTheme || SelectedPrimaryPalette is null || SelectedSecondaryPalette is null) return;
        _themeService.Apply(SelectedPrimaryPalette.Hex, SelectedSecondaryPalette.SecondaryHex, IsDarkTheme);
        _themeService.Save(new ThemeSettings
        {
            PrimaryColor = SelectedPrimaryPalette.Hex,
            SecondaryColor = SelectedSecondaryPalette.SecondaryHex,
            IsDark = IsDarkTheme
        });
    }

    [RelayCommand]
    public async Task LoadLocationsAsync()
    {
        var items = await _locationService.GetAllAsync();
        Locations.Clear();
        foreach (var item in items) Locations.Add(item);
    }

    [RelayCommand]
    public async Task LoadInterventionTypesAsync()
    {
        var items = await _typeService.GetAllAsync();
        InterventionTypes.Clear();
        foreach (var item in items) InterventionTypes.Add(item);
    }

    [RelayCommand]
    private async Task AddLocationAsync()
    {
        var dialog = new LocationEditorWindow(null) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;
        await _locationService.SaveAsync(new InterventionLocation
        {
            Name = dialog.LocationName,
            Address = dialog.LocationAddress,
            Notes = dialog.LocationNotes
        });
        await LoadLocationsAsync();
    }

    [RelayCommand]
    private async Task EditLocationAsync()
    {
        if (SelectedLocation is null) return;
        var current = SelectedLocation;
        var dialog = new LocationEditorWindow(current) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;
        await _locationService.SaveAsync(new InterventionLocation
        {
            Id = current.Id,
            Name = dialog.LocationName,
            Address = dialog.LocationAddress,
            Notes = dialog.LocationNotes
        });
        await LoadLocationsAsync();
    }

    [RelayCommand]
    private async Task DeleteLocationAsync()
    {
        if (SelectedLocation is null) return;
        if (MessageBox.Show($"Eliminare il luogo \"{SelectedLocation.Name}\"?", "Conferma eliminazione",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await _locationService.DeleteAsync(SelectedLocation.Id);
        SelectedLocation = null;
        await LoadLocationsAsync();
    }

    [RelayCommand]
    private async Task AddInterventionTypeAsync()
    {
        var dialog = new InterventionTypeEditorWindow(null) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;
        await _typeService.SaveAsync(new InterventionType
        {
            Name = dialog.TypeName,
            Description = dialog.TypeDescription
        });
        await LoadInterventionTypesAsync();
    }

    [RelayCommand]
    private async Task EditInterventionTypeAsync()
    {
        if (SelectedInterventionType is null) return;
        var current = SelectedInterventionType;
        var dialog = new InterventionTypeEditorWindow(current) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;
        await _typeService.SaveAsync(new InterventionType
        {
            Id = current.Id,
            Name = dialog.TypeName,
            Description = dialog.TypeDescription
        });
        await LoadInterventionTypesAsync();
    }

    [RelayCommand]
    private async Task DeleteInterventionTypeAsync()
    {
        if (SelectedInterventionType is null) return;
        if (MessageBox.Show($"Eliminare il tipo \"{SelectedInterventionType.Name}\"?", "Conferma eliminazione",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await _typeService.DeleteAsync(SelectedInterventionType.Id);
        SelectedInterventionType = null;
        await LoadInterventionTypesAsync();
    }
    [RelayCommand]
    private void CreateBackup()
    {
        try
        {
            var path = _backupService.CreateBackup();
            DataManagementStatus = "Backup creato: " + path;
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Creazione backup", ex);
            DataManagementStatus = "Backup non riuscito: " + ex.Message;
        }
    }

    [RelayCommand]
    private void RestoreBackup()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona un backup BeautyStudio",
            Filter = "Backup BeautyStudio (*.zip)|*.zip",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;
        if (MessageBox.Show("Il ripristino sostituirà il database e i documenti attuali. Continuare?",
            "Ripristino backup", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try
        {
            _backupService.RestoreBackup(dialog.FileName);
            DataManagementStatus = "Backup ripristinato. Riavvia l'applicazione per ricaricare tutti i dati.";
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Ripristino backup", ex);
            DataManagementStatus = "Ripristino non riuscito: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task ExportCustomersAsync()
    {
        try
        {
            var customers = await _customerService.GetAllAsync();
            var path = new ExportService().ExportCustomersCsv(customers);
            DataManagementStatus = "Elenco clienti esportato: " + path;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            DataManagementStatus = "Esportazione clienti non riuscita: " + ex.Message;
        }
    }


    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdates = true;
        UpdateStatus = "Controllo disponibilità aggiornamenti...";
        try
        {
            var result = await _updateCheckService.CheckAsync();
            CurrentVersion = result.CurrentVersion;
            _latestReleaseUrl = result.ReleaseUrl;
            UpdateStatus = result.Message;
            ApplicationLog.Write("Aggiornamenti", result.Message);
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Aggiornamenti", ex);
            UpdateStatus = "Controllo aggiornamenti non riuscito. Consulta il registro applicazione.";
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand]
    private void OpenLatestRelease()
    {
        try
        {
            var url = string.IsNullOrWhiteSpace(_latestReleaseUrl)
                ? "https://github.com/llambda/beautyShop/releases"
                : _latestReleaseUrl;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Apertura pagina aggiornamenti", ex);
            UpdateStatus = "Impossibile aprire la pagina delle versioni.";
        }
    }

    [RelayCommand]
    private void VerifyBackup()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Verifica un backup BeautyStudio",
            Filter = "Backup BeautyStudio (*.zip)|*.zip",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;

        _backupService.ValidateBackup(dialog.FileName, out var message);
        DataManagementStatus = message;
        ApplicationLog.Write("Verifica backup", $"{dialog.FileName}: {message}");
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            var folder = System.IO.Path.Combine(BeautyStudio.Data.AppDbContext.DataDirectory, "Logs");
            System.IO.Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\\"{folder}\\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ApplicationLog.Write("Apertura registro", ex);
            DataManagementStatus = "Impossibile aprire la cartella dei registri: " + ex.Message;
        }
    }

}
