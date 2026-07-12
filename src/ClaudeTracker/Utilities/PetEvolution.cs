namespace ClaudeTracker.Utilities;

/// <summary>Maps the live Claude Session usage % to a 1-4 pet evolution stage.</summary>
public static class PetEvolution
{
    public const int MinStage = 1;
    public const int MaxStage = 4;

    /// <summary>Upper bound (inclusive) of session % for each stage, ordered 1→4.
    /// Anything above the last bound (including 100%) resolves to MaxStage.</summary>
    private static readonly double[] StageUpperBounds = { 25, 50, 75, 100 };

    public static int StageForPercentage(double sessionPercentage)
    {
        for (var i = 0; i < StageUpperBounds.Length; i++)
        {
            if (sessionPercentage <= StageUpperBounds[i])
                return i + 1;
        }
        return MaxStage;
    }
}
