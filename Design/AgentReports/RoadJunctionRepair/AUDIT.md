# Road junction kerb repair — 2026-10-04

The reported pale rectangular bands are generated `DenseCity_KerbPaving` geometry above adjoining `DenseCity_FlatPaving` road surfaces, rather than a damaged bitmap texture. The normally deployed M01 renders these combined native meshes through ECS. The source builder emitted a full kerb along each dirt road slab, including across connecting carriageways.

`DenseCityRoadKerbRepair` subtracts overlapping paving footprints from kerb triangles at the same elevation. It keeps the existing materials, preserves triangle winding, and changes only the generated visual kerb mesh. `MapDenseCityBeautify.BuildVisuals` invokes the same repair after generating the paving and kerbs, preventing regeneration from restoring the bars. Gameplay surface, navigation, building owners and mission definitions were not edited.

## Automated evidence

- `/private/tmp/warline-road-repair-01.log`: Passed; 8,096 kerb triangles clipped.
- `/private/tmp/warline-road-repair-02.log`: Failed before execution because the Editor was still importing or compiling. Retained.
- `/private/tmp/warline-road-repair-03.log`: Passed; one numerical remnant clipped, followed by a repeat repair producing zero further changes.
- Scoped source `git diff --check`: Passed.

## Visual and player evidence

- Before: normal Campaign → M01 → Briefing → Loadout → Deploy → narrative dismissal; native HUD captures in `Before/`.
- After: reviewed deployed M01 captures in `After/`, at 2400×1080 English/Farsi and 1920×1080 Farsi. The junction bars are gone; road-edge kerbs remain. The normal touch journey reached the HUD without an injected outcome.
- `/private/tmp/warline-road-after-01.log`: failed waiting for deploy; fresh scene baking reported an eligible source row without a converted render entity. Retained. The warmed retry (`warline-road-after-02.log`) reached the native HUD and confirmed the road fix, then failed the strict complete-mission gate after 90 seconds with ARIA Manual and zero actions. Both failed logs are retained.
- Full mission readiness remains a separate gate. Baseline normal-input runs failed to complete M01, with ARIA staying Manual and performing zero actions. This geometry check does not establish mission completion or device acceptance.
- Physical Android acceptance: pending.

Raw logs, including failures, are retained locally under `Logs/`.
