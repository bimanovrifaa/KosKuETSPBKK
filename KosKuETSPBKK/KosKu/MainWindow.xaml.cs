using System.Data;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using KosKu.Data;
using KosKu.Models;
using KosKu.Services;
using KosKu.Views;
using KosKu.Views.Dialogs;
using Microsoft.Win32;

namespace KosKu;

public partial class MainWindow : Window
{
    private readonly User _user;
    private readonly KosRepository _repository = new();
    private DataGrid? _activeGrid;
    private string _activePage = "Dashboard";

    public MainWindow(User user, string initialPage = "Dashboard")
    {
        InitializeComponent();
        _user = user;
        ThemeButton.Content = ThemeService.ToggleLabel;
        UserNameText.Text = user.FullName;
        UserRoleText.Text = user.IsAdmin ? "Administrator" : "Penghuni Kos";
        TodayText.Text = DateTime.Today.ToString("dd MMM yyyy", new CultureInfo("id-ID"));

        if (!user.IsAdmin)
        {
            BtnRooms.Visibility = Visibility.Collapsed;
            BtnTenants.Visibility = Visibility.Collapsed;
            BtnContracts.Visibility = Visibility.Collapsed;
            BtnReports.Visibility = Visibility.Collapsed;
        }
        ShowPage(initialPage);
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        var pageToRestore = _activePage;
        ThemeService.Toggle();
        var themedWindow = new MainWindow(_user, pageToRestore);
        Application.Current.MainWindow = themedWindow;
        themedWindow.Show();
        Close();
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => ShowPage("Dashboard");
    private void NavRooms_Click(object sender, RoutedEventArgs e) => ShowPage("Rooms");
    private void NavTenants_Click(object sender, RoutedEventArgs e) => ShowPage("Tenants");
    private void NavContracts_Click(object sender, RoutedEventArgs e) => ShowPage("Contracts");
    private void NavPayments_Click(object sender, RoutedEventArgs e) => ShowPage("Payments");
    private void NavComplaints_Click(object sender, RoutedEventArgs e) => ShowPage("Complaints");
    private void NavReports_Click(object sender, RoutedEventArgs e) => ShowPage("Reports");

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Apakah kamu yakin ingin keluar dari KosKu?", "Konfirmasi Keluar",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }

    private void ShowPage(string page)
    {
        _activePage = page;
        HeaderSubtitle.Text = page switch
        {
            "Dashboard" => "Ringkasan operasional dan informasi terbaru",
            "Rooms" => "Inventaris, tarif, dan ketersediaan kamar",
            "Tenants" => "Data mahasiswa yang tinggal di kos",
            "Contracts" => "Periode sewa dan status kontrak penghuni",
            "Payments" => "Tagihan bulanan, bukti bayar, dan verifikasi",
            "Complaints" => "Pelaporan dan tindak lanjut fasilitas kos",
            "Reports" => "Ringkasan indikator dan ekspor data operasional",
            _ => "Kelola operasional kos dari satu tempat"
        };

        ContentHost.Content = page switch
        {
            "Dashboard" => BuildDashboard(),
            "Rooms" when _user.IsAdmin => BuildRoomsPage(),
            "Tenants" when _user.IsAdmin => BuildTenantsPage(),
            "Contracts" when _user.IsAdmin => BuildContractsPage(),
            "Payments" => BuildPaymentsPage(),
            "Complaints" => BuildComplaintsPage(),
            "Reports" when _user.IsAdmin => BuildReportsPage(),
            _ => BuildDashboard()
        };
    }

    private UIElement BuildRoomsPage()
    {
        return BuildTablePage("Manajemen Kamar", "Tambah, ubah, cari, dan pantau status setiap kamar.",
            _repository.GetRooms,
            new PageAction("+ Tambah Kamar", AddRoom, "BtnPrimary"),
            new PageAction("Edit", EditRoom, "BtnOutline"),
            new PageAction("Hapus", DeleteRoom, "BtnDanger"),
            new PageAction("Muat Ulang", () => { }, "BtnOutline"));
    }

    private UIElement BuildTenantsPage()
    {
        return BuildTablePage("Data Penghuni", "Akun penghuni dibuat bersamaan dengan profil mahasiswa.",
            _repository.GetTenants,
            new PageAction("+ Tambah Penghuni", AddTenant, "BtnPrimary"),
            new PageAction("Edit", EditTenant, "BtnOutline"),
            new PageAction("Hapus", DeleteTenant, "BtnDanger"),
            new PageAction("Muat Ulang", () => { }, "BtnOutline"));
    }

    private UIElement BuildContractsPage()
    {
        return BuildTablePage("Kontrak Sewa", "Kamar yang memiliki kontrak aktif tidak dapat disewakan ke kontrak lain pada periode yang sama.",
            _repository.GetContracts,
            new PageAction("+ Kontrak Baru", AddContract, "BtnPrimary"),
            new PageAction("Ubah Status", ChangeContractStatus, "BtnOutline"),
            new PageAction("Muat Ulang", () => { }, "BtnOutline"));
    }

    private UIElement BuildPaymentsPage()
    {
        var actions = new List<PageAction>();
        if (_user.IsAdmin)
        {
            actions.Add(new PageAction("+ Generate Tagihan Bulan Ini", GenerateBills, "BtnPrimary"));
            actions.Add(new PageAction("Buka Bukti", OpenPaymentProof, "BtnOutline"));
            actions.Add(new PageAction("Verifikasi Lunas", VerifyPayment, "BtnSuccess"));
            actions.Add(new PageAction("Tolak Pembayaran", RejectPayment, "BtnDanger"));
        }
        else
        {
            actions.Add(new PageAction("Unggah Bukti Bayar", UploadPaymentProof, "BtnPrimary"));
        }
        actions.Add(new PageAction("Muat Ulang", () => { }, "BtnOutline"));
        return BuildTablePage("Pembayaran dan Tagihan", _user.IsAdmin
            ? "Buat tagihan periode bulanan, periksa bukti, lalu validasi status pembayaran."
            : "Lihat tagihan sewa dan unggah bukti transfer untuk diverifikasi pengelola.",
            () => _repository.GetPayments(_user), actions.ToArray());
    }

    private UIElement BuildComplaintsPage()
    {
        var actions = new List<PageAction>();
        if (_user.IsAdmin)
        {
            actions.Add(new PageAction("Tandai Diproses", () => ChangeComplaintStatus("Diproses"), "BtnPrimary"));
            actions.Add(new PageAction("Tanggapi", RespondToComplaint, "BtnOutline"));
            actions.Add(new PageAction("Selesaikan", () => ChangeComplaintStatus("Selesai"), "BtnSuccess"));
        }
        else
        {
            actions.Add(new PageAction("+ Buat Komplain", AddComplaint, "BtnPrimary"));
        }
        actions.Add(new PageAction("Muat Ulang", () => { }, "BtnOutline"));
        return BuildTablePage("Komplain Fasilitas", _user.IsAdmin
            ? "Tinjau keluhan penghuni dan perbarui status penanganannya."
            : "Laporkan masalah fasilitas dan pantau status tindak lanjut dari pengelola.",
            () => _repository.GetComplaints(_user), actions.ToArray());
    }

    private UIElement BuildReportsPage()
    {
        return BuildTablePage("Laporan dan Ekspor", "Angka ringkasan dihitung dari database lokal. Ekspor tersedia dalam format CSV yang dapat dibuka di Excel.",
            _repository.GetReportSummary,
            new PageAction("Ekspor Kamar", () => ExportCsv("rooms", "data_kamar"), "BtnOutline"),
            new PageAction("Ekspor Penghuni", () => ExportCsv("tenants", "data_penghuni"), "BtnOutline"),
            new PageAction("Ekspor Kontrak", () => ExportCsv("contracts", "data_kontrak"), "BtnOutline"),
            new PageAction("Ekspor Pembayaran", () => ExportCsv("payments", "data_pembayaran"), "BtnOutline"),
            new PageAction("Ekspor Komplain", () => ExportCsv("complaints", "data_komplain"), "BtnOutline"));
    }

    private UIElement BuildTablePage(string title, string subtitle, Func<DataTable> loader, params PageAction[] actions)
    {
        var table = loader();
        var root = new Grid { Margin = new Thickness(28, 24, 28, 26) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
        heading.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 25,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        });
        heading.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 12,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        });
        root.Children.Add(heading);

        var toolbar = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(255) });
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var search = new TextBox
        {
            Style = (Style)FindResource("ModernTextBox"),
            ToolTip = "Cari pada tabel",
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        search.TextChanged += (_, _) => ApplySearch(table, search.Text);
        toolbar.Children.Add(search);

        var actionPanel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        foreach (var action in actions)
        {
            var button = new Button
            {
                Content = action.Label,
                Style = (Style)FindResource(action.StyleKey),
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(11, 8, 11, 8),
                MinHeight = 36
            };
            button.Click += (_, _) =>
            {
                try
                {
                    action.Execute();
                    if (IsVisible) ShowPage(_activePage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Operasi tidak berhasil", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            actionPanel.Children.Add(button);
        }
        Grid.SetColumn(actionPanel, 1);
        toolbar.Children.Add(actionPanel);
        Grid.SetRow(toolbar, 1);
        root.Children.Add(toolbar);

        var grid = CreateDataGrid(table);
        _activeGrid = grid;
        var tableCard = new Border
        {
            Style = (Style)FindResource("CardBorderStyle"),
            Padding = new Thickness(0),
            Child = grid
        };
        Grid.SetRow(tableCard, 2);
        root.Children.Add(tableCard);
        return root;
    }

    private DataGrid CreateDataGrid(DataTable table, double? height = null)
    {
        var grid = new DataGrid
        {
            Style = (Style)FindResource("ModernDataGrid"),
            AutoGenerateColumns = true,
            Margin = new Thickness(0),
            MinHeight = height ?? 300,
            MaxHeight = height ?? double.PositiveInfinity
        };
        grid.AutoGeneratingColumn += (_, e) =>
        {
            if (e.PropertyName.StartsWith("_", StringComparison.Ordinal))
                e.Column.Visibility = Visibility.Collapsed;
            if (e.Column is DataGridTextColumn textColumn &&
                (e.PropertyName.Contains("Harga", StringComparison.OrdinalIgnoreCase) ||
                 e.PropertyName.Contains("Nominal", StringComparison.OrdinalIgnoreCase) ||
                 e.PropertyName.Contains("Sewa per Bulan", StringComparison.OrdinalIgnoreCase)))
            {
                textColumn.Binding = new Binding(e.PropertyName) { StringFormat = "N0" };
            }
            e.Column.Width = DataGridLength.Auto;
        };
        grid.ItemsSource = table.DefaultView;
        return grid;
    }

    private static void ApplySearch(DataTable table, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            table.DefaultView.RowFilter = string.Empty;
            return;
        }
        var columns = table.Columns.Cast<DataColumn>()
            .Where(column => column.DataType == typeof(string) && !column.ColumnName.StartsWith("_", StringComparison.Ordinal))
            .ToList();
        if (columns.Count == 0) return;
        var safe = searchText.Replace("'", "''");
        table.DefaultView.RowFilter = string.Join(" OR ", columns.Select(column => $"[{column.ColumnName.Replace("]", "]]", StringComparison.Ordinal)}] LIKE '%{safe}%'"));
    }

    private UIElement BuildDashboard()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var content = new StackPanel { Margin = new Thickness(25, 23, 25, 28) };
        var welcome = new StackPanel { Margin = new Thickness(4, 0, 4, 20) };
        welcome.Children.Add(new TextBlock
        {
            Text = $"Selamat datang, {_user.FullName.Split(' ')[0]}!",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        });
        welcome.Children.Add(new TextBlock
        {
            Text = _user.IsAdmin
                ? "Berikut ringkasan operasional kos dan hal yang perlu ditindaklanjuti hari ini."
                : "Periksa tagihan sewa, informasi kontrak, dan status laporan fasilitasmu.",
            FontSize = 12,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        content.Children.Add(welcome);

        var tiles = new List<(string Label, string Value, string Detail, string Icon, string Accent)>();
        (string Label, string Value, string Detail, string Icon, string Accent) hero = default;

        if (_user.IsAdmin)
        {
            var month = DateTime.Today.ToString("yyyy-MM");
            tiles.Add(("Total Kamar", _repository.ScalarInt("SELECT COUNT(*) FROM Rooms;").ToString("N0"), "Semua unit terdaftar", "⌂", "#2563EB"));
            tiles.Add(("Kamar Tersedia", _repository.ScalarInt("SELECT COUNT(*) FROM Rooms WHERE status='Tersedia';").ToString("N0"), "Siap disewakan", "✓", "#15803D"));
            tiles.Add(("Kamar Terisi", _repository.ScalarInt("SELECT COUNT(*) FROM Rooms WHERE status='Terisi';").ToString("N0"), "Sedang digunakan", "▣", "#7C3AED"));
            tiles.Add(("Tagihan Menunggu", _repository.ScalarInt("SELECT COUNT(*) FROM Payments WHERE status='Menunggu';").ToString("N0"), "Perlu verifikasi", "Rp", "#D97706"));
            var revenue = _repository.ScalarDecimal("SELECT COALESCE(SUM(amount),0) FROM Payments WHERE status='Lunas' AND billing_period=@month;", ("@month", month));
            hero = ("Pendapatan Bulan Ini", $"Rp {revenue:N0}", "Pembayaran tervalidasi", "↗", "#0F766E");
            tiles.Add(("Komplain Aktif", _repository.ScalarInt("SELECT COUNT(*) FROM Complaints WHERE status<>'Selesai';").ToString("N0"), "Belum diselesaikan", "◇", "#DC2626"));
        }
        else
        {
            int tenantId = _user.TenantId ?? 0;
            var monthlyRent = _repository.ScalarDecimal("SELECT COALESCE(MAX(monthly_price),0) FROM Contracts WHERE tenant_id=@tenant AND status='Aktif';", ("@tenant", tenantId));
            hero = ("Sewa per Bulan", $"Rp {monthlyRent:N0}", "Sesuai kontrak aktif", "Rp", "#2563EB");
            tiles.Add(("Tagihan Belum Lunas", _repository.ScalarInt("SELECT COUNT(*) FROM Payments p JOIN Contracts c ON c.contract_id=p.contract_id WHERE c.tenant_id=@tenant AND p.status<>'Lunas';", ("@tenant", tenantId)).ToString("N0"), "Periksa periode tagihan", "!", "#D97706"));
            tiles.Add(("Komplain Aktif", _repository.ScalarInt("SELECT COUNT(*) FROM Complaints WHERE tenant_id=@tenant AND status<>'Selesai';", ("@tenant", tenantId)).ToString("N0"), "Sedang ditangani", "◇", "#DC2626"));
            var contractEnd = Convert.ToString(_repository.Scalar("SELECT MAX(end_date) FROM Contracts WHERE tenant_id=@tenant AND status='Aktif';", ("@tenant", tenantId)));
            var daysLeft = DateTime.TryParse(contractEnd, out var parsedEnd) ? Math.Max(0, (parsedEnd.Date - DateTime.Today).Days) : 0;
            tiles.Add(("Sisa Masa Sewa", $"{daysLeft} hari", "Berdasarkan kontrak aktif", "◷", "#7C3AED"));
            tiles.Add(("Tagihan Bulan Ini", _repository.ScalarInt("SELECT COUNT(*) FROM Payments p JOIN Contracts c ON c.contract_id=p.contract_id WHERE c.tenant_id=@tenant AND p.billing_period=@period;", ("@tenant", tenantId), ("@period", DateTime.Today.ToString("yyyy-MM"))).ToString("N0"), "Tagihan periode berjalan", "▤", "#0F766E"));
            tiles.Add(("Total Laporan", _repository.ScalarInt("SELECT COUNT(*) FROM Complaints WHERE tenant_id=@tenant;", ("@tenant", tenantId)).ToString("N0"), "Riwayat komplainmu", "≡", "#64748B"));
        }
        content.Children.Add(BuildStatsLayout(hero, tiles));

        if (_user.IsAdmin)
            content.Children.Add(BuildOccupancyPanel());
        else
            content.Children.Add(BuildTenantContractPanel());

        var lower = new Grid { Margin = new Thickness(4, 16, 4, 0) };
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var recentPayments = _repository.GetRecentPayments(_user, 5);
        var recentComplaints = _repository.GetComplaints(_user);
        if (recentComplaints.Rows.Count > 5)
        {
            var limited = recentComplaints.Clone();
            for (int i = 0; i < 5; i++) limited.ImportRow(recentComplaints.Rows[i]);
            recentComplaints = limited;
        }
        var paymentSection = BuildSection("Tagihan Terbaru", recentPayments);
        var complaintSection = BuildSection("Komplain Terbaru", recentComplaints);
        paymentSection.Margin = new Thickness(0, 0, 8, 0);
        complaintSection.Margin = new Thickness(8, 0, 0, 0);
        lower.Children.Add(paymentSection);
        Grid.SetColumn(complaintSection, 1);
        lower.Children.Add(complaintSection);
        content.Children.Add(lower);

        scroll.Content = content;
        return scroll;
    }

    private UIElement BuildStatsLayout((string Label, string Value, string Detail, string Icon, string Accent) hero,
        List<(string Label, string Value, string Detail, string Icon, string Accent)> tiles)
    {
        // Left: one large gradient highlight card. Right: compact tiles (icon circle + value) in two columns.
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.9, GridUnitType.Star) });

        var heroCard = new Border { Style = (Style)FindResource("AccentCardStyle"), Margin = new Thickness(4, 4, 8, 4), Padding = new Thickness(24) };
        var heroLayers = new Grid();
        heroLayers.Children.Add(new TextBlock
        {
            Text = hero.Icon, FontSize = 150, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Opacity = 0.13,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, -10, -30)
        });
        var heroText = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
        heroText.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 5, 12, 5), HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock { Text = hero.Label, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White }
        });
        heroText.Children.Add(new TextBlock { Text = hero.Value, FontSize = 36, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 28, 0, 6), TextTrimming = TextTrimming.CharacterEllipsis });
        heroText.Children.Add(new TextBlock { Text = hero.Detail, FontSize = 12, Foreground = Brushes.White, Opacity = 0.85, TextWrapping = TextWrapping.Wrap });
        heroLayers.Children.Add(heroText);
        heroCard.Child = heroLayers;
        grid.Children.Add(heroCard);

        var tileGrid = new Grid();
        tileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        int rows = (tiles.Count + 1) / 2;
        for (int r = 0; r < rows; r++) tileGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            var tile = BuildStatTile(t.Label, t.Value, t.Detail, t.Icon, t.Accent);
            bool last = i == tiles.Count - 1 && tiles.Count % 2 == 1;
            Grid.SetRow(tile, i / 2);
            Grid.SetColumn(tile, last ? 0 : i % 2);
            if (last) Grid.SetColumnSpan(tile, 2);
            tileGrid.Children.Add(tile);
        }
        Grid.SetColumn(tileGrid, 1);
        grid.Children.Add(tileGrid);
        return grid;
    }

    private UIElement BuildStatTile(string label, string value, string detail, string icon, string accent)
    {
        var color = ParseColor(accent);
        var card = new Border { Style = (Style)FindResource("CardBorderStyle"), Margin = new Thickness(4), Padding = new Thickness(16, 12, 16, 12), CornerRadius = new CornerRadius(16), MinHeight = 76 };
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var iconCircle = new Border
        {
            Width = 40, Height = 40, CornerRadius = new CornerRadius(20), Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromArgb(46, color.R, color.G, color.B))
        };
        iconCircle.Child = new TextBlock { Text = icon, Foreground = new SolidColorBrush(color), FontWeight = FontWeights.Bold, FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(iconCircle);
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = (Brush)FindResource("BrushTextSecondary"), TextTrimming = TextTrimming.CharacterEllipsis });
        text.Children.Add(new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("BrushTextPrimary"), Margin = new Thickness(0, 1, 0, 1), TextTrimming = TextTrimming.CharacterEllipsis });
        text.Children.Add(new TextBlock { Text = detail, FontSize = 10, Foreground = (Brush)FindResource("BrushTextSecondary"), TextTrimming = TextTrimming.CharacterEllipsis });
        Grid.SetColumn(text, 1);
        row.Children.Add(text);
        card.Child = row;
        return card;
    }

    private UIElement BuildOccupancyPanel()
    {
        int total = _repository.ScalarInt("SELECT COUNT(*) FROM Rooms;");
        int occupied = _repository.ScalarInt("SELECT COUNT(*) FROM Rooms WHERE status='Terisi';");
        int repair = _repository.ScalarInt("SELECT COUNT(*) FROM Rooms WHERE status='Perbaikan';");
        int available = _repository.ScalarInt("SELECT COUNT(*) FROM Rooms WHERE status='Tersedia';");
        double percentage = total == 0 ? 0 : occupied * 100.0 / total;
        var border = new Border { Style = (Style)FindResource("CardBorderStyle"), Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(22) };
        var stack = new StackPanel();

        var head = new Grid();
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var headText = new StackPanel();
        headText.Children.Add(new TextBlock { Text = "Ketersediaan Kamar", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("BrushTextPrimary") });
        headText.Children.Add(new TextBlock { Text = $"{occupied} dari {total} kamar sedang terisi", FontSize = 11, Foreground = (Brush)FindResource("BrushTextSecondary"), Margin = new Thickness(0, 4, 0, 0) });
        head.Children.Add(headText);
        var pct = new TextBlock { Text = $"{percentage:N0}%", FontSize = 28, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("BrushPrimary"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(pct, 1);
        head.Children.Add(pct);
        stack.Children.Add(head);

        // Segmented bar: widths follow the real room counts.
        var bar = new Grid { Height = 12, Margin = new Thickness(0, 18, 0, 18) };
        var segments = new[] { (occupied, "#3CC8D8"), (available, "#34D399"), (repair, "#FBBF24") };
        int col = 0;
        foreach (var (count, color) in segments)
        {
            if (count <= 0) continue;
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(count, GridUnitType.Star) });
            var seg = new Border { Background = new SolidColorBrush(ParseColor(color)), CornerRadius = new CornerRadius(6), Margin = new Thickness(col == 0 ? 0 : 3, 0, 0, 0) };
            Grid.SetColumn(seg, col++);
            bar.Children.Add(seg);
        }
        if (col == 0) bar.Children.Add(new Border { Background = (Brush)FindResource("BrushProgressBg"), CornerRadius = new CornerRadius(6) });
        stack.Children.Add(bar);

        var legend = new UniformGrid { Columns = 3 };
        legend.Children.Add(LegendItem("Terisi", occupied, "#3CC8D8"));
        legend.Children.Add(LegendItem("Tersedia", available, "#34D399"));
        legend.Children.Add(LegendItem("Perbaikan", repair, "#FBBF24"));
        stack.Children.Add(legend);
        border.Child = stack;
        return border;
    }

    private UIElement LegendItem(string label, int value, string color)
    {
        var chip = new Border { Background = (Brush)FindResource("BrushSubtleBg"), CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 8, 0) };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(new Border { Width = 10, Height = 10, CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(ParseColor(color)), Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center });
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = (Brush)FindResource("BrushTextSecondary") });
        text.Children.Add(new TextBlock { Text = value.ToString("N0"), FontSize = 16, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("BrushTextPrimary") });
        row.Children.Add(text);
        chip.Child = row;
        return chip;
    }

    private UIElement BuildTenantContractPanel()
    {
        var table = _repository.GetActiveContractForTenant(_user.TenantId ?? 0);
        var border = new Border { Style = (Style)FindResource("CardBorderStyle"), Margin = new Thickness(4, 0, 4, 0), Padding = new Thickness(22) };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = "Informasi Kontrak Aktif", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("BrushTextPrimary"), Margin = new Thickness(0, 0, 0, 12) });
        if (table.Rows.Count == 0)
        {
            stack.Children.Add(new TextBlock { Text = "Belum ada kontrak aktif. Silakan hubungi pengelola kos.", FontSize = 12, Foreground = (Brush)FindResource("BrushTextSecondary") });
        }
        else
        {
            var row = table.Rows[0];
            var detail = new Grid();
            for (int i = 0; i < 4; i++) detail.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            AddDetail(detail, 0, 0, "Kamar", Convert.ToString(row["Kamar"]) ?? "-");
            AddDetail(detail, 0, 1, "Tipe", Convert.ToString(row["Tipe Kamar"]) ?? "-");
            AddDetail(detail, 0, 2, "Tanggal Mulai", Convert.ToString(row["Tanggal Mulai"]) ?? "-");
            AddDetail(detail, 0, 3, "Tanggal Selesai", Convert.ToString(row["Tanggal Selesai"]) ?? "-");
            stack.Children.Add(detail);
        }
        border.Child = stack;
        return border;
    }

    private void AddDetail(Grid grid, int row, int column, string label, string value)
    {
        var chip = new Border { Background = (Brush)FindResource("BrushSubtleBg"), CornerRadius = new CornerRadius(14), Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, column == 3 ? 0 : 8, 0) };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = (Brush)FindResource("BrushTextSecondary") });
        stack.Children.Add(new TextBlock { Text = value, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("BrushTextPrimary"), Margin = new Thickness(0, 4, 0, 0) });
        chip.Child = stack;
        Grid.SetRow(chip, row);
        Grid.SetColumn(chip, column);
        grid.Children.Add(chip);
    }

    private Border BuildSection(string title, DataTable table)
    {
        var border = new Border { Style = (Style)FindResource("CardBorderStyle"), Padding = new Thickness(15) };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("BrushTextPrimary"), Margin = new Thickness(0, 0, 0, 12) });
        var grid = CreateDataGrid(table, 215);
        grid.MinHeight = 215;
        grid.MaxHeight = 215;
        stack.Children.Add(grid);
        border.Child = stack;
        return border;
    }

    private void AddRoom()
    {
        var dialog = new EntityDialog("Tambah Kamar", new[]
        {
            Field("number", "Nomor Kamar"),
            ComboField("type", "Tipe Kamar", "Standard", "Deluxe", "VIP"),
            Field("price", "Harga Bulanan (angka, tanpa titik)", FormFieldType.Number, defaultValue: "1250000"),
            ComboField("status", "Status", "Tersedia", "Perbaikan"),
            Field("facilities", "Fasilitas", FormFieldType.Multiline, required: false),
            Field("description", "Keterangan", FormFieldType.Multiline, required: false)
        }) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var v = dialog.Values;
        _repository.CreateRoom(v["number"], v["type"], decimal.Parse(v["price"], CultureInfo.InvariantCulture), v["status"], v.GetValueOrDefault("facilities", ""), v.GetValueOrDefault("description", ""));
        MessageBox.Show("Data kamar berhasil ditambahkan.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void EditRoom()
    {
        var row = SelectedRow();
        if (row is null) return;
        var initial = new Dictionary<string, string>
        {
            ["number"] = Convert.ToString(row["No. Kamar"]) ?? "",
            ["type"] = Convert.ToString(row["Tipe Kamar"]) ?? "Standard",
            ["price"] = Convert.ToDecimal(row["Harga Bulanan"]).ToString(CultureInfo.InvariantCulture),
            ["status"] = Convert.ToString(row["Status"]) ?? "Tersedia",
            ["facilities"] = Convert.ToString(row["Fasilitas"]) ?? "",
            ["description"] = Convert.ToString(row["Keterangan"]) ?? ""
        };
        var dialog = new EntityDialog("Edit Kamar", new[]
        {
            Field("number", "Nomor Kamar"), ComboField("type", "Tipe Kamar", "Standard", "Deluxe", "VIP"),
            Field("price", "Harga Bulanan (angka, tanpa titik)", FormFieldType.Number), ComboField("status", "Status", "Tersedia", "Terisi", "Perbaikan"),
            Field("facilities", "Fasilitas", FormFieldType.Multiline, required: false), Field("description", "Keterangan", FormFieldType.Multiline, required: false)
        }, initial) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var v = dialog.Values;
        _repository.UpdateRoom(Convert.ToInt32(row["_id"]), v["number"], v["type"], decimal.Parse(v["price"], CultureInfo.InvariantCulture), v["status"], v.GetValueOrDefault("facilities", ""), v.GetValueOrDefault("description", ""));
        MessageBox.Show("Data kamar berhasil diperbarui.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteRoom()
    {
        var row = SelectedRow();
        if (row is null) return;
        var number = Convert.ToString(row["No. Kamar"]);
        if (MessageBox.Show($"Hapus kamar {number}?", "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _repository.DeleteRoom(Convert.ToInt32(row["_id"]));
        MessageBox.Show("Data kamar berhasil dihapus.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddTenant()
    {
        var dialog = new EntityDialog("Tambah Penghuni", new[]
        {
            Field("fullName", "Nama Lengkap"), Field("studentId", "NIM"), Field("university", "Nama Kampus"),
            Field("phone", "Nomor Telepon"), Field("emergencyContact", "Kontak Darurat"),
            Field("username", "Username Login"), Field("password", "Password Awal (minimal 6 karakter)", FormFieldType.Password)
        }) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var v = dialog.Values;
        _repository.CreateTenant(v["fullName"], v["studentId"], v["university"], v["phone"], v["emergencyContact"], v["username"], v["password"]);
        MessageBox.Show($"Penghuni berhasil ditambahkan.\nUsername: {v["username"]}\nSampaikan password awal secara pribadi kepada penghuni.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void EditTenant()
    {
        var row = SelectedRow();
        if (row is null) return;
        var initial = new Dictionary<string, string>
        {
            ["fullName"] = Convert.ToString(row["Nama Lengkap"]) ?? "",
            ["studentId"] = Convert.ToString(row["NIM"]) ?? "",
            ["university"] = Convert.ToString(row["Kampus"]) ?? "",
            ["phone"] = Convert.ToString(row["Telepon"]) ?? "",
            ["emergencyContact"] = Convert.ToString(row["Kontak Darurat"]) ?? ""
        };
        var dialog = new EntityDialog("Edit Data Penghuni", new[]
        {
            Field("fullName", "Nama Lengkap"), Field("studentId", "NIM"), Field("university", "Nama Kampus"),
            Field("phone", "Nomor Telepon"), Field("emergencyContact", "Kontak Darurat")
        }, initial) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var v = dialog.Values;
        _repository.UpdateTenant(Convert.ToInt32(row["_id"]), v["fullName"], v["studentId"], v["university"], v["phone"], v["emergencyContact"]);
        MessageBox.Show("Data penghuni berhasil diperbarui.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteTenant()
    {
        var row = SelectedRow();
        if (row is null) return;
        var name = Convert.ToString(row["Nama Lengkap"]);
        if (MessageBox.Show($"Hapus akun dan profil penghuni {name}? Data dengan riwayat kontrak/komplain tidak dapat dihapus.", "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _repository.DeleteTenant(Convert.ToInt32(row["_id"]));
        MessageBox.Show("Data penghuni berhasil dihapus.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddContract()
    {
        var tenants = _repository.GetTenantChoices();
        var rooms = _repository.GetAvailableRoomChoices();
        if (tenants.Rows.Count == 0) throw new InvalidOperationException("Tambahkan data penghuni terlebih dahulu.");
        if (rooms.Rows.Count == 0) throw new InvalidOperationException("Tidak ada kamar tersedia. Periksa status kamar terlebih dahulu.");
        var tenantOptions = tenants.Rows.Cast<DataRow>().Select(r => new FormOption { Value = Convert.ToString(r["Value"]) ?? "", Label = Convert.ToString(r["Label"]) ?? "" }).ToList();
        var roomOptions = rooms.Rows.Cast<DataRow>().Select(r => new FormOption { Value = Convert.ToString(r["Value"]) ?? "", Label = Convert.ToString(r["Label"]) ?? "" }).ToList();
        var firstPrice = Convert.ToDecimal(rooms.Rows[0]["Price"]).ToString(CultureInfo.InvariantCulture);
        var dialog = new EntityDialog("Buat Kontrak Sewa", new[]
        {
            new FormField { Key = "tenantId", Label = "Penghuni", Type = FormFieldType.Combo, Options = tenantOptions },
            new FormField { Key = "roomId", Label = "Kamar Tersedia", Type = FormFieldType.Combo, Options = roomOptions },
            new FormField { Key = "startDate", Label = "Tanggal Mulai", Type = FormFieldType.Date, DefaultValue = DateTime.Today.ToString("yyyy-MM-dd") },
            new FormField { Key = "endDate", Label = "Tanggal Selesai", Type = FormFieldType.Date, DefaultValue = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd") },
            Field("price", "Harga Sewa Bulanan (angka, tanpa titik)", FormFieldType.Number, defaultValue: firstPrice)
        }) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var v = dialog.Values;
        _repository.CreateContract(int.Parse(v["tenantId"]), int.Parse(v["roomId"]), DateTime.Parse(v["startDate"]), DateTime.Parse(v["endDate"]), decimal.Parse(v["price"], CultureInfo.InvariantCulture));
        MessageBox.Show("Kontrak berhasil dibuat dan status kamar diperbarui menjadi Terisi.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ChangeContractStatus()
    {
        var row = SelectedRow();
        if (row is null) return;
        var dialog = new EntityDialog("Ubah Status Kontrak", new[]
        {
            ComboField("status", "Status Kontrak", "Aktif", "Selesai", "Dibatalkan")
        }, new Dictionary<string, string> { ["status"] = Convert.ToString(row["Status"]) ?? "Aktif" }) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _repository.SetContractStatus(Convert.ToInt32(row["_id"]), dialog.Values["status"]);
        MessageBox.Show("Status kontrak berhasil diperbarui.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void GenerateBills()
    {
        if (MessageBox.Show($"Buat tagihan untuk kontrak aktif pada periode {DateTime.Today:MMMM yyyy}? Tagihan yang sudah ada tidak akan digandakan.",
            "Generate Tagihan", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        int count = _repository.GenerateMonthlyBills(DateTime.Today);
        MessageBox.Show($"Selesai. {count} tagihan baru dibuat untuk periode {DateTime.Today:yyyy-MM}.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenPaymentProof()
    {
        var row = SelectedRow();
        if (row is null) return;
        var path = Convert.ToString(row["_proof_path"]);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new InvalidOperationException("Bukti pembayaran belum diunggah atau file tidak ditemukan.");
        Process.Start(new ProcessStartInfo(path!) { UseShellExecute = true });
    }

    private void VerifyPayment()
    {
        var row = SelectedRow();
        if (row is null) return;
        var status = Convert.ToString(row["Status"]);
        if (status == "Lunas")
        {
            MessageBox.Show("Tagihan ini sudah berstatus Lunas.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var proof = Convert.ToString(row["_proof_path"]);
        if (string.IsNullOrWhiteSpace(proof) && MessageBox.Show("Belum ada bukti pembayaran yang diunggah. Tetap tandai sebagai Lunas?", "Konfirmasi Verifikasi", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        _repository.SetPaymentStatus(Convert.ToInt32(row["_id"]), "Lunas", "Diverifikasi oleh pengelola");
        MessageBox.Show("Pembayaran berhasil diverifikasi.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RejectPayment()
    {
        var row = SelectedRow();
        if (row is null) return;
        var dialog = new EntityDialog("Tolak Pembayaran", new[] { Field("reason", "Alasan penolakan") }) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _repository.SetPaymentStatus(Convert.ToInt32(row["_id"]), "Ditolak", dialog.Values["reason"]);
        MessageBox.Show("Pembayaran ditolak. Penghuni dapat mengunggah bukti yang benar.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void UploadPaymentProof()
    {
        var row = SelectedRow();
        if (row is null) return;
        if (Convert.ToString(row["Status"]) == "Lunas") throw new InvalidOperationException("Tagihan yang sudah lunas tidak dapat diubah.");
        var dialog = new OpenFileDialog
        {
            Title = "Pilih Bukti Pembayaran",
            Filter = "Gambar atau PDF|*.png;*.jpg;*.jpeg;*.webp;*.pdf|Semua File|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;
        _repository.UploadPaymentProof(Convert.ToInt32(row["_id"]), dialog.FileName);
        MessageBox.Show("Bukti pembayaran berhasil diunggah ke penyimpanan lokal aplikasi.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddComplaint()
    {
        if (_user.TenantId is null) throw new InvalidOperationException("Akun ini belum terhubung dengan profil penghuni.");
        var dialog = new EntityDialog("Buat Komplain Fasilitas", new[]
        {
            Field("title", "Judul Komplain"), Field("description", "Deskripsi Masalah", FormFieldType.Multiline)
        }) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _repository.CreateComplaint(_user.TenantId.Value, dialog.Values["title"], dialog.Values["description"]);
        MessageBox.Show("Komplain berhasil dikirim kepada pengelola.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ChangeComplaintStatus(string status)
    {
        var row = SelectedRow();
        if (row is null) return;
        _repository.UpdateComplaintStatus(Convert.ToInt32(row["_id"]), status);
        MessageBox.Show($"Status komplain diperbarui menjadi {status}.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RespondToComplaint()
    {
        var row = SelectedRow();
        if (row is null) return;
        var dialog = new EntityDialog("Tanggapan Pengelola", new[] { Field("response", "Tanggapan / tindak lanjut", FormFieldType.Multiline) }) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _repository.RespondToComplaint(Convert.ToInt32(row["_id"]), dialog.Values["response"]);
        MessageBox.Show("Tanggapan tersimpan. Status komplain menjadi Diproses.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportCsv(string key, string baseName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Simpan Laporan CSV",
            FileName = $"{baseName}_{DateTime.Today:yyyyMMdd}.csv",
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true
        };
        if (dialog.ShowDialog(this) != true) return;
        var table = _repository.GetExportData(key, _user);
        CsvExportService.Export(table, dialog.FileName);
        MessageBox.Show($"Laporan berhasil diekspor:\n{dialog.FileName}", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private DataRowView? SelectedRow()
    {
        if (_activeGrid?.SelectedItem is DataRowView row) return row;
        MessageBox.Show("Pilih satu baris data terlebih dahulu.", "KosKu", MessageBoxButton.OK, MessageBoxImage.Information);
        return null;
    }

    private static Color ParseColor(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex);
    }

    private static FormField Field(string key, string label, FormFieldType type = FormFieldType.Text, bool required = true, string? defaultValue = null)
        => new() { Key = key, Label = label, Type = type, Required = required, DefaultValue = defaultValue };

    private static FormField ComboField(string key, string label, params string[] values)
        => new()
        {
            Key = key,
            Label = label,
            Type = FormFieldType.Combo,
            Required = true,
            Options = values.Select(value => new FormOption { Label = value, Value = value }).ToList()
        };

    private sealed class PageAction
    {
        public string Label { get; }
        public Action Execute { get; }
        public string StyleKey { get; }
        public PageAction(string label, Action execute, string styleKey)
        {
            Label = label;
            Execute = execute;
            StyleKey = styleKey;
        }
    }
}
