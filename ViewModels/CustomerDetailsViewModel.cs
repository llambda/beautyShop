
using BeautyStudio.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BeautyStudio.ViewModels;

public partial class CustomerDetailsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    private Customer customer;

    public CustomerDetailsViewModel(
        Customer customer,
        MainViewModel mainViewModel)
    {
        Customer = customer;
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private void BackToCustomers()
    {
        _mainViewModel.ShowCustomers();
    }
}
