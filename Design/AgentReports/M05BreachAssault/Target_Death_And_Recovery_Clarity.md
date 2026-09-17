# M5 target death and archive recovery

Request: remove the transition return control, clear dead-target markers immediately, use existing destroyed visuals, and explain the final wait.

## Changes

- The transition-only “Return to command view” control is hidden from OnEnable onward. Camera tours still finish automatically; the reduced-motion preference remains supported.
- Attack command frames and rings now test the target's current health/existence before their 14-second display duration. Preview markers also remove dead targets between their position refreshes.
- Minimap markers retain their source entity so death/removal can prune them every update, without waiting for the 0.2-second position refresh.
- Buildings can use an inactive legacy `Destroyed` child when no destroyed prefab is assigned. The visible replacement preserves its world transform, survives hiding the live hierarchy, and is reused on repeated calls. Explicit destroyed prefabs are activated on instantiation.
- Vehicle authoring now retains a legacy destroyed child when no explicit wreck prefab exists. The death path falls back when the prefab reference is null and preserves the child's authored scale.
- M5 archive guidance now uses the same occupancy result as the objective. Previously guidance accepted an 18-metre radius while recovery required the configured 6 metres; this could remove Show Me before the player actually arrived.
- ARIA distinguishes approaching reinforcements, remaining enemies, moving into the archive, recovery in progress, and completion. During recovery it states the seconds left, the requirement to leave a unit inside, and the reset consequence of leaving.
- The assistant panel cache now includes recovery status and countdown seconds. The final lesson title is “Recover the archive.” New English/Farsi strings live in `M05BreachAssaultCopyCatalog` and the generated localization catalog.

## Asset check

The M5 `Building_Satelite_Dish` prefab has no destroyed prefab or destroyed child. Its hierarchy contains `Model`, `SM_Prop_Satelite_Dish_02_Top`, and `SM_Prop_Drone_Control_Room_02`. It therefore disappears on destruction; no unrelated wreck was substituted. Existing wreck assets remain source-specific.

## Verification

`M05TargetLifecycleTests.RunFocusedValidation` covers:

- Live attack markers persist; death or entity removal clears all three attack marker types despite a future expiry time.
- An inactive destroyed building child becomes visible at the original world position/scale; repeated destruction reuses it.
- A null vehicle wreck-prefab reference falls back to the authored child and preserves scale.
- Archive guidance remains available outside the actual recovery radius, stops asking for movement inside, and recovery resets after leaving.
- Minimap death cleanup occurs between scheduled position refreshes.
- Existing configured building and vehicle wreck tests, including vehicle health bar/selection marker cleanup.

`Game.Editor.M05BreachAssaultEditorProbe.RunLifecycleAudit` checks the hidden transition control and ARIA's actual read model each recovery second, captures both-language recovery layouts, and runs the established M5 combat/victory/replay journey. Gate attacks enter through the Attack button and screen-coordinate picker. Later combat and movement use production ECS command queues; the probe does not simulate every physical mobile gesture or modify health/objective outcomes.

Final-run log: `/private/tmp/m05-target-lifecycle-final.log`.
Recovery screenshots: `/private/tmp/warline-m05-editor-probe/archive-recovery-en-16.png`, `archive-recovery-en-10.png`, `archive-recovery-fa-IR-16.png`, `archive-recovery-fa-IR-10.png`.

Earlier iterations caught a startup visibility flash and a stale assistant cache before final validation. These were corrected rather than waived.

Final result: passed focused lifecycle tests, gate picking regressions, English victory/campaign return, and Farsi replay victory/campaign return. ARIA's displayed recovery read model was checked at every remaining-second value from 18 down to 2 in both locales. English (1920×1080) and Farsi (2400×1080) captures were visually reviewed for text/button clipping. All 17 changed runtime/probe/test/catalog files matched between the main project and the validated QA copy.
