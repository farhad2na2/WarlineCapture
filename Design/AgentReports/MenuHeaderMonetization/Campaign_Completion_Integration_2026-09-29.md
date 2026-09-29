# Completed-Campaign home integration

Source direction: [supplied handoff](../CampaignCompletionHome/Handoff.md), approved by the user. Full-panel direction was also approved with “looks good implement that.”

## Implementation

- Full native ARIA command-room panel with the unchanged original ARIA portrait; six separate commander scenes mapped to saved portrait indices 0–5. Native text, frames and controls remain separate from the raster illustrations.
- Three additive aftermath sprites from the supplied package, with direct serialized scene/caption pairs and EN/FA localization. Native top-aligned aspect cropping keeps faces visible.
- Completion requires every registered required mission to be ready and first-cleared. Required missions are not filtered by availability or ownership. Legacy dedicated mission runtimes contribute readiness through the shared access service.
- Today's 18 registered missions produce **All Available Missions Completed**. **Campaign Complete** requires the registered authored final Chapter V mission and full completion. Pending resume takes precedence.
- Each home visit keeps one scene/caption pair stable across refresh, locale and overlays. Returning from another route rotates without immediate repetition.
- Choose Mission re-reads progression and opens Campaign review. Story Archive uses the existing narrative canvas with isolated story-only playback, earned mission/chapter gates, close/escape and chooser return. M01's sequences are bound explicitly because they are absent from the bootstrap replay array. Chapter bookends require all five missions; the canonical epilogue requires full campaign registration/completion.

## Evidence categories

| Evidence | Status / scope |
| --- | --- |
| User visual direction | Approved for full panels and completed-Campaign concept. |
| Automated/native state checks | `Logs/menu-header-completion-07.log`, exit 0, final corrected builder/localization: whole catalog, saved reload, missing readiness, pending resume, stable pairs, locale/overlay stability, no immediate repeat, distinct full ending, archive earned gates and Choose Mission without deployment. M01's three supplemental references are required by builder validation. Earlier completion-02 also passed. |
| Native captures | Eight ordinary/stress EN/FA homes at 1920×1080 / 2400×1080, six saved commander scenes, and sixteen completion captures. All commander identities plus representative completion and long-name/max-Credits captures visually inspected. |
| Full Campaign ending | Four native captures use a future 25-mission projection fixture. This does not establish Chapter V gameplay release or acceptance. |
| Normal input | Initial physical input reached Story Archive, replayed M02 briefing on the native canvas and returned to the chooser. The user paused before the remaining route journey; that run is incomplete. Final corrected-candidate input-04 was stopped because computer control selected the separate MapVariants Editor. Temporary save/input settings were restored. Campaign review/back, Operations/back, Skirmish/back and final-candidate archive playback remain pending physical verification. |
| Player / device / packaged build | Pending. Broader gates in Final_Handoff.md remain open. |

Live corrected-menu inspection also verified all 54 brief/comms/debrief sequences for the 18 registered missions are bound, with no missing IDs. This read-only CLI inspection does not establish normal-input playback of every story.

Candidate source/config/art hashes are in `candidate-completion-current.json`. The earlier `candidate-completion-before-input.json` snapshot preceded the final prefab/catalog rebuild; expected generated changes are listed in the current manifest. Original ARIA source/import hashes remain unchanged.

## Retained failures

- panels-01: Pipeline reset before validation ownership; no native result claimed.
- completion-01: required readiness correctly failed for legacy dedicated runtimes; corrected in projection, completion-02 passed.
- input-01: helper localization namespace compile error, fixed before input review.
- input-02: Pipeline dispatch reset, no validation ownership.
- input-03: partial physical input only; Editor closed during user pause, no pass marker. Isolated save and temporary input settings were scoped to that Editor process.
- input-04: native fixture initialized, but computer control repeatedly selected the separate MapVariants Editor despite exact-project focus commands. Stopped via its own cleanup path with a failure receipt; no physical navigation pass claimed.
- completion-03: restarted Editor still compiling/importing; Pipeline returned 503 before validation ownership. Retried only through the checked wrapper.
- completion-04/05: Pipeline timeout/reset before validation ownership during assembly settling.
- completion-06: incorrect qualified test type name; corrected to the actual global `MenuHeaderRemediationTests` entrypoint.

Full logs and receipts are retained in Logs/. No direct Unity executable, batchmode, licensing recovery, IPC reset or unrelated process termination was used.
