using BeautyStudio.Data;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BeautyStudio.Services;

public sealed class BackupService
{
    public string? CreateAutomaticBackupIfNeeded()
    {
        var folder = Path.Combine(AppDbContext.DataDirectory, "Backups");
        Directory.CreateDirectory(folder);
        var today = Path.Combine(folder, "BeautyStudio_" + DateTime.Now.ToString("yyyyMMdd") + "_automatico.zip");
        if (File.Exists(today)) return null;
        return CreateBackup(today);
    }

    public string CreateBackup() => CreateBackup(Path.Combine(AppDbContext.DataDirectory, "Backups", "BeautyStudio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip"));

    private string CreateBackup(string destination)
    {
        Directory.CreateDirectory(Path.Combine(AppDbContext.DataDirectory, "Backups"));
        if (File.Exists(AppDbContext.DatabasePath))
            using (var archive = ZipFile.Open(destination, ZipArchiveMode.Create))
                AddFile(archive, AppDbContext.DatabasePath, "Database/BeautyStudio.db");

        using (var archive = ZipFile.Open(destination, ZipArchiveMode.Update))
        {
            AddDirectory(archive, AppDbContext.PhotosDirectory, "Photos");
            AddDirectory(archive, Path.Combine(AppDbContext.DataDirectory, "Documents"), "Documents");
        }
        return destination;
    }

    public void RestoreBackup(string zipPath)
    {
        if (!File.Exists(zipPath)) throw new FileNotFoundException("Backup non trovato.", zipPath);
        var temp = Path.Combine(Path.GetTempPath(), "BeautyStudioRestore_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            ZipFile.ExtractToDirectory(zipPath, temp);
            var database = Path.Combine(temp, "Database", "BeautyStudio.db");
            if (!File.Exists(database)) throw new InvalidDataException("Il backup non contiene il database.");
            Directory.CreateDirectory(Path.GetDirectoryName(AppDbContext.DatabasePath)!);
            File.Copy(database, AppDbContext.DatabasePath, true);
            CopyDirectory(Path.Combine(temp, "Photos"), AppDbContext.PhotosDirectory);
            CopyDirectory(Path.Combine(temp, "Documents"), Path.Combine(AppDbContext.DataDirectory, "Documents"));
        }
        finally { Directory.Delete(temp, true); }
    }

    private static void AddFile(ZipArchive archive, string file, string entry)
    {
        if (File.Exists(file)) archive.CreateEntryFromFile(file, entry, CompressionLevel.Optimal);
    }

    private static void AddDirectory(ZipArchive archive, string source, string entryRoot)
    {
        if (!Directory.Exists(source)) return;
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            archive.CreateEntryFromFile(file, entryRoot + "/" + Path.GetRelativePath(source, file).Replace('\\', '/'), CompressionLevel.Optimal);
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) return;
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}