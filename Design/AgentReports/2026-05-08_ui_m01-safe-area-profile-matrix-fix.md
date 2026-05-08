Lane:
UI

Task:
Close the UI-owned Gate 4 safe-area evidence gap `QAHCI-G4-011` by adding named M01 route-driven safe-area profile captures and manifests for `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9`.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs`
- `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`

Contracts touched:
- Editor-only UI capture tooling now has `WarlineCapture/UI/Capture M01 Safe Area Profile Matrix`.
- Added explicit profile manifests with `profile_id`, resolution, safe-area insets, cutout rectangles, per-surface clearance notes, invalid-command reason-code status, and marker/VFX status.
- Runtime UI prefabs/contracts were not changed.

User-visible behavior:
No runtime behavior changes. This is a QA evidence/tooling fix for M01 HUD safe-area review.

Validation run:
- Unity batch capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureM01SafeAreaProfileMatrix`
- Image/manifest sanity check: 24 PNG captures, 3 profile manifests, and `M01_SafeAreaProfile_CaptureMatrix_ContactSheet.png`
- Unity EditMode `WarlineCaptureUiShellTests`
- Unity EditMode `WarlineCaptureUiMatchOverlayTests`
- Unity EditMode `WarlineCaptureUiAssistantRuntimeBindingTests`
- `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix`

Validation result:
- Capture command exited 0.
- Image/manifest sanity check passed: 24 captures and 3 manifests validated.
- `WarlineCaptureUiShellTests`: 15/15 passed.
- `WarlineCaptureUiMatchOverlayTests`: 18/18 passed.
- `WarlineCaptureUiAssistantRuntimeBindingTests`: 7/7 passed.
- `git diff --check`: passed.

Known gaps:
- This fix is intended to close UI safe-area evidence blocker `QAHCI-G4-011`, pending PM/QA acceptance.
- Runtime invalid-command canonical reason-code proof is still not closed by this UI evidence pass and remains `QAHCI-G4-012` for Gameplay/Support-FTUE.
- Human touch/camera ergonomics remain `QAHCI-G4-013`.
- Final approved marker/VFX readiness remains `QAHCI-G4-014`.

Cross-lane impacts:
- QA/HCI can rerun the affected safe-area profile review against `Design/AgentReports/Captures/2026-05-08_m01-safe-area-profile-matrix/`.
- Gameplay/Support-FTUE still needs to provide runtime reason-code mapping/alignment proof.
- Art/gameplay still needs approved marker/VFX assets or a PM waiver for temporary feedback assets.

Next recommended task:
PM should review this safe-area profile matrix fix. If accepted, QA/HCI should rerun only the Gate 4 safe-area profile checks before broader Gate 4 validation resumes.
