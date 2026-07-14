namespace ClaudeTracker.Models;

/// <summary>A desktop pet character skin. Single-image skins reuse the same path for all
/// three states and rely on EyeLeft/RightXFrac for the synthetic Sleeping eye overlay;
/// multi-pose skins point each state at dedicated artwork and need no eye overlay.
/// WalkFramePaths (played while Working), BlinkFramePaths (played periodically while
/// Idle, ordered open→half→closed→open), SleepFramePaths (slow breathing loop while
/// Sleeping) and CelebrateFramePaths (one-shot jump when work finishes) are optional
/// frame animations.</summary>
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
    string? FamilyId = null,
    int Stage = 1,
    string? HeroIdleImagePath = null,
    string[]? HeroBlinkFramePaths = null,
    string[]? HeroWalkFramePaths = null,
    string[]? HeroPowerIdleFramePaths = null,
    string[]? HeroTransformFramePaths = null,
    string[]? HeroTransformBackFramePaths = null)
{
    public bool HasDedicatedPoses => SleepingImagePath != IdleImagePath;

    /// <summary>Character family for round-robin/pinning/disabling purposes. Defaults to
    /// Id so single-stage skins behave exactly as before evolution stages existed.</summary>
    public string EffectiveFamilyId => FamilyId ?? Id;

    /// <summary>Whether this skin has a click-to-toggle alternate "Hero" form. Sleeping
    /// always renders normal art regardless of Hero mode, so there is no HeroSleepFramePaths.</summary>
    public bool HasHeroMode => HeroIdleImagePath != null;
}
