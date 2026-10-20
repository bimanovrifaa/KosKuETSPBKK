using System.Data;
using System.IO;
using KosKu.Models;
using KosKu.Services;
using Microsoft.Data.SqlClient;

namespace KosKu.Data;

public sealed class KosRepository
{
    public DataTable Query(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        using var reader = command.ExecuteReader();

        // Build the result table from the reader's values instead of DataTable.Load().
        // DataTable.Load() may import key/unique/nullability metadata from joined provider queries;
        // provider metadata can otherwise leak constraints into a presentation-only table.
        // This UI result table is read-only data, so it should not enforce database constraints.
        var table = new DataTable();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var originalName = reader.GetName(i);
            if (string.IsNullOrWhiteSpace(originalName))
                originalName = $"Column{i + 1}";

            var columnName = originalName;
            var suffix = 2;
            while (table.Columns.Contains(columnName))
                columnName = $"{originalName}_{suffix++}";

            // Database providers may return mixed CLR types across projections. Use object columns to avoid coercing a later row
            // to the first row's storage type. The boxed values retain their actual types.
            // No primary key/unique constraints are copied into this presentation table.
            table.Columns.Add(new DataColumn(columnName, typeof(object))
            {
                AllowDBNull = true,
                Unique = false
            });
        }

        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            table.Rows.Add(values);
        }

        return table;
    }

    public int Execute(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return command.ExecuteNonQuery();
    }

    public object? Scalar(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddParameters(command, parameters);
        return command.ExecuteScalar();
    }

    public int ScalarInt(string sql, params (string Name, object? Value)[] parameters)
    {
        var value = Scalar(sql, parameters);
        return value is null or DBNull ? 0 : Convert.ToInt32(value);
    }

    public decimal ScalarDecimal(string sql, params (string Name, object? Value)[] parameters)
    {
        var value = Scalar(sql, parameters);
        return value is null or DBNull ? 0m : Convert.ToDecimal(value);
    }

    private static void AddParameters(SqlCommand command, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var parameter in parameters)
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
    }

    public DataTable GetRooms() => Query(@"
SELECT room_id AS _id, room_number AS 'No. Kamar', room_type AS 'Tipe Kamar', monthly_price AS 'Harga Bulanan',
       status AS Status, COALESCE(facilities,'') AS Fasilitas, COALESCE(description,'') AS Keterangan
FROM Rooms ORDER BY room_number;");

    public DataTable GetTenants() => Query(@"
SELECT t.tenant_id AS _id, t.full_name AS 'Nama Lengkap', t.student_id AS NIM,
       t.university AS Kampus, t.phone AS Telepon, t.emergency_contact AS 'Kontak Darurat',
       u.username AS Username
FROM Tenants t JOIN Users u ON u.user_id=t.user_id ORDER BY t.full_name;");

    public DataTable GetContracts() => Query(@"
SELECT c.contract_id AS _id, t.full_name AS Penghuni, t.student_id AS NIM,
       r.room_number AS Kamar, c.start_date AS 'Tanggal Mulai', c.end_date AS 'Tanggal Selesai',
       c.monthly_price AS 'Sewa per Bulan', c.status AS Status
FROM Contracts c JOIN Tenants t ON t.tenant_id=c.tenant_id
JOIN Rooms r ON r.room_id=c.room_id ORDER BY c.created_at DESC;");

    public DataTable GetPayments(User user, int? limit = null)
    {
        string where = user.IsAdmin ? "" : "WHERE t.tenant_id=@tenantId";
        string limitClause = limit.HasValue ? $"TOP ({Math.Max(1, limit.Value)}) " : "";
        var parameters = user.IsAdmin
            ? Array.Empty<(string Name, object? Value)>()
            : new (string Name, object? Value)[] { ("@tenantId", (object?)user.TenantId ?? DBNull.Value) };
        return Query($@"
SELECT {limitClause}p.payment_id AS _id, t.full_name AS Penghuni, r.room_number AS Kamar,
       p.billing_period AS Periode, p.bill_date AS 'Tanggal Tagihan', p.due_date AS 'Jatuh Tempo',
       p.amount AS Nominal, p.status AS Status,
       CASE WHEN COALESCE(p.proof_path,'')='' THEN 'Belum diunggah' ELSE 'Tersedia' END AS 'Bukti Bayar',
       COALESCE(p.proof_path,'') AS _proof_path, COALESCE(p.notes,'') AS _notes
FROM Payments p JOIN Contracts c ON c.contract_id=p.contract_id
JOIN Tenants t ON t.tenant_id=c.tenant_id JOIN Rooms r ON r.room_id=c.room_id
{where}
ORDER BY p.billing_period DESC, t.full_name;", parameters);
    }

    public DataTable GetComplaints(User user)
    {
        string where = user.IsAdmin ? "" : "WHERE c.tenant_id=@tenantId";
        var parameters = user.IsAdmin
            ? Array.Empty<(string Name, object? Value)>()
            : new (string Name, object? Value)[] { ("@tenantId", (object?)user.TenantId ?? DBNull.Value) };
        return Query($@"
SELECT c.complaint_id AS _id, t.full_name AS Penghuni, c.title AS Judul,
       c.description AS Deskripsi, c.reported_at AS 'Tanggal Lapor', c.status AS Status,
       COALESCE(c.admin_response,'') AS 'Tanggapan Pengelola'
FROM Complaints c JOIN Tenants t ON t.tenant_id=c.tenant_id
{where}
ORDER BY CASE c.status WHEN 'Diajukan' THEN 0 WHEN 'Diproses' THEN 1 ELSE 2 END, c.reported_at DESC;", parameters);
    }

    public DataTable GetRecentPayments(User user, int limit = 5) => GetPayments(user, limit);

    // Apply DATABASE_DEFAULT to every string operand used by CONCAT. Existing SQL Server
    // installations may have mixed column collations (for example, SQL_Latin1_General_CP1_CI_AS
    // and Latin1_General_100_CI_AS); explicit collation here prevents runtime conflicts without
    // altering the user's existing database or data.
    public DataTable GetTenantChoices() => Query(@"
SELECT tenant_id AS Value,
       CONCAT(full_name COLLATE DATABASE_DEFAULT, N' • ', student_id COLLATE DATABASE_DEFAULT) AS Label
FROM dbo.Tenants
ORDER BY full_name;");

    public DataTable GetAvailableRoomChoices() => Query(@"
SELECT room_id AS Value,
       CONCAT(room_number COLLATE DATABASE_DEFAULT, N' • ', room_type COLLATE DATABASE_DEFAULT,
              N' • Rp ', FORMAT(monthly_price, 'N0', 'id-ID') COLLATE DATABASE_DEFAULT) AS Label,
       monthly_price AS Price
FROM dbo.Rooms
WHERE status='Tersedia'
ORDER BY room_number;");

    public void CreateRoom(string number, string type, decimal price, string status, string facilities, string description)
    {
        if (price <= 0) throw new InvalidOperationException("Harga bulanan harus lebih besar dari nol.");
        Execute(@"INSERT INTO Rooms(room_number,room_type,monthly_price,status,facilities,description)
VALUES(@number,@type,@price,@status,@facilities,@description);",
            ("@number", number.Trim()), ("@type", type), ("@price", price), ("@status", status),
            ("@facilities", EmptyToNull(facilities)), ("@description", EmptyToNull(description)));
    }

    public void UpdateRoom(int id, string number, string type, decimal price, string status, string facilities, string description)
    {
        if (price <= 0) throw new InvalidOperationException("Harga bulanan harus lebih besar dari nol.");
        if (status == "Tersedia" && ScalarInt("SELECT COUNT(*) FROM Contracts WHERE room_id=@id AND status='Aktif';", ("@id", id)) > 0)
            throw new InvalidOperationException("Kamar masih memiliki kontrak aktif. Selesaikan kontrak terlebih dahulu.");
        if (status == "Terisi" && ScalarInt("SELECT COUNT(*) FROM Contracts WHERE room_id=@id AND status='Aktif';", ("@id", id)) == 0)
            throw new InvalidOperationException("Status Terisi harus didukung oleh kontrak aktif. Buat kontrak terlebih dahulu.");
        Execute(@"UPDATE Rooms SET room_number=@number, room_type=@type, monthly_price=@price,
status=@status, facilities=@facilities, description=@description WHERE room_id=@id;",
            ("@number", number.Trim()), ("@type", type), ("@price", price), ("@status", status),
            ("@facilities", EmptyToNull(facilities)), ("@description", EmptyToNull(description)), ("@id", id));
    }

    public void DeleteRoom(int id)
    {
        if (ScalarInt("SELECT COUNT(*) FROM Contracts WHERE room_id=@id;", ("@id", id)) > 0)
            throw new InvalidOperationException("Kamar memiliki riwayat kontrak dan tidak dapat dihapus. Ubah status menjadi Perbaikan bila sudah tidak digunakan.");
        Execute("DELETE FROM Rooms WHERE room_id=@id;", ("@id", id));
    }

    public void RegisterTenantAccount(string fullName, string studentId, string university, string phone,
        string emergencyContact, string email, string username, string password, string address)
    {
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        bool Exists(string sql, string parameterName, string value)
        {
            using var check = connection.CreateCommand();
            check.Transaction = transaction;
            check.CommandText = sql;
            check.Parameters.AddWithValue(parameterName, value.Trim());
            return Convert.ToInt64(check.ExecuteScalar() ?? 0) > 0;
        }

        if (Exists("SELECT COUNT(*) FROM Users WHERE username=@value;", "@value", username))
            throw new InvalidOperationException("Username sudah digunakan. Silakan pilih username lain.");
        if (Exists("SELECT COUNT(*) FROM Users WHERE email=@value;", "@value", email))
            throw new InvalidOperationException("Email sudah terdaftar. Gunakan email lain atau masuk dengan akun yang sudah ada.");
        if (Exists("SELECT COUNT(*) FROM Tenants WHERE student_id=@value;", "@value", studentId))
            throw new InvalidOperationException("NIM/NPM sudah terdaftar. Periksa kembali data atau hubungi pengelola kos.");

        try
        {
            // Return the new identity in the same INSERT statement.
            // SCOPE_IDENTITY() in a separate SqlCommand runs in a separate SQL batch/scope
            // and may return NULL, which would cause the tenant insert to fail its foreign key.
            long userId;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.Users(username,email,password_hash,role,full_name)
OUTPUT INSERTED.user_id
VALUES(@username,@email,@hash,'penghuni',@name);";
                command.Parameters.AddWithValue("@username", username.Trim());
                command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
                command.Parameters.AddWithValue("@hash", PasswordHasher.HashPassword(password));
                command.Parameters.AddWithValue("@name", fullName.Trim());
                userId = Convert.ToInt64(command.ExecuteScalar());
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO Tenants(user_id,full_name,student_id,university,phone,emergency_contact,address)
VALUES(@userId,@name,@studentId,@university,@phone,@emergency,@address);";
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@name", fullName.Trim());
                command.Parameters.AddWithValue("@studentId", studentId.Trim());
                command.Parameters.AddWithValue("@university", university.Trim());
                command.Parameters.AddWithValue("@phone", phone.Trim());
                command.Parameters.AddWithValue("@emergency", emergencyContact.Trim());
                object addressValue = string.IsNullOrWhiteSpace(address) ? DBNull.Value : address.Trim();
                command.Parameters.AddWithValue("@address", addressValue);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            transaction.Rollback();
            throw new InvalidOperationException("Username, email, atau NIM/NPM sudah terdaftar. Periksa kembali data yang kamu masukkan.", ex);
        }
    }

    public void CreateTenant(string fullName, string studentId, string university, string phone,
        string emergencyContact, string username, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new InvalidOperationException("Password akun penghuni minimal 6 karakter.");
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        // Retrieve the generated user_id directly from the INSERT statement.
        long userId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Users(username,email,password_hash,role,full_name)
OUTPUT INSERTED.user_id
VALUES(@username,@email,@hash,'penghuni',@name);";
            command.Parameters.AddWithValue("@username", username.Trim());
            command.Parameters.AddWithValue("@email", $"{username.Trim()}@kosku.local");
            command.Parameters.AddWithValue("@hash", PasswordHasher.HashPassword(password));
            command.Parameters.AddWithValue("@name", fullName.Trim());
            userId = Convert.ToInt64(command.ExecuteScalar());
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Tenants(user_id,full_name,student_id,university,phone,emergency_contact)
VALUES(@userId,@name,@studentId,@university,@phone,@emergency);";
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@name", fullName.Trim());
            command.Parameters.AddWithValue("@studentId", studentId.Trim());
            command.Parameters.AddWithValue("@university", university.Trim());
            command.Parameters.AddWithValue("@phone", phone.Trim());
            command.Parameters.AddWithValue("@emergency", emergencyContact.Trim());
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void UpdateTenant(int id, string fullName, string studentId, string university, string phone, string emergencyContact)
    {
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"UPDATE Tenants SET full_name=@name, student_id=@studentId, university=@university,
phone=@phone, emergency_contact=@emergency WHERE tenant_id=@id;";
            command.Parameters.AddWithValue("@name", fullName.Trim());
            command.Parameters.AddWithValue("@studentId", studentId.Trim());
            command.Parameters.AddWithValue("@university", university.Trim());
            command.Parameters.AddWithValue("@phone", phone.Trim());
            command.Parameters.AddWithValue("@emergency", emergencyContact.Trim());
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "UPDATE Users SET full_name=@name WHERE user_id=(SELECT user_id FROM Tenants WHERE tenant_id=@id);";
            command.Parameters.AddWithValue("@name", fullName.Trim());
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void DeleteTenant(int id)
    {
        if (ScalarInt("SELECT COUNT(*) FROM Contracts WHERE tenant_id=@id;", ("@id", id)) > 0)
            throw new InvalidOperationException("Penghuni memiliki riwayat kontrak. Data tidak dihapus agar histori pembayaran tetap terjaga.");
        if (ScalarInt("SELECT COUNT(*) FROM Complaints WHERE tenant_id=@id;", ("@id", id)) > 0)
            throw new InvalidOperationException("Penghuni memiliki riwayat komplain. Data tidak dihapus agar histori tetap terjaga.");
        var userId = Scalar("SELECT user_id FROM Tenants WHERE tenant_id=@id;", ("@id", id));
        if (userId is null or DBNull) return;
        Execute("DELETE FROM Users WHERE user_id=@userId;", ("@userId", Convert.ToInt64(userId)));
    }

    public void CreateContract(int tenantId, int roomId, DateTime startDate, DateTime endDate, decimal monthlyPrice)
    {
        if (endDate.Date < startDate.Date) throw new InvalidOperationException("Tanggal selesai tidak boleh sebelum tanggal mulai.");
        if (monthlyPrice <= 0) throw new InvalidOperationException("Harga sewa harus lebih besar dari nol.");
        var roomStatus = Convert.ToString(Scalar("SELECT status FROM Rooms WHERE room_id=@id;", ("@id", roomId)));
        if (roomStatus == "Perbaikan") throw new InvalidOperationException("Kamar sedang dalam perbaikan dan tidak dapat disewakan.");
        var overlap = ScalarInt(@"SELECT COUNT(*) FROM Contracts WHERE room_id=@room AND status='Aktif'
AND start_date<=@endDate AND end_date>=@startDate;",
            ("@room", roomId), ("@startDate", startDate.ToString("yyyy-MM-dd")), ("@endDate", endDate.ToString("yyyy-MM-dd")));
        if (overlap > 0) throw new InvalidOperationException("Kamar sudah memiliki kontrak aktif pada rentang tanggal tersebut.");

        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO Contracts(tenant_id,room_id,start_date,end_date,monthly_price,status)
VALUES(@tenant,@room,@start,@end,@price,'Aktif');";
            command.Parameters.AddWithValue("@tenant", tenantId);
            command.Parameters.AddWithValue("@room", roomId);
            command.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@price", monthlyPrice);
            command.ExecuteNonQuery();
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "UPDATE Rooms SET status='Terisi' WHERE room_id=@room;";
            command.Parameters.AddWithValue("@room", roomId);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void SetContractStatus(int id, string status)
    {
        if (!new[] { "Aktif", "Selesai", "Dibatalkan" }.Contains(status))
            throw new InvalidOperationException("Status kontrak tidak valid.");
        var contract = Query("SELECT room_id FROM Contracts WHERE contract_id=@id;", ("@id", id));
        if (contract.Rows.Count == 0) throw new InvalidOperationException("Kontrak tidak ditemukan.");
        int roomId = Convert.ToInt32(contract.Rows[0]["room_id"]);
        if (status == "Aktif")
        {
            var roomStatus = Convert.ToString(Scalar("SELECT status FROM Rooms WHERE room_id=@room;", ("@room", roomId)));
            if (roomStatus == "Perbaikan") throw new InvalidOperationException("Kontrak tidak dapat diaktifkan karena kamar sedang dalam perbaikan.");
            var activeOther = ScalarInt("SELECT COUNT(*) FROM Contracts WHERE room_id=@room AND status='Aktif' AND contract_id<>@id;",
                ("@room", roomId), ("@id", id));
            if (activeOther > 0) throw new InvalidOperationException("Kamar masih terikat pada kontrak aktif lain.");
        }
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "UPDATE Contracts SET status=@status WHERE contract_id=@id;";
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = status == "Aktif"
                ? "UPDATE Rooms SET status='Terisi' WHERE room_id=@room AND status<>'Perbaikan';"
                : "UPDATE Rooms SET status='Tersedia' WHERE room_id=@room AND status='Terisi' AND NOT EXISTS (SELECT 1 FROM Contracts WHERE room_id=@room AND status='Aktif');";
            command.Parameters.AddWithValue("@room", roomId);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public int GenerateMonthlyBills(DateTime month)
    {
        var firstDay = new DateTime(month.Year, month.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var dueDate = firstDay.AddDays(9);
        return Execute(@"INSERT INTO Payments(contract_id,billing_period,bill_date,due_date,amount,status)
SELECT c.contract_id,@period,@billDate,@dueDate,c.monthly_price,'Menunggu'
FROM Contracts c
WHERE c.status='Aktif' AND c.start_date<=@lastDay AND c.end_date>=@firstDay
AND NOT EXISTS (SELECT 1 FROM Payments p WHERE p.contract_id=c.contract_id AND p.billing_period=@period);",
            ("@period", firstDay.ToString("yyyy-MM")), ("@billDate", firstDay.ToString("yyyy-MM-dd")),
            ("@dueDate", dueDate.ToString("yyyy-MM-dd")), ("@lastDay", lastDay.ToString("yyyy-MM-dd")),
            ("@firstDay", firstDay.ToString("yyyy-MM-dd")));
    }

    public void SetPaymentStatus(int paymentId, string status, string? notes = null)
    {
        if (!new[] { "Menunggu", "Lunas", "Ditolak" }.Contains(status))
            throw new InvalidOperationException("Status pembayaran tidak valid.");
        Execute(@"UPDATE Payments SET status=@status, notes=@notes,
paid_at=CASE WHEN @status='Lunas' THEN CURRENT_TIMESTAMP ELSE NULL END WHERE payment_id=@id;",
            ("@status", status), ("@notes", EmptyToNull(notes)), ("@id", paymentId));
    }

    public void UploadPaymentProof(int paymentId, string sourcePath)
    {
        var status = Convert.ToString(Scalar("SELECT status FROM Payments WHERE payment_id=@id;", ("@id", paymentId)));
        if (status is null) throw new InvalidOperationException("Tagihan tidak ditemukan.");
        if (status == "Lunas") throw new InvalidOperationException("Tagihan yang sudah lunas tidak dapat diubah.");
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("File bukti pembayaran tidak ditemukan.", sourcePath);
        var uploadDirectory = Path.Combine(DatabaseConfig.DataDirectory, "uploads");
        Directory.CreateDirectory(uploadDirectory);
        var extension = Path.GetExtension(sourcePath);
        var destination = Path.Combine(uploadDirectory, $"payment_{paymentId}_{DateTime.Now:yyyyMMddHHmmss}{extension}");
        File.Copy(sourcePath, destination, overwrite: false);
        Execute("UPDATE Payments SET proof_path=@path, status='Menunggu', notes=NULL WHERE payment_id=@id;",
            ("@path", destination), ("@id", paymentId));
    }

    public void CreateComplaint(int tenantId, string title, string description)
    {
        Execute("INSERT INTO Complaints(tenant_id,title,description,status) VALUES(@tenant,@title,@description,'Diajukan');",
            ("@tenant", tenantId), ("@title", title.Trim()), ("@description", description.Trim()));
    }

    public void UpdateComplaintStatus(int id, string status)
    {
        if (!new[] { "Diajukan", "Diproses", "Selesai" }.Contains(status))
            throw new InvalidOperationException("Status komplain tidak valid.");
        Execute("UPDATE Complaints SET status=@status WHERE complaint_id=@id;", ("@status", status), ("@id", id));
    }

    public void RespondToComplaint(int id, string response)
    {
        Execute("UPDATE Complaints SET admin_response=@response, status='Diproses' WHERE complaint_id=@id;",
            ("@response", response.Trim()), ("@id", id));
    }

    public DataTable GetExportData(string key, User user)
    {
        return key switch
        {
            "rooms" => Query("SELECT room_number AS 'No. Kamar',room_type AS 'Tipe Kamar',monthly_price AS 'Harga Bulanan',status AS Status,facilities AS Fasilitas,description AS Keterangan FROM Rooms ORDER BY room_number;"),
            "tenants" => Query("SELECT full_name AS 'Nama Lengkap',student_id AS NIM,university AS Kampus,phone AS Telepon,emergency_contact AS 'Kontak Darurat' FROM Tenants ORDER BY full_name;"),
            "contracts" => Query(@"SELECT t.full_name AS Penghuni,t.student_id AS NIM,r.room_number AS Kamar,c.start_date AS 'Tanggal Mulai',c.end_date AS 'Tanggal Selesai',c.monthly_price AS 'Sewa per Bulan',c.status AS Status FROM Contracts c JOIN Tenants t ON t.tenant_id=c.tenant_id JOIN Rooms r ON r.room_id=c.room_id ORDER BY c.start_date DESC;"),
            "payments" => Query(@"SELECT t.full_name AS Penghuni,r.room_number AS Kamar,p.billing_period AS Periode,p.bill_date AS 'Tanggal Tagihan',p.due_date AS 'Jatuh Tempo',p.amount AS Nominal,p.status AS Status,p.proof_path AS 'Path Bukti',p.notes AS Catatan FROM Payments p JOIN Contracts c ON c.contract_id=p.contract_id JOIN Tenants t ON t.tenant_id=c.tenant_id JOIN Rooms r ON r.room_id=c.room_id ORDER BY p.billing_period DESC;"),
            "complaints" => Query(@"SELECT t.full_name AS Penghuni,c.title AS Judul,c.description AS Deskripsi,c.reported_at AS 'Tanggal Lapor',c.status AS Status,c.admin_response AS Tanggapan FROM Complaints c JOIN Tenants t ON t.tenant_id=c.tenant_id ORDER BY c.reported_at DESC;"),
            _ => throw new ArgumentException("Jenis laporan tidak dikenal.", nameof(key))
        };
    }

    public DataTable GetReportSummary()
    {
        var table = new DataTable();
        table.Columns.Add("_id", typeof(int));
        table.Columns.Add("Indikator", typeof(string));
        table.Columns.Add("Nilai", typeof(string));
        var month = DateTime.Today.ToString("yyyy-MM");
        table.Rows.Add(1, "Total kamar", ScalarInt("SELECT COUNT(*) FROM Rooms;").ToString("N0"));
        table.Rows.Add(2, "Kamar tersedia", ScalarInt("SELECT COUNT(*) FROM Rooms WHERE status='Tersedia';").ToString("N0"));
        table.Rows.Add(3, "Penghuni terdaftar", ScalarInt("SELECT COUNT(*) FROM Tenants;").ToString("N0"));
        table.Rows.Add(4, "Kontrak aktif", ScalarInt("SELECT COUNT(*) FROM Contracts WHERE status='Aktif';").ToString("N0"));
        table.Rows.Add(5, "Tagihan menunggu", ScalarInt("SELECT COUNT(*) FROM Payments WHERE status='Menunggu';").ToString("N0"));
        table.Rows.Add(6, "Pendapatan bulan ini", $"Rp {ScalarDecimal("SELECT COALESCE(SUM(amount),0) FROM Payments WHERE status='Lunas' AND billing_period=@month;", ("@month", month)):N0}");
        table.Rows.Add(7, "Komplain belum selesai", ScalarInt("SELECT COUNT(*) FROM Complaints WHERE status<>'Selesai';").ToString("N0"));
        return table;
    }

    public DataTable GetActiveContractForTenant(int tenantId) => Query(@"
SELECT TOP (1) c.contract_id AS _id,r.room_number AS Kamar,r.room_type AS 'Tipe Kamar',c.start_date AS 'Tanggal Mulai',
c.end_date AS 'Tanggal Selesai',c.monthly_price AS 'Sewa per Bulan',c.status AS Status
FROM Contracts c JOIN Rooms r ON r.room_id=c.room_id WHERE c.tenant_id=@tenantId AND c.status='Aktif'
ORDER BY c.start_date DESC;", ("@tenantId", tenantId));

    private static object? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
