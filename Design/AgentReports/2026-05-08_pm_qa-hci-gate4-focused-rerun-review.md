Status:
needs fixes; Gate 4 rejected for user review

Lane:
PM

Task:
Review `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md` and route concrete follow-up work before Gate 4 can be presented to the user.

Files changed:
- `Design/AgentReports/2026-05-08_pm_qa-hci-gate4-focused-rerun-review.md`

Contracts touched:
- M01 golden playthrough gate.
- M01 infantry-only scope.
- Public M01 first-control HCI/readability contract.
- ECS atlas-backed infantry presentation contract.
- Gate 4 final QA/HCI acceptance contract.

User-visible behavior:
- Do not ask the user to review Gate 4 yet.
- Automated M01 route and ECS architecture tests pass, but the public player-facing image still fails the visual/HCI bar for a first mission tutorial.

Validation run:
- Reviewed `Design/AgentReports/2026-05-08_qa-hci_gate4-focused-rerun.md`.
- Checked it against the standard WarlineCapture handoff format and current PM Gate 4 rule.

Validation result:
- QA/HCI Gate 4 focused rerun is accepted as a valid needs-fixes report.
- Gate 4 remains rejected for user review.
- Accepted evidence:
  - `WarlineCapture-CodexUnity3` was refreshed enough to run the focused M01 tests.
  - `Chapter01M01PlayModeValidationTests` passed 8/8 in the QA workspace.
  - Public Campaign automated golden path reaches result popup.
  - Opening-control protection passes.
  - Select, move-to-cover, attack, patrol neutralization/result readiness, and build-rejection coverage pass.
  - ECS atlas assertions pass: `MissionRuntimeAtlasQuadRuntime` required, `MissionRuntimeSpriteRendererRuntime` rejected, separate destroyed visual components rejected, tactical projectile trace sizing asserted.
  - Automated scope assertions report one player command squad, one hostile patrol, no player vehicles, build entry, transport, base, or extra player unit type.
- Rejected Gate 4 because public captures and HCI checks still fail the user-facing readability and scope bar.

Known gaps:
- Player rifle squad is too small at gameplay camera scale and does not read clearly as four distinct soldiers in public captures.
- Selected-state clarity is not visually accepted from public captures, despite automated selected-marker assertions.
- M01 infantry-only scope is contradicted by the HUD showing APC, Tank, air support, and Build affordances/cards.
- Final atlas art is still not approved: `FinalAtlasArtReady = 0`.
- Touch/camera ergonomics were not manually validated beyond automated route coverage.
- Invalid command recovery and assistant ownership/Stop behavior were not revalidated after the presentation change.
- Batchmode performance/log data is not acceptable for final performance signoff due to low initial fps during menu/bootstrap and known editor/tooling noise.

Cross-lane impacts:
- UI: make the public M01 HUD match the infantry-only teaching slice. Remove, lock, or clearly suppress APC, Tank, air support, and Build affordances/cards for M01. Ensure selected-squad HUD state remains readable.
- Gameplay/Art: improve camera-scale unit readability for the four-soldier squad and selected marker. The squad must read as four soldiers under one command identity in the actual public first-control composition.
- Gameplay/Art: either advance final atlas art readiness or prepare a focused temporary-art approval package for user/art review.
- QA/HCI: rerun Gate 4 after UI and Gameplay/Art fixes land, including visual review, touch/camera ergonomics or documented substitute, invalid command recovery, assistant ownership/Stop behavior, and log/performance classification.
- Support/FTUE: no current action unless the next QA/HCI pass finds assistant, Stop, Show Me, result explanation, or invalid-command recovery issues.

Next recommended task:
- Assign UI immediately to fix M01 HUD scope mismatch.
- Assign Gameplay/Art immediately to fix public first-control squad/selection readability and art-readiness packaging.
- QA/HCI should wait for those handoffs, then rerun the final Gate 4 HCI pass.
