# M01 Selected Readability Gameplay Visual Target Manifest

Date: 2026-05-08
Owner: Art/Atlas

## Target Files

- `M01_SelectedReadability_Gameplay_Target.png` - full in-world gameplay target / paintover board.
- `M01_SelectedReadability_Scale_Board.png` - soldier, road, building, and marker scale board.
- `M01_SelectedReadability_Selected_Marker_Target.png` - per-soldier selected-state marker target.
- `M01_SelectedReadability_Move_Attack_Marker_Target.png` - move and attack marker target sizing.
- `M01_SelectedReadability_Enemy_Readability_Target.png` - friendly/enemy readability and rejected enemy artifact guidance.
- `M01_SelectedReadability_Idle_Run_Pose_Guide.png` - acceptable idle/walk/run/aim rows and rejected hit/death usage.
- `M01_SelectedReadability_Rejected_Bad_Examples.png` - named visual cases that must not reappear.

## Source References Used

- `Assets/Game/Art/Generated/2DISO/Units/Unit_Chr_Soldier_Male_02/SpriteSheets/Transparent/Unit_Chr_Soldier_Male_02_FullSetup_4Facing_8State_UnityGrid_960x1680.png`
- `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/selection_ring.png`
- `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/move_destination_ring.png`
- `Assets/Game/Art/UI/Generated/MatchHUD/M01TacticalFeedback/Markers/attack_target_ring.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-selected-first-control.png`
- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`

## Acceptance Checks

- Runtime capture should align to the target boards before another selected-readability approval request.
- Unit visuals must be ECS/atlas-backed and not accepted through visible renderer-wrapper GameObjects.
- Infantry scale should read near the 0.15 target and preserve aspect ratio.
- Selected state should use small per-soldier warm grounded rings/brackets/contact marks, not yellow squares.
- Move/attack markers should read around two soldier footsteps wide.
- Alive enemy patrol must use standing/walking/aiming rows with restrained hostile tint, not hit/death/sitting artifacts.
- Normal movement must use walk/run rows and must not sample adjacent-cell foot/top artifacts.
