using System.Windows;
using KosKu.Services;

namespace KosKu.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _authService = new();

    public LoginWindow()
    {
        InitializeComponent();
        ThemeButton.Content = ThemeService.ToggleLabel;
        PasswordBox.Focus();
        PasswordBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) Login();
        };
        UsernameBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) PasswordBox.Focus();
        };
    }

    private void Login_Click(object sender, RoutedEventArgs e) => Login();

    private void Register_Click(object sender, RoutedEventArgs e)
    {
        var registerWindow = new RegisterWindow { Owner = this };
        if (registerWindow.ShowDialog() == true)
        {
            UsernameBox.Text = registerWindow.RegisteredUsername;
            PasswordBox.Clear();
            ErrorText.Text = "Akun berhasil dibuat. Silakan masuk menggunakan password kamu.";
            PasswordBox.Focus();
        }
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        // Preserve form values while rebuilding the window against the new resource dictionary.
        var username = UsernameBox.Text;
        var password = PasswordBox.Password;
        ThemeService.Toggle();
        var themedLogin = new LoginWindow();
        themedLogin.UsernameBox.Text = username;
        themedLogin.PasswordBox.Password = password;
        Application.Current.MainWindow = themedLogin;
        themedLogin.Show();
        Close();
    }

    private void Login()
    {
        ErrorText.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
        {
            ErrorText.Text = "Username dan password wajib diisi.";
            return;
        }

        try
        {
            var user = _authService.Login(UsernameBox.Text, PasswordBox.Password);
            if (user is null)
            {
                ErrorText.Text = "Username atau password tidak sesuai.";
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            // Keep the login window open if dashboard initialization fails.
            var main = new MainWindow(user);
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        }
        catch (Exception ex)
        {
            CrashLogger.Log("Proses login / membuka dashboard", ex);
            MessageBox.Show(
                $"Login berhasil atau sedang diproses, tetapi halaman utama gagal dibuka.\n\n" +
                $"Detail: {ex.Message}\n\n" +
                $"Log lengkap tersimpan di:\n{CrashLogger.LogPath}",
                "KosKu - Gagal Membuka Dashboard", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
