using System.IO;

namespace KosKu.Services;

/// <summary>Writes diagnostic exceptions to a local log so unexpected UI failures do not disappear silently.</summary>
public static class CrashLogger
{
    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KosKu", "logs", "kosku-error.log");

    public static void Log(string context, Exception exception)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.AppendAllText(LogPath,
                $"{Environment.NewLine}===== {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} | {context} ====={Environment.NewLine}" +
                exception + Environment.NewLine);
        }
        catch
        {
            // Logging must never cause a second failure.
        }
    }
}
