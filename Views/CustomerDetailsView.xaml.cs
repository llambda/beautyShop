
using System.Windows.Controls;
using System.Windows;

namespace BeautyStudio.Views;

public partial class CustomerDetailsView : UserControl
{
    public CustomerDetailsView()
    {
        InitializeComponent();
    }

    private void NewIntervention_Click(object sender, RoutedEventArgs e)
    {
        InterventionDetailsPanel.Visibility = Visibility.Visible;
    }

    private void Interventions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool selected = ((DataGrid)sender).SelectedItem is not null;
        EditInterventionButton.IsEnabled = selected;
        DeleteInterventionButton.IsEnabled = selected;
        if (selected)
            InterventionDetailsPanel.Visibility = Visibility.Visible;
    }

    private void NewAppointment_Click(object sender, RoutedEventArgs e)
    {
        AppointmentDetailsPanel.Visibility = Visibility.Visible;
    }

    private void Appointments_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool selected = ((DataGrid)sender).SelectedItem is not null;
        EditAppointmentButton.IsEnabled = selected;
        DeleteAppointmentButton.IsEnabled = selected;
        if (selected)
            AppointmentDetailsPanel.Visibility = Visibility.Visible;
    }
}
