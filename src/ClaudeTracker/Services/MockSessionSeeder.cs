using System.Windows.Threading;
using ClaudeTracker.Models;
using ClaudeTracker.Services.Interfaces;

namespace ClaudeTracker.Services;

/// <summary>Seeds fake Claude Code sessions for UI testing (--mock-agents).
/// Dev builds listen on their own hook pipe and receive no real events,
/// so this exercises the desktop pets end-to-end instead.</summary>
public class MockSessionSeeder
{
    private readonly ISessionTrackingService _tracking;
    private DispatcherTimer? _timer;
    private int _tick;
    private int _extraSessionCounter;
    private string? _extraSessionId;

    public MockSessionSeeder(ISessionTrackingService tracking)
    {
        _tracking = tracking;
    }

    public void Start()
    {
        _tracking.RegisterSession("mock-1", @"C:\dev\claude-tracker", "default", "opus");
        _tracking.RegisterSession("mock-2", @"C:\dev\website", "default", "sonnet");
        _tracking.RegisterSession("mock-3", @"C:\dev\api", "default", "opus");

        _tracking.RecordActivity("mock-1", new ActivityEntry { Summary = "Editing Foo.cs", ToolName = "Edit" });
        _tracking.RecordActivity("mock-2", new ActivityEntry { Summary = "Running tests", ToolName = "Bash" });

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
        _timer.Tick += (_, _) => Mutate();
        _timer.Start();

        LoggingService.Instance.Log("MockSessionSeeder started (3 fake sessions)");
    }

    private void Mutate()
    {
        _tick++;

        // mock-1 stays busy → permanently Working
        _tracking.RecordActivity("mock-1", new ActivityEntry
        {
            Summary = _tick % 2 == 0 ? "Editing Foo.cs" : "Running dotnet build",
            ToolName = _tick % 2 == 0 ? "Edit" : "Bash"
        });

        // mock-2 gets a subagent every 3rd tick, released the tick after → badge churn
        if (_tick % 3 == 0)
            _tracking.RegisterSubagent("mock-2", $"agent-{_tick}", "Explore");
        else if (_tick % 3 == 1)
            _tracking.EndSubagent("mock-2", $"agent-{_tick - 1}");

        // mock-3 is never touched → decays Working → Idle → Sleeping

        // Every ~90s: churn a 4th session to exercise add/remove diffing
        if (_tick % 13 == 0)
        {
            if (_extraSessionId != null)
                _tracking.EndSession(_extraSessionId);
            _extraSessionId = $"mock-extra-{++_extraSessionCounter}";
            _tracking.RegisterSession(_extraSessionId, @"C:\dev\scratch", "default", "haiku");
            _tracking.RecordActivity(_extraSessionId, new ActivityEntry { Summary = "Reading files", ToolName = "Read" });
        }
    }
}
