# Art/Atlas M01 V4 Target-Perfect Iteration

Date: 2026-05-17
Owner: Art/Atlas
Status: review candidate; not target-perfect
Priority: P0

## Lane

Art/Atlas

## Task

Iterate M01 target-match assets after user review called out soldier proportions and incorrect shadow treatment.

## Output

- Manifest: `Design/VisualLock/Gameplay/M01_AIProductionAssets/Manifests/m01_target_match_asset_manifest_v4.json`
- Contact sheet: `Design/VisualLock/Gameplay/M01_AIProductionAssets/ContactSheets/m01_target_match_assets_v4_contact.png`
- Composite: `Design/AgentReports/Captures/M01_TargetMatchV4_AssetPlacementReview_1920x1080.png`
- Comparison: `Design/AgentReports/Captures/M01_TargetMatchV4_AssetPlacementReview_vs_Target_Comparison.png`
- Diff heatmap: `Design/AgentReports/Captures/M01_TargetMatchV4_AssetPlacementReview_vs_Target_DiffHeat.png`

## Imagegen Provenance

Built-in imagegen source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected v4 files:

- Clean plate: `ig_061caec3064fc95a016a09cb61eb088198a675fd5124c687a2.png`
- Player corrected-shadow sheet: `ig_061caec3064fc95a016a09d0b0a6048198bff9bb6209863bac.png`
- Enemy corrected-shadow sheet: `ig_061caec3064fc95a016a09d109345081988f2529d848ddbc5b.png`

The earlier v4 player/enemy sheets with the lower-left shadow direction were rejected and not used for the packaged v4 candidate.

## Assessment

What improved:

- Soldier silhouettes are leaner and less cartoon/chubby than v3.
- Shadow treatment no longer uses a compact oval base.
- Shadow direction now trails lower-right/southeast, matching the corrected background-lighting read better than the rejected lower-left pass.
- Enemy/player scale parity remains preserved.

Still not target-perfect:

- The v4 plate composition shifted farther from the target than v3; road/building masses and cover walls do not align closely enough.
- Unit silhouettes are improved but still brighter/more separated from the road than the target soldiers.
- Player-region numeric error worsened because the v4 plate under the player formation diverges strongly from the target road/corner.

## Metrics

- Full-frame MSE: `1266.20`
- World crop MSE: `1171.94`
- Player region MSE: `1449.64`
- Enemy region MSE: `983.78`

These are worse than v3 numerically because the background composition drift dominates the comparison, even though the unit shadow direction improved.

## Validation

- `m01_target_match_asset_manifest_v4.json` written.
- V4 transparent unit/marker PNGs scanned for opaque chroma-green residue: `M01_V4_GREEN_REMAINING 0`.
- No runtime code, prefabs, scenes, UI implementation, or `Assets/` imports modified.
- No target mockup crops, screenshots, contact sheets, deterministic vector art, or scripted composites used as final runtime art.

## Recommendation

Do not route v4 as target-perfect. Use it as direction feedback:

- keep the v4 corrected lower-right/southeast shadow direction
- keep leaner, lower-detail unit proportions
- generate the next plate with stricter target composition constraints, especially the player road corner and enemy road/cover layout
