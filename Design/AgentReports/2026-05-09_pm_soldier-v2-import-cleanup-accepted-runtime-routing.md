# PM Review - Soldier V2 Import Cleanup Accepted For Runtime Integration

Lane: PM
Task: Review Gameplay v2 import-readiness cleanup and route runtime integration
Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-09_pm_soldier-v2-import-cleanup-accepted-runtime-routing.md`
Contracts touched:
- M01 v2 soldier import-readiness gate
- ECS atlas-backed soldier animation runtime integration gate
- QA/HCI runtime validation gate
User-visible behavior:
- No user-visible runtime behavior should change until Gameplay integrates v2 and produces capture/video proof.
Validation run:
- Reviewed `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`.
- Checked current Gameplay, Art/Atlas, and QA/HCI task files.
Validation result:
- Accepted for runtime integration. The cleanup report resolves the previous import-readiness blockers: v2 `.meta` files, import policy, anchor/contact metadata, and atlas layout policy are present and validated.
- This is not final visual approval. PM/user review still requires runtime capture/video at actual M01 camera scale.
Known gaps:
- V2 soldiers are not integrated into live ECS runtime yet.
- No v2 soldier playback capture/video exists yet.
- QA/HCI has not validated in-scene animation continuity, scale, alpha, edge bleed, selection readability, or mobile memory cost.
Cross-lane impacts:
- Gameplay owns `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-runtime-integration.md`.
- Art/Atlas waits unless runtime proof exposes a specific art-side issue.
- QA/HCI waits for runtime proof before validation.
Next recommended task:
- Gameplay should integrate `m01_soldier_animation_manifest_v2.json` into the M01 ECS atlas animation runtime and capture selected player rifle squad and enemy patrol playback proof for PM/user review.
