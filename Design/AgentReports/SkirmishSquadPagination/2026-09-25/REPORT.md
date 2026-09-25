# Squad pagination implementation evidence — 2026-09-25

Source checkout: `2bea6e6f1` plus the uncommitted changes in this task. Mission: the fifth visible playable Skirmish tile, catalog `S003`, Regular / Standard, seed `104732` (`Desert Base · Base Assault · Air Mobile · Field`). The mockup's 12 squads and objective text are illustrative; S003 starts with five actual groups.

## Visual direction and native review

The user approved `Design/ImplementationPlans/SquadPagination/approved-direction.png`. After feedback, the implemented pager is a single 72-reference-unit strip directly above five cards. Its range and selected-count lines are centered, with the latter at 24 reference units for phone readability. The minimap remains anchored on the left. The card order and localized controls reverse in Persian. One-page missions hide the page arrows. The selected-state fixture uses a direct group-selection request solely to review the visual state.

Native captures self-reviewed:

| Locale | 1280×720 | 1920×1080 | 2400×1080 |
| --- | --- | --- | --- |
| English | [Opening](opening-en-1280-final.png) | [Opening](opening-en-1920-final.png) | [Opening](opening-en-2400-final.png) |
| Persian | [Selected fixture, final subtitle](selected-fa-1280-final-03.png) | [Opening](opening-fa-1920-final.png) | [Opening](opening-fa-2400-final.png) |

The final 1280 Persian capture shows the selection subtitle, Clear, and card check. S003 terrain in these captures contains the desert base, runway, aircraft, roads, and buildings. The earlier flat brown view was an incomplete capture during startup and is not representative of the loaded mission. The minimap's logged parent is `HeaderContent` with left anchors; at 1280 its screen corners begin at x=11.48.

These captures cover opening and one selected state. Native middle/final page, partial/zero groups, mixed on/off-page selection, ARIA running/stopped, and modal/command-mode states from the specification's full capture matrix are still pending.

## Automated checks

- Prefab builder: [pass marker](prefab-build-final-03.log), `[MatchHudV3PrefabBuilder] result=Passed`.
- EditMode suites: [visual](visual-final.xml) 10/10, [army](army-final.xml) 12/12, [ARIA](aria-final-03.xml) 22/22, [harness](harness-final-03.xml) 31/31.
- Existing planner validation: [101 cases passed](aria-plan-01.log).
- [Scenario/menu validation](scenario-final.log): 29 cases passed, including the playable S003 scenario-index-5 mapping and menu library prefab.
- English/Persian localization catalog imported through the checked Unity wrapper; [pass marker](localization-import-03.log).

The final 24-unit subtitle adjustment and selected-check rotation were compiled and verified by the prefab build and native selected-state capture after the focused suites. The focused suites ran before those two presentation-only adjustments.

## Normal-input ARIA completion

The S003 automated runs entered the mission through its accepted launch payload and used the supported normal touch/control path at normal simulation speed. They did not inject a result. Both reached the frozen victory result and completed the `MAIN MENU` return through touch input:

| Locale | Log | Touch gestures | Result | Counted |
| --- | --- | ---: | --- | --- |
| English | [full run](s003-aria-full-03.log) | 58/58, 0 unexpected, 0 interventions | result and main-menu return passed | yes |
| Persian | [full run](s003-aria-fa-final-01.log) | 75/75, 0 unexpected, 0 interventions | result and main-menu return passed | yes |

The Persian run used the final narrow pager and left minimap. The last subtitle-size adjustment does not change mission or ARIA logic. The UI shows S003's actual `BASE ASSAULT` objective; no mission objective or terrain content was replaced to imitate the mockup.

The specification's entire human-style flow remains unverified: tapping the fifth menu tile, manually selecting the fifth group, paging both directions after production, cross-page order and Clear, ARIA Stop/handback, and Campaign/legacy entry/return. The harness launches the selected S003 payload instead of tapping its visible menu tile. A real player/device acceptance run is also pending.

## Failed evidence retained

Earlier failed implementation and validation logs are retained in this folder. In particular, `prefab-build-final.log` used a nonexistent `BuildAndValidate` entry point; `prefab-build-final-02.log` and `prefab-build-final-03.log` used `Build` and passed. `selected-fa-1280-final.log` omitted the fixture's `WARLINE_PAGINATION_OPENING_ONLY=1` flag and timed out; `selected-fa-1280-final-02.log` and `selected-fa-1280-final-03.log` captured successfully. The first full ARIA failures and their traces remain available alongside the counted wins.

## Acceptance status

- Visual direction: approved mockup; final native captures reviewed locally, with user review of the latest native revision pending.
- Automated checks: focused suites and prefab/localization validation passed; presentation-only changes after the suites were verified by build and native capture.
- Complete normal-input playthroughs: ARIA completed and returned twice through normal touch controls; the full manual menu/paging/Stop flow and full visual matrix are pending.
- Real player/device acceptance: pending.
