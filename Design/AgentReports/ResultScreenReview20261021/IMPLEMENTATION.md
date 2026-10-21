# Shared result screen implementation

Implemented locally on `codex/result-screen-design-review`, based on main `16ecb2c70f894180d40141e71e929bd714d30393`. No result-screen changes are published or merged.

## Rendering and behavior

All three modes use `Assets/Game/Resources/UI/SharedMissionResult.prefab`. Campaign nests this exact prefab; Skirmish and Operations mount it through the existing result adapter. Native validation checks the prefab identity. The shared asset owns layout, typography, objective rows, status symbols, stars, artwork viewport, performance/report module and footer styling. Existing mode projections and callbacks still own rewards, settlement, debrief, retry, replay and return permissions.

The implementation follows the approved three-column design: existing Campaign comics or mission previews on the left, independent objective facts in the middle, and performance plus actual reward/medal/report data on the right. Unearned stars remain neutral. Completed goals stay green after a defeat. Save failures retain the recovery action. Campaign's chapter and replay buttons remain conditional on existing flow permissions; this change does not invent exits that skip mandatory debrief or settlement.

Native QA corrected missing symbol renderers, the earned-star adapter, long operation titles, heading localization, Persian line boxes and right margins, ambiguous Operations status translations, and callback targets after the shared prefab migration. Live Skirmish exposed a long catalog title overlapping its summary and the underlying HUD showing beside the 16:9 board; the final asset separates the text and covers the complete parent behind safe-area content. Scrollbars use the menu cyan and hide when text fits. Stars now use five-point native geometry, a subtle gold gradient and neutral hollow outlines matching the approved mockups. Reduced-motion reveal behavior is retained. The short Campaign outcome reason is localized before sentence extraction. Credits-only replay awards show only their actual credit icon.

The artwork catalog reuses 25 existing Campaign comics, six existing Skirmish previews and the existing O001 Operations illustration. It does not generate physical maps or new rewards.

## Validation gates

| Gate | Result | Scope |
| --- | --- | --- |
| Native visual/layout and callbacks | Passed: 4,185 checks, 30 renders | Three modes × two outcomes, English/Persian, 1920×1080, 1280×720, 2400×1080 and 1440×1080; border separation, right insets, text bounds, action routing/settlement, full-parent backdrop and actual long catalog title |
| Campaign adapter catalog | Passed: 100 cases | 25 mission adapters × both outcomes × both languages, using representative models; no gameplay readiness claim |
| Focused result regressions | Passed: six suites | Stars, M01 result/progression/pointer targets, M02 result/debrief and settlement, native terminal observation, medal and Battlefield return bindings |
| S002 live ARIA and result return | Passed | Normal menu/ARIA touch controls, real victory at 230.095 seconds, actual result touch to Main Menu; no outcome injection or physical interventions. Separate UI-return lane preserves the original saga-medal victory requirements |
| Final Campaign normal-input journey | Passed | Bilingual M01 touch journey, actual victory/result/return, including settled stars; 26 menu gestures, no injected outcomes, wrapper exit 0 |
| O001 live ARIA/result/return | Failed before result | Existing startup navigation refusal: objective blocked at `int2(1860, 710)`; see [Operations load gate](Implementation/OPERATIONS_LOAD_GATE.md) |
| Broader legacy Campaign menu regression | Failed; retained | Old hidden chapter-tab Icon assertion at `M02EstablishBaseCampaignUiTests.cs:266`, outside the result renderer |
| Broader repository architecture audit | Failed; retained | 30 helper paths require manifest authorizations; all 30 are byte-identical to the base commit. Existing guard remains unchanged. See [baseline proof](Implementation/ARCHITECTURE_BASELINE_FAILURE.json) |
| Real player/device acceptance | Pending | These agent runs and native fixtures do not replace human/device acceptance |

## Evidence and scope

The final comparison-only localization check first inspected retained English source strings rather than rendered Persian. That failed fixture run is preserved; the corrected check validates the displayed text. The runtime prefab and normal-input-tested renderer did not change during this cleanup.


[Comparison gallery](REVIEW.html) · [Approved design review](DESIGN_REVIEW.md) · [Candidate hashes](Implementation/CANDIDATE.json)

`Implementation/FinalNative` contains the final 30 representative native renders. `Implementation/Live` contains screenshots from actual normal-input outcomes and returns. Full logs, earlier native attempts and failed evidence are retained in the validation archive. The original failed saga-medal run observed defeat; it is not presented as a win. The next attempt reached victory but its old action assumption targeted Main Menu while expecting setup; its failure is retained, and the final UI-return run resolves the available action by the real outcome.

Test builders regenerated unrelated HUD, localization and narrative assets. Exact copies, binary diffs and hashes were backed up before returning only those previously clean files to the packaged baseline. A cleanup auto-review initially rejected an unbacked restore; reversible backups resolved the risk. Neither preexisting dirty font asset is included. No mission geometry, terrain, source scenes, objectives, win predicates, budgets or progression rules are changed by this UI work.
