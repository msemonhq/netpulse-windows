namespace NetPulse;

/// <summary>A compact optional always-on-top strip; Windows does not expose arbitrary taskbar controls to ordinary apps.</summary>
public sealed class TaskbarStrip : Form
{
    private readonly Label _rates = new();
    public TaskbarStrip()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Width = 300; Height = 30;
        BackColor = Color.FromArgb(28, 32, 40);
        _rates.Dock = DockStyle.Fill;
        _rates.TextAlign = ContentAlignment.MiddleCenter;
        _rates.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _rates.ForeColor = Color.White;
        Controls.Add(_rates);
        PositionOverTaskbar();
        Shown += (_, _) => PositionOverTaskbar();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        FormClosed += (_, _) => SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    public void UpdateTraffic(TrafficSample sample, SpeedUnit unit)
    {
        if (InvokeRequired) { BeginInvoke(() => UpdateTraffic(sample, unit)); return; }
        _rates.Text = $"↓ {RateFormatter.FormatRate(sample.DownloadBytesPerSecond, unit)}   ↑ {RateFormatter.FormatRate(sample.UploadBytesPerSecond, unit)}";
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed) BeginInvoke(PositionOverTaskbar);
    }

    private void PositionOverTaskbar()
    {
        var area = Screen.PrimaryScreen?.WorkingArea;
        if (area is null) return;
        // Adjacent to the taskbar rather than embedded inside Explorer's private taskbar surface.
        Location = new Point(area.Value.Right - Width - 8, area.Value.Bottom);
    }
}
