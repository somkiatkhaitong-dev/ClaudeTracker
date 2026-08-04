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
        "angel_chick_stage1", "/Assets/angel_chick/stage1/pet_angel_chick_happy.png", "/Assets/angel_chick/stage1/pet_angel_chick_idle.png", "/Assets/angel_chick/stage1/pet_angel_chick_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/angel_chick/stage1/pet_angel_chick_walk1.png", "/Assets/angel_chick/stage1/pet_angel_chick_walk2.png",
            "/Assets/angel_chick/stage1/pet_angel_chick_walk3.png", "/Assets/angel_chick/stage1/pet_angel_chick_walk4.png",
            "/Assets/angel_chick/stage1/pet_angel_chick_walk5.png", "/Assets/angel_chick/stage1/pet_angel_chick_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/angel_chick/stage1/pet_angel_chick_blink1.png", "/Assets/angel_chick/stage1/pet_angel_chick_blink2.png",
            "/Assets/angel_chick/stage1/pet_angel_chick_blink3.png", "/Assets/angel_chick/stage1/pet_angel_chick_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/angel_chick/stage1/pet_angel_chick_sleep1.png", "/Assets/angel_chick/stage1/pet_angel_chick_sleep2.png",
            "/Assets/angel_chick/stage1/pet_angel_chick_sleep3.png", "/Assets/angel_chick/stage1/pet_angel_chick_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/angel_chick/stage1/pet_angel_chick_celebrate1.png", "/Assets/angel_chick/stage1/pet_angel_chick_celebrate2.png",
            "/Assets/angel_chick/stage1/pet_angel_chick_celebrate3.png", "/Assets/angel_chick/stage1/pet_angel_chick_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 1);

    private static readonly PetSkin AngelChickStage2 = new(
        "angel_chick_stage2", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_happy.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_idle.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_walk1.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_walk2.png",
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_walk3.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_walk4.png",
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_walk5.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_blink1.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_blink2.png",
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_blink3.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_sleep1.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_sleep2.png",
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_sleep3.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_celebrate1.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_celebrate2.png",
            "/Assets/angel_chick/stage2/pet_angel_chick_stage2_celebrate3.png", "/Assets/angel_chick/stage2/pet_angel_chick_stage2_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 2);

    private static readonly PetSkin AngelChickStage3 = new(
        "angel_chick_stage3", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_happy.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_idle.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_walk1.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_walk2.png",
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_walk3.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_walk4.png",
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_walk5.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_blink1.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_blink2.png",
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_blink3.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_sleep1.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_sleep2.png",
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_sleep3.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_celebrate1.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_celebrate2.png",
            "/Assets/angel_chick/stage3/pet_angel_chick_stage3_celebrate3.png", "/Assets/angel_chick/stage3/pet_angel_chick_stage3_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 3);

    private static readonly PetSkin AngelChickStage4 = new(
        "angel_chick_stage4", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_happy.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_idle.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_walk1.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_walk2.png",
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_walk3.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_walk4.png",
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_walk5.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_blink1.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_blink2.png",
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_blink3.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_blink4.png",
        },
        SleepFramePaths: new[]
        {
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_sleep1.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_sleep2.png",
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_sleep3.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_sleep4.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_celebrate1.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_celebrate2.png",
            "/Assets/angel_chick/stage4/pet_angel_chick_stage4_celebrate3.png", "/Assets/angel_chick/stage4/pet_angel_chick_stage4_celebrate4.png",
        },
        FamilyId: AngelChickFamily, Stage: 4);

    /// <summary>Standalone skin — no evolution stages, no dedicated happy/sleep art
    /// (idle.png is reused for both the unused WorkingImagePath fallback and, via a
    /// duplicate file, SleepingImagePath so <c>HasDedicatedPoses</c> stays true and the
    /// walk/blink frame timers apply).</summary>
    private static readonly PetSkin Softdebut = new(
        "softdebut", "/Assets/softdebut/normal/pet_softdebut_idle.png", "/Assets/softdebut/normal/pet_softdebut_idle.png", "/Assets/softdebut/normal/pet_softdebut_sleep.png",
        WalkFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_walk1.png", "/Assets/softdebut/normal/pet_softdebut_walk2.png",
            "/Assets/softdebut/normal/pet_softdebut_walk3.png", "/Assets/softdebut/normal/pet_softdebut_walk4.png",
            "/Assets/softdebut/normal/pet_softdebut_walk5.png", "/Assets/softdebut/normal/pet_softdebut_walk6.png",
        },
        BlinkFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_blink1.png", "/Assets/softdebut/normal/pet_softdebut_blink2.png",
            "/Assets/softdebut/normal/pet_softdebut_blink3.png", "/Assets/softdebut/normal/pet_softdebut_blink4.png",
        },
        // 8-frame gentle-breathing loop (lying-down pose, cropped from a separate reference
        // sheet than the standing poses above). Background removed with rembg model
        // u2net — isnet-anime produced a globally washed-out alpha (~150/255 average
        // instead of ~240/255) on this specific source image, visible as a faded pet.
        SleepFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_sleep1.png", "/Assets/softdebut/normal/pet_softdebut_sleep2.png",
            "/Assets/softdebut/normal/pet_softdebut_sleep3.png", "/Assets/softdebut/normal/pet_softdebut_sleep4.png",
            "/Assets/softdebut/normal/pet_softdebut_sleep5.png", "/Assets/softdebut/normal/pet_softdebut_sleep6.png",
            "/Assets/softdebut/normal/pet_softdebut_sleep7.png", "/Assets/softdebut/normal/pet_softdebut_sleep8.png",
        },
        CelebrateFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_celebrate1.png", "/Assets/softdebut/normal/pet_softdebut_celebrate2.png",
            "/Assets/softdebut/normal/pet_softdebut_celebrate3.png", "/Assets/softdebut/normal/pet_softdebut_celebrate4.png",
        },
        // Sit-down/stand-up transition (stand→crouch→sit, played once when idle crosses
        // Constants.Pets.SittingThresholdMinutes) plus a 4-frame breathing loop held while
        // seated. All three share one bottom-anchored canvas cropped from the same
        // reference sheet so the head sinks down naturally with no jitter across the cut.
        SitEnterFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_sit_stand.png", "/Assets/softdebut/normal/pet_softdebut_sit_crouch.png",
            "/Assets/softdebut/normal/pet_softdebut_sit1.png",
        },
        SitFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_sit1.png", "/Assets/softdebut/normal/pet_softdebut_sit2.png",
            "/Assets/softdebut/normal/pet_softdebut_sit3.png", "/Assets/softdebut/normal/pet_softdebut_sit4.png",
        },
        SitExitFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_sit_crouch.png", "/Assets/softdebut/normal/pet_softdebut_sit_stand.png",
        },
        // Rare one-shot greeting during Idle (~every 30-90s) — alternates right arm then
        // left arm, neutral pose at both ends so it cuts cleanly back to idle art.
        WaveFramePaths: new[]
        {
            "/Assets/softdebut/normal/pet_softdebut_wave1.png", "/Assets/softdebut/normal/pet_softdebut_wave2.png",
            "/Assets/softdebut/normal/pet_softdebut_wave3.png", "/Assets/softdebut/normal/pet_softdebut_wave4.png",
            "/Assets/softdebut/normal/pet_softdebut_wave5.png", "/Assets/softdebut/normal/pet_softdebut_wave6.png",
            "/Assets/softdebut/normal/pet_softdebut_wave7.png", "/Assets/softdebut/normal/pet_softdebut_wave8.png",
        },
        HeroIdleImagePath: "/Assets/softdebut/hero/pet_softdebut_hero_idle.png",
        HeroWalkFramePaths: new[]
        {
            "/Assets/softdebut/hero/pet_softdebut_hero_walk1.png", "/Assets/softdebut/hero/pet_softdebut_hero_walk2.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_walk3.png", "/Assets/softdebut/hero/pet_softdebut_hero_walk4.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_walk5.png", "/Assets/softdebut/hero/pet_softdebut_hero_walk6.png",
        },
        // Regenerated 2026-07-14: the original 8-frame set had a baked-in art defect
        // (crown mounting post didn't blend into the head shell — visible seam/gap) and
        // an inconsistent crown position across frames. This 4-frame set was generated
        // fresh with the crown/head frozen in place and only the cape animating, then
        // background-removed, height-normalized to hero_idle.png (493px), and crown-
        // aligned so there's zero head movement — only the cape flows. Seamless loop:
        // frame 4 flows back into frame 1.
        HeroPowerIdleFramePaths: new[]
        {
            "/Assets/softdebut/hero/pet_softdebut_hero_power_idle_gen1.png", "/Assets/softdebut/hero/pet_softdebut_hero_power_idle_gen2.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_power_idle_gen3.png", "/Assets/softdebut/hero/pet_softdebut_hero_power_idle_gen4.png",
        },
        HeroTransformFramePaths: new[]
        {
            "/Assets/softdebut/hero/pet_softdebut_hero_transform1.png", "/Assets/softdebut/hero/pet_softdebut_hero_transform2.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_transform3.png", "/Assets/softdebut/hero/pet_softdebut_hero_transform4.png",
        },
        HeroTransformBackFramePaths: new[]
        {
            "/Assets/softdebut/hero/pet_softdebut_hero_transform_back1.png", "/Assets/softdebut/hero/pet_softdebut_hero_transform_back2.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_transform_back3.png", "/Assets/softdebut/hero/pet_softdebut_hero_transform_back4.png",
        },
        // Hero-costume sit/sleep/wave — same idea as the normal-mode set above (a seated
        // breathing loop, a lying-down breathing loop, and a rare idle wave) but with the
        // cape and crown carried through every frame so Hero mode never has to fall back
        // to a non-hero body for these poses. No HeroSitEnter/ExitFramePaths yet — only
        // the seated pose itself was generated, no stand-crouch-sit transition art, so
        // this cuts straight to the seated loop per PetSkin's "ship SitFramePaths alone"
        // fallback.
        // 8-frame procedural breathing loop (desktop-pet-animation skill's "breathe" preset
        // over one clean seated-hero pose, 8 FPS) — plays faster than the 4-frame normal-mode
        // sit loop, see HeroSitLoopTicksPerFrame in AgentPetControl.
        HeroSitFramePaths: new[]
        {
            "/Assets/softdebut/hero/pet_softdebut_hero_sit1.png", "/Assets/softdebut/hero/pet_softdebut_hero_sit2.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_sit3.png", "/Assets/softdebut/hero/pet_softdebut_hero_sit4.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_sit5.png", "/Assets/softdebut/hero/pet_softdebut_hero_sit6.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_sit7.png", "/Assets/softdebut/hero/pet_softdebut_hero_sit8.png",
        },
        HeroSleepFramePaths: new[]
        {
            "/Assets/softdebut/hero/pet_softdebut_hero_sleep1.png", "/Assets/softdebut/hero/pet_softdebut_hero_sleep2.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_sleep3.png", "/Assets/softdebut/hero/pet_softdebut_hero_sleep4.png",
        },
        HeroWaveFramePaths: new[]
        {
            "/Assets/softdebut/hero/pet_softdebut_hero_wave1.png", "/Assets/softdebut/hero/pet_softdebut_hero_wave2.png",
            "/Assets/softdebut/hero/pet_softdebut_hero_wave3.png", "/Assets/softdebut/hero/pet_softdebut_hero_wave4.png",
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
