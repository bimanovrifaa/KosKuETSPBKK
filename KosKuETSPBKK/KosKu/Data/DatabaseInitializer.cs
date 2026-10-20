using KosKu.Services;
using Microsoft.Data.SqlClient;

namespace KosKu.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        EnsureDatabaseExists();
        using var connection = DatabaseConfig.GetConnection();
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        user_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        username NVARCHAR(30) COLLATE Latin1_General_100_CI_AS NOT NULL,
        email NVARCHAR(254) COLLATE Latin1_General_100_CI_AS NOT NULL,
        password_hash NVARCHAR(500) NOT NULL,
        role NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_role DEFAULT 'penghuni',
        full_name NVARCHAR(100) NOT NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_Users_created_at DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_Users_username UNIQUE(username),
        CONSTRAINT UQ_Users_email UNIQUE(email),
        CONSTRAINT CK_Users_role CHECK(role IN ('admin','penghuni'))
    );
END;
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants (
        tenant_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY,
        user_id INT NOT NULL,
        full_name NVARCHAR(100) NOT NULL,
        student_id NVARCHAR(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        university NVARCHAR(150) NOT NULL,
        phone NVARCHAR(30) NOT NULL,
        emergency_contact NVARCHAR(30) NOT NULL,
        address NVARCHAR(500) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_Tenants_created_at DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_Tenants_user UNIQUE(user_id),
        CONSTRAINT UQ_Tenants_student_id UNIQUE(student_id),
        CONSTRAINT FK_Tenants_Users FOREIGN KEY(user_id) REFERENCES dbo.Users(user_id) ON DELETE CASCADE
    );
END;
IF OBJECT_ID(N'dbo.Rooms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rooms (
        room_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY,
        room_number NVARCHAR(10) NOT NULL,
        room_type NVARCHAR(50) NOT NULL,
        monthly_price DECIMAL(12,2) NOT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_Rooms_status DEFAULT 'Tersedia',
        facilities NVARCHAR(MAX) NULL,
        description NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_Rooms_created_at DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_Rooms_room_number UNIQUE(room_number),
        CONSTRAINT CK_Rooms_price CHECK(monthly_price > 0),
        CONSTRAINT CK_Rooms_status CHECK(status IN ('Tersedia','Terisi','Perbaikan'))
    );
END;
IF OBJECT_ID(N'dbo.Contracts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contracts (
        contract_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Contracts PRIMARY KEY,
        tenant_id INT NOT NULL,
        room_id INT NOT NULL,
        start_date DATE NOT NULL,
        end_date DATE NOT NULL,
        monthly_price DECIMAL(12,2) NOT NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_Contracts_status DEFAULT 'Aktif',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_Contracts_created_at DEFAULT SYSDATETIME(),
        CONSTRAINT CK_Contracts_dates CHECK(end_date >= start_date),
        CONSTRAINT CK_Contracts_status CHECK(status IN ('Aktif','Selesai','Dibatalkan')),
        CONSTRAINT FK_Contracts_Tenants FOREIGN KEY(tenant_id) REFERENCES dbo.Tenants(tenant_id),
        CONSTRAINT FK_Contracts_Rooms FOREIGN KEY(room_id) REFERENCES dbo.Rooms(room_id)
    );
END;
IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments (
        payment_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
        contract_id INT NOT NULL,
        billing_period CHAR(7) NOT NULL,
        bill_date DATE NOT NULL,
        due_date DATE NOT NULL,
        amount DECIMAL(12,2) NOT NULL,
        proof_path NVARCHAR(1000) NULL,
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_Payments_status DEFAULT 'Menunggu',
        notes NVARCHAR(1000) NULL,
        paid_at DATETIME2 NULL,
        created_at DATETIME2 NOT NULL CONSTRAINT DF_Payments_created_at DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_Payments_contract_period UNIQUE(contract_id, billing_period),
        CONSTRAINT CK_Payments_amount CHECK(amount > 0),
        CONSTRAINT CK_Payments_status CHECK(status IN ('Menunggu','Lunas','Ditolak')),
        CONSTRAINT FK_Payments_Contracts FOREIGN KEY(contract_id) REFERENCES dbo.Contracts(contract_id)
    );
END;
IF OBJECT_ID(N'dbo.Complaints', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Complaints (
        complaint_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Complaints PRIMARY KEY,
        tenant_id INT NOT NULL,
        title NVARCHAR(150) NOT NULL,
        description NVARCHAR(MAX) NOT NULL,
        reported_at DATETIME2 NOT NULL CONSTRAINT DF_Complaints_reported_at DEFAULT SYSDATETIME(),
        status NVARCHAR(20) NOT NULL CONSTRAINT DF_Complaints_status DEFAULT 'Diajukan',
        admin_response NVARCHAR(MAX) NULL,
        CONSTRAINT CK_Complaints_status CHECK(status IN ('Diajukan','Diproses','Selesai')),
        CONSTRAINT FK_Complaints_Tenants FOREIGN KEY(tenant_id) REFERENCES dbo.Tenants(tenant_id)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Contracts_RoomStatus' AND object_id=OBJECT_ID(N'dbo.Contracts'))
    CREATE INDEX IX_Contracts_RoomStatus ON dbo.Contracts(room_id,status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Payments_StatusPeriod' AND object_id=OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX IX_Payments_StatusPeriod ON dbo.Payments(status,billing_period);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Complaints_Status' AND object_id=OBJECT_ID(N'dbo.Complaints'))
    CREATE INDEX IX_Complaints_Status ON dbo.Complaints(status);
";
            command.ExecuteNonQuery();
        }

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM dbo.Users;";
        var userCount = Convert.ToInt64(countCommand.ExecuteScalar() ?? 0);
        if (userCount == 0) SeedDemoData(connection);
    }


    private static void EnsureDatabaseExists()
    {
        var target = new SqlConnectionStringBuilder(DatabaseConfig.ConnectionString);
        var databaseName = target.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Nama database belum diisi pada ConnectionStrings:KosKu di appsettings.json.");

        // If the configured database already exists and is accessible, don't require
        // CREATE DATABASE permission or extra permissions in master.
        using (var targetConnection = new SqlConnection(target.ConnectionString))
        {
            try
            {
                targetConnection.Open();
                return;
            }
            catch (SqlException ex) when (ex.Number is 4060 or 911)
            {
                // The configured database is missing; attempt creation below.
            }
        }

        target.InitialCatalog = "master";
        using var connection = new SqlConnection(target.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
IF DB_ID(@databaseName) IS NULL
BEGIN
    DECLARE @createSql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@databaseName);
    EXEC(@createSql);
END;";
        command.Parameters.AddWithValue("@databaseName", databaseName);
        command.ExecuteNonQuery();
    }

    private static void SeedDemoData(SqlConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        long tenantUserId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Users(username,email,password_hash,role,full_name)
VALUES(@username,@email,@hash,'penghuni','Nadia Putri'); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            command.Parameters.AddWithValue("@username", "mahasiswa");
            command.Parameters.AddWithValue("@email", "mahasiswa@kosku.local");
            command.Parameters.AddWithValue("@hash", PasswordHasher.HashPassword("mahasiswa123"));
            tenantUserId = Convert.ToInt64(command.ExecuteScalar());
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Users(username,email,password_hash,role,full_name)
VALUES(@username,@email,@hash,'admin','Administrator Kos');";
            command.Parameters.AddWithValue("@username", "admin");
            command.Parameters.AddWithValue("@email", "admin@kosku.local");
            command.Parameters.AddWithValue("@hash", PasswordHasher.HashPassword("admin123"));
            command.ExecuteNonQuery();
        }

        long tenantId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Tenants(user_id,full_name,student_id,university,phone,emergency_contact,address)
VALUES(@uid,'Nadia Putri','20261001','Universitas Nusantara','081234567890','081298765432','Surabaya'); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            command.Parameters.AddWithValue("@uid", tenantUserId);
            tenantId = Convert.ToInt64(command.ExecuteScalar());
        }

        var rooms = new (string Number, string Type, decimal Price, string Status, string Facilities, string Description)[]
        {
            ("A-01", "Standard", 1250000m, "Terisi", "Kasur, lemari, meja belajar, Wi-Fi", "Kamar dekat area belajar bersama"),
            ("A-02", "Standard", 1200000m, "Tersedia", "Kasur, lemari, meja belajar", "Menghadap taman dalam"),
            ("A-03", "Deluxe", 1650000m, "Tersedia", "AC, kamar mandi dalam, Wi-Fi", "Kamar dengan kamar mandi dalam"),
            ("B-01", "Standard", 1150000m, "Tersedia", "Kasur, lemari, meja belajar", "Lantai dua"),
            ("B-02", "Deluxe", 1750000m, "Perbaikan", "AC, kamar mandi dalam, Wi-Fi", "Sedang perawatan instalasi air"),
            ("B-03", "VIP", 2200000m, "Tersedia", "AC, kamar mandi dalam, meja belajar, Wi-Fi", "Kamar luas dengan jendela besar")
        };
        foreach (var room in rooms)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Rooms(room_number,room_type,monthly_price,status,facilities,description)
VALUES(@number,@type,@price,@status,@facilities,@description);";
            command.Parameters.AddWithValue("@number", room.Number);
            command.Parameters.AddWithValue("@type", room.Type);
            command.Parameters.AddWithValue("@price", room.Price);
            command.Parameters.AddWithValue("@status", room.Status);
            command.Parameters.AddWithValue("@facilities", room.Facilities);
            command.Parameters.AddWithValue("@description", room.Description);
            command.ExecuteNonQuery();
        }

        long roomId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT room_id FROM dbo.Rooms WHERE room_number='A-01';";
            roomId = Convert.ToInt64(command.ExecuteScalar());
        }
        var today = DateTime.Today;
        var start = today.AddMonths(-2).Date;
        var end = today.AddMonths(10).Date;
        long contractId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Contracts(tenant_id,room_id,start_date,end_date,monthly_price,status)
VALUES(@tenant,@room,@start,@end,1250000,'Aktif'); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            command.Parameters.AddWithValue("@tenant", tenantId);
            command.Parameters.AddWithValue("@room", roomId);
            command.Parameters.AddWithValue("@start", start);
            command.Parameters.AddWithValue("@end", end);
            contractId = Convert.ToInt64(command.ExecuteScalar());
        }

        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var previousMonth = currentMonth.AddMonths(-1);
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Payments(contract_id,billing_period,bill_date,due_date,amount,status,notes,paid_at)
VALUES(@contract,@previous,@previousDate,@previousDue,1250000,'Lunas','Pembayaran demo',@paidAt),
      (@contract,@current,@currentDate,@currentDue,1250000,'Menunggu',NULL,NULL);";
            command.Parameters.AddWithValue("@contract", contractId);
            command.Parameters.AddWithValue("@previous", previousMonth.ToString("yyyy-MM"));
            command.Parameters.AddWithValue("@previousDate", previousMonth);
            command.Parameters.AddWithValue("@previousDue", previousMonth.AddDays(9));
            command.Parameters.AddWithValue("@paidAt", previousMonth.AddDays(5));
            command.Parameters.AddWithValue("@current", currentMonth.ToString("yyyy-MM"));
            command.Parameters.AddWithValue("@currentDate", currentMonth);
            command.Parameters.AddWithValue("@currentDue", currentMonth.AddDays(9));
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO dbo.Complaints(tenant_id,title,description,status)
VALUES(@tenant,N'AC kurang dingin',N'AC membutuhkan pemeriksaan karena suhu kamar tidak lagi dingin.',N'Diajukan');";
            command.Parameters.AddWithValue("@tenant", tenantId);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }
}
