# S005 current acceptance

**Not technically ready / not player-ready.** Catalog remains InProgress. This is the current status; prior candidate narratives and failed evidence remain in `CURRENT_ACCEPTANCE-history-through-native934.md` and their original proofs. `TECHNICAL_READINESS_CURRENT.json` records the active validation and completed proofs.

Current source: `816a3ae39556d1bc1308eb67ec466e7859cb6dc6a56cdde5b084620a4fc5dc26`. Existing Campaign map, materials, fixed gameplay budgets and S005 binding are unchanged. Compilation completed without errors (`compile-930-fullmap-input-status.json`).

## Passed on the current source

- Shared selection/input regressions: native930, including full-map Open/Close preserving the next deliberate world command. Actual wrapper/bridge0; full source PRE/POST matched. Legacy pass marker reports65 tests, while the executed suite contains69 RunCase calls. See `native930-focused-current-proof.json`.
- Normal paid Heavy APC boarding and recovery: native931, four passengers boarded, schema7 Save/Continue preserved the attempt and paid280 receipt,4978 old physical entities removed, four restored passenger links and hidden visuals verified, then four unloaded through native controls. Strict input/source audits passed. This is a focused recovery case, not a full-match win. See `native931-normal-boarded-save-continue-unload-pass.json`.
- Complete English and Persian manual matches: native932–933, victories at306.749s and131.645s, both with home HP800, strict touch/receipt audits, frozen result and Main Menu return. The English match includes combat Save/Continue with the paid AntiAir220 receipt preserved and the same attempt resumed. See `native932-933-current-manual-en-fa-locale-pair.json`.
- Complete fresh ARIA baseline: native934–939, three seeds in English and Persian on Standard/Regular. All six normal journey audits passed, including native menu/Play/Confirm, frozen result/Adjust Setup return, strict touch/receipt and full source equality. Per-locale outcomes and timings are recorded in `native934-939-current-fresh-aria-seed-matrix.json`; this minimum baseline does not substitute for family20 or additional exposed presets.

These are agent-operated native controls in the Editor, not real human or phone acceptance.

## Retained technical evidence and applicability

Native923's fifteen ground/infantry paid queues, native member delivery/ownership, declared target domains, death capacity release/idempotence and owned-world cleanup remain applicable to their unchanged API scope. All source changes since that execution are exactly the shared full-map UI handler, Editor counter reset and selection/input tests; production/prefab/config/economy/actor/death dependencies are byte-identical. This is a source comparison, not a new run, normal UI production proof or full role lifecycle. See `native923-ground-api-current-scope-applicability.json`.

Native890's normal paid Strike production/flight/fire/Return/touchdown/refuel/second-sortie case remains pinned to its original source and scoped applicability record. Current aircraft recovery/capacity and remaining-role validation are still required. Earlier ARIA matrices and manual victories remain recorded against their exact old source hashes and are excluded from the current complete-journey baseline.

## Pending gates

Validate remaining enabled-role movement/combat/service lifecycles, aircraft taxi/flight recovery and bilateral parking/runway capacity; close remaining checkpoint/result/replay and exposed-preset cases; run affected Campaign M1–M5 regressions and applicable family/stress/cleanup/performance checks. Owner native visual review is deferred until technically ready. Real player/device acceptance remains a separate gate.


Native935: ARIA fa-IR seed104734, Victory at224.826s. Normal journey audits passed; actual wrapper0/bridge0; full source unchanged=True. Proof `native935-current-fresh-aria-fa-IR-seed104734-victory.json`.

Native936: ARIA en seed130368, Victory at236.787s. Normal journey audits passed; actual wrapper0/bridge0; full source unchanged=True. Proof `native936-current-fresh-aria-en-seed130368-victory.json`.

Native937: ARIA fa-IR seed130368, Victory at236.853s. Normal journey audits passed; actual wrapper0/bridge0; full source unchanged=True. Proof `native937-current-fresh-aria-fa-IR-seed130368-victory.json`.

Native938: ARIA en seed155926, Victory at240.696s. Normal journey audits passed; actual wrapper0/bridge0; full source unchanged=True. Proof `native938-current-fresh-aria-en-seed155926-victory.json`.

Native939: ARIA fa-IR seed155926, Victory at240.917s. Normal journey audits passed; actual wrapper0/bridge0; full source unchanged=True. Proof `native939-current-fresh-aria-fa-IR-seed155926-victory.json`.

Native940: Campaign M1 bilingual player-action regression Failed; wrapper1/bridge1; full source unchanged=True. Input scope is native raycast UI clicks and screen-position command requests, without outcome injection. Proof `native940-shared-campaign-m01-en-fa-journey-failure.json`.

Native948: Campaign M1 bilingual player-action regression Failed; wrapper1/bridge1; full source unchanged=True. Input scope is native raycast UI clicks and screen-position command requests, without outcome injection. Proof `native948-shared-campaign-m01-en-fa-journey-failure.json`.

Native947: current-source checkpoint staging/conservation, transient, support and Strike recovery technical suites passed; actual wrapper0/bridge0, all suite markers present and complete source unchanged. Negative missing-footprint and incomplete-map cases are intentional fixtures. No normal aircraft journey or full X36 closure is claimed. Proof `native947-focused-current-proof.json`.
