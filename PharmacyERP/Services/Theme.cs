namespace PharmacyERP.Services;

/// <summary>Light/Dark theme + Big-Font mode for WinForms. No extra packages.</summary>
public static class Theme
{
    public static bool IsDark => string.Equals(BdSettings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase);

    public static Color Back => IsDark ? Color.FromArgb(32, 32, 36) : Color.White;
    public static Color Surface => IsDark ? Color.FromArgb(45, 45, 50) : Color.FromArgb(245, 245, 247);
    public static Color Fore => IsDark ? Color.WhiteSmoke : Color.FromArgb(30, 30, 30);
    public static Color Accent => Color.FromArgb(0, 122, 204);
    public static Color Danger => IsDark ? Color.FromArgb(255, 110, 110) : Color.FromArgb(192, 0, 0);
    public static Color Ok => IsDark ? Color.FromArgb(120, 220, 150) : Color.FromArgb(0, 130, 60);

    public static void Apply(Form form)
    {
        form.BackColor = Back;
        form.ForeColor = Fore;
        ApplyTo(form.Controls);
        // Grids readable in both themes
        foreach (var g in form.Controls.OfType<DataGridView>().Concat(
            form.Controls.OfType<Control>().SelectMany(All).OfType<DataGridView>()))
        {
            g.BackgroundColor = Back;
            g.ForeColor = IsDark ? Color.WhiteSmoke : Color.Black;
            g.DefaultCellStyle.BackColor = Back;
            g.DefaultCellStyle.ForeColor = IsDark ? Color.WhiteSmoke : Color.Black;
            g.ColumnHeadersDefaultCellStyle.BackColor = Surface;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Fore;
        }
    }

    private static void ApplyTo(Control.ControlCollection controls)
    {
        foreach (Control c in controls)
        {
            // Skip branded areas (top bar / sidebar) — MainForm styles them after Theme.Apply
            if (Equals(c.Tag?.ToString(), "NoTheme"))
                continue;
            switch (c)
            {
                case Button b:
                    if (b.BackColor == SystemColors.Control || b.BackColor == Color.White || IsDark)
                    {
                        b.BackColor = Surface;
                        b.ForeColor = Fore;
                        b.FlatStyle = FlatStyle.Flat;
                        b.FlatAppearance.BorderColor = Accent;
                    }
                    break;
                case DataGridView:
                    break; // handled above
                case Panel or FlowLayoutPanel or SplitContainer or GroupBox or TabControl or TabPage:
                    c.BackColor = Surface;
                    c.ForeColor = Fore;
                    break;
                case Label l:
                    l.ForeColor = Fore;
                    l.BackColor = Color.Transparent;
                    break;
                default:
                    c.BackColor = Back;
                    c.ForeColor = Fore;
                    break;
            }
            if (BdSettings.Current.BigFont && c.Font.Size < 12)
                c.Font = new Font(c.Font.FontFamily, c.Font.Size + 2, c.Font.Style);
            if (c.HasChildren) ApplyTo(c.Controls);
        }
    }

    private static IEnumerable<Control> All(Control c)
    {
        foreach (Control ch in c.Controls) { yield return ch; foreach (var g in All(ch)) yield return g; }
    }
}
