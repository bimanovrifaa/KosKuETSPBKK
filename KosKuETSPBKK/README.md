# KosKu — Sistem Informasi Pengelolaan Kos Mahasiswa

KosKu adalah aplikasi desktop Windows untuk membantu pemilik/pengelola kos dan penghuni mahasiswa mengelola kamar, profil penghuni, kontrak sewa, tagihan bulanan, bukti pembayaran, dan komplain fasilitas.

## 1. Teknologi

- **Platform:** Windows 10/11
- **UI:** C# + WPF + XAML
- **Framework:** .NET 8 Windows Desktop
- **Database:** Microsoft SQL Server (Express/Developer/Standard)
- **Data access:** Microsoft.Data.SqlClient dengan parameterized query
- **Arsitektur:** pemisahan Model, Data/Repository, Services, Views, dan Dialogs
- **Password:** PBKDF2-SHA256 dengan salt acak; bukan teks biasa

KosKu menggunakan SQL Server. Aplikasi akan mencoba membuat database `KosKu` dan tabel-tabelnya saat pertama kali dijalankan. Akun SQL Server yang dipakai harus memiliki izin membuat database. Pada restore pertama, .NET mengunduh paket NuGet `Microsoft.Data.SqlClient`; koneksi internet diperlukan jika paket belum tersimpan di komputer.

## 2. Cara Menjalankan



1. Ekstrak seluruh ZIP ke folder lokal, misalnya `C:\KosKu`.
2. Pastikan **.NET 8 SDK** sudah terpasang di Windows.
3. Klik dua kali `Jalankan-KosKu.bat`.
4. Tunggu proses restore/build pertama selesai, lalu login.

### Melalui terminal

Buka PowerShell pada folder hasil ekstrak, kemudian jalankan:

```powershell
dotnet run --project .\KosKu\KosKu.csproj
```

Atau buka `KosKu.sln` menggunakan Visual Studio 2022 yang mendukung .NET 8, kemudian tekan **F5**.

Untuk menjalankan smoke test sederhana setelah restore, gunakan:

```powershell
dotnet run --project .\KosKu.Tests\KosKu.Tests.csproj
```

> Aplikasi WPF ini berjalan di Windows, bukan langsung di macOS/Linux atau browser. File ZIP ini berisi kode sumber, bukan executable `.exe` hasil build.

## 3. Akun Demo

| Peran | Username | Password |
|---|---|---|
| Admin/pengelola | `admin` | `admin123` |
| Penghuni mahasiswa | `mahasiswa` | `mahasiswa123` |

Data demo dibuat otomatis saat database baru pertama kali dibuat. Ganti kebiasaan password demo sebelum aplikasi digunakan untuk data nyata.

## 4. Fitur yang Tersedia

### Admin/pengelola

- Dashboard dengan total kamar, kamar tersedia/terisi, tagihan menunggu, pendapatan bulan berjalan, dan komplain aktif.
- CRUD kamar: nomor, tipe, harga, fasilitas, status, dan keterangan.
- CRUD data penghuni dan pembuatan akun login penghuni.
- Pembuatan kontrak sewa dan pembaruan status kontrak.
- Pembuatan tagihan untuk kontrak aktif pada periode bulan berjalan tanpa menggandakan tagihan yang sudah ada.
- Membuka bukti pembayaran, memverifikasi pembayaran menjadi Lunas, atau menolak pembayaran dengan alasan.
- Meninjau, menanggapi, memproses, dan menyelesaikan komplain.
- Ringkasan indikator dan ekspor data kamar, penghuni, kontrak, pembayaran, serta komplain ke CSV UTF-8.

### Penghuni

- Dashboard ringkas tentang sewa bulanan, tagihan yang belum lunas, sisa masa sewa, dan komplain.
- Melihat kontrak aktif dan daftar tagihan milik sendiri.
- Mengunggah bukti pembayaran (gambar atau PDF) untuk diverifikasi pengelola.
- Membuat komplain fasilitas dan melihat status serta tanggapan pengelola.
- Tidak dapat membuka data penghuni lain atau melakukan operasi administrasi.

### Registrasi akun penghuni

- Tombol **Belum punya akun? Daftar sebagai penghuni** tersedia pada halaman login.
- Form pendaftaran meminta nama lengkap, NIM/NPM, universitas, telepon, kontak darurat, email, username, password, konfirmasi password, dan alamat opsional.
- Username, email, dan NIM/NPM diperiksa agar tidak terduplikasi. Password minimal 8 karakter dan disimpan sebagai hash PBKDF2-SHA256.
- Registrasi publik selalu membuat akun berperan `penghuni`; form tidak menyediakan opsi membuat akun admin.
- Setelah registrasi berhasil, pengguna dikembalikan ke halaman login dengan username terisi.

## 5. Penyimpanan Data

Data utama tersimpan pada database SQL Server yang ditentukan melalui `KosKu/appsettings.json`. Bukti pembayaran disimpan pada komputer Windows pengguna di `%LOCALAPPDATA%\KosKu\uploads\`. Jika SQL Server berjalan di komputer server/jaringan, beberapa komputer dapat memakai database yang sama selama jaringan, izin login, firewall, dan konfigurasi SQL Server mengizinkannya.

## 6. Menghubungkan SQL Server

1. Pasang SQL Server Express atau Developer dan SQL Server Management Studio (SSMS).
2. Buka `KosKu/appsettings.json` dan sesuaikan `Server` dengan nama instance di komputer kamu. Contoh:
   - SQL Server Express: `Server=.\\SQLEXPRESS;...`
   - Default instance: `Server=localhost;...`
   - LocalDB: `Server=(localdb)\\MSSQLLocalDB;...` (dukungan tergantung instalasi dan izin LocalDB)
3. Contoh konfigurasi Windows Authentication:

   ```json
   {
     "ConnectionStrings": {
       "KosKu": "Server=.\\SQLEXPRESS;Database=KosKu;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True;"
     }
   }
   ```

4. Jalankan `dotnet restore .\KosKu\KosKu.csproj`, kemudian `dotnet run --project .\KosKu\KosKu.csproj`.
5. Saat startup, KosKu mencoba membuat database `KosKu` (jika belum ada), tabel, indeks, dan data demo. Login Windows yang menjalankan aplikasi perlu memiliki izin membuat database. Jika tidak, minta administrator membuat database melalui `Database/create_database.sql` dan memberi izin ke login Windows tersebut.
6. Jika menggunakan SQL Authentication, ganti `Trusted_Connection=True` dengan `User ID=...;Password=...;` dan jangan membagikan file konfigurasi yang berisi password.

**Catatan:** aplikasi ini sekarang menggunakan SQL Server, bukan database SQLite lama. Data pada `%LOCALAPPDATA%\KosKu\kosku.db` tidak otomatis dipindahkan. Jika ada data lama yang ingin dipertahankan, lakukan migrasi terpisah.

## 7. Struktur Proyek

```text
KosKuProject/
├── KosKu.sln
├── KosKu.Tests/
│   ├── KosKu.Tests.csproj
│   └── Program.cs
├── Jalankan-KosKu.bat
├── README.md
├── PRD.md
├── Database/
│   ├── schema.sql
│   └── seed.sql
└── KosKu/
    ├── KosKu.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── Data/
    │   ├── DatabaseConfig.cs
    │   ├── DatabaseInitializer.cs
    │   └── KosRepository.cs
    ├── Models/
    │   └── User.cs
    ├── Services/
    │   ├── ThemeService.cs
    │   ├── AuthService.cs
    │   ├── CsvExportService.cs
    │   └── PasswordHasher.cs
    ├── Styles/
    │   ├── Colors.xaml
    │   ├── DarkColors.xaml
    │   └── Styles.xaml
    └── Views/
        ├── LoginWindow.xaml
        ├── LoginWindow.xaml.cs
        ├── RegisterWindow.xaml
        ├── RegisterWindow.xaml.cs
        └── Dialogs/
            ├── EntityDialog.cs
            └── FormField.cs
```

## 8. Alur Uji Cepat

1. Login sebagai `admin`.
2. Buka **Manajemen Kamar**, tambah kamar, lalu cari dengan kotak pencarian.
3. Buka **Data Penghuni**, buat akun penghuni baru.
4. Buka **Kontrak Sewa**, buat kontrak dengan memilih penghuni dan kamar tersedia.
5. Buka **Pembayaran**, klik **Generate Tagihan Bulan Ini**. Tagihan periode yang sama tidak akan digandakan.
6. Logout, login sebagai penghuni demo, lalu buka **Pembayaran** dan unggah bukti pada tagihan yang belum lunas.
7. Login sebagai admin lagi, pilih tagihan, klik **Buka Bukti**, lalu **Verifikasi Lunas** atau **Tolak Pembayaran**.
8. Login sebagai penghuni dan buat komplain. Admin dapat memberi tanggapan dan memperbarui statusnya.
9. Buka **Laporan & Ekspor** dan simpan salah satu laporan CSV.

## 9. Batasan dan Catatan

- Aplikasi ini merupakan implementasi awal untuk pembelajaran/prototipe lokal. Sebelum digunakan operasional, lakukan build dan pengujian di komputer target.
- Notifikasi email, pembayaran online, tanda tangan digital, unggah ke cloud, dan multi-user melalui jaringan belum diimplementasikan.
- Penghapusan kamar/penghuni yang sudah memiliki histori kontrak atau komplain dibatasi untuk menjaga integritas data.
- Akun demo dan data contoh hanya untuk pengujian. Database dapat di-reset dengan menutup aplikasi lalu menghapus `kosku.db`; tindakan ini menghapus semua data lokal.


## Jika aplikasi tertutup atau gagal membuka dashboard

Versi diagnostik ini menyimpan exception ke file berikut:

`%LOCALAPPDATA%\KosKu\logs\kosku-error.log`

Jika login berhasil tetapi dashboard gagal dimuat, aplikasi akan menampilkan pesan dan mempertahankan jendela login jika memungkinkan. Kirimkan isi file log tersebut untuk mengetahui penyebab pastinya. Jangan menghapus `kosku.db` sebelum membuat cadangan karena file tersebut berisi data lokal.



