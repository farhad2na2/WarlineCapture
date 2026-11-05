# Current game → HUD proposals

The six ImageGen directions below were approved and implemented. [See the implemented native screens and QA results](Implementation/REVIEW.md). This page preserves the original visual-direction review.

**Owner correction incorporated:** the squad tray scrolls directly. There is no extra paging panel above the cards.

[Design brief and prompt directions](DESIGN_BRIEF.md) · [Source/capture manifest](REVIEW_MANIFEST.json) · [Comparison gallery](REVIEW.html)

## HUD and minimap

Larger padded feedback and blue square zoom buttons. Owner correction: the squad tray itself scrolls; no added paging panel or arrow strip.

Actual game: [hud](Baseline/hud.png) · [minimap](Baseline/minimap.png)

![Proposed HUD and minimap](Proposals/hud-proposal.png)

## Build and placement

Clear grey disabled Place state, selected-building artwork, consistent confirmation controls and better use of a single-offer layout.

Actual game: [build](Baseline/build.png) · [placement](Baseline/placement.png)

![Proposed Build and placement](Proposals/build-placement-proposal.png)

## Commands

Retain the actual four-sector APC menu plus Cease Fire. Clear disabled styling, a visible close action and consistent spacing.

Actual game: [command](Baseline/command.png)

![Proposed Commands](Proposals/command-proposal.png)

## Passengers

Compact empty state, no overlap with mission status, no HP truncation. Loaded 4/10 state is an illustrative proposal; native capture is 0/10.

Actual game: [passenger](Baseline/passenger.png)

![Proposed Passengers](Proposals/passenger-proposal.png)

## Resource Exchange

Preserve the current five routes and rates. Grey disabled action, explicit resource reason, balanced amounts and queue.

Actual game: [exchange](Baseline/exchange.png)

![Proposed Resource Exchange](Proposals/exchange-proposal.png)

## Pause and Settings

Refine the current structure, slider thumbs, icon alignment and action hierarchy. Example stats are illustrative; the implementation removes invented fallback values and binds the real read models.

Actual game: [pause](Baseline/pause.png) · [settings](Baseline/settings.png)

![Proposed Pause and Settings](Proposals/pause-settings-proposal.png)

## Evidence limits

The baseline captures show the real runtime controls before this change. They do not establish mission readiness or real-device acceptance. First-launch-obstructed and incomplete walkthrough attempts are retained separately and excluded from the design baseline. Visual direction was approved; native implementation awaits owner review.
