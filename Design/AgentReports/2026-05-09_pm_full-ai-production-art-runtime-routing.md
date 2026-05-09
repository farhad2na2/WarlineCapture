# PM Routing - Full AI Production Art Runtime Integration

Lane: PM
Task: Clarify Gameplay must integrate all M01 AI production art assets
Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentReports/2026-05-09_pm_full-ai-production-art-runtime-routing.md`
Contracts touched:
- M01 AI production art runtime integration gate
- ECS atlas-backed soldier animation runtime integration gate
- QA/HCI runtime validation gate
User-visible behavior:
- Gameplay must replace old/pre-production M01 visuals with the AI production asset pack where equivalents exist: background/map, tactical maps, buildings, markers, and v2 soldiers.
- Gameplay must use the full production asset manifest for maps/buildings/markers and the v2 soldier animation manifest for soldiers.
Validation run:
- Read current Gameplay, Gameplay PM message, QA/HCI, and Art/Atlas task files.
- Applied the user's clarification that implementation scope is the full new art pack, not only v2 soldiers.
Validation result:
- Routed full art-pack integration to Gameplay with expected report `Design/AgentReports/2026-05-09_gameplay_m01-ai-production-assets-runtime-integration.md`.
Known gaps:
- Runtime integration and capture proof are still pending.
- Final user visual approval still requires runtime capture/video at actual M01 camera scale.
Cross-lane impacts:
- Gameplay owns full AI production art integration and capture proof.
- Art/Atlas waits unless runtime proof exposes a specific art-side issue.
- QA/HCI waits for the full runtime integration handoff before validation.
Next recommended task:
- Gameplay should integrate the strategic/background map, tactical maps, buildings/props, markers, and v2 soldier atlases into the ECS/runtime M01 scene and produce capture/video proof for PM/user review.
