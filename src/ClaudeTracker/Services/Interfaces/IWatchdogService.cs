using System.Collections.ObjectModel;
using ClaudeTracker.Models;

namespace ClaudeTracker.Services.Interfaces;

/// <summary>
/// Watches for a Claude Code CLI session getting cut off by hitting the usage limit, and — if
/// the user has opted in — resumes that exact session with a continuation prompt once quota
/// resets, retrying on failure. See docs/plans watchdog design for the permission-mode safety
/// gate: Unattended auto-resume only ever runs for sessions already in a non-prompting mode.
/// </summary>
public interface IWatchdogService
{
    /// <summary>Persisted (per-profile) activity log for the Watchdog dashboard tab, newest first.</summary>
    ObservableCollection<WatchdogEvent> EventLog { get; }

    /// <summary>Call after usage data is refreshed, with the previous and newly-fetched values for
    /// the active profile. Detects the transition into "limit reached" (captures the exact
    /// interrupted session) and out of it (triggers resume attempts for anything pending).</summary>
    void OnUsageUpdated(Profile profile, ClaudeUsage? previousUsage, ClaudeUsage newUsage);
}
