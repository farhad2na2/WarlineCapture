# M5 gate screen targeting

The gate is an enemy combat objective and must accept an Attack command. The earlier compound playthrough issued entity-targeted combat orders, which did not cover pointer picking. It therefore missed this player-facing failure.

## Reproduction

The unmodified screen picker accepted a tap at the gate's ground centre, but rejected taps one, two, three and four metres above that point on the visible gate. Those rays projected onto ground cells behind the gate's one-cell navigation footprint. Evidence: `/private/tmp/m05-gate-tap-before.log`, `[GateTapProbe]` entries.

## Correction

- Runtime building combat entities now carry a selection hitbox derived from their visible prefab bounds, using the same horizontal recentering as their rendered models.
- Attack picking checks the projected building hitbox with touch padding. It no longer requires a visible gate-face tap to hit the gate's ground cell.
- A directly hit hostile building takes priority over the nearby-unit distance fallback.
- Tutorial attack previews can resolve hostile runtime buildings. Buildings still use the existing breach-routing command path.
- Neutral/friendly structures, empty ground and destroyed buildings are not made attack targets by this change.

## Verification

`RuntimeBuildingAttackPickingTests` covers gate-face taps at five heights, three camera angles and two landscape screen sizes, plus empty-ground and neutral/friendly/destroyed target exclusions.

`M05BreachAssaultEditorProbe.RunScreenTapAudit` selects the tutorial actor, invokes the real HUD Attack button, and submits only a screen position to the normal input queue. It does not pass a gate entity to the combat system. It requires combat damage and gate destruction before allowing the existing mission completion/replay check to continue. The remaining mission combat still uses entity-targeted orders; this is not a complete manual tutorial audit.

The focused picker tests passed. The live English run and fresh Farsi replay both passed the screen-only attack check: the gate took combat damage and was destroyed. The English case ran at 1920×1080; the Farsi case ran at 2400×1080.

Evidence: `/private/tmp/m05-gate-tap-combat.log`, `[RuntimeBuildingAttackPickingTests] result=Passed` and `[M05GateScreenTap] result=Passed` for `en` and `fa-IR`. Captures: `/private/tmp/warline-m05-editor-probe/gate-screen-tap-en.png` and `gate-screen-tap-fa-IR.png`.

The test and runtime source in the main project match the validated copy. The main project's compiled `Game.Runtime.dll` also contains the new building screen picker.

Earlier attempts exposed a test-helper compile error, an uninitialized camera test dependency, and a stale search-index cache in the copied Editor project. These runs were treated as failures. The helper was corrected and the disposable copied search cache was preserved outside the project before rebuilding it; gameplay exception checks stayed enabled.

Final run: wrapper exit **0**. English victory and fresh Farsi replay victory both completed all three objectives, rewards, and campaign return. The final probe pass marker is in the same combat log. This validates the repaired gate interaction and mission completion; it does not certify every tutorial gesture or all mobile devices.
