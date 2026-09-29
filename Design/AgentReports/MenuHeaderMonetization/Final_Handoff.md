# Menu/header implementation handoff

Updated 2026-09-29. **Partial implementation; full handoff acceptance remains open.** Changes are uncommitted in the shared checkout. Concurrent mission, Operations, portrait and validation work is preserved. Baseline for this task: d3d4c5824.

## Implemented

- Campaign-led native home with one progression target shared by art, chapter, title, concise purpose and Continue. Continue requests selection/review, never deployment. Missing art is neutral; completed selection says Choose Mission. Duplicate activation is guarded.
- Credits read from the existing SaveService through one ECS projection, with neutral loading/error display, zero/large values and save-version refresh. Legacy Command storage is retained. No reward settlement or new wallet is implemented in UI.
- Thirteen menu Content prefabs have informational Credits and removed Command/plus controls. A first migration also touched a combat popup; that mistake was repaired through Unity and the migration is now restricted to Content prefabs. The combat popup has no account binding; its remaining serialization adds zero/default font-bound fields without changing combat resource behavior.
- Dedicated saved commander portrait/name card, unchanged native ARIA identity, large independent mode routes, Settings, and compact Store/Armory routes. Early mode/Store interaction and raycasts are disabled before their reveal milestone.
- English/Persian native labels, raw player-name rendering, shared Persian font coverage, wide-layout text widths and chevrons. Account initialization no longer seeds sample balances or fabricated commander identity.
- Commander name capacity now supports the onboarding limit of 32 multibyte characters. Oversized imported names are safely shortened for display without changing the saved identity; the regression also verifies intact UTF-8.
- Campaign deploy consumes the existing access authority through the ECS gateway; UI does not implement entitlements. Visual contract and authoring builders were updated together.

## Evidence categories

| Category | Result |
|---|---|
| Home/header ImageGen direction | Approved in the original handoff. No repeated approval requested. |
| Store ImageGen direction | [Two-collection proposal](Mockups/store-content-v01.png) and exact prompt saved; user review remains pending. Its implementation is paused under the handoff visual gate. |
| Automated native checks | Passed in Logs/menu-header-native-07.log: account refresh/recreation, multibyte names, preserved legacy field, target routing, missing art, clear/reload/completion, duplicate taps, hidden-control interaction, hierarchy and unchanged ARIA binding. |
| Native visual review by agent | Eight final fixture renders in After/: ordinary and long-name/large-balance EN/FA at 1920x1080 and 2400x1080. All four stress renders reviewed: full 32-character names wrap within the identity card, and 2,147,483,647 fits. Earlier failed visual iterations retained. No chapter tofu or reversed Latin name remains. This is not player acceptance. |
| Normal-input mission play | Steel Push rerun passed: 7-panel comic, live armor combat, physical Fuel spending, 5 ARIA actions, victory, result, settlement and Campaign return. M05 English also reached victory/result/settlement/Campaign return after the agent started the visible shipping ARIA control; its Persian replay passed fresh-actor/objective and return checks using command APIs, with a manual Select step. See Logs/menu-header-steel-input-02.log, Logs/menu-header-m05-input-01.log and Gameplay/. Both probes seed isolated progression and do not inject battle outcomes. They do not prove fresh-player, real account-header settlement or physical-device acceptance. |
| Human / packaged / physical device | Not performed; pending. |
| Billing / restore / release-certified collections | Not implemented or accepted. Commerce remains disabled. |

Final render fixtures deliberately use Steel Push and Credits 0 to expose the whole hierarchy; neither is a runtime initialization value. Runtime target uses registered catalog/progression, and runtime Credits use the real profile. ARIA binds Assets/Game/Art/UI/V3Shared/Portraits/ARIA_MainMenu_V3.png, with original import settings and preserveAspect. Her raster source and meta have no task diff.

## Remaining work / gates

1. Record the user decision on the Store proposal, then retire the existing consumables preview, fake prices and scarcity. Its current checkout is disabled, but its catalog copy is still obsolete. Do not mark MH-04 complete.
2. Complete shared mode ownership/readiness/download presentation and fixture combinations. Operations reveal currently uses chapter completion; the production-readiness dependency still needs consumption. Do not infer certification from MissionRuntimeEnabled.
3. M05 and Steel Push journeys passed within the evidence scopes above. Resolve M05 native result text clipping (the Heavy APC value is ellipsized in the English result capture), then complete Operations intro result/return when its scope is ready. Verify real reward settlement updates every account surface exactly once; isolated mission save owners do not establish the real profile header update.
4. Complete fresh-profile cold open/identity/M01/debrief/headquarters normal-input flow, all route/back/focus/modal cycles, enlarged text and physical safe-area/touch testing. Long-name/large-balance native fixtures now pass within their recorded scope. Baseline FA capture was not acquired and is explicitly missing.
5. Implement injectable commerce states only behind a real adapter contract; real storefront sandbox/restore, offline owner/revocation, product certification and physical-device acceptance are separate gates. No test-only ownership shortcut should ship enabled.
6. Audit the complete Commander/Profile, Armory and reward bodies for prototype earned-stat/collection claims. The new home identity and shared account headers do not establish that those older screen bodies are fully truthful.

The state coverage and gaps are detailed in [State_Matrix.md](State_Matrix.md); data ownership in [Inventory.md](Inventory.md). Current source/config SHA-256 evidence is in candidate-before-steel-input-01.json and the post-run manifest.

## Failure ledger (retained)

- Initial compile boundary errors were fixed by projecting runtime access into a UI-only enum rather than referencing runtime/Missions types from UI Contracts.
- Capture dispatch 01/02 failed during Pipeline disconnect/reload; dispatch receipts retained. No screenshots from those runs were claimed.
- tests-01 failed because a stale sprite survived an invalid target. The view clears it; tests-02 passed.
- localization-01 failed on three missing explicit translations. localization-02 passed after the entries were added.
- native-01 failed because the live Editor still exposed the prior test entrypoints. Recompilation resolved it.
- native-02 failed because English copy assertions ran under the persisted Persian locale. Tests now restore and scope their fixture locale.
- native-03 correctly failed because the shared Editor was importing/compiling at invocation. It was rerun after idle verification, without licensing recovery or alternate execution.
- native-04 automated checks passed, but visual review found Persian glyph/name/size issues and long purpose copy. Those captures are kept in After/iteration-01.
- native-05 passed after those corrections; visual review then found wide-width text/arrows needing adjustment. Captures are kept in After/iteration-02.
- native-06 passed with the final responsive corrections. Its required suite and wrapper markers are retained in the full log.
- native-07 passed after the valid Persian-name capacity fix and produced four ordinary plus four stress captures. The native review confirmed the long-name and large-number layouts.
- steel-input-01 failed after the agent's manual squad click triggered physical takeover while ARIA was running. The log records physicalTakeover before ARIA stopped; this is an interrupted run, not a gameplay/readiness pass. The second run passed without manual takeover; both logs are retained.
- m05-input-01 passed, but the older guided probe did not advance the new first lesson automatically. The agent started Watch ARIA / Start using native controls. Persian replay also required native Select input before the probe resumed command API combat. This mixed harness is not a fully unattended or entirely physical-input acceptance run.
- Source/document whitespace validation passed. The full shared diff whitespace check failed on Unity-authored empty YAML fields and wrapped localization serialization. Generated Unity assets were preserved; no manual YAML cleanup was applied.
- build-01 contained a CLI acknowledgement timeout while the wrapper-owned execution and required markers passed. It was not treated as a licensing blocker.

All Unity runs used Tools/CI/invoke_unity_macos.sh with GUI licensing/reuse, explicit logs/timeouts, Hub open, and no batchmode, direct Unity invocation, IPC reset, or unrelated process termination. RTK gain was inspected; global measured savings were 81.3K tokens (1.9%). Exact validation logs used RTK proxy passthrough.

Candidate audit: the post-run manifest includes a concurrent ComicPortraitRuntimeValidation edit and a new M05 test entrypoint appended after Steel Push ended. Menu/runtime sources did not change during the successful run; the complete shared tree was not frozen for release acceptance.

M05 candidate audit: candidate-before-m05-input-01.json and candidate-after-m05-input-01.json have identical hashes for every recorded source/config file. A subsequent menu-only name-capacity correction is separately verified by the final native check; the mission pass belongs to the recorded M05 candidate, not a packaged release.

## 2026-09-29 panel/icon correction

Credits containment and native Store/Armory emblems are implemented and validated. Full ARIA and six commander comic scene panels await visual-direction review. See [revision evidence](Panel_Icon_Revision_2026-09-29.md).
