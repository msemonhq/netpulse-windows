namespace NetPulse;

public sealed class SpeedWidget : Form
{
    private readonly Label _download = new();
    private readonly Label _upload = new();
    private readonly Label _adapter = new();
    private readonly Label _header = new();
    private readonly SettingsStore _store;
    private readonly AppSettings _settings;
    private bool _dragging;
    private Point _dragOrigin;

    public event Action? PositionChanged;

    public SpeedWidget(SettingsStore store, AppSettings settings)
    {
        _store = store; _settings = settings;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Width = 270; Height = 116;
        BackColor = SystemInformation.HighContrast ? SystemColors.Window : Color.FromArgb(28, 32, 40);
        ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Color.White;
        Padding = new Padding(12);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = BackColor };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _header.Text = "NETPULSE  ·  drag to move";
        _header.Font = new Font("Segoe UI", 8, FontStyle.Bold);
        _header.ForeColor = SystemInformation.HighContrast ? SystemColors.WindowText : Color.Silver;
        _download.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _upload.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _download.ForeColor = Color.FromArgb(114, 183, 255);
        _upload.ForeColor = Color.FromArgb(128, 223, 174);
        _adapter.Font = new Font("Segoe UI", 8);
        _adapter.ForeColor = _header.ForeColor;
        layout.Controls.Add(_header, 0, 0);
        layout.Controls.Add(_download, 0, 1);
        layout.Controls.Add(_upload, 0, 2);
        Controls.Add(layout);

        foreach (Control control in new Control[] { this, layout, _header })
        {
            control.MouseDown += BeginDrag;
            control.MouseMove += ContinueDrag;
            control.MouseUp += EndDrag;
        }

        if (_settings.WidgetX >= 0 && _settings.WidgetY >= 0)
            Location = new Point(_settings.WidgetX, _settings.WidgetY);
        else
            Location = new Point(Screen.PrimaryScreen?.WorkingArea.Right - Width - 24 ?? 800, 120);

        Shown += (_, _) => ApplyTheme();
        FormClosing += (_, e) => { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
    }

    public void UpdateTraffic(TrafficSample sample, SpeedUnit unit)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => UpdateTraffic(sample, unit)); return; }
        _download.Text = $"↓  {RateFormatter.FormatRate(sample.DownloadBytesPerSecond, unit)}";
        _upload.Text = $"↑  {RateFormatter.FormatRate(sample.UploadBytesPerSecond, unit)}";
        _header.Text = sample.IsConnected ? $"NETPULSE  ·  {sample.PreferredAdapterName}" : "NETPULSE  ·  Offline";
    }

    private void ApplyTheme()
    {
        // Use the Windows system preference as a safe baseline. High contrast is always respected.
        if (SystemInformation.HighContrast) { BackColor = SystemColors.Window; ForeColor = SystemColors.WindowText; }
    }

    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _dragOrigin = new Point(e.X, e.Y);
    }

    private void ContinueDrag(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var screen = ((Control)sender!).PointToScreen(e.Location);
        Location = new Point(screen.X - _dragOrigin.X, screen.Y - _dragOrigin.Y);
    }

    private void EndDrag(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        _settings.WidgetX = Left; _settings.WidgetY = Top;
        try { _store.Save(_settings); } catch (IOException) { }
        PositionChanged?.Invoke();
    }
}
