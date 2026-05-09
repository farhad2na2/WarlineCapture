# PM Message For Gameplay

Date: 2026-05-09
Priority: P0 make v2 soldier atlas import-ready before runtime integration

The Designer audit accepts v2 visually with minor notes, but the Gameplay audit blocks direct runtime import.

Use:

- `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`
- `Design/AgentReports/2026-05-09_art-atlas_m01-soldier-animation-atlas-fix-v2.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/PlayerRifleSquad/player_rifle_squad_animation_atlas_v2.png`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Units/EnemyPatrol/enemy_patrol_animation_atlas_v2.png`

Required cleanup:

- source-control Unity `.meta` files for every v2 soldier runtime PNG and v2 manifest file,
- explicit mobile importer settings,
- explicit manifest pivot, foot-anchor, contact-bounds, and normalized bounds metadata,
- documented atlas layout policy: keep separate player/enemy atlases, repack/pad, or split only if needed,
- gutter/extrusion or import settings that eliminate bilinear/mipmap bleeding risk.

Do not integrate v2 into gameplay yet.

Expected report:

`Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`

Return whether the package is ready for PM/user runtime acceptance or needs Art/Atlas fixes. Do not commit or push.
