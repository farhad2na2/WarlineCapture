# Coded mission changes and readiness evidence

Date: 2026-09-28. Source/document review after `f419cd462`, with unrelated Support implementation in the working tree. No new Unity sessions or gameplay acceptance were run. The [mission product contract](../Monetization/Mission_Product_Contract_2026-09-28.md) and [205-entry register](../Monetization/Mission_Product_Policies_2026-09-28.csv) define the updated implementation plans.

Implementation assignment: [implemented-missions remediation handoff](../Monetization/Implemented_Missions_Remediation_Handoff_2026-09-28.md). It rechecks the later `a299f9c5f` Support commit and treats the real Steel Push Smoke reward as implemented work to preserve and verify; the earlier audit rows below retain their original evidence context.

## Existing Campaign missions with complete recorded Editor journeys

These eight missions have evidence of complete normal-input Editor journeys. This is the useful implementation shortlist, not a claim that the current dirty build or a phone release is certified. Human, device, presentation and recovery limitations remain as stated in each original report.

| Mission | Required modification or check | Preserve | Evidence and limit |
|---|---|---|---|
| CH01-M01 First Contact | Shared reward/profile presentation and free-access/ARIA regression only; no new tactical mechanic. Keep commerce outside the opening and debrief. | Select/move/attack, story, immediate retry. | [M01–M05 completion](../Roadmap/M01_M05_Readiness/COMPLETION.md): bilingual complete journeys; phone and unfamiliar-player gates pending; APK predates a source follow-up. |
| CH01-M02 Establish The Base | Shared access/reward regression; confirm no legacy Money or account wallet can re-enter affordability. | Already Materials-only: 120 start, 90 Barracks, 20 squad. | Same Chapter 1 evidence; no new economy redesign required. |
| CH01-M03 Radar Warning | **Gameplay economy migration:** replace remaining tactical Money/Credits cost paths with validated Materials costs/budget; update HUD/affordability and rerun preparation/defense and retry. | Radar/scan allowance, defense rhythm and current shortened wait; no harder free finale funnel. | Same Chapter 1 evidence. Serialized 50,000 scenario Credits are distinct from account Credits; actual cost users must be traced before migration. |
| CH01-M04 Airlift | Shared access/reward regression and reward-label/consumer audit. Keep normal transport distinct from optional Support products. | Existing boarding, clearance, extraction and failure/retry; economy remains disabled. | Same Chapter 1 evidence. No new paid Fuel or transport capacity. |
| CH01-M05 Breach Assault | **Progression/result change:** settle debrief, clue and rewards before the package offer; gate CH02 separately from earned progression. Resolve the 35-APC-parts target/consumer and retain legacy ownership. | Current combat difficulty, free finale/replay, ARIA and story resolution for the chapter. | Same Chapter 1 evidence. No combat redesign needed solely for monetization. |
| CH02-M02 Supply Line | Shared owned-content/zero-wallet/reward checks. Retain actual Oil/refinery/Fuel routing, reserve action and supplied force. | Functional authored logistics chain, 20 protected civilian barrels, normal manual/ARIA route recovery. | [Supply Line validation](CH02M02SupplyLine/VALIDATION.md): manual and Watch victories, real hauling, result/return, 54 regressions + 13 rule groups. Captioned presentation; new voices, broader lifecycle/device acceptance and a Persian label remain unclosed. |
| CH04-M01 Air Corridor | **Gameplay economy migration:** audit/migrate tactical Money dependence and HUD to Materials, with enough budget for supported construction/recruitment; verify supplied anti-air/radar and ordinary owned progression. | Air-defense decisions, scenario-provided counters, voices/comics and ARIA. | [Air Corridor readiness](CH04M01AirCorridor/readiness.md): EN/FA normal-input victories, settlement/return and ARIA. Human, packaged-player and physical-device acceptance pending. |
| CH04-M02 Steel Push | **Gameplay economy migration:** replace tactical Money/Credits costs/presentation with a tested Materials budget. Integrate the earned Smoke milestone from the separate approved Support work; no paid charges. | Three-tank counterforce, actual combat, 120 usable military Fuel and protected 40 civilian barrels. | [Steel Push readiness](CH04M02SteelPush/readiness.md): complete EN/FA automated normal-input journeys. Human, packaged-player and device acceptance pending. |

Priority: three tactical economy migrations (Radar Warning, Air Corridor, Steel Push), M05's result/access/reward boundary, then shared integration regression on the other four. The monetization plan does not require rebuilding all eight missions.

## Coded candidates that must not be called fully play-ready

| Entry | What is coded/evidenced | Modification under this plan | Remaining evidence |
|---|---|---|---|
| CH02-M01 Gridlock | Voiced route-clearing checkpoint and one developmental Watch victory; [implementation record](CH02M01Gridlock/implementation_status.md). | First paid-mission access after ordinary M05 completion; no wallet, parts or account-upgrade prerequisite; supplied engineer tools. | Final public-entry/manual/Watch matrix and production acceptance remain incomplete. |
| CH02-M03–M05 and CH03-M01–M05 | Eight definitions/scenarios registered in the 17-entry Campaign sequence. | Apply their specific updated mission-plan rows, included-content access and scenario-owned tools/resources. | Registration is not acceptance; no complete mission-specific player-readiness certificate established in this review. |
| Original Base Assault prototype, compatibility mapping S001 | [Internal Editor readiness](../Roadmap/Skirmish_Prototype/COMPLETION.md): complete bilingual player-control win/loss/replay loop with local Materials/Oil/Fuel and zero Credits. | Free-sample identity, same ARIA access and account isolation; expanded S001 must earn its own acceptance. | Original prototype report does not certify expanded roster, full current Watch coverage or a phone release. |
| S025 City Crossroads / S073 Industrial Basin prototype mappings | Compatibility mappings in the [handoff manifest](../Roadmap/Skirmish_Expansion/IMPLEMENTATION_MANIFEST.csv). [Industrial inspection](Skirmish3Readiness/readiness.md) proves movement/build/defeat/replay, not a victory. | Core membership, scenario-only economy and per-expanded-entry recertification. | No expanded acceptance inferred from prototype mapping or that intentional-loss inspection. |
| Expanded S002 Ground Maneuver / Established Base | Checked-in publication manifest says Playable; [acceptance scaffold](SkirmishExpansion/S002/acceptance.md) explicitly leaves manual/ARIA matrix open. | Core ownership only; preserve Materials-funded readiness/research; no account roster purchase. | Complete required normal-input/ARIA/device evidence; do not change its existing publication flag in this documentation task. |
| Expanded S003 Air Mobile / Field Base; S004 Air Mobile / Established Base | Checked-in publication manifest says Playable, with flight/refuel and counted ARIA gaps. [S003 input evidence](SkirmishExpansion/S003/INPUT_EVIDENCE_AUDIT_2026-09-25.md) records pending native verification. | Core membership; preserve real Fuel/counters/starting grants and prohibit paid refills/queue boosts. | Historical S004 notes still say InProgress; current asset is newer for publication status, not proof of the missing acceptance. |
| O001 Street Signals | [Latest player-experience report](Operations/O001_PLAYER_EXPERIENCE_20260924/README.md): native UI, five Regular ARIA wins, Persian win, save/resume, 76 tests. Agent mouse journey reached Partial Success. | Add standalone intro/full scope to launch/save/settlement and introduction recap; retain tactical scan/evidence/extraction graph. | Report explicitly says not accepted as player-ready; complete human-controlled Victory and player/device gates. |
| O002 Clinic Supply Route; O003 Courtyard Water Point | Authored prototype graphs/model captures in [three-mission slice](../Roadmap/Operations/P4_VERTICAL_SLICE.md) and catalog. | Integrate normal-input shipping flow and standalone intro. O003 retains 80-Materials repair, supplied locally; completion requires all three Victories. | Model/capture wins do not certify shipping manual/ARIA or device acceptance. |

## Remaining planning scope

- **Campaign:** CH04-M03–M05 and CH05-M01–M05 are the eight entries absent from the audited 17-entry runtime registration. All 25 high-level mission entries and five chapter plans now include the product/resource contract. Existing coded-but-unaccepted missions remain identified above rather than being mislabeled as uncoded.
- **Skirmish:** all 120 stable IDs remain core content. Twenty six-entry packets carry explicit membership, sample status and shared economy/ARIA rules; their generator preserves the amendment. Neither this edit nor the generated manifest promotes a readiness status.
- **Operations:** all 60 briefs retain their objectives, deadlines, force packages, consequences and prerequisites. Each carries full-theater membership; O001–O003 additionally reference the standalone free scope. O004–O060 remain under their existing production gates.

## Validation for this documentation update

Required checks: exact 25/120/60 policy coverage and canonical IDs; five free Campaign rows, one free Skirmish row and three free Operations rows; valid local links/entry anchors; unchanged scenario matrices, IDs, numerical budgets and historical acceptance columns; generation consistency for the modified Skirmish packets; whitespace/diff review. Gameplay, storefront transactions and device acceptance are not run in this task.

The pre-edit full Skirmish generator check failed because its September 21 roster audit hardcodes 23 buildings while the current source contains 24. Preserve that failure as an existing source-inventory mismatch. Use the explicitly scoped packet-generation/check mode for this documentation change; it must not claim to validate the old roster inventory or overwrite historical roster hashes.

Completed documentation checks:

- PASS: 205 unique canonical identities and exact 25/120/60 membership; free scopes count 5/1/3.
- PASS: every Campaign and Operations entry has its specific adjustment; every Skirmish entry has its generated product contract. All policy-to-brief and Support references resolve.
- PASS: original Operations objective graphs, outcomes, deadlines and tactical implementation paragraphs remain intact. All twenty Skirmish packets preserve their original gameplay paragraphs/tables exactly after removing the added product sections.
- PASS: scenario catalogs, implementation/status manifest, work queue, 360 setup vectors and historical roster-audit CSV remain byte-identical to HEAD.
- PASS: scoped generator `--check --packets-only` reports `[SkirmishPacketValidation] result=Passed scenarios=120 packets=20 productPolicies=120 sourceInventory=NotChecked manifests=NotWritten mode=check`.
- PASS: local-link review and `git diff --check` for the documentation changes.
- OPEN, pre-existing: full generator source-inventory audit raises `AssertionError: ('Building', 24)` against its historical 23-building expectation. No source inventory or gameplay acceptance pass is claimed.

No gameplay source, scene, prefab, balance JSON or publication status was edited by this planning task. Concurrent Support changes to those files were left intact.
