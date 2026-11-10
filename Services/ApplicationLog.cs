using BeautyStudio.Data;
using System;
using System.IO;

namespace BeautyStudio.Services;

public static class ApplicationLog
{
    private static readonly object Sync = new();
    private static string LogPath => Path.Combine(AppDbContext.DataDirectory, "Logs", $"BeautyStudio_{DateTime.Now:yyyyMMdd}.log");

    public static void Write(string area, Exception exception) =>
        Write(area, exception.ToString());

    public static void Write(string area, string message)
    {
        try
        {
            var path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            lock (Sync)
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{area}] {message}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Il logging non deve mai impedire l'avvio o l'uso dell'applicazione.
        }
    }
}
