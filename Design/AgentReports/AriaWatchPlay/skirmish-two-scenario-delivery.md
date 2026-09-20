# Skirmish 1 and 2 delivery

Owner request, 2026-09-20: finish the current Base Assault and its Watch ARIA behavior, then add a second selectable Base Assault scenario with different battlefield layout and approach routes. Frontline Control and the hundreds-of-units expansion are not this second scenario.

## Delivery order

1. Finish the current scenario's autonomous English/Farsi baseline wins, visible hand/selection, Stop/restart, terminal cancellation and shared command checks. Retain failed-run evidence. No hidden gameplay commands or balance concessions for ARIA.
2. Author a separate scenario identity, deployment layout and battlefield window using the existing desert environment. Validate all starting structure footprints, staging ground, production exits, connected direct/flanking routes and camera/minimap bounds. Preserve campaign and Skirmish 1 assets.
3. Add two large, localized scenario choices in Skirmish setup. Show each name, battlefield preview, rules and selected state. Persist the selection through deployment, replay, adjust setup and save migration; existing saves default to scenario 1.
4. Resolve the selected map and preset through the launch boundary. Keep normal base destruction, time limit, economy, recruitment, combat and delivery rules shared. Remove assumptions that every Skirmish launch has the first map's identity or anchors.
5. Run player-control smoke checks and full touch-only ARIA matches in both languages on scenario 2. Recheck scenario 1 after the shared launch/setup changes. Verify Stop/handback and terminal UI, then document exact results and remaining release-only gates.

## Evidence and scope

The Editor baseline is separate from the roadmap's multi-seed statistical certification, physical Android performance/touch testing and unfamiliar-player teaching study. None may be marked passed without evidence. The requested two scenarios must be playable and independently selectable, and ARIA must demonstrate actual wins in both; working gesture traces alone are insufficient.

Status: scenario 1 English/Farsi Editor baseline victories and focused checks passed. Scenario 2 runtime/configuration/chooser authoring implemented; terrain, player flow and autonomous validation in progress.

## Scenario 2 authoring candidate

The read-only surface survey shows the current scenario approaching the city from west/east. Evaluate a north/south approach for scenario 2: staging north of the city and an opposing base south of the highway. This provides a central urban route and routes around the city's western/eastern edges, rather than merely swapping the existing bases. Candidate world window: x=900–1300, z=260–875. These are survey bounds, not accepted deployment coordinates; footprint, road/sidewalk overlap, delivery clearance, connected navigation and visual inspection must pass before authoring is accepted.

Implementation boundaries:

- Add a stable scenario choice to `QuickGameConfig` and its UI/save projection; normalization preserves the supported choice and maps unknown/old values to scenario 1.
- Resolve preset, mission/scenario/map identity together from the queued launch snapshot. Every startup, resource, production, combat and layout-audit preset lookup must use that selection rather than the global scenario-1 resource constant.
- Create separate initial-force, construction and logical operation-map assets through Editor APIs. Reuse the accepted physical scene and shared combat rules.
- Add two localized, clearly selected setup cards; keep the existing launch, replay and adjust-setup flow.
- Validate the actual pending-touch sequence as well as completed actions. A plan must retain a map-opening target until the popup is visible; an instantaneous unit-test transition previously missed cancellation during the hand approach.

## Scenario 2 placement validation (2026-09-20)

The initial north/south candidate failed closed: nine sites intersected map reservations. The revised layout then exposed three sidewalk overlaps; the road and sidewalk masks are separate. Final live layout audit passed all ten exact authored origins, with zero road, sidewalk or water cells. Player staging is (1020,750), opposing staging (1100,400). The first autonomous run subsequently stopped at the opening tower preview: a camera transition hid a pending touch site beneath the HUD. This run is a failure, not a win. The planner now re-observes site reachability and switches to another visible site, normal valid confirmation, or Cancel; three focused regression cases cover the transition. Autonomous rerun is pending.

## Further QA findings (2026-09-20)

The opening construction recovery passed a subsequent real match, but the second assault later blocked after repeatedly focusing an already-visible enemy base. The approach observer depended on friendly contacts still being on the local minimap; camera movement could remove those contacts. It now uses the two public base objectives for an approach when no friendly contact is displayed, with a planner fallback to the visible base when no ground approach is reachable. A focused regression covers that fallback. Group observation now prioritizes a tighter visible contact cluster to avoid pulling the selection rectangle toward base logistics. These changes require fresh full-match wins; they are not yet accepted on gesture traces alone.

The tactical map title now follows the selected scenario. The English/Farsi setup chooser was visually reviewed, exposing and fixing live locale refresh for the new labels. The current focused checks pass: 82 planner, 14 decision, 18 touch-input and 11 scenario configuration checks. A previous fixed test-count string was replaced with an executed-check counter.

The current computer-control tool returns `windowNotFoundAtPosition` on Unity coordinate clicks, although screenshots and accessibility menu actions work. API-started QA was rejected by automatic approval review because it skips the visible consent confirmation. Explicit test-only approval is pending; do not treat this as permission to bypass the player confirmation or to mark gameplay validation complete. The Editor was returned to Edit mode while that approval is pending.
