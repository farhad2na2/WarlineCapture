Lane:
Art/Atlas

Task:
Assess and package the current M01 infantry atlas/source art for Gate 4 readability and art-readiness decisions.

Files changed:
- `Design/AgentReports/2026-05-08_art-atlas_m01-infantry-atlas-readiness.md`

Contracts touched:
- No source contract changed.
- Reviewed M01 runtime ids and atlas/readability requirements for `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, `marker.selection.ring`, `vfx.impact.light`, and `vfx.unit.destroyed.small`.

User-visible behavior:
No runtime behavior changed in this Art/Atlas pass. Current public M01 visual readiness remains dependent on Gameplay integrating approved atlas frames and QA/HCI rerunning Gate 4.

Validation run:
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`.
- Read `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-focused-rerun-review.md`.
- Read `Design/AgentTasks/gameplay_current.md`.
- Read `Design/AgentTasks/M01_CRITICAL_PATH.md`.
- Checked `Design/WarlineCapture_M01_FirstContact_Production_Contract.md` for M01 unit, marker, projectile/VFX, and selected-state requirements.
- Reviewed relevant handoffs:
  - `Design/AgentReports/2026-05-08_gameplay_m01-final-atlas-runtime-blocker.md`
  - `Design/AgentReports/2026-05-08_pm_gameplay-m01-ecs-atlas-presentation-review.md`
  - `Design/AgentReports/2026-05-08_pm_m01-squad-selection-projectile-art-blocker.md`
- Inspected current candidate art/source package:
  - `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`
  - `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/Manifests/Unit_Chr_Soldier_Male_02_FullSetup_Manifest.json`
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-08_RifleSquad.png`
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-17_SelectionRing.png`
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/selection_ring.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- Checked current runtime catalog state in `Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs`.
- Checked for generated 2DISO VFX assets under `Assets/Game/Art/Generated/2DISO/VFX`.

Validation result:
Needs PM/user art approval before Gate 4 can treat the current infantry art as milestone-ready. The current package is suitable as a focused temporary-art approval package, not final art.

Handoff assessment:
- `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`: accepted as a valid needs-fixes HCI report. It correctly blocks Gate 4 on public readability, selected-state clarity, HUD scope mismatch, and `FinalAtlasArtReady = 0`.
- `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-focused-rerun-review.md`: accepted as the active PM routing report for Art/Atlas scope.
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-ecs-atlas-presentation-review.md`: accepted for architecture direction only. It still leaves final/milestone art approval open.
- `Design/AgentReports/2026-05-08_gameplay_m01-final-atlas-runtime-blocker.md`: accepted as historical blocker context, but superseded where PM later accepted the ECS atlas quad architecture. Its art-approval gap remains valid.
- `Design/AgentReports/2026-05-08_pm_m01-squad-selection-projectile-art-blocker.md`: accepted as active visual-readability criteria.

Prepared temporary-art approval package:
- Player rifle squad source candidate: `Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`.
- Covered player states in the sheet: idle, walk, run, aim, fire, reload, hit, death across NE, SE, SW, NW.
- Maps to required M01 player states as temporary approval only:
  - idle -> idle row
  - move -> walk row
  - attack -> fire row, with aim row usable as pre-fire pose if Gameplay supports it later
  - damaged/hit -> hit row
  - death/destroyed -> death row
- Current four-soldier squad visual reference: `GA-08_RifleSquad.png`.
- Selected-state visual reference: `GA-17_SelectionRing.png` and runtime marker `selection_ring.png`.
- Projectile/impact scale reference: no generated final VFX asset exists under `Assets/Game/Art/Generated/2DISO/VFX`; current acceptance must rely on Gameplay trace sizing plus future `vfx.impact.light` art.

Public camera-scale readability notes:
- `GA-08_RifleSquad.png` reads clearly as four soldiers at source scale, but it is a single baked squad image and does not satisfy the desired runtime identity of four distinct soldiers under one squad entity.
- The 4-facing/8-state soldier sheet provides individual soldier frames and is a better source for a four-soldier runtime formation, but it is key-pose only and the manifest explicitly says it is not final multi-frame animation.
- At current public capture scale, QA/HCI already found the squad too small and selected state unclear. Art cannot mark this final without a Gameplay capture showing the approved soldier frames at final scale/framing with the selection marker.
- Selection art exists, but current captures do not prove the marker is readable around the squad. Gameplay should scale/position the marker against the four-soldier formation and provide public first-control captures.

Known gaps:
- No Art/PM-approved final or milestone atlas assignment exists for `unit.player.rifle_squad_01` and `unit.enemy.patrol_01`.
- No enemy-tinted infantry patrol variant is present in the reviewed package. The current soldier sheet is friendly blue-accented; using it for enemy patrol requires either a red-accent variant or an explicit temporary tint/material approval.
- The candidate sheet is 4-facing/8-state key poses, not final multi-frame animation loops.
- `FinalAtlasArtReady` remains `0` in `Chapter01M01SpritePresenterCatalog`.
- `vfx.impact.light` and `vfx.unit.destroyed.small` planned art are not present under the expected generated 2DISO VFX folder.
- Art/Atlas cannot validate final public readability without a Gameplay-integrated public first-control capture using the selected package.

Cross-lane impacts:
- PM/user must decide whether the current soldier sheet can be milestone-approved for Gate 4 temporary art.
- Gameplay owns integrating the approved source into the ECS atlas quad path for the player squad and enemy patrol, including four-soldier formation spacing, selected-marker scale/position, and public captures.
- Gameplay/VFX owns tactical projectile trace and impact runtime scale until final `vfx.impact.light` art exists.
- UI owns the separate M01 HUD affordance mismatch.
- QA/HCI owns final Gate 4 rerun after Art/Atlas approval decision and Gameplay/UI integration land.

Next recommended task:
PM/user should approve or reject the temporary-art package:
- Approve temporary Gate 4 art: use `Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png` as milestone source for `unit.player.rifle_squad_01`, require a red-accent/tinted enemy patrol variant or explicit tint waiver for `unit.enemy.patrol_01`, keep `FinalAtlasArtReady = 0`, and route Gameplay to produce public first-control captures proving four-soldier readability and selected-state clarity.
- Reject temporary Gate 4 art: Art/Atlas must generate or source final/milestone infantry atlas frames plus enemy variant and VFX/impact assets before Gameplay can claim visual readiness.

Waiting on lane:
PM/user, then Gameplay

Waiting on exact file/report/asset/command:
- PM/user approval or rejection of `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png` as Gate 4 temporary M01 infantry atlas source.
- If approved, Gameplay follow-up report and captures proving integrated public readability.
- If rejected, Art/Atlas generation/source package for final or milestone player/enemy infantry atlas frames and VFX/impact art.

Owner of next action:
PM/user owns the art approval decision. Gameplay owns integration and public capture proof after approval. Art/Atlas owns new source generation only if PM/user rejects the current temporary package or requests a red-accent enemy variant package.

Can my lane still continue fallback work? no.
