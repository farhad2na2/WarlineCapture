# Skirmish battle library visual direction — 2026-10-14

ImageGen review mockups for replacing the 120-entry scrolling Skirmish list. Implementation scope and work packages: [IMPLEMENTATION_HANDOFF.md](IMPLEMENTATION_HANDOFF.md). These are **visual concepts**, not native Unity screens, map captures or player-readiness evidence. All counts, medals and text are illustrative; generated lettering is not authoritative. Bind every label and state to runtime data and native localized text.

## Owner decision

| Question | Decision |
|---|---|
| Browsing | **Approved:** Theater map → battlefield zoom, with the War Table grid as a toggle on the battlefield level. |
| Progression | **Approved:** One medal per scenario per difficulty (Recruit bronze, Regular silver, Veteran gold, Commander red star). |
| Not selected | Skirmish/Operations rank ladders, combined Command Rank, main-menu mode-card progress, Operations city-liberation progress view. |

## Approved mockups

| Level | File | Content |
|---|---|---|
| Theater | [approved-1-theater-map.jpg](approved-1-theater-map.jpg) | Five battlefield regions with won/24 rings; medal summary and Recommended Next; selected battlefield summary; Back, Filters, Zoom In, Quick Battle. |
| Battlefield — map view | [approved-2-battlefield-map-view.jpg](approved-2-battlefield-map-view.jpg) | Objective tabs (6 cards per objective: 3 army profiles × 2 starts); pinned cards with four medal slots; MAP/GRID toggle; selected briefing; Start Battle. |
| Battlefield — grid view | [approved-3-battlefield-grid-view.jpg](approved-3-battlefield-grid-view.jpg) | 4 objective rows × 6 army/start columns for one battlefield, battlefield tabs and selected briefing. In production this is the GRID state of the battlefield level, not a separate route. |

Known generation defects: approved-2 medal fill states do not match the intended per-card examples; approved-3 shows a separate battlefield tab strip and Favorites button that should be reconciled with the toggle layout during implementation.

## Constraints for implementation

- Ranks or medals must not gate scenarios; all published scenarios remain selectable ([BATTLE_CATALOG.md](../../Roadmap/Skirmish_Expansion/BATTLE_CATALOG.md), [MATCH_SETUP.md](../../Roadmap/Skirmish_Expansion/MATCH_SETUP.md)). Medals are tracked per scenario + difficulty; Custom runs do not earn them.
- The theater plate is an illustrated collage, not one continuous playable map. Battlefields use existing Campaign sources per the [map reuse policy](../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md); card previews come from each scenario's actual source capture, and player copy reflects actual geography (for example MP does not imply mountains).
- Show unavailable/unowned states per the [mission product contract](../../Monetization/Mission_Product_Contract_2026-09-28.md) (S001 free, S002–S120 core package) and do not advertise unvalidated catalog entries as playable.
- Reuse existing Warline typography, frames and command button colors from the SCN-05 Campaign District Atlas. Restore filters, zoom level and selection after returning from a match.

## Exploration history

Retained in [exploration/](exploration/): rank-panel theater variant, first battlefield zoom, main-menu mode progress, rank ladder, and Operations city progress.

Style references: native Campaign `NativeLayoutQA/chapter-02-2400.png`, current main menu and Skirmish Setup captures from S005 evidence, SCN-03 Commander Profile V3 target, and the O001 Operations dashboard capture.
