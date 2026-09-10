# Campaign return, unlock presentation, and header clearance

## User report and confirmed causes

The supplied campaign screenshot showed the briefing overlapping the resource header and M3 looking locked after M2 completion.

Read-only inspection of the real profile confirmed M2's first clear and rewards were already saved and M3 was available. No completion receipts or unlocks were manually granted. Live Editor inspection reproduced a stale view: after clicking M2, the read model selected M2 while the right panel still displayed M3.

- The campaign and briefing screens mount in the shell's `PopupLayer`. The cached mission presentation refresh collected the other five regions but omitted this one.
- The campaign prefab's five `missionLockIcons` references were null: its authoring helper searched for `Lock`, while V3 nodes use `StateIcon`.
- Node art was authored as M1 completed, M2 selected, and later missions locked. It did not project all missions' completion state.
- M2's result Continue action requested the general main menu. The shell retained only Campaign as a special return destination and discarded the originating skirmish/operations page.
- The right briefing began at reference y=102 while the header ended at y=108.

## Changes

- Refresh mission views mounted in `PopupLayer` through the existing cached, version-driven presentation path.
- Project the completed-mission mask alongside availability. Bound node sprites, numbers, checkmarks, and lock icons now reflect saved state and current selection.
- Refreshing the campaign page with a completed mission selected focuses the next unfinished available mission. Explicit mission selections still remain selected for replay.
- M2's final Continue returns to Campaign. The shell remembers each match's originating mode and preserves that page through teardown and loading for Campaign, Quick Custom/Skirmish, and Operations.
- Move the right briefing to y=151, aligned with the map, and fit its contents into the available height above the footer. Preserve the current UI artwork and typography.
- Extend the real M2 Editor replay through debrief, final Continue, campaign return, M3 availability, live M2/M3 selection, briefing, and M3 deployment.

## QA scope and evidence

Editor-only validation uses an isolated project copy with private campaign saves, leaving the user's Editor open. QA captures remain under `/private/tmp/warline-m02-placement`; no evidence images are added to Design or Git.

The first isolated replay stopped on a Unity Search cache indexing exception before gameplay. The copied Library did not include the source project's `UserSettings/Search.index` descriptor. Rebuilding that copy's cache alone was insufficient; restoring the descriptor corrected its search configuration. No gameplay exceptions were filtered out.

The real-button M2 replay passed in Farsi: build, confirm, construction, recruitment, Victory, debrief, final Continue, direct Campaign return with M3 unlocked and selected, switching M2/M3 updates the panel, and M3 deploys into `Engage`. Place and confirm remained responsive (39.4 ms / 47.3 ms); result Continue took only a few milliseconds. Log: `/private/tmp/warline-m02-campaign-return-play-04.log`.

The first return assertion checked the page during `EnteringMenu`; the probe now requires `MenuReady` and a completed presentation transition. The campaign screenshot was inspected and confirmed header/footer clearance and M3's available numbered marker. A remaining static M2 label highlight was corrected to follow current selection.

The user's original Editor was refreshed and opened to Campaign with their existing save. It reports `MenuReady`, selects M3, displays `M03`, enables the M3 node, hides its lock, and shows M2's completed label in green and M3's selected label in gold.

The 229-test regression matrix initially passed 220 tests; nine history-based architecture checks lacked Git history/Jenkins inputs in the isolated copy. The copy now has a local shared-object Git clone and the tracked root files. The affected source-growth suite then passed **17/17** in 223.0 seconds. Across the matrix and focused rerun, **all 229 distinct tests passed**, with no remaining failures or skipped tests. No baselines or assertions were relaxed.

- Matrix: `/private/tmp/warline-campaign-return-tests.xml` (220 passed; 9 infrastructure failures subsequently resolved).
- Final architecture rerun: `/private/tmp/warline-campaign-return-architecture-final.xml` (17 passed).
- Coverage includes six mode-return/retry cases, three landscape layout sizes, campaign node state, next-mission focus and replay selection, M2 settlement and result UI, M1 result UI, shell routing, audio route regressions, and all test names matching `Architecture`.
- `git diff --check` passed.
