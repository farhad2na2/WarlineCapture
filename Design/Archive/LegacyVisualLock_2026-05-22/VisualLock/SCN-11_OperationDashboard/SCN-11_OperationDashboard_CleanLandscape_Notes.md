# SCN-11 Persistent Operation Dashboard Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-11_OperationDashboard/SCN-11_OperationDashboard_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-11_operation_dashboard.jpg`.
- Recreate in Unity as modular strategic-map UI with real district hit areas and data-bound metric panels.

## Implementation Notes

- District regions must be selectable Unity objects or UI hit targets, not baked pixels.
- Metric sidebar, daily briefing, active warnings, resource bar, and bottom action bar should be separate prefabs.
- Warning rows need severity states.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Persistent Operation Dashboard screen, matching the premium military strategy HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target.

Scene/backdrop: Strategic city operation command screen with a district map, dark tactical console, graphite metal HUD panels, cyan highlights, orange/yellow warning accents.

UI layout:
- Full-screen futuristic military HUD frame with dark graphite panels, cyan bevels, smooth shadows.
- Top header bar: left back button, title "OPERATION DASHBOARD", day/time text "DAY 12 / 09:00", resource counters on the right.
- Large center-left city district map viewport with colored district regions, labels, small warning/status icons, and selectable outlines.
- Left metric sidebar with vertical meters: "Region Stability", "Civilian Trust", "Threat Level", "Heat Level", "Force Readiness".
- Right panel: "DAILY BRIEFING" with short briefing text and "ACTIVE WARNINGS" list with orange alert rows.
- Bottom action bar: buttons "INTEL REPORT", "BLACK MARKET", "ARMORY", "COMMAND LOG", and a highlighted "END DAY" button.

Style requirements:
- Match accepted targets: dark beveled military HUD, cyan highlights, blue selected states, yellow/orange warning and CTA accents, crisp readable typography.
- Map regions, metric tiles, briefing panel, warning list, and bottom buttons must look like separate Unity UI parts.
- No baked static menu masquerading as UI; keep elements modular and replaceable.
- No bright white borders, no hard black block shadows, no cramped text, no stretched UI, no watermark.
- Text must be legible and exactly as specified where quoted.
```
