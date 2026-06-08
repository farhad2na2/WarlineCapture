# SCN-12 District Detail / Actions Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-12_DistrictDetailActions/SCN-12_DistrictDetailActions_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-12_district_detail_actions.jpg`.
- Recreate in Unity as separate district image, stat rows, intel panels, activity rows, and action buttons.

## Implementation Notes

- Action buttons need available/disabled/locked/pressed states.
- Stat rows and intel confidence should bind directly to `DistrictState`.
- District image and minimap marker must be replaceable per selected district.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture District Detail / Actions screen, matching the premium military strategy HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target.

Scene/backdrop: Tactical district command interface with dark graphite HUD frame, cyan bevels, subtle city-map texture, low-poly district thumbnail/key art, orange threat accents.

UI layout:
- Full-screen futuristic military HUD frame with dark graphite panels, cyan edge highlights, subtle bevels, and smooth shadows.
- Top header bar: left back button, title "DISTRICT DETAIL", district name "Harbor District", red/orange tag "HIGH THREAT".
- Left large district image/map panel: low-poly urban harbor/district view, replaceable image, small inset minimap marker.
- Center stats panel: title "KEY STATS" with rows "Stability", "Civilian Trust", "Security", "Economic Output", "Population" using bars and numeric values.
- Center/right intel panel: "INTEL CONFIDENCE" with cyan progress bar, "Known Threat" panel with enemy icon cards.
- Right activity panel: title "RECENT ACTIVITY" with three log rows and timestamps.
- Bottom action grid: large touch buttons "PATROL", "DRONE SCAN", "AID", "RAID", "REPAIR", "EVACUATE", "BUILD OUTPOST" with clear icons and some locked/disabled states.

Style requirements:
- Match accepted targets: dark beveled panels, cyan highlights, blue selected states, orange/yellow warning and CTA accents, crisp readable typography, AAA mobile strategy polish.
- District image, stat rows, intel confidence, recent activity, and action buttons must read as separate Unity UI prefabs.
- No bright white borders, no hard block shadows, no cramped controls, no stretched UI, no watermark.
- Text must be legible and exactly as specified where quoted.
```
