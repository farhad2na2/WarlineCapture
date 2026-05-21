# Art/Atlas AAA Readiness Visual Lock Revisions

## Lane

Art/Atlas

## Task

Revise the first PM-routed AAA readiness VisualLockLayered subset only:

- `Design/VisualLockLayered/POP-05_MissionResult/`
- `Design/VisualLockLayered/SCN-02_MainMenu/`

Held and not touched: `POP-11`, `POP-10`, `SCN-11`, `SCN-12`, `POP-06`, M01 Gameplay art, runtime code, and `Assets/` imports.

## Handoff Assessment

- `Design/AgentReports/2026-05-16_designer_aaa-readiness-recommendation-validation.md`: accepted as Designer/Game Design validation source.
- `Design/AgentReports/2026-05-10_pm_aaa-readiness-recommendation-approval.md`: accepted as the recommendation background; only the PM-routed subset was executed.
- `Design/AgentReports/2026-05-16_pm_art-atlas-aaa-readiness-first-visual-lock-dispatch.md`: accepted as routing for POP-05 and SCN-02 only.

## Source References Used

- `Design/VisualLockLayered/README.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_UIUX_Mockup_Target_Alignment_Audit.md`
- `Design/WarlineCapture_Gameplay_North_Star_And_Content_Grammar.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- Existing high-quality visual references under `Design/VisualLock/MainMenu/` and `Design/VisualLock/POP-05_MissionResult/`

## Package Paths Revised Or Created

### POP-05 Mission Result

Revised in place:

- `Design/VisualLockLayered/POP-05_MissionResult/reference/POP-05_MissionResult_Landscape_Target.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/generated_layer_atlas_alpha.png`
- `Design/VisualLockLayered/POP-05_MissionResult/generated_one_go/source/generated_layer_atlas_chromakey.png`
- `Design/VisualLockLayered/POP-05_MissionResult/layers/*.png`
- `Design/VisualLockLayered/POP-05_MissionResult/layer_manifest.json`
- `Design/VisualLockLayered/POP-05_MissionResult/README.md`
- `Design/VisualLockLayered/POP-05_MissionResult/prompts/high_end_target_and_layers.md`

POP-05 now uses M01 canonical content:

- `saga.ch01.m01.first_contact`
- `scenario.ch01.m01.first_contact`
- `level.ch01.district_edge_01`
- `iso.ch01.district_edge_01`
- objective complete: `Destroy hostile patrol`
- rewards: `CommanderXP`, `Credits`, `Materials`, `Intel`
- visible civilian/district consequence row with neutral tutorial zero-delta outcome
- replay and continue button states

### SCN-02 Main Menu

Created:

- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_20x9_Target.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/generated_layer_atlas_alpha.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/generated_one_go/source/generated_layer_atlas_chromakey.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layers/*.png`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_manifest.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/README.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py`
- `Design/VisualLockLayered/SCN-02_MainMenu/prompts/high_end_target_and_layers.md`

SCN-02 now exposes:

- wallet resources: `Credits`, `Materials`, `Command Authority`
- mode cards: `Saga Campaign`, `Persistent Operation`, `Quick Custom Game`
- Persistent Operation subtitle framed around district/city operation pressure
- designed-unavailable badges/copy for `Inbox`, `Store`, `Events`, `Ranking`, and `Command Feed`
- commander profile fallback placeholder when `PlayerProfileState` is not live

## Layer Manifests Updated

- POP-05 manifest now maps canonical M01 result content, reward types, consequence row, buttons, stat frames, reward cards, icons, and TMP/live-text rules.
- SCN-02 manifest defines 16:9 and 20:9 references, wallet resources, mode cards, designed-unavailable routes, commander fallback state, icons, card art, frames, badges, and dynamic data bindings.

## Visual QA Notes

- POP-05 target keeps high-end modal/background direction while replacing stale mission/reward content with M01 First Contact result content.
- POP-05 consequence row is visible as a first-class field.
- SCN-02 16:9 and 20:9 review targets both show the three canonical top-strip resources without alternate third-resource semantics.
- SCN-02 non-live route states are visible rather than silent/inert.
- Existing target-style dark glass/metal, cyan trim, and amber accent language is preserved.
- TMP text is marked live in package rules; reusable layers are separated from labels and values in manifest intent.

## Validation Run

- Read current Art/Atlas task and heartbeat instructions.
- Checked latest relevant reports in `Design/AgentReports/`.
- Read Designer validation, PM recommendation approval, VisualLockLayered README, UI/gameplay alignment docs, north star, and M01 production contract.
- Inspected existing POP-05 package and confirmed SCN-02 package was missing before creation.
- Generated/revised reference PNGs, layer PNGs, contact sheets, source atlas images, manifests, README/source notes, prompts, and SCN-02 dry-run copy helper.
- Verified PNG dimensions with `sips`:
  - POP-05 reference: `1672x941`
  - SCN-02 16:9 reference: `1672x941`
  - SCN-02 20:9 reference: `2000x900`
- Parsed `layer_manifest.json` for both packages with `python3 -m json.tool`.
- Ran dry-run copy helpers:
  - `python3 Design/VisualLockLayered/POP-05_MissionResult/copy_layers_to_unity.py`
  - `python3 Design/VisualLockLayered/SCN-02_MainMenu/copy_layers_to_unity.py`
- Searched routed packages for stale rejected strings and found no remaining occurrences.

## Validation Result

Ready for PM/user review.

- Runtime code changed: no
- `Assets/` imports changed: no
- Non-routed VisualLockLayered packages changed: no
- POP-05 package revised: yes
- SCN-02 package created: yes
- 16:9 SCN-02 target: yes
- 20:9 SCN-02 target: yes
- Layer manifests present and valid JSON: yes
- Contact sheets present: yes
- Dry-run copy helpers available: yes

## Known Gaps

- The reference targets are review/implementation guidance, not imported runtime UI.
- Final Unity import/slicing and TMP binding should wait for PM/user approval.
- POP-11, POP-10, SCN-11, SCN-12, and POP-06 remain held by PM routing and were not started.
