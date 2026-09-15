# M3 mission clarity audit — 15 September 2026

Scope: the user's step 10/12 and 11/12 screenshots, the preparation leading to them, and the real defensive result. Editor validation only. Screenshots and logs belong under `/private/tmp`, not this design folder.

## Findings and changes

| Finding | Player impact | Change |
| --- | --- | --- |
| Combat lessons highlighted a disabled Continue button. | No indicator, no Show Me, and no useful next action. | Combat guidance now resolves a rifle, its movement, its defensive position, its Hold order, and a confirmed living contact. It never asks for Continue during combat. |
| Advice said to prioritize/adapt without naming the required gesture or explaining waiting. | Player could wait at the barracks or repeatedly press unrelated controls. | State-specific English/Farsi instructions: select rifle, Move to the marked line, wait for arrival, press Hold, wait for convoy, and watch confirmed contact. Stop recovers to a Hold instruction. |
| The mission goal was buried beneath control lessons. | A series of button presses had no clear purpose. | First lesson explicitly says to stop both convoys before they reach the post and keep the post standing; preparation pauses their approach. |
| The build sequence could look like a requirement to construct everything. | Player wasted resources and lost sight of the defense objective. | Preparation explains that the next construction lesson is optional and requires one tower OR road barrier, not all structures. Existing green-footprint confirmation remains authoritative. |
| Radar's small unlabeled countdown looked like a progression timer. | Player waited for an optional ability instead of defending. | The toolbar says Radar recharge while cooling down, uses a larger counter, and distinguishes depleted uses. The lesson explains two uses/60 seconds, no damage, and that recharge never needs to finish to defend/win. Enemy ETA remains in the warning. |
| Reinforcement/selection camera could remain at a building after combat started. | The action happened outside the view. | A smooth battle-entry focus goes to the defensive line. Subsequent Show Me targets the actual squad or defended road, without continuously overriding player camera movement. Tutorial target focus uses height 40. |
| Selected rifles on Hold were labeled Idle. | The selection panel contradicted ARIA and made a correct defense look inactive. | A distinct localized Holding position status is used for both individual and group selection. |
| The selection control badge always said Moving. | It contradicted the real current order. | The badge now identifies player control only; the current-order row reports movement or Hold. Existing loaded prefabs receive the localized correction. |
| Radar selection subtitle leaked an English config description. | Farsi UI showed clipped English. | Radar description is registered in the central localization catalog; selection descriptions resolve keys and registered source text. |

## Design behavior

There is a difference between an action and a wait. Move/Hold are actionable button cues; the yellow ring marks the defensive line. During a correct defensive wait ARIA explicitly says no extra click is needed. Confirmed contact updates the instruction to explain automatic fire from Hold; it does not move the marker after an enemy or tell rifles to abandon the post. An offscreen location offers Show Me; once the indicator is visible, Show Me hides. The battle does not require spending Radar Ping charges, selecting the radar vehicle, or repeatedly dismissing explanation cards.

The same ARIA panel and existing toolbar are used. No extra timer popup or persistent floating debug panel is introduced. Existing recorded narrative remains; the new situational directions are localized visual guidance, not newly generated audio.

## Validation

Passed regression validation: three battle-guidance cases, seven selection-status cases, and 139 architecture checks across nine fixtures. Wrapper exit 0; log `/private/tmp/warline-m03-clarity-regression-v3.log`. Coverage includes selection → movement → Hold → waiting, recovery after Stop, exclusion of scout/stale/dead contacts, rifle targeting while the sensor is selected, and English/Farsi copy. The final playthrough assembly also runs the three battle cases, including the stable defensive-line marker assertion.

Farsi full guidance journey passed, wrapper exit 0: `/private/tmp/warline-m03-battle-clarity-fa.log`. It visited all twelve lessons, used normal Move/Stop/Hold commands, spent one optional radar charge, observed the recharge and both wait/contact instruction states, and won through actual defensive combat with three stars and zero civilian losses. The finale froze the mission clock, all three debrief panels completed, and the result/guide round trip passed in English/Farsi at 16:9 and 20:9. Wait/contact screenshots were visually reviewed for readable Farsi, unclipped ARIA text, the visible road marker, and the larger labeled recharge counter.

English full guidance journey passed, wrapper exit 0: `/private/tmp/warline-m03-battle-clarity-en-final.log`. It visited all twelve lessons, verified one radar charge spent, readable waiting/contact instructions and visible targets, and won through actual defensive Hold orders with three stars and zero civilian losses. The unchanged finale timing assertion, three debrief panels, bilingual result layouts and result/guide return all passed. The preceding simultaneous English run defeated both convoys but failed the finale timing assertion; that failure did not reproduce in this isolated rerun and is not counted as a pass. Earlier failed compile/probe attempts are likewise not passes. This is functional Editor coverage, not a performance certification.

This focused journey skips optional construction/reinforcement, so it does not substitute for separate placement coverage. New situational copy was checked visually, not recorded as new voice audio. No Android/device performance certification is claimed.

The main Editor finished recompilation with `status=completed`, `failed=false`, and no compiler errors. `git diff --check` passed. Validation images remain in `/private/tmp/warline-m03-editor-probe/`.
