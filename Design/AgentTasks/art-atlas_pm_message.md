# PM Message For Art/Atlas

Date: 2026-05-08

The user rejected the gameplay visual target package. Treat this as a hard art-direction failure, not a small revision.

Rejected issues:

- visual quality is far below the UI Visual Lock mockups,
- inconsistent soldier sizes,
- some soldiers appear half underground,
- placeholder/low-quality assets,
- inconsistent style and grounding,
- not an AAA AI-generated mockup target.

Create a new gameplay-only AAA visual target package under:

`Design/VisualTargets/Gameplay/M01_SelectedReadability/`

Use UI Visual Lock targets only as quality/style references:

- `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD/generated_one_go/layers_contact_sheet.png`

The new package must look like a high-quality production mockup target, not a placeholder board.

Expected report:

`Design/AgentReports/2026-05-08_art-atlas_m01-gameplay-visual-lock-package.md`

Latest user review of the AAA replacement:

- The visuals look great.
- They are rejected because they are not isometric.

Keep the quality bar, but regenerate the package as true isometric gameplay reference:

- orthographic/isometric camera feel,
- consistent parallel ground-plane axes,
- no cinematic perspective convergence,
- no wide-angle camera look,
- add an isometric grid/axis proof or annotation.

The true-isometric package is now approved. Before Gameplay resumes, create a gameplay VisualLock package under:

`Design/VisualLock/Gameplay/M01_ApprovedIsometricGameplay/`

It must lock all item families:

- strategic map,
- tactical map/background,
- road, sidewalk, building, and map tile treatment,
- player rifle squad atlas,
- enemy patrol atlas,
- idle/run/aim/fire/death/destroyed atlas states,
- selection, move, attack, enemy, objective, and hover markers,
- scale and grounding rules.

Include short user review steps. Do not commit or push.
