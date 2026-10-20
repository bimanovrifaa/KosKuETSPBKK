# Catatan Implementasi KosKu

## Keputusan teknis

Dokumen acuan awal menggambarkan kebutuhan manajemen kos dan skema relasional. Paket kode yang menyertainya mengimplementasikan kebutuhan tersebut sebagai aplikasi desktop Windows mengikuti pola struktur proyek WPF dari contoh ZIP.

SQL Server digunakan sebagai basis data terpusat; koneksi dikonfigurasi melalui KosKu/appsettings.json. Struktur tabel fisik menggunakan nama `Users`, `Tenants`, `Rooms`, `Contracts`, `Payments`, dan `Complaints`; padanannya dalam dokumen analisis adalah `users`, `penghuni`, `kamar`, `kontrak_sewa`, `pembayaran`, dan `komplain`.

## Alur tagihan

1. Admin memilih **Generate Tagihan Bulan Ini**.
2. Sistem mengambil kontrak berstatus Aktif yang periodenya mencakup bulan berjalan.
3. Sistem menyimpan satu tagihan per kombinasi kontrak dan periode.
4. Penghuni mengunggah bukti bayar dari menu Pembayaran.
5. Admin membuka file bukti, lalu memverifikasi atau menolak dengan alasan.

## Alur komplain

1. Penghuni mengisi judul dan deskripsi masalah.
2. Komplain tersimpan dengan status `Diajukan`.
3. Admin dapat mengubahnya menjadi `Diproses`, menyimpan tanggapan, atau menyelesaikannya.
4. Penghuni melihat status dan tanggapan tersebut di akunnya.

## Pengujian manual yang disarankan

- Login dengan kedua akun demo dan uji akses peran.
- Masukkan kamar dengan nomor duplikat atau harga tidak valid.
- Buat kontrak pada kamar yang sama dan periode bertabrakan.
- Generate tagihan dua kali untuk bulan yang sama.
- Uji upload bukti, buka bukti, verifikasi, dan penolakan.
- Buat komplain sebagai penghuni dan selesaikan sebagai admin.
- Ekspor tiap jenis CSV dan buka menggunakan Excel/LibreOffice.


## Tema Light dan Dark

Tombol **☾ Dark mode / ☀ Light mode** tersedia di halaman login dan header dashboard. Preferensi tema disimpan di `%LOCALAPPDATA%\KosKu\theme.txt`. Pergantian tema mempertahankan halaman aktif pada dashboard dengan membuat ulang jendela agar seluruh style berbasis resource mengikuti palet baru. Palet berada di `KosKu/Styles/Colors.xaml` dan `KosKu/Styles/DarkColors.xaml`, sedangkan pengelolaan preferensi ada di `KosKu/Services/ThemeService.cs`.


## Registrasi akun penghuni

Pengunjung dapat memilih **Belum punya akun? Daftar sebagai penghuni** pada halaman login. Form registrasi meminta nama lengkap, NIM/NPM, universitas, nomor telepon, kontak darurat, email, username, password, dan konfirmasi password; alamat bersifat opsional. Validasi dilakukan di `AuthService`, sedangkan penyimpanan dua entitas (`Users` dan `Tenants`) dilakukan dalam satu transaksi di `KosRepository`. Username, email, dan NIM harus unik. Password minimal 8 karakter dan disimpan menggunakan PBKDF2-SHA256. Pendaftaran publik selalu membuat role `penghuni`, tidak dapat membuat akun admin, dan setelah berhasil pengguna dikembalikan ke halaman login.
