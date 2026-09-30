# Airlift tactical marker implementation

## Visual review

The user approved `approved-direction.png` before implementation and expanded the scope to soldier, vehicle and building selection markers. The approved image was generated with the built-in ImageGen tool. Its direction is implemented with native geometry and localized UI text; the image is design evidence, not a runtime texture.

Direction: sparse amber ground marks for mission destinations, small charcoal labels with amber keylines and pictograms, cyan selection corners, no filled selection plates or continuous ground rings, no giant words on the terrain. Existing HUD controls and ARIA Play / Stop remain.

## Implementation

- Landing perimeter: eight short ticks at the real gameplay radius; amber securing / coral contested / green clear states, with clock / alert / check pictograms.
- Pickup, transfer and departure: compact localized labels and corner marks. Anchor positions come from the active mission map and extraction state.
- ARIA ground target and Move destination: small diamond with sparse corner marks.
- Soldiers and vehicles: native prefab meshes use cyan ground corners; vehicle fill and continuous bounds frame are hidden.
- Buildings: the runtime ground corners retain the canonical gameplay footprint and authored orientation. Filled plates, elevated cages and scan bands are hidden.
- Selection drag: open corner frames preserve the existing drag coordinates and selection input.
- Labels have no Button, GraphicRaycaster or raycast targets. Their bounds stay inside the battlefield so they cannot cover the HUD rails.

Soldier, vehicle, building, selection-drag and Move destination styles are shared across the game. Airlift mission markers and its ARIA ground cue are scoped to Airlift; other missions retain their existing ARIA ring cues. Mission anchors, combat rules, transport rules, hitboxes and command semantics remain the same.

## Evidence

First native iteration: `/private/tmp/airlift-marker-watch-en-01.log` passed the complete English ARIA mission, rescued 4/4, and returned through the result to Campaign. Its screenshots exposed label occlusion by vehicles; `first-native-iteration/` preserves that evidence. This iteration is not the final visual candidate.

## Final candidate evidence (2026-09-30)

All successful runs used the required macOS GUI wrapper and finished with exit 0. Full logs, including unsuccessful building-probe launches, are retained as lossless gzip files; `logs.json` records raw SHA-256 values and exact pass/fail markers.

| Check | Evidence | Result |
|---|---|---|
| Native prefab/config preparation | preparation-02 | Passed |
| English ARIA normal-input mission | watch-en-03 | Passed: 4/4 specialists, both transport legs, secure hold, aircraft departure, all six active comic panels, debrief, result and Campaign return; 164,247 ms |
| Persian manual normal-input mission | manual-fa-02 | Passed: same complete journey; 91,711 ms; zero specialist losses |
| Mission ground presentation | both final journeys | Sparse geometry, actual landing radius, localized font and non-interactive labels passed; securing and clear observed |
| Soldier/vehicle selection | vehicle-focused-01 | Passed: 22 cases; completed GUI lifecycle with the repository completion helper |
| Building selection | building-focused-01 | Passed: 11 cases including footprint, owned center, authored rotation, clear/destroy visibility and portrait |
| Airlift passenger rules and scenery sockets | focused-01 | Passed: 15 rule cases, two socket cases; neighboring scenery and prior visibility preserved |
| ARIA guidance regression | focused-01 | Passed: 13 Show Me cases and extraction-status recovery |

`final-aria-en/` and `final-manual-fa/` contain native screen captures from those final runs. Agent visual inspection checked pickup/soldier selection, transfer/boarding, clear-state and Persian departure. The actual departure target is captured in `final-manual-fa/destination-11.png`. Shared vehicle selection is visible during the manual transport legs. Earlier `watch-en-02` and `manual-fa-01` passed their full journeys, but predate the last transfer-label visibility and icon replacement refinements; they do not establish final-candidate readiness.

The first building play-mode launch overlapped the previous GUI test's completed but still open Editor; it produced no pass marker and was cancelled through its launch dialog. The second launch entered Play mode but timed out before deployment because the old probe waits for a deployment button directly from Menu. Its failure is retained. It does not establish native building click-ownership or building-screen acceptance. The eleven focused building cases are separate evidence.

Known pre-existing Burst BC1016 diagnostics remain in the native logs for `CampaignMissionAttemptResourceInitializationSystem`; this was also present in the earlier map candidate. No C# compilation errors were observed. No failed or partial run is counted as a readiness pass.

## Review and pending gates

Open the isolated worktree and use Main Menu → Campaign → Chapter 1 → Airlift. Review ground labels, landing ticks, pickup/transfer/departure and soldier/vehicle selection using both manual controls and ARIA Play / Stop. The building selection style is shared, so review it in a mission with selectable buildings as well.

Mockup direction: **user approved**. Native implementation visuals: **agent inspected; user review pending**. Complete native Airlift input journeys: **passed**. Real player/device acceptance: **pending**. The contested state was not encountered in those journeys and remains a native visual gate. Native building click ownership/screens need a current-menu probe or real player review. The map candidate's existing audio, minimap, resume/replay and device-performance gates remain in `../WORKSPACE.md`. This is a mission review candidate, not a production release qualification.

