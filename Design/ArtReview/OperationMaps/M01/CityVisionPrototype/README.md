# Sahrin City Vision Prototype

Status: Visual direction candidate. Not an approved production map or gameplay asset.

Purpose: Establish the visual and spatial target for a large Sahrin city before generating or rebuilding any 3D environment.

## First Marble Prototype

- World: [Middle Eastern Urban Landscape](https://marble.worldlabs.ai/world/d456d44d-4d81-47db-a262-b667f3ac3d76)
- Generator: World Labs Marble 1.1 multi-image mode
- Inputs: the three generated city-vision images in this directory
- Cost: 1,600 Marble credits
- Account balance after generation: 5,400 credits
- Decision: retain as a city-shell and visual-prototype candidate; do not export or import as production gameplay geometry yet.

The first world successfully preserves the mountain basin, large urban extent, dense low-rise blocks, palms, market color accents, water-tower silhouette, and continuous primary streets. At normal high-angle RTS distance it communicates a massive, coherent Sahrin far more effectively than free placement of demo-scene objects.

Closer inspection shows the expected reconstruction limitations: soft surface detail, view-dependent geometry, reduced low-poly readability, and insufficient confidence in road collision, building boundaries, doors, alleys, and ground contact. It is appropriate for the near/distant city shell, cinematics, skyline reference, and modular reconstruction blueprint. It is not approved for unit navigation, collision, cover, mission anchors, or the playable district.

## Reference Authority

Use the references in this order when they disagree:

1. `Sahrin_CityVision_TopDown.png` owns roads, district placement, tactical route, clinic, damaged choke point, and utility compound locations.
2. `Sahrin_CityVision_Gameplay_A.png` owns city density, material richness, lighting, skyline, and primary gameplay-camera quality.
3. `Sahrin_CityVision_Gameplay_B.png` owns the alternate high-angle camera target and confirms that the district must remain coherent from another direction.
4. `Assets/Game/Art/Narrative/FirstLaunch/Panels/16x9/FL-P01.png` remains the authority for WarlineCapture's low-poly visual identity and the civilian character of Sahrin.

The generated views are concept references, not exact projections of one existing 3D mesh. Do not ask a reconstruction tool to reconcile every small facade or prop across all views. Preserve the top-down road/block plan and the perspective views' visual language.

## Intended World Layers

### Playable District

- Main east-west road and connected side roads.
- Western JRC arrival and staging area.
- Central Old Market blocks and first cover locations.
- Eastern localized damaged choke point.
- Northeast clinic and civilian service area.
- Southeast utility/relay compound.
- Clean collision, pathing, cover, mission anchors, and complete modular buildings.

This layer must eventually be rebuilt or validated as deterministic game geometry. An AI reconstruction mesh must never become gameplay authority without cleanup and validation.

### Near City Shell

- Surrounding residential, commercial, service, and courtyard blocks.
- Visible from normal gameplay cameras but not required to support detailed tactical interaction.
- Lower collision complexity and aggressive LOD/occlusion treatment.

### Distant City

- Civic domes and towers, dense skyline, palms, utility silhouettes, mountains, and atmospheric depth.
- Visual-only world shell, optimized mesh, impostors, or another non-gameplay representation.
- No unit navigation, mission anchors, or detailed per-building simulation.

## World-Generation Prompt

```text
Generate a persistent, explorable 3D world representing Sahrin, a large fictional Middle East-inspired city in a warm mountain basin.

Use Sahrin_CityVision_TopDown.png as the spatial authority. Preserve its continuous east-west main road, connected side roads, central market blocks, western staging courtyard, northeast clinic and civilian area, eastern damaged choke point, southeast utility compound, and dense surrounding residential blocks.

Use Sahrin_CityVision_Gameplay_A.png and Sahrin_CityVision_Gameplay_B.png as visual and camera references. Preserve the handcrafted urban density, warm early-morning light, mountain skyline, market awnings, palms, rooftop water tanks, cables, worn roads, compacted dirt, sparse vegetation, courtyards, utility structures, and localized damage.

Use FL-P01.png as the style authority: cohesive stylized low-poly/faceted 3D forms, readable silhouettes, sandstone and weathered-plaster architecture, restrained terracotta and faded teal accents, and a living civilian city rather than a generic battlefield.

The city must be coherent from a high-angle perspective RTS camera. Roads must be connected and correctly joined. Buildings must be complete exterior structures resting on the ground. Maintain plausible scale for doors, people, vehicles, streets, walls, palms, and civic landmarks.

Create visual density around a readable central tactical corridor. The city should feel massive beyond the playable district, but only one localized area should show rubble and emergency damage. The remainder of the city should appear inhabited and functional.

Avoid isometric board-game presentation, disconnected roads, abrupt road endings, exposed interiors, missing facades, floating geometry, buried objects, intersecting buildings, repeated procedural blocks, uniform prop scatter, flat terrain colors, real flags or organizations, graphic casualties, UI, labels, arrows, grids, and total devastation.
```

## Generation Order

1. Block the road network and major parcels from the top-down authority.
2. Generate the central market, clinic, damaged choke point, utility compound, and western staging area.
3. Expand surrounding city blocks while retaining the road network.
4. Add the distant skyline, civic landmarks, palms, utilities, and mountains.
5. Export a preview world and inspect it from both high-angle gameplay cameras.
6. Export 3D only after the visual world is accepted.

## Acceptance Gate

- The city resembles the same Sahrin established by `FL-P01`.
- The main road and all visible intersections are connected.
- The central route remains readable at gameplay camera height.
- No exposed interiors, floating objects, buried objects, or intersecting structures are visible.
- Damage is localized and the rest of the city feels inhabited.
- Clinic, market, water tower, utility compound, and mountains provide stable orientation.
- The generated world looks coherent from at least two high-angle perspective camera directions.
- A raw generated mesh is not imported into the production Match scene.

## Known Concept Issue

The top-down concept contains two water-tower-like structures. Treat the western tower as the primary landmark. The smaller eastern utility structure may remain only if it reads as a different utility tank rather than a duplicate landmark.
