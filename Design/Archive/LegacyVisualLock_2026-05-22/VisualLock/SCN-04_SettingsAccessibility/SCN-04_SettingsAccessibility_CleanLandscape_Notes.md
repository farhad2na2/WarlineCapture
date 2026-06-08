# SCN-04 Settings Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-04_SettingsAccessibility/SCN-04_SettingsAccessibility_Landscape_Target.png`.
- Direction: generated AAA landscape target using the same workflow that produced the accepted Main Menu and Saga targets.
- Source references: original Settings mockup under `Design/UIUX_Codex_Package/uiux_spec_assets` plus the accepted `MainMenu_Landscape_Visual_Target`.
- Do not use `Assets/Game/Textures/Backgrounds` as a visual source for this target.
- Do not treat this PNG as a baked UI background for Unity implementation. Recreate it as split canvas sections, 9-sliced panels, buttons, sliders, toggles, dropdowns, labels, and replaceable icons.

## Implementation Notes

- Preserve the filled blue selected tab treatment.
- Preserve generous padding between controls, borders, labels, sliders, toggles, and bottom panel lines.
- Use subtle graphite/cyan beveled borders only. Avoid bright white outlines and hard black drop shadows.
- Keep all interactive elements separate in Unity so buttons, toggles, sliders, and dropdowns can have hover/pressed/disabled states.
- Keep the back button as a separate button asset, not baked into the header.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Settings & Accessibility screen, in the same premium military strategy HUD style as the existing MainMenu_Landscape_Visual_Target and the new Saga Map landscape target. This is a new optimized landscape target, not an exact crop of the portrait reference.

Scene/backdrop: Dark tactical command-interface background, subtle graphite grid and panel texture, no large illustration. The UI should feel like a polished military RTS control console.

UI layout:
- Full-screen futuristic military HUD frame with dark graphite/black panels, cyan edge highlights, restrained blue selected states, subtle bevels, soft shadows, and premium AAA mobile strategy game polish.
- Top header bar: left back button with a polished military back arrow icon matching the style of the original settings mockup, title text "SETTINGS".
- Tab row under header: "GENERAL" selected, "CONTROLS", "NOTIFICATIONS", plus one empty/disabled tab segment on the right. The selected tab should be filled blue like the original settings mockup, not just underlined.
- Main content organized for landscape mobile into four clean sections while preserving the original settings content: AUDIO, GRAPHICS, ACCESSIBILITY, LANGUAGE.
- AUDIO section: rows for "Master Volume" 80%, "Music" 60%, "SFX" 85%, "Voice" 75%. Each row has a cyan slider track, dark remaining track, circular knob, and percentage at the right. Clear spacing between rows.
- GRAPHICS section: quality buttons "LOW", "MEDIUM", "HIGH", "ULTRA" with "HIGH" selected in blue. Frame Rate row with "30 FPS", "60 FPS", "120 FPS" and "60 FPS" selected in blue.
- ACCESSIBILITY section: "Colorblind Mode" dropdown with value "Protanopia", "High Contrast UI" toggle ON, "Large Text" toggle OFF. Toggles should match the original style with rounded dark/blue track and circular knob, but polished for landscape.
- LANGUAGE section: "Language" dropdown with value "English".

Style requirements:
- Match MainMenu_Landscape_Visual_Target style: premium military RTS UI, dark beveled panels, cyan highlights, blue active controls, realistic soft shadows, crisp readable typography, strong hierarchy.
- Also respect the original SCN-04 settings mockup style: back button silhouette, tab button style, section panels, slider/toggle/dropdown treatment.
- Do not invent bright white borders. Borders should be dark metal/cyan-gray, thin, beveled, and subtle.
- Shadows must be soft and smooth, not hard black blocks.
- Controls must not touch borders or each other. Text, sliders, knobs, toggles, and percentages need generous padding and clear spacing.
- Landscape mobile composition with safe margins and no stretched UI.
- Text must be legible and exactly as specified.
- No spec-sheet footer labels, no captions, no explanatory text, no watermark.
```
