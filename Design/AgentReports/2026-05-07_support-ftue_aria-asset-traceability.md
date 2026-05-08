Lane: Support/FTUE
Task: Continued from the completed assistant-panel contract by auditing ARIA asset-register coverage and adding traceability from the seven `ui.assistant` asset rows to `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`.
Files changed:
- `Design/WarlineCapture_Art_Asset_Requirements_Register.md`
- `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`
- `Design/AgentReports/2026-05-07_support-ftue_aria-asset-traceability.md`
Contracts touched: ARIA assistant art source tracking for `PREFAB-04 Assistant Button`, `PREFAB-05 Assistant Panel`, `PREFAB-06 Tutorial Card`, `PREFAB-07 Tutorial Highlight`, and `POP-10 Assistant Takeover`. No asset approval, completion, prefab path, runtime id, or UI element id was changed.
User-visible behavior: No runtime behavior changed. The art register now makes the M01 assistant-panel contract a source for ARIA portrait, assistant icon, button states, panel layer pack, tutorial card frame, highlight/path preview set, and takeover banner requirements.
Validation run:
- `ruby -rcsv` parsed `Design/WarlineCapture_Art_Asset_Requirements_Register.csv`, counted 667 rows and 7 `ui.assistant` rows, and confirmed every `ui.assistant` row still has `missing/not_reviewed/not_started` status while referencing `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`.
- `git diff --check -- Design/WarlineCapture_Art_Asset_Requirements_Register.md Design/WarlineCapture_Art_Asset_Requirements_Register.csv`
- `rg` checked the register references to `WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`, flat visual target caveat, typed path/preview target note, and no-screen-coordinate note.
Validation result: Passed. CSV remains parseable, ARIA row count stays 7, and the asset statuses remain unapproved/incomplete as required.
Known gaps: No art files were created or approved. The assistant-panel layer pack remains `missing`, and all ARIA assets remain production requirements rather than completed assets.
Cross-lane impacts: Art has clearer source traceability for ARIA requirements. UI should not treat the flat `PREFAB-05` visual target as a final layer pack. FTUE and gameplay can rely on the register note that highlight/path preview art must support typed targets, not screen coordinates.
Next recommended task: PM should dispatch the next support/FTUE task; recommended candidate is a runtime handoff spec for producing M01 `AssistantRecommendation` data from live match state.
