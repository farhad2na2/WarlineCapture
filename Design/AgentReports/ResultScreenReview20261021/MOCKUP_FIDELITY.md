# Mockup fidelity correction

The first implementation review missed visible differences from the approved boards. The user rejected that revision; its local commit is `c1c7b8f4b`. The approved ImageGen boards remain unchanged.

## Corrected visual details

- Outcome ribbon: a color-matched vertical divider separates the outcome title and reason. The reason, earned/empty stars and visible saved-state glyphs share their intended vertical centers.
- Identity: mode, mission title and cyan context are stacked as in the approved boards. The duration module includes the cyan clock symbol.
- Content: the three-column proportions, heading spacing, artwork, story rule and summary spacing are tightened. Body text uses the existing medium game font rather than making the whole narrative bold.
- Rewards: white labels and gold amounts have independent columns and consistent row centers. Gold XP chevrons and the existing stacked-credit artwork replace the mismatched icon pair. Dividers separate rewards. Credits-only replays retain exactly their existing amount and show no XP award.
- Medal/report: a standalone tier-colored medal star, white earned title and separate gold first-award caption follow the approved hierarchy. Empty medals are neutral. Operations uses the shared report layout and native document symbol.
- Navigation: icon/label groups are centered within their buttons. Skirmish victory places Main Menu, Replay and Battlefield in the approved visual order while preserving the existing callbacks.
- Persian: header line boxes, right margins, mirrored reward columns and full short aftermath text are checked. Visible glyph geometry is used for optical centering and to avoid a scrollbar caused only by empty font leading.

## Evidence and review status

The final native alignment files record the reason and saved-state center offsets. The native checker also inspects divider presence, reward label/value hierarchy, row centers, text bounds, independent panel borders and all 25 Campaign adapters.

Automated layout checks and normal-input journeys are recorded separately in `Implementation/VALIDATION.json`. Fresh native/game captures replace the rejected captures in the comparison gallery only after their checks pass. Failed compile/layout attempts are preserved in the validation archive. Owner visual review and real player/device acceptance remain pending.

Campaign exposes actions permitted by the existing settlement and debrief flow. Existing authored art, mission facts, rewards and availability still determine the content. The known O001 objective/source-fit failure prevents a live Operations result run; native Operations examples remain representative view fixtures.
