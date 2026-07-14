# Task: Hero mode for the Softdebut pet (click-to-toggle alternate form)

Follow the existing `pet-sprite` skill workflow for the art half of this task
(same pipeline used for idle/blink/walk/celebrate — see
`2026-07-12-softdebut-pet-sprite-task.md` for the locked character
description and the established gotchas). The app-code half is new: this is
the first pet feature that isn't just "swap in more frames for an existing
state" — it adds a new orthogonal toggle (`IsHeroMode`) and a click
interaction, on top of the existing Working/Idle/Sleeping state machine.

## 0. Decide before doing anything

- **Scope**: Softdebut only. `angel_chick` gets no hero art in this phase —
  the mechanism is skin-agnostic (`PetSkin.HasHeroMode`), so nothing breaks
  for skins that don't ship hero fields, but no hero content is being
  generated for `angel_chick` now.
- **Trigger**: click directly on the pet toggles Hero mode on; click again
  toggles it off. No other UI (no button, no menu entry, no auto-revert
  timer) in this phase.
- **Persistence**: Hero mode does **not** persist across app restarts —
  always resets to off. Same bucket as `EvolutionStage` (a derived/session-
  local presentation flag, recomputed/reset live, not a durable per-project
  preference like `ProjectSkinAssignments`/`ProjectPetPositions`).
- **Art concept — LOCKED**: the cape flows/billows strongly as if caught in
  a strong wind, in every Hero pose (idle, blink, walk, power_idle,
  transform, transform_back). No other visual change (same eye glow, same
  badge, same color palette) — cape motion alone is what reads as "Hero" for
  this character. State this explicitly in every Hero generation prompt.
- **Reference note**: an external "desktop pet pack"
  (`C:\Users\Somkait\Downloads\softdebut_desktop_pet_pack_v1\`) has a
  `hero/` folder with `idle`/`blink`/`walk`/`power_idle`/`transform_back`
  taxonomy — useful only as *naming inspiration*, not as source art. Its
  actual images are confirmed low quality (duplicate-pose walk cycle, wrong
  antenna style, several states are byte-identical duplicates of others) and
  must not be reused directly.

## 1. Code changes (do these first — art can be generated in parallel/after)

| # | Task | Where | How |
|---|------|-------|-----|
| 1 | Add 6 optional Hero fields + `HasHeroMode` | `Models/PetSkin.cs` | Mirror the existing `CelebrateFramePaths` pattern: `HeroIdleImagePath` (`string?`), `HeroBlinkFramePaths`, `HeroWalkFramePaths`, `HeroPowerIdleFramePaths`, `HeroTransformFramePaths`, `HeroTransformBackFramePaths` (all `string[]?`), plus `HasHeroMode => HeroIdleImagePath != null` mirroring `HasDedicatedPoses`. No generic manifest/dictionary. No `HeroSleepFramePaths`. |
| 2 | Add `IsHeroMode` bool | `ViewModels/AgentPetViewModel.cs` | `[ObservableProperty] private bool _isHeroMode;` — plain bool, orthogonal to `PetState`, not a new enum value. |
| 3 | Load hero frame arrays | `Views/Controls/AgentPetControl.xaml.cs` → `ResolveSkin()` | Use the existing `LoadFrames()` helper for the 5 arrays; load `HeroIdleImagePath` on demand like `IdleImagePath` today. |
| 4 | Branch Working/Idle rendering on `IsHeroMode` | `Views/Controls/AgentPetControl.xaml.cs` → `ApplyDedicatedPoseState(PetState state)` | Working: prefer `_heroWalkFrames` when hero-on and non-empty, else normal walk. Idle: prefer a continuous `_heroPowerIdleFrames` loop (rendered like `_sleepFrames`, no static+blink split) when present, else fall back to `HeroIdleImagePath` + `_heroBlinkFrames`. Sleeping: always normal art, ignore `IsHeroMode`. |
| 5 | One-shot transform/transform-back | `Views/Controls/AgentPetControl.xaml.cs` → `FrameTick()` + new `StartHeroTransform(bool enteringHero)` | Mirror `_celebratePos`'s pattern with a new `_heroTransformPos` sentinel (`-1` = idle), as a new priority branch **above** celebrate. Loop count 1. New `HeroTransformTicksPerFrame` constant (reuse celebrate's ~220ms cadence value). On completion call `ApplyDedicatedPoseState(_viewModel.State)`. Guard mutual exclusion both ways with celebrate. |
| 6 | Fix click vs. drag ambiguity | `Views/Controls/AgentPetControl.xaml.cs` → `OnDragStart`/`OnDragMove`/`OnDragEnd` | Today every mouse-down unconditionally sets `IsDragging=true` and every mouse-up unconditionally calls `SavePetPosition` — no click/drag distinction exists. Add `_dragStartPos` (Point) + `_dragMoved` (bool). `OnDragStart`: record start pos, reset `_dragMoved=false`. `OnDragMove`: set `_dragMoved=true` once movement exceeds `ClickMoveThresholdPx=4` (live drag itself unchanged). `OnDragEnd`: branch on `_dragMoved`. |
| 7 | Wire click-to-toggle | `Views/Controls/AgentPetControl.xaml.cs` → `OnDragEnd`, the `!_dragMoved` branch | If `_skin.HasHeroMode` and no transform in flight: toggle `_viewModel.IsHeroMode`. Skip `SavePetPosition` in this branch. |

## 2. Asset pipeline (per animation: idle / blink / walk / power_idle / transform / transform_back)

1. Reuse the locked Softdebut character description from
   `2026-07-12-softdebut-pet-sprite-task.md` verbatim in every prompt, plus
   whatever the Hero-concept answer from §0 adds (e.g. "glowing orange visor
   accents, cape animated as if caught in wind").
2. **Generate one frame at a time for anything needing genuinely distinct
   poses** (walk, transform, transform_back) — strips produced near-duplicate
   mirrored poses twice already this project (walk cycle, then again on a
   first attempt at celebrate frames). Attach the most-recently-approved
   Softdebut asset each time for style continuity, and restate the exact
   antenna style (black rods, black crown base) explicitly in text — this
   drifted to the wrong style twice already (celebrate frames 3–4) even with
   an attached reference.
   - `hero_idle`: 1 static frame.
   - `hero_walk`: 6-frame strip or 6 single frames (walk cycles have been
     strip-safe once contact/push-off/passing poses are spelled out
     explicitly per frame — use judgment, fall back to single-frame if the
     strip looks duplicated).
   - `hero_blink`: 4 frames, only needed if going the static-idle+blink route
     rather than a continuous power-idle loop.
   - `hero_power_idle`: 4 frames, continuous loop.
   - `hero_transform`: 4 frames, one-shot, normal→hero.
   - `hero_transform_back`: 4 frames, one-shot, hero→normal — or reuse
     `hero_transform` reversed if that reads acceptably as a "power-down"
     (saves one full generation pass; flag this option to the user rather
     than deciding unilaterally).
3. Extract with a standalone script (do not `import process_frames`/
   `process_statics` — re-executes every module-level call and can
   resurrect/overwrite unrelated skins' assets). Start from Softdebut's
   already-tuned background-removal params: `warm_max=6, bright_min=222,
   reclaim_warm_min=3, keep_frac=0.05`, then `despeckle_alpha(radius=2)`.
   Re-verify bg RGB/warmth/brightness stats per new sheet — don't assume
   identical numbers carry over from a different ChatGPT session.
4. Watch for the grounded-pose shadow/leg-gap artifact found on celebrate
   frames 1 and 4 (standing poses only) — a soft drop-shadow ellipse under
   the feet plus a solid gray patch between the legs, both near-identical in
   tone to the character's white plastic so color-based flood removal alone
   can't separate them. Fix (if it recurs): envelope-crop alpha outside the
   last clean row's column range for all rows below it, then a small local
   seeded flood-fill confined to the leg-gap bounding box.
5. Trim every frame to its tight alpha bbox, then rescale so trimmed height
   matches `pet_softdebut_idle.png`'s existing height (493px). Not optional —
   prevents the character visibly resizing when Hero mode toggles.
6. Composite every frame over dark orange (200,120,70) at 2–3× and inspect
   for interior holes, leftover background, clipped tops, drifted style.
   Reject and regenerate rather than hand-patching a bad frame.
7. Build a looping GIF per animation (`scripts/make_gif.py`, 6 total) plus
   one extra "handoff preview" (transform.gif immediately followed by
   power_idle.gif) so the one-shot-to-loop visual continuity can be
   sanity-checked, not just each clip alone. Send all together for sign-off.
   **Do not touch `PetSkins.cs` until approved.**

## 3. Register + deploy (only after sign-off)

1. Add the 6 new fields to the `Softdebut` entry in `PetSkins.cs`, flat-named
   `/Assets/pet_softdebut_hero_*.png` (idle, walk1-6, blink1-4, power_idle1-4,
   transform1-4, transform_back1-4) — already covered by the existing
   `Assets\pet_*.png` csproj wildcard, zero csproj changes needed.
2. `dotnet build --configuration Release` then `dotnet test --configuration
   Release` in `D:\ClaudeTracker\source` — must stay 151/151 green. Optional,
   not blocking: one lightweight test asserting that if
   `Softdebut.HasHeroMode` is true, all 5 hero frame arrays are non-empty.
3. Stop the running `ClaudeTracker.exe` (Release build locks the exe),
   rebuild, relaunch.
4. **Manual live verification required** — ClaudeTracker isn't Start-Menu-
   registered, so computer-use tooling cannot drive or view its window
   (confirmed again this session). The user must manually: click a Softdebut
   pet once → transform-in plays → settles into hero idle/power-idle art;
   confirm Working/Idle render hero art while toggled on; confirm Sleeping
   still shows normal art; click again → transform-back plays → settles into
   normal art; drag the pet a nontrivial distance → confirm dragging still
   works exactly as before (updates, saves on drop, does **not**
   accidentally toggle hero mode); relaunch the app → confirm Hero mode
   reset to off.
5. Commit + push: ask the user first, same pattern as the last two rounds
   (OAuth fix, celebrate animation) — do not assume approval.

## What NOT to produce (this phase)

No `wave`/`sit`/`wakeup`/other new idle-variant states, no cursor-tracking
"random events" (`look_at_cursor`, `dodge_cursor`, `spin`, etc.), no
`effects/` overlay layer, no manifest.json/data-driven loader, no nested
`Assets/` subfolders, no hero art for `angel_chick`. Those were all candidate
next-phase ideas raised alongside Hero mode but explicitly deferred — pick
them up as separate follow-up tasks if wanted later, don't fold them into
this one.
