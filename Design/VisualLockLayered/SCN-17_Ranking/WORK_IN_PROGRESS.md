# SCN-17 Commander Ranking V3 Work In Progress

Canonical target: `reference/SCN-17_RankingV3_Final_Target.png`.

The screen uses a responsive 1672x941 composition, shared V3 resource/settings
icons, and the existing portrait library without screen-local raster copies.
All visible framed chrome uses directional gradients and one 3 px border width.
Portraits are masked with `AspectRatioFitter.EnvelopeParent`, so they crop rather
than stretch. Global, Region, Friends, Season, and View Rewards controls are
interactive.

Prefab generation and structural validation are complete. Exact 1920x1080 and
4800x2160 live Play Mode captures, visual comparison, correction, and immutable
iteration locking remain required before this screen is accepted.
