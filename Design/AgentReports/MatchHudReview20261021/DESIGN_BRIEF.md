# Match HUD visual direction — 21 October 2026

Status: owner approved all six proposals; implementation complete; owner visual review and physical-device acceptance pending on `codex/match-hud-approved-polish`.

## Actual game baseline

Fresh native Campaign M02 and M04 captures were made through the repository macOS GUI wrapper in the isolated menu worktree. The HUD prefab, feedback presentation source and Exchange prefab blob IDs match the primary checkout; identities are recorded in REVIEW_MANIFEST.json. Existing older target mockups were inspected for history and excluded as authoritative baselines.

The first capture attempt was obstructed by a first-launch language overlay and is excluded. The completed-profile capture fixed only its isolated first-launch save. Three passenger capture attempts were incomplete (legacy walkthrough lacked normal selection, then defeat, then an offscreen world target after commands were captured); their logs remain under Logs/hud-current-review-20261021-*.log. This is presentation evidence, not a mission-readiness or device-acceptance claim.

## Findings in current native screens

- Normal feedback presentation uses 18 reference pixels; errors use 24. Propose a padded command-width ribbon, stronger hierarchy and approximately 26–28 reference-pixel instruction text, checked later at mobile ratios and in Persian.
- Minimap zoom controls retain older gold circular chrome. Use the same navy square/silver glyph/cyan accent family as current controls.
- Build before selection shows BUILD {0}, a blank detail pane and weak disabled text. Resolve player-facing labels, keep disabled label/icon/fill/border coherent and make the reason readable.
- Placement Barracks uses a power-plant thumbnail. Bind the actual selected building artwork. Valid and blocked states need distinct icon, text and fully grey disabled confirmation.
- Exchange is the current five-route screen (not old export/import mockups). Native Insufficient Oil currently accompanies a green Start Exchange surface. Use disabled slate styling and show required/available resource totals. Preserve rates, queue capacity and resource gates.
- Pause and Settings already have useful structure. Refine padding, border weight, centered icons, slider thumbs, tab selection and action emphasis; keep their actual controls. Pause values and Skip to Victory behavior need an explicit binding/behavior audit before any implementation; sample mockup values are illustrative, not runtime measurements.

## ImageGen prompt briefs

Tool: built-in image_gen. Outputs are review mockups, not screenshots or imported game assets.

1. HUD: preserve actual native layout and seven colorful commands; enlarge/pad feedback above commands; replace legacy zoom with blue squares and simple silver +/−; keep ARIA Play/Stop visible, correct selected unit role and existing portraits. OWNER CORRECTION: the squad tray itself scrolls; remove the extra paging strip and do not add navigation controls above squads. No A-10 or new gameplay.
2. Build/placement: reference actual M02 drawer and native confirmation; compact single-offer layout without black filler; preserve real Barracks cost90/time00:30/footprint28×15; correct thumbnail; show green valid and fully grey blocked Place Building states, clear reason, consistent Rotate/Cancel.
3. Exchange: preserve the current five recipe routes and exact rates; clearer spend/receive/amount/duration; grey Start Exchange when insufficient; preserve queue0/2 and native resource icons. No old export/import scheme.
4. Pause/settings: preserve current controls and typography; quieter secondary restart, green Resume, blue Options/Help, red exit, consistent borders and centered close/gear; improve audio sliders and toggles. Existing Skip to Victory remains explicitly labeled in the proposal, with behavior subject to review.
5. Commands: the actual current APC wheel has four sectors (Return, Destroy, Camera, Board) plus separate Cease Fire. Preserve those actions; improve spacing and close affordance; fully grey disabled Destroy. Resolve the center portrait from the actual selected definition. No older six-sector layout.
6. Passengers: capture the actual drawer using its visible passenger chip after normal carrier selection; refine row padding, capacity and health hierarchy, action labels and disabled states. Do not fabricate occupancy in native evidence.

## Review gates

Visual direction: approved by owner (“I liked the mockups. can u update according to the mockups.”).
Implementation: native prefab authoring and rendered comparison complete. See Implementation/REVIEW.md.
Automated regression: 166 native-view checks passed in English/Persian. Failed attempts remain in the implementation evidence.
Normal-input flow: final-candidate M01 completed in English/Persian through 25 touch gestures, real combat, victory and Campaign return. Autonomous ARIA Play and loaded M04 transport completion are not claimed by these visual captures.
Real player/device acceptance: pending.

## Native passenger capture complete

The fourth bounded capture opened the selected carrier’s visible passenger chip after normal touch selection and captured the real 0/10 drawer. Exit All is actually non-interactable while still visually red; the native control receipt confirms this. The drawer overlaps the mission status strip and squeezes HP text. The loaded 4/10 proposal is explicitly illustrative and was not injected into the native game. Both final capture wrappers exited 0 with the Captured marker. No complete mission/device acceptance is claimed.
