# Bridge road, sidewalk and water refinement

Owner request: match the attached bridge mockup with continuous road markings, matching sidewalks, visible deck depth and improved water. The exact owner reference is preserved in `owner-reference.png`. This is a continuation of the approved visual-only scene dressing, based on main commit 8665300be.

## Implementation

The twelve existing bridge sites in AshLinePort (2), TrustUnderFire (2) and DenseCity (8) receive continuous asphalt, 2.2 m center dashes on the existing 5 m cadence, slim edge lines, and sidewalk paving made from the same mesh and material as nearby roads. Harbour markings extend onto the immediate approaches. Combined visual meshes add fascia and short deck support faces. Original bridges, terrain, collision, navigation, placements and gameplay components remain in place.

Bridge surfaces use the actual visible road elevation. This matters in DenseCity because its consolidated paving sits above its original road quads. Sidewalks fit inside the existing railings; the shorter city bridges use narrower paving strips. The visual pass retains the original crossing and water elevations. A derived visual copy of DenseCity_FlatPaving removes its redundant surface only inside the eight existing bridge footprints (48 intersecting triangles); the source mesh and collision meshes are unchanged. The road-height sampler also checks both approaches, so rebuilding after this cutout preserves the reviewed deck height.

Map-owned water materials use deeper teal color and fine, irregular surface ripples. The new shader control defaults to zero for other materials. The apply step verifies the chosen value after saving and synchronous reimport.

The new surfaces are children of the existing canonical scenery owners. Each bridge adds four material rows: asphalt, paint, sidewalk paving and deck faces. DenseCity adds exactly 32 rows beyond the previous 168-row dressing pass. Shared source meshes and material templates are reused without editing them.

## Native review history

- `audit-01.log`: baseline bridge dimensions, nearby materials and before captures; source scenes unchanged.
- `build-01.log`: original scene protection passed, but visual review found hidden DenseCity paving and a newly added ripple parameter saved at zero. The original captures and receipts are retained in `build-01-superseded`.
- `build-02.log`: corrected visible-surface height sampling, sidewalk kerb orientation, and explicit saved ripple value verification. City visual review still found overlapping consolidated paving; these captures and receipts are retained in `build-02-superseded`.
- `visibility-01.log` and `visibility-02.log`: unsaved isolation captures and native ray probes established the bridge geometry was present. These are diagnostic evidence only.
- `build-03.log`: trims redundant city paving within bridge footprints, selects the exact sidewalk mesh material instead of a different sidewalk atlas, and refreshes rewritten mesh GPU data. Scene protection passes for all three maps.
- `integration-01.log`: failed a validation assertion that expected a mesh name suffix. The mesh saver uses the asset filename as its name; corrected both the validation and repeat-build guard to check the exact asset path. No scene change was needed.
- `integration-02.log`: final fresh-load validation and packed source parity on the updated main base; see `VALIDATION.json` for the observed result.

Final native validation and packed database results are recorded in `VALIDATION.json` and full compressed logs. Normal-input mission playthrough, real device acceptance and GPU performance measurement were not performed for this visual pass.

## Reproduction

Use `Tools/CI/invoke_unity_macos.sh` in GUI licensing mode with Unity Hub open, an explicit log and timeout. Run `Game.Editor.MapVariants.OtherMapSurfacePolish.BuildBridgeContinuity` after the earlier base dressing pass. Review the resulting DenseCity source hash and exact row ledger before updating its strict inventory pin and rebuilding packed data. Run `Game.Editor.MapVariants.OtherMapSurfacePolish.ValidateBridgeContinuityPackaging` after updating the pin. Do not infer validation success from wrapper exit alone; require explicit pass markers.

The prior work's raw evidence, unrelated fighter-camera stash, and ProjectAuditorSettings.asset are retained. Selected native captures, the owner reference, receipts and full compressed logs accompany the follow-up commit.

## Final results

All twelve bridges passed fresh-load ownership, exact row count, persisted water and repeatable road-height checks. Source parity, deterministic database rebuild and isolation passed with 124,505 source rows, zero removed source rows and 38,909 unchanged logical placements. Packed content hash: `fb9782db3133172ef0957c97a4bd6d19644b5b3e66653ab67141656320191859`. Provisional active proxy capacity is 7427/8,000. The existing MMI entity count is still above its unchanged 24,000 limit; the camera/projection sweep, normal-input playthrough and device/GPU acceptance remain pending. This is a completed visual refinement, not a claim of full mission or device readiness.
