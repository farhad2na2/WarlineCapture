# PM Art/Atlas Strategic Approved Soldier Animation Rejected

## Lane

PM

## Task

Record the user's latest Art/Atlas review: the regenerated strategic map is approved, but the soldier sprite animation is rejected because it is static one-frame state art rather than frame-by-frame animation.

## Files changed

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/README.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-strategic-approved-soldier-animation-rejected.md`

## Contracts touched

- `Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/VL_M01_Atlas_StateGuide_IdleRunAimFireDeathDestroyed.png`

## User-visible behavior

No runtime behavior changed. The strategic map is approved for downstream use, but soldier animation remains blocked.

## Validation run

- Read the active Art/Atlas, Gameplay, and QA/HCI task files.
- Routed the user approval/rejection into lane-readable task files.
- Added minimum frame-count requirements and manifest metadata requirements for animated soldier atlases.

## Validation result

Needs fixes for soldier animation. Static one-frame poses per state/facing are not accepted.

Accepted:

- Regenerated strategic map.

Rejected:

- Player/enemy soldier sprite outputs that only provide one static image for each state/facing.

Required fix:

- Player rifle squad and enemy patrol remain separate sheets/manifests.
- Every required facing must include multi-frame animation sequences:
  - idle: 4 loopable frames,
  - run: 8 loopable frames,
  - aim: 3 frames,
  - shoot/fire: 4 frames,
  - hit/damaged: 3 frames,
  - die/death: 6 non-looping frames.
- Manifest must include state id, facing id, frame order, frame count, suggested fps, loop/non-loop flag, atlas rects or individual frame paths, runtime path, and review path.

## Known gaps

- Art/Atlas must regenerate animated soldier atlases.
- Gameplay must not wire the current static soldier sheets as final animated art.

## Cross-lane impacts

- Art/Atlas owns the next correction.
- Gameplay remains blocked on soldier animation assets.
- QA/HCI remains blocked until Art/Atlas fixes the animated soldier atlas and Gameplay captures it in runtime.

## Next recommended task

Art/Atlas should write `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix.md` after replacing the static soldier state images with true multi-frame animation atlases.
