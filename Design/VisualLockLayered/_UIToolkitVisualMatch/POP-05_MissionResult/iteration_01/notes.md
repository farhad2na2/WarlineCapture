# POP-05 Mission Result - Iteration 01

Status: Target matched for current static pass.

## Baseline

Added and used the editor-only `Open POP-05 Mission Result Static Preview` hook so the popup can be validated in the shadow project without opening the main project.

Baseline findings:

- The current popup is readable but does not yet match the reference composition.
- It needs a larger Target Lock modal, a `MISSION RESULT` header, close chrome, victory/defeat result panels, mission-rating stars, a horizontal stat strip, separate objective/reward/casualty/score panels, and a three-button footer.
- The next implementation slice should preserve runtime-bound names: `Title`, `Subtitle`, `SummaryBody`, `ResultBadge`, `ContinueButton`, and `ReplayButton`.
- New passive visual panels can be added for target parity, but runtime behavior and C# bindings must remain unchanged.

## Slice 01-03 implementation

Rebuilt the compact mission-result popup into live Target Lock UI Toolkit sections while preserving the runtime-bound names `Title`, `Subtitle`, `SummaryBody`, `ResultBadge`, `ContinueButton`, and `ReplayButton`.

Implemented:

- Large modal frame and dark map backdrop.
- `MISSION RESULT` header with badge and close chrome.
- Selected `VICTORY` and inactive `DEFEAT` result panels.
- Mission rating rules and three star icons.
- Horizontal stat rail with time, deployed units, enemies, captured buildings, and accuracy.
- Four separate lower panels: objectives, rewards, casualties, and score.
- Three visible footer actions: continue, replay, and main menu.
- Hover/focus/press lift/scale states for footer actions and close chrome.

Focused audit:

- Lower panels were changed from the previous multi-section armory frame to a cleaner reusable Target Lock panel frame so the panel backgrounds no longer read as baked section lines.
- Stat rail, panel titles, objective/reward/casualty rows, score, and footer button text were scaled up after the first shadow capture showed readability was still too small.
- No C# runtime, scene, prefab, ECS, gameplay, or Canvas fallback files were edited.

Validation:

- Synced `UiToolkitTargetLockStaticPreview.cs`, `POP05_MissionResultPopup.uxml`, and `.uss` to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Visible shadow UI Builder preview opened from `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
- Match Game View was enabled and Fit Viewport was clicked before capture.
- No PlayMode or runtime Game View validation was used.
- `git diff --check` passes after slice 03.

Artifacts:

- Baseline shadow UI Builder capture: `shadow_ui_builder_pop05_baseline_window.png`.
- Slice 01 shadow UI Builder capture: `shadow_ui_builder_pop05_slice01_window.png`.
- Slice 02 shadow UI Builder capture: `shadow_ui_builder_pop05_slice02_window.png`.
- Slice 03 shadow UI Builder capture: `shadow_ui_builder_pop05_slice03_window.png`.
- Slice 03 focused modal crop: `shadow_ui_builder_pop05_slice03_modal_crop.png`.
- Slice 03 focused footer crop: `shadow_ui_builder_pop05_slice03_footer_crop.png`.
