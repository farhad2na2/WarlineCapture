# Prepared-map mission review

This branch is an isolated Editor review candidate. Human review and real device acceptance remain pending. Normal-input mission runs, automated checks and visual inspection are reported separately below.

## Open the review

Use the isolated checkout `/Users/farhad/.codex/worktrees/airlift-airfield/WarlineCapture`. In its Unity Editor choose **Game → Campaign → Prepared Maps → Open Seven Mission Review**. This opens the ordinary Campaign menu using a separate review profile, with all mission nodes available. Existing progress is preserved. Select any mission and use Deploy; ARIA Play / Stop and normal command controls remain available.

| Mission | New map | Review status |
|---|---|---|
| CH01-M04 Airlift | CityEdgeAirfield | Final English ARIA and Persian manual journey passed; ready for review |
| CH02-M02 Supply Line | RefineryDistrict | English ARIA / Persian manual full delivery, reserve hold and result/return passed; ready for review |
| CH02-M04 Power Relay | RefineryDistrict | Final English manual / Persian ARIA, school landmark, repair/hold and result/return passed; ready for review |
| CH02-M05 Route Reopened | AshLinePort | Final English ARIA / Persian manual and native loss / Retry journeys passed; ready for review |
| CH04-M01 Air Corridor | CityEdgeAirfield | Final English manual / Persian ARIA, loss / Retry and Build checks passed; ready for review |
| CH04-M02 Steel Push | RefineryDistrict | Final English ARIA / Persian manual, shortage, native defeat / Retry passed; ready for review |
| CH04-M03 Split Front | RefineryDistrict | Final English ARIA / Persian manual, optional Smoke/Hold and result/return passed; ready for review |

## Review priorities

- **Airlift:** landing, boarding and exit markers; extraction visibility and return.
- **Supply Line:** pump inside its fenced yard, single fences, refinery fit, southern truck reroute, visible newly delivered Fuel count and full reserve hold.
- **Power Relay:** protected convoy road, identifiable school shelter frontage, repair zone, both engineers and readable guidance.
- **Route Reopened:** convoy arrivals, hub and records markers, capture objective and result.
- **Air Corridor:** helicopter handling, radar protection, visibly disabled Build options and selection markers.
- **Steel Push:** tank / building selection markers, refinery approach, base defense and reserve.
- **Split Front:** launcher Attack using existing controls, industrial road clearances, delayed diversion, civilian separation and optional Smoke.

Keep remaining release gates separate: packaged device content/performance, real player/device acceptance, updated voice assets where reported, saved-state compatibility and mission-specific negative journeys not already qualified. See each mission's readiness/workspace document and retained full validation logs. These candidates have not been promoted to `main`.
