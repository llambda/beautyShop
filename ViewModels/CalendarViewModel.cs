using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BeautyStudio.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    private readonly AppointmentService _appointmentService = new();

    [ObservableProperty] private DateTime displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private bool isLoading;

    public ObservableCollection<CalendarDayViewModel> Days { get; } = new();
    public string MonthTitle => DisplayedMonth.ToString("MMMM yyyy");

    public CalendarViewModel() => _ = LoadAsync();

    partial void OnDisplayedMonthChanged(DateTime value)
    {
        OnPropertyChanged(nameof(MonthTitle));
        BuildCalendar();
    }

    [RelayCommand]
    private void PreviousMonth() => DisplayedMonth = DisplayedMonth.AddMonths(-1);

    [RelayCommand]
    private void NextMonth() => DisplayedMonth = DisplayedMonth.AddMonths(1);

    [RelayCommand]
    private void Today() => DisplayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var appointments = await _appointmentService.GetAllAsync();
            BuildCalendar(appointments);
        }
        finally { IsLoading = false; }
    }

    private void BuildCalendar(System.Collections.Generic.List<Appointment>? appointments = null)
    {
        var first = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, 1);
        int offset = ((int)first.DayOfWeek + 6) % 7;
        var start = first.AddDays(-offset);
        var all = appointments;
        if (all == null)
        {
            // Month navigation rebuilds from the appointments already loaded.
            all = _loadedAppointments;
        }
        Days.Clear();
        for (int i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var day = new CalendarDayViewModel(date, date.Month == DisplayedMonth.Month);
            foreach (var appointment in all.Where(x => x.Date.Date == date.Date))
            {
                var customer = appointment.Customer;
                day.Appointments.Add(new AppointmentReminderViewModel(
                    appointment.Date.ToString("HH:mm"),
                    customer == null ? "Cliente" : $"{customer.FirstName} {customer.LastName}",
                    appointment.InterventionType?.Name ?? "Tipo non specificato"));
            }
            Days.Add(day);
        }
        if (appointments != null) _loadedAppointments = appointments;
    }

    private System.Collections.Generic.List<Appointment> _loadedAppointments = new();
}

public sealed class CalendarDayViewModel
{
    public DateTime Date { get; }
    public string DayNumber => Date.Day.ToString();
    public bool IsCurrentMonth { get; }
    public bool IsToday => Date.Date == DateTime.Today;
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
    public AppointmentReminderViewModel(string time, string customerName, string interventionType)
    {
        Time = time;
        CustomerName = customerName;
        InterventionType = interventionType;
    }
}