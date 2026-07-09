using ClaudeTracker.Models;

namespace ClaudeTracker.Utilities;

/// <summary>Registered desktop pet character skins. Add a character by dropping the
/// processed PNG(s) into Assets/, registering them as Resource items in the csproj,
/// and appending one entry here.</summary>
public static class PetSkins
{
    public static readonly PetSkin[] All =
    {
        new("lumig", "/Assets/pet_lumig.png", "/Assets/pet_lumig.png", "/Assets/pet_lumig.png",
            EyeLeftXFrac: 0.47, EyeLeftYFrac: 0.538,
            EyeRightXFrac: 0.6725, EyeRightYFrac: 0.5087),
        new("bee", "/Assets/pet_bee_working.png", "/Assets/pet_bee_idle.png", "/Assets/pet_bee_sleeping.png"),
    };
}
