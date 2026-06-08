Lane:
Art/Atlas

Task:
Provide the M01 art-side marker, animation-frame, scale/aspect, and enemy/patrol visual package after the user rejected the selected-readability pass.

Files changed:
- `Design/AgentReports/2026-05-08_art-atlas_m01-marker-animation-scale-package.md`

Contracts touched:
- No source/runtime contract files changed by Art/Atlas.
- Art/Atlas accepts the updated Designer rejection contract as the current art review bar:
  - `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`
  - `Design/M01_Metric_Scale_Readability_Contract.md`
  - `Design/M01_FirstContact_Production_Contract.md`

User-visible behavior:
No runtime behavior changed by Art/Atlas in this pass. This report defines the art/frame/marker package Gameplay should implement before PM asks for another selected-readability review.

Validation run:
- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Read `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`.
- Read `Design/AgentTasks/art-atlas_pm_message.md`.
- Read `Design/AgentTasks/user_feedback_review_gate.md`.
- Read `Design/M01_FirstContact_Production_Contract.md`.
- Read `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`.
- Read `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`.
- Reviewed `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/Manifests/Unit_Chr_Soldier_Male_02_FullSetup_Manifest.json`.
- Reviewed `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`.
- Reviewed marker assets:
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/selection_ring.png`
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/move_destination_ring.png`
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/attack_target_ring.png`
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-17_SelectionRing.png`
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-18_MoveMarker.png`
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-19_AttackMarker.png`
- Checked marker dimensions with `sips -g pixelWidth -g pixelHeight`.

Validation result:
needs fixes

Handoff assessment:
- `Design/AgentReports/2026-05-08_pm_selected-readability-rejected-process-failure.md`: accepted as the active user-feedback gate. The rejection is blocking and must not be treated as polish.
- `Design/AgentReports/2026-05-08_designer_m01-rejection-scale-marker-contract.md`: accepted. It correctly updates scale, marker, animation, ECS presentation, and regression-matrix requirements.
- `Design/AgentReports/2026-05-08_ui_m01-marker-selection-overlay-audit.md`: accepted for Art/Atlas scope. UI removed the harmful static HUD marker preview path; the remaining selected square/marker issue is Gameplay runtime marker output with Art/Atlas style ownership.

Atlas/frame package:
- Temporary source sheet:
  - `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`
- Manifest:
  - `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/Manifests/Unit_Chr_Soldier_Male_02_FullSetup_Manifest.json`
- Import/cell contract:
  - 4 columns: `NE`, `SE`, `SW`, `NW`
  - 8 rows: `idle`, `walk`, `run`, `aim`, `fire`, `reload`, `hit`, `death`
  - cell size: `240x210`
  - pivot: bottom center

Required frame mapping for the next implementation:
- Player idle/selected/hold:
  - `unit.player.rifle_squad_01.idle` -> `Unit_Chr_Soldier_Male_02_Idle_SE`
- Player moving:
  - Preferred: `unit.player.rifle_squad_01.move` -> `Unit_Chr_Soldier_Male_02_Run_SE`
  - Acceptable if movement reads more like a jog: `Unit_Chr_Soldier_Male_02_Walk_SE`
- Player attack:
  - `unit.player.rifle_squad_01.attack` -> `Unit_Chr_Soldier_Male_02_Aim_SE`
  - Use `Unit_Chr_Soldier_Male_02_Fire_SE` only for a short firing beat, not as the sustained attack idle.
- Player damaged:
  - `unit.player.rifle_squad_01.damaged` -> `Unit_Chr_Soldier_Male_02_Hit_SE` only for a short hit reaction. Do not use it for idle, move, hold, selected, or enemy patrol default.
- Player destroyed/death:
  - `Unit_Chr_Soldier_Male_02_Death_SE` is an atlas death state only. Keep destroyed/death as atlas/VFX state; do not restore a separate child `Destroyed` visual.
- Enemy patrol idle/hold:
  - `unit.enemy.patrol_01.idle` -> `Unit_Chr_Soldier_Male_02_Idle_SW` if facing the player reads better, otherwise `Unit_Chr_Soldier_Male_02_Idle_SE`.
- Enemy patrol moving:
  - `unit.enemy.patrol_01.move` -> `Unit_Chr_Soldier_Male_02_Walk_SW` or `Unit_Chr_Soldier_Male_02_Run_SW`; avoid any hit/death row while the enemy is alive and patroling.
- Enemy patrol attack:
  - `unit.enemy.patrol_01.attack` -> `Unit_Chr_Soldier_Male_02_Aim_SW` or a short `Unit_Chr_Soldier_Male_02_Fire_SW` beat.

Animation/artifact guidance:
- Do not use row `hit` or row `death` for normal movement, idle, selected, patrol hold, or target-highlight states. Those rows visibly include seated/falling/prone poses and are the likely source of the "red flashing sitting enemy/object" rejection.
- Do not use `reload` as an idle substitute; it reads as a weapon-handling pose, not normal standing idle.
- Normal movement must use `walk` or `run` row frames only. A bob/phase effect is acceptable, but it must not distort the frame aspect or make the soldier look squashed.
- If a top/foot artifact appears in public capture, Gameplay should clamp the UV rect to the exact 240x210 sprite cell and avoid sampling neighboring rows. Art/Atlas cannot approve any capture that shows feet/top fragments from an adjacent cell.
- Idle animation is not final multi-frame art. For this gate, acceptable temporary idle is a standing idle frame plus a subtle breathing/stance scale or alpha phase; it must not change into `hit`, `death`, crouched, sitting, kneeling, or prone frames.

Scale/aspect guidance:
- Do not force the previous `0.20` visual scale if it makes soldiers too large or squashed in public capture.
- For the next fix, use a `0.15` visual scale target as the center of the review range, then tune only enough to keep four soldiers readable against road/building context.
- Preserve sprite aspect ratio exactly. Scale should be uniform on X/Y/Z in the presentation path; do not squash the quad vertically or stretch it horizontally.
- QA/HCI should reject any capture where soldiers read as huge board pieces, squashed stickers, or exact-foot-only click targets.

Selected-state marker package:
- Reject the current placeholder yellow square as user-facing selected-state art.
- Reject any huge green/blue screen-covering or formation-covering marker.
- Existing small marker candidate:
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/selection_ring.png`
  - Size: `183x91`
  - Use only if recolored/tinted away from bright cyan-blue into a restrained warm/amber ground contact and scaled per soldier.
- Golden selection ring candidate:
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-17_SelectionRing.png`
  - Size: `1536x1024`
  - High-quality style reference only unless cropped/downscaled aggressively. Full-size use is rejected because it will read as a large sci-fi plate.
- Required runtime selected marker:
  - one marker per soldier
  - under or just around the foot/ground contact
  - warm amber/yellow-orange, low saturation, semi-transparent
  - no filled square; use a thin ring, broken ellipse, bracket pair, or contact shadow/glow
  - about the soldier footprint width, not the squad width
  - marker visible in selected first-control capture without hiding boots, road, or target markers

Move/target/attack marker package:
- Move destination candidate:
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/move_destination_ring.png`
  - Size: `113x93`
  - This is acceptable as a small grounded move marker if scaled to roughly two soldier footsteps wide and anchored to the target ground point.
- Attack target candidate:
  - `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/attack_target_ring.png`
  - Size: `151x83`
  - This is acceptable as the attack/target ring if scaled to roughly two soldier footsteps wide and anchored under/around the enemy patrol footprint.
- Golden move/attack markers:
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-18_MoveMarker.png`
  - `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-19_AttackMarker.png`
  - Size: `1536x1024`
  - Use as style references or source for cropped/downscaled marker sprites only. Full-size world use is rejected.
- Target marker size rule:
  - target/move/attack marker should read around two soldier footsteps wide
  - it must not cover the enemy, the player squad, the road context, or the playfield
  - no huge green target marker may be present in public captures

Enemy/patrol clarity package:
- The unclear red flashing sitting object is most likely caused by the enemy using a hit/death/crouched-looking row, or by an oversized red marker/tint attached to a non-idle enemy frame.
- For alive enemy patrol:
  - use standing `Idle_SW`/`Idle_SE`, `Walk_SW`/`Run_SW`, and `Aim_SW`/`Fire_SW`
  - never use `Hit_*` or `Death_*` except for a short damage/death state
  - use a restrained red/hostile tint or outline; do not flash a full-body red overlay while idle
- If hostile readability is still weak, Art/Atlas blocks final signoff on a true red-accent enemy variant, but for this M01 temporary rerun a tinted individual soldier frame is acceptable if QA can verify it reads as an enemy without becoming a red artifact.

User feedback matrix for art-owned items:

| Feedback ID | Art/Atlas assessment | Status | Required evidence before user review |
| --- | --- | --- | --- |
| UFB-2026-05-08-01 | Art/Atlas cannot approve a GameObject renderer-wrapper visual path as public ECS/atlas art evidence. | open, Gameplay/QA-owned implementation | QA proof that public unit/building visuals are ECS entity/atlas-backed and do not expose accepted `MeshRenderer`, `MeshFilter`, or `SpriteRenderer` gameplay presentation. |
| UFB-2026-05-08-02 | Huge green target marker is rejected. Use small grounded move/attack marker assets scaled to about two footsteps. | open | Capture or video showing target/move/attack markers at correct size with no large green overlay. |
| UFB-2026-05-08-03 | Correct frames identified. Movement must use walk/run rows; idle must use idle row; hit/death rows blocked for normal movement. | open | Frame sequence/video or automated state capture proving idle, move, attack, hit, death use correct rows without top/foot artifacts. |
| UFB-2026-05-08-04 | Scale target should move toward `0.15` and preserve aspect ratio. | open | 16:9 and 20:9 captures showing unsquashed soldiers around the updated scale target. |
| UFB-2026-05-08-05 | Red flashing sitting enemy likely comes from hit/death row or oversized red marker/tint. Use standing enemy frames while alive. | open | Capture/video showing enemy alive states use standing/walking/aiming frames, not sitting/prone hit/death frames. |
| UFB-2026-05-08-06 | Placeholder yellow square is rejected. Use per-soldier thin ring/bracket/contact marker; selection hit target is Gameplay-owned. | open | Capture plus selection interaction evidence showing no yellow square and no foot-only selection requirement. |
| UFB-2026-05-08-07 | Art/Atlas accepts the process failure; this package narrows the art checks QA must enforce. | open, PM/QA-owned | QA feedback regression matrix includes every user item. |
| UFB-2026-05-08-08 | Repeated feedback must remain P0 until fixed or waived. | open, PM/QA-owned | PM does not request user review until all rows are fixed/blocked/waived. |

Known gaps:
- No new marker PNG was generated by Art/Atlas in this pass.
- The current selected-state art is not final; it needs Gameplay implementation using a thin ring/bracket/contact marker rather than a material square.
- Current infantry sheet is still key-pose temporary art, not final multi-frame animation.
- Final multi-frame idle/run/walk loops remain missing.
- Final enemy red-accent patrol variant remains missing.
- Final impact VFX and final destroyed/death VFX remain missing.

Cross-lane impacts:
- Gameplay owns wiring the corrected frames, runtime marker sprite/mesh replacement, scale/aspect tuning, selection hit affordance, enemy state/tint behavior, and ECS presentation proof.
- UI's harmful static marker overlay path is addressed per UI handoff; UI only re-engages if Gameplay finds the remaining marker is UI-owned.
- QA/HCI must validate the user feedback matrix directly and must not pass using only narrow SpriteRenderer checks or still screenshots for animation.
- PM must not request another user selected-readability review until the matrix is closed.

Next recommended task:
Gameplay should implement the corrected frame mapping, replace the yellow selected-state square with a small grounded per-soldier marker, scale the target/move/attack markers to about two soldier footsteps, tune soldier visual scale around `0.15` without aspect distortion, and prove the enemy patrol does not use hit/death/sitting frames while alive. Then QA/HCI should run the rejection-aware feedback matrix before PM asks for another user review.

Waiting on lane:
Gameplay, then QA/HCI

Waiting on exact file/report/asset/command:
- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`
- `Design/AgentReports/2026-05-08_qa-hci_user-feedback-regression-gate.md`

Owner of next action:
Gameplay

Can my lane still continue fallback work? no
