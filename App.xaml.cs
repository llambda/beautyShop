using BeautyStudio.Data;
using BeautyStudio.Services;
using BeautyStudio.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace BeautyStudio;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await using (var db = new AppDbContext())
            {
                await db.Database.EnsureCreatedAsync();
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE TABLE IF NOT EXISTS InterventionLocations (Id INTEGER NOT NULL CONSTRAINT PK_InterventionLocations PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Address TEXT NULL, Notes TEXT NULL);");
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE TABLE IF NOT EXISTS InterventionTypes (Id INTEGER NOT NULL CONSTRAINT PK_InterventionTypes PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT NULL);");
            }

            var customerService = new CustomerService();
            var photoStorageService = new PhotoStorageService();
            var dialogService = new CustomerDialogService(customerService, photoStorageService);
            var locationService = new InterventionLocationService();
            var typeService = new InterventionTypeService();
            var settingsViewModel = new SettingsViewModel(locationService, typeService);

            var mainViewModel = new MainViewModel(customerService, dialogService, settingsViewModel);
            var mainWindow = new MainWindow(mainViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();

            await mainViewModel.LoadCustomersCommand.ExecuteAsync(null);
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(
                $"Errore durante l'avvio del programma:\n{ex.Message}",
                "Beauty Studio",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}
