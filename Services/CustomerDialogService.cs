
using BeautyStudio.Models;
using BeautyStudio.ViewModels;
using BeautyStudio.Views;

namespace BeautyStudio.Services;

public class CustomerDialogService
{
    private readonly CustomerService _customerService;
    private readonly PhotoStorageService _photoStorageService;

    public CustomerDialogService(
        CustomerService customerService,
        PhotoStorageService photoStorageService)
    {
        _customerService = customerService;
        _photoStorageService = photoStorageService;
    }

    public bool? ShowDialog(Customer? customer = null)
    {
        var viewModel = new CustomerEditorViewModel(
            _customerService,
            _photoStorageService,
            customer);

        var window = new CustomerEditorWindow
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        viewModel.CloseAction = result =>
        {
            window.DialogResult = result;
        };

        return window.ShowDialog();
    }
}
