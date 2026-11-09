using MessageBox = BeautyStudio.Services.StyledMessageBox;
using BeautyStudio.Data;
using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace BeautyStudio.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly BackupService _backupService = new();
    public ObservableCollection<Appointment> UpcomingAppointments { get; } = new();
    public string TodayLabel => DateTime.Now.ToString("dddd dd MMMM yyyy", CultureInfo.GetCultureInfo("it-IT"));
    [ObservableProperty] private int totalCustomers;
    [ObservableProperty] private int appointmentsToday;
    [ObservableProperty] private int overdueAppointments;
    [ObservableProperty] private string? statusMessage;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            await using var db = new AppDbContext();
            var now = DateTime.Now;
            TotalCustomers = await db.Customers.CountAsync();
            AppointmentsToday = await db.Appointments.CountAsync(x => x.Date >= now.Date && x.Date < now.Date.AddDays(1));
            OverdueAppointments = await db.Appointments.CountAsync(x => x.Date < now);
            var upcoming = await db.Appointments.AsNoTracking().Include(x => x.Customer).Include(x => x.InterventionType)
                .Where(x => x.Date >= now).OrderBy(x => x.Date).Take(8).ToListAsync();
            UpcomingAppointments.Clear();
            foreach (var item in upcoming) UpcomingAppointments.Add(item);
            StatusMessage = null;
        }
        catch (Exception ex) { StatusMessage = "Impossibile aggiornare la panoramica: " + ex.Message; }
    }

    [RelayCommand]
    private void CreateBackup()
    {
        try
        {
            var path = _backupService.CreateBackup();
            StatusMessage = "Backup creato: " + path;
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { StatusMessage = "Backup non riuscito: " + ex.Message; }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        var dialog = new OpenFileDialog { Title = "Seleziona un backup BeautyStudio", Filter = "Backup BeautyStudio (*.zip)|*.zip", CheckFileExists = true };
        if (dialog.ShowDialog() != true) return;
        if (MessageBox.Show("Il ripristino sostituirà il database e i documenti attuali. Continuare?", "Ripristino backup", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            _backupService.RestoreBackup(dialog.FileName);
            await RefreshAsync();
            StatusMessage = "Backup ripristinato. Verifica i dati prima di continuare.";
        }
        catch (Exception ex) { StatusMessage = "Ripristino non riuscito: " + ex.Message; }
    }
}