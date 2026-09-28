# Implemented mission monetization remediation

Baseline: `3d728d005` on clean `main`; exact SHA-256 inventory of 516 source/config files is in [baseline-source-hashes.json](Evidence/baseline-source-hashes.json). Existing Editor PID 98258, Unity 6000.5.2f1, Pipeline 0.6.0-exp.1; Hub open. Fig has a separate Editor; preserve both. No live purchases or ownership grants exist in the inspected runtime.

## Package ledger

| Package | Current status | Evidence / dependency |
|---|---|---|
| IM-00 | Baseline recorded; source audit continuing | Clean starting tree; 205 policy rows; historical readiness remains historical. |
| IM-01 | Construction/recruitment consumers traced | Placement transaction → faction helper; camp order → unit metadata → same helper; account SaveService is absent from these paths. |
| IM-02 | Source candidate authored; runtime acceptance pending | Explicit complete Materials price list, zero tactical Credits, migrated budgets, HUD/briefing repair. |
| IM-03 | Consumer audit in progress | M05 APC parts only have grant/storage consumers; no usable upgrade application found. Smoke is already canonical; preserve. |
| IM-04 | Pending | One outside-simulation access authority and verified entitlement boundary. |
| IM-05 | Pending | Stable S001/S025/S073 prototype mapping and S002–S004 expanded paths. |
| IM-06 | Pending | Existing shipping Operations flow is O001-specific; O002/O003 remain model candidates. Intro/full isolation and shipping input still required. |
| IM-07 | Pending | New source invalidates historic economy/HUD certificates; do not promote readiness. |

## Before / candidate costs

All costs below are owned by the scenario-controlled faction, never the account wallet. M02 retains the existing Materials-only policy. For migrated defense candidates an exact original cost pair selects an authored complete price; unknown Credit prices fail closed.

| Action | Previous tactical cost | Defense candidate Materials | M02 Materials | Actual consumer |
|---|---:|---:|---:|---|
| Barracks | 40,000 Credits + 90 Materials | 120 | 90 | BuildingPlacementConstructionTransaction → TryReserve → FactionConstructionResourceUtilitySystemHelper |
| Guard Tower | 22,000 + 50 | 70 | Not offered | Same placement/rollback transaction |
| Road barrier | 6,000 + 15 | 20 | Not offered | Same placement/rollback transaction |
| Four rifle soldiers | 10,000 + 20 | 30 | 20 | BuildingProductionCampRequestTransaction → configured metadata → TrySpendConstructionResources |

| Mission | Old serialized Credits / Materials | Candidate Credits / Materials | Budget rationale |
|---|---:|---:|---|
| M02 | 55,000 / 120; Money already suppressed | 0 / 120 | Barracks 90 + squad 20 = 10 remaining. No rebalance. |
| Radar Warning | 50,000 / 100 | 0 / 140 | Tower 70 + barrier 20 + squad 30 = 20 remaining; or two towers exhaust 140. Existing supplied force, scan allowance and pacing unchanged. |
| Air Corridor | 50,000 / 100 | 0 / 140 | Supplied G2A/radar remain required counters; optional ground preparations use the same explicit defense price list. |
| Steel Push | 50,000 / 200 | 0 / 200 | Two towers 140 + barrier 20 + squad 30 = 10 remaining. Physical reserve still 160 with 40 protected and 120 usable. No ambient Fuel bypass. |

These are authored candidates, not accepted balance values. Real shortages, manual/ARIA combat and retry evidence must validate them. Credit components are not suppressed by merely enabling the prior flag: the version-2 price list increases the complete Materials costs explicitly.

## Validation and failures

- Initial sandbox process inspection denied; read-only process/Pipeline inspection succeeded with escalation. This is an environment permission result, not a Unity licensing failure.
- First compilation-status query encountered connection reset during domain reload; the next status returned completed, failed=false, errors=[].
- All forthcoming wrapper logs and receipts are retained in Evidence. A receipt without the required marker does not pass.

## Acceptance remains separate

Historical complete Editor journeys: M01–M05, Supply Line, Air Corridor, Steel Push. Gridlock, CH02-M03–M05/CH03-M01–M05 remain coded candidates without complete current acceptance. Prototype mappings do not certify expanded Skirmish. O001 historical manual input was Partial; ARIA wins do not close manual Victory. O002/O003 model outcomes do not close shipping input.

Native EN/FA review, automated checks, agent-operated normal input, real-player acceptance, packaged-player and physical-device acceptance remain separate gates. Complete 25+120+60 content and real commerce are independent release dependencies.
