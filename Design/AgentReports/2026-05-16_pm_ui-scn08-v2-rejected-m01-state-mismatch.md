# PM UI SCN-08 V2 Rejected - M01 State Mismatch

Date: 2026-05-16
Owner: UI
Status: rejected, continue
Priority: P0

## Decision

`Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v2.md` is rejected as completion.

The scoped tests passing is useful, but the provided visual evidence does not match the M01-01 no-selection state required by the task.

## Evidence Reviewed

UI evidence capture:

- `Design/AgentReports/Captures/2026-05-07_m01-ui-matrix/M01_Integrated_1920x1080_01_MatchStart.png`

## Rejection Reasons

- Objective panel shows non-M01 objectives such as `Capture the Forward HQ`, not `Destroy hostile patrol`.
- ARIA panel is visible; M01-01 requires assistant/ARIA closed.
- Build command is visible; M01 Build must be unavailable/hidden or clearly disabled with `MissionDoesNotAllowBuild`.
- Command state shows active `MOVE`; M01-01 requires no selected command state.
- Move/attack command target markers are visible; M01-01 requires no command target markers.

## Required Next UI Delivery

UI must write:

- `Design/AgentReports/2026-05-16_ui_scn08-battlehud-target-implementation-v3.md`

Required proof:

- explicit M01-01 no-selection evidence capture
- objective text `Destroy hostile patrol`
- ARIA closed
- Build unavailable/hidden/disabled with the M01 reason if visible
- no active Move/Attack/Special/Build command state
- no selected squad panel/status
- no command target markers
- SCN-08 target-vs-implementation checklist
- validation commands/tests
- remaining mismatches classified by owner

## Routing

Current owner remains UI.

Runtime capture blockers can be documented, but they do not excuse incorrect UI-owned evidence state.
