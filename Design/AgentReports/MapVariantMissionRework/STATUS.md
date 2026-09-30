# Existing Campaign map migration status

Updated 2026-09-30. This isolated checkout contains the CH02-M02 Supply Line
pilot and a CH02-M04 Power Relay review candidate. No map or mission is
qualified for release, and the new Power Relay binding has not been promoted
to `main`.

| Mission | Planned disposition | This checkout |
|---|---|---|
| CH01-M01 First Contact | Retain urban | No migration; baseline not rerun |
| CH01-M02 Establish the Base | Retain urban | No migration; baseline not rerun |
| CH01-M03 Radar Warning | Retain urban | No migration; baseline not rerun |
| CH01-M04 Airlift | CityEdgeAirfield | Not started |
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
| CH04-M01 Air Corridor | CityEdgeAirfield | Not started |
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
