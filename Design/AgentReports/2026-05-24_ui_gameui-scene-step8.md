Lane: UI

Task: GameUI Step 8 - add hard layout guards for the isolated runtime shell scene.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs
- Assets/Game/Scenes/GameUI.unity

Contracts touched:
- GameUI Step 8 validation now enforces authored shell region bounds in the 2400x1080 design grid.
- Major shell regions must not overlap.
- Installed loading, menu, match HUD, and popup content must stay inside their assigned shell regions.
- Header region must remain stable during menu middle swaps.
- Popup frame must be center anchored, center pivoted, and centered for scale-in popup motion.

User-visible behavior:
- No new runtime behavior was added beyond Step 7. The improvement is validation: bad canvas placement, panel overlap, content overflow, or off-center popup setup now fails in batchmode before PM/QA review.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep8 -logFile /private/tmp/warlinecapture-gameui-step8-unity2-rerun.log

Validation result:
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP8_VALIDATED scene=Assets/Game/Scenes/GameUI.unity
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP8_BUILT scene=Assets/Game/Scenes/GameUI.unity

Known gaps:
- Step 8 validates layout structure and containment, not pixel-perfect target art matching.
- Screenshot/video capture remains for Step 9.
- Text fit and icon alpha-visible center checks are not yet implemented.

Cross-lane impacts:
- None expected. Game scene and legacy UI remain untouched.

Next recommended task:
- Step 9: capture stable GameUI states and compare the loading, main menu, match HUD, result popup, and returned-main-menu visual states.
