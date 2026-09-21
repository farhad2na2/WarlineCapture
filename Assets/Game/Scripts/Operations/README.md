# Operations game assemblies

Operations packages add assemblies under this folder. They do not edit Skirmish or Campaign trees.

| Package | Assembly | Notes |
|---|---|---|
| P0 | `Game.Operations.Contracts` | IDs, schemas, payloads, save DTOs. No engine references. |
| P1 | `Game.Operations.Strategic` | City and profile transactions. References only `Game.Operations.Contracts`. No engine references. |

Shared-seam blockers: `Design/Roadmap/Operations/P0_SHARED_SEAMS.md` and `Design/Roadmap/Operations/P1_STRATEGIC_CORE.md`.
