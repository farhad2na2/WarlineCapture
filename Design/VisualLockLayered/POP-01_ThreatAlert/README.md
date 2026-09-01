# POP-01 Threat Alert

## Review status

Iteration 2 is review-frozen, not user-accepted.

The shared `ThreatAlertPopup.prefab` implements both final V3 target states:

- incoming-threat alert;
- route preview after `JUMP TO THREAT`.

The popup suppresses the obsolete Match HUD threat banner while either V3
state is visible. The alert uses a centered responsive frame. Route preview
removes the scrim, compacts the summary, exposes the battlefield route, and
keeps the live ARIA/minimap panel and full-width command footer available.

All framed surfaces use the same 3 px border contract. Buttons use procedural
directional gradients. Symbols reuse the shared V3 Match atlas, and the convoy
preview reuses the existing aspect-preserved vehicle art rather than creating a
screen-local duplicate.

## Evidence

Immutable comparison and Play Mode captures are in
`iterations/iteration_02/` at exact 1920x1080 and 4800x2160 resolutions for
both states. Focused prefab validation, existing threat behavior regression,
and all four Play Mode capture logs are stored beside the images.

