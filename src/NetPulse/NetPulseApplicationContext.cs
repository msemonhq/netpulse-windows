using System.Net.NetworkInformation;

namespace NetPulse;

public sealed class NetPulseApplicationContext : ApplicationContext
{
    private readonly NetworkTrafficMonitor _monitor = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _rateItem = new("Reading network traffic…") { Enabled = false };
    private readonly ToolStripMenuItem _sessionItem = new("Session totals: collecting…") { Enabled = false };
    private readonly ToolStripMenuItem _adapterStatusItem = new("Adapter: All active adapters") { Enabled = false };
    private readonly ToolStripMenuItem _adapterMenu = new("Network adapter");
    private readonly ToolStripMenuItem _startupItem = new("Launch at Windows sign-in");
    private readonly System.Windows.Forms.Timer _timer;
    private readonly NotifyIcon _notifyIcon;
    private AppSettings _settings;
    private Icon? _currentIcon;
    private TrafficCounters? _lastCounters;
    private DateTimeOffset _lastSampleAt;
    private double _downloadBytesPerSecond;
    private double _uploadBytesPerSecond;
    private long _sessionDownloaded;
    private long _sessionUploaded;
    private int _tickCount;
    private bool _showDownloadInIcon = true;

    public NetPulseApplicationContext()
    {
        _settings = _settingsStore.Load();

        _menu.Items.Add(_rateItem);
        _menu.Items.Add(_sessionItem);
        _menu.Items.Add(_adapterStatusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_adapterMenu);
        _startupItem.Checked = StartupRegistration.IsEnabled();
        _startupItem.Click += ToggleStartup;
        _menu.Items.Add(_startupItem);

        var aboutItem = new ToolStripMenuItem("About NetPulse");
        aboutItem.Click += (_, _) => MessageBox.Show(
            "NetPulse\nA lightweight, local network traffic meter.\n\n" +
            "The tray icon alternates between download and upload rates. " +
            "Hover over it to see both readings.",
            "About NetPulse",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        _menu.Items.Add(aboutItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();
        _menu.Items.Add(exitItem);

        _menu.Opening += (_, _) => RebuildAdapterMenu();

        _currentIcon = TrayIconRenderer.CreateIcon(0, isDownload: true);
        _notifyIcon = new NotifyIcon
        {
            Icon = _currentIcon,
            Text = "NetPulse: starting",
            ContextMenuStrip = _menu,
            Visible = true
        };

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) => UpdateMetrics();
        _timer.Start();
        UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        var now = DateTimeOffset.UtcNow;

        try
        {
            if (!_monitor.TryReadCounters(_settings.SelectedAdapterId, out var current))
            {
                // Clear the baseline while offline so reconnection does not create a spike.
                _lastCounters = null;
                _lastSampleAt = now;
                _downloadBytesPerSecond = 0;
                _uploadBytesPerSecond = 0;
                RefreshDisplay();
                return;
            }

            if (_lastCounters is { } previous)
            {
                var elapsed = now - _lastSampleAt;
                var receivedDelta = TrafficRateCalculator.GetDeltaBytes(
                    current.BytesReceived, previous.BytesReceived);
                var sentDelta = TrafficRateCalculator.GetDeltaBytes(
                    current.BytesSent, previous.BytesSent);

                _downloadBytesPerSecond = TrafficRateCalculator.GetBytesPerSecond(receivedDelta, elapsed);
                _uploadBytesPerSecond = TrafficRateCalculator.GetBytesPerSecond(sentDelta, elapsed);
                _sessionDownloaded = SaturatingAdd(_sessionDownloaded, receivedDelta);
                _sessionUploaded = SaturatingAdd(_sessionUploaded, sentDelta);
            }
            else
            {
                // The first sample is a baseline, not an amount transferred during this session.
                _downloadBytesPerSecond = 0;
                _uploadBytesPerSecond = 0;
            }

            _lastCounters = current;
            _lastSampleAt = now;
            _tickCount++;
            _showDownloadInIcon = _tickCount % 2 == 0;
            RefreshDisplay();
        }
        catch (Exception)
        {
            // A transient driver or adapter error must not close the tray app.
            _lastCounters = null;
            _downloadBytesPerSecond = 0;
            _uploadBytesPerSecond = 0;
            _notifyIcon.Text = "NetPulse: network counters unavailable";
            RefreshDisplay();
        }
    }

    private void RefreshDisplay()
    {
        _rateItem.Text =
            $"↓ Download: {RateFormatter.FormatRate(_downloadBytesPerSecond)}    " +
            $"↑ Upload: {RateFormatter.FormatRate(_uploadBytesPerSecond)}";
        _sessionItem.Text =
            $"Session: ↓ {RateFormatter.FormatTotal(_sessionDownloaded)}  ↑ {RateFormatter.FormatTotal(_sessionUploaded)}";
        _adapterStatusItem.Text = $"Adapter: {GetSelectedAdapterLabel()}";

        // NotifyIcon.Text is limited to 63 characters by Windows Forms.
        _notifyIcon.Text =
            $"↓ {RateFormatter.FormatRate(_downloadBytesPerSecond)}  ↑ {RateFormatter.FormatRate(_uploadBytesPerSecond)}";

        var visibleRate = _showDownloadInIcon
            ? _downloadBytesPerSecond
            : _uploadBytesPerSecond;
        var replacement = TrayIconRenderer.CreateIcon(visibleRate, _showDownloadInIcon);
        var previousIcon = _currentIcon;
        _currentIcon = replacement;
        _notifyIcon.Icon = replacement;
        previousIcon?.Dispose();
    }

    private void RebuildAdapterMenu()
    {
        foreach (ToolStripItem item in _adapterMenu.DropDownItems)
        {
            item.Dispose();
        }

        _adapterMenu.DropDownItems.Clear();

        var allAdapters = new ToolStripMenuItem("All active adapters")
        {
            Checked = _settings.SelectedAdapterId is null
        };
        allAdapters.Click += (_, _) => SelectAdapter(null);
        _adapterMenu.DropDownItems.Add(allAdapters);

        var adapters = _monitor.GetAdapters();
        if (adapters.Count == 0)
        {
            _adapterMenu.DropDownItems.Add(new ToolStripMenuItem("No network adapters found")
            {
                Enabled = false
            });
            return;
        }

        _adapterMenu.DropDownItems.Add(new ToolStripSeparator());

        foreach (var adapter in adapters)
        {
            var status = adapter.Status == OperationalStatus.Up ? string.Empty : " (disconnected)";
            var label = Truncate($"{adapter.Name}{status}", 42);
            var item = new ToolStripMenuItem(label)
            {
                Checked = string.Equals(
                    _settings.SelectedAdapterId, adapter.Id, StringComparison.Ordinal)
            };
            var adapterId = adapter.Id;
            item.Click += (_, _) => SelectAdapter(adapterId);
            _adapterMenu.DropDownItems.Add(item);
        }
    }

    private void SelectAdapter(string? adapterId)
    {
        if (string.Equals(_settings.SelectedAdapterId, adapterId, StringComparison.Ordinal))
        {
            return;
        }

        var previousAdapterId = _settings.SelectedAdapterId;
        _settings.SelectedAdapterId = adapterId;

        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _settings.SelectedAdapterId = previousAdapterId;
            MessageBox.Show(
                $"NetPulse could not save this preference.\n\n{exception.Message}",
                "NetPulse",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // New adapter counters establish a fresh baseline to avoid a false speed spike.
        _lastCounters = null;
        _downloadBytesPerSecond = 0;
        _uploadBytesPerSecond = 0;
        RefreshDisplay();
    }

    private void ToggleStartup(object? sender, EventArgs e)
    {
        var enable = !_startupItem.Checked;

        try
        {
            StartupRegistration.SetEnabled(enable);
            _startupItem.Checked = enable;
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or InvalidOperationException or System.Security.SecurityException)
        {
            MessageBox.Show(
                $"NetPulse could not update the Windows sign-in setting.\n\n{exception.Message}",
                "NetPulse",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private string GetSelectedAdapterLabel()
    {
        if (_settings.SelectedAdapterId is null)
        {
            return "All active adapters";
        }

        var adapter = _monitor.GetAdapters().FirstOrDefault(item =>
            string.Equals(item.Id, _settings.SelectedAdapterId, StringComparison.Ordinal));

        return adapter is null ? "Selected adapter (not found)" : adapter.Name;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private static long SaturatingAdd(long current, long delta)
    {
        if (delta <= 0)
        {
            return current;
        }

        return current > long.MaxValue - delta ? long.MaxValue : current + delta;
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _currentIcon?.Dispose();
        _currentIcon = null;
        base.ExitThreadCore();
    }
}
