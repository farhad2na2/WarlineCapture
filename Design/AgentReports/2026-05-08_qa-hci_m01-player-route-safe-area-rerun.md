Lane:
QA/HCI

Task:
M01 player-route and simulated safe-area Gate 4 rerun after UI route-driven capture tooling handoff.

Files changed:
- Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md

Contracts touched:
- Design/AgentTasks/qa-hci_current.md: continued the active QA/HCI rerun after the UI handoff landed; no task files were edited.
- Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md: consumed the UI route-driven capture and safe-area tooling handoff.
- Design/WarlineCapture_M01_FirstContact_Production_Contract.md, Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md, Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md, and Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md: checked current command reason-code expectations against runtime/code evidence.
- Design/WarlineCapture_Art_Asset_Requirements_Register.md and Design/WarlineCapture_Art_Asset_Requirements_Register.csv: checked M01 marker/VFX asset status.
- No runtime API, prefab path, route id, mission id, data schema, source contract, asset row, or production source file was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed. This report validates the new route-driven UI evidence and classifies the remaining Gate 4 risks before active M01 balance QA.

Validation run:
- Unity PlayMode in isolated third workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity3`: `Chapter01M01PlayModeValidationTests`, results `/private/tmp/warlinecapture-qa-hci-rerun-playmode-results.xml`, log `/private/tmp/warlinecapture-qa-hci-rerun-playmode.log`.
- Unity EditMode in isolated third workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity3`: `WarlineCaptureUiShellTests`, results `/private/tmp/warlinecapture-qa-hci-rerun-shell-results.xml`, log `/private/tmp/warlinecapture-qa-hci-rerun-shell.log`.
- Unity EditMode in isolated third workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity3`: `WarlineCaptureUiMatchOverlayTests`, results `/private/tmp/warlinecapture-qa-hci-rerun-matchoverlay-results.xml`, log `/private/tmp/warlinecapture-qa-hci-rerun-matchoverlay.log`.
- Unity EditMode in isolated third workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity3`: `WarlineCaptureUiAssistantRuntimeBindingTests`, results `/private/tmp/warlinecapture-qa-hci-rerun-assistant-runtime-results.xml`, log `/private/tmp/warlinecapture-qa-hci-rerun-assistant-runtime.log`.
- Reviewed route-driven capture set: `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/`.
- Reviewed contact sheet: `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/M01_RouteSafeArea_CaptureMatrix_ContactSheet.png`.
- Verified capture dimensions with `sips`: all eight 1920x1080 state captures are 1920x1080, all eight 2400x1080 state captures are 2400x1080, and contact sheet is 1920x1192.
- Reviewed safe-area manifests: `safe_area_1920x1080.json` and `safe_area_2400x1080.json`.
- Reviewed current tactical sprite evidence: `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`.
- Scanned logs for `NullReferenceException`, `RenderTexture.Create failed`, `EntitiesGraphicsSystemUtility`, `AIProduction`, `AIBuild`, `AISquad`, `FreezeDetect`, `PerfDiag`, `RuntimeCitySpawner`, exceptions, errors, warnings, and failures.
- Scanned runtime/test code for canonical and legacy `TacticalCommandReasonCode` usage.
- Scanned asset register for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Validation result:
Needs fixes for final Gate 4 acceptance. The new route-driven capture set exists and is dimensionally valid for the required eight states at 1920x1080 and 2400x1080. Focused Unity smoke is green: `Chapter01M01PlayModeValidationTests` passed 3/3, `WarlineCaptureUiShellTests` passed 15/15, `WarlineCaptureUiMatchOverlayTests` passed 18/18, and `WarlineCaptureUiAssistantRuntimeBindingTests` passed 7/7.

The route screenshots are readable as editor evidence and the assistant/result states are visible. However, Gate 4 still has blocker/major risks: safe-area evidence is two generic simulated inset profiles rather than the PM-requested named matrix, runtime command reason codes still use legacy aliases in production/test code, real touch/camera ergonomics remain unverified, and required feedback marker/VFX assets are still missing or unapproved in the asset register.

Known gaps:
- No real Android/device safe-area, cutout, rounded-corner, thermal, or input-latency pass was run.
- UI handoff safe-area profiles are generic `simulated landscape inset` manifests, not the named `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9` matrix requested by PM audit.
- Route capture proves `WarlineCaptureRouter.Match` screen evidence, but not human touch camera drag/pinch behavior.
- Runtime/code still contains legacy reason-code aliases such as `InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`, `InsufficientResources`, `AbilityOnCooldown`, and `TransportUnavailable`; docs now describe canonical M01 names, but runtime mapping is not complete.
- Marker/VFX rows `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small` remain `missing` / `not_reviewed` / `not_started`.
- Current AI-generated tactical/unit art and route screenshots are review evidence only, not final art approval.

Cross-lane impacts:
- QA/HCI keeps active balance QA blocked until the remaining Gate 4 blocker/major findings are resolved or explicitly waived by PM.
- UI owns the safe-area profile matrix follow-up and should either produce the named profiles or get PM acceptance for the two generic inset profiles.
- Gameplay/Support-FTUE own the runtime reason-code cleanup/mapping because QA found legacy aliases in runtime and test code after doc cleanup.
- Art/design or UI/gameplay integration owns marker/VFX asset status before final visual readability/art approval.
- Gameplay has no reproduced freeze/input-stall blocker from this pass.

Next recommended task:
Route the remaining Gate 4 fixes: UI should close the safe-area profile evidence gap, Gameplay/Support-FTUE should align runtime reason codes to canonical M01 names or provide an explicit mapping, and art/design should provide or formally label temporary marker/VFX evidence. QA/HCI should rerun only the affected checks after those handoffs land.

## Current balance-QA gate status

Blocked. Automated route smoke and route-driven screenshots are green, but M01 balance conclusions would still be premature because safe-area profile coverage, real touch/camera ergonomics, runtime reason-code consistency, and final feedback-marker readability are not accepted.

## QA Unity workspace smoke checks run

- `Chapter01M01PlayModeValidationTests`: Passed 3/3 in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- `WarlineCaptureUiShellTests`: Passed 15/15 in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- `WarlineCaptureUiMatchOverlayTests`: Passed 18/18 in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- `WarlineCaptureUiAssistantRuntimeBindingTests`: Passed 7/7 in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.

## Performance/freeze/log-health findings

- No `NullReferenceException`, `RenderTexture.Create failed`, `EntitiesGraphicsSystemUtility`, `FreezeDetect`, or `RuntimeCitySpawner` issue reproduced in the accepted rerun logs.
- The old `RuntimeCitySpawner=1350.3ms` hitch did not reproduce.
- One `PerfDiag:ECS:PreGame` line appeared in the PlayMode log, but no visible freeze or test failure accompanied it.
- Remaining editor/tooling noise: Unity licensing handshake/access-token messages, Xcode plist warnings, and `usbmuxd` shutdown messages. These are not classified as player-visible blockers in this pass.

## New HCI risks introduced by latest handoffs

- The route-driven captures are editor generated and can look stable while real touch camera behavior still fails on device.
- The safe-area evidence shows visible margins, but the lack of named no-inset, rounded-corner, and left-cutout profiles leaves mobile clipping risk unresolved.
- The invalid-command recovery screenshot is readable, but runtime reason-code aliases can still make ARIA/UI recovery assertions drift from the canonical M01 contract.
- Feedback markers are readable enough as current screenshot evidence, but their asset-register status blocks final art/readability approval.

## Waiting/blocker ownership fields

Waiting on lane:
UI, Gameplay/Support-FTUE, and art-design/integration.

Waiting on exact file/report/asset/command:
- UI safe-area evidence for `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`, or PM acceptance of the current generic inset manifests.
- Runtime reason-code cleanup/mapping report or code/test proof that M01 emits canonical reason codes.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
UI owns safe-area evidence; Gameplay/Support-FTUE own reason-code runtime alignment; art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI consumed the available UI handoff, verified captures, ran focused Unity smoke, reviewed logs, checked runtime reason-code usage, and reviewed marker/VFX asset status.

## QA findings

### QAHCI-G4-011: Route-driven capture matrix is present and readable, but safe-area profile coverage is incomplete

- Severity: Blocker for final mobile Gate 4 acceptance.
- Affected lane: UI.
- Reproduction steps:
  1. Open `Design/AgentReports/2026-05-08_ui_m01-route-driven-capture-safe-area-tooling.md`.
  2. Open `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/safe_area_1920x1080.json`.
  3. Open `Design/AgentReports/Captures/2026-05-08_m01-route-safe-area/safe_area_2400x1080.json`.
  4. Compare the manifests with `Design/AgentReports/2026-05-08_pm_design-audit-safe-area-profile-ambiguity.md` and `Design/AgentReports/2026-05-08_pm_design-audit-unrouted-gate4-findings.md`.
- Expected: UI provides named safe-area profiles `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`, or PM explicitly accepts a replacement matrix.
- Actual: UI provides two generic `simulated landscape inset` manifests: 1920x1080 with left/right/top/bottom `64/64/32/24`, and 2400x1080 with `112/112/44/28`.
- Blocks next milestone: yes for mobile-safe-area Gate 4 acceptance; no for basic route screenshot existence.
- Recommended owner: UI, with PM if the current generic inset evidence should be accepted as the replacement matrix.

### QAHCI-G4-012: Runtime reason-code aliases still diverge from the canonical M01 contract

- Severity: Blocker for invalid-command recovery acceptance.
- Affected lane: gameplay / UI / support-FTUE.
- Reproduction steps:
  1. Run `rg -n "enum TacticalCommandReasonCode|InvalidTarget|BlockedRoute|OutOfRange|BuildModeUnavailable|InsufficientResources|AbilityOnCooldown|TransportUnavailable" Assets/Game/Scripts Assets/Tests -g '*.cs'`.
  2. Compare results to the canonical M01 reason codes in `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`.
- Expected: M01 runtime/code either emits canonical names (`TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, `CameraJumpUnavailable`, `NoSelection`) or documents an explicit mapping for QA assertions.
- Actual: Runtime/test code still defines and asserts legacy aliases including `InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`, `InsufficientResources`, `AbilityOnCooldown`, and `TransportUnavailable`. The UI handoff does not state whether the invalid-command capture uses legacy or canonical runtime reason codes.
- Blocks next milestone: yes for invalid-command recovery Gate 4 acceptance.
- Recommended owner: Gameplay with Support/FTUE contract support; UI should update capture assertions after mapping lands.

### QAHCI-G4-013: Human touch/camera ergonomics remain unverified

- Severity: Major.
- Affected lane: gameplay / UI.
- Reproduction steps:
  1. Run `Chapter01M01PlayModeValidationTests`.
  2. Review the route-driven capture contact sheet.
  3. Confirm no real or simulated touch camera drag/pinch pass is documented in the UI or QA reports.
- Expected: Before active balance QA, the M01 route has evidence for player input, camera/touch behavior, assistant takeover release, and result-flow Stop behavior under playable conditions.
- Actual: PlayMode route logic and UI route screenshots are green, but real touch/camera behavior and input latency are not exercised.
- Blocks next milestone: yes for active balance QA; no new code blocker by itself.
- Recommended owner: QA/HCI for manual/device pass; Gameplay/UI only if a route/input defect reproduces.

### QAHCI-G4-014: Feedback marker and destroyed VFX assets remain missing or unapproved

- Severity: Major for final visual readability/art approval.
- Affected lane: art-design / UI / gameplay.
- Reproduction steps:
  1. Run `rg -n "marker.selection.ring|marker.move.destination|marker.attack.target|vfx.unit.destroyed.small" Design/WarlineCapture_Art_Asset_Requirements_Register.md Design/WarlineCapture_Art_Asset_Requirements_Register.csv`.
  2. Review the route-driven squad selected, move feedback, attack feedback, and result/objective states.
- Expected: Gate 4 final visual readability has approved or explicitly temporary marker/VFX evidence for selection, move destination, attack target, and destroyed feedback.
- Actual: Screenshot evidence is readable enough for current route review, but asset rows remain `missing`, `not_reviewed`, and `not_started`; UI report only explicitly calls out `vfx.unit.destroyed.small`.
- Blocks next milestone: blocks final art/readability approval; does not block the current automated route-flow smoke by itself.
- Recommended owner: art-design or the implementing UI/gameplay lane assigned to marker/VFX integration.

### QAHCI-G4-015: Focused route/UI smoke is green

- Severity: Informational/pass.
- Affected lane: gameplay / UI / support-FTUE.
- Reproduction steps:
  1. Run the focused PlayMode and EditMode tests listed in this report from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
  2. Inspect the four Test Runner XML files in `/private/tmp/`.
- Expected: M01 route, shell, match overlay, and assistant runtime tests pass without severe log-health regressions.
- Actual: PlayMode passed 3/3; UI shell passed 15/15; match overlay passed 18/18; assistant runtime binding passed 7/7. No severe tracked runtime exception/freeze signatures reproduced.
- Blocks next milestone: no.
- Recommended owner: QA/HCI watch only.

## 2026-05-08 safe-area profile matrix addendum

Lane:
QA/HCI

Task:
Verify the PM-accepted UI safe-area profile matrix fix and update the Gate 4 blocker record.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_ui_m01-safe-area-profile-matrix-fix.md`
- `Design/AgentReports/2026-05-08_pm_ui-m01-safe-area-profile-matrix-fix-review.md`
- `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed. This addendum updates QA/HCI evidence status after PM accepted the simulated safe-area matrix.

Validation run:
- Reviewed PM acceptance in `Design/AgentReports/2026-05-08_pm_ui-m01-safe-area-profile-matrix-fix-review.md`.
- Reviewed UI handoff in `Design/AgentReports/2026-05-08_ui_m01-safe-area-profile-matrix-fix.md`.
- Reviewed manifests:
  - `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/profile_safe_none_16x9.json`
  - `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/profile_safe_rounded_20x9.json`
  - `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/profile_safe_cutout_left_20x9.json`
- Ran `file Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/*.png` to verify PNG dimensions.

Validation result:
Pass for simulated safe-area profile evidence. QA/HCI accepts PM's closure of `QAHCI-G4-011` for the simulated profile matrix only.

Evidence verified:
- `safe.none_16x9`: manifest profile id present, 1920x1080 resolution, zero insets, no cutout rectangles, eight state PNGs at 1920x1080.
- `safe.rounded_20x9`: manifest profile id present, 2400x1080 resolution, `96/96/34/28` left/right/top/bottom insets, no cutout rectangles, eight state PNGs at 2400x1080.
- `safe.cutout_left_20x9`: manifest profile id present, 2400x1080 resolution, `184/72/34/28` left/right/top/bottom insets, `left_camera_cutout` rectangle present, eight state PNGs at 2400x1080.
- Each manifest includes per-surface pass notes for HUD/header, objective panel, threat feed, squad tray, command controls, minimap, assistant panel, and result popup.
- Each manifest explicitly keeps runtime reason-code proof and final marker/VFX approval outside this UI safe-area fix.

Known gaps:
- Public M01 launch path still enters legacy 3D gameplay and blocks manual HCI/balance validation.
- Runtime canonical reason-code proof remains blocked pending passing Unity validation from the owning implementation lane.
- Human touch/camera ergonomics remain unverified.
- Marker/VFX evidence remains temporary or unapproved, and `vfx.unit.destroyed.small` remains absent.
- No real device safe-area/cutout pass was run; this closure is only for the PM-requested simulated matrix.

Cross-lane impacts:
- UI no longer owns `QAHCI-G4-011` unless a later regression appears in public launch or device validation.
- Gameplay/UI still own the public M01 production launch-path blocker.
- Gameplay/Support-FTUE still own runtime reason-code validation.
- Art-design or the implementing lane still owns final marker/VFX readiness.

Next recommended task:
Gameplay/UI should fix or explicitly label the public M01 production launch path. Support/FTUE or the owning implementation lane should land passing focused Unity validation for runtime reason-code alignment. QA/HCI should rerun only the affected public-launch and reason-code checks after reviewed handoffs land.

### QAHCI-G4-011: Simulated safe-area profile matrix accepted

- Severity: Informational/pass after PM review; previously a blocker.
- Affected lane: UI.
- Reproduction steps:
  1. Open `Design/AgentReports/2026-05-08_ui_m01-safe-area-profile-matrix-fix.md`.
  2. Open `Design/AgentReports/2026-05-08_pm_ui-m01-safe-area-profile-matrix-fix-review.md`.
  3. Open the three profile manifests under `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`.
  4. Run `file Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/*.png`.
- Expected: UI provides named simulated profiles `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`, with dimensions, insets, cutout rectangles where applicable, and per-surface clearance notes.
- Actual: All three profile manifests and all 24 state PNGs are present with expected dimensions and per-surface pass notes. PM accepted this as closing the UI-owned simulated safe-area evidence gap.
- Blocks next milestone: no for simulated safe-area evidence. Gate 4 still remains blocked by public launch path, reason-code validation, touch/camera validation, and marker/VFX readiness.
- Recommended owner: QA/HCI watch only; UI if a later safe-area regression appears.

## Updated waiting/blocker ownership fields

Waiting on lane:
Gameplay/UI, Gameplay/Support-FTUE, and art-design/integration.

Waiting on exact file/report/asset/command:
- Reviewed Gameplay/UI handoff proving a public M01 production launch path no longer enters the legacy 3D prototype, or explicitly labeling legacy paths as sandbox.
- Passing focused Unity validation report for runtime canonical reason-code alignment.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
Gameplay/UI owns the public launch-path blocker; Gameplay/Support-FTUE owns reason-code runtime validation; art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI verified the PM-accepted simulated safe-area profile matrix and should wait for reviewed handoffs on the remaining blockers before rerunning affected checks.

## 2026-05-08 public launch smoke attempt addendum

Lane:
QA/HCI

Task:
Attempt the PM-requested public launch validation after `Design/AgentReports/2026-05-08_pm_ui-m01-public-launch-path-review.md` accepted the Quick Custom slice as focused evidence but kept the overall public launch handoff in `needs fixes`.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_ui-m01-public-launch-path-review.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed. QA/HCI attempted validation only.

Validation run:
- Attempt 1, Quick Custom PlayMode smoke:
  - Command: `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute -testResults /private/tmp/warlinecapture-qa-hci-public-quickcustom-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-quickcustom.log -quit`
  - Log: `/private/tmp/warlinecapture-qa-hci-public-quickcustom.log`
  - Expected results XML: `/private/tmp/warlinecapture-qa-hci-public-quickcustom-results.xml`
- Attempt 2, Quick Custom PlayMode smoke retry:
  - Command: `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute -testResults /private/tmp/warlinecapture-qa-hci-public-quickcustom-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-quickcustom-retry.log -quit`
  - Log: `/private/tmp/warlinecapture-qa-hci-public-quickcustom-retry.log`
  - Expected results XML: `/private/tmp/warlinecapture-qa-hci-public-quickcustom-results.xml`
- Attempt 1, existing campaign route EditMode support tests:
  - Command: `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiSagaCampaignTests -testResults /private/tmp/warlinecapture-qa-hci-public-saga-editmode-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-saga-editmode.log -quit`
  - Log: `/private/tmp/warlinecapture-qa-hci-public-saga-editmode.log`
  - Expected results XML: `/private/tmp/warlinecapture-qa-hci-public-saga-editmode-results.xml`
- Attempt 2, existing campaign route EditMode support tests retry:
  - Command: `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiSagaCampaignTests -testResults /private/tmp/warlinecapture-qa-hci-public-saga-editmode-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-saga-editmode-retry.log -quit`
  - Log: `/private/tmp/warlinecapture-qa-hci-public-saga-editmode-retry.log`
  - Expected results XML: `/private/tmp/warlinecapture-qa-hci-public-saga-editmode-results.xml`

Validation result:
Blocked/inconclusive. All four Unity commands exited 0, but Unity did not emit the requested Test Runner XML files. Log review found editor startup/shutdown and licensing handshake/access-token errors, but no usable Test Runner pass/fail evidence. QA/HCI cannot count these attempts as a Quick Custom pass or campaign pass.

Known gaps:
- No end-to-end campaign public launch smoke has landed. The existing `WarlineCaptureUiSagaCampaignTests` are useful route/button support checks, but they do not prove the full user path `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch` reaches the M01 production slice.
- No screenshot/player-visible capture was produced.
- The public launch handoff remains `needs fixes` per PM review until the campaign path and player-visible evidence are covered.
- Runtime reason-code validation, human touch/camera ergonomics, and marker/VFX readiness remain separate open Gate 4 blockers.

Cross-lane impacts:
- QA/HCI cannot advance manual HCI/balance validation from this attempt.
- UI/GamePlay still own the campaign public launch evidence gap from the PM review.
- QA/HCI owns no production-code follow-up from this failed validation attempt; it should rerun only after a reviewed handoff or a healthy Unity Test Runner path is available.

Next recommended task:
UI/GamePlay should provide the missing campaign path smoke or a PM-approved graphics/player-visible capture path. If the Unity Test Runner remains unable to emit XML in the QA workspace, the owning lane should include verifiable logs/captures in its handoff before QA/HCI reruns.

### QAHCI-G4-016: Public launch validation is still not accepted from QA/HCI

- Severity: Blocker for manual HCI/balance readiness.
- Affected lane: UI / gameplay / support-FTUE.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`.
  2. Read `Design/AgentReports/2026-05-08_pm_ui-m01-public-launch-path-review.md`.
  3. Attempt the focused Quick Custom PlayMode command listed above from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
  4. Attempt the existing campaign route EditMode command listed above from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
  5. Check for `/private/tmp/warlinecapture-qa-hci-public-quickcustom-results.xml` and `/private/tmp/warlinecapture-qa-hci-public-saga-editmode-results.xml`.
- Expected: QA/HCI rerun produces Test Runner XML proving the Quick Custom public launch reaches `WarlineCaptureRoute.Match`, keeps `UI_Canvas` inactive, and shows the M01 sprite-presenter production slice; campaign path has end-to-end public launch evidence or a player-visible capture.
- Actual: Unity exited 0 but did not emit Test Runner XML for either focused command. Campaign path still lacks end-to-end public launch smoke and player-visible capture evidence.
- Blocks next milestone: yes. Manual HCI/balance QA should not start until public launch proof is accepted.
- Recommended owner: UI/GamePlay for campaign/public launch evidence; QA/HCI to rerun once reviewed evidence or a reliable Unity validation path is available.

## Updated waiting/blocker ownership fields after public launch attempt

Waiting on lane:
Gameplay/UI, Gameplay/Support-FTUE, and art-design/integration.

Waiting on exact file/report/asset/command:
- Reviewed Gameplay/UI handoff proving both Quick Custom and campaign public launch paths reach the M01 production slice, including the previously failing `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch` path.
- Passing focused Unity validation or player-visible capture evidence for the public launch path.
- Passing focused Unity validation report for runtime canonical reason-code alignment.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
Gameplay/UI owns the public launch-path evidence gap; Gameplay/Support-FTUE owns reason-code runtime validation; art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI attempted the available focused public launch validation, recorded the blocked/inconclusive Unity result, and should wait for reviewed handoffs or a reliable validation path before rerunning.

## 2026-05-08 reason-code final validation addendum

Lane:
QA/HCI

Task:
Validate the PM-accepted Support/FTUE runtime reason-code final handoff and update `QAHCI-G4-012`.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md`
- `Design/AgentReports/2026-05-08_pm_support-ftue-m01-runtime-reason-code-alignment-final-review.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. The validated handoff changes invalid-command feedback to canonical M01 reason-code names and player-facing strings.

Validation run:
- Reviewed PM acceptance in `Design/AgentReports/2026-05-08_pm_support-ftue-m01-runtime-reason-code-alignment-final-review.md`.
- Reviewed Support/FTUE handoff in `Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md`.
- Static legacy alias scan:
  - `rg -n "InvalidTarget|BlockedRoute|OutOfRange|BuildModeUnavailable|InsufficientResources|AbilityOnCooldown|TransportUnavailable" Assets/Game/Scripts Assets/Tests -g '*.cs'`
- Static canonical-name scan:
  - `rg -n "TargetOutOfBounds|TargetBlocked|TargetUnreachable|TargetNotEnemy|TargetNotAttackable|CommandUnavailable|MissionDoesNotAllowBuild|CameraJumpUnavailable|NoSelection" Assets/Game/Scripts Assets/Tests -g '*.cs'`
- Verified referenced focused Unity XML artifacts:
  - `/private/tmp/warlinecapture-support-reason-code-final-results.xml`
  - `/private/tmp/warlinecapture-support-reason-code-matchoverlay-results.xml`
  - `/private/tmp/warlinecapture-support-reason-code-executor-results.xml`
  - `/private/tmp/warlinecapture-support-reason-code-command-runtime-results.xml`

Validation result:
Accepted. QA/HCI closes `QAHCI-G4-012` for canonical runtime reason-code alignment.

Evidence verified:
- Legacy alias scan has no active alias hits in `Assets/Game/Scripts` or `Assets/Tests`; the only broad `OutOfRange` hit is the unrelated `ArgumentOutOfRangeException` framework type in `Assets/Game/Scripts/UI/MenuView.cs`.
- Canonical enum names are present in runtime, assistant, UI bridge, and focused tests.
- `WarlineCaptureUiAssistantRuntimeBindingTests`: 7/7 passed in `/private/tmp/warlinecapture-support-reason-code-final-results.xml`.
- `WarlineCaptureUiMatchOverlayTests`: 18/18 passed in `/private/tmp/warlinecapture-support-reason-code-matchoverlay-results.xml`.
- `CommandIntentExecutorTests`: 14/14 passed in `/private/tmp/warlinecapture-support-reason-code-executor-results.xml`.
- `M01AssistantCommandRuntimeTests`: 10/10 passed in `/private/tmp/warlinecapture-support-reason-code-command-runtime-results.xml`.

Known gaps:
- Some generic invalid-target cases still map to `TargetNotAttackable`; PM accepted this for canonical-name closure. Gameplay/UI may refine semantics later if the runtime can distinguish out-of-bounds, not-enemy, or unreachable contexts.
- This addendum does not validate public launch, touch/camera ergonomics, or marker/VFX readiness.

Cross-lane impacts:
- Support/FTUE no longer owns `QAHCI-G4-012` unless later public launch or device validation reveals a regression.
- Gameplay/UI still own the public launch-path evidence gap, especially the campaign path `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch`.
- Art-design or the implementing lane still owns marker/VFX readiness and final art approval.

Next recommended task:
QA/HCI should wait for the reviewed Gameplay/UI public launch handoff or PM-approved player-visible capture evidence, then rerun only the affected public launch checks. Marker/VFX readiness remains a separate Gate 4 blocker unless PM waives temporary evidence.

### QAHCI-G4-012: Runtime reason-code alignment accepted

- Severity: Informational/pass after PM review; previously a blocker.
- Affected lane: gameplay / UI / support-FTUE.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md`.
  2. Read `Design/AgentReports/2026-05-08_pm_support-ftue-m01-runtime-reason-code-alignment-final-review.md`.
  3. Run the static legacy alias scan listed above.
  4. Inspect the four focused Unity XML result files listed above.
- Expected: Runtime/test code uses canonical M01 reason-code names and focused assistant/runtime/UI tests pass.
- Actual: Legacy aliases are absent except unrelated `ArgumentOutOfRangeException`; canonical names are present; focused XML results pass 7/7, 18/18, 14/14, and 10/10.
- Blocks next milestone: no for reason-code alignment. Gate 4 remains blocked by public launch evidence, touch/camera validation, and marker/VFX readiness.
- Recommended owner: QA/HCI watch only; Gameplay/UI if later semantic refinement is required.

## Updated waiting/blocker ownership fields after reason-code validation

Waiting on lane:
Gameplay/UI and art-design/integration.

Waiting on exact file/report/asset/command:
- Reviewed Gameplay/UI handoff proving both Quick Custom and campaign public launch paths reach the M01 production slice, including the previously failing `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch` path.
- Passing focused Unity validation or player-visible capture evidence for the public launch path.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
Gameplay/UI owns the public launch-path evidence gap; art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI validated and closed the accepted reason-code handoff, and should wait for reviewed public launch or marker/VFX handoffs before rerunning affected checks.

## 2026-05-08 public visible-scene blocker update

Lane:
QA/HCI

Task:
Record the latest PM/manual-test blocker update for public Test/Custom launch visibility.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_pm_manual-test-test-custom-still-legacy-scene.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. The latest manual evidence says Test/Custom still shows the old scene.

Validation run:
- Reviewed `Design/AgentReports/2026-05-08_pm_manual-test-test-custom-still-legacy-scene.md`.
- No new Unity validation was run because this is not a handoff claiming a fix; it is a blocker report.

Validation result:
Blocked. Public launch proof is still not ready for QA/HCI acceptance. The latest PM/manual report rejects the assumption that `WarlineCaptureRoute.Match` plus `UI_Canvas` inactive is sufficient; acceptance now requires player-visible screenshot/capture or manual/graphics validation proving the first rendered scene is the current M01 2D/isometric production slice, not the old 3D prototype.

Known gaps:
- Test launch path still reportedly shows the old scene.
- Custom/Quick Custom launch path still reportedly shows the old scene.
- Campaign path remains unaccepted until independently proven.
- No player-visible screenshot/capture evidence has landed for any fixed public launch path.

Cross-lane impacts:
- Gameplay/UI still own the public visible-scene blocker.
- QA/HCI should not rerun the same route-only validation or ask for manual HCI/balance testing until a reviewed handoff includes visible-scene proof.
- Art/design marker/VFX readiness remains a separate open Gate 4 blocker.

Next recommended task:
Gameplay/UI should provide a reviewed handoff with player-visible screenshot/capture or manual/graphics validation proving the public Test/Custom and campaign paths render the M01 production slice. The handoff must state entry path, active mission id, route state, `UI_Canvas` state, whether old 3D visuals are visible, whether current M01 2D/isometric visuals are visible, and evidence paths.

### QAHCI-G4-017: Public launch route state is not enough; visible scene still fails

- Severity: Blocker for manual HCI/balance readiness.
- Affected lane: gameplay / UI.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_pm_manual-test-test-custom-still-legacy-scene.md`.
  2. Launch the public Test path or Custom/Quick Custom path.
  3. Observe the first player-visible gameplay scene after launch.
- Expected: Public Test/Custom launch renders the current M01 2D/isometric sprite-presenter/sprite-renderer production slice, with the old 3D prototype not visible.
- Actual: User manual feedback reported the old scene is still visible after Test/Custom launch.
- Blocks next milestone: yes. Manual HCI/balance QA remains blocked.
- Recommended owner: Gameplay/UI.

## Updated waiting/blocker ownership fields after visible-scene blocker update

Waiting on lane:
Gameplay/UI and art-design/integration.

Waiting on exact file/report/asset/command:
- Reviewed Gameplay/UI handoff with player-visible screenshot/capture or manual/graphics validation proving public Test/Custom and campaign paths render the M01 production slice and not the old 3D prototype.
- Evidence must include entry path, active mission id, route state, `UI_Canvas` state, old-3D visibility status, current M01 2D/isometric visibility status, and screenshot/capture/log paths.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
Gameplay/UI owns the public visible-scene launch blocker; art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI has the latest blocker recorded and should wait for a reviewed visible-scene handoff before rerunning public launch checks.

## 2026-05-08 unreported public-launch implementation watch

Lane:
QA/HCI

Task:
Heartbeat check for new public-launch handoff reports after the visible-scene blocker update.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentTasks/qa-hci_current.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Checked `Design/AgentReports/` for new 2026-05-08 handoff reports.
- Checked workspace status.

Validation result:
Blocked. Production files related to visible-scene launch work are currently modified, but no new reviewed Gameplay/UI handoff report is available for QA/HCI validation.

Observed unreported/in-flight files:
- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs`
- `Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

Expected handoff:
A Gameplay/UI report under `Design/AgentReports/` proving public Test/Custom and campaign paths render the M01 production slice and not the old 3D prototype. The report must include entry path, active mission id, route state, `UI_Canvas` state, whether old 3D visuals are visible, whether current M01 2D/isometric visuals are visible, screenshot/capture/log paths, and focused validation results.

Actual:
No new public-launch fix handoff report is present yet, so QA/HCI cannot validate or accept the in-flight production changes.

Known gaps:
- Public visible-scene launch blocker remains open.
- Marker/VFX readiness remains open.
- QA/HCI should not rerun validation against unreported production changes as a substitute for lane handoff evidence.

Cross-lane impacts:
- Gameplay/UI can continue and must write the handoff report when the public visible-scene fix is ready.
- QA/HCI remains blocked until that report lands.

Next recommended task:
Gameplay/UI should finish the visible-scene public launch fix and write the required handoff report under `Design/AgentReports/`. QA/HCI should validate immediately once the handoff exists.

## Updated waiting/blocker ownership fields after unreported implementation watch

Waiting on lane:
Gameplay/UI and art-design/integration.

Waiting on exact file/report/asset/command:
- Missing Gameplay/UI public visible-scene handoff report under `Design/AgentReports/` for the modified public-launch implementation files.
- Report must prove public Test/Custom and campaign paths render M01 production visuals, not old 3D prototype visuals.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
Gameplay/UI owns the public visible-scene handoff; art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI should not validate unreported in-flight production changes and should wait for a reviewed handoff.

## 2026-05-08 UI public launch visible-scene handoff validation

Lane:
QA/HCI

Task:
Validate the UI public launch-path handoff in `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md` against the active visible-gameplay HCI gate.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. The handoff claims both public campaign and Quick Custom launch paths now suppress legacy 3D visuals and enter the M01 production route.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Read `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`.
- Reviewed capture evidence:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- Checked capture dimensions with `sips -g pixelWidth -g pixelHeight`.
- Inspected existing Unity result XML from the handoff:
  - `/private/tmp/warlinecapture-m01-public-launch-results.xml`
  - `/private/tmp/warlinecapture-m01-quickcustom-editmode-results.xml`
  - `/private/tmp/warlinecapture-m01-sagacampaign-editmode-results.xml`
- Searched handoff logs for exceptions, freezes, performance diagnostics, and leak warnings:
  - `/private/tmp/warlinecapture-m01-public-launch.log`
  - `/private/tmp/warlinecapture-m01-quickcustom-editmode.log`
  - `/private/tmp/warlinecapture-m01-sagacampaign-editmode.log`
- Ran the banned runtime lookup scan on touched runtime files:
  - `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
  - `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs`
  - `Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs`

Validation result:
Needs fixes. The focused automated evidence is green and the captures show the old 3D prototype appears suppressed, but the visible gameplay evidence still fails the QA/HCI manual-readiness gate. Both captures are 1280x720 camera renders with a tiny centered play area on a mostly empty brown field. They do not show a readable first player task, objective, HUD/assistant context, usable camera framing, command feedback, invalid-input recovery, or touch/click ergonomics.

Automated evidence observed:
- `Chapter01M01PlayModeValidationTests`: 5/5 passed in `/private/tmp/warlinecapture-m01-public-launch-results.xml`.
- `WarlineCaptureUiQuickCustomTests`: 16/16 passed in `/private/tmp/warlinecapture-m01-quickcustom-editmode-results.xml`.
- `WarlineCaptureUiSagaCampaignTests`: 8/8 passed in `/private/tmp/warlinecapture-m01-sagacampaign-editmode-results.xml`.
- Runtime banned lookup scan on the three touched runtime files returned no matches.
- Capture dimensions are 1280x720 for both public launch captures.

Performance/freeze/log-health findings:
- No `FreezeDetect` lines were found in the inspected logs.
- `/private/tmp/warlinecapture-m01-public-launch.log` contains one `[PerfDiag] slowUpdate` line with `total=26.9ms`, `cpuFrame=940.2ms`, `units=0`, and `focused=0`; this needs owner classification if the capture path remains part of readiness evidence.
- `/private/tmp/warlinecapture-m01-public-launch.log` still reports preview-scene and persistent-allocation leak warnings at shutdown.
- Licensing handshake/access-token and usbmuxd messages remain present in all inspected Unity logs but did not prevent these tests from completing.

Known gaps:
- The handoff provides camera-render captures, not full screen-overlay screenshots; the report states `ScreenCapture.CaptureScreenshot` did not emit files in batchmode.
- The visible captures do not demonstrate that a player can understand where they are, identify the objective, select a unit, issue move/attack commands with feedback, recover from invalid input, or read HUD/assistant guidance.
- Touch/camera ergonomics remain unverified.
- Marker/VFX readiness remains open for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Cross-lane impacts:
- UI/GamePlay have made progress on route-state and legacy-visual suppression evidence, but QA/HCI cannot accept manual readiness until the first visible player scene is readable and actionable.
- QA/HCI should not ask the user for manual M01 balance or HCI testing from these captures.
- PM can treat automated public-route evidence as improved, but Gate 4 remains blocked on visible HCI readiness.

Next recommended task:
UI/GamePlay should provide an improved public visible-scene handoff with a readable first gameplay screenshot or capture from `Main Menu -> Saga Map -> First Contact -> Mission Briefing/Loadout -> Launch` and Quick Custom/Test launch. The evidence must show the active route/mission, suppress legacy 3D visuals, and frame the unit, target, objective/HUD, and next action clearly enough for the minimum HCI sanity pass.

### QAHCI-G4-018: Public launch reaches route but visible gameplay remains unreadable

- Severity: Blocker for manual HCI/balance readiness.
- Affected lane: gameplay / UI.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`.
  2. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`.
  3. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`.
  4. Compare the captures against the active QA/HCI requirement that the actual first rendered scene prove the player can understand and operate the M01 production slice.
- Expected: Public campaign and Quick Custom launch evidence shows the intended M01 2D/isometric production slice with readable camera framing, visible unit/target context, objective or HUD/assistant context, and enough feedback affordance to begin the first interaction.
- Actual: Captures show a tiny play area centered on a mostly empty brown field. Campaign evidence shows only a small visible patch/object; Quick Custom shows tiny sprites/markers. Neither capture proves readable objective context, command feedback, HUD/assistant state, invalid-input recovery, or touch/camera usability.
- Blocks next milestone: yes. Manual HCI/balance QA remains blocked.
- Recommended owner: Gameplay/UI.

## Updated waiting/blocker ownership fields after UI public launch validation

Waiting on lane:
Gameplay/UI and art-design/integration.

Waiting on exact file/report/asset/command:
- Revised Gameplay/UI public visible-scene handoff with readable full-player evidence for campaign and Quick Custom/Test public launch paths.
- Evidence must include entry path, active mission id, route state, `UI_Canvas` state, old-3D visibility status, current M01 2D/isometric visibility status, HUD/objective/assistant visibility or an accepted explanation, camera framing, and screenshot/capture/log paths.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Owner of next action:
Gameplay/UI owns the visible-scene readability blocker; art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI validated the new public-launch handoff and rejected it for manual-readiness evidence. Further QA/HCI public launch work should wait for revised visible-scene evidence or an explicit PM/user waiver of the current visible gameplay gate.

## 2026-05-08 revised UI public launch visible-scene validation

Lane:
QA/HCI

Task:
Validate the revised UI public launch-path handoff in `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md` after the readable first-playable camera/capture update.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. The revised handoff claims the public campaign and Quick Custom paths now reach M01 production gameplay with live HUD/objectives/threat feed/assistant entry/command bar/minimap visible and legacy 3D scene roots suppressed.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Re-read the revised `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`.
- Reviewed regenerated capture evidence:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- Checked capture dimensions with `sips -g pixelWidth -g pixelHeight`.
- Inspected updated Unity result XML:
  - `/private/tmp/warlinecapture-m01-public-launch-results.xml`
  - `/private/tmp/warlinecapture-m01-quickcustom-editmode-results.xml`
  - `/private/tmp/warlinecapture-m01-sagacampaign-editmode-results.xml`
- Searched the handoff logs for exceptions, freezes, performance diagnostics, and leak warnings.
- Re-ran the banned runtime lookup scan on touched runtime files:
  - `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
  - `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerSystem.cs`
  - `Assets/Game/Scripts/Environment/RuntimeGridBlockerSystem.cs`

Validation result:
Accepted for the specific public-launch visible-scene blocker. The revised captures now show the player-facing Match HUD, objectives, threat feed, ARIA entry, unit cards, command bar, minimap, and a closer readable first-playable M01 unit view for both campaign and Quick Custom launch paths. The old 3D prototype is not visible in the reviewed captures.

This does not fully open balance QA or final Gate 4 by itself. Manual/device touch-camera ergonomics, the eight-state 1920x1080 and 2400x1080 capture matrix, marker/VFX readiness, and remaining log-health classification are still separate active blockers or follow-ups.

Automated evidence observed:
- `Chapter01M01PlayModeValidationTests`: 5/5 passed in `/private/tmp/warlinecapture-m01-public-launch-results.xml`, updated at 2026-05-08 10:32 local time.
- `WarlineCaptureUiQuickCustomTests`: 16/16 passed in `/private/tmp/warlinecapture-m01-quickcustom-editmode-results.xml`.
- `WarlineCaptureUiSagaCampaignTests`: 8/8 passed in `/private/tmp/warlinecapture-m01-sagacampaign-editmode-results.xml`.
- Runtime banned lookup scan on the three touched runtime files returned no matches.
- Capture dimensions are 1280x720 for both public launch captures.

Performance/freeze/log-health findings:
- No `FreezeDetect` lines were found in the inspected logs.
- `/private/tmp/warlinecapture-m01-public-launch.log` still contains one `[PerfDiag] slowUpdate` line with `total=31.3ms`, `cpuFrame=1445.4ms`, `units=0`, and `focused=0`; this should be classified by the owning implementation lane before final Gate 4, but it did not block acceptance of the visible-scene handoff.
- `/private/tmp/warlinecapture-m01-public-launch.log` still reports preview-scene and persistent-allocation leak warnings at shutdown.
- Licensing handshake/access-token and usbmuxd messages remain present in inspected Unity logs but did not prevent the focused tests from completing.

Known gaps:
- The captures are 1280x720 deterministic camera/canvas renders, not the required final 1920x1080 and 2400x1080 eight-state matrix.
- The captures prove readable launch state, but they do not yet prove real touch ergonomics, invalid-input recovery, assistant takeover/Stop behavior, or result popup behavior.
- Marker/VFX readiness remains open for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.
- The handoff documents a pre-existing `WarlineCaptureGameLaunchUtility` `Resources.FindObjectsOfTypeAll` cleanup gap outside the touched runtime files.

Cross-lane impacts:
- Gameplay/UI can treat the public visible-scene mismatch as QA/HCI-accepted for this handoff.
- QA/HCI should continue to block full manual HCI/balance readiness until remaining Gate 4 evidence lands.
- PM should still require owner classification for the slowUpdate/leak warnings or a documented benign waiver before final Gate 4.

Next recommended task:
Gameplay/UI should move to the remaining Gate 4 evidence: final capture matrix at `1920x1080` and `2400x1080`, touch/camera HCI proof, marker/VFX evidence or waiver, and classification of the remaining public-launch log warnings.

### QAHCI-G4-018 closure: Public launch visible gameplay evidence accepted

- Severity: Previously blocker; accepted for the public visible-scene handoff after revised captures.
- Affected lane: gameplay / UI.
- Reproduction steps:
  1. Read the revised `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`.
  2. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`.
  3. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`.
  4. Inspect whether the player-visible scene shows readable M01 production gameplay, HUD/objective context, and no old 3D prototype.
- Expected: Public campaign and Quick Custom launch evidence shows the intended M01 2D/isometric production slice with readable first-playable camera framing, visible HUD/objective context, command affordances, minimap, and old 3D visuals suppressed.
- Actual: Revised captures show readable Match HUD/objectives/threat feed/ARIA entry/unit cards/command bar/minimap and a closer M01 unit view. The old 3D prototype is not visible in the reviewed captures.
- Blocks next milestone: no for the public visible-scene blocker specifically. Yes for full Gate 4/manual balance readiness until remaining touch/camera, eight-state capture, marker/VFX, and log-health gaps close.
- Recommended owner: Gameplay/UI for remaining Gate 4 evidence; QA/HCI watch/validate.

## Updated waiting/blocker ownership fields after revised visible-scene acceptance

Waiting on lane:
Gameplay/UI and art-design/integration.

Waiting on exact file/report/asset/command:
- Final Gate 4 capture matrix for `1920x1080` and `2400x1080` covering match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, and result popup.
- Touch/camera HCI proof or manual/device smoke evidence for campaign and Quick Custom/Test public launch paths.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.
- Owner classification or waiver for the remaining `[PerfDiag] slowUpdate` and shutdown leak warnings in the public-launch PlayMode log.

Owner of next action:
Gameplay/UI owns the final capture/touch-camera/public-route evidence and log-warning classification. Art-design or the implementing lane owns marker/VFX asset readiness.

Can my lane still continue fallback work? no. QA/HCI has accepted the revised public visible-scene handoff and should wait for the next focused handoff covering the remaining Gate 4 blockers before rerunning affected checks.

## 2026-05-08 PM override: public launch visible scene still needs fixes

Lane:
QA/HCI

Task:
Record the PM rejection of the revised public-launch captures and correct the active QA/HCI gate state.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_pm_public-launch-brown-tiny-world-rejection.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. PM rejected the current captures because HUD chrome is visible, but the gameplay world remains a mostly flat brown field with tiny centered tactical content.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Read `Design/AgentReports/2026-05-08_pm_public-launch-brown-tiny-world-rejection.md`.
- Rechecked the current waiting/blocker ownership against the latest PM decision.

Validation result:
Needs fixes. The prior QA/HCI acceptance of the revised public visible-scene handoff is superseded by the PM gate ruling in `Design/AgentReports/2026-05-08_pm_public-launch-brown-tiny-world-rejection.md`. The public-launch captures are not accepted as production gameplay evidence because they show HUD chrome over a brown/blank-looking world with tiny tactical content, and do not match the accepted M01 map/readability references.

Known gaps:
- Public launch evidence still needs authored M01 tactical map/terrain visible, not a flat brown/blank field.
- Unit/target scale and camera composition need to be comparable to `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` and accepted integrated HUD captures.
- Full-screen campaign and Quick Custom/Test captures remain required if those routes are claimed manual-ready.
- The remaining Gate 4 blockers from the prior section still stand: final eight-state matrix, touch/camera proof, marker/VFX readiness, and log-health classification.

Cross-lane impacts:
- Gameplay/UI own the public visible-scene implementation/evidence fix.
- QA/HCI should not request user/manual balance testing from the current brown-field captures.
- PM updated Gameplay/UI tasking; QA/HCI should wait for the next revised public-launch handoff.

Next recommended task:
Gameplay/UI should provide revised full-screen public-launch captures for campaign and Quick Custom/Test paths showing the authored M01 tactical map/terrain, readable unit/target scale, HUD/objective/assistant context, route/mission/camera/map ids, old-3D visibility status, terrain visibility status, and capture paths.

### QAHCI-G4-019: PM rejected brown-field public launch evidence

- Severity: Blocker for manual HCI/balance readiness.
- Affected lane: gameplay / UI / art-design.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_pm_public-launch-brown-tiny-world-rejection.md`.
  2. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`.
  3. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`.
  4. Compare the gameplay world and camera scale against `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` and accepted integrated HUD captures.
- Expected: Public launch captures show authored M01 tactical map/terrain, readable unit/target scale, HUD/objective/assistant context, and no legacy 3D prototype.
- Actual: PM reports the captures show HUD chrome over a mostly flat brown field with tiny centered gameplay content; old 3D suppression alone is not sufficient evidence of a usable production scene.
- Blocks next milestone: yes. Manual HCI/balance QA remains blocked.
- Recommended owner: Gameplay/UI, with art-design support if terrain/readability assets are missing or miswired.

## Updated waiting/blocker ownership fields after PM brown-field rejection

Waiting on lane:
Gameplay/UI and art-design/integration.

Waiting on exact file/report/asset/command:
- Revised Gameplay/UI public-launch handoff with full-screen campaign and Quick Custom/Test captures showing authored M01 tactical map/terrain, readable unit/target scale, HUD/objective/assistant context, and no old 3D prototype.
- Report must state mission id, route, camera, map id, legacy 3D visibility, terrain visibility, and capture paths.
- Final Gate 4 capture matrix for `1920x1080` and `2400x1080` covering match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, and result popup.
- Touch/camera HCI proof or manual/device smoke evidence for campaign and Quick Custom/Test public launch paths.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.
- Owner classification or waiver for the remaining `[PerfDiag] slowUpdate` and shutdown leak warnings in the public-launch PlayMode log.

Owner of next action:
Gameplay/UI owns the public visible-scene brown-field blocker and final capture/touch-camera evidence. Art-design or the implementing lane owns terrain/readability assets if missing or miswired, plus marker/VFX readiness.

Can my lane still continue fallback work? no. QA/HCI has recorded the PM rejection and should wait for the next revised public-launch handoff before rerunning affected checks.

## 2026-05-08 Gameplay public launch authored-terrain handoff validation

Lane:
QA/HCI

Task:
Validate `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md` against the public-launch brown-field/tiny-world blocker.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_public-launch-brown-tiny-world-rejection.md`
- `Design/AgentReports/2026-05-08_pm_unity-workspace-lane-priority.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. The Gameplay handoff claims public campaign and Quick Custom launch now show authored M01 tactical terrain, readable squad/enemy scale, HUD/objective/assistant context, no legacy `UI_Canvas`, no old 3D prototype, and no flat brown/blank gameplay field.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Read `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`.
- Reviewed capture evidence:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01-20x9.png`
- Checked capture dimensions with `sips -g pixelWidth -g pixelHeight`.
- Inspected Gameplay-provided Unity result XML and logs:
  - `/private/tmp/warlinecapture-m01-public-launch-results.xml`
  - `/private/tmp/warlinecapture-m01-public-launch.log`
  - `/private/tmp/warlinecapture-m01-public-launch-playmode.log`
- Ran the banned runtime lookup scan on the touched runtime files listed in the Gameplay handoff.
- Attempted the required QA/HCI PlayMode rerun in the assigned workspace:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-qa-hci-public-launch-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-launch.log`
- Stopped the stuck Unity process after licensing initialization failed before tests started.

Validation result:
Blocked for final QA/HCI automation acceptance because the assigned QA workspace run did not reach tests and did not produce `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml`. Visual QA of the handoff captures is accepted for the brown-field/tiny-world issue: the captures now show authored tactical terrain, road/yard detail, readable command squad and enemy patrol scale, HUD/objective/assistant context, and no obvious old 3D prototype or flat blank brown world.

Gameplay-provided automated evidence observed:
- `/private/tmp/warlinecapture-m01-public-launch-results.xml`: `Chapter01M01PlayModeValidationTests` 5/5 passed, but the XML path references `/Users/farhad/Projects/WarlineCapture-CodexUnity`, not the assigned QA workspace.
- Handoff states a clean WarlineCapture-CodexUnity2 run also passed, but the current XML available at `/private/tmp/warlinecapture-m01-public-launch-results.xml` is from WarlineCapture-CodexUnity.
- Runtime banned lookup scan on the listed touched runtime files returned no matches.

QA workspace blocker:
- Command: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-qa-hci-public-launch-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-launch.log`
- Log path: `/private/tmp/warlinecapture-qa-hci-public-launch.log`
- Symptom: Unity reported licensing initialization failure immediately after startup, never emitted test XML, and had to be stopped.
- Missing file: `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml`

Performance/freeze/log-health findings:
- No `FreezeDetect` lines were found in the inspected Gameplay logs.
- `/private/tmp/warlinecapture-m01-public-launch-playmode.log` still contains one `[PerfDiag] slowUpdate` line with `total=33.8ms`, `cpuFrame=988.1ms`, `units=0`, and `focused=0`.
- Gameplay logs still report preview-scene and persistent-allocation leak warnings at shutdown.
- Licensing/access-token/usbmuxd messages remain present in inspected logs.

Known gaps:
- QA/HCI cannot mark the handoff fully accepted until the assigned QA workspace PlayMode run passes or PM explicitly accepts cross-lane Unity results plus visual QA as sufficient.
- The four public-launch captures are 1280x720 and 1600x720; they are not the final required 1920x1080 and 2400x1080 eight-state matrix.
- Touch/camera ergonomics, invalid-input recovery, assistant takeover/Stop, and result popup behavior remain unverified.
- Marker/VFX readiness remains open for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.

Cross-lane impacts:
- Gameplay appears to have fixed the visible authored-terrain/readability portion of the public launch blocker, pending clean QA-workspace automation or PM waiver.
- QA/HCI should not ask the user for full M01 balance testing until the remaining Gate 4 blockers close.
- PM/user may need to restore Unity licensing for `/Users/farhad/Projects/WarlineCapture-CodexUnity3` or explicitly authorize acceptance based on Gameplay/UI workspace runs and QA visual review.

Next recommended task:
Restore QA workspace Unity licensing/health and rerun `Chapter01M01PlayModeValidationTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`, or have PM explicitly accept the Gameplay/UI workspace PlayMode results with QA visual review for this specific handoff. After that, continue with the final capture matrix, touch/camera proof, marker/VFX readiness, and log-health classification.

### QAHCI-G4-020: QA workspace PlayMode validation blocked by Unity licensing

- Severity: Blocker for final QA/HCI automation acceptance; visual public-launch evidence is acceptable pending clean QA validation or PM waiver.
- Affected lane: support-FTUE / UI / gameplay / support tooling.
- Reproduction steps:
  1. Run the QA/HCI PlayMode command listed above in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
  2. Inspect `/private/tmp/warlinecapture-qa-hci-public-launch.log`.
  3. Check for `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml`.
- Expected: Unity starts in the assigned QA workspace, runs `Chapter01M01PlayModeValidationTests`, and writes passing XML.
- Actual: Unity reports licensing initialization failure before tests start, writes no XML, and hangs until stopped.
- Blocks next milestone: yes for QA automation acceptance of this handoff; not a gameplay-code failure based on available cross-lane test/capture evidence.
- Recommended owner: PM/tooling or whoever owns Unity licensing/workspace health for `WarlineCapture-CodexUnity3`.

### QAHCI-G4-019 visual update: authored terrain evidence now acceptable pending automation

- Severity: Previously blocker; visually acceptable pending clean QA-workspace validation or PM waiver.
- Affected lane: gameplay / UI / art-design.
- Reproduction steps:
  1. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`.
  2. Open `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`.
  3. Open the 20:9 variants in the same folder.
  4. Compare against the brown-field rejection criteria in `Design/AgentReports/2026-05-08_pm_public-launch-brown-tiny-world-rejection.md`.
- Expected: Authored M01 tactical map/terrain, readable unit/target scale, HUD/objective/assistant context, and no legacy 3D prototype.
- Actual: Captures show authored terrain/roads/yards, readable squad/enemy scale, visible HUD/objective/assistant context, and no obvious old 3D prototype.
- Blocks next milestone: yes until QA automation blocker is resolved and remaining Gate 4 checks close.
- Recommended owner: QA/HCI for rerun after Unity workspace health is restored; Gameplay/UI for any follow-up if PM rejects the visual evidence.

## Updated waiting/blocker ownership fields after Gameplay public-launch handoff validation

Waiting on lane:
PM/tooling for QA Unity workspace health; Gameplay/UI and art-design/integration for remaining Gate 4 evidence.

Waiting on exact file/report/asset/command:
- Passing QA/HCI workspace rerun XML for `Chapter01M01PlayModeValidationTests` at `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml`, or explicit PM waiver accepting cross-lane Unity results plus QA visual review.
- Final Gate 4 capture matrix for `1920x1080` and `2400x1080` covering match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, and result popup.
- Touch/camera HCI proof or manual/device smoke evidence for campaign and Quick Custom/Test public launch paths.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.
- Owner classification or waiver for the remaining `[PerfDiag] slowUpdate` and shutdown leak warnings in the public-launch PlayMode logs.

Owner of next action:
PM/tooling owns the QA workspace licensing blocker or waiver decision. Gameplay/UI own remaining final capture/touch-camera evidence and log-warning classification. Art-design or the implementing lane owns marker/VFX readiness.

Can my lane still continue fallback work? no. QA/HCI completed visual review and attempted the assigned validation run. Further QA/HCI validation requires QA workspace Unity health or PM waiver.

## 2026-05-08 UI public launch authored-terrain handoff follow-up validation

Lane:
QA/HCI

Task:
Validate the updated UI public-launch handoff after PM reviewed the Gameplay authored-terrain handoff.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-path-review.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. The UI handoff uses the improved authored-terrain captures and reports that the HUD/canvas composition is visible over the M01 production slice.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Read the updated `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`.
- Read `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-path-review.md`.
- Rechecked the current capture file list and dimensions under `Design/AgentReports/Captures/2026-05-08_m01-public-launch/`.
- Did not rerun Unity because the immediately prior QA/HCI assigned-workspace run failed at licensing initialization before tests started, and PM's latest review identifies implementation/evidence blockers independent of another QA rerun.

Validation result:
Needs fixes / blocked. The UI canvas/capture composition is visibly improved and includes the 16:9 and 20:9 public-launch captures, but this handoff cannot be accepted while PM's latest Gate 4 review rejects the underlying world evidence for missing ECS source-of-truth proof. The UI report also uses `WarlineCapture-CodexUnity2` validation; QA/HCI's assigned `WarlineCapture-CodexUnity3` validation remains blocked by Unity licensing and has not produced passing XML.

Known gaps:
- PM requires proof or implementation changes so every visible non-Canvas world object in the M01 tactical slice is ECS-backed, not standalone world GameObject/SpriteRenderer proof.
- The UI report's runtime banned-lookup scan still documents `Transform.Find` in `TacticalMapRuntimeLoader`; PM has not accepted that as final Gate 4 compliant.
- The UI report is validated from the UI workspace, not the QA/HCI workspace.
- The final `1920x1080` and `2400x1080` eight-state Gate 4 matrix, touch/camera proof, marker/VFX readiness, and log-warning classification remain open.

Cross-lane impacts:
- UI appears to own and provide the HUD/capture composition portion, but Gameplay still owns ECS source-of-truth for terrain/map/decor/markers and should revise the implementation/handoff.
- QA/HCI should not treat the improved captures as manual-ready until the ECS architecture proof and QA validation blocker are resolved.
- PM/tooling still owns the QA workspace licensing/health blocker or waiver decision.

Next recommended task:
Gameplay should satisfy the ECS world-source rule or get explicit PM acceptance for the terrain/map presentation approach. UI should keep the full-screen capture composition ready, but should not own the world-source fix. QA/HCI should rerun from `WarlineCapture-CodexUnity3` once Unity licensing is healthy or after PM grants a specific waiver.

### QAHCI-G4-021: UI handoff blocked by Gameplay ECS world-source proof and QA workspace licensing

- Severity: Blocker for final Gate 4/manual HCI readiness.
- Affected lane: gameplay / UI / support tooling.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_ui_m01-public-launch-path.md`.
  2. Read `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-path-review.md`.
  3. Inspect the four captures under `Design/AgentReports/Captures/2026-05-08_m01-public-launch/`.
  4. Attempt the QA/HCI workspace PlayMode rerun from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- Expected: UI provides full-screen public launch HUD/canvas evidence over a Gameplay-approved ECS-backed world, and QA/HCI workspace tests pass.
- Actual: Visual composition is improved, but PM still requires ECS source-of-truth proof for visible world objects; QA/HCI workspace validation remains blocked by Unity licensing before tests start.
- Blocks next milestone: yes.
- Recommended owner: Gameplay for ECS world-source proof/fix, UI for capture composition maintenance, PM/tooling for QA workspace licensing or waiver.

## Updated waiting/blocker ownership fields after UI handoff follow-up

Waiting on lane:
Gameplay, UI, and PM/tooling.

Waiting on exact file/report/asset/command:
- Revised Gameplay handoff proving or fixing ECS source-of-truth for visible M01 terrain/map/decor/markers/non-Canvas world objects.
- Updated UI handoff only if capture composition changes after the Gameplay world-source fix.
- Passing QA/HCI workspace rerun XML for `Chapter01M01PlayModeValidationTests` at `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml`, or explicit PM waiver accepting cross-lane Unity results plus QA visual review.
- Final Gate 4 capture matrix for `1920x1080` and `2400x1080` covering match start, squad selected, move feedback, attack feedback, invalid command recovery, assistant open, assistant takeover/Stop, and result popup.
- Touch/camera HCI proof, marker/VFX evidence or waiver, and log-warning classification.

Owner of next action:
Gameplay owns ECS world-source proof/fix. UI owns capture/HUD composition if changed. PM/tooling owns QA workspace licensing or waiver. Art-design or implementing lane owns marker/VFX readiness.

Can my lane still continue fallback work? no. QA/HCI has validated the available handoffs to the current blocker boundary and is waiting on Gameplay/PM before rerunning affected checks.

## 2026-05-08 QA workspace licensing recheck

Lane:
QA/HCI

Task:
Recheck Unity licensing health for the assigned QA/HCI workspace before attempting another focused public-launch PlayMode validation.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentTasks/qa-hci_current.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI.

Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -logFile /private/tmp/warlinecapture-qa-hci-license-recheck.log`
- Stopped the stuck Unity and LicensingClient processes after licensing initialization failed.

Validation result:
Blocked. The assigned QA/HCI workspace still fails during Unity licensing initialization before any focused PlayMode test can run. The recheck log shows Unity could not connect to `LicenseClient-farhad`, launched `Unity.Licensing.Client`, then timed out waiting for `LicenseClient-farhad-6000.4.0` and logged `Licensing initialization failed after 74.83s`.

Known gaps:
- `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml` is still missing because the QA/HCI workspace cannot reach test execution.
- No additional public-launch PlayMode evidence was produced from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- PM/tooling still needs to restore Unity licensing health or provide an explicit waiver for QA/HCI workspace rerun evidence.

Cross-lane impacts:
- Gameplay/UI handoffs cannot receive final QA/HCI automation acceptance from the assigned QA workspace while licensing blocks test startup.
- The separate Gameplay ECS world-source proof/fix blocker remains open and is not resolved by this licensing recheck.

Next recommended task:
PM/tooling should restore Unity licensing for `/Users/farhad/Projects/WarlineCapture-CodexUnity3` or explicitly waive the QA/HCI workspace rerun requirement so QA/HCI can close the validation path against accepted evidence.

### QAHCI-G4-020 update: QA workspace licensing still blocks validation

- Severity: Blocker for final QA/HCI automation acceptance.
- Affected lane: support tooling / QA-HCI.
- Reproduction steps:
  1. Run Unity 6000.4.0f1 in batch mode against `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
  2. Use `-quit` and `-logFile /private/tmp/warlinecapture-qa-hci-license-recheck.log` to isolate licensing from test execution.
  3. Inspect the licensing section of the log.
- Expected: Unity initializes licensing and exits cleanly, allowing the next focused PlayMode validation command to run.
- Actual: Unity cannot connect to the licensing client channel, times out after 60 seconds waiting for `LicenseClient-farhad-6000.4.0`, then reports licensing initialization failure after 74.83 seconds.
- Blocks next milestone: yes, for QA/HCI automation acceptance from the assigned workspace.
- Recommended owner: PM/tooling.

## Updated waiting/blocker ownership fields after licensing recheck

Waiting on lane:
PM/tooling and Gameplay.

Waiting on exact file/report/asset/command:
- Restored Unity licensing health for `/Users/farhad/Projects/WarlineCapture-CodexUnity3`, or explicit PM waiver accepting cross-lane Unity results plus QA/HCI visual review.
- Passing QA/HCI workspace rerun XML for `Chapter01M01PlayModeValidationTests` at `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml` once licensing is healthy.
- Revised Gameplay handoff proving or fixing ECS source-of-truth for visible M01 terrain/map/decor/markers/non-Canvas world objects.

Owner of next action:
PM/tooling owns the licensing blocker or waiver. Gameplay owns ECS world-source proof/fix.

Can my lane still continue fallback work? no. QA/HCI rechecked licensing directly and remains blocked before test execution in the assigned workspace.

## 2026-05-08 QA workspace licensing repeat recheck

Lane:
QA/HCI

Task:
Repeat the Unity licensing health check for the assigned QA/HCI workspace after the earlier licensing failure.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI.

Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -logFile /private/tmp/warlinecapture-qa-hci-license-recheck-2.log`
- Stopped the stuck Unity and LicensingClient processes after licensing initialization failed again.

Validation result:
Blocked, unchanged. Unity again failed before test execution. The repeat log shows `LicenseClient-farhad` connection failure, missing `LicenseClient-farhad-6000.4.0`, `Unity.Licensing.Client` launch, a 60 second wait timeout, and `Licensing initialization failed after 74.84s`.

Known gaps:
- `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml` is still missing.
- No new QA/HCI PlayMode validation evidence was produced from `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.

Cross-lane impacts:
- Final QA/HCI automation acceptance remains blocked on PM/tooling restoring Unity licensing or waiving the assigned-workspace rerun.
- Gameplay ECS world-source proof/fix remains a separate milestone blocker.

Next recommended task:
PM/tooling should fix Unity licensing for `/Users/farhad/Projects/WarlineCapture-CodexUnity3` or issue a specific waiver for QA/HCI to accept cross-lane Unity results plus visual review.

### QAHCI-G4-020 repeat update: licensing issue persists

- Severity: Blocker for final QA/HCI automation acceptance.
- Affected lane: support tooling / QA-HCI.
- Reproduction steps:
  1. Run Unity 6000.4.0f1 in batch mode against `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
  2. Log to `/private/tmp/warlinecapture-qa-hci-license-recheck-2.log`.
  3. Wait for licensing initialization to complete.
- Expected: Unity initializes licensing and exits cleanly.
- Actual: Unity times out waiting for `LicenseClient-farhad-6000.4.0` and reports licensing initialization failure after 74.84 seconds.
- Blocks next milestone: yes, for QA/HCI automation acceptance from the assigned workspace.
- Recommended owner: PM/tooling.

## 2026-05-08 Hub-open licensing clarification

Lane:
QA/HCI

Task:
Recheck the licensing blocker after the project was opened successfully from Unity Hub UI.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI.

Validation run:
- User confirmed `/Users/farhad/Projects/WarlineCapture-CodexUnity3` opens from Unity Hub UI.
- QA/HCI reran the isolated Codex/headless command:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -logFile /private/tmp/warlinecapture-qa-hci-license-recheck-after-hub.log`
- Stopped the stuck headless Unity and LicensingClient processes after the same timeout.

Validation result:
Blocked for Codex-driven batchmode only. The user-observed Hub UI launch means the project and interactive Unity license path are not generally blocked. The remaining blocker is narrower: the Codex/headless batchmode process cannot connect to `LicenseClient-farhad`, cannot find `LicenseClient-farhad-6000.4.0`, launches `Unity.Licensing.Client`, then times out after 60 seconds and logs `Licensing initialization failed after 74.83s`.

Expected vs actual:
- Expected: Opening the project from Hub should leave licensing available to the follow-up batchmode validation command, or batchmode should initialize licensing independently.
- Actual: Hub UI can open the project, but Codex/headless batchmode still fails licensing initialization before tests start.

Cross-lane impacts:
- This is no longer evidence that the QA workspace cannot open in Unity UI.
- It remains a blocker for automated QA/HCI evidence generated from Codex batchmode.

Recommended owner:
PM/tooling, specifically for Codex/headless Unity batchmode licensing or for approving a GUI/Test Runner/manual waiver path.

## 2026-05-08 licensing resolution: escalated batchmode works

Lane:
QA/HCI

Task:
Verify whether the Unity licensing failure is caused by Codex sandbox isolation.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI.

Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -logFile /private/tmp/warlinecapture-qa-hci-license-recheck-escalated.log`
- Command was run with Codex escalation/out-of-sandbox execution.

Validation result:
Accepted for licensing health. The same batchmode launch that failed inside the sandbox succeeded when run with escalation. The log shows `[Licensing::Client] Successfully resolved entitlement details`, Package Manager registration, AssetDatabase refresh, and process exit code 0.

Expected vs actual:
- Expected: Unity batchmode should initialize licensing and exit cleanly when it can access the user's Unity Licensing Client/keychain/session services.
- Actual: Escalated/out-of-sandbox batchmode initializes licensing successfully. The previous failures were sandbox isolation issues, not a broken Unity Hub license or broken `WarlineCapture-CodexUnity3` workspace.

Cross-lane impacts:
- QA/HCI can proceed with focused Unity validation by running required Unity batchmode commands with Codex escalation/out-of-sandbox execution.
- PM/tooling no longer needs to restore Unity licensing for the GUI project path; the remaining operational requirement is approving/running Unity batchmode outside the sandbox when tests are needed.

Next recommended task:
Rerun the focused QA/HCI PlayMode validation command for `Chapter01M01PlayModeValidationTests` from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` with Codex escalation/out-of-sandbox execution.

### QAHCI-G4-020 resolution update: licensing healthy under escalated batchmode

- Severity: Resolved for licensing; automation still needs the focused PlayMode rerun.
- Affected lane: support tooling / QA-HCI.
- Reproduction steps:
  1. Run Unity 6000.4.0f1 batchmode against `/Users/farhad/Projects/WarlineCapture-CodexUnity3` from Codex without escalation.
  2. Observe licensing client timeout.
  3. Rerun the same command with Codex escalation/out-of-sandbox execution.
- Expected: The escalated command can access Unity licensing services and initialize successfully.
- Actual: Escalated batchmode resolved entitlement details and exited cleanly.
- Blocks next milestone: no for licensing itself; yes until the focused PlayMode validation command is rerun successfully.
- Recommended owner: QA/HCI for escalated validation rerun; PM/tooling only if escalation approval is unavailable.

## 2026-05-08 QA workspace focused PlayMode rerun after licensing resolution

Lane:
QA/HCI

Task:
Rerun the focused public-launch QA/HCI PlayMode validation from the assigned QA workspace using escalated/out-of-sandbox Unity batchmode.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI.

Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-qa-hci-public-launch-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-launch.log`
- Command was run with Codex escalation/out-of-sandbox execution.

Validation result:
Accepted for the focused QA/HCI PlayMode rerun. Unity licensing initialized successfully under escalated batchmode, the test runner exited with code 0, and `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml` reports `total="5" passed="5" failed="0"`.

Evidence:
- `Chapter01M01PlayModeValidationTests.GameScene_M01BuildRejectionUsesSharedFeedbackReason`: passed.
- `Chapter01M01PlayModeValidationTests.GameScene_M01RuntimeSpawnsVisibleAnchoredSquadsAndStartsAtCameraAnchor`: passed.
- `Chapter01M01PlayModeValidationTests.GameScene_M01SelectionAttackAndResultRouteRespectSurvivalGuard`: passed.
- `Chapter01M01PlayModeValidationTests.PublicCampaignLaunch_ReachesM01ProductionVisibleSlice`: passed.
- `Chapter01M01PlayModeValidationTests.PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute`: passed.

Known gaps:
- The PlayMode log still reports preview-scene and persistent-allocation leak warnings during shutdown.
- This rerun does not close PM's separate Gameplay ECS world-source proof/fix blocker.
- This rerun does not replace the final `1920x1080` and `2400x1080` eight-state capture matrix, touch/camera proof, marker/VFX readiness, or log-warning classification.

Cross-lane impacts:
- QA/HCI no longer has a licensing blocker for the focused public-launch PlayMode suite when Unity is run with Codex escalation/out-of-sandbox execution.
- Gameplay still owns ECS world-source proof/fix if PM continues to require it before final Gate 4 acceptance.

Next recommended task:
Treat future Unity batchmode validation from Codex as requiring escalation/out-of-sandbox execution. Continue remaining Gate 4 validation only after Gameplay/PM resolve the ECS world-source blocker or provide a specific waiver.

### QAHCI-G4-020 closure: focused QA workspace PlayMode rerun passed

- Severity: Closed for licensing and focused PlayMode automation; remaining Gate 4 blockers are separate.
- Affected lane: QA-HCI / gameplay / support tooling.
- Reproduction steps:
  1. Run the focused PlayMode command above from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` using Codex escalation/out-of-sandbox execution.
  2. Inspect `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml`.
  3. Inspect `/private/tmp/warlinecapture-qa-hci-public-launch.log` for shutdown warnings.
- Expected: Unity licensing initializes, the focused suite runs, and all public-launch tests pass.
- Actual: Licensing initialized, test runner exited with code 0, and 5/5 tests passed. Shutdown leak warnings remain in the log.
- Blocks next milestone: no for QAHCI-G4-020; yes only for remaining separate Gate 4 items.
- Recommended owner: QA/HCI watch for reruns; PM/tooling only if escalation is unavailable in future.

## 2026-05-08 QA/HCI validation of UI safe-area profile matrix fix

Lane:
QA/HCI

Task:
Validate the PM-accepted UI safe-area profile matrix fix for the named Gate 4 simulated profiles `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_ui_m01-safe-area-profile-matrix-fix.md`
- `Design/AgentReports/2026-05-08_pm_ui-m01-safe-area-profile-matrix-fix-review.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. This validates simulated safe-area evidence only.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Read `Design/AgentReports/2026-05-08_ui_m01-safe-area-profile-matrix-fix.md`.
- Read PM acceptance in `Design/AgentReports/2026-05-08_pm_ui-m01-safe-area-profile-matrix-fix-review.md`.
- Listed artifacts under `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`.
- Verified PNG dimensions with `sips -g pixelWidth -g pixelHeight`.
- Scanned the three profile manifests for profile id, resolution, safe-area mode, cutout rectangles, per-surface pass/fail notes, invalid-command reason-code status, and marker/VFX status.
- Visually reviewed `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/M01_SafeAreaProfile_CaptureMatrix_ContactSheet.png`.

Validation result:
Accepted for `QAHCI-G4-011` simulated safe-area profile evidence. The artifact folder contains the required 24 state PNGs across the three named profiles, plus a contact sheet and three manifests. `safe.none_16x9` captures are 1920x1080; `safe.rounded_20x9` and `safe.cutout_left_20x9` captures are 2400x1080. The manifests include explicit profile IDs, safe-area assumptions, cutout data where applicable, per-surface pass notes for HUD/objective/threat/squad/command/minimap/assistant/result surfaces, invalid-command reason-code status, and marker/VFX status.

Known gaps:
- This acceptance is simulated safe-area evidence, not real-device Android cutout validation.
- Runtime canonical reason-code closure remains separate from this UI evidence, although the manifest now documents that scope.
- Marker/VFX readiness remains separate; the manifests classify marker visualizations as temporary and `vfx.unit.destroyed.small` as absent from the UI safe-area capture.
- Human touch/camera ergonomics remain unverified by this artifact review.
- Broader Gate 4/manual balance readiness remains blocked until the current public-launch/ECS/source-of-truth and remaining touch/camera/log/art readiness items are resolved or waived.

Cross-lane impacts:
- UI no longer owns the named simulated safe-area profile evidence gap unless later QA/PM changes the required profile matrix.
- QA/HCI can close `QAHCI-G4-011` for simulated safe-area evidence.
- Art/design or implementing lanes still own marker/VFX readiness.
- Gameplay/PM still own the currently tracked public-launch/ECS world-source blocker.

Next recommended task:
Do not rerun this safe-area profile check unless UI changes the capture tooling or PM changes the required profile matrix. Continue waiting for the remaining Gate 4 handoffs before manual HCI/balance.

## Current balance-QA gate status after safe-area profile validation

Blocked. Simulated safe-area profile evidence is accepted, but active balance QA remains invalid until public player launch/world-source readiness, human touch/camera ergonomics, marker/VFX readiness or waiver, and remaining log-health/art-readiness questions are resolved.

## QA Unity workspace smoke checks run or deferred

- New Unity run for this safe-area profile validation: deferred. PM accepted the UI capture command/test results, and QA/HCI validated artifacts directly.
- Latest QA workspace focused public-launch PlayMode rerun remains `/private/tmp/warlinecapture-qa-hci-public-launch-results.xml`, with `Chapter01M01PlayModeValidationTests` passed 5/5 under escalated batchmode.

## Performance/freeze/log-health findings

- No new runtime log was generated by this safe-area artifact review.
- Existing QA PlayMode rerun still has shutdown preview-scene and persistent-allocation leak warnings; no new freeze, input stall, or severe route-runtime failure was introduced by the artifact review.

## New HCI risks introduced by latest handoff

No new blocker introduced. Residual risk is that simulated safe-area captures can pass while real-device touch/cutout behavior still differs; that remains a device/manual validation risk, not a blocker against the accepted simulated profile evidence.

### QAHCI-G4-011 closure: named simulated safe-area profile matrix accepted

- Severity: Closed for simulated safe-area evidence; remaining real-device/touch risks are separate.
- Affected lane: UI / QA-HCI.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_ui_m01-safe-area-profile-matrix-fix.md`.
  2. Read `Design/AgentReports/2026-05-08_pm_ui-m01-safe-area-profile-matrix-fix-review.md`.
  3. Inspect `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`.
  4. Verify there are eight state captures each for `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.
  5. Verify the profile manifests include safe-area assumptions, cutout rectangles where applicable, and per-surface pass notes.
- Expected: The three named PM profiles exist with explicit safe-area assumptions, required state coverage, valid dimensions, and surface-clearance notes.
- Actual: All three named profiles are present; 24 state captures exist; dimensions match 1920x1080 or 2400x1080 as required; manifests include profile IDs, cutout data, per-surface notes, reason-code scope, and marker/VFX scope.
- Blocks next milestone: no for simulated safe-area profile evidence. Gate 4 remains blocked by separate public-launch/world-source, touch/camera, marker/VFX, and final-readiness items.
- Recommended owner: QA/HCI watch only; UI if the profile matrix changes.

## Updated waiting/blocker ownership fields after safe-area profile validation

Waiting on lane:
Gameplay/PM, QA-HCI, and art-design/integration.

Waiting on exact file/report/asset/command:
- Revised Gameplay/PM handoff proving or accepting ECS source-of-truth for visible M01 terrain/map/decor/markers/non-Canvas world objects, or explicit PM waiver.
- Touch/camera HCI proof or manual/device smoke evidence for campaign and Quick Custom/Test public launch paths.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.
- Owner classification or waiver for remaining shutdown leak warnings if PM treats them as Gate 4 blocking.

Owner of next action:
Gameplay/PM owns the public-launch/ECS world-source decision. QA/HCI owns the next affected validation rerun after a handoff or waiver. Art-design or implementing lanes own marker/VFX readiness.

Can my lane still continue fallback work? no. QA/HCI validated the accepted safe-area profile handoff to closure and should not start unrelated QA until the next active Gate 4 handoff lands.

## 2026-05-08 QA/HCI public launch ground-orientation follow-up

Lane:
QA/HCI

Task:
Validate the latest Gameplay public-launch/ground-orientation handoff status against the active QA/HCI priority.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-ground-orientation-review.md`
- `Design/AgentReports/2026-05-08_ui_m01-public-launch-waiting-on-gameplay-ecs.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. Latest captures appear to improve the user-reported upside-down tactical ground issue, but public-launch/manual readiness is not accepted.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Read `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`.
- Read PM review in `Design/AgentReports/2026-05-08_pm_gameplay-m01-ground-orientation-review.md`.
- Read UI status report `Design/AgentReports/2026-05-08_ui_m01-public-launch-waiting-on-gameplay-ecs.md`.
- Deferred new Unity rerun because PM review rejects the handoff for implementation/evidence blockers independent of another QA/HCI rerun.

Validation result:
Needs fixes / blocked. QA/HCI accepts PM's finding that the latest public-launch captures show visual progress on ground orientation: authored terrain, road direction, HUD, squad, enemy patrol, and no obvious upside-down tactical plate. Gate 4/manual readiness remains blocked because the handoff still does not prove ECS source-of-truth for every non-Canvas visible world object, especially the visible tactical ground/map surface. The PM review also flags touched PlayMode validation broad lookup usage, especially `GetComponentInChildren`, as unresolved.

Known gaps:
- Visible terrain/map ownership still needs ECS-backed proof or implementation revision.
- Touched validation needs broad lookup cleanup or explicit PM-accepted justification.
- QA/HCI should not treat the current public launch as manual-ready until PM accepts the revised Gameplay handoff or grants a specific waiver.
- Touch/camera HCI proof and marker/VFX readiness or waiver remain separate blockers.

Cross-lane impacts:
- Gameplay owns the ECS world-source proof/fix and touched-test lookup cleanup.
- UI has no new owner action unless a later reviewed handoff identifies a HUD/canvas/capture-composition issue.
- QA/HCI should rerun affected public-launch checks only after the revised Gameplay handoff or PM waiver.

Next recommended task:
Gameplay should revise the public M01 launch implementation/report to satisfy the ECS world-source rule, remove or justify touched broad lookup usage, and rerun focused validation from `/Users/farhad/Projects/WarlineCapture-CodexUnity`.

### QAHCI-G4-022: Public launch visual orientation improved, but ECS world-source proof still blocks Gate 4

- Severity: Blocker for Gate 4/manual HCI readiness.
- Affected lane: gameplay / QA-HCI.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`.
  2. Read `Design/AgentReports/2026-05-08_pm_gameplay-m01-ground-orientation-review.md`.
  3. Inspect the current public-launch captures under `Design/AgentReports/Captures/2026-05-08_m01-public-launch/`.
  4. Review the PM finding against visible non-Canvas world objects and touched test lookup usage.
- Expected: Public launch evidence proves the player-visible M01 tactical world is correctly oriented and ECS-backed for all non-Canvas world objects, with validation avoiding broad hierarchy discovery unless explicitly justified.
- Actual: Visual orientation appears improved, but PM still rejects the handoff because visible tactical ground/map proof depends on standalone world presentation and touched validation still uses broad child-component lookup.
- Blocks next milestone: yes.
- Recommended owner: Gameplay.

## Updated waiting/blocker ownership fields after ground-orientation follow-up

Waiting on lane:
Gameplay, then QA/HCI.

Waiting on exact file/report/asset/command:
- Revised `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md` or successor report proving/fixing ECS-backed visible terrain/map/decor/markers/non-Canvas world objects.
- Gameplay validation command in `/Users/farhad/Projects/WarlineCapture-CodexUnity` after cleanup.
- PM acceptance or explicit waiver for the ECS world-source and touched-test lookup findings.
- QA/HCI public-launch rerun from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` after that acceptance/waiver.

Owner of next action:
Gameplay owns the blocking fix/proof. QA/HCI owns the next affected validation rerun after PM acceptance or waiver.

Can my lane still continue fallback work? no. QA/HCI has validated the latest handoff state to the blocker boundary and should not start unrelated QA.

## 2026-05-08 QA/HCI update after PM ECS terrain contract audit

Lane:
QA/HCI

Task:
Process the latest PM design-audit report on the ECS-backed tactical terrain acceptance contract.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_pm_design-audit-ecs-terrain-contract-gap.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Checked `Design/AgentReports/` for reports newer than this QA/HCI rerun report.
- Read `Design/AgentReports/2026-05-08_pm_design-audit-ecs-terrain-contract-gap.md`.
- Deferred Unity validation because the new PM audit is a contract/acceptance blocker, not a fix handoff with new runtime evidence to validate.

Validation result:
Blocked. The PM audit confirms that the public-launch/ECS terrain blocker is not ready for QA/HCI rerun because the concrete ECS terrain presentation acceptance contract is underdefined. QA/HCI cannot accept or reject a future terrain/map proof consistently until Gameplay/PM define the named ECS component or tag, stable map entity id, runtime presentation link, owned/referenced map data, and no-broad-lookup validation path.

Known gaps:
- No named ECS terrain presentation component/tag is defined for the visible tactical ground/map surface.
- No stable terrain/map entity id tied to `iso.ch01.district_edge_01` is specified.
- No explicit runtime link is defined proving the visible ground SpriteRenderer is ECS-driven rather than independent world state.
- Validation path requirements still need to prohibit hierarchy/broad lookup and prefer explicit bootstrap/binder/provider references or ECS queries.

Cross-lane impacts:
- Gameplay cannot produce a final accepted ECS terrain proof until PM clarifies or Gameplay proposes the exact contract.
- QA/HCI should not rerun public-launch/manual readiness against the current evidence because the acceptance target is now explicitly underdefined.
- UI has no new action except as a consumer of final accepted captures.

Next recommended task:
PM should update `Design/AgentTasks/gameplay_current.md` with the concrete terrain ECS presentation acceptance contract, or Gameplay should propose the exact component/entity contract in its next handoff before further implementation.

### QAHCI-G4-023: ECS terrain acceptance contract underdefined

- Severity: Blocker for public-launch/manual HCI readiness.
- Affected lane: gameplay / QA-HCI / PM.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_pm_design-audit-ecs-terrain-contract-gap.md`.
  2. Compare the active public-launch ECS requirement against existing terrain/map runtime evidence.
  3. Attempt to identify the named ECS terrain presentation component/tag, stable map entity id, runtime presentation link, and no-broad-lookup validation route.
- Expected: Gameplay has a concrete contract for proving the visible tactical terrain/map surface is ECS-backed.
- Actual: The contract is underdefined, so QA/HCI cannot consistently validate a future terrain proof or mark the public launch manual-ready.
- Blocks next milestone: yes.
- Recommended owner: PM for contract clarification, then Gameplay for implementation/proof.

## Updated waiting/blocker ownership fields after ECS terrain contract audit

Waiting on lane:
PM, then Gameplay.

Waiting on exact file/report/asset/command:
- Updated `Design/AgentTasks/gameplay_current.md` with the concrete ECS terrain presentation acceptance contract, or a Gameplay handoff proposing the exact component/entity contract for PM acceptance.
- Revised Gameplay public-launch handoff after the terrain contract is defined.
- QA/HCI public-launch rerun from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` after PM accepts the contract and Gameplay evidence.

Owner of next action:
PM owns the contract clarification. Gameplay owns the subsequent implementation/proof. QA/HCI owns the rerun after acceptance.

Can my lane still continue fallback work? no. The current blocker is an acceptance-contract gap, not a QA-executable runtime check.

## 2026-05-08 QA/HCI validation of accepted Gameplay ECS terrain public-launch handoff

Lane:
QA/HCI

Task:
Validate the PM-accepted Gameplay public-launch ECS terrain proof handoff from the assigned QA/HCI workspace.

Files changed:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Contracts touched:
- `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`
- `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-ecs-terrain-review.md`
- No production source, prefab, task, or design contract was changed by QA/HCI.

User-visible behavior:
No runtime behavior changed by QA/HCI. The latest evidence shows public Quick Custom and campaign launch entering the M01 2D/isometric production slice with authored terrain, readable player/enemy squads, no old 3D prototype, no flat brown/tiny-world field, and no obvious upside-down tactical plate.

Validation run:
- Read `Design/AgentTasks/qa-hci_current.md`.
- Read Gameplay handoff `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`.
- Read PM acceptance `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-ecs-terrain-review.md`.
- QA/HCI Unity PlayMode rerun in assigned workspace:
  - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-qa-hci-public-launch-ecs-results.xml -logFile /private/tmp/warlinecapture-qa-hci-public-launch-ecs.log`
  - Run with Codex escalation/out-of-sandbox execution because Unity batchmode licensing requires it from Codex.
- Static ECS terrain/no-broad-lookup scan:
  - `rg -n "MissionRuntimeTerrainSurface|MissionRuntimeTerrainSurfaceRendererRuntime|terrain.iso.ch01.district_edge_01|GetComponentInChildren|GetComponentsInChildren|Resources.FindObjectsOfTypeAll|FindAnyObject|FindFirstObject|GameObject.Find|Transform.Find|FindButton|FindMissionNode" Assets/Game/Scripts/Components/MissionRuntimeComponents.cs Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs Assets/Game/Scripts/Systems/MissionRuntimeTerrainSurfaceRendererSystem.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- Capture dimension check:
  - `sips -g pixelWidth -g pixelHeight Design/AgentReports/Captures/2026-05-08_m01-public-launch/*.png`
- Visual inspection of:
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
  - `Design/AgentReports/Captures/2026-05-08_m01-public-launch/quick-custom-public-m01.png`
- QA log scan for exceptions, freezes, PerfDiag, legacy warnings, and leak warnings.

Validation result:
Accepted for the public-launch ECS terrain proof and focused QA workspace automation. `/private/tmp/warlinecapture-qa-hci-public-launch-ecs-results.xml` reports `Chapter01M01PlayModeValidationTests` passed 5/5 in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`. The focused suite includes Quick Custom and public campaign launch coverage. Static review confirms the named ECS terrain surface contract and renderer runtime link are present, and the previously flagged broad child-component discovery tokens are absent from the touched M01 test, tactical map loader, and terrain surface renderer system. Visual capture review confirms authored terrain orientation is readable and no old 3D prototype or flat brown/tiny-world view is visible in the inspected public-launch captures.

Evidence:
- `GameScene_M01BuildRejectionUsesSharedFeedbackReason`: passed.
- `GameScene_M01RuntimeSpawnsVisibleAnchoredSquadsAndStartsAtCameraAnchor`: passed.
- `GameScene_M01SelectionAttackAndResultRouteRespectSurvivalGuard`: passed.
- `PublicCampaignLaunch_ReachesM01ProductionVisibleSlice`: passed.
- `PublicQuickCustomLaunch_ReachesM01ProductionMatchRoute`: passed.
- `campaign-public-m01.png`: 1280x720.
- `quick-custom-public-m01.png`: 1280x720.
- `campaign-public-m01-20x9.png`: 1600x720.
- `quick-custom-public-m01-20x9.png`: 1600x720.

Known gaps:
- These public-launch captures support the Gameplay handoff but are not the final PM-locked 1920x1080 and 2400x1080 eight-state matrix.
- This does not mark review art or AI-generated tactical/unit/building assets final-approved.
- This does not close real-device touch/camera ergonomics.
- Marker/VFX readiness remains separate; prior safe-area manifests classify tracked marker visuals as temporary and `vfx.unit.destroyed.small` as absent from UI safe-area evidence.
- QA log still contains recovered Unity licensing/access-token noise, Xcode plist warning, repeated Animator warnings, one `PerfDiag:ECS:PreGame` line, preview-scene leak warnings, persistent allocation leak warnings, and usbmuxd shutdown noise.

Cross-lane impacts:
- Gameplay no longer owns the ECS terrain/public-launch proof blocker unless a new regression appears.
- QA/HCI can treat public Quick Custom and campaign launch as reaching the M01 production slice for this focused automation/capture scope.
- UI remains responsible only if a later review finds HUD/canvas/capture-composition issues over the accepted gameplay world.
- Art/design or implementing lanes still own marker/VFX readiness and final art approval.

Next recommended task:
Continue Gate 4 only on the remaining blockers: human touch/camera ergonomics or approved equivalent, marker/VFX readiness or waiver, final-readiness log-warning classification/waiver, and any PM-required final 1920x1080/2400x1080 eight-state review packaging.

### QAHCI-G4-022 closure: Public launch ECS terrain proof accepted

- Severity: Closed for the public-launch ECS terrain proof and focused QA workspace automation.
- Affected lane: gameplay / QA-HCI.
- Reproduction steps:
  1. Read `Design/AgentReports/2026-05-08_gameplay_m01-public-launch-path.md`.
  2. Read `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-ecs-terrain-review.md`.
  3. Run `Chapter01M01PlayModeValidationTests` from `/Users/farhad/Projects/WarlineCapture-CodexUnity3` with Codex escalation/out-of-sandbox execution.
  4. Inspect `/private/tmp/warlinecapture-qa-hci-public-launch-ecs-results.xml`.
  5. Inspect public-launch captures under `Design/AgentReports/Captures/2026-05-08_m01-public-launch/`.
- Expected: Public Quick Custom and campaign launch reach the M01 production visible slice with ECS-backed terrain proof and no touched-test broad lookup regression.
- Actual: PM accepted the ECS terrain proof, QA/HCI PlayMode rerun passed 5/5, static scan confirms the ECS terrain contract/link and no broad lookup tokens in touched files, and visual captures show authored non-upside-down M01 terrain rather than legacy 3D/brown field.
- Blocks next milestone: no for this blocker. Gate 4 remains blocked by separate touch/camera, marker/VFX, final matrix/review packaging, and log-warning classification or waiver items.
- Recommended owner: QA/HCI watch only; Gameplay only if new public-launch regression appears.

### QAHCI-G4-023 closure: ECS terrain acceptance contract now defined and proven

- Severity: Closed for the current M01 terrain ECS presentation contract.
- Affected lane: gameplay / QA-HCI / PM.
- Reproduction steps:
  1. Read PM acceptance in `Design/AgentReports/2026-05-08_pm_gameplay-m01-public-launch-ecs-terrain-review.md`.
  2. Inspect `MissionRuntimeTerrainSurface` and `MissionRuntimeTerrainSurfaceRendererRuntime`.
  3. Inspect `TacticalMapRuntimeLoader` and `MissionRuntimeTerrainSurfaceRendererSystem`.
  4. Run the focused QA/HCI PlayMode suite listed above.
- Expected: The visible tactical ground/map has a named ECS source contract and renderer runtime link, with validation through ECS queries/explicit references rather than broad hierarchy search.
- Actual: The contract and link are present, PM accepted the hybrid ECS-backed SpriteRenderer presentation, and QA/HCI rerun passed.
- Blocks next milestone: no for this contract gap.
- Recommended owner: QA/HCI watch only.

## Updated waiting/blocker ownership fields after public-launch ECS terrain acceptance

Waiting on lane:
QA/HCI and art-design/integration.

Waiting on exact file/report/asset/command:
- Touch/camera HCI proof or manual/device smoke evidence for campaign and Quick Custom/Test public launch paths.
- Marker/VFX asset evidence or explicit temporary-evidence waiver for `marker.selection.ring`, `marker.move.destination`, `marker.attack.target`, and `vfx.unit.destroyed.small`.
- Owner classification or PM waiver for remaining Animator warnings, `PerfDiag:ECS:PreGame`, preview-scene leak warnings, persistent allocation warnings, and usbmuxd/editor-tooling noise.
- Final PM-required 1920x1080/2400x1080 eight-state review packaging if the accepted safe-area/profile matrices are not considered sufficient.

Owner of next action:
QA/HCI owns the next affected HCI/touch/log-readiness validation. Art-design or implementing lanes own marker/VFX readiness. PM owns any waiver decisions.

Can my lane still continue fallback work? limited. QA/HCI can continue only on the remaining active Gate 4 validation items; the public-launch ECS terrain blocker is closed.
