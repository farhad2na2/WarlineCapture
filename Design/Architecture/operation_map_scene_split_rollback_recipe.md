# Operation Map Scene Split Rollback Recipe

Date: 2026-07-15
Status: Shared foundation contract
Owner tracker: `operation_map_scene_split_and_generator_tracker.md`

## Purpose

Restore the accepted pre-split Match map exactly if the non-destructive scene ownership split or its atomic cutover fails. This recipe is independent of whether later maps are editor-authored or runtime-generated and independent of the eventual content loader.

## Rollback Ownership Set

The split/cutover change record must list every changed or created path. At minimum, rollback owns these existing paths:

```text
Assets/Game/Scenes/Match.unity
Assets/Game/Scenes/Match.unity.meta
Assets/Game/Scenes/Match/MatchSubScene.unity
Assets/Game/Scenes/Match/MatchSubScene.unity.meta
Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset
Assets/Game/Configs/Scene/Match_MapBuildingPlacement_Config.asset.meta
Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset
Assets/Game/Configs/Scene/Match_MapVehiclePlacement_Config.asset.meta
Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset
Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset.meta
Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationSceneIntegrity.json
Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationSceneIntegrity.json.meta
Assets/Game/GeneratedStaticMapPresentation/Scenes/
ProjectSettings/EditorBuildSettings.asset
```

The change record must also include every extracted operation-map scene, folder `.meta`, subscene, map config, catalog entry, generated map product, and build-setting path added by the split. No undeclared path may be part of the atomic cutover.

## Required Pre-Cutover Record

Before the first scene edit:

1. Require a clean `main` worktree and close Unity.
2. Record `git rev-parse HEAD` as `<pre-cutover-sha>` in the tracker validation log.
3. Run `Game.Editor.OperationMapPhase0BaselineProbe.Run` to an external `/private/tmp` JSON path.
4. Preserve the report SHA-256 and verify its result is `Passed`.
5. Record `git diff --name-status <pre-cutover-sha>..HEAD` after the staged split and before cutover. This is the exact cutover path ledger.
6. Keep shell stripping and route activation in one commit or one explicitly recorded contiguous commit range. Do not mix unrelated work into that range.

The accepted pre-split evidence is documented in:

- `../AgentReports/2026-07-14_opmap-002_phase0_baseline_probe.md`
- `../AgentReports/2026-07-15_opmap-005_static_map_presentation_refresh.md`

## Repository Rollback

Use this path when the cutover is already committed or pushed.

```bash
git switch main
git status --short
git revert --no-commit <first-cutover-sha>^..<last-cutover-sha>
git diff --name-status
git diff --check
git commit -m "Revert operation map scene split cutover"
```

The revert range must be the exact contiguous range recorded before cutover. Review `git diff --name-status` against the cutover path ledger before committing. The revert must restore tracked bytes and `.meta` GUIDs and remove tracked paths that the cutover created.

If validation fails before the cutover is committed, do not create a revert commit. Restore only the declared ownership set from `<pre-cutover-sha>` and remove only untracked paths listed in the cutover ledger. Never run a repository-wide clean or reset.

## Unity State And Product Verification

After repository rollback:

1. Confirm `Match.unity` again references `Assets/Game/Scenes/Match/MatchSubScene.unity`.
2. Run `Game.Editor.OperationMapPhase0BaselineProbe.Run`. The probe captures and restores the Editor scene setup in `finally`, so inspection cannot leave Match scenes loaded accidentally.
3. Compare manifest schema, canonical dependency hash, content hash, chunk size, chunk/source counts, integrity file set, every generated scene/meta SHA-256, placement counts, grid dimensions, and map-surface dimensions with the pre-cutover report.
4. Run the focused static-map ownership, integrity, rollback, scene-wiring, structural-validation, and Android build-resolver EditMode tests.
5. Run `Game.Editor.StaticMapPresentationBaker.Bake` twice. Both runs must be no-op and byte-identical; any generated-path change fails rollback acceptance.
6. Verify `ProjectSettings/EditorBuildSettings.asset` is byte-identical to `<pre-cutover-sha>` and the Android resolver selects the accepted current-map chunks.
7. Launch the current Match flow in Editor and verify camera, minimap, authored buildings/vehicles, surface movement, runway/helipad behavior, static presentation, and teardown.
8. Run Android APK validation when the failed cutover changed build settings, scene inclusion, generated presentation ownership, or Android resolver behavior.

## Acceptance Gate

Rollback is accepted only when:

- `git diff --check` passes;
- the worktree contains only the deliberate rollback commit;
- the authoritative baseline probe passes and matches the pre-cutover evidence;
- generated scenes and `.meta` GUIDs have exact set/hash parity;
- scene wiring, placement configs, manifest/integrity data, and build settings match the checkpoint;
- focused EditMode tests pass; and
- Editor gameplay parity passes, with Android validation added according to the risk rule above.

If any check fails, keep the extracted route disabled and do not regenerate or hand-edit evidence to make the rollback appear current.
