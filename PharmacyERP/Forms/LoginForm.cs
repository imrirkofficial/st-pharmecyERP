using PharmacyERP.Services;

namespace PharmacyERP.Forms
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter username and password.", "Login Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            try
            {
                var result = await ApiClient.Instance.LoginAsync(username, password);

                CurrentUser.UserId = 0; // resolved server-side from JWT
                CurrentUser.Username = result.Username;
                CurrentUser.FullName = result.FullName;
                CurrentUser.Role = result.Role;
                CurrentUser.Token = result.Token;

                MessageBox.Show($"Welcome, {result.FullName}! ({result.Role})", "Login Successful",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Hide();
                using MainForm mainForm = new();
                mainForm.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login failed: {ex.Message}", "Login Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtUsername.Focus();
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }

    // Session info from JWT (server is source of truth)
    public static class CurrentUser
    {
        public static int UserId { get; set; }
        public static string Username { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
        public static string Token { get; set; } = string.Empty;
    }
}
