# Campaign Operations V3 — Iteration 01

Status: review candidate only; not accepted.

Runtime captures:

- `campaign_mission_v3_16x9.png` — 1920×1080
- `campaign_mission_v3_20x9.png` — 4800×2160 with the actual Unity Game View set to 20:9 first
- `campaign_chapter_v3_16x9.png` — 1920×1080
- `campaign_chapter_v3_20x9.png` — 4800×2160 with the actual Unity Game View set to 20:9 first

Comparison corrections completed before freezing this iteration:

- restored the shared header after fixing a Chapter backdrop clipping/order defect
- split the Mission Select title into the target white/amber hierarchy without truncation
- replaced square/text mission markers with circular node art and atlas-backed check/lock icons
- replaced repeated objective reticles with barracks, squad, and hold icons
- added Chapters, Start Briefing, Story Archive, M01, M02, and Back icons
- kept all borders at the V3 3 px rule and all action surfaces gradient-driven
- created `UI_V3_CampaignIcons_01.spriteatlas`; sources are packed once and validated for duplicate content

Known content-art follow-up: Dalia and Samira still use existing portrait stand-ins until their dedicated V3 baked commander assets are supplied.
