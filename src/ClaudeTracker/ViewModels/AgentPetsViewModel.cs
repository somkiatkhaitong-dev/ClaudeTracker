using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeTracker.Services.Interfaces;
using ClaudeTracker.Utilities;

namespace ClaudeTracker.ViewModels;

/// <summary>Maintains one pet per active Claude Code session, diffing by SessionId
/// so walk position and facing survive session updates.</summary>
public partial class AgentPetsViewModel : ObservableObject
{
    private static readonly string[] AccentCycle =
    {
        "AccentBlue", "AccentMagenta", "AccentTeal", "AccentAmber",
        "AccentCyan", "AccentGreen", "AccentPurple"
    };

    private readonly ISessionTrackingService _sessionTracking;
    private readonly Random _random = new();
    private int _nextColorIndex;

    public ObservableCollection<AgentPetViewModel> Pets { get; } = new();

    [ObservableProperty] private bool _hasPets;

    public event EventHandler? PetsCountChanged;

    public AgentPetsViewModel(ISessionTrackingService sessionTracking)
    {
        _sessionTracking = sessionTracking;
        _sessionTracking.SessionsChanged += (_, _) => SyncPets();
        SyncPets();
    }

    /// <summary>Diff Pets against ActiveSessions by SessionId. Runs on the UI thread
    /// (SessionsChanged is always raised inside Dispatcher.Invoke).</summary>
    private void SyncPets()
    {
        var sessions = _sessionTracking.ActiveSessions;
        var previousCount = Pets.Count;

        for (int i = Pets.Count - 1; i >= 0; i--)
        {
            if (sessions.All(s => s.SessionId != Pets[i].SessionId))
                Pets.RemoveAt(i);
        }

        foreach (var session in sessions)
        {
            var pet = Pets.FirstOrDefault(p => p.SessionId == session.SessionId);
            if (pet == null)
            {
                pet = new AgentPetViewModel(session.SessionId)
                {
                    ColorToken = AccentCycle[_nextColorIndex++ % AccentCycle.Length],
                    X = _random.NextDouble() * (Constants.Pets.WindowWidth - Constants.Pets.PetWidth),
                    FacingRight = _random.Next(2) == 0,
                    SpeedJitter = 0.8 + _random.NextDouble() * 0.4
                };
                Pets.Add(pet);
            }
            pet.UpdateFrom(session);
        }

        HasPets = Pets.Count > 0;
        if (Pets.Count != previousCount)
            PetsCountChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Recompute pet states from cached activity times — Working→Idle→Sleeping
    /// transitions happen silently with no hook event to trigger SyncPets.</summary>
    public void RefreshStates()
    {
        foreach (var pet in Pets)
            pet.RefreshState();
    }
}
