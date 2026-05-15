# Art/Atlas M01 Step-By-Step Gameplay Mockups

## Lane

Art/Atlas

## Task

Revise the tiny AAA layered M01 approval sample after Designer alignment review and Gameplay implementation-readiness audit.

## Handoff assessment

- `Design/AgentReports/2026-05-14_designer_m01-step-by-step-gameplay-spec.md`: accepted as gameplay source of truth.
- `Design/AgentReports/2026-05-14_designer_m01-art-sample-alignment-review.md`: accepted; prior sample needed camera/zoom, state, objective, Build, and enemy-affiliation fixes.
- `Design/AgentReports/2026-05-14_gameplay_m01-mockup-asset-implementation-audit.md`: accepted; LayerPack needed asset-prep metadata and implementation ownership clarity.
- `Design/AgentReports/2026-05-14_pm_art-atlas-combined-design-gameplay-feedback.md`: accepted as current PM routing.
- `Design/AgentReports/2026-05-14_pm_art-atlas-unit-scale-feedback.md`: accepted; player/enemy infantry scale comparison was added to the LayerPack.
- `Design/AgentReports/2026-05-14_gameplay_m01-corrected-sample-asset-audit.md`: accepted; M01-02 selected markers needed explicit per-soldier visible marker layers.
- `Design/AgentReports/2026-05-14_pm_art-atlas-selected-marker-audit-routing.md`: accepted as the current focused Art/Atlas correction.
- `Design/AgentReports/2026-05-14_pm_art-atlas-selected-marker-style-rejection.md`: accepted; rejected thick hand-painted blue circles were replaced with markers composited from the clean `selection_ring.png` source.
- `Design/AgentReports/2026-05-14_pm_art-atlas-original-reference-marker-healthbar-rejection.md`: accepted; the prior clean-marker pass still missed the original selected-squad shield/status bar, enemy red readability, and higher-fidelity HUD chrome.
- `Design/AgentReports/2026-05-14_pm_art-atlas-latest-reference-comparison-rejection.md`: accepted; the local composite pass still missed original HUD quality, command order, Star Goals density, minimap/card polish, and selected status treatment.
- `Design/AgentReports/2026-05-14_pm_art-atlas-imagegen-only-marker-rejection.md`: accepted; flattened visual targets must be fresh imagegen frames, not deterministic marker/UI patching.

## Files changed

- Replaced corrected sample review images with fresh imagegen outputs resized to required project resolution:
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- Replaced corrected sample contact sheet:
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- Updated layered implementation package:
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-01_TacticalStart_layers.json`
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/Frames/M01-02_SquadSelected_layers.json`
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/SourceNotes.md`
- Added asset-prep metadata:
  - `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/CameraLock_M01_DefaultStart.json`
- Replaced this handoff report.

## User-visible behavior

No runtime behavior changed. No source docs, other lane task files, Unity runtime files, or `Assets/` imports were modified.

## Output summary

- Sample scope only: 2 frame targets plus 1 contact sheet.
- M01-01 and M01-02 now use fresh cohesive imagegen target-lock frames with matching tactical camera direction, HUD family, and unit scale.
- M01-01 reads as no selection: no selected ring, no command mode, no move/attack/objective/invalid world marker, neutral/disabled commands, M01-only objective, Build disabled.
- M01-02 reads as selected but not in command mode: selection rings and selected squad/card state are visible, command controls are enabled/readable, and no Move/Attack highlight or command banner is present.
- M01-02 now includes four imagegen-native cyan segmented per-soldier selected marker rings aligned to the selected squad feet.
- M01-02 now includes the original-reference selected-squad world status treatment: a blue/cyan shield icon plus segmented blue horizontal status/health bar above the selected squad, with no extra squad-name label.
- Enemy red readability is now present in the flattened samples and metadata: restrained red foot rings plus red segmented above-head health bars are permanent unit-affiliation UI, not stateful target markers.
- HUD chrome was regenerated in both imagegen frames for the original reference quality target: restrained beveled dark glass/metal panels, Star Goals density, command order `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`, intentional secondary Build treatment, richer squad tray/card trim, and polished minimap frame/viewport direction.
- Player and enemy infantry are normalized to the same isometric projection scale; rect differences are documented as formation spread rather than distance shrink.
- Remaining M01 frames were intentionally not generated pending Designer/PM/user approval.

## Review entry points

- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01_StepByStepGameplay_SampleContactSheet_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-02_SquadSelected_1920x1080.png`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/manifest.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/CameraLock_M01_DefaultStart.json`
- `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/LayerPack/AssetPrep_M01_Sample.json`

## Validation run

- Read `Design/AgentTasks/art-atlas_current.md`.
- Read `Design/AgentTasks/art-atlas_heartbeat.md`.
- Checked recent `Design/AgentReports/` handoffs.
- Read Designer alignment review, Gameplay asset-prep audit, and PM combined feedback.
- Regenerated both sample frames with the imagegen skill as cohesive AAA bitmap targets using the original VisualLock HUD/marker references.
- Checked sample frame/contact sheet dimensions with `sips`.
- Parsed all LayerPack JSON files with `python3 -m json.tool`.
- Confirmed `Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/` contains only the corrected sample review images and sample contact sheet.
- Spot-reviewed the revised sample contact sheet.
- Spot-reviewed `M01-02_SquadSelected_1920x1080.png` after the imagegen pass and confirmed four visible cyan selected marker rings are present as integrated scene elements.
- Applied the PM original-reference rejection in a fresh imagegen frame: selected-squad shield/status bar, enemy red affiliation rings/health bars, Star Goals density, command order, and refreshed HUD chrome/button/minimap treatment.
- Applied the PM imagegen-only rejection: regenerated the flattened gameplay frame artwork with the imagegen skill instead of deterministic overlay/composite patching.
- Generated source files retained by Codex:
  - M01-01: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_03458752e3125a38016a060e66b2408191bbd3e0692ea215f4.png`
  - M01-02: `/Users/farhad/.codex/generated_images/019e0857-c8b1-7813-a48e-bcd2dda90618/ig_03458752e3125a38016a060dfbbf608191b5621d93b4ab0122.png`

## Validation result

Ready for Designer/PM/user review.

- Corrected sample frame count: 2
- Corrected sample contact sheets: 1
- Sample PNG dimensions: `1920x1080`
- LayerPack manifest present: yes
- Per-frame layer manifests present: 2
- Camera lock metadata present: yes
- Asset-prep metadata present: yes
- Per-soldier selected marker child layers in M01-02: 4
- Selected-squad world status child layers in M01-02: 2
- Enemy affiliation/readability layers in each frame: 8
- HUD chrome groups updated in LayerPack: objective panel, threat feed, squad tray, command bar, Build button, minimap panel
- Imagegen-only visual pass completed: yes
- Command order corrected in target/metadata: `SELECT`, `MOVE`, `ATTACK`, `STOP`, `HOLD`
- Star Goals restored in target: yes
- Rejected ugly marker pass retained: no
- Runtime/project import changes: none
- Gameplay and QA/HCI routing: held

## Known gaps

- This is an approval sample, not the full step-by-step frame set.
- The flattened PNGs are visual references only. Gameplay still needs approved clean runtime source layers before implementation.
- Text in the generated target is visual-reference only; runtime UI must use native TMP text and separate icons/counters/labels.
- Optional `M01-05_AttackPreview_1920x1080.png` was not generated because the current task restricts Art/Atlas to the first 1-2 sequence revision.

## Next recommended task

Designer/PM/user should review this corrected shared-camera sample. If approved, Gameplay may implement only `M01-01_TacticalStart` from the approved layered package before any full-sequence expansion.
