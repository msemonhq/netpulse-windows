using System.Net.NetworkInformation;

namespace NetPulse;

public sealed class NetPulseApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore = new();
    private readonly AppSettings _settings;
    private readonly ICounterSource _counterSource;
    private readonly TrafficSampler _sampler;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu = new();
    private readonly System.Threading.Timer _timer;
    private readonly ToolStripMenuItem _unitItem = new("Show speeds in Mbps");
    private readonly ToolStripMenuItem _stripItem = new("Show taskbar-adjacent strip");
    private readonly ToolStripMenuItem _startupItem = new("Launch at Windows sign-in");
    private readonly SpeedWidget _widget;
    private TaskbarStrip? _strip;
    private int _sampling;
    private bool _disposed;

    public NetPulseApplicationContext()
    {
        _settings = _settingsStore.Load();
        _counterSource = new NetworkTrafficMonitor();
        _sampler = new TrafficSampler(_counterSource);

        _widget = new SpeedWidget(_settingsStore, _settings);
        _widget.Show();
        _widget.UpdateTraffic(new TrafficSample(0, 0, 0, 0, "Detecting network…", false), CurrentUnit());

        _unitItem.Checked = _settings.UseMegabits;
        _unitItem.Click += (_, _) =>
        {
            _settings.UseMegabits = !_settings.UseMegabits;
            SaveSettings();
            _unitItem.Checked = _settings.UseMegabits;
        };
        _menu.Items.Add(_unitItem);

        _stripItem.Checked = _settings.ShowTaskbarStrip;
        _stripItem.Click += (_, _) => ToggleStrip();
        _menu.Items.Add(_stripItem);

        _startupItem.Checked = StartupRegistration.IsEnabled();
        _startupItem.Click += (_, _) =>
        {
            try
            {
                StartupRegistration.SetEnabled(!_startupItem.Checked);
                _startupItem.Checked = StartupRegistration.IsEnabled();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException)
            {
                MessageBox.Show($"Could not change startup setting.\n{ex.Message}", "NetPulse", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        _menu.Items.Add(_startupItem);

        var showItem = new ToolStripMenuItem("Show speed widget");
        showItem.Click += (_, _) => { _widget.Show(); _widget.Activate(); };
        _menu.Items.Add(showItem);
        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => ShowSettings();
        _menu.Items.Add(settingsItem);
        _menu.Items.Add(new ToolStripSeparator());
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();
        _menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "NetPulse — starting",
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => { _widget.Show(); _widget.Activate(); };
        if (_settings.ShowTaskbarStrip) ShowStrip();

        _timer = new System.Threading.Timer(_ => SampleOffUiThread(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private SpeedUnit CurrentUnit() => _settings.UseMegabits
        ? SpeedUnit.MegabitsPerSecond : SpeedUnit.MegabytesPerSecond;

    private void SampleOffUiThread()
    {
        if (Interlocked.Exchange(ref _sampling, 1) != 0) return;
        try
        {
            var sample = _sampler.Sample();
            if (_disposed) return;
            _widget.BeginInvoke(() =>
            {
                if (_disposed) return;
                _widget.UpdateTraffic(sample, CurrentUnit());
                if (_strip is { IsDisposed: false }) _strip.UpdateTraffic(sample, CurrentUnit());
                _notifyIcon.Text = $"↓ {RateFormatter.FormatRate(sample.DownloadBytesPerSecond, CurrentUnit())} ↑ {RateFormatter.FormatRate(sample.UploadBytesPerSecond, CurrentUnit())}";
            });
        }
        catch (Exception)
        {
            if (!_disposed)
            {
                try { _widget.BeginInvoke(new Action(() => { if (!_disposed) _notifyIcon.Text = "NetPulse — counters unavailable"; })); }
                catch (InvalidOperationException) { }
            }
        }
        finally { Volatile.Write(ref _sampling, 0); }
    }

    private void ToggleStrip()
    {
        _settings.ShowTaskbarStrip = !_settings.ShowTaskbarStrip;
        _stripItem.Checked = _settings.ShowTaskbarStrip;
        if (_settings.ShowTaskbarStrip) ShowStrip();
        else { _strip?.Close(); _strip?.Dispose(); _strip = null; }
        SaveSettings();
    }

    private void ShowStrip()
    {
        if (_strip is { IsDisposed: false }) return;
        _strip = new TaskbarStrip();
        _strip.Show();
    }

    private void ShowSettings()
    {
        using var form = new Form
        {
            Text = "NetPulse Settings",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = true,
            ClientSize = new Size(330, 150)
        };
        var unit = new CheckBox { Text = "Display speeds in Mbps (instead of MB/s)", AutoSize = true, Left = 16, Top = 18, Checked = _settings.UseMegabits };
        var strip = new CheckBox { Text = "Show taskbar-adjacent speed strip", AutoSize = true, Left = 16, Top = 50, Checked = _settings.ShowTaskbarStrip };
        var close = new Button { Text = "Save", DialogResult = DialogResult.OK, Left = 238, Top = 104, Width = 75 };
        form.Controls.AddRange([unit, strip, close]);
        form.AcceptButton = close;
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings.UseMegabits = unit.Checked;
            if (_settings.ShowTaskbarStrip != strip.Checked) ToggleStrip();
            _unitItem.Checked = _settings.UseMegabits;
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        try { _settingsStore.Save(_settings); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Could not save settings.\n{ex.Message}", "NetPulse", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void ExitThreadCore()
    {
        _disposed = true;
        _timer.Dispose();
        _strip?.Close();
        _strip?.Dispose();
        _widget.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        base.ExitThreadCore();
    }
}
