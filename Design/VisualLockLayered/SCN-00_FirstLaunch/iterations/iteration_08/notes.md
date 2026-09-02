# Iteration 08 — runtime ultrawide comic chrome

Target lock: `../../reference/SCN-00_FirstLaunchV3_ComicPlayback_Final_Target.png`

## Fixes

- Expanded the comic header timeline and dialogue body to the physical canvas width on ultrawide displays.
- Kept playback controls and the dialogue next panel pinned to the physical right edge.
- Preserved the authored 16:9 and 20:9 comic crops so the illustration is cover-fit rather than stretched.
- Kept the pause, subtitles, and skip panels on their opaque V3 gradient during narrative visibility changes.
- Updated the Menu scene installer to install the V3 narrative prefab instead of rebuilding legacy chrome.

## Runtime proof

- `comic_playback_v3_live_16x9.png`
- `comic_playback_v3_live_20x9.png`
- `live_capture_16x9.log`
- `live_capture_20x9.log`

Both live capture runs completed all four First Launch states and emitted `result=Passed`.
