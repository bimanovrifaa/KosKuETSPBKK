# Patch Notes — Registrasi KosKu

## Fitur baru
- Tombol registrasi penghuni tersedia di halaman login.
- Form registrasi meminta nama lengkap, NIM/NPM, universitas, telepon, kontak darurat, email, username, password, konfirmasi password, dan alamat opsional.
- Validasi input: kolom wajib, format email, username 3–30 karakter, password minimal 8 karakter, dan kecocokan konfirmasi.
- Validasi duplikasi username, email, dan NIM/NPM.
- Record akun `Users` dan profil `Tenants` dibuat atomik dalam satu transaksi SQL Server.
- Password disimpan dengan PBKDF2-SHA256; role dari registrasi publik dikunci menjadi `penghuni`.
- Setelah sukses, dialog registrasi ditutup dan username otomatis terisi pada halaman login.
- Tampilan menggunakan resource warna aktif sehingga mengikuti Light/Dark Mode.

## Berkas utama yang berubah
- `KosKu/Views/LoginWindow.xaml` dan `.xaml.cs`
- `KosKu/Views/RegisterWindow.xaml` dan `.xaml.cs` (baru)
- `KosKu/Services/AuthService.cs`
- `KosKu/Data/KosRepository.cs`
- `README.md`, `PRD.md`, dan `docs/IMPLEMENTASI.md`

## Pengujian yang perlu dilakukan di Windows
1. Build solution dengan `dotnet build .\KosKu.sln`.
2. Daftarkan penghuni baru dengan data valid lalu login menggunakan akun tersebut.
3. Coba username, email, atau NIM duplikat; sistem harus menampilkan pesan validasi dan tidak membuat akun parsial.
4. Coba password kurang dari 8 karakter dan konfirmasi berbeda.
5. Coba buka registrasi saat tema light dan dark.
6. Pastikan akun hasil registrasi hanya berperan sebagai penghuni.

Kompilasi Windows belum dapat diverifikasi pada lingkungan penyusunan paket ini karena .NET SDK tidak tersedia di lingkungan tersebut.
