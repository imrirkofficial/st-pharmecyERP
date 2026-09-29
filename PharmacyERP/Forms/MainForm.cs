using PharmacyERP.Services;

namespace PharmacyERP.Forms
{
    public partial class MainForm : Form
    {
        private Button btnDueKhata = new();
        private Button btnReturns = new();
        private Button btnClosing = new();
        private Button btnSettings = new();

        public MainForm()
        {
            InitializeComponent();
            AddBdButtons();
            ApplyShopStyle();
            lblUserInfo.Text = $"User: {CurrentUser.FullName} ({CurrentUser.Role})";
            ApplyRoleAccess();
            LoadFormIntoPanel(new DashboardForm());
        }

        private static Button MakeSideButton(string text)
        {
            return new Button
            {
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.White,
                Height = 50,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
            };
        }

        private void AddBdButtons()
        {
            btnDueKhata = MakeSideButton("📒 " + Lang.T("dueKhata"));
            btnReturns = MakeSideButton("↩️ " + Lang.T("returns"));
            btnClosing = MakeSideButton("🌙 " + Lang.T("closing"));
            btnSettings = MakeSideButton("⚙️ " + Lang.T("settings"));
            btnDueKhata.Click += (_, _) => LoadFormIntoPanel(new DueKhataForm());
            btnReturns.Click += (_, _) => LoadFormIntoPanel(new ReturnsForm());
            btnClosing.Click += (_, _) => LoadFormIntoPanel(new ClosingForm());
            btnSettings.Click += (_, _) => LoadFormIntoPanel(new SettingsForm());
            foreach (var b in new[] { btnDueKhata, btnReturns, btnClosing, btnSettings })
            {
                b.FlatAppearance.BorderSize = 0;
                panelSidebar.Controls.Add(b);
            }
            // Re-add top-docked buttons in reverse-visual order so sidebar reads:
            // Dashboard..Prescriptions, Due, Returns, Reports, Closing, Settings
            foreach (var b in new[]
            {
                btnSettings, btnClosing, btnReports, btnReturns, btnDueKhata,
                btnPrescriptions, btnCustomers, btnSuppliers, btnPurchase,
                btnSales, btnMedicines, btnDashboard
            })
                panelSidebar.Controls.Add(b);
        }

        private void ApplyShopStyle()
        {
            // Exclude brand areas BEFORE theming, otherwise Theme.Apply overwrites them.
            panelTop.Tag = "NoTheme";
            panelSidebar.Tag = "NoTheme";

            // 1) Theme first (content area, grids, dialogs)
            Theme.Apply(this);

            // 2) Brand areas AFTER theme
            var s = BdSettings.Current;
            var branch = BdSettings.IsMulti && !string.IsNullOrWhiteSpace(s.CurrentBranchName) ? $" [{s.CurrentBranchName}]" : "";
            lblTitle.Text = s.PharmacyName + branch;
            btnDashboard.Text = "🏠 " + Lang.T("dashboard");
            btnMedicines.Text = "💊 " + Lang.T("medicines");
            btnSales.Text = "💰 " + Lang.T("salesPos");
            btnPurchase.Text = "🛒 " + Lang.T("purchase");
            btnSuppliers.Text = "🏢 " + Lang.T("suppliers");
            btnCustomers.Text = "👥 " + Lang.T("customers");
            btnPrescriptions.Text = "📋 " + Lang.T("prescriptions");
            btnReports.Text = "📊 " + Lang.T("reports");
            btnLogout.Text = "🚪 " + Lang.T("logout");

            var sidebarBack = Theme.IsDark ? Color.FromArgb(25, 25, 30) : Color.FromArgb(0, 51, 102);
            var topBack = Theme.IsDark ? Color.FromArgb(25, 25, 30) : Theme.Accent;

            panelTop.BackColor = topBack;
            panelSidebar.BackColor = sidebarBack;

            // Top bar labels must stay white on accent/dark
            lblTitle.ForeColor = Color.White;
            lblTitle.BackColor = Color.Transparent;
            lblUserInfo.ForeColor = Color.White;
            lblUserInfo.BackColor = Color.Transparent;

            // Sidebar buttons: white text on dark sidebar in both themes
            foreach (Control c in panelSidebar.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = sidebarBack;
                    b.ForeColor = Color.White;
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderSize = 0;
                    b.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                        Math.Min(255, sidebarBack.R + 25),
                        Math.Min(255, sidebarBack.G + 25),
                        Math.Min(255, sidebarBack.B + 25));
                }
            }
        }

        private void ApplyRoleAccess()
        {
            // Server enforces auth; UI just hides what the role can't use.
            var role = CurrentUser.Role;
            bool isAdmin = role is "Admin" or "Manager";
            bool isStaff = isAdmin || role is "Pharmacist";
            btnPurchase.Enabled = isStaff;
            btnSuppliers.Enabled = isStaff;
            btnReports.Enabled = isAdmin;
            btnPrescriptions.Enabled = isStaff || role is "Cashier";
            btnMedicines.Enabled = isStaff;
            btnSales.Enabled = true;
            btnCustomers.Enabled = true;
            btnDashboard.Enabled = true;
            btnDueKhata.Enabled = true;
            btnReturns.Enabled = isStaff;
            btnClosing.Enabled = isAdmin;
            btnSettings.Enabled = isAdmin;
        }

        private void LoadFormIntoPanel(Form form)
        {
            panelContent.Controls.Clear();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            panelContent.Controls.Add(form);
            form.Show();
        }

        private void btnDashboard_Click(object sender, EventArgs e) => LoadFormIntoPanel(new DashboardForm());
        private void btnMedicines_Click(object sender, EventArgs e) => LoadFormIntoPanel(new MedicinesForm());
        private void btnSales_Click(object sender, EventArgs e) => LoadFormIntoPanel(new PosForm());
        private void btnPurchase_Click(object sender, EventArgs e) => LoadFormIntoPanel(new PurchaseForm());
        private void btnSuppliers_Click(object sender, EventArgs e) => LoadFormIntoPanel(new SuppliersForm());
        private void btnCustomers_Click(object sender, EventArgs e) => LoadFormIntoPanel(new CustomersForm());
        private void btnPrescriptions_Click(object sender, EventArgs e) => LoadFormIntoPanel(new PrescriptionsForm());
        private void btnReports_Click(object sender, EventArgs e) => LoadFormIntoPanel(new ReportsForm());

        private void btnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ApiClient.Instance.ClearToken();
                this.Close();
                LoginForm loginForm = new LoginForm();
                loginForm.Show();
            }
        }
    }
}
