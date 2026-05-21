# Art/Atlas AAA Readiness Visual Lock Revisions Imagegen Redo

Date: 2026-05-16
Owner: Art/Atlas
Status: ready for PM/user review
Priority: P0

## Lane

Art/Atlas

## Task

Redo the rejected first AAA readiness VisualLockLayered subset with imagegen-sourced target-lock visuals only:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`

Rejected pass replaced:

- `Design/AgentReports/2026-05-16_art-atlas_aaa-readiness-visual-lock-revisions.md`

Held and not touched: `POP-11`, `POP-10`, `SCN-11`, `SCN-12`, `POP-06`, Operation screens, M01 Gameplay art, runtime code, and `Assets/` imports.

## Handoff Assessment

- `Design/AgentReports/2026-05-16_pm_art-atlas-current-work-rejected-imagegen-redo.md`: accepted as P0 rejection and redo instruction.
- `Design/AgentReports/2026-05-16_pm_art-atlas-imagegen-only-heartbeat-rule.md`: accepted as mandatory production rule.
- `Design/AgentReports/2026-05-16_pm_art-atlas-aaa-readiness-first-visual-lock-dispatch.md`: accepted as routing for POP-05 and SCN-02 only.
- `Design/AgentReports/2026-05-16_pm_art-atlas-imagegen-redo-review-report-missing.md`: accepted as a handoff-completion blocker; this report is the required missing imagegen-redo handoff.
- `Design/AgentReports/2026-05-16_designer_aaa-readiness-recommendation-validation.md`: accepted as Designer/Game Design validation source.
- `Design/AgentReports/2026-05-10_pm_aaa-readiness-recommendation-approval.md`: accepted as recommendation background.

## Source References Used

- `Design/VisualLockLayered/README.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_UIUX_Mockup_Target_Alignment_Audit.md`
- `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- Current routed packages under `Design/VisualLockLayered/POP-05_MissionResult/` and `Design/VisualLockLayered/SCN-02_MainMenu/`

## Imagegen Confirmation

Confirmed: target-lock reference images, flattened review PNGs, and layer contact sheets in this redo were generated with the imagegen workflow, not deterministic local rendering, scripted compositing, manual shape overlays, HTML/CSS screenshots, pixel patching, or programmatic HUD assembly.

Selected generated source root:

- `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618`

Selected imagegen files:

- POP-05 16:9 reference: `ig_066f017118725f96016a07aa498e0081918250bef985e1ae5f.png`
- POP-05 layer contact sheet: `ig_0fcf02061a747390016a081097e92c819197b37e7ed2c4a7ee.png`
- SCN-02 16:9 reference: `ig_066f017118725f96016a080da5502481919e275ae414bfee31.png`
- SCN-02 20:9 reference: `ig_0fcf02061a747390016a08101b2cf481919b52c67917b753a4.png`
- SCN-02 layer contact sheet: `ig_0fcf02061a747390016a0810df5f448191a5d5b443d18913f2.png`

## Package Paths Revised Or Created

### POP-05 Mission Result

Revised in place:

- `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/imagegen_selected_reference.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/imagegen_layers_contact_sheet_source.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/generated_layer_atlas_alpha.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/generated_layer_atlas_chromakey.png`
- `Design/VisualLockLayered/POP-05_MissionResult/layer_manifest.json`
- `Design/VisualLockLayered/POP-05_MissionResult/README.md`

POP-05 visible review content:

- `M01 First Contact`
- `saga.ch01.m01.first_contact`
- `scenario.ch01.m01.first_contact`
- `level.ch01.district_edge_01`
- `iso.ch01.district_edge_01`
- objective complete: `Destroy hostile patrol`
- rewards: `CommanderXP`, `Credits`, `Materials`, `Intel`
- visible `City Consequence` row with `Civilian delta 0` and `Infrastructure delta 0`
- `Replay` and `Continue` button states

### SCN-02 Main Menu

Revised/created in place:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_selected_reference_16x9.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_selected_reference_20x9.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/imagegen_layers_contact_sheet_source.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/generated_layer_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/generated_layer_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`

SCN-02 visible review content:

- top strip resources: `Credits`, `Materials`, `Command Authority`
- mode cards: `Saga Campaign`, `Persistent Operation`, `Quick Custom Game`
- `Persistent Operation` card includes `District pressure rising` and `City operation risk`
- non-live route states visibly marked `Designed Unavailable`: `Inbox`, `Store`, `Events`, `Ranking`, `Command Feed`
- commander profile fallback: `Profile data pending`

## Layer Manifests Updated

- POP-05 manifest status changed to `ReadyForReview_ImagegenRedo_M01CanonicalRevision`.
- POP-05 manifest now records imagegen selected reference/contact sheet source paths and the generated source file names.
- SCN-02 manifest status changed to `ReadyForReview_ImagegenRedo_CanonicalMainMenuRevision`.
- SCN-02 manifest now records imagegen selected 16:9, 20:9, and contact sheet source paths and generated source file names.
- Both manifests explicitly mark `deterministicFinalVisualsAllowed` as `false`.

## Visual QA Notes

- POP-05 imagegen target is a high-fidelity dark glass/metal result screen and shows the canonical M01 identity row, complete objective, rewards, city consequence, and replay/continue actions in one glance.
- POP-05 selected result does not show the rejected labels `Downtown Breakthrough`, `Supply Crate`, or `Unlock Fragments`.
- SCN-02 16:9 imagegen target uses only `Credits`, `Materials`, and `Command Authority` in the top strip.
- SCN-02 16:9 imagegen target marks all five non-live routes as `Designed Unavailable`; `Command Feed` is not presented as a live message list.
- SCN-02 20:9 imagegen target preserves the same canonical resource and designed-unavailable semantics in an ultrawide review layout.
- Contact sheets are imagegen-sourced visual planning boards for implementation slicing and ownership review.

## Validation Run

- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Read `Design/AgentTasks/art-atlas_current.md`.
- Checked latest Art/Atlas-relevant handoffs in `Design/AgentReports/`.
- Generated imagegen reference targets/contact sheets.
- Copied selected imagegen outputs into routed package paths.
- Updated manifests and source notes.
- Verified PNG dimensions with `sips`:
  - POP-05 reference: `1672x941`
  - POP-05 contact sheet: `1536x1024`
  - SCN-02 16:9 reference: `1672x941`
  - SCN-02 20:9 reference: `1870x841`
  - SCN-02 contact sheet: `1536x1024`
- Parsed both `layer_manifest.json` files with `python3 -m json.tool`: passed.
- Searched routed packages for stale rejected strings: no matches.
- Follow-up heartbeat after `Design/AgentReports/2026-05-16_pm_art-atlas-imagegen-redo-review-report-missing.md`: confirmed this report exists at the required path and now explicitly responds to the PM missing-report blocker.

## Validation Result

Ready for PM/user review.

- Runtime code changed: no
- `Assets/` imports changed: no
- Non-routed VisualLockLayered packages changed: no
- POP-05 package revised: yes
- SCN-02 package revised/created: yes
- Imagegen-sourced target references present: yes
- Imagegen-sourced contact sheets present: yes
- Layer manifests present and valid JSON: yes
- Deterministic rejected visual pass replaced at review-reference/source level: yes

## Known Gaps

- Final Unity import/slicing and TMP binding should wait for PM/user approval.
- Generated UI text is suitable for target-lock review but final runtime labels should remain TMP/live text per manifest rules.
- POP-11, POP-10, SCN-11, SCN-12, and POP-06 remain held by PM routing and were not started.
