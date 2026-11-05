
using BeautyStudio.Models;
using BeautyStudio.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BeautyStudio.ViewModels;

public partial class CustomerEditorViewModel : ObservableObject
{
    private readonly CustomerService _customerService;
    private readonly PhotoStorageService _photoStorageService;

    private readonly int? _customerId;

    public Action<bool?>? CloseAction { get; set; }

    [ObservableProperty]
    private string firstName = string.Empty;

    [ObservableProperty]
    private string lastName = string.Empty;

    [ObservableProperty]
    private DateTime? birthDate;

    [ObservableProperty]
    private string? phone;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? address;

    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private string? photoPath;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isSaving;

    public bool IsEditing => _customerId.HasValue;

    public string WindowTitle =>
        IsEditing ? "Modifica cliente" : "Nuovo cliente";

    public CustomerEditorViewModel(
        CustomerService customerService,
        PhotoStorageService photoStorageService,
        Customer? customer = null)
    {
        _customerService = customerService;
        _photoStorageService = photoStorageService;

        if (customer != null)
        {
            _customerId = customer.Id;

            FirstName = customer.FirstName;
            LastName = customer.LastName;
            BirthDate = customer.BirthDate;
            Phone = customer.Phone;
            Email = customer.Email;
            Address = customer.Address;
            Notes = customer.Notes;
            PhotoPath = customer.PhotoPath;
        }
    }

    [RelayCommand]
    private void SelectPhoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona fotografia cliente",
            Filter = "Immagini|*.jpg;*.jpeg;*.png;*.bmp;*.webp"
        };

        if (dialog.ShowDialog() == true)
        {
            PhotoPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseAction?.Invoke(false);
    }


    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving)
            return;

        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = "Inserisci il nome del cliente.";
            return;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "Inserisci il cognome del cliente.";
            return;
        }

        IsSaving = true;

        try
        {
            string? finalPhotoPath = PhotoPath;

            if (!string.IsNullOrWhiteSpace(PhotoPath) &&
                File.Exists(PhotoPath) &&
                !Path.GetFullPath(PhotoPath).StartsWith(
                    Path.GetFullPath(
                        BeautyStudio.Data.AppDbContext.PhotosDirectory)
                        + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                finalPhotoPath =
                    await _photoStorageService.SaveCustomerPhotoAsync(PhotoPath);
            }

            Customer customer;

            if (_customerId.HasValue)
            {
                customer = await _customerService.GetByIdAsync(
                    _customerId.Value)
                    ?? throw new InvalidOperationException(
                        "Cliente non trovato nel database.");
            }
            else
            {
                customer = new Customer();
            }

            customer.FirstName = FirstName.Trim();
            customer.LastName = LastName.Trim();
            customer.BirthDate = BirthDate;
            customer.Phone = Phone?.Trim();
            customer.Email = Email?.Trim();
            customer.Address = Address?.Trim();
            customer.Notes = Notes?.Trim();
            customer.PhotoPath = finalPhotoPath;

            if (_customerId.HasValue)
                await _customerService.UpdateAsync(customer);
            else
                await _customerService.AddAsync(customer);

            CloseAction?.Invoke(true);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

}
