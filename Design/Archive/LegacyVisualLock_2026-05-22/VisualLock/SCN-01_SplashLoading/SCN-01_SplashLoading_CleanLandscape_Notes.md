# SCN-01 Splash / Loading Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-01_SplashLoading/SCN-01_SplashLoading_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted Main Menu, Saga, and Settings visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-01_splash_loading.jpg`.
- Do not use this PNG as a baked Unity UI background. Recreate it with a separable background image, logo/emblem, loading bar, status text, tip text, and HUD frame assets.

## Implementation Notes

- Keep the brand/logo, loading bar, percentage, and tip line as separate canvas elements.
- The loading bar should support runtime progress binding.
- Keep the background as replaceable low-poly city key art behind interactive-safe UI layers.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Splash / Loading screen, in the same premium military strategy HUD style as the accepted WarlineCapture Main Menu, Saga Campaign, and Settings visual targets visible in this conversation. This is a new optimized landscape target, not an exact crop of the original portrait/source reference.

Scene/backdrop: Cinematic low-poly military city at dawn, Synty-style urban skyline, command center silhouette, subtle helicopters in the distance, graphite-blue atmosphere, polished mobile RTS launch screen. The background should support the UI but not make text unreadable.

UI layout:
- Full-screen dark futuristic HUD frame with graphite metal, cyan edge highlights, subtle bevels, and smooth shadows.
- Center top/mid brand area: WarlineCapture lion/emblem brand art above or beside the title "PROJECT CITY".
- Large centered title "PROJECT CITY" with a premium military RTS logo feel.
- Loading area near the lower third: label "LOADING ASSETS..." and a clear cyan progress bar at 76% with numeric "76%".
- Tip line below progress: "Tip: Upgrade your Command Center to unlock stronger squads.".
- Optional small bottom status strip with dark panel treatment, but no buttons.

Style requirements:
- Match the accepted visual targets: dark beveled panels, cyan highlights, restrained blue active elements, yellow/orange accents only for important emphasis, crisp readable typography, AAA military mobile game polish.
- Controls and labels must have generous padding and never touch borders.
- No stretched UI, no blurry text, no spec-sheet footer labels, no watermark, no explanatory captions.
- Text must be legible and exactly as specified.
```
