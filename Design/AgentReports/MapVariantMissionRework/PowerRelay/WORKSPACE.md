# CH02-M04 Power Relay: RefineryDistrict review candidate

Started from `a188a967b` on `codex/power-relay-refinery-review`. This is an
isolated mission candidate. The original logical map asset is retained for
rollback; no Power Relay migration has been promoted to `main`.

## Mission graph and provisional layout

The current coded graph remains: family convoy confirms Samira's protected
route, reaches the school shelter, Fuel truck reaches the repair zone, both
engineers hold the repair zone, and the squad defeats the hostiles and holds.
Engineer loss, family loss, exposed-route use and missing Fuel remain failures
or victory blockers according to the current rules. No new gameplay control is
planned.

| Role | X,Z | Placement intent |
|---|---:|---|
| Family convoy | 555,505 | Western three-lane fork |
| Exposed shortcut | 700,505 | Direct industrial main road |
| Protected waypoint | 705,385 | Southern service road |
| School shelter marker | 885,385 | Eastern service-road city frontage |
| Repair point | 813,487 | Inside the substation north service gate |
| Fuel truck | 905,505 | Eastern main-road approach |
| Engineers | 815,505 | North-gate staging |

The candidate uses physical RefineryDistrict source hash
`2d4fd91a415a68f0299a4075e37730ecd7b746e94e5a2a1e2a6cd7baca687778`.
The native configuration check passes with a two-cell clearance around moving
centers and a seven-cell square around each hostile formation. It found
family-to-protected-waypoint 270 cells, waypoint-to-shelter 180,
Fuel-to-repair 110, and engineers-to-repair 20, with the protected legs
avoiding the exposed-route radius. These distances are static route evidence.

The prepared RefineryDistrict packed content rebuilt and passed its exact
physical source hash check. A public-input Editor run reached Campaign,
Power Relay briefing, Persian comics, the match HUD, and ARIA Play. ARIA
selected the family truck and issued a move, but the first command landed at
`(655,431)` rather than the protected waypoint `(705,385)`. The truck then
moved only partway before ARIA's objective watchdog stopped. The camera focus
path now uses the throttled refinery behavior already used by Supply Line.
With the editor-only background input fixture, the complete public-input
Watch run passed in `Evidence/power-relay-refinery-watch-12.log.gz`. It
confirmed `SafeRouteConfirmed=1`, `FamiliesSheltered=1`, `FuelDelivered=1`,
`PowerRestored=1`, a 10-second victory hold, zero civilian losses, and no
exposed-route use. The seven comic panels, Persian Lina identity, match HUD,
debrief, result and Campaign return all passed. The checked wrapper exited 0.
The focused rule check passed in `Evidence/power-relay-refinery-rules-01.log.gz`,
also with wrapper exit 0. Full configuration and packed-source logs, plus the
earlier timed-out native run, are retained in `Evidence/`.
The repack changed transient catalog and minimap bytes while preserving the
pinned physical source hash. Those generated physical artifacts are left at
the Supply Line baseline; this review branch changes the Power Relay logical
map and mission binding only.

## Final review preparation — 2026-10-01

Both final native normal-input journeys now pass: English independent manual
(`Logs/power-relay-review-manual-02.log.gz`) and Persian ARIA
(`Logs/power-relay-review-watch-01.log.gz`). Each reaches the protected waypoint,
shelters the families, delivers Fuel, restores power with both engineers, holds
for 10 seconds, and completes debrief/result/Campaign return. See `READINESS.md`
for exact timing, visual inspection and separated acceptance gates.

The school shelter at (885,385) now visibly identifies the existing House_07
frontage at (890,377), size 5×6. Its exact owner is
`densecity.3c718931b282002708f27070c2c0f14619d641ffc3d200a807b9b566be9faa55`.
A compact localized label and the approved ground corners/check mark bind to
that source owner. No physical prepared-map geometry or mission building was
added. The current Power Relay-only destination-focus throttle allows camera
arrival before another focus request; it issues no troop order. Mission results
reuse the existing rows with shelter, restoration and perimeter objectives.

## Remaining release gates

- Human map, camera, minimap and guidance review.
- Native exposed-route, engineer-loss and missing-Fuel negative journeys;
  focused rule checks already pass.
- Real player/device acceptance and packaged content/performance.

Earlier evidence and failed runs remain retained. The final native journeys,
not the older candidate win, establish this Editor review readiness.
