# SCN-19 Armory Layer Pack Prompt V01

Date: 2026-05-23

## Workflow Rule

This layer pack uses generated implementation sources only. The approved target-lock reference was not cut into layers.

## Source Groups

### Background

Create one wide 21:9 opaque background image for `SCN-19 Armory` with no UI, no panels, no logo, no text, no icons, no buttons, and no overlays. The scene is a forward command tent / armory bay looking out over a Middle-East town and vehicle staging yard.

### Chrome Frames

Create a source sheet on a perfectly flat `#00ff00` chroma-key background. Generate blank reusable UI chrome pieces matching the approved target and `SCN-13 Skirmish Setup` header style:

- header logo panel background
- header resource counter panel background
- header right actions background
- title/back panel frame
- selected/default category rail button frames
- dropdown frame
- selected/default/locked roster card frames
- right inspection panel frame
- selected/default bottom tab frames
- primary, secondary, and disabled CTA frames
- empty progress meter frame
- status/counter chip frames
- route breadcrumb strip frame
- comms/status panel frame

All frames must be blank: no text, no icons, no counters, no locks, no badges, no stars, no progress fill.

### Icons And Meters

Create a separate icon/meter source sheet on a perfectly flat `#00ff00` chroma-key background. Icons must be separate sprites, not baked into frames:

- back arrow, Armory crossed weapons, category icons, resource icons, inbox, settings, dropdown, owned, locked, upgrade-ready, stats icons, ability icons, blueprint parts, source building, disabled slash, comms signal
- separate gold and olive progress fill segments
- selected glow strip

### Roster Art

Create separate rectangular content art tiles for Rifleman Male II, Marksman Male I, Assault Breacher Female II, Field Commander, Cargo Truck, Canopy Truck, Attack Helicopter, Transport Helicopter, Oil Pump, Oil Refinery, Guard Tower, and Ammunition Depot. These are rectangular image sprites, not UI frames, and must contain no text/icons/badges.

## Runtime Binding

All labels, values, states, counts, descriptions, levels, stats, abilities, CTA labels, tabs, and breadcrumb strings are live TMP/runtime-bound in Unity.
