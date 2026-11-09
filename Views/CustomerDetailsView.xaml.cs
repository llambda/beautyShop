using System.Windows.Controls;
using BeautyStudio.ViewModels;

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

    private void Interventions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem != null
            && DataContext is CustomerDetailsViewModel viewModel)
            viewModel.OpenInterventionDetailsCommand.Execute(grid.SelectedItem);
    }

    private void Appointments_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = ((DataGrid)sender).SelectedItem != null;
        EditAppointmentButton.IsEnabled = hasSelection;
        DeleteAppointmentButton.IsEnabled = hasSelection;
    }
}
