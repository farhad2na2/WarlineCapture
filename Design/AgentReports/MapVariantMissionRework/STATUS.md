# Existing Campaign map migration status

Updated 2026-10-01. Seven migrated candidates have completed final native
English/Persian normal-input mission journeys. The isolated branch is
`codex/airlift-airfield-review`; these bindings have not been promoted to `main`.
See `REVIEW.md` for the ordinary Campaign review entry and priorities. Human
visual/player review and real device acceptance remain pending.

| Mission | Map disposition | Editor evidence |
|---|---|---|
| CH01-M01 First Contact | Retain urban | Asset baseline preserved; native regression not rerun |
| CH01-M02 Establish the Base | Retain urban | Asset baseline preserved; native regression not rerun |
| CH01-M03 Radar Warning | Retain urban | Asset baseline preserved; native regression not rerun |
| CH01-M04 Airlift | CityEdgeAirfield | Final English ARIA / Persian manual extraction, result and return passed; `Airlift/WORKSPACE.md` |
| CH01-M05 Breach Assault | Retain urban | Asset baseline preserved; native regression not rerun |
| CH02-M01 Gridlock | Retain urban | Asset baseline preserved; native regression not rerun |
| CH02-M02 Supply Line | RefineryDistrict | English ARIA / Persian manual, all 40 deliveries, real reroute arrival, reserve hold and result/return passed; `SupplyLine/READINESS.md` |
| CH02-M03 Market Lifeline | Retain urban | Legacy generator basis preserved; native regression not rerun |
| CH02-M04 Power Relay | RefineryDistrict | English manual / Persian ARIA, existing school frontage, complete protected route/repair/hold and result/return passed; `PowerRelay/READINESS.md` |
| CH02-M05 Route Reopened | AshLinePort | English ARIA / Persian manual, native convoy/records losses, fresh Retry and result/return passed; `RouteReopened/READINESS.md` |
| CH03-M01 Signal Trace | Retain urban | Asset baseline preserved; native regression not rerun |
| CH03-M02 Safehouse Sweep | Retain urban | Asset baseline preserved; native regression not rerun |
| CH03-M03 False Front | Retain urban | Asset baseline preserved; native regression not rerun |
| CH03-M04 Evidence Chain | Retain urban | Asset baseline preserved; native regression not rerun |
| CH03-M05 Network Break | Retain urban | Asset baseline preserved; native regression not rerun |
| CH04-M01 Air Corridor | CityEdgeAirfield | English manual / Persian ARIA, visible disabled Build actions, shortage/refund, saved Continue, real radar loss/Retry and result/return passed; `AirCorridor/WORKSPACE.md` |
| CH04-M02 Steel Push | RefineryDistrict | English ARIA first clear/replay, Persian manual, shortage, native defeat/Retry and result/return passed; `SteelPush/READINESS.md` |
| CH04-M03 Split Front | RefineryDistrict | English ARIA / Persian manual, direct Attack/fire, all five hostile defeats, intact core, settlement/result/return passed; optional production-context Smoke and native pre-launch Hold passed; `SplitFront/READINESS.md` |

## Scope of verification

Native mission journeys qualify the exact migrated layouts for Editor review.
Agent visual inspection, focused automated checks, normal-input journeys and
human/device acceptance are separate evidence categories in each mission report.
No new gameplay controls were added. Existing ARIA Play / Stop, colorful commands
and approved selection/ground marker direction are retained.

The retained asset comparison checked 27 mission definition/operation-map assets
against baseline `aeffb64c` with zero mismatches. It does not claim native gameplay
regressions for the eleven retained missions. Shared hauler changes passed three
native ECS detour cases and all 31 resource-hauler cases; target-device performance
and other affected native regressions remain separate release gates.

## Remaining release gates

- Human map/camera/minimap, visual direction and real player acceptance.
- Packaged device content, performance and real device acceptance.
- Updated/approved voice assets where each report says they are pending;
  Split Front currently uses captioned content.
- Saved-state/resume compatibility and dedicated native negative/replay/access
  journeys not already qualified for each mission.
- Broader affected/retained mission regression before production integration.

Full failed and superseded runs remain in the evidence and log manifest. Earlier
wins do not substitute for the final delivery/layout requirements. No candidate
is described as release-qualified.
