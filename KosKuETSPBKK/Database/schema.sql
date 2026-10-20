/* SQL Server schema KosKu. Jalankan setelah database KosKu dibuat. */
USE KosKu;
GO
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
GO
