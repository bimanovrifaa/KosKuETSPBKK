# PRODUCT REQUIREMENT DOCUMENT (PRD)
## KosKu — Sistem Informasi Pengelolaan Kos Mahasiswa

**Versi:** 1.0  
**Status:** Draft implementasi awal  
**Platform:** Aplikasi desktop Windows  
**Teknologi:** C#, WPF, XAML, .NET 8, Microsoft SQL Server  
**Dokumen acuan:** *Laporan PRD dan Desain Sistem KosKu.docx* yang disertakan pengguna dan referensi Rich UI SISMA pada blog Fajar Baskoro.

---

## 1. Ringkasan Produk

KosKu adalah aplikasi desktop untuk memusatkan pengelolaan operasional rumah kos mahasiswa. Aplikasi menghubungkan pemilik/pengelola kos dengan penghuni dalam satu sistem yang menyediakan data kamar, profil penghuni, kontrak sewa, tagihan bulanan, verifikasi pembayaran, dan pelacakan komplain fasilitas.

Antarmuka mengikuti pola Rich UI: navigasi sidebar, dashboard berisi kartu statistik, tabel yang dapat dicari, formulir terstruktur, status yang mudah dibaca, dan ekspor laporan. Nilai statistik dashboard dihitung dari database lokal, bukan angka tampilan statis.

## 2. Latar Belakang dan Permasalahan

Pengelolaan kos sering membutuhkan pencatatan terpisah untuk ketersediaan kamar, identitas penghuni, masa sewa, pembayaran, dan kerusakan fasilitas. Cara manual dapat memperlambat pencarian informasi, meningkatkan risiko data ganda, menyulitkan pemantauan tunggakan, serta membuat status komplain tidak transparan.

KosKu dirancang untuk membantu pengelola menyimpan data secara terstruktur dan membantu penghuni melihat kewajiban sewa serta mengirim bukti pembayaran atau laporan fasilitas.

## 3. Tujuan Produk

1. Menyediakan satu aplikasi untuk mengelola data kamar, penghuni, kontrak, pembayaran, dan komplain.
2. Mempercepat pencarian serta pembaruan data menggunakan tabel, formulir tervalidasi, dan pencarian.
3. Mengurangi tagihan ganda melalui batas unik kontrak dan periode tagihan.
4. Menjaga keterkaitan data melalui primary key, foreign key, dan aturan penghapusan.
5. Memberikan informasi operasional melalui dashboard dan ekspor laporan.
6. Membatasi akses berdasarkan peran admin dan penghuni.
7. Menyediakan pilihan tampilan Light Mode dan Dark Mode yang tersimpan untuk sesi berikutnya.

## 4. Target Pengguna

### 4.1 Admin/Pengelola Kos

- Mengelola inventaris kamar.
- Menambahkan dan memperbarui data penghuni serta akun login.
- Membuat kontrak dan mengubah status kontrak.
- Menghasilkan tagihan bulanan, memeriksa bukti, dan memvalidasi pembayaran.
- Meninjau serta menindaklanjuti komplain.
- Melihat ringkasan operasional dan mengekspor laporan.

### 4.2 Penghuni Mahasiswa

- Melihat kontrak aktif dan tagihan milik sendiri.
- Mengunggah bukti pembayaran.
- Mengirim komplain fasilitas.
- Melihat status komplain dan tanggapan pengelola.

## 5. Ruang Lingkup Fitur

| Modul | Admin | Penghuni |
|---|---|---|
| Login/logout | Ya | Ya |
| Registrasi akun | Membuat akun penghuni dari menu pengelolaan | Daftar mandiri dengan data identitas dan kredensial |
| Dashboard | Statistik operasional | Ringkasan sewa pribadi |
| Manajemen kamar | Tambah, lihat, ubah, hapus dengan validasi histori | Tidak |
| Data penghuni | Tambah, lihat, ubah, hapus dengan validasi histori | Tidak; profil dibaca melalui informasi kontrak |
| Kontrak sewa | Buat dan ubah status | Lihat kontrak aktif |
| Tagihan/pembayaran | Generate tagihan, buka bukti, verifikasi atau tolak | Lihat tagihan sendiri, unggah bukti |
| Komplain fasilitas | Lihat semua, beri tanggapan, ubah status | Buat komplain dan pantau status sendiri |
| Laporan/ekspor | Ekspor CSV untuk data utama | Tidak |

### 5.1 Aturan Bisnis Utama

- Nomor kamar, username, email, dan NIM harus unik.
- Harga kamar/sewa harus lebih besar dari nol.
- Tanggal selesai kontrak tidak boleh sebelum tanggal mulai.
- Kamar berstatus Perbaikan tidak dapat dikontrakkan.
- Kontrak aktif tidak boleh tumpang tindih untuk kamar yang sama.
- Status kamar disesuaikan ketika kontrak dibuat atau diselesaikan.
- Satu kontrak hanya memiliki satu tagihan per periode `YYYY-MM`.
- Bukti pembayaran disimpan di direktori lokal aplikasi dan status pembayaran harus diverifikasi pengelola.
- Penghuni hanya dapat melihat pembayaran dan komplain yang terkait dengan profilnya.
- Data kamar/penghuni yang sudah memiliki histori transaksi tidak dapat dihapus sembarangan.

## 6. Kebutuhan Fungsional

**FR-01 Autentikasi:** sistem menerima username/password, memverifikasi hash password, dan menentukan hak akses berdasarkan role.

**FR-01A Registrasi Penghuni:** pengunjung dapat membuat akun penghuni dengan nama lengkap, NIM/NPM, universitas, nomor telepon, kontak darurat, email, username, password dan konfirmasi, serta alamat opsional. Sistem memvalidasi email, panjang dan format username, kecocokan password, serta keunikan username/email/NIM. Registrasi publik tidak dapat membuat akun admin. Kredensial disimpan sebagai hash PBKDF2-SHA256 dan pembuatan record Users/Tenants dilakukan dalam satu transaksi database.

**FR-02 Manajemen Kamar:** admin dapat melihat, menambah, mengubah, mencari, dan menghapus kamar apabila tidak memiliki histori kontrak. Data mencakup nomor, tipe, harga bulanan, status, fasilitas, dan keterangan.

**FR-03 Manajemen Penghuni:** admin dapat mengelola profil mahasiswa dan membuat akun penghuni. Data mencakup nama, NIM, kampus, telepon, kontak darurat, username, dan password awal.

**FR-04 Manajemen Kontrak:** admin dapat membuat kontrak dengan memilih penghuni dan kamar tersedia, mengatur periode serta tarif, dan memperbarui status Aktif/Selesai/Dibatalkan.

**FR-05 Tagihan:** admin dapat membuat tagihan periode bulan berjalan untuk kontrak aktif. Pembuatan berulang tidak menghasilkan tagihan duplikat.

**FR-06 Pembayaran:** penghuni dapat mengunggah bukti gambar/PDF. Admin dapat membuka bukti, menandai pembayaran Lunas, atau menolak dengan alasan.

**FR-07 Komplain:** penghuni dapat mengirim judul dan deskripsi. Admin dapat memberi tanggapan dan mengubah status Diajukan/Diproses/Selesai.

**FR-08 Dashboard:** dashboard menampilkan statistik sesuai peran dan menggunakan data aktual dari database.

**FR-09 Pencarian:** tabel utama menyediakan pencarian teks pada kolom yang terlihat.

**FR-10 Laporan:** admin dapat mengekspor data kamar, penghuni, kontrak, pembayaran, dan komplain ke CSV UTF-8.

**FR-11 Tema Tampilan:** pengguna dapat mengganti Light Mode/Dark Mode dari halaman login maupun dashboard. Pilihan disimpan secara lokal dan diterapkan kembali saat aplikasi dibuka.

## 7. Kebutuhan Non-Fungsional

- **Keamanan:** password disimpan menggunakan PBKDF2-SHA256 dengan salt acak; query database menggunakan parameter.
- **Integritas:** relasi foreign key, unique constraint, check constraint, dan pembatasan penghapusan menjaga konsistensi data.
- **Usabilitas:** sidebar konsisten, formulir memiliki label, pesan validasi jelas, tabel dapat dicari/diurutkan, serta tersedia pilihan Light Mode/Dark Mode.
- **Portabilitas lokal:** database dibuat otomatis di profil pengguna Windows; tidak memerlukan server database terpisah.
- **Kinerja:** operasi CRUD dan pencarian pada data skala kecil/menengah diharapkan selesai dalam waktu singkat; pengukuran formal masih perlu dilakukan pada komputer target.
- **Ketersediaan:** aplikasi berjalan lokal dan tidak memerlukan koneksi internet setelah paket .NET/NuGet tersedia.

## 8. Arsitektur dan Struktur Implementasi

- **Views:** `LoginWindow.xaml`, `RegisterWindow.xaml`, `MainWindow.xaml`, dan dialog formulir.
- **Models:** model akun pengguna.
- **Data:** konfigurasi SQL Server, inisialisasi skema/seed, dan repository akses data.
- **Services:** autentikasi/password hashing serta ekspor CSV.
- **Styles:** palet warna light/dark dan style reusable untuk kartu, tombol, input, dan DataGrid. `ThemeService` menyimpan preferensi tema lokal.

Database utama memiliki entitas `Users`, `Tenants`, `Rooms`, `Contracts`, `Payments`, dan `Complaints`.

## 9. Model Data Konseptual

```text
Users 1 ───── 0..1 Tenants
Tenants 1 ──── N Contracts
Rooms 1 ────── N Contracts
Contracts 1 ── N Payments
Tenants 1 ──── N Complaints
```

- `Users` menyimpan kredensial dan role.
- `Tenants` menyimpan detail identitas mahasiswa dan terhubung ke akun.
- `Rooms` menyimpan inventaris dan status kamar.
- `Contracts` menghubungkan penghuni dan kamar untuk suatu periode sewa.
- `Payments` menyimpan tagihan per kontrak/periode dan status verifikasi.
- `Complaints` menyimpan laporan fasilitas dan tindak lanjutnya.

## 10. Kriteria Penerimaan

1. Admin dapat login dengan akun yang valid dan ditolak ketika password salah.
2. Admin dapat menambah/mengubah/mencari data kamar dan penghuni.
3. Kamar yang sedang dikontrak tidak dapat dikontrakkan ulang untuk periode yang bertabrakan.
4. Pembuatan tagihan pada periode yang sama dapat dijalankan berulang tanpa duplikasi.
5. Penghuni hanya melihat data pembayaran dan komplain miliknya.
6. Bukti pembayaran yang dipilih tersimpan di direktori uploads lokal dan dapat dibuka admin.
7. Status pembayaran dapat berubah menjadi Lunas atau Ditolak dengan validasi dasar.
8. Komplain dapat dibuat penghuni, ditanggapi admin, dan ditandai selesai.
9. Laporan CSV dapat dibuka di spreadsheet dengan karakter UTF-8 yang benar.
10. Database dan data demo dibuat otomatis pada penggunaan pertama.
11. Tema dapat diubah dari halaman login/dashboard dan pilihan tetap aktif setelah aplikasi ditutup lalu dibuka kembali.
12. Mahasiswa dapat mendaftar dengan username, email, dan NIM unik; password tidak disimpan sebagai teks biasa; role hasil registrasi selalu `penghuni`.

## 11. Rencana Implementasi

| Tahap | Aktivitas | Hasil |
|---|---|---|
| 1. Analisis | Menetapkan aktor, kebutuhan, aturan bisnis, dan batasan | PRD |
| 2. Desain | Menyusun skema relasional dan navigasi Rich UI | Skema SQL Server dan rancangan layar |
| 3. Fondasi | Inisialisasi database, seed, hashing password, login | Aplikasi dapat dibuka dan login |
| 4. Master data | Implementasi kamar dan penghuni | CRUD dengan validasi |
| 5. Transaksi | Implementasi kontrak, tagihan, bukti, dan verifikasi | Alur sewa dan pembayaran |
| 6. Layanan | Implementasi komplain, dashboard, dan ekspor CSV | Operasional dan laporan |
| 7. Pengujian | Uji login, CRUD, relasi, validasi, ekspor, dan alur peran | Checklist hasil pengujian |
| 8. Deployment lokal | Build dan jalankan pada Windows dengan .NET SDK | Paket source yang dapat dijalankan |

## 12. Batasan Implementasi Saat Ini

- Belum ada integrasi payment gateway, notifikasi email/WhatsApp, tanda tangan digital, atau sinkronisasi cloud.
- Aplikasi menggunakan database lokal pada satu komputer; multi-user jaringan memerlukan rancangan server terpisah.
- Login demo disediakan untuk uji coba dan harus diganti sebelum penggunaan nyata.
- Pengujian build perlu dilakukan di Windows dengan .NET 8 SDK; lingkungan penyusunan source ini tidak menyediakan `dotnet` untuk menjalankan kompilasi WPF secara langsung.

## 13. Referensi

1. Baskoro, F. (2026). *Rich UI — Sistem Informasi Mahasiswa SISMA*. Blog Fajar Baskoro. https://fajarbaskoro.blogspot.com/2026/10/rich-ui-sistem-informasi-mahasiswa-sisma.html
2. *Laporan PRD dan Desain Sistem KosKu* (dokumen acuan yang diberikan pengguna, tanpa metadata publikasi).

## Registrasi akun penghuni

Pengunjung halaman login dapat membuat akun penghuni melalui form registrasi. Data wajib meliputi nama lengkap, NIM/NPM, universitas, nomor telepon, kontak darurat, email, username, dan password beserta konfirmasi. Alamat domisili bersifat opsional. Username, email, dan NIM/NPM harus unik. Password minimum 8 karakter dan disimpan sebagai hash PBKDF2-SHA256. Registrasi publik hanya dapat membuat role `penghuni`; pembuatan akun admin tetap menjadi wewenang administrator. Setelah pendaftaran berhasil, pengguna diarahkan kembali ke halaman login.
