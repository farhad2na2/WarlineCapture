# SCN-08 Transport Passengers — Iteration 5

Review-frozen on 2026-09-01 against `SCN-08_MatchHudV3_TransportPassengers_Final_Target.png`.

- Replaced the hidden oversized legacy drawer with the live V3 passenger state.
- Drawer reference bounds are x=328, y=87, w=449, h=618 after the compact transport panel is applied.
- Ten capacity slots update from the runtime passenger count/capacity model.
- Four pooled passenger rows reuse existing unit and Dalia portraits; no replacement unit art was generated.
- Health bars use live fill values and green/yellow/red severity colors.
- Passenger, Board, Rope Drop, Exit, Exit All, Close, ARIA, and feedback symbols use the shared V3 icon set.
- Rope Drop invokes the existing Exit All transport action; per-row Exit and Close retain their production bindings.
- Primary frames/buttons use visible directional gradients and constant 3 px borders. Row separators use a subordinate 2 px stroke.
- The selected transport portrait uses `EnvelopeParent` with its sprite ratio refreshed at runtime, so it fills without stretching.
- Runtime feedback now changes its V3 border/text/icon accent by severity; the ready state is cyan instead of inheriting the red error style.
- Material-fabrication chip layout was also repaired so its three-line content stays within the V3 selection frame and restores the passenger default cleanly.

Validation:

- Deterministic build/capture passed at 1920x1080 and 4800x2160.
- Actual Menu -> Match Play Mode captured at both exact resolutions after clicking the real `PassengerChip` button.
- Focused passenger suite passed 4 tests, including serialized references, pooled rows, capacity slots, per-passenger exit, Rope Drop/Exit All, Close behavior, material-fabrication containment/restoration, and ECS disembark request storage.
- The live Menu-route harness does not load a battlefield world; its black center is expected. The deterministic captures provide the gameplay-background composition comparison.

This iteration is review-frozen, not user-accepted.
