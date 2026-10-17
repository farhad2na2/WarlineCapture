# Completed water, bridge and road visual pass — 2026-10-16

The owner resumed the handoff and explicitly approved visual-only scene dressing under existing props. This supersedes the historical pause and no-scene-edit restriction in HANDOFF.md. Work was performed on the requested CampaignVisualAudit checkout and integrated with main at f6694ad6f.

## Result

- Channel water now has bank depth, contact foam, flow-aligned ripples and restrained sky reflection. Existing bank mesh GUIDs, positions, triangles, normals, colors and bounds are preserved; only UV channels were added.
- AshLinePort and TrustUnderFire use terrain-intersection distance masks so foam follows the actual shore. Their masks contain 1,530 and 916 source intersection segments respectively.
- Nineteen dirt/paved joins across six maps receive continuous broken edges, dust, tyre traces and scattered grit. All seven DenseCity joins are now editable through renderer-local, map-owned material copies. Shared templates remain unchanged; unrelated conflicting materials remain listed in map receipts.
- Seven water maps received 132 visual-only groups (436 material rows): bridge poles and light fixtures, wing walls, narrow-bridge abutments, supported sandbag posts, bank grasses, canal-end grasses and quay bollards. Existing harbour boats and physical crossings are retained.
- DenseCity adds exactly 168 rows under existing canonical RenderOnly owners. Its packed database was rebuilt and reconciled; placements remain 38,909 and capacity is 7,427 of the existing 8,000 slots. Gameplay budgets were not changed.

## Verification and acceptance

| Gate | Evidence | Result |
| --- | --- | --- |
| Visual direction | Three owner-approved reference mockups; explicit permission for visual scene dressing | Approved direction |
| Native visual review | Current native captures across the eleven-map census; roads, harbours, canals and corrected bridge lamps inspected | Agent reviewed; owner review of final screenshots remains available |
| Scene protection | dressing-03.log; protected original state and render-only component checks for all seven maps | Passed |
| Material/mesh isolation | integration-01.log; maps=11, joins=19, crossMapMaterials=0; shader-error and unchanged mesh topology checks | Passed |
| Render reconciliation | 124,473 rows, including exactly 168 new DenseCity dressing rows | Passed |
| Packed determinism/source parity | Same content hash on repeated rebuild; removedRows=0, isolation=Passed | Passed |
| Main integration compilation | main-integration-compile.log after fast-forward to f6694ad6f; native reconciliation rechecked | Passed |
| Normal-input mission playthrough | No mission behavior or UI was changed; this task did not run a complete normal-input mission | Not performed |
| Real player/device acceptance and performance | No new device acceptance or GPU performance measurement | Pending |

The packed content hash changed from `746887b5f55cd8b642c61cecb5aa573aa40a0817caa254a7f2120823263b72de` to `3c34bcab103855870dbf8374e7c7c963a4142c0b36aea7dbe6ac10af8bebc82b` because of the authorized dressing. A separate pre-dressing parity run confirmed the original hash first. The DenseCity source pin is `d1f099ac88ed6585433da2d22b37003985a9cb40ba35c4ee88adbebb5a6fa267`.

Scene protection verifies existing gameplay objects, transforms and components during dressing. Shader application then verifies source scene hashes are unchanged relative to those dressed scenes; it does not claim the seven scene files are byte-identical to the original handoff.

## Failed and superseded evidence

Full original logs are retained locally, with lossless compressed copies committed. The evidence manifest identifies retained files by SHA-256, size, and whether selected for this commit.

- Original apply-01 and apply-05 failed; apply-02 through apply-08 were superseded for visual defects documented in HANDOFF.md.
- apply-09 was reviewed, then superseded by shoreline and scene dressing work.
- dressing-01 failed because an unsupported bank location lacked a nearby eligible owner. Such bank placements now skip explicitly; missing bridge owners still fail.
- dressing-02 passed structural checks but was visually rejected for oversized lamp heads. Its native captures and receipts remain in `dressing/attempt-02-rejected-lamp-scale`; one representative rejected capture is committed.
- dressing-03 corrected the lamps using separate poles and correctly scaled fixtures and passed native protection checks.
- integration-01 passed the full visual apply, reconciliation, inventory, repeat database build and source parity sequence.

Selected before/after road captures, final water and bridge captures, canal ends, reference images, all current map receipts and full compressed logs are committed. The larger superseded capture sets remain in this checkout and are recorded in `evidence-manifest.json`. Wide overview captures can show pre-existing dark/black objects; these are retained as evidence and are not presented as a fixed issue.

## Reproduction

Keep Unity Hub open and use `Tools/CI/invoke_unity_macos.sh` in GUI licensing mode with an explicit log, timeout and Editor path. Never pass batchmode or invoke Unity directly. The CRLF version-resolution issue mentioned in the historical handoff was fixed by intervening main changes.

1. `Game.Editor.MapVariants.OtherMapSurfacePolish.BuildWaterBridgeDressing` rebuilds only this task's visual groups. It preserves the existing source scene objects and records placement skips.
2. Reconcile and review any resulting source pin before repackaging; inventory intentionally fails on an unreviewed DenseCity source revision.
3. `Game.Editor.MapVariants.OtherMapSurfacePolish.ValidateWaterRoadIntegration` applies water/road materials, captures native views, checks reconciliation, rebuilds packed data twice, and validates source parity. Require its explicit `[WaterRoadIntegration] result=Passed` marker; wrapper exit alone is insufficient.

The unrelated fighter-camera stash and `ProjectSettings/ProjectAuditorSettings.asset` were preserved and excluded from the task commit.

Offline review: source/shader/document patch whitespace checks passed. The full patch reports only 105 standard Unity-generated empty YAML property values with trailing spaces in new material/meta files; their serialized bytes were retained. Asset metadata pairing and exact staged scope were verified.
