# Implemented mission monetization remediation

Baseline: `3d728d005` on clean `main`; exact SHA-256 inventory of 516 source/config files is in [baseline-source-hashes.json](Evidence/baseline-source-hashes.json). Existing Editor PID 98258, Unity 6000.5.2f1, Pipeline 0.6.0-exp.1; Hub open. Fig has a separate Editor; preserve both. No live purchases or ownership grants exist in the inspected runtime.

## Package ledger

| Package | Current status | Evidence / dependency |
|---|---|---|
| IM-00 | Baseline recorded; source audit continuing | Clean starting tree; 205 policy rows; historical readiness remains historical. |
| IM-01 | Construction/recruitment consumers traced | Placement transaction → faction helper; camp order → unit metadata → same helper; account SaveService is absent from these paths. |
| IM-02 | Source candidate authored; runtime acceptance pending | Explicit complete Materials price list, zero tactical Credits, migrated budgets, HUD/briefing repair. |
| IM-03 | Source candidate; focused Unity regression passed | No usable APC parts consumer found. New reward teaser removed, result calls them archived parts; grants and earned data retained. Versioned non-destructive migration. Smoke remains canonical. |
| IM-04 | Authority and launch integration candidate | All 205 canonical memberships match the CSV. Four ownership profiles pass isolated checks. Release ownership restrictions remain disabled. Actual platform billing/restore/offline storage is not implemented. |
| IM-05 | Access mapping candidate; input regression pending | S001/S025/S073 prototype mapping and S002–S004 expanded resolver consult shared access authority. Existing local economy/research rules retained. |
| IM-06 | Scoped model candidate; shipping integration pending | Intro/full scope travels through run, launch, checkpoint and result. Intro filters missions/districts/incidents/adjacency and completes after three Victories. Fresh full run retains account first clears. Native O002/O003 screens and input remain pending visual approval. |
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
- `economy-assets-01.log`: wrapper exit 0 and `[ImplementedMissionEconomyAssets] result=Passed`. All four authored budgets updated through the checked Editor builder.
- `economy-transactions-01.log`: wrapper exit 0; 7 placement and 8 faction transaction checks, Steel Push rules, and `[ImplementedMissionEconomy] result=Passed`. These precede later scope/access changes and are not full-candidate native acceptance.
- `operations-p4-02.log`: host/model regression passed 14 checks after fixing a compile error in the new scope guard (the initial missing `OperationsCommand.MissionId` error was a remediation defect).
- `product-scope-01.log`: access passed; scope fixture failed its duplicate-settlement assertion. The fixture incorrectly called CompleteSettlement without a pending commit; corrected to submit the duplicate result through ProbeSettlement. Failure retained.
- `product-scope-03.log`: host/model checks passed access 205/four ownership profiles and intro filtering/checkpoint/completion/replay/upgrade/account reward deduplication. No native input or human acceptance is implied. Subsequent local-day/adjacency filtering is under revalidation.
- `air-en-input-01.log`: wrapper exit 1, `[AirCorridorInput] result=Failed Comic voice cut off: air_corridor-comms-laila`. Actual missiles and radar support observed, seven ARIA actions, no completed result/return certificate. Screenshots retained under `Evidence/20260928-213511-air-corridor-en/`.
- Candidate repair: dialogue auto-advance now waits until its audio source finishes, rather than advancing solely from elapsed frame time. The second Air retest was interrupted by shared assembly reload; audio repair remains without a completed input certificate. Radar Warning localized briefing now shows 140 Materials; checked import passed.
- `products-unity-01.log`: wrapper exit 1, missing new validation type because a shared Main Menu assembly compile failed. `products-unity-02` dispatch failed during reload, existing Editor preserved. Neither is a pass; later successful compilation does not retroactively pass them.

## Shared checkout changes and visual review

The Support and Menu/Header chats share this checkout and Editor. The user authorized messaging the Support chat to coordinate Editor time. Play/validation windows are coordinated; no unrelated Editor/Hub/licensing process was terminated.

Shared commits `d3d4c5824` and `4e47f0410` appeared during implementation and included this task's earlier in-progress economy/access/scope source and O002 mockup. Preserve them; they are not acceptance certificates. Subsequent uncommitted changes need their own hash/evidence record. Menu/Header owns the account menu read models and native home presentation; do not duplicate that work.

Built-in ImageGen produced [O002 corrected stock](o002-hud-direction-v2.png) and [O003](o003-hud-direction-v1.png) directions using actual Steel Push Campaign and O001 native HUD references. Exact prompts/references/output paths are in [mockup-prompts-v1.json](mockup-prompts-v1.json). User visual-direction review requested and pending; no O002/O003 native screen work started. Generated map art is illustrative and does not alter authored graphs, force budgets, routes or assets.

## Acceptance remains separate

Historical complete Editor journeys: M01–M05, Supply Line, Air Corridor, Steel Push. Gridlock, CH02-M03–M05/CH03-M01–M05 remain coded candidates without complete current acceptance. Prototype mappings do not certify expanded Skirmish. O001 historical manual input was Partial; ARIA wins do not close manual Victory. O002/O003 model outcomes do not close shipping input.

Native EN/FA review, automated checks, agent-operated normal input, real-player acceptance, packaged-player and physical-device acceptance remain separate gates. Complete 25+120+60 content and real commerce are independent release dependencies.

## Latest evidence, 2026-09-29

- `products-unity-03.log` and receipt 0: `[ImplementedMissionProducts] result=Passed` and access marker. Profile preservation/idempotency/future version, M05 first-clear/replay/duplicate, Smoke canonical/early replay checks passed. These are focused checks, not native result screens.
- `economy-transactions-02.log` and receipt 0: 7 placement, 8 faction, Steel rules and authored prices/wallet isolation/shortage/retry/duplicate/refund/commit-failure markers passed.
- `reward-copy-01.log` and receipt 0: checked asset import passed M05 teaser removal with earned parts retained, plus M03 140 Materials copy.
- `product-scope-08.log`: access 205, Operations scope and checkpoint compatibility markers passed, including actual cross-scope conflicting-result rejection. Host-only model coverage; platform restore/offline and playable catalogs remain unaccepted.
- A subsequent direct host rerun passed the same three markers. Its output was observed but not saved as a new log, so `product-scope-08.log` remains the retained artifact.
- `operations-p1-01.log` and `operations-p1-behavior-02.log`: 18 model regressions passed; `operations-p4-03.log`: 14 passed.
- `air-en-input-02.log`: interrupted by assembly reload before completion; wrapper exit 124, no receipt and no pass marker. Real combat reached 3/6 hostiles with seven ARIA actions; no result/return. Failed screenshots preserved under `Evidence/20260928-215345-air-corridor-en/`. Editor was subsequently observed stopped; no process was terminated by this chat.
- Wrapper candidate now records an active capture's assembly reload as failed, and stops only Play initiated by that capture. Probe input routing is restored before reload. Compilation completed with failed=false/errors=[]; focused wrapper reruns passed. Reload interruption cleanup itself remains without a deliberate trigger test; historical failure is retained unchanged.
- `candidate-before-final-focused.json` records 3,424 files before the next focused validation. Shared Menu/Header changes continue; a dispatch snapshot is not a whole latest-checkout acceptance certificate.

Remaining work: current native EN/FA input journeys (manual and ARIA), M05 native reward/result/return review, fresh Chapter 1 and affected Skirmish/Supply Line regressions; O002/O003 visual approval followed by shipping integration and intro/full UI; profile/platform commerce acceptance; player, packaged-player and device gates. No live ownership restrictions or purchases have been enabled.

- Latest `products-unity-04.log` receipt 0 contains access, product and wrapper pass markers. `economy-transactions-03.log` receipt 0 contains placement/faction/Steel/economy and wrapper pass markers. These ran in the connected GUI Editor after cleanup compiled successfully.
- Before/after source comparison (`focused-04-source-comparison.json`) found one concurrent Editor source edit, `ComicPortraitRuntimeValidation.cs`; no tracked mission economy/access/scope runtime change occurred between those snapshots. This still is not a complete latest native candidate certificate.
