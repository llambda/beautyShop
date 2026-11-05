using BeautyStudio.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BeautyStudio.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void PrimaryPalette_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm || sender is not Button button || button.Tag is not string hex)
            return;
        vm.SelectedPrimaryPalette = vm.PrimaryPalettes.FirstOrDefault(p => p.Hex == hex);
    }

    private void SecondaryPalette_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm || sender is not Button button || button.Tag is not string hex)
            return;
        vm.SelectedSecondaryPalette = vm.SecondaryPalettes.FirstOrDefault(p => p.SecondaryHex == hex);
    }

    private void Locations_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool selected = ((DataGrid)sender).SelectedItem is not null;
        EditLocationButton.IsEnabled = selected;
        DeleteLocationButton.IsEnabled = selected;
    }

    private void Types_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool selected = ((DataGrid)sender).SelectedItem is not null;
        EditTypeButton.IsEnabled = selected;
        DeleteTypeButton.IsEnabled = selected;
    }
}
