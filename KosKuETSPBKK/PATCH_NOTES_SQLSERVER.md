# Patch Notes — SQL Server edition

## Perubahan
- Mengganti provider `Microsoft.Data.Sqlite` menjadi `Microsoft.Data.SqlClient`.
- Menambahkan `KosKu/appsettings.json` sebagai lokasi konfigurasi connection string.
- Default koneksi menggunakan Windows Authentication ke `localhost\SQLEXPRESS` dan database `KosKu`.
- Startup mencoba membuat database jika belum ada (login SQL Server harus memiliki izin `CREATE DATABASE`).
- Mengubah initializer menjadi skema T-SQL: `IDENTITY`, `DATETIME2`, `DECIMAL`, constraints, foreign keys, dan indexes.
- Seed demo menggunakan `SCOPE_IDENTITY()`.
- Mengubah query SQLite-specific (`LIMIT`, `last_insert_rowid`, `INSERT OR IGNORE`, `COLLATE NOCASE`, concatenation `||`, dan `printf`) menjadi SQL Server-compatible.
- Memperbarui dokumentasi dan schema script untuk SQL Server.

## Konfigurasi
Edit `KosKu/appsettings.json` dan sesuaikan `Server` dengan nama instance SQL Server pada komputer.

## Catatan verifikasi
File JSON, struktur XML project, keseimbangan blok kode, dan pemindaian pola SQL khusus SQLite sudah diperiksa secara statis. Kompilasi dan koneksi database aktual belum bisa dijalankan di lingkungan pembuat karena .NET SDK dan SQL Server tidak tersedia di sana.
