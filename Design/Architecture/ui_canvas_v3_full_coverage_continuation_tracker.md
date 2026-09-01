# Canvas V3 Full-Coverage Continuation Tracker

Purpose:
Implement every canonical V3 screen/state in `Design/VisualLockLayered/V3_SCREEN_INVENTORY.md` as live Unity Canvas UI while preserving current runtime bindings, route semantics, and gameplay behavior.

This tracker supersedes any assumption that completion against the older gold Target Lock family also completes V3. The older work remains useful implementation infrastructure and interaction evidence, but V3 visual acceptance is counted separately.

Last updated: 2026-09-01

## Governing Sources

- Canonical screen/state list: `Design/VisualLockLayered/V3_SCREEN_INVENTORY.md`
- Canvas implementation rules: `Design/Architecture/ui_canvas_target_lock_art_direction_tracker.md`
- Canvas conversion workflow: `Design/Architecture/ui_canvas_target_lock_mockup_conversion_playbook.md`
- Screen-specific visual authority: each canonical `*_Final_Target.png` named by the V3 inventory

## Completion Contract

A row is complete only when all of the following are true:

1. The player-facing state is implemented as a live Canvas hierarchy; a single composited screenshot is not an implementation.
2. Existing bound object names, route actions, ECS/UI gateways, and gameplay behavior remain intact.
3. Default, highlighted, pressed, selected/current, focused, disabled, and locked states are implemented wherever the control family supports them.
4. A current capture is compared directly with the exact canonical final PNG.
5. 1280x720, 1920x1080, 2400x1080, and 4800x2160 layouts are readable, unclipped, and safe-area-correct where applicable.
6. The state has no new compile errors, runtime errors, runaway Canvas rebuilds, or unresolved visual P0-P2 findings.
7. Evidence, tracker status, code/prefab changes, and assets are committed and pushed as one clean dependency-ready slice.

## Snapshot

- Canonical V3 targets present: `46 / 46`
- V3 targets visually accepted in live Canvas: `1 / 46`
- Current live baselines captured against V3: `3 / 46` (`SCN-01 Splash / Loading`, `SCN-02 Main Menu`, `SCN-11 Operations Dashboard`)
- Current implementation host present: `36 / 46`
- Missing or incorrectly routed implementation host: `10 / 46`
- Active slice: `SCN-02 Main Menu V3 migration`
- SCN-01 accepted evidence: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/V3/SCN-01/`
- Main Menu baseline: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/V3/baseline/scn02_mainmenu_current_1920x1080.png`
- Operations baseline: `Design/VisualLockLayered/_CanvasTargetLockVisualMatch/V3/baseline/scn11_operations_current_1920x1080.png`
- Baseline finding: the current live Main Menu is the older gold Canvas family and is not a V3 visual-match candidate.

## Canonical 46-Target Matrix

Status vocabulary: `Open` means a host exists but V3 is not accepted; `Missing host` means a distinct live screen/state still needs an implementation owner; `Wrong route` means the route currently presents another screen.

| # | Canonical screen/state | Current Canvas host | V3 status |
|---:|---|---|---|
| 01 | Splash / loading | `SCN01_LoadingContent.prefab` | Complete; live progress binding preserved; 1280x720, 1920x1080, 2400x1080, and 4800x2160 evidence captured |
| 02 | Main menu | `SCN02_MainMenuContent.prefab` | Open; baseline captured |
| 03 | Commander profile | `SCN03_CommanderProfileContent.prefab` | Open |
| 04 | Campaign chapter select | `SCN05_CampaignOperationsContent.prefab` | Open |
| 05 | Campaign mission select | `SCN05_CampaignOperationsContent.prefab` | Open |
| 06 | Mission briefing | `SCN06_MissionBriefingContent.prefab` | Open |
| 07 | Match HUD | `SCN08_MatchHudContent.prefab` | Open |
| 08 | Match HUD transport passengers | `SCN08_MatchHudContent.prefab` | Open |
| 09 | Build drawer | `SCN09_BuildDrawerPopup.prefab` | Open |
| 10 | Operations dashboard | `SCN11_OperationsDashboardContent.prefab` | Open |
| 11 | Skirmish setup | `SCN13_SkirmishSetupContent.prefab` | Open |
| 12 | Store | none | Missing host |
| 13 | Armory | `SCN19_ArmoryContent.prefab` | Open |
| 14 | Mission result victory | `POP05_MissionResultPopup.prefab` and mission-result presentation | Open |
| 15 | Mission result defeat | `POP05_MissionResultPopup.prefab` and mission-result presentation | Open |
| 16 | Settings | `SCN_SettingsPopup.prefab` | Open |
| 17 | Pause options | `PauseMenuPopup.prefab` | Open |
| 18 | Expanded ARIA command assistant | `POP13_ARIACommandAssistantPopup.prefab` | Open |
| 19 | Full tactical map | `SCN08_FullMapPopup.prefab` | Open |
| 20 | Build placement confirmation bar | `SCN08_BuildPlacementConfirmationBar.prefab` | Open |
| 21 | Resource logistics exchange | `POP12_ResourceExchangePopup.prefab` | Open |
| 22 | First-launch language choice | `FirstLaunchLanguageChoice.prefab` | Open |
| 23 | First-launch comic playback | `FirstLaunchNarrativeSequence.prefab` | Open |
| 24 | First-launch commander identity | `FirstLaunchNarrativeSequence.prefab` | Open |
| 25 | First-launch ARIA guidance | `FirstLaunchNarrativeSequence.prefab` | Open |
| 26 | Threat alert | `ThreatAlertPopup.prefab` | Open |
| 27 | Threat alert route preview | `ThreatAlertPopup.prefab` | Open |
| 28 | Confirm raid | `ConfirmRaidPopup.prefab` | Open |
| 29 | Build placement | `BuildPlacementPanel.prefab` | Open |
| 30 | Build placement metadata validity | `BuildPlacementPanel.prefab` | Open |
| 31 | Reward unlock | `RewardUnlockPopup.prefab` | Open |
| 32 | End-of-day report | `EndOfDayReportPopup.prefab` | Open |
| 33 | Intel reveal | `IntelRevealPopup.prefab` | Open |
| 34 | Ability / upgrade detail | `AbilityUpgradeDetailPopup.prefab` | Open |
| 35 | Assistant takeover | none | Missing host |
| 36 | Loadout / squad prep | none | Missing host |
| 37 | Match tactical feedback | `SCN08_MatchHudContent.prefab` and runtime feedback views | Open |
| 38 | Build drawer disabled state | `SCN09_BuildDrawerPopup.prefab` | Open |
| 39 | Unit command wheel | none | Missing host |
| 40 | Unit command wheel targeting | none | Missing host |
| 41 | District detail actions | none | Missing host |
| 42 | Inbox | none | Missing host |
| 43 | Events | none | Missing host |
| 44 | Ranking | none | Missing host |
| 45 | Command feed | `UIRoute.CommandFeed` currently presents Commander Profile | Wrong route |
| 46 | Tutorial overlay / highlight | assistant/tutorial presentation components, no canonical V3 capture host | Missing host |

## Dependency Order

### V3-01 - Foundation And Capture Coverage

- [x] Reconcile the 46-target V3 inventory against current Canvas hosts.
- [x] Record a current Main Menu baseline against the canonical V3 target.
- [x] Make route capture safe for a live GUI Editor; only batch mode may exit the Editor process.
- [x] Extend capture routing to every existing `UIRoute`, including Operations and currently missing-route fallbacks.
- [ ] Create shared V3 color, type, border, panel, icon, and selectable-state tokens without adding an Update loop.
- [ ] Add deterministic capture selectors for every popup and shared-state variant.
- [ ] Add a validation that fails if a V3 route silently falls back to Main Menu or Commander Profile.

### V3-02 - First Launch And Loading

- [ ] First-launch language choice.
- [ ] First-launch comic playback.
- [ ] First-launch commander identity.
- [ ] First-launch ARIA guidance.
- [x] Splash/loading.

### V3-03 - Menu Shell And Progression Routes

- [ ] Main Menu.
- [ ] Commander Profile.
- [ ] Campaign chapter select.
- [ ] Campaign mission select.
- [ ] Mission briefing.
- [ ] Operations dashboard.
- [ ] District detail actions.
- [ ] Skirmish setup.
- [ ] Store.
- [ ] Armory.
- [ ] Loadout / squad prep.
- [ ] Inbox.
- [ ] Events.
- [ ] Ranking.
- [ ] Command Feed.

### V3-04 - Match HUD And Tactical Tools

- [ ] Match HUD.
- [ ] Transport passengers.
- [ ] Tactical feedback.
- [ ] Build drawer.
- [ ] Build drawer disabled state.
- [ ] Full tactical map.
- [ ] Build placement confirmation bar.
- [ ] Unit command wheel.
- [ ] Unit command wheel targeting.
- [ ] Expanded ARIA command assistant.
- [ ] Tutorial overlay / highlight.

### V3-05 - Popup And Result Family

- [ ] Resource logistics exchange.
- [ ] Threat alert.
- [ ] Threat alert route preview.
- [ ] Confirm raid.
- [ ] Build placement.
- [ ] Build placement metadata validity.
- [ ] Reward unlock.
- [ ] Mission result victory.
- [ ] Mission result defeat.
- [ ] Settings.
- [ ] Pause options.
- [ ] End-of-day report.
- [ ] Intel reveal.
- [ ] Ability / upgrade detail.
- [ ] Assistant takeover.

### V3-06 - Global Acceptance

- [ ] All 46 rows have current 1920x1080 direct-comparison evidence.
- [ ] All applicable screens pass 1280x720, 2400x1080, and 4800x2160 responsive capture.
- [ ] All selectable families have state evidence.
- [ ] Accessibility contrast, large-text, localization, and RTL checks pass.
- [ ] Canvas rebuild and frame-time gates pass with no new per-frame polling.
- [ ] Editor console is clear of new errors.
- [ ] Checked Unity validation passes when no Editor owns the project.
- [ ] `main == origin/main` and the repository is clean except independently owned user changes.
