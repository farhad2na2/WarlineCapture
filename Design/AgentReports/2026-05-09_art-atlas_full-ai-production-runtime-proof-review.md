# Art/Atlas Full AI Production Runtime Proof Review

## Lane

Art/Atlas

## Task

Review Gameplay's full M01 AI production art runtime integration proof against the current Art/Atlas source-of-truth task.

## Handoff assessment

- `Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md`: needs fixes before PM/user art approval.
- `Design/AgentReports/2026-05-09_pm_gameplay-soldier-only-proof-partial.md`: accepted as prior PM routing context.
- Art/Atlas asset package remains unchanged. This review does not request new source art generation yet.

## Files changed

- `Design/AgentReports/2026-05-09_art-atlas_full-ai-production-runtime-proof-review.md`

## Contracts touched

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_heartbeat.md`
- `Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/TacticalMaps/m01_tactical_plate_a_source.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/TacticalMaps/m01_tactical_plate_a_pot_2048x1024.png`

## User-visible behavior

Gameplay captures now show v2 soldiers over a production tactical ground texture, but the runtime view is not ready for PM/user visual acceptance as the full M01 AI production art presentation.

## Validation run

- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Checked latest reports in `Design/AgentReports/`.
- Read `Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md`.
- Reviewed Gameplay proof captures:
  - `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-selected-player-idle.png`
  - `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-selected-player-run.png`
  - `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-enemy-patrol.png`
- Compared against:
  - `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
  - `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/TacticalMaps/m01_tactical_plate_a_source.png`
  - `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_maps_contact.png`

## Validation result

Needs fixes before PM/user art approval.

Positive findings:

- V2 player and enemy soldiers are visible in the full-runtime captures.
- No obvious soldier alpha speckle or atlas edge bleed is visible at capture scale.
- Runtime is no longer showing the old clean tan tactical map from the previous soldier-only proof.
- The active plate source itself is a valid production tactical plate asset.

Blocking visual/readability findings:

- Runtime framing/scale appears wrong for tactical plate A. The captures show a close asphalt/crack texture crop rather than the full readable road/intersection/wall composition visible in `m01_tactical_plate_a_source.png`.
- The current runtime view does not match the approved close tactical target composition: roads, sidewalks, walls/building context, and tactical landmarks are largely absent from the camera view.
- Command-support building art is not visibly assessable in the proof captures despite the Gameplay report saying command-point decor resolves to production building sprites.
- Soldiers read smaller and less grounded against the oversized terrain texture than they do in the approved target board.
- Selection rings are present but too subtle to serve as approval evidence for marker readability.
- The proof does not yet establish that tactical map plate scale, camera crop, building placement, marker footprint, and soldier scale are working together as a coherent approved M01 presentation.

## Art/Atlas assessment

This does not look like an Art/Atlas source-asset failure yet. The source tactical plate and v2 soldier atlas remain usable. The issue appears to be runtime placement, scale, crop, or active plate presentation:

- use the full tactical plate composition at the intended M01 camera scale,
- ensure production building/prop sprites are visible and scale-checked in the proof frame,
- bring marker presentation up to approved selected-readability footprint,
- produce a capture that shows the road/intersection/wall/building context from the approved production target, not only a zoomed asphalt patch.

Art/Atlas should not generate new art or repack assets until Gameplay/PM identifies a concrete source-art defect after corrected runtime framing.

## Known gaps

- No PM/user final art approval yet.
- No QA/HCI selected-readability validation yet.
- Strategic Saga Map display remains outside this tactical runtime proof.

## Next recommended task

Gameplay should revise runtime framing/scale/placement and produce a new full AI production runtime proof. If the corrected proof still exposes source-art issues, route a specific Art/Atlas fix with the exact asset id, capture path, and requested change.
