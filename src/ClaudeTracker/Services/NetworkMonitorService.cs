using System.Net.NetworkInformation;
using System.Windows.Threading;
using ClaudeTracker.Services.Interfaces;

namespace ClaudeTracker.Services;

public class NetworkMonitorService : INetworkMonitorService
{
    private bool _wasAvailable = true;
    private DispatcherTimer? _debounceTimer;

    public event EventHandler? NetworkRestored;

    public void Start()
    {
        _wasAvailable = NetworkInterface.GetIsNetworkAvailable();
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        LoggingService.Instance.Log($"NetworkMonitor started (available: {_wasAvailable})");
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        LoggingService.Instance.Log($"Network availability changed: {e.IsAvailable}");

        // NetworkChange.NetworkAvailabilityChanged fires on a ThreadPool thread, which has no
        // message loop — a DispatcherTimer created there would never reliably tick. Marshal
        // onto the UI dispatcher before touching the timer or the shared _wasAvailable field.
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            _wasAvailable = e.IsAvailable;
            return;
        }

        dispatcher.BeginInvoke(() => HandleAvailabilityChanged(e.IsAvailable));
    }

    private void HandleAvailabilityChanged(bool isAvailable)
    {
        if (isAvailable && !_wasAvailable)
        {
            _debounceTimer?.Stop();
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer?.Stop();
                LoggingService.Instance.Log("Network restored — triggering refresh");
                NetworkRestored?.Invoke(this, EventArgs.Empty);
            };
            _debounceTimer.Start();
        }

        _wasAvailable = isAvailable;
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        _debounceTimer?.Stop();
    }
}
