using BeautyStudio.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace BeautyStudio.Services;

public sealed class BackupService
{
    private const string ManifestName = "backup-manifest.json";
    private static string BackupFolder => Path.Combine(AppDbContext.DataDirectory, "Backups");

    public string? CreateAutomaticBackupIfNeeded()
    {
        Directory.CreateDirectory(BackupFolder);
        var today = Path.Combine(BackupFolder, $"BeautyStudio_{DateTime.Now:yyyyMMdd}_automatico.zip");
        if (File.Exists(today) && ValidateBackup(today, out _)) return null;
        var destination = File.Exists(today)
            ? Path.Combine(BackupFolder, $"BeautyStudio_{DateTime.Now:yyyyMMdd}_automatico_{DateTime.Now:HHmmss}.zip")
            : today;
        return CreateBackup(destination);
    }

    public string CreateBackup() =>
        CreateBackup(Path.Combine(BackupFolder, $"BeautyStudio_{DateTime.Now:yyyyMMdd_HHmmss}.zip"));

    public bool ValidateBackup(string zipPath, out string message)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var archiveEntry in archive.Entries)
            {
                var normalized = archiveEntry.FullName.Replace('\\\\', '/');
                if (Path.IsPathRooted(normalized) || normalized.Split('/').Any(x => x == ".."))
                    throw new InvalidDataException("Il backup contiene un percorso non consentito.");
            }

            var manifestEntry = archive.GetEntry(ManifestName);
            if (manifestEntry == null)
            {
                // Compatibilità con i backup ZIP creati dalle versioni precedenti, senza manifest/checksum.
                if (archive.GetEntry("Database/BeautyStudio.db") == null)
                    throw new InvalidDataException("Il backup non contiene il database.");
                message = "Backup precedente al sistema di checksum. Il database verrà comunque controllato durante il ripristino.";
                return true;
            }
            using var stream = manifestEntry.Open();
            var manifest = JsonSerializer.Deserialize<BackupManifest>(stream)
                ?? throw new InvalidDataException("Manifest del backup non valido.");

            if (manifest.FormatVersion != 1 || !manifest.Files.ContainsKey("Database/BeautyStudio.db"))
                throw new InvalidDataException("Formato del backup non riconosciuto.");

            foreach (var item in manifest.Files)
            {
                var entry = archive.GetEntry(item.Key)
                    ?? throw new InvalidDataException($"File mancante nel backup: {item.Key}");
                using var input = entry.Open();
                var hash = Convert.ToHexString(SHA256.HashData(input));
                if (!hash.Equals(item.Value, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Verifica checksum non riuscita: {item.Key}");
            }

            message = $"Backup valido. File verificati: {manifest.Files.Count}.";
            return true;
        }
        catch (Exception ex)
        {
            message = "Backup non valido: " + ex.Message;
            return false;
        }
    }

    private string CreateBackup(string destination)
    {
        Directory.CreateDirectory(BackupFolder);
        if (!File.Exists(AppDbContext.DatabasePath))
            throw new FileNotFoundException("Database BeautyStudio non trovato.", AppDbContext.DatabasePath);

        var tempDatabase = Path.Combine(Path.GetTempPath(), $"BeautyStudioBackup_{Guid.NewGuid():N}.db");
        var tempZip = destination + ".tmp";
        try
        {
            using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = AppDbContext.DatabasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString()))
            using (var target = new SqliteConnection($"Data Source={tempDatabase}"))
            {
                source.Open();
                target.Open();
                source.BackupDatabase(target);
            }

            if (File.Exists(tempZip)) File.Delete(tempZip);
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var archive = ZipFile.Open(tempZip, ZipArchiveMode.Create))
            {
                AddFile(archive, tempDatabase, "Database/BeautyStudio.db", hashes);
                AddDirectory(archive, AppDbContext.PhotosDirectory, "Photos", hashes);
                AddDirectory(archive, Path.Combine(AppDbContext.DataDirectory, "Documents"), "Documents", hashes);
                var manifest = new BackupManifest { CreatedAt = DateTimeOffset.Now, FormatVersion = 1, Files = hashes };
                var entry = archive.CreateEntry(ManifestName, CompressionLevel.Optimal);
                using var stream = entry.Open();
                JsonSerializer.Serialize(stream, manifest, new JsonSerializerOptions { WriteIndented = true });
            }

            if (File.Exists(destination)) File.Delete(destination);
            File.Move(tempZip, destination);
            if (!ValidateBackup(destination, out var validation))
                throw new InvalidDataException(validation);
            return destination;
        }
        finally
        {
            if (File.Exists(tempDatabase)) File.Delete(tempDatabase);
            if (File.Exists(tempZip)) File.Delete(tempZip);
        }
    }

    public void RestoreBackup(string zipPath)
    {
        if (!File.Exists(zipPath)) throw new FileNotFoundException("Backup non trovato.", zipPath);
        if (!ValidateBackup(zipPath, out var validation)) throw new InvalidDataException(validation);

        var temp = Path.Combine(Path.GetTempPath(), "BeautyStudioRestore_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName == ManifestName || string.IsNullOrEmpty(entry.Name)) continue;
                    var relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    if (Path.IsPathRooted(relative) || relative.Split(Path.DirectorySeparatorChar).Any(x => x == ".."))
                        throw new InvalidDataException("Il backup contiene un percorso non consentito.");
                    var target = Path.Combine(temp, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    entry.ExtractToFile(target, true);
                }
            }

            var database = Path.Combine(temp, "Database", "BeautyStudio.db");
            if (!File.Exists(database)) throw new InvalidDataException("Il backup non contiene il database.");

            // Crea un backup di sicurezza dei dati attuali prima di sostituirli.
            CreateBackup();
            var wal = AppDbContext.DatabasePath + "-wal";
            var shm = AppDbContext.DatabasePath + "-shm";
            if (File.Exists(wal)) File.Delete(wal);
            if (File.Exists(shm)) File.Delete(shm);

            Directory.CreateDirectory(Path.GetDirectoryName(AppDbContext.DatabasePath)!);
            var replacement = AppDbContext.DatabasePath + ".restore";
            File.Copy(database, replacement, true);
            using (var check = new SqliteConnection($"Data Source={replacement};Mode=ReadOnly"))
            {
                check.Open();
                using var command = check.CreateCommand();
                command.CommandText = "PRAGMA integrity_check;";
                var result = Convert.ToString(command.ExecuteScalar());
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Il database nel backup non supera il controllo di integrità.");
            }
            File.Move(replacement, AppDbContext.DatabasePath, true);

            ReplaceDirectory(Path.Combine(temp, "Photos"), AppDbContext.PhotosDirectory);
            ReplaceDirectory(Path.Combine(temp, "Documents"), Path.Combine(AppDbContext.DataDirectory, "Documents"));
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }

    private static void ReplaceDirectory(string source, string destination)
    {
        if (Directory.Exists(destination)) Directory.Delete(destination, true);
        if (Directory.Exists(source)) CopyDirectory(source, destination);
        else Directory.CreateDirectory(destination);
    }

    private static void AddFile(ZipArchive archive, string file, string entryName, IDictionary<string, string> hashes)
    {
        using (var input = File.OpenRead(file))
            hashes[entryName] = Convert.ToHexString(SHA256.HashData(input));
        archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
    }

    private static void AddDirectory(ZipArchive archive, string source, string root, IDictionary<string, string> hashes)
    {
        if (!Directory.Exists(source)) return;
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var name = root + "/" + Path.GetRelativePath(source, file).Replace('\\', '/');
            AddFile(archive, file, name, hashes);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }

    private sealed class BackupManifest
    {
        public int FormatVersion { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Dictionary<string, string> Files { get; set; } = new();
    }
}
