# SCN-05 Campaign Operations Target Lock V01

## Status

- Target lock: Complete
- Source style: `SCN-13 Skirmish Setup Command Base Target Lock V02`
- Intended output: Unity Canvas prefab for `SCN-05 Campaign Map`

## Visual Contract

- Keep the shared Warline Capture header unchanged across Main Menu, Skirmish, and Campaign.
- Use one full-width body layout below the header; do not add a separate page background card.
- Left: five chapter cards with Chapter I selected and Chapters II-V visibly locked.
- Center: Sahrin strategic district map with five stable mission nodes and a chapter progress strip.
- Right: Mission 01 briefing for `BLACKOUT AT SAHRIN`, with objective, risk, intel, reward, and star-goal hierarchy.
- Footer: Story Archive, Chapter Intel, and Launch Mission commands using the same command-base button geometry as SCN-13.
- Maintain generous inner padding. Text, icons, and status labels must not touch panel borders.
- Use the existing ivory, gold, olive, cyan, charcoal, and black command-base palette.

## Runtime Truth

- Main Menu Campaign opens SCN-05 and pushes Main Menu into route history.
- Back returns through ECS route history to Main Menu.
- Chapter II-V remain disabled until Campaign progression exists.
- Story Archive, Chapter Intel, and Launch Mission remain disabled until their runtime contracts exist.
- SCN-05 must not silently launch the current Skirmish/default Match path.

## Generated Production Art

- `scn05_sahrin_district_map_v01.png`: plain strategic map art used behind Canvas mission nodes.
- `scn05_blackout_relay_preview_v01.png`: plain Mission 01 preview art.
- The full target lock is reference-only and is not used as a flattened runtime background.

## Image Generation Prompt Summary

The target was generated as a premium 16:9 RTS Campaign Operations interface using SCN-13 as the visual-system reference. It preserves the shared header and replaces the page body with a five-chapter rail, strategic mission-node map, Mission 01 briefing, and command footer.
