Lane:
Art/Atlas

Task:
Assess Gameplay's M01 individual-soldier source/layout/selection fix against the Art/Atlas frame review.

Files changed:
- `Design/AgentReports/2026-05-08_art-atlas_gameplay-soldier-readability-selection-review.md`

Contracts touched:
- No source contracts changed by Art/Atlas.
- Confirms the M01 temporary readability direction remains: individual soldier atlas cells, four distinct soldier quads under one squad entity, small grounded per-soldier selection markers, and no separate child `Destroyed` visual.

User-visible behavior:
No runtime behavior changed by Art/Atlas in this pass. Gameplay reports the visible player squad now reads as four separate soldier quads instead of four duplicated mini-squad sprites, with small warm/amber foot markers.

Validation run:
- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read `Design/AgentReports/2026-05-08_pm_art-atlas-individual-soldier-frame-review.md`.
- Read `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`.
- Reviewed `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`.
- Reviewed `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`.
- Spot-checked test assertions in:
  - `Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs`
  - `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

Validation result:
accepted

Handoff assessment:
- `Design/AgentReports/2026-05-08_pm_art-atlas-individual-soldier-frame-review.md`: accepted. PM correctly routed the source fix to Gameplay and kept Art/Atlas waiting unless a missing sprite/manifest blocker appeared.
- `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`: accepted for Art/Atlas scope. Gameplay wired player/enemy `.idle`, `.move`, `.attack`, and `.damaged` state ids to individual `Unit_Chr_Soldier_Male_02_*_SE` cells before the manifest fallback can reuse `infantry_squad.png`.

Art/Atlas findings:
- The resolver now maps M01 player/enemy state ids to the individual-soldier sheet:
  - idle -> `Unit_Chr_Soldier_Male_02_Idle_SE`
  - move -> `Unit_Chr_Soldier_Male_02_Run_SE`
  - attack -> `Unit_Chr_Soldier_Male_02_Aim_SE`
  - damaged -> `Unit_Chr_Soldier_Male_02_Hit_SE`
- Runtime soldier renderers are now created as explicit soldier children rather than treating the root quad as soldier one plus three children.
- Player squad offsets are widened into a readable row/formation, which matches the Art/Atlas recommendation to fix spacing after replacing the group source.
- Selection marker material is warm/amber and per-soldier. Marker scale is still small relative to the formation and remains grounded under each soldier footprint rather than becoming a large formation overlay.
- Death/destroyed was not broadened back into a separate child visual path.

Known gaps:
- Final art is still not ready. `FinalAtlasArtReady` should remain `0`.
- The `Unit_Chr_Soldier_Male_02` sheet is still key-pose temporary art, not final multi-frame animation.
- Enemy patrol uses the same individual soldier sheet with temporary tint; final enemy red-accent/final patrol variant remains missing.
- Final impact VFX and final destroyed/death VFX remain missing.
- UI/unit-card icon polish remains separate if PM routes it.

Cross-lane impacts:
- QA/HCI can rerun or review the fresh selected first-control captures from Gameplay.
- PM/user should still wait for QA/HCI's focused visual readiness review before any new art approval request.
- Art/Atlas has no further actionable work unless QA/HCI or PM reports a concrete sprite, marker, enemy variant, VFX, or approval blocker.

Next recommended task:
QA/HCI should review the refreshed 16:9 and 20:9 selected first-control captures and report whether the individual-soldier source/layout/selection fix is ready for PM/user review.

Waiting on lane:
QA/HCI

Waiting on exact file/report/asset/command:
- Focused QA/HCI review or rerun report for `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md`

Owner of next action:
QA/HCI

Can my lane still continue fallback work? no
