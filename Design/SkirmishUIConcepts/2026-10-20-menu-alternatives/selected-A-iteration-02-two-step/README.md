# Skirmish — map first, then six big missions

**Campaign A is accepted.** Skirmish A's small side mission list is rejected. The owner wants to choose a map first, then choose from six substantial individual mission targets per page. Further visual iteration is pending; no native menu implementation changed.

## Shared first step: choose the map

[Full image](01-map-selection.png)

![Choose map](01-map-selection.png)

Keep the A atlas with five adjacent Operations-style map zones and large map-selector rows. The right panel shows the selected map, its24 mission slots and four objective families; it has no individual mission selector. **CHOOSE MISSIONS** opens a separate page.

## Second step: choose a mission

Each map contains four objective families of six missions. A selected family is a page: six large targets, a visible selected mission briefing and clear launch control. Four pages ×six =24 per map; five maps ×24 =120 catalog entries. Future entries stay coming soon according to the native catalog. Switching family and previous/next page reach the same four groups.

| Mission-screen idea | Full image | Assessment |
| --- | --- | --- |
| 1 — Large cards | [Open](02-missions-large-cards.png) | Clearest mission choice and largest thumbnail/touch targets; simple two-row, three-column grid |
| 2 — Six map sectors | [Open](03-missions-six-sectors.png) | Closest Operations map feel; each of six adjoining navigation zones is a substantial mission target |
| 3 — Scrolling objective regions | [Open](04-missions-scrolling-regions.png) | One six-mission family fills the map viewport; scroll to the next family or jump using the fixed type rail |

### 1 — Large cards

![Six large mission cards](02-missions-large-cards.png)

### 2 — Operations-style mission sectors

![Six large mission sectors](03-missions-six-sectors.png)

### 3 — Scrolling objective regions

![Scrolling map with six large targets per family](04-missions-scrolling-regions.png)

## Shared UI and content constraints

Preserve the Operations Back button, button palette/roles, dimensions, borders, panel backgrounds, typography and spacing. Back on the mission screen returns to map selection. Keep mission purpose and next action visible. Generated art, example progress/medals and concise mission copy illustrate layout; the native implementation must bind exact existing mission content, ready states, ledger and launch authority. Sector/map areas are menu navigation overlays and do not create physical Campaign maps or alter mission gameplay.

[Campaign A accepted reference](../A-campaign-territory-atlas.png) · [Exact ImageGen prompts and hashes](prompts-and-manifest.json). Earlier single-screen small-drawer iterations are historical. Runtime implementation and merging remain on hold for this design iteration.
