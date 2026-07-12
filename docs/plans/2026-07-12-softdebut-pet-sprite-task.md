# Task: Generate Softdebut mascot core sprite set (idle / blink / walk)

Follow the existing `pet-sprite` skill workflow end to end. Do not invent a new
pipeline, folder structure, or deliverable format — this project already has one.

## 0. Decide before generating anything

- **Skin id**: working id is `softdebut` (rename before registering in
  `PetSkins.cs` if you want something else).
- **Family/stage**: is this a standalone character (`FamilyId` = itself,
  `Stage: 1`, no evolution) or a new stage in an existing family (like
  `angel_chick` stage 1→4)? Default to **standalone** unless told otherwise —
  don't silently attach it to an existing evolution family.
- **Reference image**: received —
  `C:\Users\Somkait\Downloads\ChatGPT Image 12 ก.ค. 2569 19_39_32.png`
  (Softdebut brand mascot sheet: front/3-4/side/back turnaround + 6 expression
  poses + 6 "in action" scenes + color palette swatches, all one generation
  session so internally style-consistent). Use this as the attached reference
  for every subsequent generation call.
- **Locked character description** (reuse verbatim in every prompt so later
  sessions don't redescribe from scratch and drift):

  > Softdebut mascot: small chibi robot with a round pearl-white dome head, a
  > black oval visor face showing soft amber-orange glowing round eyes and a
  > small friendly smile line, a crown-like antenna of 5 orange spheres on
  > thin white stalks radiating from the top of the head, a rounded white
  > torso with a small orange atom/node badge on the chest, chunky white
  > rounded arms and legs, a charcoal-gray cape with orange trim and an
  > orange atom-node "SOFTDEBUT" logo badge visible on the back. Color
  > palette: orange #FF7A00, charcoal #1F2328, white #FFFFFF, silver gray
  > #E5E7EB, light gray #9CA3AF.

- **Idle pose caveat**: the sheet's `FRONT` turnaround pose has a raised
  waving hand — that's a gesture pose (closer to the `WELCOME` expression
  below it), not a neutral idle stance. Don't crop it as idle directly;
  generate a fresh neutral standing pose (arms relaxed at sides) for true
  idle, referencing this sheet for style only. `SIDE`/`BACK` poses look closer
  to neutral if a faster shortcut is ever wanted, but front-facing is what the
  app renders by default.

## 1. Scope — exactly 3 assets, one strip per image-gen call

| Animation | Frames | Notes |
|---|---|---|
| idle | 1 (static) | Front standing pose. **Not a baked animation** — the running app already drives idle "breathing" via a code-side `ScaleTransform` storyboard for any skin without dedicated sleep frames (`AgentPetControl.xaml.cs`). Don't generate 6 breathing frames; that plumbing doesn't exist in the app today. If baked idle-breathing frames are wanted later, that's a separate app-code task (new `PetSkin` field + wiring), not part of this sprite-gen pass. |
| blink | 4 | Matches every existing skin. Body frozen, only eyelids change: open → half → closed → open. |
| walk | 6 | **Not 8** — every existing skin (angel_chick stage 1–4) uses 6-frame walk cycles; `prompts.md` explicitly recommends 6 ("more frames add nothing at 96px display size, and every extra frame is another chance to drift off-model"). |

Total: 2 image-gen calls (blink strip, walk strip) + 1 for the static idle pose
(or extract idle from the master sheet's front-view pose if the master sheet
already has one — cheaper and guarantees exact style match, see §5 gotcha below).

## 2. Generation — one call at a time, external tool

Claude Code has no image-generation tool of its own. Generation happens via an
external tool (ChatGPT image gen has worked well for this project). For each
animation:

1. Use the shared style block + per-animation prompt from `prompts.md`
   (walk = §2 but trimmed to 6 frames using the contact/push-off/passing
   pattern, blink = §3 verbatim).
2. **Attach an image reference — but not always the same one.** For the first
   strip, attach the master sheet. For every strip after the first, attach
   whichever already-generated asset most recently got user sign-off (not
   necessarily the master) — style drifts across separate generation sessions
   even with a careful prompt (happened twice already: a `celebrate` and a
   `blink` strip both came out visibly glossier than the original). Restate
   specific stylistic details in text too (halo/cape/antenna shape, colors) —
   don't rely on "same as attached" alone.
3. State walk direction explicitly based on project convention (right-facing
   art; the app mirrors for left-facing screen travel) — see §5.

## 2b. Ready-to-paste prompts

Shared style block (prepend to all three):

```
3D chibi character sprite [strip], glossy toy-like render, soft studio
lighting, clean plain light-gray background (#F0F0F0), no gradient, no
vignette.
Character: Softdebut mascot: small chibi robot with a round pearl-white
dome head, a black oval visor face showing soft amber-orange glowing round
eyes and a small friendly smile line, a crown-like antenna of 5 orange
spheres on thin white stalks radiating from the top of the head, a rounded
white torso with a small orange atom/node badge on the chest, chunky white
rounded arms and legs, a charcoal-gray cape with orange trim and an orange
atom-node "SOFTDEBUT" logo badge visible on the back. Color palette: orange
#FF7A00, charcoal #1F2328, white #FFFFFF, silver gray #E5E7EB, light gray
#9CA3AF.
Consistency is critical: exactly the same character as the attached
reference, same proportions, same colors, same rendering style.
NO text, NO numbers, NO labels anywhere in the image.
```

**Idle** (single image, attach the master sheet):
```
[shared style block above]

Single image, standing neutral front view, arms relaxed down at the sides
(not waving, not gesturing), calm resting pose, facing directly at camera.
```

**Blink** (4-frame strip, attach the approved idle output once it exists,
otherwise the master sheet):
```
[shared style block above]

"BLINK CYCLE" — exactly 4 frames, standing front view, same neutral relaxed
pose as idle (arms down at sides). The body is FROZEN — identical pose and
position in every frame, ONLY the eyelids change:
frame 1: eyes fully open, frame 2: eyes half closed,
frame 3: eyes fully closed, frame 4: eyes fully open again.
Evenly spaced in one horizontal row, clear gaps between frames, no
overlapping elements.
```

**Walk** (6-frame strip, attach the most recently approved output):
```
[shared style block above]

"WALK CYCLE" — exactly 6 frames of one seamless walking loop, side-front
(3/4) view walking to the right:
frame 1: contact (front foot planted), frame 2: push off,
frame 3: passing pose (body at highest point), frame 4: contact (other
foot), frame 5: push off, frame 6: passing pose.
Body bobs up and down naturally, cape flows slightly with the motion, arms
swing slightly. The loop must be seamless: frame 6 flows directly back into
frame 1. Evenly spaced in one horizontal row, clear gaps between frames.
```

Generate in order idle → blink → walk, get sign-off on each (§6) before
moving to the next, and re-attach the latest approved output per §2 step 2.

## 3. Extraction

- Static idle pose → `scripts/process_statics.py` pattern.
- Blink/walk strips → `scripts/process_frames.py` pattern, both import
  `scripts/bgremove.py`.
- **Don't `import process_frames`/`process_statics`** to extract just these
  new frames — that re-executes every module-level call at the bottom of the
  file and silently re-writes/resurrects every other skin's assets, including
  ones deleted on purpose. Write a small standalone script that imports only
  `bgremove.flood_remove_bg` and inlines the extraction loop for these 3
  assets. Diff `ls Assets/pet_*.png` before/after to confirm nothing
  unexpected changed.

## 4. Background removal — per-sheet tuning, not a fixed call

Before running `bgremove.py`, print the background's RGB/warmth/brightness
stats for *this specific sheet* and pick `warm_max`/`bright_max` accordingly
(reference points: Angel Chick bg warmth ≈0.7 → `warm_max=6`; Angel Knight bg
warmth ≈7.8 → `warm_max=14`). Don't reuse a fixed value from another
character's sheet. Use `keep_frac≈0.001` unless there's a reason to raise it
— tiny decorations (sparkles, glyphs, thin cape edges) get silently dropped
above that.

## 5. Verification gotchas (each has caused a real bug before)

- **Walk facing**: don't eyeball left/right. Compute the x-centroid of dark
  ("eye") pixels after bg removal vs. canvas half-width — eyes right of
  center ⇒ already right-facing (no mirror), left of center ⇒ mirror with
  `Image.FLIP_LEFT_RIGHT`. A visually-judged sheet was mirrored backward once,
  producing a moonwalk-looking gait on screen.
- **Height normalization**: trim every frame to its tight alpha bbox, then
  rescale so trimmed height matches the idle pose's trimmed height (width
  varies naturally). Fixing scale by a fixed width instead caused the
  character to visibly shrink/grow when switching idle↔walk in the past.
  Verify with `getbbox()` — trimmed content height should match across idle,
  blink, and walk frames.
- **Orange-background composite check**: composite every produced PNG over
  dark orange (200,120,70) at 2–3× and inspect for interior holes (wallpaper
  showing through the body), leftover white patches, clipped tops, or
  leftover label text/frame numbers. Never skip this step.
- **Reject, don't patch, drifted frames**: if a frame doesn't match the
  character (redesigned proportions, wrong colors, different render style),
  regenerate that strip — don't hand-edit a bad frame into passing.

## 6. Sign-off gate before touching app code

Build a looping GIF per animation (`scripts/make_gif.py`) and send all of them
in one batch for user approval **before** editing `PetSkins.cs` or doing any
build/publish. Registering first and asking after wastes a full
build+publish+restart cycle if a frame needs fixing.

## 7. Register + deploy (only after sign-off)

1. Add the `PetSkin` entry to `PetSkins.cs` — `id: "softdebut"`, idle/happy
   paths, `WalkFramePaths`/`BlinkFramePaths` arrays. No `EyeLeftXFrac`/
   `EyeRightXFrac` needed — those are only for single-image skins without
   dedicated pose art; this skin has real blink frames.
2. `dotnet build --configuration Release` then `dotnet test` in
   `D:\ClaudeTracker\source`.
3. Publish + hot-swap the running app per the standard flow in `SKILL.md` §5.

## What NOT to produce

No `manifest.json`, no `README.md`, no ZIP, no `DesktopPet/normal/...` nested
folder tree, no spritesheet-grid variant, no run/fly/hero-form/random-event
animations — none of that exists in this project's actual pipeline. Output is
just: transparent PNGs in `Assets/pet_softdebut_*.png` + a registered
`PetSkin` entry + a passing build.
