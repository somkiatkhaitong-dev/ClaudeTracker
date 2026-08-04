namespace ClaudeTracker.Models;

/// <summary>A desktop pet character skin. Single-image skins reuse the same path for all
/// three states and rely on EyeLeft/RightXFrac for the synthetic Sleeping eye overlay;
/// multi-pose skins point each state at dedicated artwork and need no eye overlay.
/// WalkFramePaths (played while Working), BlinkFramePaths (played periodically while
/// Idle, ordered open→half→closed→open), SleepFramePaths (slow breathing loop while
/// Sleeping), CelebrateFramePaths (one-shot jump when work finishes) and WaveFramePaths
/// (rare one-shot greeting during Idle) are optional frame animations. SitEnterFramePaths /
/// SitExitFramePaths are the one-shot stand↔sit transition played when entering/leaving
/// the Sitting state (idle too long); SitFramePaths is the breathing loop held while seated.
/// A skin may ship SitFramePaths alone with no transition art — the state still works, it
/// just cuts straight to the seated pose.</summary>
public record PetSkin(
    string Id,
    string WorkingImagePath,
    string IdleImagePath,
    string SleepingImagePath,
    double? EyeLeftXFrac = null,
    double? EyeLeftYFrac = null,
    double? EyeRightXFrac = null,
    double? EyeRightYFrac = null,
    string[]? WalkFramePaths = null,
    string[]? BlinkFramePaths = null,
    string[]? SleepFramePaths = null,
    string[]? CelebrateFramePaths = null,
    string[]? SitEnterFramePaths = null,
    string[]? SitFramePaths = null,
    string[]? SitExitFramePaths = null,
    string[]? WaveFramePaths = null,
    string? FamilyId = null,
    int Stage = 1,
    string? HeroIdleImagePath = null,
    string[]? HeroBlinkFramePaths = null,
    string[]? HeroWalkFramePaths = null,
    string[]? HeroPowerIdleFramePaths = null,
    string[]? HeroTransformFramePaths = null,
    string[]? HeroTransformBackFramePaths = null,
    string[]? HeroSitEnterFramePaths = null,
    string[]? HeroSitFramePaths = null,
    string[]? HeroSitExitFramePaths = null,
    string[]? HeroSleepFramePaths = null,
    string[]? HeroWaveFramePaths = null)
{
    public bool HasDedicatedPoses => SleepingImagePath != IdleImagePath;

    /// <summary>Character family for round-robin/pinning/disabling purposes. Defaults to
    /// Id so single-stage skins behave exactly as before evolution stages existed.</summary>
    public string EffectiveFamilyId => FamilyId ?? Id;

    /// <summary>Whether this skin has a click-to-toggle alternate "Hero" form. The
    /// Hero* frame sets above are each optional independently of the normal-mode
    /// ones — a skin can have hero idle/walk art but no hero sit/sleep/wave art yet.
    /// Unlike Blink/Walk/PowerIdle (which fall back to normal-mode art when hero art
    /// is missing, since those are small overlays or already-matching poses), Sit/
    /// Sleep/Wave never fall back: those poses show the whole body, and normal-mode
    /// art has no cape/crown, so a missing Hero* array means AgentPetControl falls
    /// through to the hero idle pose instead of a mismatched non-hero body.</summary>
    public bool HasHeroMode => HeroIdleImagePath != null;
}
