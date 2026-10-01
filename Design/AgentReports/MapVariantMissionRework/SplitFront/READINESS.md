# Split Front — ready for human Editor review

## Native normal-input journeys

| Journey | Result | Evidence |
|---|---|---|
| English ARIA | Passed: direct Attack, actual launcher fire and combat, all five hostile defeats, intact core, eight comic panels, result/settlement/Campaign return; 75,063 active ms | `Logs/split-front-review-watch-01.log.gz`, `Evidence/20261001-190136-split-front-en` |
| Persian independent manual | Passed with ARIA off: ordinary touch Select and Attack, same native mission graph and return; 75,179 active ms | `Logs/split-front-review-manual-01.log.gz`, `Evidence/20261001-190633-split-front-fa-IR` |
| English optional Smoke / Hold, then ARIA | Passed: native production-context Smoke, exact costs/expiry, pre-launch Hold, full win and return; 72,451 active ms | `Logs/split-front-review-smoke-03.log.gz`, `Evidence/20261001-192234-split-front-en-smoke` |

Neither journey injected unit positions, tactical orders, health or mission
outcomes. An isolated prior-clear profile qualified the ordinary unlock path.
Precision Strike first-clear settlement passed; duplicate settlement was
rejected. The wrapper exited 0 for all three completed journeys.

## Automated and native visual checks

The final preparation asserts all 1,070 depot/barracks clearance-envelope cells are
unblocked, allow BuildingPlacement and exclude Road/Reserved surfaces. It also
passed 2,387 seven-cell road clearance samples, the exact unchanged Refinery
source, launcher/range/protection/progression rules, support context and media.
See `Logs/split-front-refinery-prep-02.log.gz`. Its initial review-launcher
compile error was corrected during GUI import before the wrapper passed; the
full log preserves that failure. Earlier draft preparation is superseded.

Inspected native English launcher HUD/result and Persian manual HUD/result.
They retain the ordinary colorful controls, approved selection markers and
visible ARIA Play / Stop. No custom launcher confirmation panel exists. Eight
comic panels completed in each language; this checkout uses captioned content
(voice assets are absent), not newly qualified voice playback.

The final optional path exercised the corrected Support context: ground
(535,575)–(945,685), a protected civilian box (689,650)–(725,685), and revision 2.
The original 18 m protection box is intersected with the playable area; positions
beyond that edge already reject as outside the mission. Preview spent zero;
cast spent one Fuel and one charge, preserving the civilian floor of 40.
Coverage (650 permille) and expiry were observed. Native Hold cleared the
command before launch; subsequent normal Attack fired and won. Neither smoke
nor a custom launcher confirmation control is required for the ordinary win.

Inspected the native Smoke/Support, preparing/held launcher and result screens.
The first Smoke run retained a failed Editor-frame raycast harness; the second
exposed legacy Support targeting bounds. Both full failures are retained.
The final English path includes the corrected context. The no-Smoke English
and Persian paths preceded that correction; their gameplay rules and layout
were unchanged.

## Review and remaining release gates

Human map/camera/minimap and player acceptance, device content/performance,
voice approval, saved-state/resume compatibility and dedicated native defeat,
replay and access negative journeys remain pending. Focused rules and the
completed native wins are reported separately from those release gates.
