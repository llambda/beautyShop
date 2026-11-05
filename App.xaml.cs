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
                await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS InterventionLocations (Id INTEGER NOT NULL CONSTRAINT PK_InterventionLocations PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Address TEXT NULL, Notes TEXT NULL);");
                await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS InterventionTypes (Id INTEGER NOT NULL CONSTRAINT PK_InterventionTypes PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT NULL);");
                await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS Interventions (
                    Id INTEGER NOT NULL CONSTRAINT PK_Interventions PRIMARY KEY AUTOINCREMENT,
                    CustomerId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    LocationId INTEGER NOT NULL,
                    TypeId INTEGER NOT NULL,
                    Notes TEXT NULL,
                    BeforePhotosJson TEXT NOT NULL DEFAULT '[]',
                    AfterPhotosJson TEXT NOT NULL DEFAULT '[]',
                    CONSTRAINT FK_Interventions_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Interventions_InterventionLocations_LocationId FOREIGN KEY (LocationId) REFERENCES InterventionLocations (Id) ON DELETE RESTRICT,
                    CONSTRAINT FK_Interventions_InterventionTypes_TypeId FOREIGN KEY (TypeId) REFERENCES InterventionTypes (Id) ON DELETE RESTRICT
                );");
                await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS Appointments (
                    Id INTEGER NOT NULL CONSTRAINT PK_Appointments PRIMARY KEY AUTOINCREMENT,
                    CustomerId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    Notes TEXT NULL,
                    CONSTRAINT FK_Appointments_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE CASCADE
                );");
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
            MessageBox.Show($"Errore durante l'avvio del programma:\n{ex.Message}", "Beauty Studio", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}