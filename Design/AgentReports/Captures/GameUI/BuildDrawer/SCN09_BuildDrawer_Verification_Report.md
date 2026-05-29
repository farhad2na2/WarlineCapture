# SCN09 Build Drawer GameUI Verification Report

Date: 2026-05-27

## Result

Pass. `SCN09_BuildDrawerPopup.prefab` is implemented as an in-match drawer over the SCN08 Match HUD. The final capture follows the active target composition: the Match HUD remains visible, the drawer overlays the center-right play area, the build grid uses a 4x2 card layout, and the selected building detail panel stays readable.

## Source

- Active target: `Design/VisualLockLayered/SCN-09_BuildDrawer/reference/SCN-09_BuildDrawer_OnExistingMatchHUD_TargetLock_V01.png`
- Active layers: `Design/VisualLockLayered/SCN-09_BuildDrawer/layers/`
- Imported Unity layers: `Assets/Game/Art/UI/Generated/BuildDrawer/LayeredOneGo/`

## Outputs

- Prefab: `Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab`
- Stable capture: `Design/AgentReports/Captures/GameUI/BuildDrawer/CleanTargetLock/GameUI_BuildDrawer_Stable.png`
- Responsive captures:
  - `Design/AgentReports/Captures/GameUI/BuildDrawer/CleanTargetLock/Responsive/GameUI_BuildDrawer_1920x1080.png`
  - `Design/AgentReports/Captures/GameUI/BuildDrawer/CleanTargetLock/Responsive/GameUI_BuildDrawer_2400x1080.png`
  - `Design/AgentReports/Captures/GameUI/BuildDrawer/CleanTargetLock/Responsive/GameUI_BuildDrawer_3840x2160.png`
  - `Design/AgentReports/Captures/GameUI/BuildDrawer/CleanTargetLock/Responsive/GameUI_BuildDrawer_4800x2160.png`

## Verification

- Shadow Unity project used: `D:\Projects\WarlineCapture-CodexUnity1`
- Prefab build marker: `WARLINECAPTURE_GAMEUI_BUILD_DRAWER_POPUP_BUILT prefab=SCN09_BuildDrawerPopup.prefab`
- Scene/capture marker: `WARLINECAPTURE_GAMEUI_SCENE_STEP9_BUILT scene=Assets/Game/Scenes/GameUI.unity`
- `SCN01_LoadingContent.prefab` was not touched.
- Mechanical prefab search found no implementation references to `SCN-09_BuildDrawer_OnExistingMatchHUD_TargetLock`, `VisualLockLayered`, `reference`, `Archive`, or old `BuildPlacement_` popup assets.

## Notes

- Build drawer is installed through `WarlineCaptureShellContentPresenterView.InstallBuildDrawerPopup()`.
- The drawer uses active SCN09 sprites plus live TMP text for labels, costs, build time, warnings, and placement instructions.
- Current implementation is presentation-ready and intentionally not connected to ECS/build-placement data yet.
