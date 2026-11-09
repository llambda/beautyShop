using System.Windows.Controls;

namespace BeautyStudio.Views;

public partial class CustomerDetailsView : UserControl
{
    public CustomerDetailsView() => InitializeComponent();

    private void Interventions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = ((DataGrid)sender).SelectedItem != null;
        EditInterventionButton.IsEnabled = hasSelection;
        OpenInterventionDetailsButton.IsEnabled = hasSelection;
        DeleteInterventionButton.IsEnabled = hasSelection;
    }

    private void Appointments_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = ((DataGrid)sender).SelectedItem != null;
        EditAppointmentButton.IsEnabled = hasSelection;
        DeleteAppointmentButton.IsEnabled = hasSelection;
    }
}
