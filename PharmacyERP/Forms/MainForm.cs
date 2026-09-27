using PharmacyERP.Services;

namespace PharmacyERP.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            lblUserInfo.Text = $"User: {CurrentUser.FullName} ({CurrentUser.Role})";
            ApplyRoleAccess();
            LoadFormIntoPanel(new DashboardForm());
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
