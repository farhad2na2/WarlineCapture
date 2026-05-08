Status: accepted as QA blocker report
Reviewed report:
- `Design/AgentReports/2026-05-08_qa-hci_m01-player-route-safe-area-rerun.md`

Lane:
PM review

Summary:
QA/HCI reran the M01 player-route/safe-area review against the available UI route-driven capture evidence and focused Unity smoke. The report is accepted as a valid blocker classification, not as Gate 4 acceptance.

Acceptance decision:
Accepted for QA/HCI reporting. Gate 4 remains blocked.

Validation accepted:
- `Chapter01M01PlayModeValidationTests`: passed 3/3 in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`.
- `WarlineCaptureUiShellTests`: passed 15/15.
- `WarlineCaptureUiMatchOverlayTests`: passed 18/18.
- `WarlineCaptureUiAssistantRuntimeBindingTests`: passed 7/7.
- Route-driven capture dimensions were checked for eight states at 1920x1080 and 2400x1080.
- QA log-health review found no reproduced severe runtime exception/freeze signatures.

Gate 4 decision:
Still blocked. QA correctly identified that the available UI evidence is not enough for final Gate 4 acceptance.

Blocking findings accepted:
- `QAHCI-G4-011`: safe-area profile coverage is incomplete because UI still has two generic simulated inset manifests instead of the named `safe.none_16x9`, `safe.rounded_20x9`, and `safe.cutout_left_20x9` matrix or an explicit PM-approved replacement.
- `QAHCI-G4-012`: runtime reason-code aliases still diverge from canonical M01 reason-code expectations.
- `QAHCI-G4-013`: human touch/camera ergonomics remain unverified.
- `QAHCI-G4-014`: feedback marker and destroyed VFX assets remain missing or unapproved for final visual readability/art approval.

Important PM note:
QA/HCI advanced from a UI handoff that PM had already marked `needs fixes`. The result is still useful because QA preserved the blockers instead of accepting Gate 4. Going forward, QA/HCI should only rerun affected checks after the owning lane lands a reviewed fix report, unless PM explicitly asks for an early blocker-classification pass.

Cross-lane routing:
- UI owns safe-area evidence/profile closure.
- Gameplay with Support/FTUE owns runtime reason-code alignment or explicit mapping proof.
- Art-design or implementing UI/gameplay lane owns marker/VFX readiness or temporary-evidence waiver.
- QA/HCI should wait until those handoffs land, then rerun only affected checks.

Needs user decision:
No immediate user decision. A future PM/user waiver would be needed only if the team wants to accept generic simulated safe-area insets instead of the named profile matrix.

Next recommended task:
UI should close `QAHCI-G4-011` first because it is the active UI-owned blocker. Gameplay/Support-FTUE should prepare the reason-code runtime mapping/cleanup in parallel if available.
