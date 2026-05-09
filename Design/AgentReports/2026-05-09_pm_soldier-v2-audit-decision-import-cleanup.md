# PM Review - Soldier V2 Audit Decision

Lane: PM
Task: Review Designer and Gameplay v2 soldier audits and route next owner
Files changed:
- `Design/AgentTasks/gameplay_current.md`
- `Design/AgentTasks/gameplay_pm_message.md`
- `Design/AgentTasks/designer_current.md`
- `Design/AgentTasks/art-atlas_current.md`
- `Design/AgentTasks/qa-hci_current.md`
- `Design/AgentReports/2026-05-09_pm_soldier-v2-audit-decision-import-cleanup.md`
Contracts touched:
- M01 soldier v2 art acceptance gate
- Gameplay import-readiness gate
- QA/HCI runtime validation gate
User-visible behavior:
- No runtime integration should proceed yet. The user should not be asked to approve v2 in Unity until import metadata/layout cleanup is complete and runtime capture exists.
Validation run:
- Reviewed `Design/AgentReports/2026-05-09_designer_m01-soldier-v2-animation-aaa-audit.md`.
- Reviewed `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-atlas-runtime-audit.md`.
Validation result:
- Designer: accept with minor notes for current M01 visual/design scope.
- Gameplay: needs manifest/layout/import fixes before integration.
- PM decision: not final accepted. Proceed to Gameplay-owned import-readiness cleanup.
Known gaps:
- Missing Unity `.meta` files for v2 soldier PNGs and manifests.
- Need explicit mobile importer settings.
- Need explicit manifest pivot, foot-anchor, contact-bounds, and normalized bounds metadata.
- Need documented atlas layout policy and bleeding-risk mitigation.
Cross-lane impacts:
- Gameplay owns `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-import-metadata-cleanup.md`.
- Designer waits; its audit result is accepted as a design audit only.
- Art/Atlas waits unless Gameplay identifies a specific art repack or visual cleanup that cannot be handled in import metadata.
- QA/HCI waits for cleanup, PM/user acceptance, and runtime capture.
Next recommended task:
- Gameplay should complete import-readiness cleanup without integrating v2 into live ECS gameplay, then report whether the package is ready for PM/user runtime acceptance or needs Art/Atlas fixes.
