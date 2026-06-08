# PM Art/Atlas AI Production Asset Pack Routing

## Lane

PM

## Task

Reject the board-only Gameplay VisualLock package as insufficient and route Art/Atlas to produce ready-to-implement AI-generated production PNG assets.

## Files changed

- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/ui_current.md`
- `Design/AgentTasks/support-ftue_current.md`
- `Design/AgentReports/2026-05-09_pm_art-atlas-ai-production-asset-pack-routing.md`
- `Design/VisualLock/Gameplay/M01_AIProductionAssets/README.md`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/README.md`

## Contracts touched

- `Design/Tactical_Map_AI_Workflow.md`
- `Design/Art_Asset_Requirements_Register.md`
- `Design/M01_FirstContact_Production_Contract.md`
- `Design/Chapter01_Tactical_Production_Implementation_Plan.md`
- `Design/VisualReferences/2DIsometricProduction/GoldenAssets/README.md`
- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-lock-package.md`

## User-visible behavior

No runtime behavior changed. The project remains blocked before Gameplay implementation until Art/Atlas produces actual AI-generated production assets, not review boards.

## Validation run

- Read active Art/Atlas and Gameplay tasks.
- Located the accepted tactical map AI workflow.
- Located the art asset requirements register.
- Located the M01 production contract.
- Located the existing 2D isometric GoldenAssets production reference folders.
- Routed Art/Atlas to use those workflows explicitly.

## Validation result

The previous Gameplay VisualLock package is rejected for implementation.

Rejected gaps:

- No big zoomed-out Tehran strategic map asset.
- No complete zoomed-in tactical map set.
- No high-quality ready-to-use marker PNG sprites.
- No player/enemy soldier sprite atlas frames.
- No building PNG atlas states.
- Output is board/reference style, not ready-to-implement asset production.
- The user explicitly requires AI-generated high-quality assets, not deterministic placeholder/vector outputs.

## Known gaps

- The new asset pack has not been produced yet.
- Gameplay remains blocked.

## Cross-lane impacts

- Art/Atlas is active on the AI production asset pack.
- Gameplay waits for the asset pack before runtime implementation.
- QA/HCI waits for Art/Atlas, then Gameplay runtime evidence.
- UI/Designer/Support-FTUE wait unless a concrete issue is routed.

## Next recommended task

Art/Atlas should produce:

`Design/AgentReports/2026-05-09_art-atlas_m01-ai-production-asset-pack.md`

with runtime PNGs under:

`Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/`

and review mirrors under:

`Design/VisualLock/Gameplay/M01_AIProductionAssets/`
