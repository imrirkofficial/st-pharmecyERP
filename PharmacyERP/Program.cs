using PharmacyERP.Forms;
using PharmacyERP.Services;

namespace PharmacyERP
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            // No direct DB access: API (with EF Migrations) is the source of truth.
            // If the API is down, LoginForm will show the connection error.
            ApiClient.Instance.ToString();
            Application.Run(new LoginForm());
        }
    }
}
