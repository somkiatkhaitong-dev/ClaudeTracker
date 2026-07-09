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
        new("angel_chick", "/Assets/pet_angel_chick_happy.png", "/Assets/pet_angel_chick_idle.png", "/Assets/pet_angel_chick_sleep.png"),
        new("angel_knight", "/Assets/pet_angel_knight_happy.png", "/Assets/pet_angel_knight_idle.png", "/Assets/pet_angel_knight_sleep.png"),
        new("crystal_golem", "/Assets/pet_crystal_golem_happy.png", "/Assets/pet_crystal_golem_idle.png", "/Assets/pet_crystal_golem_sleep.png"),
        new("leaf_fairy", "/Assets/pet_leaf_fairy_happy.png", "/Assets/pet_leaf_fairy_idle.png", "/Assets/pet_leaf_fairy_sleep.png"),
        new("space_mage", "/Assets/pet_space_mage_happy.png", "/Assets/pet_space_mage_idle.png", "/Assets/pet_space_mage_sleep.png"),
        new("fire_engineer", "/Assets/pet_fire_engineer_happy.png", "/Assets/pet_fire_engineer_idle.png", "/Assets/pet_fire_engineer_sleep.png"),
        new("ice_princess", "/Assets/pet_ice_princess_happy.png", "/Assets/pet_ice_princess_idle.png", "/Assets/pet_ice_princess_sleep.png"),
        new("star_angel", "/Assets/pet_star_angel_happy.png", "/Assets/pet_star_angel_idle.png", "/Assets/pet_star_angel_sleep.png"),
    };
}
