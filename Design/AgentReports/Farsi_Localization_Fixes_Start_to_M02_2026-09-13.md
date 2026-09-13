# Farsi localization fixes: start through M2

Date: 2026-09-13. Scope: all 86 entries in the [original audit](Farsi_Localization_Audit_Start_to_M02_2026-09-13.md), including conditional errors and defeat/replay cases. The original audit remains unchanged as the before-state record.

## Delivered

- 125 reviewed key/source/translation records in [StartThroughM02UiStrings.json](../../Assets/Game/Configs/Localization/StartThroughM02UiStrings.json), imported into the shared runtime catalog. The catalog has 1,997 English and 1,997 Farsi keys; existing source aliases receive the same reviewed translations.
- First-launch explicit narrative bindings expanded from 18 to 57 targets. Names, roles, profile copy, guidance descriptions, support levels, toggles, and Previous controls now use catalog data. Existing identities are transliterated, not renamed.
- Dynamic writes use the shared localization binding for loading, campaign/briefing copy, build cards/details/instructions, production queue names, selected buildings, ARIA ownership, and results. Building/unit names are localized before insertion into translated messages.
- Loading keeps compact ECS status data intact and localizes the visible label. Raw map-unload error details remain diagnostic data; the player receives a localized retry message.
- ARIA initializes its shared header in the active locale before its first read model, and its interaction copy/step labels resolve through the catalog instead of branching on a legacy English/Farsi flag.
- Result objective/status columns have a visible gap for Farsi labels, preserved by the prefab builder and responsive baseline data.
- Config import participates in full catalog regeneration. Build validation rejects incomplete locale tables, missing/duplicate/unknown keys, mismatched format argument indexes, stale config imports, and unbound static first-launch labels.

## Verification

- Focused localization suite: **24 tests passed**, including runtime writers, all configured source/key aliases, loading phases, errors, dynamic metadata, initial ARIA state, production queue names, and a synthetic German locale. The synthetic incomplete locale correctly fails validation.
- Shared V3 validation: **38 prefabs, 1,242 bindings**, English/Farsi completeness, and Farsi font coverage passed.
- M3 ARIA regression check: **48 combinations** (12 lessons × 2 languages × 2 text sizes) passed without clipping or missing glyphs.
- First-launch identity/guidance: actual prefabs rendered and visually checked in Farsi.
- Live Editor flow: main menu, commander profile, settings, chapter selection, M1/M2 mission selection and briefings, M1 through victory.
- Live M2: valid building placement and four explicit ARIA recruitment clicks (Build → Soldiers → rifle → Produce), with actual soldier delivery. A separate run completed M2 through its debrief/victory result.
- M1 defeat, M2 defeat, and M2 replay: actual result prefab rendered with controlled outcome models. These are presentation checks, not claims of additional live defeats/replays.
- Final visible-text recordings for M2 and the result previews contain no remaining unlocalized English prose. Intentional brand text, IDs, numbers, and agreed acronyms remain.

Evidence is local only under `/private/tmp/warline-fa-fix/` and `/private/tmp/warline-m02-placement/`; no evidence images were added to Design. Wrapper logs include `/private/tmp/warline-fa-fix-closeout-tests.log`, `warline-fa-fix-menus.log`, `warline-fa-fix-m2.log`, and `warline-fa-fix-results.log`. GUI Unity can return zero after an execute-method exception, so the explicit pass markers were checked. Earlier test-harness compile/lifecycle issues were corrected before the passing runs.

Editor-only validation, as requested. Translation completeness and wiring checks do not replace linguistic/layout review for a newly authored language. See the [UI localization workflow](../../Documentation/Localization/UI_LOCALIZATION_WORKFLOW.md).

## Audit disposition

All rows below are implemented. Evidence describes the validation mode, rather than implying every rare branch was triggered in live gameplay.

| Audit ID | Screen / element | Resolution and verification |
|---|---|---|
| FI-01 | First launch — commander identity — Instruction | Shared config + explicit narrative binding; Farsi prefab preview. |
| FI-02 | First launch — commander identity — Portrait role labels and selected-profile role | Shared config + explicit narrative binding; Farsi prefab preview. |
| FI-03 | First launch — commander identity — Callsign rules | Shared config + explicit narrative binding; Farsi prefab preview. |
| FI-04 | First launch — commander identity — Selected-profile heading | Shared config + explicit narrative binding; Farsi prefab preview. |
| FI-05 | First launch — commander identity — Selected-profile description | Shared config + explicit narrative binding; Farsi prefab preview. |
| FI-06 | First launch — commander identity — Previous button | Shared config + explicit narrative binding; Farsi prefab preview. |
| FI-07 | First launch — commander identity — Portrait names and selected-profile name | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-01 | First launch — ARIA guidance choice — Eyebrow | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-02 | First launch — ARIA guidance choice — Full guidance description | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-03 | First launch — ARIA guidance choice — Tactical hints description | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-04 | First launch — ARIA guidance choice — Minimal guidance description | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-05 | First launch — ARIA guidance choice — Three support-level headings | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-06 | First launch — ARIA guidance choice — Support-level values | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-07 | First launch — ARIA guidance choice — Subtitle setting label | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-08 | First launch — ARIA guidance choice — Subtitle setting explanation | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-09 | First launch — ARIA guidance choice — Reduced-motion setting label | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-10 | First launch — ARIA guidance choice — Reduced-motion explanation | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-11 | First launch — ARIA guidance choice — Both setting toggles | Shared config + explicit narrative binding; Farsi prefab preview. |
| FG-12 | First launch — ARIA guidance choice — Previous button | Shared config + explicit narrative binding; Farsi prefab preview. |
| LD-01 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-02 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-03 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-04 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-05 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-06 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-07 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-08 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-09 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-10 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-11 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-12 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-13 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-14 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-15 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-16 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-17 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-18 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-19 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-20 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-21 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-22 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-23 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-24 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-25 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-26 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| LD-27 | Splash / loading / return transition — Progress status | Shared loading source/key mapping; every configured phase and error presentation exercised by component tests. |
| MN-01 | Commander profile — Commander subtitle | Shared config + localized presentation; live menu/briefing pass. |
| CH-01 | Campaign — chapter selection — Character labels | Shared config + localized presentation; live menu/briefing pass. |
| CH-02 | Campaign — chapter selection — ARIA protocol heading | Shared config + localized presentation; live menu/briefing pass. |
| MS1-01 | Campaign — M1 selected — Summary | Shared config + localized presentation; live menu/briefing pass. |
| MS1-02 | Campaign — M1 selected — Reward strip | Shared config + localized presentation; live menu/briefing pass. |
| MS1-03 | Campaign — M1 selected — Objective card | Shared config + localized presentation; live menu/briefing pass. |
| MS1-04 | Campaign — M1 selected — Objective card | Shared config + localized presentation; live menu/briefing pass. |
| MS2-01 | Campaign — M2 selected — Summary | Shared config + localized presentation; live menu/briefing pass. |
| MS2-02 | Campaign — M2 selected — Reward strip | Shared config + localized presentation; live menu/briefing pass. |
| MB1-01 | M1 mission briefing — Mission summary | Shared config + localized presentation; live menu/briefing pass. |
| MB1-02 | M1 mission briefing — Protect-squad objective | Shared config + localized presentation; live menu/briefing pass. |
| MB2-01 | M2 mission briefing — Mission summary | Shared config + localized presentation; live menu/briefing pass. |
| AH-01 | M1 / M2 match HUD — ARIA — Ownership / preview state | Shared ARIA source/step mapping and explicit writes; runtime/initial-state tests and live M1/M2. |
| AH-02 | M1 / M2 match HUD — ARIA — Ownership / preview state | Shared ARIA source/step mapping and explicit writes; runtime/initial-state tests and live M1/M2. |
| AH-03 | M1 / M2 match HUD — ARIA — Ownership / preview state | Shared ARIA source/step mapping and explicit writes; runtime/initial-state tests and live M1/M2. |
| BU-01 | M2 Build popup — Building list title and selected-item title | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-02 | M2 Build popup — Building role subtitle | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-03 | M2 Build popup — Build instruction / placement-request message | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-04 | M2 Build popup — Empty selection requirements | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-05 | M2 Build popup — Soldier primary action button | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-06 | M2 Build popup — Soldier instruction and recruitment confirmation | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-07 | M2 Build popup — Not-enough-materials instruction | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-08 | M2 Build popup — conditional errors — Request/requirements failure | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-09 | M2 Build popup — conditional errors — Request/requirements failure | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-10 | M2 Build popup — conditional errors — Request/requirements failure | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| BU-11 | M2 Build popup — conditional errors — Request/requirements failure | Shared metadata/message/error configs and explicit writes; live build/recruitment and conditional-error tests. |
| PL-01 | M2 building placement — Building title | Localized metadata and parsed placement status; live placement/selection plus valid/blocked status tests. |
| PL-02 | M2 building placement — Valid-location status | Localized metadata and parsed placement status; live placement/selection plus valid/blocked status tests. |
| PL-03 | M2 building placement — Invalid-location status | Localized metadata and parsed placement status; live placement/selection plus valid/blocked status tests. |
| SE-01 | M2 selected-building panel — Selection title | Localized metadata and parsed placement status; live placement/selection plus valid/blocked status tests. |
| RE1-01 | M1 victory result — Outcome sentence | Translated summary through result binding; live M1 victory. |
| RE2-01 | M2 victory result — Mission identity heading | Shared result/reward config and localized identity parts; live M2 victory or replay prefab preview. |
| RE2-02 | M2 victory result — First-clear outcome paragraph | Shared result/reward config and localized identity parts; live M2 victory or replay prefab preview. |
| RE2-03 | M2 victory result — Unlock reward | Shared result/reward config and localized identity parts; live M2 victory or replay prefab preview. |
| RE2-04 | M2 replay victory result — Replay outcome | Shared result/reward config and localized identity parts; live M2 victory or replay prefab preview. |
| RF-01 | M1 / M2 defeat result — Mission status | Shared loss/status config and explicit result binding; M1/M2 loss prefab previews. |
| RF-02 | M1 / M2 defeat result — Objective status labels | Shared loss/status config and explicit result binding; M1/M2 loss prefab previews. |
| RF-03 | M1 / M2 defeat result — Civilian status | Shared loss/status config and explicit result binding; M1/M2 loss prefab previews. |
| RF-04 | M1 defeat result — Outcome sentence | Shared loss/status config and explicit result binding; M1/M2 loss prefab previews. |
| RF-05 | M2 defeat result — Outcome sentence | Shared loss/status config and explicit result binding; M1/M2 loss prefab previews. |

## Additional findings resolved during the fix pass

- Both short “Insufficient credits.” key variants now resolve directly through the catalog.
- Initial `TUTORIAL 1/3` / `STEP 1/1` header flashes use the active locale immediately.
- Styled first-step preview instructions have explicit catalog entries.
- The active production queue updates its authored placeholder to the localized queued unit name.
- Farsi objective/status labels have separate space on victory and defeat screens.
