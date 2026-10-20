# Patch: Light/Dark Theme

- Menambahkan `ThemeService` dengan penyimpanan preferensi tema di `%LOCALAPPDATA%\KosKu\theme.txt`.
- Menambahkan palet `DarkColors.xaml` dan resource warna netral untuk kontrol, tabel, kartu, dan input.
- Menambahkan tombol ganti tema pada halaman login dan header dashboard.
- Saat tema diubah, jendela dibuat ulang supaya `StaticResource` dan style yang bergantung pada palet dimuat ulang; halaman aktif dipertahankan saat berpindah tema.
- Memperbarui dialog dan input agar menggunakan resource tema, bukan latar putih hard-coded.

Catatan: build WPF harus dikonfirmasi pada Windows dengan .NET 8 SDK. Lingkungan pengemasan ini tidak memiliki `dotnet` untuk kompilasi.
