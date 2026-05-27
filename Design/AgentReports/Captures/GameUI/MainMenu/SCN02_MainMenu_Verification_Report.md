# SCN02 Main Menu GameUI Verification Report

Date: 2026-05-27
Unity project used: `D:\Projects\WarlineCapture-CodexUnity1`

## Commands Run

- `WarlineCaptureGameUiContentPrefabBuilder.BuildMainMenuOnly`
- `WarlineCaptureGameUiSceneBuilder.BuildStep9`

Both commands were run through the shadow sibling Unity project, not the main Unity project.

## Generated Captures

- `Design/AgentReports/Captures/GameUI/GameUI_MainMenu_Stable.png`
- `Design/AgentReports/Captures/GameUI/GameUI_ReturnedMainMenu_Stable.png`
- `Design/AgentReports/Captures/GameUI/GameUI_MatchHud_Stable.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/GameUI_MainMenu_1920x1080.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/GameUI_MainMenu_2400x1080.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/GameUI_MainMenu_3840x2160.png`
- `Design/AgentReports/Captures/GameUI/MainMenu/GameUI_MainMenu_4800x2160.png`

## Verified

- `SCN02_MainMenuContent.prefab` now has `MenuBackgroundContent`, `HeaderContent`, `LeftContent`, `MiddleContent`, and `RightContent` as direct section roots.
- The menu background is installed into `MenuBackgroundRegion`.
- The header and background remain present when returning to the main menu capture.
- The match HUD capture does not show the SCN02 menu background.
- Main menu captures were generated at 16:9 and 20:9 resolutions.
- The prefab builder validates required SCN02 layer sprites before building.
- The prefab builder rejects old generated main menu folders, MainMenuAlt, V15B references, and target-reference PNG usage as implementation sprites.
- `SCN01_LoadingContent.prefab` was not regenerated.

## Known Visual Differences From Target

- Header resource text is smaller and less target-locked than the reference.
- The header still reads as separated panel chunks, while the target visually reads as a more continuous top command bar.
- Mode card typography and progress areas need a tuning pass to better match the target.
- Right commander panel locked rows and readiness elements need tighter containment and spacing.
- The left navigation matches the ownership/anchoring goal, but text casing and proportions still need visual polish.

## Next Recommended Pass

Tune the visual positions and typography inside the already-correct ownership hierarchy. Do not change the source-of-truth assets or return to generated Unity mockup scenes.
