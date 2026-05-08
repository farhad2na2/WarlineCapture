# PM Review: Support/FTUE Assistant Service Slice

Date: 2026-05-07
Reviewed report: `Design/AgentReports/2026-05-07_support-ftue_assistant-service-slice.md`

## Decision

Accepted as the first read-only M01 assistant recommendation-service slice.

## Validation Checked

- `/private/tmp/warlinecapture-m01-assistant-runtime-results.xml`: `M01AssistantRuntimeTests` passed 9/9.
- The clean rerun exited successfully after the malformed `.meta` GUID issue was fixed.

## Accepted Behavior

- `WarlineCaptureAssistantService` can evaluate an M01 assistant context and produce panel-safe presentation data without executing gameplay.
- `M01AssistantRecommendationProvider` covers objectives intro, select squad, move to cover, attack patrol, invalid command recovery, build rejection, and result explanation.
- Assistant recommendations use typed ids and typed intents instead of screen coordinates.
- `Do It` remains disabled unless `AssistantContext.TypedCommandHooksAvailable` is true, which is the correct boundary until gameplay command wrappers are accepted.
- `TutorialSessionState` tracks completed steps, dismissed recommendations, rejected command context, and assistant-owned preview/takeover ids at in-session scope.

## Known Gaps Accepted

- No live `AssistantContextProvider` wiring exists yet from mission/session/objective/selection/bridge state.
- No UI binding exists yet from the service into the mounted panel.
- No command intent executor boundary exists yet for actual gameplay execution.
- No integrated PlayMode, device, or HCI validation is possible until gameplay hooks and UI visual mount are accepted.

## Cross-Lane Notices

- Gameplay remains the owner for `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`.
- UI can bind service-produced `AssistantPanelPresentationData` once the visual match HUD mount is fixed and accepted.
- QA/HCI should treat this as a functional service baseline, not an integrated tutorial pass.
- There are untracked assistant command-runtime files visible in the shared workspace that appear to belong to the gameplay wrapper lane. They are not reviewed or accepted by this Support/FTUE review.

## Next Recommended Task

Support/FTUE should wait for gameplay typed command hooks and UI visual mount acceptance before implementing live `AssistantContextProvider` wiring. The next support task should be the context-provider/live-state binding only after those dependencies are accepted or clearly stubbed behind interfaces.
