
using System.Windows.Controls;

namespace BeautyStudio.Views;

public partial class CustomerDetailsView : UserControl
{
    public CustomerDetailsView()
    {
        InitializeComponent();
    }

    private void Interventions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool selected = ((DataGrid)sender).SelectedItem is not null;
        EditInterventionButton.IsEnabled = selected;
        DeleteInterventionButton.IsEnabled = selected;
    }
}
