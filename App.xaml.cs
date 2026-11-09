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
                await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS InterventionHistory (
                    Id INTEGER NOT NULL CONSTRAINT PK_InterventionHistory PRIMARY KEY AUTOINCREMENT,
                    InterventionId INTEGER NOT NULL,
                    CustomerId INTEGER NOT NULL,
                    ChangedAt TEXT NOT NULL,
                    Action TEXT NOT NULL,
                    Details TEXT NOT NULL,
                    CONSTRAINT FK_InterventionHistory_Interventions_InterventionId FOREIGN KEY (InterventionId) REFERENCES Interventions (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_InterventionHistory_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE CASCADE
                );");
                await db.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS Appointments (
                    Id INTEGER NOT NULL CONSTRAINT PK_Appointments PRIMARY KEY AUTOINCREMENT,
                    CustomerId INTEGER NOT NULL,
                    Date TEXT NOT NULL,
                    InterventionTypeId INTEGER NULL,
                    Notes TEXT NULL,
                    CONSTRAINT FK_Appointments_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE CASCADE
                );");

                var consentFormColumn = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('Customers') WHERE name = 'ConsentFormPath'").SingleAsync();
                if (consentFormColumn == 0)
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Customers ADD COLUMN ConsentFormPath TEXT NULL;");

                var appointmentTypeColumn = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('Appointments') WHERE name = 'InterventionTypeId'").SingleAsync();
                if (appointmentTypeColumn == 0)
                    await db.Database.ExecuteSqlRawAsync("ALTER TABLE Appointments ADD COLUMN InterventionTypeId INTEGER NULL;");

                async Task EnsureColumnAsync(string table, string column, string alterSql)
                {
                    // I valori table/column vengono passati come parametri SQL;
                    // il comando ALTER è scelto esclusivamente tra stringhe statiche interne.
                    var count = await db.Database.SqlQuery<int>(
                        $"SELECT COUNT(*) AS Value FROM pragma_table_info({table}) WHERE name = {column}")
                        .SingleAsync();

                    if (count == 0)
                        await db.Database.ExecuteSqlRawAsync(alterSql);
                }

                await EnsureColumnAsync("Customers", "Allergies",
                    "ALTER TABLE Customers ADD COLUMN Allergies TEXT NULL;");
                await EnsureColumnAsync("Customers", "Sensitivities",
                    "ALTER TABLE Customers ADD COLUMN Sensitivities TEXT NULL;");
                await EnsureColumnAsync("Interventions", "Price",
                    "ALTER TABLE Interventions ADD COLUMN Price TEXT NOT NULL DEFAULT '0';");
                await EnsureColumnAsync("Interventions", "IsPaid",
                    "ALTER TABLE Interventions ADD COLUMN IsPaid INTEGER NOT NULL DEFAULT 0;");
                await EnsureColumnAsync("Interventions", "PaidAt",
                    "ALTER TABLE Interventions ADD COLUMN PaidAt TEXT NULL;");
                await EnsureColumnAsync("Interventions", "PhotoMetadataJson",
                    "ALTER TABLE Interventions ADD COLUMN PhotoMetadataJson TEXT NOT NULL DEFAULT '{{}}';");
                await EnsureColumnAsync("Appointments", "Status",
                    "ALTER TABLE Appointments ADD COLUMN Status TEXT NOT NULL DEFAULT 'Da confermare';");
                await EnsureColumnAsync("Appointments", "LocationId",
                    "ALTER TABLE Appointments ADD COLUMN LocationId INTEGER NULL;");
                await EnsureColumnAsync("Customers", "ConsentAcquiredAt",
                    "ALTER TABLE Customers ADD COLUMN ConsentAcquiredAt TEXT NULL;");
            }

            try
            {
                new BackupService().CreateAutomaticBackupIfNeeded();
            }
            catch (System.Exception backupException)
            {
                MessageBox.Show($"Backup automatico non riuscito. L'applicazione verrà comunque avviata.\n{backupException.Message}",
                    "Beauty Studio - Backup", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            var customerService = new CustomerService();
            var photoStorageService = new PhotoStorageService();
            var dialogService = new CustomerDialogService(customerService, photoStorageService);
            var locationService = new InterventionLocationService();
            var typeService = new InterventionTypeService();
            var settingsViewModel = new SettingsViewModel(locationService, typeService, customerService);
            var mainViewModel = new MainViewModel(customerService, dialogService, settingsViewModel);
            var mainWindow = new MainWindow(mainViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();
            await mainViewModel.LoadCustomersCommand.ExecuteAsync(null);
        }
        catch (System.Exception ex)
        {
            string logPath = System.IO.Path.Combine(AppDbContext.DataDirectory, "avvio-errore.log");
            string message = $"Data: {DateTime.Now:O}{Environment.NewLine}{ex}{Environment.NewLine}";
            try
            {
                System.IO.Directory.CreateDirectory(AppDbContext.DataDirectory);
                System.IO.File.WriteAllText(logPath, message);
            }
            catch { logPath = "Impossibile creare il file di log."; }

            MessageBox.Show(
                $"Errore durante l'avvio del programma:\n{ex.Message}\n\nDettagli completi: {logPath}",
                "Beauty Studio", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}