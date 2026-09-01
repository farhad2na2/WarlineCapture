# POP-02 Confirm Raid

## Review status

Iteration 2 is review-frozen, not user-accepted.

The old raster-frame presentation has been replaced by the final V3 modal
composition. The popup reuses the existing high-resolution Sahrin district map
with aspect-fill cropping, uses shared V3/Operations symbols, and renders its
chrome, meters, and action gradients procedurally.

The modal is authored against the 1672x941 target reference, remains centered
on wider canvases, and expands only its scrim. Every visible framed surface uses
the same 3 px border contract.

## Evidence

`iterations/iteration_02/` contains deterministic and actual Play Mode captures
at exact 1920x1080 and 4800x2160 sizes, plus the focused interaction and capture
logs.

