using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeTracker.Models;
using ClaudeTracker.Utilities;

namespace ClaudeTracker.ViewModels;

public enum PetState
{
    Working,
    Idle,
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
    [ObservableProperty] private string _skinId = "lumig";
    [ObservableProperty] private double _x;
    [ObservableProperty] private bool _facingRight = true;
    [ObservableProperty] private string _tooltipText = "";

    /// <summary>Per-pet walk speed variation (0.8–1.2) so pets don't move in lockstep.</summary>
    public double SpeedJitter { get; set; } = 1.0;

    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

    public AgentPetViewModel(string sessionId)
    {
        SessionId = sessionId;
    }

    public void UpdateFrom(SessionState session)
    {
        ProjectName = session.ProjectName;
        CurrentActivity = session.CurrentActivity;
        SubagentCount = session.ActiveSubagents.Count;
        LastActivityUtc = session.LastActivityTime;
        State = ComputeState(LastActivityUtc);
        TooltipText = string.IsNullOrEmpty(CurrentActivity)
            ? ProjectName
            : $"{ProjectName} — {CurrentActivity}";
    }

    public void RefreshState() => State = ComputeState(LastActivityUtc);

    public static PetState ComputeState(DateTime lastActivityUtc)
    {
        var idle = DateTime.UtcNow - lastActivityUtc;
        if (idle.TotalSeconds < Constants.Pets.WorkingThresholdSeconds) return PetState.Working;
        if (idle.TotalMinutes > Constants.Pets.SleepThresholdMinutes) return PetState.Sleeping;
        return PetState.Idle;
    }
}
