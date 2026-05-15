# PM Correction: Runtime Visual-Match Proof Required Before Approval

Date: 2026-05-14
Lane: PM
Status: routed to Gameplay

## Decision

The current Gameplay implementation handoff is not complete enough for PM/user approval.

Gameplay delivered code/test changes and a report:

- `Design/AgentReports/2026-05-14_gameplay_m01-imagegen-sample-implementation.md`

But it did not prove that the runtime implementation visually matches the approved target mockup.

## Workflow Update

Runtime implementation of an approved visual target must include visual-match proof before PM/user approval or QA/HCI routing.

Required proof:

- fresh runtime screenshot/capture from the implemented build
- explicit target mockup path
- side-by-side/contact-sheet or overlay comparison against the target mockup
- written match/mismatch notes
- validation command/workspace/log path/result
- recommended next steps

This has been added to:

- `Design/AgentTasks/README.md`
- `Design/AgentTasks/M01_CRITICAL_PATH.md`
- `Design/AgentTasks/user_feedback_review_gate.md`

## Gameplay Supplemental Task

Gameplay must provide:

- `Design/AgentReports/2026-05-14_gameplay_m01-01-runtime-visual-match-proof.md`

The proof must compare the implemented runtime M01-01 tactical start/no-selection state against:

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`

Required assessment areas:

- Build disabled/secondary treatment
- command buttons neutral/inactive
- objective and Star Goals
- squad cards
- threat feed
- minimap
- camera/framing
- no selected rings
- no selected status

If Gameplay cannot produce a fresh runtime capture, it must write the same report as blocked with exact command, blocker, log path, and unblock owner.

## Routing

Current owner: Gameplay

Held until proof exists and PM/user approves:

- QA/HCI
- further Gameplay slices
- Art/Atlas full sequence production
- selected-state implementation
