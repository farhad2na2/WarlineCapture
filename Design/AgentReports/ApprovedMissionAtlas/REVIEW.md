# Native Campaign and Skirmish review

These are actual Unity Game view captures of the approved implementation. PR70 remains draft and unmerged; owner visual review is required before merging.

## Campaign

Choose a chapter with its own comic cover, then select one of five large mission cards on the map. Every mission card and the matching detail preview use that mission's own existing comic.

![Native chapter overview](20261020-211119/campaign-chapters-en.png)

![Native Campaign mission selection](20261020-211119/campaign-false-front-en.png)

[Approved Campaign reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/campaign-A-five-missions-on-map-02/campaign-five-comic-missions-on-map.png).

## Skirmish

Choose one of five map groups, then browse four scrolling objective regions with six large thumbnail choices each. This provides 120 catalog slots while preserving actual published/access states. Coming-soon entries are not made playable by this redesign.

![Native map selection](20261020-211119/skirmish-maps-en.png)

![Native Skirmish mission selection](20261020-211119/skirmish-s002-en.png)

[Approved Skirmish reference](../../SkirmishUIConcepts/2026-10-20-menu-alternatives/selected-A-iteration-02-two-step/04-missions-scrolling-regions.png).

## Smaller screens and Persian

All four pages were captured and checked at 2400×1080,1920×1080 and1440×1080 in English and Persian. The input review uses Input System touches, including map swiping, page Back, mission selection and Campaign briefing/return. Text checks inspect actual glyph bounds with transparent SDF padding accounted for.

[4:3 Campaign](20261020-211119/campaign-missions-1440x1080-en.png) · [4:3 Skirmish](20261020-211119/skirmish-missions-1440x1080-en.png) · [Persian Campaign](20261020-211119/campaign-false-front-fa-IR.png) · [Persian Skirmish](20261020-211119/skirmish-s002-fa-IR.png).

The capture profile contains isolated layout/progression fixtures. It does not certify a mission. [Validation status and separate gameplay/device gates](STATUS.md).

The separate Campaign M01 journey uses Input System touches for the menu, briefing, selection drag, command controls, world destinations and result action. It reached real victories and returned in English and Persian without outcome injection. [English result](evidence/campaign/1-journey-victory.png) · [Persian result](evidence/campaign/6-journey-victory.png). The focused [34-test XML](evidence/focused-tests-34.xml) covers library/access/launch/result/medal models and main-menu navigation.

## Implementation details relevant to review

- Shared native frame, Back control, borders, backgrounds, typography, colors and primary/secondary commands.
- Exact sprite subrects are cover-cropped without stretching characters; objective icons retain their full silhouette.
- Main-page Campaign comic coverage is repaired from 14 to 25 while preserving its existing bindings. No mission borrows another mission's comic.
- Native mission-size controls survive prefab rebuilds and retain the existing launch/configuration guards.
- Campaign briefing return preserves the selected mission page; unavailable map entry cannot open a different map's mission.
- New city artwork is an illustrative menu backdrop, not a playable map. Gameplay geometry, definitions, budgets and Campaign sources remain unchanged.

[English Skirmish earned medal after return](evidence/skirmish-en/returned-card.png) · [English cold-restart proof](evidence/skirmish-en/cold-restart.json). This is a separate real normal-speed S002 ARIA victory, not the layout fixture.

[Persian Skirmish earned medal after return](evidence/skirmish-fa/returned-card.png) · [Persian cold-restart proof](evidence/skirmish-fa/cold-restart.json). Both final native S002 runs and both distinct-process medal reloads passed.
