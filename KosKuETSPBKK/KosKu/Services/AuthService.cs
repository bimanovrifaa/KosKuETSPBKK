using System.Net.Mail;
using System.Text.RegularExpressions;
using KosKu.Data;
using KosKu.Models;

namespace KosKu.Services;

public sealed class AuthService
{
    private readonly KosRepository _repository = new();

    public void RegisterTenant(string fullName, string studentId, string university, string phone,
        string emergencyContact, string email, string username, string password, string confirmPassword, string address)
    {
        fullName = (fullName ?? string.Empty).Trim();
        studentId = (studentId ?? string.Empty).Trim();
        university = (university ?? string.Empty).Trim();
        phone = (phone ?? string.Empty).Trim();
        emergencyContact = (emergencyContact ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();
        username = (username ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(studentId) ||
            string.IsNullOrWhiteSpace(university) || string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(emergencyContact) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Semua kolom bertanda * wajib diisi.");

        if (fullName.Length > 100) throw new InvalidOperationException("Nama lengkap maksimal 100 karakter.");
        if (studentId.Length > 50) throw new InvalidOperationException("NIM/NPM maksimal 50 karakter.");
        if (university.Length > 150) throw new InvalidOperationException("Nama universitas maksimal 150 karakter.");
        if (phone.Length > 30 || emergencyContact.Length > 30)
            throw new InvalidOperationException("Nomor telepon maksimal 30 karakter.");
        if (username.Length < 3 || username.Length > 30 || !Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$"))
            throw new InvalidOperationException("Username harus 3–30 karakter dan hanya boleh berisi huruf, angka, titik, garis bawah, atau tanda hubung.");
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Format email tidak valid.");
        if (password.Length < 8)
            throw new InvalidOperationException("Password minimal 8 karakter.");
        if (password.Length > 256)
            throw new InvalidOperationException("Password terlalu panjang.");
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            throw new InvalidOperationException("Konfirmasi password tidak sama.");

        _repository.RegisterTenantAccount(fullName, studentId, university, phone, emergencyContact,
            parsedEmail!.Address, username, password, address ?? string.Empty);
    }

    public User? Login(string username, string password)
    {
        var table = _repository.Query(@"SELECT TOP (1) u.user_id,u.username,u.full_name,u.role,u.password_hash,t.tenant_id
FROM Users u LEFT JOIN Tenants t ON t.user_id=u.user_id
WHERE u.username=@username;", ("@username", username.Trim()));
        if (table.Rows.Count == 0) return null;
        var row = table.Rows[0];
        var passwordHash = Convert.ToString(row["password_hash"]) ?? string.Empty;
        if (!PasswordHasher.VerifyPassword(password, passwordHash)) return null;
        int? tenantId = row["tenant_id"] is DBNull ? null : Convert.ToInt32(row["tenant_id"]);
        return new User
        {
            UserId = Convert.ToInt32(row["user_id"]),
            Username = Convert.ToString(row["username"]) ?? string.Empty,
            FullName = Convert.ToString(row["full_name"]) ?? "Pengguna",
            Role = Convert.ToString(row["role"]) ?? "penghuni",
            TenantId = tenantId
        };
    }
}
