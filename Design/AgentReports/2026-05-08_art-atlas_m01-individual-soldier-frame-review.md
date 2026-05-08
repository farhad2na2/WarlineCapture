Lane:
Art/Atlas

Task:
Review whether the current M01 atlas/source frame makes each runtime soldier read like a mini-squad/cluster instead of one individual soldier after PM rejected the selected first-control captures.

Files changed:
- `Design/AgentReports/2026-05-08_art-atlas_m01-individual-soldier-frame-review.md`

Contracts touched:
- No source contract files changed.
- Art/Atlas confirms the M01 metric/readability contract still stands: four distinct soldiers under one squad entity, small grounded selection treatment, ECS atlas-backed presentation, and death/destroyed as atlas state rather than a separate `Destroyed` child.

User-visible behavior:
No runtime behavior changed by this Art/Atlas pass. This is a focused source-frame recommendation for Gameplay and the next QA/HCI rerun.

Validation run:
- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read `Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`.
- Read `Design/AgentReports/2026-05-08_pm_qa-hci-rejected-art-rerun-review.md`.
- Read `Design/AgentReports/2026-05-08_art-atlas_m01-rejected-temp-art-scale-readability.md`.
- Read `Design/WarlineCapture_M01_Metric_Scale_Readability_Contract.md`.
- Reviewed `Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset`.
- Reviewed `Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_scale_contract.asset`.
- Reviewed `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/Manifests/Unit_Chr_Soldier_Male_02_FullSetup_Manifest.json`.
- Reviewed `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png.meta`.
- Reviewed `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`.
- Reviewed `Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs`.
- Reviewed `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`.

Validation result:
needs fixes

Handoff assessment:
- `Design/AgentReports/2026-05-08_qa-hci_gate4-rejected-art-rerun.md`: accepted as automated validation evidence, but not accepted as user-review readiness because PM found the selected captures visually unclear.
- `Design/AgentReports/2026-05-08_pm_qa-hci-rejected-art-rerun-review.md`: accepted as the current routing source for Art/Atlas. The world squad and selected state need another fix before user approval.

Frame/source finding:
- The current runtime manifest source is unsuitable for the four-soldier player formation. `unit.player.rifle_squad_01` and `unit.enemy.patrol_01` still resolve to `Assets/Game/Art/Generated/IsometricMaps/TacticalProductionBatch_A/Sprites/infantry_squad.png`.
- That PNG is a squad/group image. Duplicating it four times in `MissionRuntimeAtlasQuadPresentationSystem` makes each runtime "soldier" read as a mini-squad, which explains PM's crowded blob/duplicated-cluster finding.
- The state ids `unit.player.rifle_squad_01.idle`, `.move`, `.attack`, and `.damaged` are not present as distinct manifest entries. `Chapter01M01SpriteAssetResolver` strips those suffixes and falls back to the same base `unit.player.rifle_squad_01` asset, so the visual source remains the group sprite.

Required individual-soldier replacement:
- Use the existing individual-soldier sheet as the temporary M01 source instead of `infantry_squad.png`:
  - `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`
- The sheet is already imported as a multiple-sprite grid with 240x210 cells, bottom-center pivots, four facings, and these key-pose rows: idle, walk, run, aim, fire, reload, hit, death.
- For the next temporary rerun, the minimum recommended SE-facing state mapping is:
  - `unit.player.rifle_squad_01.idle` -> `Unit_Chr_Soldier_Male_02_Idle_SE`
  - `unit.player.rifle_squad_01.move` -> `Unit_Chr_Soldier_Male_02_Run_SE` or `Unit_Chr_Soldier_Male_02_Walk_SE` if the move speed reads as a jog
  - `unit.player.rifle_squad_01.attack` -> `Unit_Chr_Soldier_Male_02_Aim_SE` or `Unit_Chr_Soldier_Male_02_Fire_SE`
  - `unit.player.rifle_squad_01.damaged` -> `Unit_Chr_Soldier_Male_02_Hit_SE`
  - destroyed/death remains an atlas state using `Unit_Chr_Soldier_Male_02_Death_SE` or the existing `vfx.unit.destroyed.small` overlay route; do not reintroduce separate child `Destroyed` visuals.
- Enemy patrol can use the same individual-soldier cells with the existing temporary enemy tint, or a red-accent variant if PM requires stronger hostile readability. The lack of a final enemy variant remains a final-art gap, not a reason to keep the group sprite.

Gameplay layout/marker recommendation after source replacement:
- Once each renderer uses a single-soldier cell, keep four ECS atlas quads under one controllable player squad entity.
- Formation spacing should visibly separate the four footprints at public camera scale; if the next capture still reads crowded, widen the local offsets before changing art again.
- Selection should read as one small grounded marker per soldier, aligned to each soldier footprint. The current treatment should be more visible in capture than the last rerun, but it must stay below the soldiers and must not become a large formation plate.
- The marker should read warm/amber or low-saturation tactical ground light, not the previous large green/blue overlay.

Known gaps:
- Final art is still not ready. `FinalAtlasArtReady` should remain `0`.
- The individual-soldier sheet is key-pose temporary art, not final multi-frame animation.
- Final multi-frame run/walk loops remain missing.
- Final enemy red-accent variant remains missing.
- Final impact/death VFX remains missing.
- Unit card/icon art should not reuse the clustered `infantry_squad.png` source for the user review pass; use an individual soldier crop or an intentionally separated four-soldier roster icon if PM later routes UI/icon work.

Cross-lane impacts:
- Gameplay owns replacing the runtime/manifest source so the M01 player squad duplicates individual soldier cells, not the old group sprite.
- Gameplay owns any formation offset and marker visibility tuning needed after the source replacement.
- QA/HCI should rerun selected first-control captures after Gameplay lands the source/layout/marker fix.
- PM should not send the current selected captures to the user.
- UI has no immediate owner action unless PM separately routes the unit card/icon cluster issue.

Next recommended task:
Gameplay should deliver `Design/AgentReports/2026-05-08_gameplay_m01-soldier-readability-selection-fix.md` by wiring M01 infantry presentation to the individual `Unit_Chr_Soldier_Male_02` cells, preserving the `0.20` infantry scale direction, making per-soldier selection markers clearly visible, and then handing to QA/HCI for a focused selected first-control rerun.
