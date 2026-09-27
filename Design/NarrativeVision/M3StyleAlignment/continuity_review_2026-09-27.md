# Campaign comic continuity review — 2026-09-27

## Accepted direction

Use the approved M3 refined faceted character style and a restrained desert color grade. Keep Samira, Dalia and ARIA on their M3 facial references. Sahrin is arid: bare tan/rose mountains, dry streets and sparse palms. Tactical cyan/red lines are electronic overlays, not water. All landscape narrative panels use a 1672 × 941 canvas. The binding rules and reusable prompt are in `visual_direction.md`.

## Changes reviewed

- CH02M03 Market Lifeline: regraded the three market scenes to reduce the strong orange cast while keeping the relief convoy, manifest and people readable.
- CH02M04 Power Relay: brought the illustrated panels into a milder grade. `DebriefCivicHandshake.png` had a one-pixel width mismatch and a generic-looking ARIA; it now uses the standard canvas and chapter 1 ARIA identity. The `PowerRelay.png` mission UI screenshot was left as authored.
- CH02M05 Route Reopened: toned the dusk scenes, removed water-like marks from the paper routing map and wet-looking streets, and corrected ARIA in `DebriefProtocolFragment.png` against the M3 hologram references.
- CH03M01 Signal Trace: reduced the saturated dusk grade and wet-looking streets. `DebriefSafehouseRoute.png` received an ARIA identity correction after a first color candidate changed her face. The `SignalTrace.png` mission UI screenshot was left as authored.
- CH03M04 Evidence Chain: lifted a desaturated set toward natural sandstone color, removed unexplained wet reflections, and expanded the unusually wide panels vertically to the standard canvas without cropping their cast or evidence. `DebriefAuditBunker.png` kept its existing color grade; its canvas alone was normalized. The 16:9 and 20:9 Unity sprite rectangles were updated while retaining sprite IDs.
- M03 Radar Warning `M03-D03.png`: removed a lake-like background feature while retaining the cast and sunset. M04 Airlift received a light grade adjustment. M05 Breach Assault's two unusually wide debriefs were expanded to the standard canvas; Samira and ARIA remain visible and recognizable. Their 16:9 and 20:9 sprite rectangles were updated while retaining sprite IDs.

## Rejected candidates and review checks

The generated ARIA variants shown during this review changed her face. The first remained a rejected candidate; the second was replaced after the user flagged it. The accepted ARIA panels were compared at face scale with `M03-B01.png`, `M03-C01.png` and `M03-D02.png`. Generated outpainting candidates were reviewed as a contact sheet and individually for lost people, medical/evidence props, geographic drift and face changes. Very wide images were expanded at the top and bottom because a center crop would remove narrative content. The one-pixel width cases were normalized without a scene redraw.

The dimension checker passes for **132 narrative landscape PNGs**, excluding portraits and dialogue UI parts: `python3 Design/NarrativeVision/M3StyleAlignment/check_comic_dimensions.py`. This count includes mission screen illustrations stored in the narrative folder; they follow the same canvas contract. The checker validates dimensions, while the visual review covers composition, identities, desert geography and color. HSV saturation summaries were used only to find outliers: Market Lifeline moved from about 0.58 to 0.41, Route Reopened from 0.56 to 0.48, Signal Trace from 0.55 to 0.48, and Evidence Chain from 0.28 to 0.36. Different day/night lighting means these numbers are not fixed acceptance targets.

This pass checked source PNGs and side-by-side contact sheets. Native comic presentation, 20:9 cropping and device acceptance still require an Editor/device review; the dimension check alone does not prove them.
