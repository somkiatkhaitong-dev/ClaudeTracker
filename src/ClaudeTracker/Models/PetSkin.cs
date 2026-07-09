namespace ClaudeTracker.Models;

/// <summary>A desktop pet character skin. Single-image skins reuse the same path for all
/// three states and rely on EyeLeft/RightXFrac for the synthetic Sleeping eye overlay;
/// multi-pose skins point each state at dedicated artwork and need no eye overlay.</summary>
public record PetSkin(
    string Id,
    string WorkingImagePath,
    string IdleImagePath,
    string SleepingImagePath,
    double? EyeLeftXFrac = null,
    double? EyeLeftYFrac = null,
    double? EyeRightXFrac = null,
    double? EyeRightYFrac = null)
{
    public bool HasDedicatedPoses => SleepingImagePath != IdleImagePath;
}
