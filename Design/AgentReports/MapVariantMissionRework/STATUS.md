# Existing Campaign map migration status

Updated 2026-10-01. This isolated checkout contains the CH02-M02 Supply Line
pilot, a CH02-M04 Power Relay review candidate, and a CH01-M04 Airlift
candidate and a CH04-M01 Air Corridor candidate ready for mission review. No map or mission is qualified for release,
and these review bindings have not been promoted
to `main`.

| Mission | Planned disposition | This checkout |
|---|---|---|
| CH01-M01 First Contact | Retain urban | No migration; baseline not rerun |
| CH01-M02 Establish the Base | Retain urban | No migration; baseline not rerun |
| CH01-M03 Radar Warning | Retain urban | No migration; baseline not rerun |
| CH01-M04 Airlift | CityEdgeAirfield | ARIA English and manual Persian normal-input extraction/result/return passed on final layout; ready for user review; see `Airlift/WORKSPACE.md` |
| CH01-M05 Breach Assault | Retain urban | No migration; baseline not rerun |
| CH02-M01 Gridlock | Retain urban | No migration; baseline not rerun |
| CH02-M02 Supply Line | RefineryDistrict | Review candidate in progress; see `SupplyLine/WORKSPACE.md` |
| CH02-M03 Market Lifeline | Retain urban | Generator basis pinned to legacy Supply Line map; regression pending |
| CH02-M04 Power Relay | RefineryDistrict | ARIA public-input victory and result/return passed; visual and negative gates pending; see `PowerRelay/WORKSPACE.md` |
| CH02-M05 Route Reopened | AshLinePort | Not started |
| CH03-M01 Signal Trace | Retain urban | No migration; baseline not rerun |
| CH03-M02 Safehouse Sweep | Retain urban | No migration; baseline not rerun |
| CH03-M03 False Front | Retain urban | No migration; baseline not rerun |
| CH03-M04 Evidence Chain | Retain urban | No migration; baseline not rerun |
| CH03-M05 Network Break | Retain urban | No migration; baseline not rerun |
| CH04-M01 Air Corridor | CityEdgeAirfield | Final English manual/Retry and Persian ARIA normal-input victory/result/return passed; visible gray Build actions, exact shortage/refund, saved Continue and real radar-loss gates passed; ready for Editor review; see `AirCorridor/WORKSPACE.md` |
| CH04-M02 Steel Push | RefineryDistrict | Generator basis pinned to legacy Supply Line map; migration not started |
| CH04-M03 Split Front | RefineryDistrict | Generator basis pinned to legacy Supply Line map; migration not started |

Supply Line gates: candidate generation, packed content build, mission
configuration, exact native spawns, and generated-grid pump truck clearance
**passed**. Full ARIA normal-input outcome **failed** in the latest run; manual
mission, result/return, renewed native route suite, human visual review, and
Android device acceptance remain **pending**. See `SupplyLine/WORKSPACE.md`.

Power Relay gates: static route clearance, packed source hash, focused rules,
and complete public-input ARIA victory through Campaign return **passed**.
Manual play, native negative paths, school landmark and camera visual review,
human approval, and device acceptance remain **pending**. See
`PowerRelay/WORKSPACE.md`.

Airlift gates: packed source, static route clearance, 15 focused passenger-rule
cases, two socket cases, 13 Show Me cases and complete final-layout ARIA/manual
native journeys **passed**. Human map/camera/minimap review, updated briefing
voices, native negative journeys, saved-state migration and device acceptance
remain **pending**. See `Airlift/WORKSPACE.md` for preserved failures and evidence.

Air Corridor gates: prepared source, full Barracks footprint/service clearance,
reachable overlapping defense coverage, focused mission/media checks, open
marker geometry, 22 vehicle and 11 building selection cases, Burst resources,
28 Build drawer and 31 production checks passed. Native shortage/cancel/refund
checks passed. Final English manual and Persian ARIA victory/result/settlement/
latest Campaign return passed. Saved Continue, legacy-map rejection, real radar
loss with no rewards, and fresh Retry through victory passed. Previous failures
remain in the evidence. Human map/camera/minimap review, packaged device content,
device acceptance and other affected mission regressions remain pending; see
`AirCorridor/WORKSPACE.md`.
