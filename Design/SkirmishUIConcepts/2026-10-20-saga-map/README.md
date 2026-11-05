# Skirmish library: saga-map direction (owner-approved 2026-10-20)

This replaces the grid layouts in the 2026-10-14 battle library design (`../2026-10-14/`) with a saga-map mission path.

| Screen | Approved mockup | Replaces |
| --- | --- | --- |
| Battlefield | `skirmish-saga-battlefield-v02.jpg` | approved-3 Grid view, the MAP/GRID toggle, and the six pinned cards of approved-2 |
| Theatre | `skirmish-saga-theatre-v01.jpg` | the 4×6 medal grid in approved-1's right panel |

## What was approved

- **Battlefield:**
  - One winding road across the battlefield plate with 24 round mission stops in catalog order (BA, FC, BT, CE × G/A/C × F/E).
  - A banner flag starts each objective chapter (Base Assault, Frontline Control, Breakthrough, Convoy Escort).
  - Stop states:
    - completed stops show their earned medals;
    - the selected stop glows with a marker;
    - NotOwned stops show a lock with a store tag.
  - The left objective tabs jump to their chapter on the road.
  - The briefing panel, medal row and footer (Back, Theater Map, Start Battle, Quick Battle) are unchanged from approved-2.
  - There is no Grid state and no MAP/GRID toggle.
- **Theatre:**
  - A dotted route links the five battlefields in order on the theatre map.
  - The right panel shows each objective's medals as a short trail instead of a grid.
  - Everything else stays as in approved-1.
- **No locking by order.** Every owned, Playable mission stays selectable. The road shows the suggested order and progress only. Locks mean NotOwned or unavailable, exactly as in the original brief. "Medals never gate content" still holds.

## Notes for implementation

- **Mockup text is illustrative.** The generator garbled some numbers and one banner label. Stop numbers, labels and the briefing come from catalog data as native localised TMP (EN/FA, RTL-safe).
- **Stop positions are authored data per battlefield** (24 points along a spline over the plate), not baked into art. The road is drawn at runtime or as a separate overlay, not painted into the plate.
- **Touch targets.** Stops must keep mobile touch sizes at 4:3. If 24 stops don't fit, scroll or pan the road, or focus one chapter at a time. Validate this at 16:9, 20:9, 4:3 and 20:9-notch in EN and FA.
- **Rejected or superseded variants:**
  - `skirmish-saga-battlefield-v01.jpg` was still card-based;
  - `skirmish-saga-theatre-v02.jpg` had a cramped side trail and wrong labels.
