# S073 opening-focus source-host fixture

Run the recipe against a passed, current source build and a new evidence directory:

```bash
python3 Tools/CI/SkirmishSourceSimulation/Recipes/opening_focus/run_source_host_fixture.py \
  --build-dir Logs/SkirmishProduction/s073-focus-prereq-build-20261103-r1 \
  --output-dir Logs/SkirmishProduction/s073-opening-focus-source-host-fixture-<run-id>
```

The recipe checks every source-build pin, extracts the current `SkirmishPresentationSystem.Focus` body and its original method owners, compiles a separate entry point, and runs only `FocusFixtureProgram`. The fixture uses isolated source-host `EntityManager` worlds for missing-target, missing-transform, missing-request, and duplicate-request guards. It retains positive controls for an attempt-owned authored army and adds a source-census-shaped scenario-3 base with `LocalTransform` only, no attempt ownership or visual-spawn tag, and exactly one camera request singleton. Both positive paths check caller-supplied mobile and nonmobile facts and the expected original request semantics.

The fixture establishes source-host behavior only. It does not create or claim a native or same-world S073 session. For the positive controls only, the recipe substitutes the single `UnityEngine.Application.isMobilePlatform` property read with `SourceHostFixturePlatform.IsMobile`, supplied as `false` and `true`. The original mobile-limit policy body and Focus body remain intact; this fixture boundary does not model or infer the native device platform. The runtime failure from the prior direct Unity internal call remains in its r6 evidence directory.
