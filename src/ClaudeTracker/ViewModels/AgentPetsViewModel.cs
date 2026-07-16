using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeTracker.Models;
using ClaudeTracker.Services.Interfaces;
using ClaudeTracker.Utilities;

namespace ClaudeTracker.ViewModels;

/// <summary>Maintains one pet per active Claude Code session, diffing by SessionId
/// so walk position and facing survive session updates.</summary>
public partial class AgentPetsViewModel : ObservableObject, IDisposable
{
    private readonly ISessionTrackingService _sessionTracking;
    private readonly ISettingsService _settingsService;
    private readonly IProfileService _profileService;
    private readonly IUsageRefreshCoordinator _refreshCoordinator;
    private readonly Random _random = new();
    private int _nextSkinIndex;

    public ObservableCollection<AgentPetViewModel> Pets { get; } = new();

    [ObservableProperty] private bool _hasPets;

    public event EventHandler? PetsCountChanged;

    public AgentPetsViewModel(
        ISessionTrackingService sessionTracking,
        ISettingsService settingsService,
        IProfileService profileService,
        IUsageRefreshCoordinator refreshCoordinator)
    {
        _sessionTracking = sessionTracking;
        _settingsService = settingsService;
        _profileService = profileService;
        _refreshCoordinator = refreshCoordinator;
        _sessionTracking.SessionsChanged += (_, _) => SyncPets();
        _settingsService.SettingsChanged += (_, _) =>
        {
            RefreshRuntimeSettings();
            RefreshHeroHints();
        };
        _refreshCoordinator.RefreshCompleted += OnUsageUpdated;
        _profileService.ActiveProfileChanged += OnActiveProfileChanged;
        RefreshRuntimeSettings();
        SyncPets();
    }

    private void OnUsageUpdated(object? sender, EventArgs e) => RefreshEvolutionStages();
    private void OnActiveProfileChanged(object? sender, Profile? profile) => RefreshEvolutionStages();

    /// <summary>Recompute every pet's evolution stage from the current Session usage %.
    /// Session % is per-PROFILE while pets are per-SESSION/project — since there's no
    /// session-to-profile mapping today, all pets evolve together off the single active
    /// profile's usage. Accepted simplification.</summary>
    private void RefreshEvolutionStages()
    {
        var sessionPct = _profileService.ActiveProfile?.ClaudeUsage?.EffectiveSessionPercentage ?? 0.0;
        var stage = PetEvolution.StageForPercentage(sessionPct);
        foreach (var pet in Pets)
            pet.EvolutionStage = stage;
    }

    public void Dispose()
    {
        _refreshCoordinator.RefreshCompleted -= OnUsageUpdated;
        _profileService.ActiveProfileChanged -= OnActiveProfileChanged;
    }

    private void RefreshRuntimeSettings()
    {
        var settings = _settingsService.Settings;
        PetRuntimeSettings.SpeedMultiplier = settings.PetSpeedMultiplier;
        PetRuntimeSettings.SleepThresholdMinutes = settings.PetSleepThresholdMinutes;
    }

    /// <summary>Shows the hero-mode discoverability hint on any pet whose skin has hero
    /// mode and hasn't been discovered yet (<see cref="AppSettings.SeenHeroModeSkins"/>).
    /// Re-run whenever settings change so triggering hero mode on one pet clears the hint
    /// on every other pet sharing that skin family in the same tick.</summary>
    private void RefreshHeroHints()
    {
        var seen = _settingsService.Settings.SeenHeroModeSkins;
        foreach (var pet in Pets)
        {
            var skin = PetSkins.ResolveStage(pet.SkinId, pet.EvolutionStage);
            pet.ShowHeroHint = skin.HasHeroMode && !pet.IsHeroMode && !seen.Contains(skin.EffectiveFamilyId);
        }
    }

    private string ResolveSkinId(string cwd, PetSkin[] enabledFamilies)
    {
        if (!string.IsNullOrEmpty(cwd) &&
            _settingsService.Settings.ProjectSkinAssignments.TryGetValue(cwd, out var assignedId) &&
            enabledFamilies.Any(s => s.EffectiveFamilyId == assignedId))
        {
            return assignedId;
        }

        return enabledFamilies[_nextSkinIndex++ % enabledFamilies.Length].EffectiveFamilyId;
    }

    /// <summary>One representative (stage 1) PetSkin per character family — evolution
    /// stages of the same family (e.g. angel_chick_stage1..6) are never treated as
    /// separate characters for round-robin/pinning/disabling purposes.</summary>
    private PetSkin[] EnabledSkins()
    {
        var disabled = _settingsService.Settings.DisabledPetSkins;
        var families = PetSkins.All
            .GroupBy(s => s.EffectiveFamilyId)
            .Select(g => g.OrderBy(s => s.Stage).First())
            .ToArray();
        var enabled = families.Where(s => !disabled.Contains(s.EffectiveFamilyId)).ToArray();
        return enabled.Length > 0 ? enabled : families;
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

        var skins = EnabledSkins();
        var workArea = SystemParameters.WorkArea;
        foreach (var session in sessions)
        {
            var pet = Pets.FirstOrDefault(p => p.SessionId == session.SessionId);
            if (pet == null)
            {
                var (defaultX, defaultY) = DefaultSpawnPosition(workArea);
                PetPosition? saved = null;
                var hasSaved = !string.IsNullOrEmpty(session.Cwd) &&
                    _settingsService.Settings.ProjectPetPositions.TryGetValue(session.Cwd, out saved);
                pet = new AgentPetViewModel(session.SessionId)
                {
                    SkinId = ResolveSkinId(session.Cwd, skins),
                    X = hasSaved ? saved!.X : defaultX,
                    Y = hasSaved ? saved!.Y : defaultY,
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
        RefreshEvolutionStages();
        RefreshHeroHints();
    }

    /// <summary>Recompute pet states from cached activity times — Working→Idle→Sleeping
    /// transitions happen silently with no hook event to trigger SyncPets.</summary>
    public void RefreshStates()
    {
        foreach (var pet in Pets)
            pet.RefreshState();
    }

    private (double X, double Y) DefaultSpawnPosition(Rect workArea) => (
        _random.NextDouble() * (workArea.Width - Constants.Pets.PetWidth),
        workArea.Height - Constants.Pets.PetHeight - 14);

    /// <summary>Persists a pet's dragged-to position keyed by its project, so it reappears
    /// there across restarts. No-op for pets with no known Cwd (mirrors ResolveSkinId).</summary>
    public void SavePetPosition(AgentPetViewModel pet)
    {
        if (string.IsNullOrEmpty(pet.Cwd)) return;
        _settingsService.Settings.ProjectPetPositions[pet.Cwd] = new PetPosition(pet.X, pet.Y);
        _settingsService.Save();
    }

    /// <summary>Clears all saved drag positions and snaps active pets back to their
    /// default spawn spot along the bottom of the screen.</summary>
    public void ResetAllPositions()
    {
        _settingsService.Settings.ProjectPetPositions.Clear();
        _settingsService.Save();

        var workArea = SystemParameters.WorkArea;
        foreach (var pet in Pets)
        {
            var (x, y) = DefaultSpawnPosition(workArea);
            pet.X = x;
            pet.Y = y;
        }
    }
}
