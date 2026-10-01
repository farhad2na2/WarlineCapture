# Power Relay — ready for human Editor review

## Native normal-input journeys

| Journey | Result | Evidence |
|---|---|---|
| English independent manual | Passed with ARIA off: protected route, shelter arrival, Fuel delivery, both engineers, restored power, 10-second hold, debrief/result/Campaign return; 207,591 active ms | `Logs/power-relay-review-manual-02.log.gz`, `Evidence/20261001-184014-en-manual` |
| Persian ARIA | Passed: same mission facts, all seven comic panels, Persian Lina identity, exposed shortcut avoided and Campaign return; 205,700 active ms | `Logs/power-relay-review-watch-01.log.gz`, `Evidence/20261001-184729-fa-IR-aria` |

Neither journey injected unit positions, orders, arrivals or outcomes. The
profiles were isolated fixtures. Six of six hostiles were defeated, with zero
civilian losses. The wrapper exited 0 for both journeys.

## Automated checks and native visual inspection

Focused mission rules, protected-route clearances and the exact prepared source
hash passed. The shelter binds one existing House_07 city-frontage owner; it
does not add a duplicate mission building. Its compact localized school label
and approved corner/check marker were inspected in both native languages.
English and Persian mission-specific result objectives fit their existing rows.
The existing ARIA Play / Stop control remains visible. This is agent inspection;
user visual acceptance remains pending.

The first English manual run exposed repeated camera focus preventing the
protected waypoint from becoming reachable on screen. A Power Relay-only focus
throttle fixed that; the failed full log remains in `Logs/` and the manifest.

## Review and remaining release gates

Open the ordinary Campaign review as described in `../REVIEW.md`. Review the
protected convoy road, school frontage, engineer repair point and final hold.
Human player/device acceptance, packaged device performance/content and native
exposed-route, engineer-loss and missing-Fuel negative journeys remain pending.
These are release gates separate from readiness for your Editor review.
