
using BeautyStudio.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BeautyStudio.Services;

public class PhotoStorageService
{
    public async Task<string> SaveCustomerPhotoAsync(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Fotografia non trovata.");

        string extension = Path.GetExtension(sourcePath);

        string fileName = $"{Guid.NewGuid()}{extension}";

        string directory = Path.Combine(
            AppDbContext.PhotosDirectory,
            "Customers");

        Directory.CreateDirectory(directory);

        string destination = Path.Combine(directory, fileName);

        await Task.Run(() => File.Copy(sourcePath, destination));

        return destination;
    }
}
