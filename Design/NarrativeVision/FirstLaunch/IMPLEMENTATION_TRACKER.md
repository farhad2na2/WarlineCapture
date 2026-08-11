# First-Launch Narrative Vision Slice Implementation Tracker

Date: 2026-07-11

Status: Gate 9R accepted on 2026-08-12; Gate 10 remains open for M01-camera, Android-device, and existing shell-regression evidence

Scope: Build a reviewable first-launch motion-comic slice from minimal logo through the M01 gameplay handoff, plus a review-only gameplay placeholder, M01 debrief, and command-base reveal. This tracker owns execution order, artifact manifests, approval gates, Unity boundaries, review controls, skip behavior, evidence, and completion status.

## Purpose

This is the implementation and production handoff for the first screens a new player sees. It exists to expose visual, narrative, continuity, pacing, interaction, accessibility, and game-to-comic mismatch before full Campaign or M01 gameplay implementation proceeds.

The slice must prove:

- Sahrin looks like a living city before it becomes a battlefield.
- ARIA, Dalia, Samira, the Commander identity flow, JRC, civilians, and Ash Line remain visually coherent.
- The tactical motion-comic style feels like the same product as the 3D operation map.
- A fresh player reaches meaningful command within the accepted first-session timing band.
- The complete sequence can be reviewed without playing M01.
- A visible Skip control can leave the sequence and enter the gameplay handoff safely.
- Skipping never leaves Commander identity, guidance, story state, routing, or mandatory M01 context undefined.

## Authority Order

Read these files in order before changing this tracker or producing an artifact:

1. `Design/AAA_Mobile_Game_Design_Document_v0_2.md`
2. `Design/Campaign_Narrative_Bible.md`
3. `Design/Shattered_Relay_Story.md`
4. `Design/First_Player_Experience_And_Story_Onboarding_Design.md`
5. `Design/Narrative_Presentation_And_Cutscene_Design.md`
6. `Design/Campaign_Narrative_Sequence_And_Comic_Catalog.md`
7. `Design/Campaign_Mission_High_Level_Design_Catalog.md`
8. `Design/SagaChapters/Saga_Chapter01_First_Response.md`
9. `Design/M01_FirstContact_Production_Contract.md`
10. `Design/Architecture/gameplay_solid_ecs_contract.md`
11. This tracker

When documents disagree, the higher active authority wins. This tracker may define execution detail but may not change story canon, sequence meaning, first-launch product direction, character casting, cultural rules, or architecture guardrails.

## Locked Decisions

These decisions are accepted unless the user explicitly reopens them:

- The first-launch route is story-first. A fresh profile does not enter the normal Main Menu before the opening and M01 handoff.
- The visual form is a grounded illustrated tactical motion comic informed by actual project characters and environments.
- Image generation is offline production tooling only. No runtime generative AI is permitted.
- The Unity reference package is an internal accuracy aid, not final art and not a substitute for user art review.
- Every final panel requires explicit user approval before it may be copied into runtime art folders or integrated into the playable sequence.
- Phases 1-7 complete the entire review-art package before any narrative runtime implementation begins.
- Gate 6 is the blocking user approval for the complete art package. Intermediate art checks are internal and are included as evidence in that package.
- After Gate 6, implementation, integration, accessibility, reviewer-mode, Skip, QA, and closeout proceed autonomously without additional blocking user approvals. The completed integrated slice is still delivered for review.
- Existing character models and config identities are continuity anchors. Text-only generation may not redesign Dalia, Samira, JRC, civilians, or Ash Line.
- ARIA receives one dedicated visual identity that is created once, approved, and reused.
- The Commander is not shown with a fixed face before identity selection.
- Story images contain no baked subtitles, UI, logos, fake regional text, objective text, or interaction labels.
- Most scenes use one reusable data-driven narrative player. Bespoke Unity Timeline assets are not created for each panel or short exchange.
- Review MP4 files are evidence only and never the retail playback source. Runtime art, comic dialogue, portraits/icons, subtitles, Skip, and interaction surfaces remain separate layers.
- Commander identity, guidance, and every other selectable menu or popup are live game UI states and may not be baked into cinematic art or video.
- If a production Old Market operation scene and camera already exist, the final illustrated frame must match them. If they do not exist at art lock, the user-approved `FL-P18` frame becomes the binding location, direction, lighting, landmark, and camera-composition contract for later 3D implementation.
- A visible Skip control is available during normal playback.
- Reviewer mode provides play, pause, restart, previous panel, next panel, timeline position, skip-to-game, jump-to-debrief, reduced-motion preview, and capture controls.
- The vision slice does not implement M01 combat, objectives, rewards, progression, or command-base functionality. It uses typed handoff placeholders until those owners exist.

## Slice Boundaries

### Included

- Minimal logo/title mark.
- `seq.prologue.command_lost`.
- `seq.prologue.commander_identity`.
- Guidance choice: Full Guidance, Tactical Hints, or Veteran.
- `seq.ch01.open.first_response`.
- `seq.ch01.m01.brief`.
- Illustrated-to-3D Old Market handoff.
- Review-only gameplay placeholder.
- `seq.ch01.m01.debrief`.
- Static or limited command-base reveal proving the first-session destination.
- Runtime subtitles, temporary audio, motion, review controls, skip, static fallback, and accessibility behavior.

### Excluded

- Playable M01 selection, movement, combat, objective evaluation, or civilian simulation.
- M01 reward grants or persistence beyond a typed placeholder completion payload.
- Full command-base navigation or Campaign progression.
- Full Story Archive implementation.
- Chapter 1 M02-M05 sequences.
- All remaining Campaign sequences.
- Final localized voice production.
- Runtime facial animation, full-body character animation, or bespoke 3D cinematics.
- A new Main Menu or operation map redesign.
- Live AI generation, downloaded story art, or network-required playback.

## Sequence And State Inventory

The slice has 20 canonical narrative beat states plus interactive and review-only states. Visual panels may combine adjacent beats after storyboard approval, but no story beat may disappear.

| Order | Stable identity | Required story state | Expected realization |
|---:|---|---|---|
| 0 | `first_launch.logo` | Minimal brand and audio cue | One short non-blocking state |
| 1-7 | `seq.prologue.command_lost` | Living city, attacks, command loss, Dalia/Samira crisis, Relay boot, continuity candidate, Old Market handoff | Seven canonical beats; approximately 5-7 final panels |
| 8-9 | `seq.prologue.commander_identity` | Commander authentication and selected/default identity confirmation | Interactive identity screen plus confirmation state |
| 10 | `first_launch.guidance_choice` | Full Guidance, Tactical Hints, or Veteran | Interactive screen |
| 11-15 | `seq.ch01.open.first_response` | Broken district, surviving forces, civilians/services, coordinated pattern, Commander takes authority | Five canonical beats; may visually merge with M01 brief |
| 16-18 | `seq.ch01.m01.brief` | Confirmed patrol, immediate action, Dalia hands over field control | Two or three concise panels or equivalent interactive briefing |
| 19 | `first_launch.m01_handoff` | Exact illustrated-to-3D camera transition | 3D world handoff state |
| 20 | `first_launch.gameplay_placeholder` | Review build proves route without pretending M01 exists | Review-only placeholder with Jump To Debrief |
| 21-23 | `seq.ch01.m01.debrief` | Corridor secured, coordinated orders and a fragmentary revoked-credential trace found, next threat identified | Three concise panels |
| 24 | `first_launch.command_base_reveal` | Restored headquarters is the earned destination | Static or limited motion proof |

## Target Timing

| Segment | Target band | Hard rule |
|---|---:|---|
| Minimal logo | 0-5 seconds | No button wall or forced delay. |
| Cold open | 5-25 seconds | Establish life, attack, and command failure without gore or exposition dump. |
| ARIA boot | 25-40 seconds | Establish damaged continuity and immediate civilian danger. |
| Identity | 40-55 seconds | Default identity allows immediate continuation. |
| Guidance | 55-65 seconds | One selection, changeable later. |
| M01 opening/handoff | 65-90 seconds | Gameplay handoff begins by 90 seconds in normal playback. |
| M01 debrief review | 10-20 seconds | Concise consequence and next hook. |

Skip-to-game may reach the handoff earlier. Loading time is measured separately and may not be hidden by extending narrative playback.

## Progress Legend

| Mark | Meaning |
|---|---|
| `[x]` | Complete and supported by evidence. |
| `[~]` | Partial; not accepted. |
| `[ ]` | Not started. |
| `[!]` | Blocked by a named prerequisite. |

## Progress Summary

| Phase | Status | Exit gate |
|---|---|---|
| 0. Contract and workspace | Complete | Gate 0: approved 2026-07-10 with art-first amendment |
| 1. Source inventory and Unity captures | Complete | Gate 1A: internal source validation complete |
| 2. Art direction and style frames | Complete | Gate 1B: Match-aligned AI style approved 2026-07-10 |
| 3. Character and faction continuity | Complete | Gate 2: internal continuity lock complete; runtime exports remain correctly deferred until Gate 6 |
| 4. Location, prop, and handoff continuity | Complete | Gate 3: internal world-continuity lock complete |
| 5. Storyboard and panel manifest | Complete | Gate 4: internal storyboard lock complete |
| 6. Animatic and timing | Complete | Gate 5: revised presentation lock passed 2026-07-10 |
| 7. Final layered art | Complete | Gate 6: all 22 revisions approved, exported, and verified 2026-07-11 |
| 8. Audio, subtitles, and accessibility | Complete | Gate 7R: corrected runtime audio/readability accepted with Gate 9R on 2026-08-12 |
| 9. Reusable Unity sequence player | Complete | Gate 8: Addressables-backed runtime player accepted 2026-07-11 |
| 10. Review mode, skip, and route handoff | Complete | Gate 9R: corrected presentation accepted by the project owner on 2026-08-12 |
| 11. Visual, device, memory, and regression QA | In Progress | Gate 10 remains open for M01 camera continuity, physical Android profiling, and the existing shell regression |
| 12. Closeout and next-slice handoff | Not Started | Gate 11: autonomous closeout and delivery |

Progress update rule: update the phase row and the relevant checklist in the same change that adds evidence. A generated image, code file, prefab, or test is not complete until its review evidence is linked.

## Progress Reporting Contract

Every future work report must state:

- current phase and exit gate;
- concrete artifacts completed since the previous report;
- remaining work in the current phase;
- total final-panel progress out of `22`;
- whether runtime implementation has started;
- the next action; and
- whether the user must approve anything now.

Reports use tracker evidence rather than treating tool completion or image generation alone as acceptance. When work changes tracked artifacts, this snapshot and the affected checklist are reconciled before the user-facing progress report.

## Current Progress Snapshot

Last reconciled: 2026-08-12

| Measure | Current state |
|---|---|
| Passed gates | Gate 0, Gate 1A, Gate 1B, Gate 2, Gate 3, Gate 4, Gate 5, Gate 6, Gate 7R, Gate 8, and Gate 9R |
| Active gate | Gate 10: M01-camera continuity, physical Android profiling, and existing shell-regression evidence |
| Art-first phases 1-7 | Complete; all approved panel revisions are exported and source-to-runtime hash verified |
| Final narrative panels | `22/22` user-approved, exported in 16:9 and 20:9, and configured as Unity sprites |
| Checklist completion | `233/249` complete (`93.6%`), with 1 partial and 1 blocked item |
| Runtime phases 8-12 | Phase 10R accepted; Gate 10/Phase 11 stays open and Phase 12 remains deferred until its evidence exists |
| Current blocker | No FirstLaunch blocker prevents M01 implementation. Gate 10 still cannot close until the M01 handoff camera, physical Android evidence, and pre-existing `statusChipSprite` shell regression are resolved or formally dispositioned. |
| Next action | Continue the accepted M01 dense-city tracker from M01DC-002; feed its camera and Android evidence back into Gate 10 when available. |
| User approval required now | No; the project owner approved the current comic/story direction and instructed Codex to continue M01. |
| Autonomous continuation | Yes, within the accepted M01 HLD, technical architecture, and implementation tracker. |

## Current Baseline

- [x] Canonical story and Chapter 1 arc exist.
- [x] Stable first-launch and M01 sequence IDs exist.
- [x] Motion-comic format, continuity, accessibility, and cultural rules exist.
- [x] First 90-second experience target exists.
- [x] Dalia, Samira, Commander proxy, JRC, civilians, and Ash Line config anchors exist.
- [x] User selected the First-Launch Narrative Vision Slice as the next production target.
- [x] User requires a reviewable complete sequence and Skip-to-game behavior.
- [x] User approves this detailed tracker as Gate 0, amended to require art-first production and one final-art approval before autonomous implementation.
- [x] First-launch source-art review folder and production manifest exist.
- [ ] No narrative sequence player, comic panel runtime, Story Archive runtime, or first-launch sequence config currently exists.

## Required Repository Layout

Create folders only when adding their first tracked artifact. Do not add empty directories.

```text
Design/NarrativeVision/FirstLaunch/
    IMPLEMENTATION_TRACKER.md
    reference/
        characters/
        factions/
        locations/
        props/
        style/
        unity-captures/
    storyboard/
        first_launch_panel_manifest.json
        first_launch_storyboard.md
        first_launch_contact_sheet.png
    animatic/
        first_launch_animatic.mp4
        first_launch_animatic_notes.md
    source/
        layered-panels/
        audio/
        subtitles/
    evidence/
        visual/
        device/
        performance/
        validation_report.md

Assets/Game/Art/Narrative/FirstLaunch/
    Characters/
    Factions/
    Locations/
    Panels/
    Effects/

Assets/Game/Audio/Narrative/FirstLaunch/

Assets/Game/Configs/Narrative/FirstLaunch/

Assets/Game/Prefabs/UI/Narrative/

Assets/Game/Scripts/Configs/Narrative/
Assets/Game/Scripts/UI/Narrative/
Assets/Game/Scripts/Composition/Narrative/
Assets/Game/Scripts/Editor/Narrative/

Assets/Tests/Editor/Narrative/
Assets/Tests/PlayMode/Narrative/
```

`Design` contains source references, prompts, manifests, review media, and layered production sources. `Assets` contains approved runtime exports only. Runtime code must never load source-reference images from `Design`.

## Asset Roles

| Asset role | Player-facing | Reuse rule |
|---|---|---|
| Unity model/location capture | No | Permanent source reference |
| Character continuity sheet | No | Reused for every appearance of the character |
| Location continuity sheet | No | Reused for all before/during/after states of that location |
| Canonical portrait/avatar | Yes | Reused across story UI and later Story Archive |
| Character pose/cutout | Sometimes | Reused only when pose, light, and camera remain credible |
| Panel background | Yes | May support more than one adjacent beat |
| Layered final panel | Yes | Sequence-specific unless explicitly approved for reuse |
| Flattened static fallback | Yes | One fallback per final panel state |
| Subtitle/UI text | Yes | Runtime/localized only; never baked into art |

## Source And Runtime Image Standards

- Source masters use `4800 x 2160` where a full-width composition is required.
- Critical faces, actions, and story information stay inside a centered `3840 x 2160` 16:9 safe composition.
- Runtime full-screen exports begin at `2400 x 1080` for the 20:9 landscape target and are adjusted only after device/memory evidence.
- Transparent character/effect layers retain enough source resolution for the approved maximum pan/zoom.
- Normal motion must not expose empty canvas beyond an asset edge.
- Runtime panel art uses no mipmaps unless a measured world-space use requires them.
- Runtime textures are not Read/Write enabled unless a measured feature requires CPU access.
- Alpha is retained only on layers that require it.
- Compression is selected by device capture, not by file size alone.
- Every final panel has a flattened static fallback for reduced motion or layer-load failure.

## Image Generation And Provenance Contract

- Imagegen uses actual Unity captures and approved continuity/style images as references.
- Dalia, Samira, JRC, civilians, and Ash Line are never generated from text alone.
- ARIA may begin from generated candidates because no canonical model exists; one candidate must be approved before any panel generation.
- Generate or edit one principal character at a time for complex scenes, then composite. Do not trust a one-shot multi-character generation to preserve identity.
- Backgrounds are generated or painted without baked characters when practical.
- Character, foreground, smoke, light, and UI layers remain separable when motion requires them.
- No generated image may contain final text, subtitles, logos, flags, insignia, scripture, news graphics, or regional writing.
- No real military, extremist, religious, national, or political symbol may appear.
- Every generated candidate records source references, prompt, tool/model, date, output path, approval state, and known corrections in the panel manifest or continuity notes.
- Approval states are `Candidate`, `Review`, `Approved`, `Rejected`, or `Superseded`.
- Agents may mark an asset technically ready for review, but only the user may assign `Approved` to final panel art.
- A rejected image remains outside runtime folders and is never silently reused by another agent.

## Review Package Definitions

### Unity Reference Package

This is an internal technical package used to prevent invented characters, incorrect uniforms, mismatched props, and an illustrated handoff that does not resemble the real game scene. It is not polished narrative art and does not require the user to treat raw Unity captures as an art approval milestone.

It contains:

- Labeled model turnarounds and face, material, equipment, and weapon close-ups.
- Exact config, prefab, model, scene, and camera paths.
- Old Market location captures, handoff camera transform, field of view, lighting, and time-of-day evidence.
- A contact sheet that lets the production agent verify that every later generation used the correct source.

Gate 1A is an internal accuracy validation. The package remains available for user inspection, but the first required user-facing visual decision is Gate 1B, the style-frame selection.

### Final Art Review Package

This is the mandatory package the user reviews before any final panel enters the runtime art library. It contains:

- One numbered contact sheet covering every final panel in narrative order.
- One full-resolution flattened composite for every panel, with its stable panel ID visible outside the image area.
- Phone-scale previews at the primary 20:9 target and the 16:9 safe composition, including subtitle and Skip safe-area overlays.
- Side-by-side comparisons against the approved storyboard, style frame, character continuity sheet, and relevant Unity reference.
- A short layered-motion proof for every panel that uses parallax, camera motion, lighting, smoke, signal, or foreground separation.
- A review ledger with one user-owned disposition per panel: `Approved`, `Changes Required`, or `Rejected`.

The review contract is strict:

- Contact-sheet approval alone is insufficient; every panel must also be inspectable at full resolution.
- An agent self-review, test pass, or technical export cannot replace explicit user approval.
- Panels marked `Changes Required` or `Rejected` stay outside runtime art folders, are revised, and are presented again.
- Runtime exports must be generated only from the exact approved revision recorded in the review ledger.
- After panel approval, the user receives a separate integrated-playback review so motion, crop, transitions, subtitles, and timing can still be corrected.

## Character And Faction Manifest

### ARIA

- Dedicated visual identity with no soldier/civilian face reuse.
- Neutral, booting, warning, damaged/incomplete, and stable states.
- Reusable avatar, close portrait, terminal representation, and signal/glitch language.
- Stable geometry, palette, eye/focal point, and interface motif.
- No random face, hair, clothing, age, or ethnicity changes between states.

### Major Dalia Rahim

Canonical config: `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config.asset`.

- Unity front, rear, profile, and three-quarter captures.
- Face close-up and proportion reference.
- Calm, urgent, concerned, commanding, and exhausted expressions.
- Field report, survivor assistance, and tactical command poses for the opening.
- Exact uniform, equipment, weapon, insignia policy, and color/material callouts.
- Reusable runtime portrait and approved pose cutouts.

### Engineer Samira Haddad

Canonical config: `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Civilian_Female_01_Config.asset`.

- Unity front, rear, profile, and three-quarter captures.
- Face close-up and proportion reference.
- Calm, skeptical, urgent, protective, and exhausted expressions.
- Road engineer, emergency report, and civilian coordination poses.
- Stable civilian engineering clothing and equipment; no unapproved military gear.
- Reusable runtime portrait and approved pose cutouts.

### Commander Identity

Current optional battlefield proxy: `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Leader_Male_01_Config.asset`.

- Six diverse, free, player-facing portrait choices plus a neutral fallback, matching the active art register.
- No portrait implies a canonical gender, ethnicity, or biography.
- Pre-identity compositions use hands, silhouette, first-person, back view, or terminal framing.
- The selected portrait is injected dynamically into the identity confirmation and later portrait surfaces.
- No single pre-rendered Commander face appears in shared story panels.

### JRC, Civilians, And Ash Line

- JRC regular-force silhouette/equipment sheet based on actual roster models.
- Civilian crowd and responder sheet based on actual civilian models and occupations.
- Ash Line first-contact patrol sheet based on actual insurgent configs.
- JRC, civilians, and Ash Line remain distinct through authored role, equipment, weapons, behavior, and context, never ethnicity or neighborhood.
- Ash Line art uses fictional markings only and does not inherit civilian identity assets.

## Location And Prop Manifest

### Old Market

- Living morning state with vendors, clinic delivery, road work, traffic remnants, vegetation, material wear, and ordinary activity.
- Attack state with smoke, interrupted services, blocked routes, and emergency response without gore.
- M01 handoff state matching the real 3D location and camera.
- After-action/debrief state showing a secured corridor and continuing response.
- One approved geography sheet locking major landmarks, road direction, sun direction, and camera orientation.

### Damaged JRC Terminal

- Command room establishing local legitimacy rather than a generic science-fiction bridge.
- Damaged communications and Civic Relay terminal.
- ARIA boot light and interface language.
- Identity-selection surface that can accept dynamic portrait/name data.

### Required Props And Overlays

- JRC radios and command terminal.
- Civic Relay symbol and interface treatment.
- Civil-defense and clinic equipment.
- Fictional Ash Line weapons/equipment consistent with actual models.
- Smoke, dust, embers, blackout, signal noise, and light overlays.
- No generated text; every readable label is a runtime text or reviewed authored texture.

## Style Frame Gate

Before storyboard finalization, produce four non-final style frames:

1. `STYLE-01 Living Sahrin`: Old Market before the attack.
2. `STYLE-02 Crisis`: attack aftermath, civilians, Dalia/JRC response, and readable danger.
3. `STYLE-03 ARIA Boot`: damaged command terminal, ARIA identity, and Commander framing.
4. `STYLE-04 Scale Stress Test`: non-runtime Chapter 4 air/armor image proving the style can scale beyond intimate scenes.

Gate 1B passes only when all four frames:

- look like one visual world;
- preserve actual character and equipment identity;
- avoid a uniform orange desert filter;
- remain readable at phone scale;
- show a living regional city rather than generic ruins;
- contain no malformed anatomy, weapons, vehicles, architecture, or duplicated subjects;
- contain no baked UI or unreviewed writing;
- pass internal art-direction review and include all four frames in the final Gate 6 evidence package.

## Storyboard Panel Manifest Contract

`first_launch_panel_manifest.json` must contain one record per visual/interactive state with these fields:

```text
PanelId
SequenceId
CanonicalBeat
ApprovalState
DurationSeconds
BackgroundAsset
MidgroundAssets
CharacterLayers
ForegroundAssets
EffectLayers
StaticFallbackAsset
CameraMotionPreset
TransitionIn
TransitionOut
SubtitleKey
SpeakerKey
TemporaryVoiceAsset
AudioEventIds
InteractiveAction
SkipDestination
ReducedMotionBehavior
GameplayHandoffAnchor
SourceReferences
GenerationRecord
KnownCorrections
```

The JSON manifest is a production/evidence artifact. Runtime content is authored as Unity configs and must not parse this Design file in production.

## Runtime Architecture Contract

The runtime implementation must preserve existing assembly and naming rules.

### Architecture Alignment Evidence (2026-07-12)

- The focused architecture refactor is complete and tracked in `Design/Architecture/first_launch_architecture_alignment_refactor_tracker.md`.
- Narrative domain contracts now live in dependency-free `Game.Narrative.Contracts`; deterministic route and sequence progression live in `Game.Narrative.Runtime` without UI, composition, ECS, Addressables, or editor dependencies.
- Canvas/audio/Addressables work remains in managed presentation edges. Narrative `*View` types are serialized-reference, visual-projection, and raw-intent boundaries; identity/guidance defaults, normalization, transition context, accessibility copy, and one-shot commit policy live in `FirstLaunchNarrativeInteractivePresentationSystemHelper`.
- Panel acquisition is asynchronous, retains current/next only, rejects stale completions by transition token, and preserves direct-sprite diagnostics fallback.
- The checked-in `FirstLaunchSequence.asset` contains its authored audio cues, semantic route roles, completion/evidence metadata, and mission flags; production behavior does not rely on an editor installer running before play.
- Final evidence: integrated gate `56/56` (`/private/tmp/warline-firstlaunch-gate89-closeout.log`), live Menu PlayMode `1/1` (`/private/tmp/warline-firstlaunch-playmode-closeout-r4.xml`), assembly boundaries `31/31`, broad naming `1/1`, non-ECS naming `9/9`, passive-view architecture `10/10`, and async residency `7/7`.
- The repository-wide source-growth runner still reports only the unrelated pre-existing `AudioPlaybackPresentationSystemHelper.cs` 548-line review debt. Exact FirstLaunch helper-path and authorization checks pass.

### Ownership

| Responsibility | Planned owner | Boundary |
|---|---|---|
| Stable narrative/route contracts | `Game.UI.Contracts` | Pure serializable ids, requests, results, and route intent; no Unity object references |
| Sequence and panel asset data | `Game.Configs` | `NarrativeSequenceConfig` ScriptableObject and serializable panel/layer records |
| Serialized Canvas references | `Game.UI.Runtime` | Narrow `*View` components only |
| Playback timing and presentation state | `Game.UI.Runtime` | `NarrativeSequencePresentationSystemHelper`; no gameplay policy |
| First-launch route composition | `Game.Composition` | `FirstLaunchNarrativeCompositionSystemHelper` connects profile state, shell route, config, view, and handoff |
| Reviewer/capture tooling | `Game.Editor` | Editor-only preview window, menu commands, validation, and capture |
| Tests | `Game.Tests.Editor` and `Game.Tests.PlayMode` | Contract, config, view, routing, skip, timing, accessibility, and capture validation |

### Required Guardrails

- No `Manager`, `Controller`, `Facade`, global registry, static `Instance`, or service locator.
- `*View` components hold serialized references and visual binding only.
- Runtime code does not use `transform.Find`, object-name searches, `FindObjectOfType`, or hierarchy path strings.
- Views do not call `SceneManager`, gameplay code, save code, or shell routes directly.
- Skip, continue, identity, guidance, and completion actions emit typed requests to composition.
- Story art/config data does not enter ECS simulation hot paths.
- No per-frame LINQ, string building, asset lookup, component discovery, or UI hierarchy rebuilding.
- Playback preloads only the current and next required panel package on constrained devices.
- A missing optional motion/audio layer falls back to the static panel; it never blocks the route.
- A missing required config fails visibly in development and routes safely in release.
- Timeline is allowed only if a later approved 3D synchronization beat cannot be expressed by the reusable player.

## Review Mode Contract

The user must be able to review the slice without entering M01 gameplay.

### Entry

- Editor menu: `Game/Narrative/Open First-Launch Vision Slice`.
- Editor capture command: `Game/Narrative/Capture First-Launch Vision Slice`.
- Development-build route may expose a clearly labeled Narrative Review entry; retail builds must not expose developer controls.
- Review mode starts from a clean transient review state and does not modify the real profile unless explicitly testing persistence.

### Controls

- Play/pause.
- Restart sequence.
- Previous panel.
- Next panel.
- Timeline/panel position.
- Auto-advance on/off.
- Subtitles on/off and size preview.
- Reduced motion on/off.
- Safe-area overlay on/off.
- Sequence/panel id overlay on/off in development only.
- Skip To Game.
- Jump To Debrief.
- Return To Start.
- Capture current panel and full sequence evidence.

### Review Evidence

- Contact sheet of all final states.
- Real-time animatic/video capture from Unity.
- Screenshots at `1920 x 1080`, `2400 x 1080`, and a representative tablet landscape viewport.
- Reduced-motion capture.
- Subtitle maximum-expansion capture.
- Skip-to-game capture from early, middle, identity, and final opening states.
- Jump-to-debrief capture.

## Skip-To-Game Contract

Skip is a product route, not merely a visual hide operation.

### Presentation

- A persistent Skip control is visible in the top-right safe area after the minimal logo state.
- Use a familiar skip-forward icon with localized `Skip` text and an accessibility label.
- The control must not cover faces, subtitles, identity choices, or urgent story information.
- Skip remains available with subtitles off and reduced motion on.
- A pending skip disables repeat input and shows immediate visual feedback.

### Fresh-Profile Behavior

- If Commander identity already exists, Skip routes directly to the M01 gameplay handoff.
- If identity does not exist, Skip presents one concise confirmation that a default neutral Commander identity and `Full Guidance` will be used.
- Confirming applies the default identity and guidance through the same typed path as normal selection, then routes to M01.
- Cancelling returns to the current narrative state without restarting playback.
- The route exposes a concise M01 start summary so skipped story does not remove required objective and civilian context.

### Other Skip Points

- Skipping after identity preserves the selected name, portrait, and guidance.
- Skipping the M01 brief routes to the exact same M01 handoff as watching it.
- Skipping the M01 debrief applies the same mandatory debrief completion/clue payload as watching it, then routes to the command-base reveal.
- Skip never bypasses legally required platform consent.
- Skip never grants gameplay rewards directly; it only preserves required narrative/route state.
- Skip is idempotent. Rapid taps or simultaneous completion cannot launch two routes or publish duplicate completion.

### Vision-Slice Placeholder Behavior

- Before M01 exists, Skip routes to `first_launch.gameplay_placeholder` using the typed handoff payload.
- The placeholder states clearly that gameplay is not part of this vision build.
- Reviewer mode can continue from the placeholder to M01 debrief.
- Production code replaces the placeholder destination without changing sequence configs or Skip semantics.

## Phase Checklists

### Phase 0: Contract And Workspace

- [x] Confirm active story, FPE, sequence, presentation, and architecture authorities.
- [x] Confirm exact sequence IDs in scope.
- [x] Confirm first-launch 60-90 second handoff target.
- [x] Confirm reviewer mode requirement.
- [x] Confirm persistent Skip-to-game requirement.
- [x] Confirm default identity and Full Guidance skip fallback.
- [x] Confirm M01 gameplay remains out of scope.
- [x] Receive user approval for this tracker.
- [x] Record Gate 0 approval date and the art-first/autonomous-implementation amendment.
- [x] Create the first tracked review-art workspace without adding empty directories.

### Phase 1: Source Inventory And Unity Captures

- [x] Resolve Dalia config to exact runtime model/prefab and record the path.
- [x] Resolve Samira config to exact runtime model/prefab and record the path.
- [x] Resolve Commander proxy config to exact runtime model/prefab and record the path.
- [x] Select the exact first-contact JRC roster models.
- [x] Select the exact first-contact Ash Line patrol models.
- [x] Select the exact civilian/responder models used in opening references.
- [x] Inventory the existing Unity character renders and generated portrait references for every selected model.
- [x] Record Dalia and Samira geometry, face, clothing, material, and color anchors from their Unity renders.
- [x] Record JRC and Ash Line equipment/weapon anchors from the selected configs and existing renders.
- [x] Confirm that the intended production Old Market/M01 3D location does not yet exist and record the art-first resolution.
- [x] Lock `FL-P18` as the future handoff camera, landmark, sun, and time-of-day authority after user art approval.
- [x] Document the missing damaged JRC terminal source and define ARIA/terminal generation constraints.
- [x] Inventory Match gameplay, max-zoom, night, base, road, and package-demo captures as the labeled source set.
- [x] Record model/config/prefab paths in the source audit.
- [x] Complete Gate 1A internal source validation and retain the package for optional user inspection.

### Phase 2: Art Direction And Style Frames

- [x] Write the Match-aligned AI production style brief from the presentation authority and user direction.
- [x] Generate three final-quality `FL-P01 Living Sahrin` candidates from actual Match and Unity model references.
- [x] Preserve faceted geometry, flat materials, simplified proportions, Match palette, elevated camera family, and Military-pack vocabulary.
- [x] Reject the realistic/painterly Direction A concepts and prevent their production reuse.
- [x] Validate 16:9 masters and automated 20:9 review crops.
- [x] Validate civilian activity, connected roads, terrain variation, rural/base relationship, and phone-scale readability.
- [x] Validate cultural/symbol safety and absence of baked UI/text.
- [x] Record prompts, references, generation method, output paths, and decision state.
- [x] Receive explicit user approval of the shared Match-aligned art direction.
- [x] Lock candidate A as the primary `FL-P01` composition anchor and retain B/C as supporting references.
- [x] Pass Gate 1B style lock.

### Phase 3: Character And Faction Continuity

- [x] Produce and approve the ARIA master identity sheet.
- [x] Produce ARIA boot, warning, damaged, neutral, and stable states.
- [x] Produce ARIA runtime avatar and portrait exports after Gate 6 approval and integrate the existing in-game focus-reticle icon.
- [x] Produce and approve Dalia continuity sheet.
- [x] Produce Dalia expression strip within `CHAR-DALIA-02_ExpressionPoseSheet.png`.
- [x] Produce Dalia opening pose references within `CHAR-DALIA-02_ExpressionPoseSheet.png`; final panel layers/cutouts remain Phase 7 work.
- [x] Produce and integrate the approved Dalia runtime portrait export after Gate 6 approval.
- [x] Produce and approve Samira continuity sheet.
- [x] Produce Samira expression strip within `CHAR-SAMIRA-02_ExpressionPoseSheet.png`.
- [x] Produce Samira opening pose references within `CHAR-SAMIRA-02_ExpressionPoseSheet.png`; final panel layers/cutouts remain Phase 7 work.
- [x] Produce and integrate the approved Samira runtime portrait export after Gate 6 approval.
- [x] Produce six Commander portrait candidates plus neutral fallback in `CHAR-COMMANDER-01_PortraitChoices.png`.
- [x] Produce pre-identity faceless Commander framing assets in `CHAR-COMMANDER-02_FacelessFraming.png`.
- [x] Produce JRC silhouette/equipment sheet in `FACTION-JRC-01_SilhouetteEquipment.png`.
- [x] Produce civilian/responder sheet in `CIVILIANS-01_RolesAndResponders.png`.
- [x] Produce Ash Line first-contact patrol sheet in `FACTION-ASH-01_FirstContactPatrol.png`, reserving Insurgent Male 05 exclusively for Qassem.
- [x] Run side-by-side model-to-illustration continuity review; evidence is stored under `evidence/visual/continuity/`.
- [x] Record prohibited drift for Dalia, Samira, ARIA, Commander, JRC, civilians, Ash Line, and Qassem-reservation rules.
- [x] Complete internal continuity review in `evidence/visual/continuity/CONTINUITY_VALIDATION.md` and include the package in Gate 6 evidence.
- [x] Pass Gate 2 internal continuity lock.

### Phase 4: Location, Prop, And Handoff Continuity

- [x] Produce Old Market geography sheet; Candidate B passed geometry, symbol, aspect, and ground-detail validation.
- [x] Produce living morning background master; Candidate B passed geography/activity continuity validation.
- [x] Produce attack/blackout background master; Candidate B passed persistent-landmark, dawn, damage, and restraint validation.
- [x] Produce M01 handoff background master from the operation-map contract; Candidate B passed `1672 x 941` aspect and route/anchor-readability validation.
- [x] Produce debrief/secured-corridor background master; Candidate B passed persistent-damage and reopened-route validation.
- [x] Produce damaged JRC terminal background master; room geometry, symbol safety, and `1672 x 941` aspect validated.
- [x] Produce Civic Relay terminal/ARIA boot treatment; approved ARIA identity remains distinct from physical Relay/terminal infrastructure.
- [x] Produce approved JRC, clinic, civil-defense, and Relay prop references; object coverage and symbol/text safety validated.
- [x] Produce reusable smoke, dust, light, blackout, and signal overlay references; visual separation validated, while final transparent layers correctly remain Phase 7 work.
- [x] Compare illustrated handoff frame against the operation-map anchor/scale contract and closest 3D vocabulary capture in `evidence/visual/world/WORLD_COMPARE_M01_HANDOFF.png`. No production M01 camera exists; approved `FL-P18` becomes the later 3D authority.
- [x] Correct landmark, sun, road, scale, camera, symbol, and aspect mismatches in the Old Market Candidate B set.
- [x] Validate all readable signs/text are removed or replaced by reviewed separate assets.
- [x] Complete internal world-continuity review in `evidence/visual/world/WORLD_CONTINUITY_VALIDATION.md` and include the package in Gate 6 evidence.
- [x] Pass Gate 3 internal world-continuity lock.

### Phase 5: Storyboard And Panel Manifest

- [x] Create one storyboard state for every inventory row in `storyboard/first_launch_storyboard.md` and separate the non-panel runtime states.
- [x] Map all seven `Command Lost` beats without omission.
- [x] Map both Commander identity states.
- [x] Map guidance choice.
- [x] Map all five Chapter 1 opening beats.
- [x] Map all three M01 brief beats.
- [x] Map illustrated-to-3D handoff.
- [x] Map gameplay placeholder and Jump To Debrief.
- [x] Map all three M01 debrief beats.
- [x] Map command-base reveal.
- [x] Define panel duration, camera move, transition, audio, subtitle, and reduced-motion behavior.
- [x] Define Skip destination for every state.
- [x] Define static fallback for every final visual state.
- [x] Complete the schema-valid 22-record draft of `first_launch_panel_manifest.json`.
- [x] Produce the full numbered storyboard contact sheet with 22 normalized `640 x 360` frames.
- [x] Review mobile composition, subtitle zones, faces, routes, evidence, and Skip controls in `first_launch_safe_area_contact_sheet_16x9.png` and `first_launch_safe_area_contact_sheet_20x9.png`.
- [x] Complete internal storyboard review and revisions, including rejected clinic-symbol, ARIA omission, remote-channel, P18 depth, and P20 subtitle-clearance candidates.
- [x] Pass Gate 4 internal storyboard lock in `storyboard/first_launch_storyboard_validation.md`.

### Phase 6: Animatic And Timing

- [x] Assemble storyboard frames into a timed animatic.
- [x] Add temporary subtitles with speaker identity.
- [x] Add Microsoft neural timing reads as offline WAV assets; release distribution remains subject to current-service/licensing review.
- [x] Add temporary market, blast, blackout, radio, ARIA boot, and transition audio.
- [x] Add restrained temporary motion and transitions.
- [x] Keep normal handoff at or before 90 seconds; validated at `88.5s`.
- [x] Validate identity and guidance interaction timing.
- [x] Validate Skip from early, middle, identity, and brief states.
- [x] Validate reviewer pause/step/jump behavior in the animatic plan.
- [x] Validate reduced-motion timing.
- [x] Produce first-pass and revised normal/reduced-motion animatic videos.
- [x] Produce `animatic/first_launch_timing_report.tsv` with 25 contiguous states.
- [x] Complete internal technical animatic review in `evidence/visual/animatic/ANIMATIC_VALIDATION.md`, including the revised presentation pass.
- [x] Add clearly audible living-market ambience before the attack; revised opening measures `-31.5dB` mean and `-15.1dB` peak from `2.5-17.5s`.
- [x] Replace the generic full-width subtitle rectangle direction with the user-approved graphic-novel reference.
- [x] Use the production ARIA focus-reticle icon and approved Dalia/Samira portraits in speaker-chrome references.
- [x] Define identity and guidance as separate live game UI references and exclude both states from the revised linear video.
- [x] Revise pacing so location, situation, Dalia, Samira, ARIA, JRC, civilian stakes, and the armed threat are clear before gameplay.
- [x] Produce `PRESENTATION_REVISION_CONTACT.png` and receive approval for the graphic-novel reference direction.
- [x] Lock 9-sliced speech-frame, separate portrait/icon, TMP, character-by-character reveal, punctuation pause, tap-to-complete, instant-text accessibility, and audio-failure contracts.
- [x] Rebuild and review the revised `176.5s` normal and reduced-motion reference animatics.
- [x] Pass Gate 5 revised presentation lock.

### Phase 7: Final Layered Art

- [x] Generate/paint each final-candidate background from locked references.
- [x] Generate/edit missing character poses one principal character at a time.
- [x] Composite characters with correct scale, perspective, light, and contact shadow.
- [x] Keep runtime-only UI/effects separate where required: P08 identity UI and P16 tactical routes are not baked into current masters.
- [x] Retouch through AI regeneration/selection for hands, faces, anatomy, weapons, vehicles, and architecture.
- [x] Remove generated text, logos, symbols, duplicated subjects, and artifacts; P12/P19 cross artifacts and P16 baked routes were explicitly rejected and corrected.
- [x] Apply the approved Match-aligned color/material pipeline across all panels.
- [x] Preserve character skin tone, materials, and wardrobe colors across lighting changes.
- [x] Save full-resolution source masters in the review workspace, outside runtime art folders.
- [x] Compare final panels against approved storyboard and style frames.
- [x] Record provenance and corrections for every offered final asset.
- [x] Create the numbered final-art contact sheet in narrative order.
- [x] Export a full-resolution review composite for every panel.
- [x] Export 20:9 and 16:9 phone-scale previews with subtitle and Skip safe-area overlays.
- [x] Create side-by-side storyboard, style, continuity, world-reference, and final-art comparisons.
- [x] Create a deterministic 44-second motion proof covering all 22 panels.
- [x] Present the complete final-art review package to the user through `ArtReview/FinalArt/GATE6_REVIEW_PACKAGE.md`.
- [x] Record `Approved`, `Changes Required`, or `Rejected` for every panel in the review ledger.
- [x] Confirm no user-rejected or changes-required panel remains in the approved package.
- [x] Receive explicit user approval for every final panel; this is the user approval portion of Gate 6.
- [x] Export runtime panel assets from the exact approved source revisions.
- [x] Export approved flattened static fallbacks in 16:9 and 20:9.
- [x] Configure Unity sprite imports with clamped wrapping, no mipmaps/readback, and ASTC 6x6 mobile compression candidates.
- [x] Create `approved_first_launch_art_manifest.json` and `APPROVED_RUNTIME_CONTACT_16x9.png`.
- [x] Verify all 44 runtime textures and Unity import metadata against the approved hashes in `APPROVED_RUNTIME_EXPORT_VALIDATION.json`.
- [x] Pass Gate 6 final-art package lock.

### Phase 8: Audio, Subtitles, And Accessibility

- [x] Write temporary/final English subtitle draft for scoped beats in `Assets/Game/Data/Narrative/FirstLaunch/first_launch_english_text_catalog.json`.
- [x] Assign stable localization keys; no visible text is hard-coded in art.
- [x] Define speaker labels and roles.
- [x] Add important non-speech captions where story meaning depends on sound.
- [x] Create or select temporary market, impact, radio, blackout, ARIA boot, and UI cues in `first_launch_audio_cue_plan.json`.
- [x] Create temporary score arc or licensed internal placeholder using the existing briefing loop.
- [x] Validate dialogue/audio rights and mark temporary assets clearly as internal-only and distribution-rights-unverified.
- [x] Implement subtitles on by default in the independent narrative settings model.
- [x] Implement subtitle size and background/opacity preview with four persisted presets and Unity visual evidence.
- [x] Implement auto-advance toggle with persisted defaults and deterministic presentation-helper consumption.
- [x] Implement reduced-motion behavior; the same state timeline advances while panel motion remains static, with focused bounds/timing tests and retained reviewer evidence.
- [x] Verify all critical facts remain understandable with audio muted; see `evidence/runtime/phase8/PHASE8_VALIDATION_REPORT.md`.
- [x] Verify maximum text expansion does not cover faces or Skip in `dialogue_max_expansion_2400x1080.png`.
- [x] Complete internal accessibility and presentation acceptance; Unity dialogue, maximum expansion, muted comprehension, temporary audio, and reduced-motion behavior pass retained evidence.
- [x] Pass Gate 7 internal accessibility/presentation lock.

### Phase 9: Reusable Unity Sequence Player

- [x] Add focused narrative config types under `Game.Configs`.
- [x] Add stable route/action contracts under `Game.UI.Contracts`.
- [x] Add `Game.Configs` dependency to `Game.UI.Runtime` after confirming no reverse dependency.
- [x] Build serialized `NarrativeSequenceView` prefab references.
- [x] Build serialized panel/layer views without runtime hierarchy lookup; V1 uses the approved flattened panels plus serialized dialogue, identity, guidance, confirmation, control, and motion roots.
- [x] Build serialized playback control view.
- [x] Implement `NarrativeSequencePresentationSystemHelper` timing and visual state.
- [x] Implement current/next asset preparation and release behavior with Addressables 3.1 `AssetReferenceSprite` handles; direct panel dependencies and temporary Resources wrappers are absent.
- [x] Use the approved flattened panel as the V1 background/character/foreground/effect/light composition; optional separated parallax layers are deferred rather than fabricating unapproved art.
- [x] Implement restrained PushIn, PullBack, DriftLeft, DriftRight, StaticImpact, Static, and StaticInteractive motion presets without exposing canvas edges.
- [x] Implement static fallback and reduced motion; all 22 flattened fallbacks remain static while the unchanged story timeline advances.
- [x] Implement subtitle and speaker binding.
- [x] Implement pause, resume, restart, previous, and next in the reusable player.
- [x] Implement typed completion/handoff result publication.
- [x] Implement safe presentation cancellation and composition-owned route wiring.
- [x] Implement missing-asset development diagnostics and release fallback; a missing optional panel logs in development and does not block state progression.
- [x] Build first-launch sequence, speaker, and punctuation config assets with 26 connected states, all 22 panels, and 17 voice lines.
- [x] Add config/view/architecture tests.
- [x] Pass Gate 8 reusable-player acceptance with the consolidated 23-test Addressables, motion, residency, presentation, player, reviewer, Menu, and profile suite.

### Phase 10: Review Mode, Skip, And Route Handoff

- [x] Build editor preview entry command at `Game/Narrative/First Launch/Review In Play Mode`.
- [x] Build editor capture command for standard, maximum-expansion, identity, guidance, and Skip-confirmation evidence.
- [x] Build development-only review overlay; it is hidden outside Editor/development contexts and does not write the production profile.
- [x] Implement Play/Pause/Restart/Previous/Next.
- [x] Implement panel/timeline position and deterministic seek.
- [x] Implement subtitle, safe-area, reduced-motion, and id overlays; all are live development reviewer controls and do not restart voice or write persistence.
- [x] Implement Jump To Debrief.
- [x] Implement Skip button visual and accessible label.
- [x] Implement default-identity/Full-Guidance confirmation path.
- [x] Implement identity-preserving skip path.
- [x] Implement typed M01 handoff payload and `HandoffPending` recovery.
- [x] Implement vision-only gameplay placeholder with reviewer Skip To Game and watched-handoff routing that do not mutate the review profile.
- [x] Implement debrief skip and mandatory clue completion payload; watched and skipped routes emit the same revoked-credential evidence and mission-context flags.
- [x] Implement command-base reveal arrival route; reviewer completion terminates without profile mutation or looping to the gameplay placeholder.
- [x] Prevent duplicate route transitions under repeated input.
- [x] Add Skip/startup/profile/routing tests.
- [x] Capture all review and Skip evidence, including every Skip checkpoint and the clean 174-second integrated Unity runtime playback with voice and ambience.
- [x] Complete internal review/skip acceptance with recorded visual, timing, audio-level, automated, and PlayMode evidence.
- [x] Pass Gate 9 review/skip acceptance.

### Phase 10R: First User Playback Revision

This revision is a hard prerequisite to M01. The first live Unity review supersedes the earlier internal visual/audio acceptance while preserving the accepted sequence-player, persistence, routing, Addressables, and panel-art architecture.

- [x] Record live-playback feedback rounds: initial text/control scale, frame artifact, environmental audio, and introduction issues; second review found generated background speech overlapping narration after Commander selection and longer dialogue lines clipping outside the fixed-height frame.
- [x] Capture and measure the current live dialogue, Skip, confirmation, identity, guidance, and reviewer controls at 16:9, 20:9, and tablet dimensions against the approved mockup and current HUD language. The first harness incorrectly used output size as its Canvas reference; live `Menu` uses `4800 x 2160`, making the old proof approximately `2.2x` too optimistic. Corrected GPU evidence uses the real Menu contract and is retained under `evidence/runtime/phase10r/`; headless gray captures were rejected and regenerated.
- [x] Increase standard dialogue body, speaker, role, and control typography for phone readability without relying on TMP auto-size. Standard body/name/role are fixed at `50/54/30 px`, and the narrative surfaces use a `2.2x` live presentation scale inside the existing `4800 x 2160` Menu canvas contract.
- [x] Reduce the default speech-frame footprint and unused interior space while retaining measured expansion for long/localized/accessibility text. Standard minimum height is `292 px`, Large/Extra Large minimum is `376 px`, and the frame grows upward from its bottom anchor using forced TMP mesh line metrics while respecting the live safe area. Ellipsis clipping is disabled.
- [x] Remove 9-slice edge, corner, alpha-fringe, and pointer-attachment artifacts at every required aspect and scale. Slice borders were corrected and the artifact-producing pointer attachment is intentionally disabled.
- [x] Increase shipping Skip, confirmation, identity, guidance, and popup touch targets and labels to the project's mobile HUD scale. Shipping command targets are at least `88 px` high.
- [x] Restyle shipping controls to use the current HUD visual language while preserving the approved graphic-novel dialogue treatment.
- [x] Increase and restyle development reviewer Pause, navigation, slider, toggle, and state controls so review mode remains legible and efficient.
- [x] Add a live, localization-ready `SAHRIN / OLD MARKET` location introduction over FL-P01; no title or context text is baked into panel art. The enlarged plate is top-left pivoted and verified fully inside the safe area.
- [x] Give the city introduction enough quiet visual and audio time to establish ordinary life before the attack. FL-P01 retains its 15-second dialogue-free establishment with city ambience and restrained score from frame one.
- [x] Give Dalia a distinct live introduction beat with approved portrait, name, role, voice, and readable hold before Samira speaks.
- [x] Give Samira a distinct live introduction beat with approved portrait, name, role, voice, and readable hold separate from Dalia.
- [x] Give ARIA a distinct activation/introduction beat with the production icon, role, boot cue, and non-human audio identity before Commander selection.
- [x] Integrate continuous live city/market, city-under-attack, and command-room ambience from sequence start, routed through the existing sound settings and paused/cancelled with the sequence.
- [x] Integrate dedicated restrained calm/crisis score loops through the existing music settings; playback does not begin as silence and remains mixed below voice.
- [x] Replace the rejected generic explosion/battlefield/objective mix with eight AI-generated FirstLaunch assets: calm/crisis score, city market, city attack, command room, convoy, distant attack, and emergency radio. Prompts, candidate metrics, rights note, and selected files are recorded under `Assets/Game/Audio/Narrative/FirstLaunch/`; generic large-explosion and transition cues are no longer wired. After second review, city-attack, command-room, and radio textures were regenerated with an explicit no-human-speech contract, and the radio event layer was disconnected from dialogue states.
- [x] Verify voice, music, ambience, vehicle/conflict cues, mute settings, pause/resume, seek/restart, Skip, and route cancellation remain independently controllable and leave no stale audio. Clip/state/mix policy lives in `Game.Composition`; `NarrativeSequenceAudioView` is a passive serialized-reference/audio presentation view; `Game.UI.Runtime` has no `Game.Configs` dependency. Focused presentation/audio validation passes `9/9`, and assembly-boundary validation passes `31/31`; final live playback remains part of the next item.
- [x] Re-run tests and capture complete 16:9/20:9/tablet normal, reduced-motion, subtitles-off, audio-layer, identity/guidance, Skip, and full-playback evidence; pass Gate 9R through another user live review before M01 begins. Corrected standard/long GPU layout evidence, `31/31` assembly-boundary checks, `29/29` focused checks, performance/residency validation, and strengthened `1/1` live Menu PlayMode integration pass through actual Commander/Guidance commits and single-source FL-P09 narration were retained from the July implementation batch. On 2026-08-12 the project owner approved the current FirstLaunch comic-style dialogue/story presentation and instructed Codex to continue M01. This is the required product acceptance; it does not claim that a new technical wrapper run occurred on that date.

### Phase 11: Visual, Device, Memory, And Regression QA

- [x] Run focused EditMode narrative config, player, interaction, startup, persistence, and architecture tests.
- [x] Run live Menu PlayMode playback and routing test through Addressables boot, reduced motion, all interactive states, gameplay placeholder, debrief Skip, and command-base arrival.
- [~] Run existing UI shell regression relevant to startup/routing; route/audio suite passes, broader content suite is blocked by pre-existing missing `statusChipSprite`.
- [x] Capture `1920 x 1080` normal playback.
- [x] Capture `2400 x 1080` normal playback.
- [x] Capture representative tablet landscape playback.
- [x] Capture maximum subtitle expansion.
- [x] Capture reduced-motion presentation and verify it in live PlayMode without changing story order.
- [x] Capture Skip from early, middle, identity, and final-opening checkpoints; route and idempotency behavior are covered by automated tests.
- [x] Verify no overlap among Skip, subtitles, faces, identity controls, and safe areas in retained 1920x1080 and 2400x1080 captures.
- [x] Verify no panel exposes empty canvas during motion with bounded overscan tests and retained 16:9/20:9 reviewer captures.
- [!] Verify illustrated-to-3D camera continuity; blocked because no production M01 handoff camera exists, and approved FL-P18 is the binding future authority.
- [x] Measure sequence load time and transition stutter.
- [x] Measure peak resident texture/audio memory.
- [x] Verify no recurring managed allocations during stable panel playback after warmup.
- [x] Verify offline playback and missing-optional-audio fallback.
- [x] Verify skip/review controls are absent from unintended retail routes.
- [x] Run cultural, symbol, equipment, and generated-art review.
- [x] Publish validation report with evidence links.
- [ ] Pass Gate 10 integrated acceptance.

### Phase 12: Closeout And Next-Slice Handoff

- [ ] Confirm all scoped sequences and interactive states are represented.
- [ ] Confirm all final assets are approved and provenance is complete.
- [ ] Confirm source files and runtime exports are separated.
- [ ] Confirm review mode remains available to the user/team.
- [ ] Confirm Skip enters the typed gameplay handoff safely.
- [ ] Confirm M01 gameplay placeholder is clearly isolated for replacement.
- [ ] Confirm README/design-index links are current.
- [ ] Record known limitations and deferred work.
- [ ] Record reusable assets approved for later sequences.
- [ ] Update progress summary to complete.
- [ ] Deliver the complete integrated slice and evidence package for non-blocking user review.
- [ ] Pass Gate 11 autonomously when all contract evidence is complete.

## Test Contract

Minimum automated coverage before Gate 10:

- Sequence configs reject duplicate sequence/panel ids.
- Every scoped sequence resolves all required panel/config references.
- Every panel has a static fallback.
- Every panel has a Skip destination and reduced-motion behavior.
- Normal completion publishes exactly one completion result.
- Skip publishes exactly one completion/handoff result.
- Rapid repeated Skip input cannot launch duplicate routes.
- Skip before identity uses the approved default identity and Full Guidance only after confirmation.
- Skip after identity preserves the selected identity and guidance.
- Debrief Skip preserves the mandatory clue/reveal state.
- Reviewer mode does not modify the production profile by default.
- Missing optional audio does not block progression.
- Missing required visual config fails development validation and uses the release fallback.
- Subtitle and Skip controls stay inside safe area at required viewports.
- Reduced-motion mode removes shake/aggressive parallax without changing duration or story order.
- Route exit releases playback state and prevents stale audio/subtitle continuation.
- Runtime UI code contains no hierarchy search or object-name discovery.
- Assembly dependencies remain directional and editor code stays outside runtime assemblies.

## Visual Acceptance Checklist

- The opening shows ordinary life before destruction.
- Sahrin is not reduced to an empty ruin or one-note desert palette.
- Dalia and Samira match their approved continuity sheets in every state.
- ARIA is recognizable in every state.
- JRC, civilians, and Ash Line are distinct without profiling local identity.
- Weapons, vehicles, uniforms, props, architecture, and lighting remain coherent.
- No malformed hands, faces, bodies, equipment, duplicate people, floating props, or impossible shadows remain.
- No real flags, extremist symbols, religious caricature, fake news branding, or unreviewed regional writing appear.
- The opening remains readable on a phone without zooming.
- Motion remains restrained and does not make subtitles or faces hard to read.
- Skip and playback controls remain visually subordinate to the story but immediately discoverable.
- The final illustrated Old Market frame matches the actual 3D handoff.
- The debrief feels like consequence, not a detached results advertisement.
- The command-base reveal feels earned and does not expose the full product complexity.

## Performance And Residency Targets

These are initial gates and must be replaced by measured device evidence when implementation begins:

- No recurring managed allocation during a stable panel after warmup.
- No per-frame asset lookup, hierarchy search, or string construction.
- Current and next panel assets may be resident; unrelated full-sequence source art may not remain loaded without evidence.
- Panel transition should not produce a visible frame hitch on the target development device.
- Optional motion/audio failure falls back without blocking progression.
- Static reduced-motion playback uses fewer active layers/effects than normal playback.
- Runtime source uses approved compressed exports, not 4800 x 2160 Design masters.
- Peak memory, load time, and frame pacing are recorded in `evidence/performance/` before Gate 10.

## Risk Register

| Risk | Early warning | Required mitigation |
|---|---|---|
| Cutscenes look unrelated to gameplay | Style frame and 3D capture differ in scale/material/camera | Use actual renders, lock handoff camera, require overlay comparison |
| Character drift | Face, age, uniform, or equipment changes between candidates | Lock continuity sheets; generate/edit one character at a time |
| Generic AI military art | Unrelated uniforms, vehicles, symbols, or stock composition | Reject before storyboard; preserve provenance and project references |
| City becomes a stereotype | Empty ruins, uniform sand/orange, civilians without agency | Require Living Sahrin frame and cultural review |
| Opening delays gameplay | Animatic exceeds 90-second handoff | Cut/merge panels without removing beats; preserve Skip |
| Skip leaves invalid profile state | Missing identity/guidance or duplicate route | Typed default confirmation, idempotent routing, automated tests |
| Reviewer cannot inspect defects | Only autoplay/video exists | Build step controls, overlays, captures, and Jump To Debrief |
| Text cannot localize | Text baked into images | Runtime localization keys only |
| Mobile memory spikes | Full 4K layers remain resident | Runtime exports, current/next loading, static fallback, measurement |
| Motion causes nausea/readability loss | Aggressive shake/parallax | Reduced-motion mode and restrained presets |
| Placeholder becomes production debt | Review placeholder silently ships | Explicit build/release validation and replacement gate |
| Story state diverges from sequence | Skip and watched paths publish different outcomes | Shared typed completion payload and equivalence tests |

## Approval And Validation Gates

| Gate | Owner and evidence | Work that must wait |
|---|---|---|
| Gate 0 | User approved this tracker and the art-first/autonomous-implementation amendment on 2026-07-10. | Source capture and generation |
| Gate 1A | Internal technical validation of labeled Unity/model/location sources; available to user on request. | Style generation |
| Gate 1B | Internal art-direction review selects one coherent style and records rejected alternatives. | Continuity and final visual direction |
| Gate 2 | Internal character/faction continuity review is complete and added to Gate 6 evidence. | Story panel generation |
| Gate 3 | Internal Old Market, terminal, prop, and handoff continuity review is complete. | Final storyboard lock |
| Gate 4 | Internal storyboard review confirms every canonical beat and mobile composition. | Animatic and final art |
| Gate 5 | Internal timed-animatic review confirms pacing, continuity, and Skip points. | Final layered art and runtime polish |
| Gate 6 | User reviews every numbered full-resolution final panel and phone-scale preview; every panel is explicitly approved. Agent then verifies approved exports. | Runtime art import and integrated sequence production |
| Gate 7 | Internal audio, subtitle, accessibility, and narrative-presentation evidence passes. | Integrated runtime acceptance |
| Gate 8 | Internal technical acceptance of reusable player controls and static fallback passes. | Route integration |
| Gate 9 | Internal reviewer-mode, full-playback, and Skip-to-game acceptance passes. | Final QA |
| Gate 10 | Internal device, performance, memory, and regression evidence passes. | Closeout |
| Gate 11 | All completion evidence is recorded and the integrated slice is delivered for non-blocking review. | Next narrative/gameplay slice |

## Immediate Next Action After Gate 0

Do not generate final panels yet.

1. Resolve Dalia, Samira, Commander proxy, JRC, civilian, and Ash Line configs to exact model/prefab paths.
2. Capture standardized Unity turnarounds and material/equipment close-ups.
3. Capture the intended Old Market handoff camera from the real 3D scene.
4. Complete the internal Gate 1A source validation and retain its contact sheet for optional user inspection.
5. Generate the four style-frame direction sets and present those to the user as the first required visual approval.

## Decision Log

| Date | Decision | State |
|---|---|---|
| 2026-07-10 | Build first-launch narrative vision before full gameplay implementation. | Accepted |
| 2026-07-10 | Use reusable continuity assets and layered motion-comic production. | Accepted |
| 2026-07-10 | Build reviewer mode for complete sequence inspection. | Accepted |
| 2026-07-10 | Include persistent Skip that enters the gameplay handoff safely. | Accepted |
| 2026-07-10 | Treat Unity source references as internal validation; require explicit user approval for every final-art panel before runtime export. | Accepted |
| 2026-07-10 | Complete all art before runtime implementation; after final-art approval, proceed through implementation and QA without further blocking approvals. | Accepted |
| 2026-07-10 | Use default neutral Commander identity plus Full Guidance only through explicit fresh-profile Skip confirmation. | Proposed in this tracker; Gate 0 approval required |

## Completion Definition

This tracker is complete only when the user can launch the first narrative slice, review it panel by panel, watch it in real time, inspect subtitles and reduced motion, skip from every required checkpoint into the gameplay handoff, jump over the gameplay placeholder to review the debrief, and see the command-base reveal, with approved art continuity and recorded device/performance evidence.
