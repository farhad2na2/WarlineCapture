# Runtime Visual Bug Diagnostic Playbook

## Purpose

Use this workflow when a runtime visual bug crosses Unity authoring, ECS baking, runtime spawning, LOD/render state, UI input, or camera state. These bugs should not be fixed by repeated guesswork. If one attempted fix does not solve the issue, or the defect can only be seen while the match is running, add targeted diagnostics and reproduce the actual game path before claiming it is fixed.

Recent example: helicopter blades were not rotating and the visual looked duplicated. Several visual-side changes did not expose the root cause. A single targeted ECS diagnostic showed the spin system was running, the unit was airborne, but `UnitHelicopterBladeReference` had `bakedBlades=0`. The correct fix was in the authoring/baking path, not a material, LOD, or model workaround.

## Trigger Cases

Use this playbook for:

- Runtime visual state bugs: static rotors, duplicate models, wrong tint, hidden/visible mismatch, missing markers, wrong selected state, or LOD state mismatch.
- ECS conversion/baking bugs: a prefab looks correct in YAML/Inspector but runtime entities have missing buffers, refs, tags, children, or source keys.
- UI-to-gameplay bugs: buttons click but no request is consumed, camera/minimap/selection jumps, zoom controls affect the wrong system, or drag state resets unexpectedly.
- Bugs where EditMode tests prove isolated math but the game scene still fails.
- Any issue marked "not fixed" after one attempted fix.

## Required Workflow

1. Reproduce or inspect the exact player path.
   - Use the real match scene, target HUD button, prefab spawn path, or camera interaction involved.
   - If batchmode PlayMode or a capture route exists, run it.
   - If the editor is already open and blocks batchmode, use the open editor path or state clearly that runtime verification is blocked.

2. Add one targeted diagnostic before the next fix.
   - One concise log line is usually enough.
   - Log the boundary where data should cross: authoring -> baked ECS, spawn request -> spawned entity, UI event -> ECS request, camera state -> UI projection, or LOD state -> visible root.
   - Do not add broad per-frame spam.

3. Include proof fields in the diagnostic.
   - Entity id, source prefab key, display name, and relevant state flags.
   - Expected references and counts: baked buffers, child counts, root entity refs, prefab refs, marker refs, or request queue length.
   - Visibility gates: disabled, hidden, culled, LOD current/desired, active renderer root, and selected/owned/faction state when relevant.
   - Work done this frame: rotated count, marker count, request consumed count, texture refresh count, or camera rect update count.

4. Run the game path after adding the log.
   - Perform the exact user action that shows the bug.
   - Read the log before editing again.
   - Let the log decide which layer is broken.

5. Patch only the broken layer.
   - If refs are missing, patch authoring/baking or config assignment.
   - If refs exist but are not consumed, patch the runtime system.
   - If requests are emitted but not consumed, patch the request consumer or binding.
   - If the system works but the visible object is wrong, patch visual state selection or prefab wiring.

6. Verify with both a focused automated test and the runtime path.
   - Add or update tests for the invariant exposed by the diagnostic.
   - Run the smallest useful Unity EditMode/PlayMode test.
   - For visual bugs, do not hand over as fixed until the scene path is exercised or the inability to run it is explicit.

7. Clean up or guard diagnostics.
   - Temporary diagnostics should be removed after the bug is proven fixed.
   - If the diagnostic is still needed for user confirmation, keep it one-shot and editor/development oriented.
   - Never leave high-frequency logs in shipped runtime paths.

## Diagnostic Pattern

Prefer one named log line that can be searched easily:

```csharp
Debug.Log(
    "[FeatureDiag] " +
    $"entity={FormatEntity(entity)} source={source} display={display} " +
    $"state={state} expectedRefs={refCount} visibleRoot={root} " +
    $"hidden={hidden} culled={culled} workThisFrame={workCount}");
```

For ECS visual bugs, include both the expected baked data and the active visual root. A root can be valid while the baked buffer is empty, or a request can exist while the visible LOD is hidden.

## Helicopter Blade Case Study

Symptom:

- Attack helicopter appeared without blade rotation.
- Earlier visual-side fixes did not solve it.
- The helicopter faction tint was separately fixed, but blades still did not rotate.

Useful diagnostic:

```text
[HeliBladeDiag] unit=361:3 source=Unit_Veh_Helicopter_Attack display=Attack Helicopter air=True visual=Detail->Detail detail=...:blades=0 bakedBlades=0 bakedRot=0
```

Interpretation:

- `air=True`: the helicopter was in the spin system query.
- `visual=Detail->Detail`: the expected detail visual was active.
- `bakedBlades=0`: no blade entities were captured by ECS baking.
- `bakedRot=0`: the spin system had no reliable blade refs to rotate.

Correct fix:

- Patch `UnitGridAuthoring` so blade refs are baked from the model root and, if that finds zero, from the full unit authoring hierarchy.
- Keep duplicate guards so a blade entity is not added twice.
- Add tests that prove idle air units rotate, baked blade refs rotate, no companion spinner is serialized, and helicopter unit prefabs expose bakeable blade transform names.

Expected confirmation:

```text
[HeliBladeDiag] ... bakedBlades=2 bakedRot=2 ...
```

## Handoff Rule

Do not say "fixed" for a runtime visual bug just because code compiles or an isolated test passes. Say "fixed" only after:

- The diagnostic explains the original failure.
- The patch addresses that specific failing boundary.
- The focused test passes.
- The real scene path has been run, or the handoff clearly says runtime verification is still blocked.
