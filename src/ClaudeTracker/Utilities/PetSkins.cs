using System.Linq;
using ClaudeTracker.Models;

namespace ClaudeTracker.Utilities;

/// <summary>Registered desktop pet character skins. Add a character by dropping the
/// processed PNG(s) into Assets/, registering them as Resource items in the csproj,
/// and appending one entry here.
///
/// <c>angel_chick</c> is an evolving family: 4 stage entries share
/// <c>FamilyId: "angel_chick"</c> and are resolved via <see cref="ResolveStage"/> based on
/// live session usage % (see <see cref="PetEvolution"/>).
/// Asset filenames for stage 2 still say "stage2" for historical reasons (originally
/// generated as the 6-stage system's stage 4, kept as-is on renumber to avoid a needless
/// file rename).</summary>
public static class PetSkins
{
    private const string AngelChickFamily = "angel_chick";

    private static readonly PetSkin AngelChickStage1 = new(
        "angel_chick_stage1", "/Assets/pet_angel_chick_happy.png", "/Assets/pet_angel_chick_idle.png", "/Assets/pet_angel_chick_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_walk1.png", "/Assets/pet_angel_chick_walk2.png",
            "/Assets/pet_angel_chick_walk3.png", "/Assets/pet_angel_chick_walk4.png",
            "/Assets/pet_angel_chick_walk5.png", "/Assets/pet_angel_chick_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_blink1.png", "/Assets/pet_angel_chick_blink2.png",
            "/Assets/pet_angel_chick_blink3.png", "/Assets/pet_angel_chick_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/pet_angel_chick_sleep1.png", "/Assets/pet_angel_chick_sleep2.png",
            "/Assets/pet_angel_chick_sleep3.png", "/Assets/pet_angel_chick_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/pet_angel_chick_celebrate1.png", "/Assets/pet_angel_chick_celebrate2.png",
            "/Assets/pet_angel_chick_celebrate3.png", "/Assets/pet_angel_chick_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 1);

    private static readonly PetSkin AngelChickStage2 = new(
        "angel_chick_stage2", "/Assets/pet_angel_chick_stage2_happy.png", "/Assets/pet_angel_chick_stage2_idle.png", "/Assets/pet_angel_chick_stage2_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage2_walk1.png", "/Assets/pet_angel_chick_stage2_walk2.png",
            "/Assets/pet_angel_chick_stage2_walk3.png", "/Assets/pet_angel_chick_stage2_walk4.png",
            "/Assets/pet_angel_chick_stage2_walk5.png", "/Assets/pet_angel_chick_stage2_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage2_blink1.png", "/Assets/pet_angel_chick_stage2_blink2.png",
            "/Assets/pet_angel_chick_stage2_blink3.png", "/Assets/pet_angel_chick_stage2_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage2_sleep1.png", "/Assets/pet_angel_chick_stage2_sleep2.png",
            "/Assets/pet_angel_chick_stage2_sleep3.png", "/Assets/pet_angel_chick_stage2_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage2_celebrate1.png", "/Assets/pet_angel_chick_stage2_celebrate2.png",
            "/Assets/pet_angel_chick_stage2_celebrate3.png", "/Assets/pet_angel_chick_stage2_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 2);

    private static readonly PetSkin AngelChickStage3 = new(
        "angel_chick_stage3", "/Assets/pet_angel_chick_stage3_happy.png", "/Assets/pet_angel_chick_stage3_idle.png", "/Assets/pet_angel_chick_stage3_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage3_walk1.png", "/Assets/pet_angel_chick_stage3_walk2.png",
            "/Assets/pet_angel_chick_stage3_walk3.png", "/Assets/pet_angel_chick_stage3_walk4.png",
            "/Assets/pet_angel_chick_stage3_walk5.png", "/Assets/pet_angel_chick_stage3_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage3_blink1.png", "/Assets/pet_angel_chick_stage3_blink2.png",
            "/Assets/pet_angel_chick_stage3_blink3.png", "/Assets/pet_angel_chick_stage3_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage3_sleep1.png", "/Assets/pet_angel_chick_stage3_sleep2.png",
            "/Assets/pet_angel_chick_stage3_sleep3.png", "/Assets/pet_angel_chick_stage3_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage3_celebrate1.png", "/Assets/pet_angel_chick_stage3_celebrate2.png",
            "/Assets/pet_angel_chick_stage3_celebrate3.png", "/Assets/pet_angel_chick_stage3_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 3);

    private static readonly PetSkin AngelChickStage4 = new(
        "angel_chick_stage4", "/Assets/pet_angel_chick_stage4_happy.png", "/Assets/pet_angel_chick_stage4_idle.png", "/Assets/pet_angel_chick_stage4_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage4_walk1.png", "/Assets/pet_angel_chick_stage4_walk2.png",
            "/Assets/pet_angel_chick_stage4_walk3.png", "/Assets/pet_angel_chick_stage4_walk4.png",
            "/Assets/pet_angel_chick_stage4_walk5.png", "/Assets/pet_angel_chick_stage4_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage4_blink1.png", "/Assets/pet_angel_chick_stage4_blink2.png",
            "/Assets/pet_angel_chick_stage4_blink3.png", "/Assets/pet_angel_chick_stage4_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage4_sleep1.png", "/Assets/pet_angel_chick_stage4_sleep2.png",
            "/Assets/pet_angel_chick_stage4_sleep3.png", "/Assets/pet_angel_chick_stage4_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/pet_angel_chick_stage4_celebrate1.png", "/Assets/pet_angel_chick_stage4_celebrate2.png",
            "/Assets/pet_angel_chick_stage4_celebrate3.png", "/Assets/pet_angel_chick_stage4_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 4);

    /// <summary>Standalone skin — no evolution stages, no dedicated happy/sleep art
    /// (idle.png is reused for both the unused WorkingImagePath fallback and, via a
    /// duplicate file, SleepingImagePath so <c>HasDedicatedPoses</c> stays true and the
    /// walk/blink frame timers apply).</summary>
    private static readonly PetSkin Softdebut = new(
        "softdebut", "/Assets/pet_softdebut_idle.png", "/Assets/pet_softdebut_idle.png", "/Assets/pet_softdebut_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/pet_softdebut_walk1.png", "/Assets/pet_softdebut_walk2.png",
            "/Assets/pet_softdebut_walk3.png", "/Assets/pet_softdebut_walk4.png",
            "/Assets/pet_softdebut_walk5.png", "/Assets/pet_softdebut_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/pet_softdebut_blink1.png", "/Assets/pet_softdebut_blink2.png",
            "/Assets/pet_softdebut_blink3.png", "/Assets/pet_softdebut_blink4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/pet_softdebut_celebrate1.png", "/Assets/pet_softdebut_celebrate2.png",
            "/Assets/pet_softdebut_celebrate3.png", "/Assets/pet_softdebut_celebrate4.png",
        });

    public static readonly PetSkin[] All =
    {
        AngelChickStage1,
        AngelChickStage2,
        AngelChickStage3,
        AngelChickStage4,
        Softdebut,
    };

    /// <summary>Resolve a family + evolution stage to its PetSkin, falling back to the
    /// family's stage 1 (then the first registered skin) if the requested stage doesn't exist.
    /// Matches on <see cref="PetSkin.EffectiveFamilyId"/> (not the raw <c>FamilyId</c>) so
    /// standalone skins — which leave <c>FamilyId</c> null and fall back to their own <c>Id</c> —
    /// resolve correctly instead of silently falling through to <c>All[0]</c>.</summary>
    public static PetSkin ResolveStage(string familyId, int stage) =>
        All.FirstOrDefault(s => s.EffectiveFamilyId == familyId && s.Stage == stage)
        ?? All.FirstOrDefault(s => s.EffectiveFamilyId == familyId && s.Stage == 1)
        ?? All[0];
}
