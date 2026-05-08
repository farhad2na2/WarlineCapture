Status: accepted for simulated safe-area profile evidence
Reviewed report:
- `Design/AgentReports/2026-05-08_ui_m01-safe-area-profile-matrix-fix.md`

Lane:
PM review

Summary:
UI delivered the named Gate 4 safe-area profile matrix requested by PM/QA. The report uses the standard WarlineCapture handoff format and the artifacts now include the three required simulated profiles: `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.

Acceptance decision:
Accepted for closing the UI-owned simulated safe-area profile evidence gap `QAHCI-G4-011`.

Validation accepted:
- `WarlineCaptureUiPhase1PrefabBuilder.CaptureM01SafeAreaProfileMatrix` reportedly exited 0.
- Capture folder contains 24 state PNGs: eight M01 states across three named profiles.
- Capture dimensions match expectations:
  - `safe.none_16x9`: 1920x1080.
  - `safe.rounded_20x9`: 2400x1080.
  - `safe.cutout_left_20x9`: 2400x1080.
- Three profile manifests exist and include profile id, resolution, insets, cutout rectangles, per-surface clearance notes, invalid-command reason-code status, and marker/VFX status.
- Reported focused UI validations passed:
  - `WarlineCaptureUiShellTests`: 15/15.
  - `WarlineCaptureUiMatchOverlayTests`: 18/18.
  - `WarlineCaptureUiAssistantRuntimeBindingTests`: 7/7.
- `git diff --check` passed for the touched UI builder and profile evidence path.

Remaining blockers:
- Public M01 launch path still enters legacy 3D gameplay and blocks manual HCI/balance validation.
- Runtime canonical reason-code proof remains blocked until the Support/FTUE or Gameplay handoff has passing Unity validation.
- Human touch/camera ergonomics remain unverified.
- Marker/VFX assets remain temporary or unapproved; `vfx.unit.destroyed.small` is still absent from this UI safe-area evidence.

Cross-lane notices:
- QA/HCI may treat `QAHCI-G4-011` as closed for simulated safe-area profile evidence after verifying this report and artifacts.
- QA/HCI must not proceed to broad manual HCI/balance until the public launch path blocker is fixed.
- Gameplay/Support-FTUE still own the reason-code runtime validation gap.
- Art/design or implementing lanes still own final marker/VFX readiness.

Tracking updates:
No project-state percent change yet. This closes one Gate 4 blocker, but Gate 4 is not accepted.

Next task:
Gameplay/UI should fix the public M01 production launch path. QA/HCI should rerun only affected checks once public launch and reason-code validation handoffs land.
