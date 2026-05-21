# PM Clarification - M01 V29 Gameplay Owns In-Game Runtime, UI Owns HUD Later

Date: 2026-05-18
Owner: PM
Status: Gameplay V29 scope clarified; UI/HUD/canvas deferred
Priority: P0

## Reason

User clarified that Gameplay should fix all in-game tasks now. UI will fix the canvas and HUD later.

This clarifies the previous PM review:

- `Design/AgentReports/2026-05-18_pm_gameplay-m01-v28-binding-accepted-visual-rejected.md`

## Gameplay Owns Now

Gameplay owns the in-game/runtime composition pass:

- camera zoom/framing
- tactical map/background composition
- player squad position, spacing, formation, and facing
- enemy patrol position, spacing, formation, and facing
- V28 soldier atlas binding through ECS/runtime presentation
- soldier idle animation proof
- no-selection gameplay state through the live M01 launch flow
- runtime gameplay state that UI will later read, such as selected state, command availability, objective state, and mission route state
- architecture compliance with `Design/Architecture/gameplay_solid_ecs_contract.md`

## UI Owns Later

Gameplay must not try to visually solve the canvas/HUD target-lock pass in V29.

Deferred to UI later:

- objective panel visual layout
- top resource bar visual layout
- command rail/button visual layout
- squad cards visual layout
- minimap panel visual layout
- threat/log panel visual layout
- HUD chrome, typography, TMP sizing, sprite slicing, anchors, and canvas scaling

Gameplay should preserve correct runtime data and state for these regions, but visual canvas/HUD matching is not the V29 implementation scope.

## Expected Gameplay Report

Gameplay must deliver:

- `Design/AgentReports/2026-05-18_gameplay_m01-v29-ingame-target-match-proof.md`

The report should compare target vs runtime for in-game/world regions only, and include a short separate note that HUD/canvas visual mismatches are intentionally deferred to UI.
