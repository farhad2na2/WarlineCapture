# Map cards, Campaign comics and army-size choices

The requested visual changes are implemented and pushed for owner review. Native review passes 24 page/locale/ratio cases with 40 actual-game captures in English and Persian. It checks real button clicks, thumbnail presence, map/rail color matching, glyph padding, scrollbar bounds, gear centering and stable army-size label styling after difficulty changes. Full normal-input journeys and final evidence collection are still in progress. Do not merge yet.

- Skirmish map cards show matching district crops from the existing atlas painting.
- Map/chapter headings and progress colors match between the map and left rail. Completed progress stays green; coming-soon counts stay amber.
- Campaign mission comics occupy 40% of card width with a taller crop and padded number/title/status areas.
- The real army-size buttons use bordered navy faces, blue/cyan selection, a check and locks. Standard is published; War and Large War remain disabled.
- Physical maps, mission bindings, publication gates, army budgets and gameplay rules are unchanged.

![Campaign missions with larger comics and colored chapter rail](screens/campaign-missions-en.png)

![Skirmish map cards and colored rail](screens/skirmish-maps-en.png)

![Army-size choices](screens/skirmish-s002-en.png)

A host reboot interrupted the earlier S002 validation and removed temporary logs. New full logs are stored inside this project. A scene-recovery backup was preserved outside Assets before dismissing its startup prompt. Prior attempts are retained separately; earlier wins do not certify this candidate.

The worktree includes preserved pre-existing HUD/font edits excluded from the PR. Clean-checkout, Windows, owner visual review and real player/device acceptance remain pending.
