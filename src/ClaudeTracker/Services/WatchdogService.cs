using System.Collections.ObjectModel;
using System.Windows;
using ClaudeTracker.Models;
using ClaudeTracker.Services.Interfaces;
using ClaudeTracker.Utilities;
using ClaudeTracker.Views;

namespace ClaudeTracker.Services;

public class WatchdogService : IWatchdogService
{
    private const int MaxHistory = 200;

    private readonly IProfileService _profileService;
    private readonly ISessionTrackingService _sessionTracking;
    private readonly INotificationService _notificationService;
    private readonly object _lock = new();
    private readonly HashSet<string> _resumingNow = new();

    public ObservableCollection<WatchdogEvent> EventLog { get; } = new();

    public WatchdogService(
        IProfileService profileService,
        ISessionTrackingService sessionTracking,
        INotificationService notificationService)
    {
        _profileService = profileService;
        _sessionTracking = sessionTracking;
        _notificationService = notificationService;

        var profile = _profileService.ActiveProfile;
        if (profile != null)
        {
            foreach (var e in profile.WatchdogHistory) EventLog.Add(e);
        }
    }

    public void OnUsageUpdated(Profile profile, ClaudeUsage? previousUsage, ClaudeUsage newUsage)
    {
        if (!profile.ResumeCliSessionOnReset) return;

        var previousPercentage = previousUsage?.EffectiveSessionPercentage;
        var newPercentage = newUsage.EffectiveSessionPercentage;

        if (WatchdogLogic.IsLimitHitTransition(previousPercentage, newPercentage))
            CaptureLimitHit(profile, newUsage);

        if (newPercentage < Constants.Watchdog.LimitReachedThreshold && profile.PendingResumes.Count > 0)
            TryResumePending(profile);
    }

    /// <summary>Snapshot currently-active CLI sessions at the instant the account limit was hit.
    /// Usage data is account-wide and does not identify the exact interrupted session, so the
    /// settings UI deliberately presents these entries for review before automatic resume.</summary>
    private void CaptureLimitHit(Profile profile, ClaudeUsage usage)
    {
        var sessions = _sessionTracking.ActiveSessions
            .Where(s => !string.IsNullOrEmpty(s.Cwd))
            .ToList();
        if (sessions.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var session in sessions)
        {
            var existing = profile.PendingResumes.FirstOrDefault(p => p.SessionId == session.SessionId);
            if (existing != null)
            {
                existing.LimitHitAtUtc = now;
                existing.SessionResetAtUtc = usage.SessionResetTime;
                existing.PermissionMode = session.PermissionMode;
            }
            else
            {
                profile.PendingResumes.Add(new PendingResume
                {
                    SessionId = session.SessionId,
                    Cwd = session.Cwd,
                    PermissionMode = session.PermissionMode,
                    LimitHitAtUtc = now,
                    SessionResetAtUtc = usage.SessionResetTime,
                    IsEnabled = true
                });
            }
        }

        _profileService.UpdateProfile(profile);
        LoggingService.Instance.Log(
            $"Watchdog: captured {sessions.Count} interrupted session(s) at limit-hit, resets at {usage.SessionResetTime:HH:mm} UTC");
    }

    private void TryResumePending(Profile profile)
    {
        var now = DateTime.UtcNow;

        var ready = profile.PendingResumes
            .Where(p => WatchdogLogic.IsReadyToResume(p.SessionResetAtUtc, now))
            .Where(p => !WatchdogLogic.IsStale(p.LimitHitAtUtc, now))
            .Where(p => p.IsEnabled)
            .Where(p => WatchdogLogic.IsProjectAllowed(p.Cwd, profile.WatchdogAllowedProjects))
            .ToList();

        foreach (var pending in ready)
        {
            bool started;
            lock (_lock) { started = _resumingNow.Add(pending.SessionId); }
            if (!started) continue; // already retrying in the background

            _ = RunResumeLoopAsync(profile, pending);
        }

        var stale = profile.PendingResumes
            .Where(p => WatchdogLogic.IsStale(p.LimitHitAtUtc, now))
            .ToList();
        if (stale.Count > 0)
        {
            foreach (var s in stale) profile.PendingResumes.Remove(s);
            _profileService.UpdateProfile(profile);
        }
    }

    private async Task RunResumeLoopAsync(Profile profile, PendingResume pending)
    {
        try
        {
            await ResumeWithRetriesAsync(profile, pending);
        }
        catch (Exception ex)
        {
            pending.LastOutcome = "Failed";
            pending.LastAttemptUtc = DateTime.UtcNow;
            if (profile.PendingResumes.Contains(pending))
            {
                _profileService.UpdateProfile(profile);
                Record(profile, new WatchdogEvent
                {
                    SessionId = pending.SessionId,
                    Cwd = pending.Cwd,
                    Kind = WatchdogEventKind.ResumeFailed,
                    Message = $"Unexpected resume error: {ex.GetType().Name}"
                });
            }
            LoggingService.Instance.LogError("Watchdog resume failed", ex);
        }
        finally
        {
            lock (_lock) { _resumingNow.Remove(pending.SessionId); }
        }
    }

    /// <summary>One session's full resume attempt, including retries with backoff. Runs until
    /// success, exhausted retries, or a permission-mode skip.</summary>
    private async Task ResumeWithRetriesAsync(Profile profile, PendingResume pending)
    {
        while (true)
        {
            if (profile.WatchdogMode == WatchdogMode.Unattended
                && !WatchdogLogic.IsNonPromptingPermissionMode(pending.PermissionMode))
            {
                pending.LastOutcome = "SkippedRequiresApproval";
                pending.LastAttemptUtc = DateTime.UtcNow;
                _profileService.UpdateProfile(profile);
                Record(profile, new WatchdogEvent
                {
                    SessionId = pending.SessionId,
                    Cwd = pending.Cwd,
                    Kind = WatchdogEventKind.SkippedRequiresApproval,
                    Message = $"Skipped — session was running in '{pending.PermissionMode}' permission mode, " +
                              "which can still prompt. Unattended resume only runs for sessions already in a " +
                              "non-prompting mode; switch to Interactive mode to resume this one."
                });
                return;
            }

            pending.AttemptCount++;
            pending.LastAttemptUtc = DateTime.UtcNow;
            _profileService.UpdateProfile(profile);
            Record(profile, new WatchdogEvent
            {
                SessionId = pending.SessionId,
                Cwd = pending.Cwd,
                Kind = WatchdogEventKind.ResumeLaunched,
                Message = $"Attempt {pending.AttemptCount}: resuming in " +
                          $"{(profile.WatchdogMode == WatchdogMode.Unattended ? "unattended" : "interactive")} mode"
            });

            var (success, detail) = await AttemptOnceAsync(profile, pending);

            if (success)
            {
                pending.LastOutcome = "Success";
                profile.PendingResumes.Remove(pending);
                _profileService.UpdateProfile(profile);
                Record(profile, new WatchdogEvent
                {
                    SessionId = pending.SessionId, Cwd = pending.Cwd,
                    Kind = WatchdogEventKind.ResumeSucceeded, Message = detail
                });

                ((NotificationService)_notificationService).SendNotification(
                    "Resuming Claude session", $"Continuing work in {pending.ProjectName}",
                    NotificationPopup.NotificationLevel.Info, cwd: pending.Cwd);
                return;
            }

            if (pending.AttemptCount >= Constants.Watchdog.MaxRetries)
            {
                pending.LastOutcome = "Failed";
                _profileService.UpdateProfile(profile);
                Record(profile, new WatchdogEvent
                {
                    SessionId = pending.SessionId, Cwd = pending.Cwd,
                    Kind = WatchdogEventKind.RetryExhausted,
                    Message = $"{detail} — giving up after {pending.AttemptCount} attempts"
                });
                return;
            }

            pending.LastOutcome = "Retrying";
            _profileService.UpdateProfile(profile);
            var backoff = WatchdogLogic.GetRetryBackoff(pending.AttemptCount);
            Record(profile, new WatchdogEvent
            {
                SessionId = pending.SessionId, Cwd = pending.Cwd,
                Kind = WatchdogEventKind.RetryScheduled,
                Message = $"{detail} — retrying in {backoff.TotalSeconds:0}s " +
                          $"(attempt {pending.AttemptCount + 1} of {Constants.Watchdog.MaxRetries})"
            });

            await Task.Delay(backoff);
        }
    }

    private static async Task<(bool Success, string Detail)> AttemptOnceAsync(Profile profile, PendingResume pending)
    {
        if (profile.WatchdogMode == WatchdogMode.Unattended)
        {
            var result = await TerminalLauncher.RunUnattendedAsync(
                pending.Cwd, pending.SessionId, pending.PermissionMode, Constants.Watchdog.DefaultContinuationPrompt);
            var detail = result.Success
                ? "Completed successfully"
                : $"Exit code {result.ExitCode}: {Truncate(result.Output, 200)}";
            return (result.Success, detail);
        }

        var launched = TerminalLauncher.LaunchInteractive(
            pending.Cwd, pending.SessionId, pending.PermissionMode, Constants.Watchdog.DefaultContinuationPrompt);
        return (launched, launched ? "Terminal opened" : "Failed to launch terminal");
    }

    private void Record(Profile profile, WatchdogEvent evt)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            EventLog.Insert(0, evt);
            while (EventLog.Count > MaxHistory) EventLog.RemoveAt(EventLog.Count - 1);
        });

        profile.WatchdogHistory.Insert(0, evt);
        while (profile.WatchdogHistory.Count > MaxHistory) profile.WatchdogHistory.RemoveAt(profile.WatchdogHistory.Count - 1);
        _profileService.UpdateProfile(profile);

        LoggingService.Instance.Log($"Watchdog[{evt.Kind}]: {evt.Message} ({evt.ProjectName})");
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}
