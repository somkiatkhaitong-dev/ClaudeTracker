using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeTracker.Models;
using ClaudeTracker.Services.Interfaces;

namespace ClaudeTracker.ViewModels;

public partial class WatchdogViewModel : ObservableObject
{
    private readonly IProfileService _profileService;
    private readonly IWatchdogService _watchdogService;
    private readonly ISessionTrackingService _sessionTracking;

    [ObservableProperty] private bool _resumeCliSessionOnReset;
    [ObservableProperty] private bool _isUnattendedMode; // false = Interactive, true = Unattended
    [ObservableProperty] private bool _hasUnsavedChanges;

    public ObservableCollection<PendingResume> PendingResumes { get; } = new();
    public ObservableCollection<WatchdogEvent> EventLog => _watchdogService.EventLog;

    /// <summary>Allowed-project paths as edited in this session — only written to the profile
    /// on Save(), like the toggle/mode above.</summary>
    public ObservableCollection<string> AllowedProjects { get; } = new();

    /// <summary>Distinct project directories Claude Code has actually been seen running in
    /// (active sessions + captured history), offered as picks when adding to the allowlist.</summary>
    public ObservableCollection<string> KnownProjectPaths { get; } = new();

    /// <summary>Currently-active CLI sessions (from hooks), offered as picks for manually
    /// queuing a PENDING resume without waiting for a real usage-limit hit.</summary>
    public ObservableCollection<SessionState> ActiveSessions => _sessionTracking.ActiveSessions;

    private bool _initialResumeEnabled;
    private bool _initialUnattended;
    private List<string> _initialAllowedProjects = new();
    private bool _initialized;

    public WatchdogViewModel(IProfileService profileService, IWatchdogService watchdogService, ISessionTrackingService sessionTracking)
    {
        _profileService = profileService;
        _watchdogService = watchdogService;
        _sessionTracking = sessionTracking;

        var profile = _profileService.ActiveProfile;
        if (profile != null)
        {
            ResumeCliSessionOnReset = profile.ResumeCliSessionOnReset;
            IsUnattendedMode = profile.WatchdogMode == WatchdogMode.Unattended;
            RefreshPendingResumes(profile);
            RefreshAllowedProjects(profile);
            RefreshKnownProjectPaths(profile);
        }

        _initialResumeEnabled = ResumeCliSessionOnReset;
        _initialUnattended = IsUnattendedMode;
        _initialAllowedProjects = AllowedProjects.ToList();
        _initialized = true;
    }

    private void RefreshPendingResumes(Profile profile)
    {
        PendingResumes.Clear();
        foreach (var p in profile.PendingResumes)
        {
            p.ResetTimeInputText = p.SessionResetAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            PendingResumes.Add(p);
        }
    }

    private void RefreshAllowedProjects(Profile profile)
    {
        AllowedProjects.Clear();
        foreach (var p in profile.WatchdogAllowedProjects) AllowedProjects.Add(p);
    }

    private void RefreshKnownProjectPaths(Profile profile)
    {
        KnownProjectPaths.Clear();
        var known = _sessionTracking.ActiveSessions.Select(s => s.Cwd)
            .Concat(profile.PendingResumes.Select(p => p.Cwd))
            .Concat(profile.WatchdogHistory.Select(h => h.Cwd))
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c);
        foreach (var c in known) KnownProjectPaths.Add(c);
    }

    /// <summary>Call when the tab becomes visible to pick up resumes/projects seen since it was built.</summary>
    public void Refresh()
    {
        var profile = _profileService.ActiveProfile;
        if (profile == null) return;
        RefreshPendingResumes(profile);
        RefreshKnownProjectPaths(profile);
    }

    partial void OnResumeCliSessionOnResetChanged(bool value) => DetectChanges();
    partial void OnIsUnattendedModeChanged(bool value) => DetectChanges();

    private void DetectChanges()
    {
        if (!_initialized) return;
        HasUnsavedChanges = ResumeCliSessionOnReset != _initialResumeEnabled
            || IsUnattendedMode != _initialUnattended
            || !AllowedProjects.SequenceEqual(_initialAllowedProjects, StringComparer.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void AddAllowedProject(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var trimmed = path.Trim();
        if (AllowedProjects.Any(p => p.Equals(trimmed, StringComparison.OrdinalIgnoreCase))) return;
        AllowedProjects.Add(trimmed);
        DetectChanges();
    }

    [RelayCommand]
    private void RemoveAllowedProject(string path)
    {
        AllowedProjects.Remove(path);
        DetectChanges();
    }

    /// <summary>Manually queue a currently-active session for resume, without waiting for a real
    /// usage-limit hit — e.g. to test the flow, or to schedule a specific project to continue
    /// later. Defaults the reset time to one minute out; edit it via SetResumeTimeCommand.</summary>
    [RelayCommand]
    private void AddPendingResume(SessionState? session)
    {
        if (session == null) return;
        var profile = _profileService.ActiveProfile;
        if (profile == null) return;
        if (profile.PendingResumes.Any(p => p.SessionId == session.SessionId)) return;

        var now = DateTime.UtcNow;
        profile.PendingResumes.Add(new PendingResume
        {
            SessionId = session.SessionId,
            Cwd = session.Cwd,
            PermissionMode = session.PermissionMode,
            LimitHitAtUtc = now,
            SessionResetAtUtc = now.AddMinutes(1),
            IsEnabled = true
        });
        _profileService.UpdateProfile(profile);
        RefreshPendingResumes(profile);
    }

    /// <summary>Live toggle on an already-captured pending resume — takes effect immediately,
    /// no Save() needed, matching how the PENDING/HISTORY lists reflect live state rather than
    /// pending edits.</summary>
    [RelayCommand]
    private void ToggleResumeEnabled(PendingResume pending)
    {
        pending.IsEnabled = !pending.IsEnabled;
        var profile = _profileService.ActiveProfile;
        if (profile != null) _profileService.UpdateProfile(profile);
    }

    /// <summary>Permanently remove a queued resume — unlike the Auto-resume checkbox (which just
    /// skips it while keeping it visible), this deletes it outright.</summary>
    [RelayCommand]
    private void RemovePendingResume(PendingResume pending)
    {
        var profile = _profileService.ActiveProfile;
        if (profile == null) return;
        profile.PendingResumes.RemoveAll(p => p.SessionId == pending.SessionId);
        _profileService.UpdateProfile(profile);
        RefreshPendingResumes(profile);
    }

    /// <summary>Live edit of a pending item's reset time — takes effect immediately, no Save()
    /// needed. On success the next poll re-evaluates readiness against the new time; a bad
    /// parse leaves SessionResetAtUtc untouched and shows an inline error instead.</summary>
    [RelayCommand]
    private void SetResumeTime(PendingResume pending)
    {
        if (!DateTime.TryParse(pending.ResetTimeInputText, out var local))
        {
            pending.ResetTimeError = "Couldn't parse that as a date/time.";
        }
        else
        {
            pending.SessionResetAtUtc = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
            pending.ResetTimeError = null;
            var profile = _profileService.ActiveProfile;
            if (profile != null) _profileService.UpdateProfile(profile);
        }

        // PendingResume has no INotifyPropertyChanged, so force the ItemsControl to re-render —
        // same reasoning as why the read-only Resets/Attempts/Outcome text needed Mode=OneWay.
        var activeProfile = _profileService.ActiveProfile;
        if (activeProfile != null) RefreshPendingResumes(activeProfile);
    }

    [RelayCommand]
    private void Save()
    {
        var profile = _profileService.ActiveProfile;
        if (profile != null)
        {
            profile.ResumeCliSessionOnReset = ResumeCliSessionOnReset;
            profile.WatchdogMode = IsUnattendedMode ? WatchdogMode.Unattended : WatchdogMode.Interactive;
            profile.WatchdogAllowedProjects = AllowedProjects.ToList();
            _profileService.UpdateProfile(profile);
        }

        _initialResumeEnabled = ResumeCliSessionOnReset;
        _initialUnattended = IsUnattendedMode;
        _initialAllowedProjects = AllowedProjects.ToList();
        HasUnsavedChanges = false;
    }
}
