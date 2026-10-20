using KosKu.Data;
using KosKu.Services;

try
{
    DatabaseInitializer.Initialize();
    var repository = new KosRepository();
    var rooms = repository.GetRooms();
    var tenants = repository.GetTenants();
    var payments = repository.Query("SELECT payment_id FROM Payments;");
    var complaints = repository.Query("SELECT complaint_id FROM Complaints;");

    if (rooms.Rows.Count == 0) throw new InvalidOperationException("Tidak ada data kamar.");
    if (tenants.Rows.Count == 0) throw new InvalidOperationException("Tidak ada data penghuni.");
    if (payments.Rows.Count == 0) throw new InvalidOperationException("Tidak ada data pembayaran.");
    if (complaints.Rows.Count == 0) throw new InvalidOperationException("Tidak ada data komplain.");

    var auth = new AuthService();
    var admin = auth.Login("admin", "admin123");
    if (admin is null || !admin.IsAdmin) throw new InvalidOperationException("Login demo admin gagal. Jalankan pengujian pada database demo yang baru.");

    // Regression test for the crash reported after login: BuildDashboard calls these queries.
    var recentAdminPayments = repository.GetRecentPayments(admin, 5);
    var adminComplaints = repository.GetComplaints(admin);
    if (recentAdminPayments.Columns.Count == 0) throw new InvalidOperationException("Kolom tagihan admin tidak terbentuk.");
    if (adminComplaints.Columns.Count == 0) throw new InvalidOperationException("Kolom komplain admin tidak terbentuk.");

    var tenant = auth.Login("mahasiswa", "mahasiswa123");
    if (tenant is null || tenant.IsAdmin || tenant.TenantId is null)
        throw new InvalidOperationException("Login demo mahasiswa gagal. Jalankan pengujian pada database demo yang baru.");
    var recentTenantPayments = repository.GetRecentPayments(tenant, 5);
    if (recentTenantPayments.Columns.Count == 0) throw new InvalidOperationException("Kolom tagihan penghuni tidak terbentuk.");

    Console.WriteLine("SMOKE TEST BERHASIL");
    Console.WriteLine($"Kamar: {rooms.Rows.Count}; Penghuni: {tenants.Rows.Count}; Tagihan: {payments.Rows.Count}; Komplain: {complaints.Rows.Count}");
    Console.WriteLine($"Tagihan terbaru admin: {recentAdminPayments.Rows.Count}; tagihan terbaru penghuni: {recentTenantPayments.Rows.Count}");
}
catch (Exception ex)
{
    Console.Error.WriteLine("SMOKE TEST GAGAL: " + ex.Message);
    Environment.ExitCode = 1;
}
