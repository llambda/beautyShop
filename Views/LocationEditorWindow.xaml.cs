using System.Windows;
using BeautyStudio.Models;

namespace BeautyStudio.Views;

public partial class LocationEditorWindow : Window
{
    public string LocationName => NameBox.Text.Trim();
    public string? LocationAddress => string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim();
    public string? LocationNotes => string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

    public LocationEditorWindow(InterventionLocation? location)
    {
        InitializeComponent();
        TitleText.Text = location is null ? "Nuovo luogo" : "Modifica luogo";
        if (location is not null)
        {
            NameBox.Text = location.Name;
            AddressBox.Text = location.Address ?? string.Empty;
            NotesBox.Text = location.Notes ?? string.Empty;
        }
        Loaded += (_, _) => NameBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = "Inserisci il nome del luogo.";
            NameBox.Focus();
            return;
        }
        DialogResult = true;
    }
}
