
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows;

namespace BeautyStudio.Converters;

public class PhotoPathToImageConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not string path ||
            string.IsNullOrWhiteSpace(path) ||
            !File.Exists(path))
        {
            return DependencyProperty.UnsetValue;
        }

        try
        {
            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.UriSource = new Uri(
                Path.GetFullPath(path),
                UriKind.Absolute);

            // Carica l'immagine interamente in memoria.
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            // Riduce il consumo di memoria nelle card.
            bitmap.DecodePixelWidth = 460;

            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch (Exception)
        {
            // File inesistente, corrotto o formato non supportato.
            return null!;
        }
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
