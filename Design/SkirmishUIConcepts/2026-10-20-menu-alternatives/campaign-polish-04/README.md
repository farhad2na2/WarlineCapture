# Campaign polish matched to refined Skirmish

Owner direction,2026-10-21 Europe/Berlin: build Campaign and Skirmish with the quality and consistency of the accepted refined Skirmish mockups, and self-QA the actual native screens. The approved Campaign A flow remains chapter selection followed by five large individual comic cards on the map.

![Campaign chapter atlas target](01-chapter-atlas.png)

![Campaign five-mission target](02-five-comic-missions.png)

These ImageGen refinements use the actual Campaign Game view captures and the accepted Skirmish polish as references. Existing chapter and individual mission comics are retained. Progress in the mockups is illustrative: native implementation binds real completion, release and progression state; Coming Soon must not become a false claim that a mission is playable. Campaign uses its actual completion/progression semantics rather than Skirmish difficulty medals.

Both modes share Back, the gear centered inside its own button, backgrounds, border weights, typography, command colors, status marks and mobile spacing. Campaign retains five large cards; Skirmish retains six per scrolling objective region and four actual difficulty-medal tiers. Read-only chapter progress rows on the atlas do not replace the large mission-selection page.

[Accepted refined Skirmish](../skirmish-polish-03/README.md) · [Actual native review and separate gameplay/device gates](../../../AgentReports/ApprovedMissionAtlas/STATUS.md).

Exact prompts and output hashes are in [generation-manifest.json](generation-manifest.json). Menu art/layout only; no physical map or gameplay-rule change. Native implementation and QA are in progress; nothing is merged.
