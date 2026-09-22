# O001 Street Signals readiness review

Reviewed 2026-09-22 at `02da3227dce8c2908743f663aa4ae49c5b3d8cd9` (latest fetched origin/main). Scope is Operations O001, not Campaign M1 First Contact.

**Decision: working automated rules/presentation prototype; not ready for ordinary players. Further development is required.** The objective sequence and settlement broadly match the brief, but the normal player journey, tactical decision, environment and intended pacing do not yet meet it. This review does not redefine the narrower internal milestone called “playable” in P4.

## Verified evidence

- Fast-forwarded main from `ae15ff7e4` to `02da3227d`; existing local modifications were preserved.
- `check_p4.py`: exit 0, `[OperationsP4Validation] result=Passed checks=14`.
- `check_mobile_ready.py`: exit 0, `[OperationsMobileReadyValidation] result=Passed checks=8`.
- `check_aria_evidence.py`: exit 0, `[OperationsAriaEvidenceValidation] result=Passed checks=8`.
- Fresh Unity 6000.5.2f1 Play Mode capture through `Tools/CI/invoke_unity_macos.sh`, timeout 720 seconds, O001 Regular EN, diagnostic seed 22102. Wrapper exit 0 and `[OperationsAriaPlayModeCapture] result=Passed` both verified. Full log: `/private/tmp/operation-o001-review-20260922.log`.
- Result: Victory at tick 39, no task-force losses, settled revision 3, Credits +120, XP +50. Screenshots and result are in [the fresh evidence directory](host-aria-evidence/operation.o001/Regular/22102/).
- Visually inspected the fresh mid-mission and victory screenshots. This was an automated API-driven capture, not a manual pointer/touch playthrough or a phone test. Seed 22102 is diagnostic, not a substitute for the prescribed acceptance matrix.

Evidence caveat: the capture runner hardcodes `platform: windows-editor` and Windows-shadow wording even when executed on this Mac. The fresh JSON is retained as generated; its true execution platform is **macOS Editor**. Its `AriaWon` label certifies the local runner's criterion, not the full shipping-input acceptance gate. The Unity log also reports system-order warnings and 398 persistent allocations at teardown; those are not attributed to O001 without further isolation.

## Findings in priority order

### P0 — A player cannot use the demonstrated shell as a complete game

`Assets/Game/Scripts/Operations/Capture/OperationsAriaPlayModePresentation.cs:399` renders its HUD with OnGUI labels/textures. The bottom chips and the Continue-shaped result control are labels, not input handlers (see lines 472–569). No selection, movement or mission-action input is wired there. `OperationsAriaInputSkills.cs:13` calls the loop APIs directly, and the Editor capture drives advancement directly in `OperationsAriaPlayModeCapture.cs:484`.

The normal UI/composition assemblies have no connection to `OperationsLoopSession`. `OperationsDashboardScreenView.cs:93` mounts the existing raid popup and closes it on confirmation; it does not launch authored O001. The P4 document explicitly leaves MatchSceneView, shared UI/Watch and shipping save seams closed. A successful win-screen capture therefore does not prove normal deployment, play or return.

### P1 — The intended tactical decision is not demonstrated

The original D01 brief describes splitting scouts for speed versus keeping the squad together for protection, with distinct street/courtyard visibility. Current O001 completes with one scout scanning all three sites, then an infantry unit collecting evidence, then extraction; the fresh trace contains no Attack intent and no casualties.

Wave A is authored against completion of the entire `scan_signals` node, rather than the first completed individual scan (`OperationsAuthoredMissions.cs:143`). Its warning adds 30 ticks. Wave B waits 45 ticks after evidence interaction. The win at tick 39 precedes both reinforcements. This substantially weakens the stated risk/reward decision.

The standalone tactical simulation also lacks the shipping combat loop: `OperationsTacticalSession.cs:409` turns an accepted in-range Attack into a queued Death event, rather than resolving weapon damage over time. Step/MoveActors advance orders and objectives but do not implement an autonomous hostile combat simulation. Do not interpret the no-loss win as balanced live combat.

### P1 — Pacing diverges sharply from the mission brief

The brief targets 8–15 minutes with a 720-second deadline and a 15-second evidence interaction. Current rules use one tick per second, a 6-second scan and a 6-second interaction (`OperationsTacticalRules.cs:78`). The captured win is 39 simulated seconds. The runner advances per Editor update, so screenshot wall-clock duration is not real-time pacing evidence either.

The 720-second deadline is retained, but it does not make the mission an 8–15 minute experience. Reconcile the brief and pacing deliberately; do not fix this merely by lengthening passive waits.

### P1 — Shipping persistence remains unproven

The loop checks cover settlement/checkpoint semantics in the isolated model. `OperationsLoopStore.cs:8` stores documents and checkpoint blobs in memory. The P4 document leaves the product SaveDataModel integration closed. Correct model revisions and district deltas are useful evidence, but do not prove app termination/relaunch restores a player's actual operation and rewards exactly once.

### P2 — Presentation is still a greybox and has a visible layout defect

The capture is a green grid with primitive markers and obstacle bars, not the desert Old Quarter courtyards/lanes described in the brief. The fresh 1920×1080 mid-mission image shows “Working... Stay on target (5s)” overlapping the coach paragraph. In DrawActiveChrome, the coach body reserves a 72-pixel rectangle without advancing y before the pressure line is drawn. Fix layout and make sites, units, cover, extraction and threats readable before using this to judge first-time comprehension or enjoyment.

## Alignment with the original goals

| Original requirement | Current finding |
|---|---|
| Scan three distinct courtyards → recover evidence → extract | Implemented and completed in the isolated loop |
| Evidence/success facts and district consequence | Implemented in the model; fresh settlement result verified |
| Partial, failure, withdrawal, recovery, exactly-once settlement | Covered by focused model checks; normal player-flow evidence still missing |
| Split scouts versus protect the squad | Not established by the current threat/combat/pacing behavior |
| Wave A after first scan; B after evidence | A instead waits for all three scans; both arrive after the demonstrated win |
| 8–15 minute mission, 15-second interaction | Current win 39 simulated seconds; interaction 6 seconds |
| Old Quarter spatial/visibility experience | Abstract greybox exists; intended environment and player experience remain unfinished |
| Normal manual and ARIA visible-input win | API-driven simulation capture only; shipping touch/Watch route not connected |
| EN/FA and acceptance coverage | Model localization checks pass; fresh review is EN only; full visible-input matrix remains unmet |

Sources: [original O001 brief](../../Roadmap/Operations/Missions/D01_old_quarter.md), [acceptance contract](../../Roadmap/Operations/ACCEPTANCE.md), [P4 status](../../Roadmap/Operations/P4_VERTICAL_SLICE.md). P4 currently contains contradictory “Yes” and “not claimed” playable statements; resolve those and keep prototype, manual-playable and player-ready statuses distinct.

## Recommended next development sequence

1. Connect real briefing/deployment, world selection and actions, pause/withdraw, result Continue and return/redeploy to the Operations loop. Demonstrate a full human-controlled O001 win.
2. Connect shipping combat/navigation/visibility and an actual Old Quarter map. Correct wave A's trigger and prove that both scout splitting and squad protection are viable, meaningfully different choices.
3. Reconcile the pacing spec, preserving decisions rather than adding idle time; fix the HUD overlap and test supported aspect ratios and Farsi.
4. Connect durable profile/checkpoint storage and test restart during scanning, carrying evidence and settlement.
5. Re-run ARIA through the real player-input path, including failed/partial/withdrawn cases and the prescribed seed/language coverage; then conduct unfamiliar-player and phone checks for release readiness.

No gameplay code or balance was changed during this review. Unity generated folder metadata and requested preservation of old scene backups; those were copied into `Assets/_Recovery/` rather than discarded. Fresh evidence and this report are uncommitted.
