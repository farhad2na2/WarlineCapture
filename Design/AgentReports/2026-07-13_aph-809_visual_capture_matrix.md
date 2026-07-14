# APH-809 Visual Capture Matrix

This report inventories the required evidence contract. It does not claim visual acceptance.

- Contract slots: `26`
- Required PNG artifacts: `32`
- Slots with submitted artifacts and reviewer pass: `0 / 26`
- Submitted artifacts: `0 / 32`
- Acceptance status: `Incomplete`
- Strict command: `python3 Tools/CI/aph809_visual_capture_matrix.py --check`

| Row | Surface | Category | Aspect | Camera | State | Required roles | Decision |
|---|---|---|---|---|---|---|---|
| `menu-main-16x9` | menu | menu | 16:9 | main-menu | main-menu-idle | capture | pending |
| `menu-main-20x9` | menu | menu | 20:9 | main-menu | main-menu-idle | capture | pending |
| `graphics-tier-gameplay-zoom-16x9` | match | graphics-tier | 16:9 | gameplay-zoom | gameplay-zoom-current-vs-candidate | current, candidate | pending |
| `graphics-tier-max-zoom-out-16x9` | match | graphics-tier | 16:9 | max-zoom-out | max-zoom-out-current-vs-candidate | current, candidate | pending |
| `graphics-tier-night-16x9` | match | graphics-tier | 16:9 | gameplay-zoom | night-current-vs-candidate | current, candidate | pending |
| `graphics-tier-gameplay-zoom-20x9` | match | graphics-tier | 20:9 | gameplay-zoom | gameplay-zoom-current-vs-candidate | current, candidate | pending |
| `graphics-tier-max-zoom-out-20x9` | match | graphics-tier | 20:9 | max-zoom-out | max-zoom-out-current-vs-candidate | current, candidate | pending |
| `graphics-tier-night-20x9` | match | graphics-tier | 20:9 | gameplay-zoom | night-current-vs-candidate | current, candidate | pending |
| `day-night-day-16x9` | match | day-night | 16:9 | gameplay-zoom | day-12-00 | capture | pending |
| `day-night-dusk-16x9` | match | day-night | 16:9 | gameplay-zoom | dusk-21-00 | capture | pending |
| `day-night-night-16x9` | match | day-night | 16:9 | gameplay-zoom | night-23-00 | capture | pending |
| `day-night-day-20x9` | match | day-night | 20:9 | gameplay-zoom | day-12-00 | capture | pending |
| `day-night-dusk-20x9` | match | day-night | 20:9 | gameplay-zoom | dusk-21-00 | capture | pending |
| `day-night-night-20x9` | match | day-night | 20:9 | gameplay-zoom | night-23-00 | capture | pending |
| `static-map-near-16x9` | match | static-map-chunks | 16:9 | static-map-near | near-chunk-readability | capture | pending |
| `static-map-medium-16x9` | match | static-map-chunks | 16:9 | static-map-medium | medium-chunk-readability | capture | pending |
| `static-map-far-16x9` | match | static-map-chunks | 16:9 | static-map-far | far-chunk-readability | capture | pending |
| `static-map-near-20x9` | match | static-map-chunks | 20:9 | static-map-near | near-chunk-readability | capture | pending |
| `static-map-medium-20x9` | match | static-map-chunks | 20:9 | static-map-medium | medium-chunk-readability | capture | pending |
| `static-map-far-20x9` | match | static-map-chunks | 20:9 | static-map-far | far-chunk-readability | capture | pending |
| `mip-streaming-near-16x9` | match | mip-streaming | 16:9 | mip-streaming-near | near-settled | capture | pending |
| `mip-streaming-medium-16x9` | match | mip-streaming | 16:9 | mip-streaming-medium | medium-settled | capture | pending |
| `mip-streaming-far-16x9` | match | mip-streaming | 16:9 | mip-streaming-far | far-settled | capture | pending |
| `mip-streaming-near-20x9` | match | mip-streaming | 20:9 | mip-streaming-near | near-settled | capture | pending |
| `mip-streaming-medium-20x9` | match | mip-streaming | 20:9 | mip-streaming-medium | medium-settled | capture | pending |
| `mip-streaming-far-20x9` | match | mip-streaming | 20:9 | mip-streaming-far | far-settled | capture | pending |

## Remaining Evidence

Every pending row still needs a real PNG captured from one exact revision and device profile, complete capture metadata, SHA-256 verification, and an explicit reviewer decision. Current-versus-candidate rows additionally require identical camera transforms. Logs alone do not satisfy this contract.
