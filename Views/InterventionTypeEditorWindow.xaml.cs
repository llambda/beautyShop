using System.Windows;
using BeautyStudio.Models;

namespace BeautyStudio.Views;

public partial class InterventionTypeEditorWindow : Window
{
    public string TypeName => NameBox.Text.Trim();
    public string? TypeDescription => string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();

    public InterventionTypeEditorWindow(InterventionType? type)
    {
        InitializeComponent();
        TitleText.Text = type is null ? "Nuovo tipo" : "Modifica tipo";
        if (type is not null)
        {
            NameBox.Text = type.Name;
            DescriptionBox.Text = type.Description ?? string.Empty;
        }
        Loaded += (_, _) => NameBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = "Inserisci il nome del tipo.";
            NameBox.Focus();
            return;
        }
        DialogResult = true;
    }
}
