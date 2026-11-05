
using BeautyStudio.ViewModels;
using System.Windows;

namespace BeautyStudio;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
