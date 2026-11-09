using MessageBox = BeautyStudio.Services.StyledMessageBox;
using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BeautyStudio.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly AppointmentService _appointmentService = new();
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private DateTime displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime selectedAgendaDate = DateTime.Today;
    [ObservableProperty] private bool isLoading;

    public ObservableCollection<CalendarDayViewModel> Days { get; } = new();
    public ObservableCollection<Appointment> DailyAppointments { get; } = new();
    public string MonthTitle => DisplayedMonth.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("it-IT"));
    public string AgendaTitle => SelectedAgendaDate.Date == DateTime.Today
        ? "Agenda di oggi"
        : $"Agenda del {SelectedAgendaDate:dd/MM/yyyy}";
    public string ReminderMessage
    {
        get
        {
            var limit = DateTime.Now.AddHours(24);
            int count = _loadedAppointments.Count(x => x.Status != "Annullato" && x.Date >= DateTime.Now && x.Date <= limit);
            return count == 0 ? "Nessun promemoria nelle prossime 24 ore." : $"Promemoria: {count} appuntamenti nelle prossime 24 ore.";
        }
    }

    public CalendarViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _ = LoadAsync();
    }

    partial void OnDisplayedMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthTitle));
        BuildCalendar();
    }

    partial void OnSelectedAgendaDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(AgendaTitle));
        var selectedMonth = new DateTime(value.Year, value.Month, 1);
        if (DisplayedMonth != selectedMonth)
            DisplayedMonth = selectedMonth;
        BuildDailyAgenda();
        foreach (var day in Days) day.IsSelected = day.Date.Date == SelectedAgendaDate.Date;
    }

    [RelayCommand]
    private void SelectDate(DateTime date)
    {
        SelectedAgendaDate = date.Date;
    }

    [RelayCommand]
    private void PreviousMonth() => DisplayedMonth = DisplayedMonth.AddMonths(-1);

    [RelayCommand]
    private void NextMonth() => DisplayedMonth = DisplayedMonth.AddMonths(1);

    [RelayCommand]
    private void Today()
    {
        SelectedAgendaDate = DateTime.Today;
        DisplayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void OpenCustomer(Appointment? appointment)
    {
        if (appointment?.Customer != null)
            _mainViewModel.OpenCustomerDetails(appointment.Customer);
    }

    [RelayCommand]
    private void ExportMonthPdf()
    {
        try
        {
            var path = new ExportService().ExportAppointmentsPdf(_loadedAppointments, DisplayedMonth);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Esportazione PDF", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }
    }

    [RelayCommand]
    private void ExportMonthCsv()
    {
        try
        {
            var path = new ExportService().ExportAppointmentsCsv(_loadedAppointments, DisplayedMonth);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Esportazione CSV", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var appointments = await _appointmentService.GetAllAsync();
            var hoursService = new WorkingHoursService();
            foreach (var appointment in appointments)
            {
                bool active = !string.Equals(appointment.Status, "Annullato", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(appointment.Status, "Non presentato", StringComparison.OrdinalIgnoreCase);
                appointment.IsOutsideWorkingHours = false;
                appointment.WorkingHoursWarning = string.Empty;
                appointment.HasOverlapWarning = active
                    && appointment.DurationMinutes > 0 && appointment.DurationMinutes <= 720
                    && AppointmentOverlapValidator.HasOverlap(
                        appointment.Date, appointment.DurationMinutes,
                        appointments.Where(x => x.Id != appointment.Id && x.Status != "Annullato" && x.Status != "Non presentato"));
                if (active && (appointment.DurationMinutes <= 0 || appointment.DurationMinutes > 720))
                {
                    appointment.IsOutsideWorkingHours = true;
                    appointment.WorkingHoursWarning = "Durata dell'appuntamento non valida.";
                }
                else if (active && !hoursService.IsAppointmentWithinWorkingHours(appointment.Date, appointment.DurationMinutes, out var reason))
                {
                    appointment.IsOutsideWorkingHours = true;
                    appointment.WorkingHoursWarning = reason;
                }
            }
            BuildCalendar(appointments);
        }
        finally { IsLoading = false; }
    }

    private void BuildCalendar(System.Collections.Generic.List<Appointment>? appointments = null)
    {
        if (appointments != null) _loadedAppointments = appointments;
        var first = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, 1);
        int offset = ((int)first.DayOfWeek + 6) % 7;
        var start = first.AddDays(-offset);
        Days.Clear();
        for (int i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var day = new CalendarDayViewModel(date, date.Month == DisplayedMonth.Month)
            {
                IsSelected = date.Date == SelectedAgendaDate.Date
            };
            foreach (var appointment in _loadedAppointments.Where(x => x.Date.Date == date.Date))
            {
                var customer = appointment.Customer;
                day.Appointments.Add(new AppointmentReminderViewModel(
                    appointment.Date.ToString("HH:mm"),
                    customer == null ? "Cliente" : $"{customer.FirstName} {customer.LastName}",
                    appointment.InterventionType?.Name ?? "Tipo non specificato",
                    appointment.Status,
                    appointment.IsOutsideWorkingHours,
                    appointment.HasOverlapWarning));
            }
            Days.Add(day);
        }
        BuildDailyAgenda();
        OnPropertyChanged(nameof(ReminderMessage));
    }

    private void BuildDailyAgenda()
    {
        DailyAppointments.Clear();
        foreach (var appointment in _loadedAppointments
            .Where(x => x.Date.Date == SelectedAgendaDate.Date)
            .OrderBy(x => x.Date))
            DailyAppointments.Add(appointment);
    }

    private System.Collections.Generic.List<Appointment> _loadedAppointments = new();
}

public sealed class CalendarDayViewModel : ObservableObject
{
    private bool _isSelected;
    public DateTime Date { get; }
    public string DayNumber => Date.Day.ToString();
    public bool IsCurrentMonth { get; }
    public bool IsToday => Date.Date == DateTime.Today;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public ObservableCollection<AppointmentReminderViewModel> Appointments { get; } = new();
    public CalendarDayViewModel(DateTime date, bool isCurrentMonth)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
    }
}

public sealed class AppointmentReminderViewModel
{
    public string Time { get; }
    public string CustomerName { get; }
    public string InterventionType { get; }
    public string Status { get; }
    public bool IsOutsideWorkingHours { get; }
    public bool HasOverlapWarning { get; }
    public AppointmentReminderViewModel(string time, string customerName, string interventionType, string status, bool isOutsideWorkingHours, bool hasOverlapWarning)
    {
        Time = time;
        CustomerName = customerName;
        InterventionType = interventionType;
        Status = status;
        IsOutsideWorkingHours = isOutsideWorkingHours;
        HasOverlapWarning = hasOverlapWarning;
    }
}