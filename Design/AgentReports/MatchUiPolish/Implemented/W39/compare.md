# W39 native vs mockup: Board 1 (left panel spacing), Board 3 (ARIA panel states)

These are native captures of the shipped `SCN08_MatchHudContent` prefab. They were made in Play Mode by `MatchHudSidePanelsCapture` on the Windows UI lab checkout. The fixture models are the same before and after.

- Before: `before/`, from unmodified UI code at 486c27cdd + cdcf889c7 (fixture only).
- After: `after/`, from the W39 implementation.
- Each folder has 12 PNGs: 3 states × EN/FA × 1920×1080/2400×1080.
- Each folder's `geometry.md` gives measured screen pixels and the rule check. Before: 156 rule failures (not enforced). After: 0 failures (enforced).

This is a static fixture. It does not show mission readiness, and the battlefield is a flat background. The ARIA journeys are recorded separately in the W39 results note.

Live native frames from the W39 ARIA regression rerun (1920×1080) are in `live/`:
- [S002 EN](live/s002-en-opening.png)
- [S031 EN](live/s031-en-opening.png)
- [S043 FA](live/s043-fa-opening.png): the active one-action state, with Field Guide above a red Stop ARIA, the minimap 24 px above the roster, and the shared 16 px inset.

## Side by side

| State | Mockup | Before (EN 1920) | After (EN 1920) | After (FA 1920 / EN 2400 / FA 2400) |
|---|---|---|---|---|
| Two actions + tall selected card + category cards | [left-panels-spacing.png](../../left-panels-spacing.png), [aria-panel-states.png](../../aria-panel-states.png) (left) | [before](before/aria-two-actions-en-1920.png) | [after](after/aria-two-actions-en-1920.png) | [fa](after/aria-two-actions-fa-IR-1920.png) · [en wide](after/aria-two-actions-en-2400.png) · [fa wide](after/aria-two-actions-fa-IR-2400.png) |
| One action + paginated expanded-Skirmish roster (8 groups) | [aria-panel-states.png](../../aria-panel-states.png) (middle) | [before](before/aria-one-action-en-1920.png) | [after](after/aria-one-action-en-1920.png) | [fa](after/aria-one-action-fa-IR-1920.png) · [en wide](after/aria-one-action-en-2400.png) · [fa wide](after/aria-one-action-fa-IR-2400.png) |
| Stopped (no guidance copy) | [aria-panel-states.png](../../aria-panel-states.png) (right) | [before](before/aria-stopped-en-1920.png) | [after](after/aria-stopped-en-1920.png) | [fa](after/aria-stopped-fa-IR-1920.png) · [en wide](after/aria-stopped-en-2400.png) · [fa wide](after/aria-stopped-fa-IR-2400.png) |

## Measured change (1920×1080; 2400×1080 is identical)

| Rule (approved spec) | Before | After |
|---|---|---|
| Card / minimap / first category card left inset 16 px | 13.8 / 17.2 / 16.1 | 16.0 / 16.0 / 16.1 |
| Selected card → minimap ≥ 24 px | 13.8 | 24.0 (card height-limited) |
| Minimap → cards 24 px | 27.5 (categories); 107.9 (expanded roster) | 24.0 / 24.0 |
| Gap between category cards 8 px | 5.7 | 8.0 |
| Last card → command strip ≥ 12 px; tray overlapping the strip | 11.5; a 6th roster card showed as a sliver | 13.8; tray ends 9 px before the strip, no sliver |
| Category cards above the bottom edge 16 px | 14.9 | 16.1 |
| Card inner padding / top padding / section gap | 16.1 / 13.8 / 9.2 | 16.0 / 16.0 / 12.0 |
| COMMANDS target ≥ 64 px | 82.6 | 82.6 |
| ARIA button side insets 16 px | 23.0 | 16.0 |
| ARIA vertical gaps 12 px | 13.8 | 12.0 |
| ARIA bottom inset (sized from content) | 13.8 | 16.0 |
| Stopped panel height | 383 px with an empty 230-unit portrait stage and telemetry | 221 px: header, "Guidance paused" status, green Play |
| Show Me glyph | native camera | native camera (unchanged) |

## Intentional differences from the mockups

1. **Button order.** In the two-action state, Show Me sits above Field Guide; the mockup has Field Guide first. Show Me belongs to the briefing layout, and Field Guide to the utility row below it. Swapping them would overlap the briefing layout's raycast area, or require reparenting controls that the ARIA input layer clicks. Both are kept above the red Stop ARIA, as required.
2. **Stop and Play captions.** These are the existing localized captions: STOP ARIA, WATCH ARIA / ARIA PLAY (Base Assault), RETRY ARIA, and the FA equivalents. The mockup's "PLAY ARIA" is not used. The caption logic is unchanged.
3. **Stopped status line.** It is new copy: "Guidance paused" / "راهنمایی متوقف شده". The pair is written inline, following the precedent in `AriaTutorialBriefingView.Watch.cs`, not added as a catalog entry.
4. **Command strip position is unchanged.** Its buttons stay 19.5 px above the bottom edge, which meets the ≥ 16 px minimum. They are not exactly 16 px, because moving the strip would shift every command target that ARIA taps. The category cards are exactly 16 px from the bottom edge.
5. **Outer ARIA rail position is unchanged.** The rail sits 9.2 px from the top and 17.2 px from the right of the screen, as authored in the prefab. The 16 px rule is applied inside the panel (side insets, button insets and bottom inset).
6. **Live data.** Mockup portraits, resource names, roster artwork and guidance text are illustrative. The fixture uses native portraits and fixture copy. The paginated roster uses EN fixture titles in both locales. Real roster titles come from the session.
7. **Card size.** Cards are 133 HUD units wide instead of 135, so the stride stays 140 units. Paging, swipe distances and the cards ARIA taps keep their positions.
