# Skirmish and Campaign — replacement menu choices

**Current direction: choose a map (Skirmish) or chapter (Campaign), then select large thumbnail mission cards directly on the map. Campaign shows five per chapter; Skirmish shows six per objective region.** [Campaign map-based mission screen](campaign-A-five-missions-on-map-02/README.md) · [Campaign A chapter overview](campaign-A-comic-thumbnails-01/README.md) · [Selected Skirmish mission screen](selected-A-iteration-02-two-step/04-missions-scrolling-regions.png). Shared Back, buttons, panels, typography and spacing. Historical drawer layouts below are superseded. Concepts do not authorize physical map or gameplay changes.

## Historical alternatives

Skirmish: **5 battlefields ×24 battles =120**, each battlefield has **4 objective families ×6 battles**.

- **D overview:** select a battlefield and see its24 mission buttons in four adjoining Operations-style objective zones.
- **B focus:** open an objective family to see six large mission-sector choices and a readable briefing.
- **E index:** optional direct lookup by ID/name, battlefield/objective filters and12 rows per page, ten pages for120. The example displays S073–S084 on page7.

These views may be combined; D/E do not require squeezing120 buttons onto one map. Full catalog capacity is separate from playable availability. Future slots remain coming soon, with no invented unlock rule.

Current Campaign navigation: Campaign A chapter overview, followed by five large comic mission cards on the selected chapter map. The tiny side drawer is superseded.

## Compare the directions

| Choice | Skirmish | Campaign |
| --- | --- | --- |
| A — territory/chapter atlas with mission drawer | [Open](A-skirmish-territory-atlas.png) | [Open](A-campaign-territory-atlas.png) |
| B — mission sectors, closest to Operations | [Open](B-skirmish-mission-sectors.png) | [Open](B-campaign-mission-sectors.png) |
| C — map plus mission tray | [Open](C-skirmish-map-and-mission-tray.png) | [Open](C-campaign-map-and-mission-tray.png) |
| D — all24 battles in one battlefield | [Open](D-skirmish-24-battle-overview.png) | — |
| E — searchable120-battle index | [Open](E-skirmish-120-battle-index.png) | — |

### D —24-battle overview

![24 battles in four objective zones](D-skirmish-24-battle-overview.png)

### E —120-battle index

![Search and filters for120 battles](E-skirmish-120-battle-index.png)

### B — six-battle focus

![Six mission sectors](B-skirmish-mission-sectors.png)

### B — Campaign

![Campaign mission sectors](B-campaign-mission-sectors.png)

## Shared consistency requirements

Back is top-left across all modes; no duplicate footer Back. Use the same component dimensions, tan arrow, background, borders, typography and interaction states. Match Operations button colors and roles (green primary, blue secondary, neutral tertiary), panel backgrounds, border weights, gutters, icon scale and readable text. Selection uses cyan. Region color is a map overlay; do not restore glowing disconnected coast islands or decorative yellow panel strokes.

Generated mockups illustrate layout and hierarchy. Illustrative art, copy, medals and progress are not final native bindings; final implementation must reuse the actual game assets, mission data, readiness states and shared prefabs. In particular, concept map partition labels are navigation areas and do not add or change physical gameplay geometry.

[Full owner requirements](REVIEW_REQUIREMENTS.md) · [Exact prompt set and native reference paths](prompts.json) · [PNG hashes](manifest.json).

PR70 remains draft and unmerged. Campaign A remains the chapter overview; the new map-based five-mission screen replaces the tiny mission drawer. Skirmish retains A map selection and the scrolling objective-region mission screen. Historical alternatives remain for context. Native implementation now follows the approved direction. [Current native validation](../../AgentReports/ApprovedMissionAtlas/STATUS.md).


## Follow-up polish of the original Skirmish mockups

[Revised map and six-mission screens](skirmish-polish-03/README.md) apply the requested clearer mission identities, four difficulty medals, calmer atlas styling, available/completed/coming-soon counts and next-region guidance. This is a design-only follow-up; the native implementation above remains separately reviewable. Progress and proposed mission copy in the refined images are illustrative.
