# PM Decision: UI Agent Suspended From POP-05/SCN-02 Target-Lock Ownership

Date: 2026-05-16
Lane: PM
Target lane: UI
Status: active routing decision

## Decision

`Design/AgentReports/2026-05-16_ui_visual-target-match-implementation-v5.md` is rejected.

The UI agent is suspended from autonomous POP-05/SCN-02 target-lock implementation.

## Reason

The UI lane repeatedly failed the same target-lock standard after explicit PM corrections:

- produced a planning-only report instead of implementation
- placed full-screen target mockups over the UI
- used placeholder/fallback/generated substitute art
- removed placeholders but left blank/null regions
- consumed accepted Art/Atlas layers but kept the old shell composition and still did not visually match the targets

V5 confirms the issue is no longer missing art. The accepted Art/Atlas package is available, but the UI implementation still did not reconstruct the target layout or quality.

## Current Routing

- Art/Atlas: held. The no-placeholder package is accepted.
- UI: held for POP-05/SCN-02 target-lock work.
- PM/user: assign a replacement implementation owner or supervise a manual implementation pass.

## If UI Agent Is Used Again

Only use the UI agent for narrow mechanical subtasks:

- import one named layer
- move/resize one named RectTransform group
- run one named capture command
- run one named test command

Do not let the UI agent own:

- complete screen reconstruction
- multi-screen passes
- target-lock acceptance claims
- visual judgment
- "done" reporting

## Recommended Next Step

Do a supervised/manual implementation pass on one screen only:

1. Start with `SCN-02_MainMenu`.
2. Rebuild the first visible section from the target: masthead/top resource strip plus commander panel.
3. Produce one capture.
4. PM/user visually approves or rejects that section.
5. Continue section by section only after approval.

Do not touch `POP-05_MissionResult` until the first SCN-02 section is visually accepted.
