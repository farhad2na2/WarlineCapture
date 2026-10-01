# Supply Line — ready for human Editor review

## Native normal-input journeys

| Journey | Result | Evidence |
|---|---|---|
| English ARIA | Passed: 40 completed deliveries, actual southern-lane arrival, protected civilian 20, full reserve hold, debrief/result/Campaign return; 621,928 active ms | `Logs/supply-line-review-watch-11.log.gz`, `Evidence/20261001-180302-en-aria` |
| Persian independent manual | Passed with ARIA off: same delivery/arrival/hold facts, corrected mission-specific result objectives and Campaign return; 630,506 active ms | `Logs/supply-line-review-manual-05.log.gz`, `Evidence/20261001-181939-fa-IR-manual` |

The English gameplay journey preceded the result-label correction; the final
Persian native journey exercised that correction. The gameplay rules and layout
were unchanged between those runs. No mission outcome, unit position, route
arrival or tactical order was injected. Profiles were isolated fixtures.

## Automated checks

Three native ECS detour cases passed, covering an automatic 190-cell haul,
a player reroute after removing the automatic order, and the retained skirmish
detour. All 31 resource-hauler rule cases passed, including exact completed
unloads despite concurrent spending and rejected full-depot unloads. Mission
checkpoint/rules/source hash and pump-access checks passed in the wrappers.

## Native visual inspection

Inspected English 40/40 selection-cue HUD and result; Persian final-stage HUD,
mission-specific result objectives and return. Text fits, selection markers use
the approved direction, and existing ARIA Play / Stop remains visible. The pump,
single service opening and refinery/fence layout are documented in `WORKSPACE.md`.
This is agent inspection, not the user's visual acceptance.

## Review and remaining release gates

Use the seven-mission review entry described in `../REVIEW.md`. Review the pump
yard, single fences, refinery fit, camera/minimap, southern reroute, delivery
counter and final reserve. Human player acceptance, Android/device content and
performance, updated voice assets and native negative journeys not already
qualified remain pending. Failed and superseded runs are retained in the full
log manifest; older wins do not qualify the final delivery requirement.
