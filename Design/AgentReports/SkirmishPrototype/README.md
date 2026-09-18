# Base Assault implementation evidence

Status: Base Assault is ready for internal Editor play in English and Farsi. Android and unfamiliar-player evaluation remain separate gates; the 8–12-minute pacing target is not established.

Final report: [completion decision](../../Roadmap/Skirmish_Prototype/COMPLETION.md) and [readiness evidence](Readiness/README.md). **309 checks pass**, six normal-speed tactical journeys cover both languages, ten repeated skirmish UI sessions and all ten bilingual campaign entry checks pass. Real supply recovery and terminal-state freeze were verified. Historical counts and balance observations below are retained as development history, not the current sign-off.

Plan: [prototype plan](../../Roadmap/Skirmish_Prototype/PLAN.md).

## Observed baseline — 2026-09-17

Launched the actual SCN13 setup command in the wrapper-owned isolated Editor at `/private/tmp/warline-readiness-validation`, from baseline `674b406d3`.

- Setup offers five presets, several difficulties, fog, intel and victory choices despite a single generic launch route.
- Configuration store retained only the AI snapshot; seed and non-AI choices were lost. Menu startup did project the AI subset, but did not establish a complete skirmish session.
- Loaded identity `skirmish` / `scenario.skirmish.desert_base_standard`.
- Live faction query after startup: neutral 4,735 health entities (4,734 buildings), player 264 (243 buildings), enemy 6 (all buildings). No active enemy combat units were present at the observation. This is not the planned bounded force.
- Default initial-unit config had no authored factions, 120 player Materials and 655 AI Materials. Map ownership supplied the apparent player force.
- HUD showed an empty tutorial panel and broad unit categories; it did not explain a Base Assault objective or time limit.
- Grid is 2048 × 1024, cell size 1, origin (0,0,0).
- Live navigation/road/blocker sampling found clear 36 × 24 ground footprints at (820,460) and (1180,460). These are candidate Main Base positions; final spawn, full supply footprints and both routes still require gameplay validation.

[English baseline capture](Baseline/en-launch.png).

## Foundation checks

Connected-Editor validation on 2026-09-17: `SkirmishSessionTests` — 9 passed. Checked seed/result persistence without campaign writes, legacy setup migration, duplicate Deploy ownership, and explicit skirmish map identity. Assets generated through `SkirmishPrototypeBuilder.Rebuild()` in the isolated Editor.

## Working balance (not yet accepted)

Both sides: 8 rifle soldiers in two authored batches, 1 light armored car, 2 tray trucks and 1 tanker; main Barracks, fabrication depot, oil pump, refinery, fuel storage. 220 Materials, capacity 600, 160 usable Fuel seed, 0 tactical Credits. Existing fabrication definition consumes 4 Oil for 20 Materials every 30 seconds. Supply throughput, refinery timing, actual roster grouping and sustainable replacement rates remain under validation.

Do not describe the prototype as complete until the acceptance matrix and normal-speed bilingual journeys are recorded.

## ARIA stability correction — 2026-09-17

The campaign Show Me availability updater re-enabled the action after the skirmish read model hid it, changing the measured rail height during successive HUD updates. Skirmish now owns a persistent briefing and does not enter campaign tutorial scheduling or expose its inactive actions. Accessibility and content updates use the same expanded layout choice.

- `SkirmishAriaStability`: passed with exact locale assertions for `en` and `fa-IR`; repeated model, highlight and accessibility updates preserve visibility, text and geometry.
- Live English: 600 Canvas render callbacks produced one state: height 364, briefing visible, Show Me hidden. Live squad selection and screen-position Move accepted and soldiers reached the destination.
- Inspected [English](AriaStability/aria-fixed-en.png) and [Farsi](AriaStability/aria-fixed-fa.png) captures. Farsi expands to fit its text and remains above the minimap. These captures verify the panel; the close base camera and surrounding vegetation still need gameplay review.
- The validation Editor's original two-hour wrapper expired during broader checks; restarted through the same repository wrapper. No user Editor was interrupted.

Additional current evidence: roster mapping regression passed; supply trucks delivered Oil and fabrication increased Materials from 220 to 320 during normal simulation. One earlier normal-speed 15-minute idle run drew at the deadline, but failed the intended AI pressure/pacing review. Real combat defeat, six final bilingual journeys, repeated lifecycle checks and the remaining acceptance matrix are still outstanding.

## Attack route validation

A live player Attack order previously stopped infantry against scenery around x=975. Skirmish now uses the existing interruptible path/breach order to approach a firing position before direct combat; automatic target acquisition does not overwrite that path. The focused firing-position regression passed. The new live English run used squad-card/Attack button hit tests and a screen-position enemy-base click. Soldiers traversed past x=1046, destroyed the enemy base and reached Victory at 147.406 seconds, with 0 player losses, 12 enemy unit losses and 1 enemy building lost. No health/position/resources/result facts were injected. [Result](victory-path-en.png).

This is a functional victory check, **not a balance pass**: 2:27 is below the planned 8–12-minute pacing range. Remaining work includes AI pressure/defeat, authored base sites (avoid road/vegetation overlap), production UI rejection feedback, startup recovery, final bilingual journeys, repeated replay cleanup and campaign regressions. All five setup/navigation tests now have explicit Passed markers.

Campaign guidance regression: `RunShowMeValidation` passed all 10 M1–M5 selection/command/destination/waiting cases after the ARIA changes.

## Ground sites and opponent follow-up — 2026-09-17

The user confirmed the starting Barracks overlapped the highway. A navigation-only check missed it because the compatibility `GridRoad` buffer was empty: authored operation-map roads and sidewalks are projected separately. The live survey now uses the same authored surface/mesh mask as placement.

- New player anchor: (835,610); enemy anchor: (1280,580). Main Barracks origins are (817,589) and (1265,559), both 28 × 15. Supply buildings are on separate ground sites alongside their base.
- [Player ground-site capture](PlacementAndOpponent/player-ground-site.png) shows the Barracks clear of the road. The initial candidate run relocated five buildings by a few cells; the final authored coordinates now match the accepted locations observed in that run.
- [Ground survey](PlacementAndOpponent/ground-survey.txt): all ten final footprints have zero authored road or sidewalk cells. This is a cross-check of the captured map, not a fresh final startup pass.
- Starting skirmish structures now require their exact authored origin. The runtime spawn boundary checks authored road/sidewalk coverage even before the Build drawer creates its cache. AI build anchors come from the faction preset rather than duplicated hardcoded coordinates.
- AI attack refresh previously removed an active skirmish approach path every two seconds. A matching approach now finishes. Target selection prioritizes the opposing Main Base, switches to combat threats near its own base, ignores nearby supply trucks as defensive threats, and keeps four initial rifle defenders out of field squads.
- Initial scenario-only combat values: rifle range 32, damage 6, cooldown 0.8s; armored car range 40, damage 18, cooldown 1.2s. Both sides and replacements receive identical values without changing health or shared campaign prefabs. Field orders wait 30 seconds unless the base is threatened. These are **working values awaiting normal-speed balance checks**, not a pacing pass.
- Build drawer preflight now distinguishes the infantry cap from the supply-vehicle cap. Both messages are in the EN/FA localization configuration and generated catalog.
- [Focused regressions](PlacementAndOpponent/focused-tests.txt): 12 passed, including immediate defensive reaction, ignoring closer logistics vehicles, returning to the main objective, equal replacement combat settings, no healing, and campaign policy isolation. Code compiles in Unity.

Validation environment: the wrapper-owned Editor’s two-hour session expired and its replacement waited in a scene-backup recovery dialog. The user then explicitly authorized temporary QA in the normal WarlineCapture Editor, and confirmed that it may be used for future development/testing without another permission request. QA uses `/private/tmp/skirmish-prototype-profile`; the prior scene/Edit-mode setup is recorded for restoration.

**Next acceptance work:** rerun the exact-origin layout audit after a fresh launch, inspect the enemy base, verify both supply chains and direct/flank traversal, evaluate the new AI in real victories/defeats, exercise cap feedback/cancel/refund, verify startup-failure recovery, then finish the six bilingual journeys and replay/campaign matrix. Do not mark S3–S5 complete yet.

### Fresh launch and gameplay findings

- [Final live layout audit](PlacementAndOpponent/final-layout.txt): **Passed, 10 buildings, 0 failures**. All ten exact preset origins matched; every actual footprint had zero road/sidewalk cells. [Player](PlacementAndOpponent/player-final.png) and [enemy](PlacementAndOpponent/enemy-final.png) captures inspected. This closes the reported starting Barracks/highway overlap for this layout.
- Actual enemy squads traversed the map and damaged the player Main Base to 834/1200. The player’s unattended defense defeated eight attackers with no player-unit loss in that run. This proves pressure reaches the base, but does not establish satisfactory balance or a real combat defeat.
- A replay exposed a late streamed `InitialUnitsSpawnConfig` with campaign defaults (10,000 Fuel) alongside the selected 160-Fuel config. A skirmish-only guard now suppresses unselected spawners before they can run, including later arrivals, while preserving their entity/rendering registry. A fresh run retained exactly one startup config and 152 Fuel after normal consumption; the 10,000-Fuel overwrite did not recur.
- Build originally showed the global campaign catalog and the objective strip over its content. Catalog metadata and request validation now use the preset (six buildings, rifle squad, two supply vehicles); the strip now also respects Build drawer visibility. Final popup capture remains pending.
- A real Recruit click produced four soldiers and closed the drawer. Population accounting now groups all soldier variants and excludes scenery/buildings; unsupported production cannot bypass the preset through a stale catalog item.
- The inherited AI target was only three produced soldiers. Working tuning now targets 16 reinforcement infantry, subject to the shared 24-infantry cap and actual resources, with first dispatch at 30 seconds. These values require new gameplay pacing evidence.
- Startup failure is latched separately from match outcome and has EN/FA Retry/Setup/Menu presentation. Blocked placement, content failure and a 120-second preparation timeout stop the partial match; no result is saved. Failure-path return/retry still needs a live check.
- Expanded focused regressions: 16 passed, including mixed-variant population accounting, preset/catalog isolation, late scene-spawner suppression and startup-failure isolation.

### Reinforcement lifecycle correction

The direct-pressure run was stopped at 602.723 seconds after confirming an empty AI squad still occupied one of the two formation slots. Actual losses were nine player units and eight enemy units; neither Main Base was destroyed. This is a failed opponent/pacing check, not a completed journey. Skirmish now removes dead squad members and retires empty squads before forming replacements, preserving surviving squads and campaign behavior. The expanded focused suite passes all 17 cases. Fresh live observation reached 537.335 seconds with six enemy squads formed, 22 enemy losses, zero player losses and player Main Base health 720/1200. Squad replenishment now continues, but the idle defense remains too strong; opponent balance is still an open acceptance failure.

The Build objective strip now stays hidden while the actual drawer is open (`IsOpen=true`, objective HUD inactive). The first Farsi capture exposed untranslated supply-building names and a stale English item argument after a live language switch. Added configured translations for all four supply buildings and the four-person rifle-squad purchase label, plus locale refresh of the instruction. Disabled catalog cards now display the actual authoritative failure instead of the prefab’s fixed Forward HQ placeholder. Final localized captures follow after compilation.

Working opponent follow-up: assemble eight-unit attack squads instead of dispatching each four-person recruitment batch immediately. This preserves four initial home defenders and uses the same paid recruitment and combat values as the player. It targets the observed piecemeal-attack failure; a fresh normal-speed outcome is still required before accepting these values.

Further live review identified three authored map cars arriving after the one-time scenery normalization. They appeared in the armor card despite the preset specifying one car. Scenery normalization now handles newly streamed objects throughout the match, and authored vehicles are excluded from squad assignment and loss tracking. The Farsi counterattack run was therefore **not accepted as a roster/balance journey** (stopped at 846.312 seconds, nine player and seven enemy losses, no outcome). Nearby-defender targeting now passes a dedicated regression. A combat capture also exposed attack-warning/objective overlap; the objective bar now moves below a visible warning.

### Stable checkpoint

- Latest focused suite: **24 passed** (9 session/rules, 10 combat/startup/roster, 5 affected Build checks). Unity compilation passed. The old recruitment test expected the popup to remain open; it now asserts the user-requested close-after-acceptance behavior.
- Fresh starting roster: eight player rifle soldiers, one armored car, one tanker and two cargo trucks. Enemy starting force matches, with a paid four-person recruitment batch already present at sampling. **Zero owned authored scenery vehicles**, still zero at 496.332 seconds.
- Fresh ten-building placement audit remains Passed, with exact preset origins and zero road/sidewalk cells.
- Farsi [Build](PlacementAndOpponent/build-fa.png) and [supply cap](PlacementAndOpponent/supply-cap-fa.png) captures inspected. Supply names, transport role, cap messages and item arguments are localized. Live FA → EN refreshed the open instruction. The English detailed cap sentence was shortened after its capture exposed clipping; the final configured catalog was regenerated, but that last shortened sentence has not received a new screenshot.
- Controlled warning geometry check: **12px separation** between warning and objective strip. This is a presentation check, not a completed combat journey.
- Last fresh run ended at 496.332 seconds for restoration: Playing, no outcome, 0 player and 7 enemy unit losses, Main Bases 912/1200 and 1200/1200. The AI remains too weak against an idle starting defense; balance is **not passed**. Investigate armored-car/infantry counterplay, stalled survivors and reinforcement grouping with full telemetry before further tuning.
- Original clean Match scene/Edit-mode setup restored. QA used its separate temporary progress directory.

Remaining acceptance: opponent balance and genuine EN/FA victory/defeat journeys, live startup failure return/retry, production cancellation/refund and supply-loss recovery, ten replay/exit cycles, and shared campaign regression checks. S5 remains open; no complete skirmish sign-off is claimed.
