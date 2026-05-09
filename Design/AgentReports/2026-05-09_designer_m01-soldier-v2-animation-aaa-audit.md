# Lane
Designer

# Task
Focused Designer audit of the Art/Atlas v2 M01 soldier animation atlas package before PM/user acceptance.

# Files changed
- `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`

# Contracts touched
- No product contracts changed.
- Reviewed against:
  - `Design/AgentTasks/designer_current.md`
  - `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
  - `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_soldier_animation_manifest_v2.md`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_sources_v2_contact.png`

# User-visible behavior
No runtime behavior changed by Designer.

# Validation run
- Read Designer current task and latest Art/Atlas current task.
- Read the Art/Atlas v2 handoff report.
- Read runtime and review v2 soldier animation manifests.
- Viewed:
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_contact_v2.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_soldier_animation_sources_v2_contact.png`
- Verified manifest coverage:
  - two factions: `player_rifle_squad`, `enemy_patrol`
  - four facings: `NE`, `SE`, `SW`, `NW`
  - states: `idle`, `run`, `aim`, `fire`, `damaged`, `death`
  - frame counts per facing: idle 4, run 8, aim 3, fire 4, damaged 3, death 6
  - runtime frame PNG count: 224 total
  - v2 runtime atlases: 4096x1792 RGBA for both factions
- Confirmed manifest `frame_count` values match frame arrays.

# Validation result
accept with minor notes for current M01 Gameplay integration audit

Designer does not give final art signoff yet. The package is strong enough for Gameplay to audit runtime import/readability, but PM/user should preserve the notes below before treating it as final AAA production art.

# Audit findings

## Required states
Pass. Player rifle squad and enemy patrol both include the required M01 states:

- idle
- run
- aim
- fire
- damaged
- death

Frame counts meet the current Art/Atlas requirement for every state/facing pair.

## Facing coverage
Pass for current M01. Four facings are acceptable for the current true-isometric M01 target and existing four-facing asset/runtime direction.

Do not reinterpret this as approval for a future eight-facing production contract. If Gameplay or camera design later requires eight-direction turning, this package must be expanded before that later requirement can pass.

## Frame-to-frame animation read
Pass with notes. The v2 package is materially better than the rejected repeated-pose handoff:

- run sequences show visible footfall/body progression,
- damaged and death sequences read as actual multi-frame reactions,
- fire sequences include muzzle/recoil/settle beats,
- idle remains subtle but is not a repeated still set.

The weakest reads are idle and some aim/fire transitions, which can feel abrupt at contact-sheet scale. This is acceptable for M01 import review but should be checked in runtime at actual camera scale before final acceptance.

## Pose, grounding, scale, silhouette, lighting, rotation
Mostly pass. The player and enemy factions are stylistically separated, readable, and broadly consistent with the approved isometric military visual target.

Notes:

- Some run frames show minor foot-contact jitter that may become sliding in motion.
- Some fire/death/damaged starts include impact or muzzle flashes that need runtime timing review so death does not read as firing.
- Minor crop/keying speckles and guide-like artifacts are visible in parts of the contact sheet; Gameplay/QA should verify they do not appear as vertical lines or stray pixels in-engine.

## Premium AAA mobile bar
Pass for implementation audit, not final art signoff. The v2 package is no longer placeholder-board quality and has enough premium detail to test in ECS/atlas runtime.

Final AAA approval still needs runtime capture comparison against the approved VisualLock style at actual M01 camera scale, because contact sheets cannot prove motion timing, silhouette stability, or selected-readability in live context.

# Rejected states/facings
No state or facing is rejected outright from Designer scope.

Designer flags the following for Gameplay/QA attention during import:

- verify run cycles do not slide or stutter at runtime speed,
- verify alpha/keying speckles are not visible in-game,
- verify death frames read as dying, not firing,
- verify aim/fire transitions do not rotate the soldier away from the M01 camera language,
- verify four-facing selection is enough for all current M01 movement/engagement angles.

# Recommendation
accept with minor notes

Route Gameplay to perform the runtime atlas integration audit against `m01_soldier_animation_manifest_v2.json`. Do not ask for final PM/user art approval until Gameplay confirms runtime playback, scale, alpha cleanup, and camera-facing behavior.

# Known gaps
- Designer did not run Unity or import the atlas.
- This audit is visual/design review of the package and manifests, not runtime validation.
- Four facings are accepted only for the current M01 scope.

# Cross-lane impacts
- Gameplay should proceed with the v2 runtime audit if PM agrees.
- QA/HCI should later validate selected-readability with fresh runtime captures/video if Gameplay import passes.
- Art/Atlas should remain available for cleanup if Gameplay finds alpha artifacts, sliding, or state timing issues.

# Next recommended task
Gameplay should write:

`Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`

That report should verify manifest import feasibility, runtime animation playback, alpha cleanliness, four-facing behavior, and selected-readability at the actual M01 camera scale.
