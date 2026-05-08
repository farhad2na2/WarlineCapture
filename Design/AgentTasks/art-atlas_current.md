# Art/Atlas Current Task

Date: 2026-05-08
Status: active
Priority: P0 create Gameplay VisualLock package from approved true-isometric reference before runtime implementation

## Assignment

The user rejected the first M01 gameplay visual target package as low quality and inconsistent.

Rejected package:

- `Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-target-package.md`

Replacement package delivered:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-gameplay-visual-target-package.md`

User review result:

- Quality direction: accepted as strong.
- Perspective: rejected because the targets are not isometric.

Regenerate the package with the same high-quality bar, but with a true isometric gameplay perspective.

True-isometric replacement report delivered:

- `Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

PM/user approval received:

- "I like it. approved. I want this as refrence quality. with all the items like the image, background, map, soldiers, markers, all like this or in this style with this high quality."
- Follow-up: "Before proceed, lets have VisualLock on this for all items ... like the stratigic and tacktical maps ... all the markers and all the atlases."

This package is now the approved M01 gameplay visual quality reference. Before Gameplay resumes runtime implementation, create a locked Gameplay VisualLock package covering all visual item families.

Read first:

- `Design/AgentReports/2026-05-08_pm_art-atlas-gameplay-visual-target-rejected.md`
- `Design/AgentTasks/art-atlas_pm_message.md`
- `Design/AgentTasks/user_feedback_review_gate.md`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/README.md`
- `Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/README.md`
- UI Visual Lock quality references:
  - `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
  - `Design/VisualLockLayered/SCN-08_RTSBattleHUD/README.md`
  - `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`

## Approved Quality Bar

- Future gameplay visual targets must look like an AAA production visual target, comparable in polish to the approved true-isometric package and UI Visual Lock targets.
- The target must be true isometric gameplay reference: orthographic/isometric camera feel, consistent parallel ground-plane axes, no cinematic perspective convergence, no wide-angle camera look.
- All soldiers must be consistent in style, proportions, lighting, scale, grounding, and perspective.
- No soldier may be half buried, floating, squashed, cut off, or different-size without a clear perspective reason.
- The target must show an in-world M01 selected-readability gameplay scene, not a UI board pretending to be gameplay.
- Keep gameplay target files under `Design/VisualTargets/Gameplay/M01_SelectedReadability/`.
- Do not put gameplay target files under `Design/VisualLock/` or `Design/VisualLockLayered/`.
- Use UI Visual Lock references only for quality/style alignment.
- Include:
  - polished full gameplay visual target,
  - polished selected-state target,
  - polished move/attack marker target,
  - polished enemy readability target,
  - scale/grounding target using believable road/building/soldier relationships,
  - isometric grid/axis proof or annotation showing the ground-plane perspective is isometric,
  - pose/animation target or high-quality contact guidance,
  - rejected bad-example sheet if useful, but do not let bad examples dominate the package.
- Runtime image/background/map/soldiers/markers must match this style and quality bar before visual approval.

## Required VisualLock Package

Create a gameplay VisualLock package under:

`Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/`

It must include or define locked visual references for:

- tactical isometric gameplay map/background,
- strategic map visual style,
- road/sidewalk/building/map tile treatment,
- player rifle squad atlas style,
- enemy patrol atlas style,
- idle/run/aim/fire/death/destroyed atlas state style,
- selection, move, attack, enemy, objective, and hover markers,
- scale/grounding rules for soldiers, doors, roads, buildings, and markers,
- a manifest that names every locked file and the exact downstream usage rule.

The package must preserve the approved quality/style of:

- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Gameplay_Target.png`
- `Design/VisualTargets/Gameplay/M01_SelectedReadability/M01_SelectedReadability_Isometric_Grid_Proof.png`

Do not mix gameplay VisualLock files into UI VisualLock scene folders. Use `Design/VisualLock/Gameplay/` for gameplay locks.

## Waiting On

Waiting on lane:
none

Owner of next action:
Art/Atlas

Can my lane still continue fallback work? no. Create the Gameplay VisualLock package first.

## Completion Report

Previously delivered:

`Design/AgentReports/2026-05-08_art-atlas_m01-aaa-isometric-gameplay-visual-target-package.md`

Now write:

`Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-lock-package.md`

Use the standard WarlineCapture handoff format and include all target file paths, plus a short `User Review Steps` section.
