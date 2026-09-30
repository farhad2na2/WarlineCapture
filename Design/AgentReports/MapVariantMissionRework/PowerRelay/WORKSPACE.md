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

## Open work and gates

- The marked shelter needs a visually identifiable school at the service-road
  frontage. The prepared physical source does not currently advertise a school
  landmark in this sector. Inspect the native map, then either bind a suitable
  existing building or request a deliberate prepared-map addition without
  adding a duplicate mission building.
- Inspect the actual hostile formations and convoy movement in the native map
  with a human reviewer. The automated native run passed, but the capture
  shows rooftops can obscure the focus cue near the service road.
- Complete a manual normal-input run and native exposed-route, engineer-loss
  and missing-Fuel negatives. The focused rule checks passed, but they do not
  replace those playthroughs. Review EN/FA native screens and target-device
  performance.
- Human visual approval and real player/device acceptance remain separate gates.
