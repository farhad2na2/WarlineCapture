# Operations game assemblies

Operations packages add assemblies under this folder. They do not edit Skirmish or Campaign trees.

| Package | Assembly | Notes |
|---|---|---|
| P0 | `Game.Operations.Contracts` | IDs, schemas, payloads, save DTOs. No engine references. |
| P1 | `Game.Operations.Strategic` | City and profile transactions. References only `Game.Operations.Contracts`. No engine references. |
| P2 | `Game.Operations.Tactical` | Neutral role binding, six tactical verbs, objective graph, facts, finite waves, and outcome precedence. References only `Game.Operations.Contracts`. No engine references. |
| P3 | `Game.Operations.Loop` | Mode-tagged launch, reserve/refund, true mission result, atomic settlement, HUD/briefing/result, shell history, and checkpoint recovery. References contracts, strategic, and tactical. No engine references. |

Shared-seam blockers: `Design/Roadmap/Operations/P0_SHARED_SEAMS.md`, `Design/Roadmap/Operations/P1_STRATEGIC_CORE.md`, `Design/Roadmap/Operations/P2_TACTICAL_RULES.md`, and `Design/Roadmap/Operations/P3_LAUNCH_RETURN.md`.
