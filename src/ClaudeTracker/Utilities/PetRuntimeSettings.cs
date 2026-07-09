namespace ClaudeTracker.Utilities;

/// <summary>Mutable runtime values for desktop pets, user-adjustable via Settings.
/// Kept separate from the immutable Constants.Pets defaults so the walk timer and
/// state calculation can read live values without a settings-service dependency.</summary>
public static class PetRuntimeSettings
{
    public static double SpeedMultiplier { get; set; } = 1.0;
    public static double SleepThresholdMinutes { get; set; } = Constants.Pets.SleepThresholdMinutes;
}
