using System.Text.Json.Serialization;

namespace ClaudeTracker.Models;

/// <summary>Root settings model persisted to settings.json in %APPDATA%/ClaudeTracker.</summary>
public class AppSettings
{
    [JsonPropertyName("profiles")]
    public List<Profile> Profiles { get; set; } = new();

    [JsonPropertyName("activeProfileId")]
    public Guid? ActiveProfileId { get; set; }

    [JsonPropertyName("appLanguage")]
    public string AppLanguage { get; set; } = "en";

    [JsonPropertyName("launchAtLogin")]
    public bool LaunchAtLogin { get; set; }

    [JsonPropertyName("multiProfileDisplayConfig")]
    public MultiProfileDisplayConfig MultiProfileDisplayConfig { get; set; } = new();

    [JsonPropertyName("displayMode")]
    public string DisplayMode { get; set; } = "single";

    [JsonPropertyName("firstLaunchDate")]
    public DateTime? FirstLaunchDate { get; set; }

    [JsonPropertyName("hasCompletedSetup")]
    public bool HasCompletedSetup { get; set; }

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "auto";

    [JsonPropertyName("isFloatingModeEnabled")]
    public bool IsFloatingModeEnabled { get; set; }

    [JsonPropertyName("floatingWindowLeft")]
    public double? FloatingWindowLeft { get; set; }

    [JsonPropertyName("floatingWindowTop")]
    public double? FloatingWindowTop { get; set; }

    [JsonPropertyName("isFloatingWidgetDocked")]
    public bool IsFloatingWidgetDocked { get; set; }

    [JsonPropertyName("agentPetsEnabled")]
    public bool AgentPetsEnabled { get; set; } = true;

    [JsonPropertyName("agentPetsWindowLeft")]
    public double? AgentPetsWindowLeft { get; set; }

    [JsonPropertyName("agentPetsWindowTop")]
    public double? AgentPetsWindowTop { get; set; }

    [JsonPropertyName("disabledPetSkins")]
    public List<string> DisabledPetSkins { get; set; } = new();

    /// <summary>Hero-capable pet skin family ids (<see cref="PetSkin.EffectiveFamilyId"/>)
    /// the user has already discovered hero mode for — the click-to-transform hint stops
    /// showing once a family id is in this list. One-way ratchet, never cleared.</summary>
    [JsonPropertyName("seenHeroModeSkins")]
    public List<string> SeenHeroModeSkins { get; set; } = new();

    [JsonPropertyName("petSpeedMultiplier")]
    public double PetSpeedMultiplier { get; set; } = 1.0;

    [JsonPropertyName("petSleepThresholdMinutes")]
    public double PetSleepThresholdMinutes { get; set; } = 2.0;

    /// <summary>Project folder (session Cwd) → pet skin id. Pins a character to a project
    /// so it's consistent across restarts instead of round-robin cycling.</summary>
    [JsonPropertyName("projectSkinAssignments")]
    public Dictionary<string, string> ProjectSkinAssignments { get; set; } = new();

    /// <summary>Project folder (session Cwd) → last dragged-to screen position, so a pet
    /// reappears where the user left it across app restarts.</summary>
    [JsonPropertyName("projectPetPositions")]
    public Dictionary<string, PetPosition> ProjectPetPositions { get; set; } = new();

    [JsonPropertyName("hasStarredGitHub")]
    public bool HasStarredGitHub { get; set; }

    [JsonPropertyName("lastStarPromptDate")]
    public DateTime? LastStarPromptDate { get; set; }

    [JsonPropertyName("starPromptDismissedForever")]
    public bool StarPromptDismissedForever { get; set; }

    [JsonPropertyName("hasSentFeedback")]
    public bool HasSentFeedback { get; set; }

    [JsonPropertyName("lastFeedbackPromptDate")]
    public DateTime? LastFeedbackPromptDate { get; set; }

    [JsonPropertyName("feedbackPromptDismissedForever")]
    public bool FeedbackPromptDismissedForever { get; set; }

    [JsonPropertyName("feedbackRating")]
    public int? FeedbackRating { get; set; }

    [JsonPropertyName("popoverTimeDisplay")]
    public PopoverTimeDisplay PopoverTimeDisplay { get; set; } = PopoverTimeDisplay.RemainingTime;

    [JsonPropertyName("timeFormatPreference")]
    public TimeFormatPreference TimeFormatPreference { get; set; } = TimeFormatPreference.System;

    // ── Hooks Integration ──

    [JsonPropertyName("hooksOnboardingSeen")]
    public bool HooksOnboardingSeen { get; set; }

    [JsonPropertyName("hooksOnboardingDismissed")]
    public bool HooksOnboardingDismissed { get; set; }

    [JsonPropertyName("hookPopupPosition")]
    public string HookPopupPosition { get; set; } = "BottomRight";

    [JsonPropertyName("hookPopupMonitor")]
    public int HookPopupMonitor { get; set; } = -1;

    [JsonPropertyName("hooksEnabled")]
    public bool HooksEnabled { get; set; }

    [JsonPropertyName("hookPermissionPopupsEnabled")]
    public bool HookPermissionPopupsEnabled { get; set; } = true;

    [JsonPropertyName("hookElicitationPopupsEnabled")]
    public bool HookElicitationPopupsEnabled { get; set; } = true;

    [JsonPropertyName("hookActivityFeedEnabled")]
    public bool HookActivityFeedEnabled { get; set; } = true;

    [JsonPropertyName("hookMaxFeedEntries")]
    public int HookMaxFeedEntries { get; set; } = 10;

    [JsonPropertyName("hookNotificationPreferences")]
    public Dictionary<string, bool> HookNotificationPreferences { get; set; } = new()
    {
        ["stop"] = true,
        ["toolError"] = true,
        ["permission"] = true,
        ["idle"] = true,
        ["configChange"] = false,
        ["sessionLifecycle"] = false,
        ["subagent"] = false
    };
}

/// <summary>Screen coordinates (WPF device-independent units, relative to the primary
/// monitor's work area) where a desktop pet was last dragged to.</summary>
public record PetPosition(double X, double Y);
