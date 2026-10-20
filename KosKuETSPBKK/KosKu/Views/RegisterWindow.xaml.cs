using System.Windows;
using KosKu.Services;

namespace KosKu.Views;

public partial class RegisterWindow : Window
{
    private readonly AuthService _authService = new();

    public string RegisteredUsername { get; private set; } = string.Empty;

    public RegisterWindow()
    {
        InitializeComponent();
        FullNameBox.Focus();
        ConfirmPasswordBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) Register();
        };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Register_Click(object sender, RoutedEventArgs e) => Register();

    private void Register()
    {
        ErrorText.Text = string.Empty;
        try
        {
            _authService.RegisterTenant(
                FullNameBox.Text,
                StudentIdBox.Text,
                UniversityBox.Text,
                PhoneBox.Text,
                EmergencyContactBox.Text,
                EmailBox.Text,
                UsernameBox.Text,
                PasswordBox.Password,
                ConfirmPasswordBox.Password,
                AddressBox.Text);

            RegisteredUsername = UsernameBox.Text.Trim();
            MessageBox.Show(
                "Pendaftaran berhasil. Silakan masuk menggunakan username dan password yang baru dibuat.",
                "Akun KosKu Berhasil Dibuat", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (InvalidOperationException ex)
        {
            ErrorText.Text = ex.Message;
        }
        catch (Exception ex)
        {
            CrashLogger.Log("Registrasi akun penghuni", ex);
            ErrorText.Text = "Pendaftaran belum berhasil karena terjadi kesalahan. Periksa data lalu coba lagi. Jika berulang, hubungi pengelola.";
        }
    }
}
