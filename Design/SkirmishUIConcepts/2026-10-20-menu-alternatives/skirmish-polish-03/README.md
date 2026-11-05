# Skirmish mockup polish — iteration03

The owner requested the recommended refinements to the original Skirmish menu mockups. This is a design revision of the approved two-step flow: choose a map, then select from six large mission cards on the map. It does not change native prefabs, mission definitions or gameplay readiness.

## Choose a map

![Revised map selector](01-map-selection.png)

Natural terrain remains visible under restrained region tints. The selected territory uses cyan; other boundaries use steel. Large map rows and the right panel distinguish available missions, completed available missions and coming-soon slots. The five-map catalog has120 planned slots, not120 released missions.

## Select a mission

![Revised six-mission scrolling map](02-missions-scrolling-regions.png)

Six substantial cards remain in a two-column/three-row layout on the map. Each has a proposed name, smaller catalog ID, short army/start profile and distinct thumbnail concept. The selected preview matches its thumbnail. Four medal slots correspond to Recruit, Regular, Veteran and Commander; filled medals indicate earned tiers, hollow medals unearned tiers. A green check indicates at least one completed difficulty and is distinct from cyan selection. Future missions use a lock and Coming Soon without a medal row.

The current objective heading is intended to remain anchored while its mission region scrolls. The next-region label and scrollbar make the continuation visible; the fixed objective rail jumps to the same regions. Army size and selected difficulty remain visible above Start Battle. Back, navy/steel panels and blue/green command roles follow the actual Campaign reference.

## Illustrative content and arithmetic

All progress and availability in these images are **design examples**, not current catalog or player-save evidence. Proposed mission names and thumbnail art require exact catalog/content binding before native use. The new image depicts menu art, not a new physical map.

| Desert Base objective | Available | Completed available | Coming soon | Total slots |
| --- | ---: | ---: | ---: | ---: |
| Base Assault |4|3|2|6|
| Frontline Control |3|1|3|6|
| Breakthrough |2|0|4|6|
| Convoy Escort |2|0|4|6|
| Total |11|4|13|24|

The mission-screen example therefore shows3/4 complete,4 available and2 coming soon for Base Assault; the atlas shows4/11 complete and13 coming soon for Desert Base. Medal colors and counts are illustrative earned tiers, not a three-star score.

## References and provenance

- [Original map-selection mockup](../selected-A-iteration-02-two-step/01-map-selection.png)
- [Original scrolling mission mockup](../selected-A-iteration-02-two-step/04-missions-scrolling-regions.png)
- [Actual Campaign UI reference](../../../AgentReports/ApprovedMissionAtlas/20261020-211119/campaign-false-front-en.png)
- [Exact built-in ImageGen prompts, source paths and output hashes](prompts-and-manifest.json)

The first mission draft was corrected for two inconsistent available/coming-soon denominators, Combined profile labels, consistent completion checks and the strength of the map tint. The saved02 image is the corrected final mockup. Native implementation and its existing validation evidence remain a separate review artifact. Nothing is merged.
