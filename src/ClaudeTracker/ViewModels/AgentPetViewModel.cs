using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeTracker.Models;
using ClaudeTracker.Utilities;

namespace ClaudeTracker.ViewModels;

public enum PetState
{
    Working,
    Idle,
    Sitting,
    Sleeping
}

/// <summary>One walking pet character bound to an active Claude Code session.</summary>
public partial class AgentPetViewModel : ObservableObject
{
    public string SessionId { get; }

    [ObservableProperty] private PetState _state = PetState.Idle;
    [ObservableProperty] private string _projectName = "";
    [ObservableProperty] private string _currentActivity = "";
    [ObservableProperty] private int _subagentCount;
    [ObservableProperty] private string _skinId = "angel_chick";
    [ObservableProperty] private int _evolutionStage = PetEvolution.MinStage;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private bool _facingRight = true;
    [ObservableProperty] private string _tooltipText = "";

    /// <summary>Click-to-toggle alternate form, orthogonal to <see cref="PetState"/> — a
    /// hero pet can still be Working/Idle/Sleeping, just rendered with hero art instead of
    /// normal art. Never persisted; always resets to false on relaunch.</summary>
    [ObservableProperty] private bool _isHeroMode;

    /// <summary>Externally driven by <c>AgentPetsViewModel.RefreshHeroHints</c> — true while
    /// this pet's skin has hero mode but the user hasn't discovered it yet (tracked in
    /// <see cref="Models.AppSettings.SeenHeroModeSkins"/>). Drives a small discoverability
    /// badge + tooltip hint; not itself persisted.</summary>
    [ObservableProperty] private bool _showHeroHint;

    /// <summary>True while the user is actively dragging this pet — <c>WalkTick</c> skips
    /// it so the 33ms walk timer doesn't fight the live drag.</summary>
    public bool IsDragging { get; set; }

    /// <summary>Session working directory, used to key <see cref="PetPosition"/>
    /// persistence — durable across restarts, unlike <see cref="SessionId"/>.</summary>
    public string Cwd { get; private set; } = "";

    /// <summary>Per-pet walk speed variation (0.8–1.2) so pets don't move in lockstep.</summary>
    public double SpeedJitter { get; set; } = 1.0;

    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

    public AgentPetViewModel(string sessionId)
    {
        SessionId = sessionId;
    }

    public void UpdateFrom(SessionState session)
    {
        Cwd = session.Cwd;
        ProjectName = session.ProjectName;
        CurrentActivity = session.CurrentActivity;
        SubagentCount = session.ActiveSubagents.Count;
        LastActivityUtc = session.LastActivityTime;
        State = ComputeState(LastActivityUtc);
        RefreshTooltip();
    }

    public void RefreshState() => State = ComputeState(LastActivityUtc);

    private void RefreshTooltip()
    {
        var baseText = string.IsNullOrEmpty(CurrentActivity)
            ? ProjectName
            : $"{ProjectName} — {CurrentActivity}";
        TooltipText = ShowHeroHint ? $"{baseText} ✨ click me!" : baseText;
    }

    partial void OnShowHeroHintChanged(bool value) => RefreshTooltip();
    partial void OnIsHeroModeChanged(bool value) => RefreshTooltip();

    public static PetState ComputeState(DateTime lastActivityUtc)
    {
        var idle = DateTime.UtcNow - lastActivityUtc;
        if (idle.TotalSeconds < Constants.Pets.WorkingThresholdSeconds) return PetState.Working;
        if (idle.TotalMinutes > PetRuntimeSettings.SleepThresholdMinutes) return PetState.Sleeping;
        if (idle.TotalMinutes > Constants.Pets.SittingThresholdMinutes) return PetState.Sitting;
        return PetState.Idle;
    }
}
