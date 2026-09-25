# S003 supported ARIA command evidence audit

Scope: the current `AriaSkirmishPlanSystem` planner and its visible touch-control paths. This is an input-provenance gate, not proof of mission quality, air lifecycle, performance, human acceptance, or arbitrary future command coverage.

## Observed chain

- `AriaPlayInputSystem` owns its touch driver and observes only that driver’s accepted Ended events. The release creates a monotonic receipt; stale, wrong-position, duplicate, and previous-attempt claims are rejected.
- Ground/air Attack and Move requests carry a claimed receipt from the shared selection request boundary. The request’s scope follows execution through shared AttackOrder/SelectedMove systems. Expanded army attack, selection/group orders, and AttackMove record accepted gameplay requests. Autonomous combat after a legal order and enemy strategy are not new player input requests.
- Hold uses the visible expanded command gateway and the same scoped army service.
- Recruitment uses BuildingUiCommandAdapter’s release scope and the authoritative paid production service’s acceptance marker.
- Construction confirmation uses BuildingUiCommandAdapter’s release scope and records only accepted placement.
- Readiness uses a queued receipt and the research acceptance boundary.
- Selection, paging, drawer navigation, map/camera focus, and placement preview/cancel remain visible gestures; they do not authorize a separate troop order or spend. The unused direct enemy-base attack gateway and background direct-assault system have been removed.

The planner source was reviewed for direct gameplay requests/entity mutation: decisions publish public control targets; `AriaPlayInputSystem` dispatches the gestures. No hidden structure-assault fallback remains. Future planner capabilities such as production cancellation, Return-to-Base or transport must be audited/instrumented before expanding this declared coverage.

## Measurement and fail-closed rules

`AriaCommandEvidence.TryReadAudit` requires an active recorder, a nonempty command stream, at least one completed touch, and exact equality between observed releases and the live driver’s completed gestures. Missing/stopped observation or mismatched counts returns unknown (-1). A measured result sums unreceipted accepted commands and unexpected driver samples. Human interventions remain an independent measured gate. Terminal navigation also contributes unexpected samples/interventions and must pass its own frozen-result/destination checks.

`SupportedAriaControlsV1` identifies this explicit scope in new logs. It does not rewrite historical runs: runs 10–14 remain diagnostic/failed or unknown as originally recorded. Regression cases verify unobserved streams, direct/stale/reused/previous-attempt commands, mismatched release counts, observer shutdown, unexpected samples, and an actual scoped command yielding measured zero.

Native validation of this new measurement revision is pending. No counted win is asserted by this document.
