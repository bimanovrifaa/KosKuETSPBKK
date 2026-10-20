using System.IO;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace KosKu.Data;

public static class DatabaseConfig
{
    private const string DefaultConnectionString =
        "Server=.\\SQLEXPRESS;Database=KosKu;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;";

    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KosKu");

    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static string ConnectionString
    {
        get
        {
            var fromEnvironment = Environment.GetEnvironmentVariable("KOSKU_SQLSERVER_CONNECTION");
            if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;

            try
            {
                if (File.Exists(ConfigPath))
                {
                    using var json = JsonDocument.Parse(File.ReadAllText(ConfigPath));
                    if (json.RootElement.TryGetProperty("ConnectionStrings", out var strings) &&
                        strings.TryGetProperty("KosKu", out var connection) &&
                        !string.IsNullOrWhiteSpace(connection.GetString()))
                        return connection.GetString()!;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gagal membaca konfigurasi SQL Server pada {ConfigPath}: {ex.Message}", ex);
            }

            return DefaultConnectionString;
        }
    }

    public static SqlConnection GetConnection() => new(ConnectionString);
}