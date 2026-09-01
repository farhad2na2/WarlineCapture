# SCN-08 Tactical Feedback — Iteration 6

Review-frozen on 2026-09-01 against `SCN-08_MatchHudV3_TacticalFeedback_Final_Target.png`.

- The real Attack command now exposes a sharp white/cyan V3 selected frame instead of the invisible legacy selected sprite.
- The range strip, dashed attack route, friendly selection ring, hostile health cells, and hostile marker are resolution-independent prefab visuals using procedural geometry and the shared V3 icon atlas.
- The range copy is `RANGE 140m / WEAPON 90m`; the persistent rejection is `ATTACK UNAVAILABLE — TARGET OUT OF RANGE`.
- Error feedback expands to the target-lock bounds while ready/neutral feedback restores the compact layout used by other Match HUD states.
- The redundant legacy current-order banner is suppressed for this V3 prefab, eliminating its 20:9 collision with ARIA.
- ARIA uses the target `TUTORIAL 1/5` copy and keeps the aspect-preserved V3 portrait at the top-right edge.
- The old dormant battlefield-preview children were removed from the generated prefab, so this state contains no visible legacy marker placeholders.
- Primary panels, feedback, range strip, and command controls retain procedural gradients and constant 3 px borders.

Validation:

- Deterministic gameplay-background captures passed at 1920x1080 and 4800x2160.
- Actual Menu -> Match Play Mode captures passed at both exact resolutions after clicking the real `AttackCommand` button.
- Both live runs asserted `CurrentCommandMode == Attack` and that the Attack V3 selected frame was the active command selection.
- Focused command-feedback validation passed 17 tests, including the new exclusive V3 Attack-selection-state check.
- The live Menu-route harness does not load a battlefield world; its black center is expected. The deterministic captures provide the gameplay-background composition comparison.

This iteration is review-frozen, not user-accepted.
