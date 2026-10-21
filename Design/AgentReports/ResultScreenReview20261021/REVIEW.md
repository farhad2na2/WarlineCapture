# Shared mission result screens

The approved result design is implemented locally on `codex/result-screen-design-review`. Campaign, Skirmish and Operations use one authored native prefab, `Assets/Game/Resources/UI/SharedMissionResult.prefab`.

[Implementation review gallery](REVIEW.html) · [Original approved design review](DESIGN_REVIEW.md) · [Frozen candidate](Implementation/CANDIDATE.json)

The final native layout pass passed **4,451 checks**, including **100 Campaign adapter cases** and **30 rendered screens** across both outcomes, English/Persian and 16:9, wide and tablet layouts. These are representative view fixtures, not gameplay wins or losses.

The complete Campaign M01 touch journey has passed in both languages, including result and return, with no injected outcomes. The final Campaign journey and S002 live ARIA victory/result/Main Menu return passed. O001 cannot reach a result because an existing objective is blocked at cell (1860, 710). Broader legacy menu and architecture failures are retained separately in [implementation notes](IMPLEMENTATION.md). Player/device acceptance remains pending. Nothing from this result-screen task has been merged or published.
