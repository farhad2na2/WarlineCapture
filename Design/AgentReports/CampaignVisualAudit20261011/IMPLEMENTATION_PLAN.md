# Campaign visual improvement plan

Date: 2026-10-11. Audit baseline: `c88c79c5453532a3dca9c9c2fd0c9d37603daed7`.

The largest improvement will come from making each place explain its purpose: a civic forecourt that people can walk across, a functioning bazaar, a serviced runway, a readable convoy route and recognizable protected facilities. Surface variation should support those uses. Adding noise to the existing uniform ground alone will not solve the composition or road connections.

This document proposes implementation. The audit branch contains evidence and capture instrumentation; it does not apply these art or gameplay changes. Existing Campaign physical sources remain authoritative. Apply the Campaign map reuse policy to every shared consumer; do not create additional Skirmish/Operations physical maps or change mission budgets to accommodate decoration.

## Pass 0 — Resolve native launch and placement gates

Before accepting visual polish, reproduce the failed launches and failed required-building placements listed in FINDINGS.md on this exact revision. Citywide Alert and any other Chapter 5 count mismatch must be traced from authored source through baking, packed content and readiness metadata. Compare the omitted generated identities and their owning authoring components. The count differs by 37 in the observed Citywide Alert and Network Collapse failures; that shared difference is a diagnostic lead, not proof of a common cause.

Keep `OperationMapEntityPresentationReadinessUtility` fail-closed. Use the repository's owned content builder and regenerate coherent packed content/metadata only after explaining the mismatch. Do not change an expected count merely to let a capture proceed. Preserve old failed receipts and attach a new successful native launch receipt.

For Steel Push, check `Building_SteelPush_ReserveDepot` and Barracks placement against the actual occupied grid, footprint and legal mission zone. The result reports Fuel site INTACT even while declaring defeat; trace initial Relay-approach/failure evaluation as well as missing placements before assigning causality. Inspect Supply Line's failed refinery placement separately. Restore the intended functional building rather than adding a decorative copy. Investigate recurring tent placement failures as a shared spawn/layout issue. Acceptance: intended facilities exist at legal locations, mission starts with Outcome=None, no immediate failure, objectives can be completed by normal input.

## Pass 1 — Road topology and civic/bazaar space

Pilot on the shared DenseCity source. Relevant implementation entry points are `DenseMiddleEasternCityEditModeBuilder.BakeRoadNetwork`, `BakeCivicBazaarCore`, `AddCivicRoadsideBazaar`, `AddCivicMarketPlazaDetails`, and `OtherMapSurfacePolish` road/ground passes. Do not regenerate the whole source casually; preserve existing approved physical bounds, building owners, road alignment, navigation and mission bindings.

### City hall and bazaar

1. Establish a continuous public forecourt between the hall steps and the market. Use large stone civic slabs, a restrained border and a small-sett market apron. Keep asphalt on the actual vehicle route and define its edge visibly.
2. Make every threshold physically plausible: steps meet a landing, stall feet sit on paving, paving meets a curb or a deliberately flush crossing. Remove competing overlapping rectangular patches and distinguish dirt accumulation from material seams.
3. Replace isolated cloth covers with several compact operating stall groups. Group produce baskets, counters, textile rolls, handcarts and loading crates under coordinated shade. Vary business type and canopy spacing, not arbitrary colors on every prop.
4. Preserve a clear hall entrance axis, patrol route, civilian safe route and delivery access. The builder currently reserves a 28 m hall exclusion area; inspect its purpose before reducing it. Use that space as a coherent forecourt, not an invitation to fill it with colliders.
5. Put density at storefront edges and quiet space in circulation. Keep the hall as the landmark and the protected exit legible at normal phone zoom.

### Sidewalks, intersections and road class changes

Create a small compatible family using existing dimensions: urban carriageway with curb, industrial access with a flush crossover, dirt shoulder road, T/cross junction, terminal endcap, corner return and curb-to-shoulder transition. Choose a transition per actual junction; a sidewalk must not disappear at a raw module seam.

At an urban-to-uncurbed join, return or taper the curb outside the drivable envelope, end paving with a proper cap, and blend it into compacted shoulder. At a driveway, provide a deliberate dropped curb or flush slab. At intersections, keep lane widths, centerline and height continuous; resolve median noses and corner geometry before placing markings. Do not cover a topology gap with a flat dirt decal.

Measure existing nav-agent turning envelopes and legal vehicle widths before changing geometry. Keep top surfaces flush where vehicles cross. Do not introduce collider lips, z-fighting, holes or new obstructions. Acceptance captures: both directions of every changed road class transition, all corner quadrants, and a driven convoy at the same native camera/FOV before and after.

## Pass 2 — Airport pavement and both runway ends

Fresh endpoint evidence covers DenseCity, CityEdgeAirfield and GroundedSignal. Five mesh instances form three inspected strips; ten mesh endpoints include internal joins, not ten operational thresholds. DenseCity also has a broad sand intrusion across an end-side edge. Audit assembled outer ends and internal continuity separately. `OtherMapSurfacePolish.RoadDetails.RunwaySurface` currently builds a flat surface with longitudinal edge strips and center dashes; it does not provide a complete threshold/end treatment.

Compute the endpoints from the assembled runway's transform and extents. Add threshold bars with a coherent start/stop, identifiers oriented toward the approach, terminal edge lines, shoulder material and compacted end ground. Use subtle touchdown rubber near the appropriate landing portion, not identical stains down every repeated tile. Keep markings out of taxiway crossings and use sensible hold/turn markings at real connections.

Do not paint a threshold at every modular mesh boundary. Distinguish pavement end, operational threshold and any existing blast/overrun area. A displaced threshold or added runway length requires checking the aircraft contract; do not invent it for decoration. Retain the existing physical footprint and approach clearances. Put sparse lights or markers outside the operating envelope, with a small service cluster and drainage/edge detail where visible.

Acceptance: capture and inspect both ends from approach and gameplay views; taxiway-to-apron continuity; aircraft spawn, taxi, takeoff/landing and extraction still work with normal input; no scenery intersects wings or landing wheels.

## Pass 3 — Mission-specific identity and prop grouping

Use the 25 mission entries in FINDINGS.md as the work list. Prioritize destinations the player must understand rather than generic scenery density:

| Family | Material/prop language | Preserve |
|---|---|---|
| Civic / market | Stone forecourt, small setts, shaded stalls, domestic delivery | Hall entrance, civilian passage, patrol width |
| Clinic / shelter | Maintained pedestrian entrance, shade, water, benches, grouped aid supplies | Protected identity, staff access, no military target cues |
| Military / communications | Bounded service concrete, cable runs, cabinets, generator, controlled storage | Actual functional owner, build area, command visibility |
| Refinery / reserve | Loading apron, pump/control grouping, localized spill/wheel wear | Resource chain, legal facility placement, fire/vehicle routes |
| Port / relief corridor | Quay edge, bollards, handling bays, receiving canopy, sorted cargo | Bridge/convoy envelope, protected delivery destinations |
| Residential / evidence | Doorstep paving, domestic shade, short frontage clusters, physical archive cases | Verification sequence; avoid revealing unconfirmed enemy identity |

Signal Trace, Safehouse Sweep and False Front especially need distinct view composition and activity cues on the shared source. Shared-source props must make sense for every consumer. If a mission-specific visual layer is justified, use an explicitly owned optional presentation layer with no accidental blockers, duplicated gameplay owners or changes to the shared map budget. Do not place hidden-target hints before the narrative permits them.

Make chapter five's repeated civic facilities visually distinct by function: clinic, utility plant, relay/audit room, evacuation receiving area and command core. Keep medical/civilian sites visually separate from military objectives; avoid military clutter on protected shelters.

## Pass 4 — Cameras and controlled lighting

For each opening, frame the actual controlled group plus its first useful destination in the world area left visible by the HUD. Use measured safe areas at supported landscape aspect ratios. Check after the briefing/camera transition settles and with ARIA visible. A whole-map bounds fit is not an opening camera.

For two-front missions, provide short deliberate camera-only destination views and a return to the command group. Camera guidance must not issue orders. Keep ARIA Play/Stop visible. Preserve the game's typography, command colors and existing HUD layout; this is a map/camera pass, not a UI redesign.

The first-generation diagnostic text lists disabled lights, so duplicate names are not sufficient evidence of doubled lighting. DenseCity and CityEdgeAirfield native probes confirm their Match lights are disabled. Trust Under Fire, Grounded Signal and Steel Push (RefineryDistrict) native receipts confirm both the source Sun and Match directional light are enabled. Inspect enabled state and scene ownership for each binding before changing exposure. Establish one intentional key light; retain secondary fill only if documented and visually useful. Compare before/after with unchanged materials, exposure, camera and quality profile.

For pale industrial slabs, first adjust material value, roughness and modest slab variation. Avoid darkening all maps to manufacture contrast. Keep shadows readable on mobile, stabilize shadow transitions and preserve unit/background separation. Do not use high-altitude diagnostic fog or terrain-edge artifacts as proof of a reachable player-camera problem.

## Pass 5 — Ground, rocks, vegetation and settlement edges

Use three scales deliberately: broad regional color changes, local activity/material patches, and sparse close-range detail. Break long repeated concrete/sand patterns with existing compatible material variants and controlled UV/atlas variation. Follow use: sweeping tracks at a gate, loading wear at a dock, dust against a curb, cleaner pedestrian stone near a civic entrance. Avoid evenly scattering rubbish, rocks and wear over every square meter.

Group rocks at slope feet and vegetation where water/shade or protected ground makes sense. Use a few larger silhouette groups, intermediate stones and fine edge rubble. Blend building foundations into their plots with a restrained base gradient. Keep distant existing ridgelines coherent; do not introduce new mountains into flat armored corridors. Correct sharp terrain/material seams visible within the legal camera range before adding more props.

## Implementation sequencing and evidence

Work in small reviewable changes: launch/readiness fixes; one DenseCity civic/road pilot; airport end treatment; shared ground/material pass; mission-specific props and cameras; device verification. Capture before/after with identical camera transforms, viewport, graphics API and quality. Regenerate owned bake/readiness data whenever a source change requires it, and rerun every Campaign/Skirmish/Operations consumer of that shared source.

| Gate | Required evidence |
|---|---|
| Source integrity | Asset/scene diff; unchanged logical bindings, owners, budgets, protected routes; documented bake/metadata update |
| Visual review | Native opening HUD, first objective, changed road joins, both runway ends, destination closeups; same framing before/after |
| Automated checks | Checked wrapper logs, required pass markers, no timeout/project lock, compile and map/placement/readiness checks |
| Normal-input mission | Deployment → ARIA interaction → objectives → result → return, no injected win; failed attempts retained |
| Device/player acceptance | Supported Android profiles, touch readability, back/pause flow, frame-time/memory/draw-call comparison; owner review |

Capture instrumentation and a passed screenshot receipt establish only that images were produced. They do not establish mission success or device readiness. Keep these gates separate in every change description.

Set rendering budgets from the existing device profile and measured baseline rather than inventing an unrestricted prop count. Prefer atlas reuse, instancing and LOD for repeated market/industrial details. Measure worst-case visible density with the HUD active before broadening the pilot. Any native mission load or placement failure blocks final acceptance even if a source preview looks attractive.
