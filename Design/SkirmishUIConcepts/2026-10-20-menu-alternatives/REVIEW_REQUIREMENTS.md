# Current menu redesign requirements

Owner requires consistent two-stage navigation: select a Campaign chapter or Skirmish map, then select individual missions using large thumbnail cards directly on the map. Campaign has five per chapter; Skirmish six per objective region. Campaign A is retained for the chapter overview, but its tiny mission drawer is superseded. [Current Campaign mission screen](campaign-A-five-missions-on-map-02/README.md). [Selected Skirmish mission screen](selected-A-iteration-02-two-step/README.md). Owner approved implementation; native validation is recorded in Design/AgentReports/ApprovedMissionAtlas/STATUS.md.

Owner direction: 2026-10-20. Existing saga and Campaign half-saga level selection are not accepted. Generate alternatives before implementation.

## Shared visual requirements

- One consistent Back button across Skirmish, Campaign and Operations: top-left placement, tan arrow, neutral background, matching dimensions, typography, border and interaction states. No duplicate bottom Back button.
- Consistent button colors and roles: green primary, blue secondary, charcoal tertiary; consistent heights, borders, gradients, icon scale, padding, selection and disabled states.
- Match Operations dark steel panels, background treatment, gutters and readable Oxanium typography. Avoid decorative yellow rectangular strokes, floating tiny portrait cards and bright glowing island outlines.
- Use Operations-style adjacent map zones with shared boundaries on one continuous existing Campaign map illustration. Replace the winding numbered saga/half-saga route.
- Preserve existing scenario IDs, mission bindings, chapter names, progression rules, resource/force budgets and difficulty identities. Mockups illustrate UI only; they do not create physical maps or gameplay sectors.
- Mission purpose, objectives, selection, progress and next action stay visible. Final EN/FA and ratio/device checks follow implementation after visual selection.

## Choices

A: Territory/chapter atlas, with mission selection in a side drawer.

B: Selected battlefield/chapter divided into contiguous mission navigation sectors, closest to Operations.

C: Continuous Operations-style map with a large horizontal mission tray.

D: A single-battlefield overview with all 24 mission choices, grouped as four adjacent objective zones with six tiles each.

E: A searchable all-battles index with 12 rows per page (10 pages for 120), battlefield/objective filters, direct ID lookup and a return to map view.

Catalog structure is five battlefields ×24 missions each; each24 is four objective families ×six missions. The focused view shows six, the overview24, and the index provides access to all120 without a120-node map. Future catalog slots remain visibly coming soon.

A–C each have separate Skirmish and Campaign mockups. D/E are additional Skirmish scalability views. The owner may choose different letters for the two modes. Generated illustrations and example progress are visual concepts, not runtime readiness or new content. All final prompts and native reference paths are recorded in prompts.json.

Owner explicitly approved both selected mockups for implementation. PR70 remains draft and unmerged; the old saga UI is superseded.

Latest owner media requirement: Campaign A chapter cards and individual mission choices each need comic thumbnails, reusing the main-page Campaign comic bindings. The selected mission preview uses the same corresponding comic. [Comic-thumbnail mockup](campaign-A-comic-thumbnails-01/README.md).

Latest clarification: missions must sit ON the map in both modes with very similar layout. Do not squeeze Campaign missions into a side drawer. Chapter and mission choices each use their own corresponding comic thumbnail.
