using System.Windows;
using System.Windows.Threading;
using KosKu.Data;
using KosKu.Services;
using KosKu.Views;

namespace KosKu;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += HandleDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += HandleDomainException;
    }

    private void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        CrashLogger.Log("WPF Dispatcher / UI", e.Exception);
        MessageBox.Show(
            $"KosKu mengalami kesalahan saat menampilkan halaman.\n\n" +
            $"Detail: {e.Exception.Message}\n\nLog lengkap tersimpan di:\n{CrashLogger.LogPath}",
            "Kesalahan KosKu", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void HandleDomainException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            CrashLogger.Log("AppDomain / unhandled exception", exception);
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            ThemeService.Initialize();
            DatabaseInitializer.Initialize();
            var login = new LoginWindow();
            MainWindow = login;
            login.Show();
        }
        catch (Exception ex)
        {
            CrashLogger.Log("Inisialisasi aplikasi/database", ex);
            MessageBox.Show(
                $"KosKu tidak dapat terhubung atau menyiapkan database SQL Server.\n\nDetail: {ex.Message}\n\nLog lengkap tersimpan di:\n{CrashLogger.LogPath}",
                "Kesalahan Inisialisasi", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
