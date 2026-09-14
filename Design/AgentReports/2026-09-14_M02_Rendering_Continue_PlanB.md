# M2 helicopter, tutorial Continue, and Plan B guidance

## Changes

- The helipad helicopter had two coincident render trees: its original faction-tinted body and an untinted detailed copy. A one-time presentation repair retains the canonical body/rotors, transfers the faction tint, removes the competing `_BaseColor` property, and suppresses only coincident duplicate meshes. Dormant gameplay entities stay dormant and unrelated attachments remain visible.
- Plan B uses a yellow frame with a dark outline and a bouncing yellow chevron. It chooses a free side and rotates toward the target. Placement reserves the whole bounce inside the safe area and avoids visible neighboring controls and text. Hidden popup content and the target's enclosing panel are excluded. The overlay never receives input. Reduced-motion mode stays stationary; idle Show Me retains its four-second delay.
- M2 Materials Continue acknowledges the typed resource lesson directly, independent of Show Me. Continue hides before dispatch and cannot reappear for the same lesson. If construction is incomplete, a localized construction-wait message replaces the lesson; completion advances to production automatically. The wait state does not replay the Materials voice recording.
- Both wait strings are in the shared localization configuration and generated EN/FA catalog.

## Editor verification

- Actual M2 helicopter: eight-second zoom/yaw/pan sweep; one visible blue body, including rotors and attachments. Evidence is outside Design at `/private/tmp/warline-m02-helicopter-fixed`.
- Actual M2 placement and Continue: English and Farsi; Build → Barracks → Place → Confirm → Continue → rifle-production instruction. Continue is hit-testable and hides immediately without first clicking Show Me. The building had already completed in these live runs; the incomplete-construction branch is covered by the guidance regression test.
- M1–M5 campaign entries and first-action transitions passed in both languages, including replay/retry profiles.
- Focused regressions cover four pointer orientations, three viewport sizes, two caption widths, safe bounce bounds, fully blocked layouts, actual chevron geometry, repeated Continue clicks, next-lesson availability, dormant duplicate render ownership, 42 M2 guidance cases, 192 ARIA presentations, 34 guidance/Show Me cases, and eight instruction viewport cases.
- Final architecture validation: all 139 tests across nine fixtures passed with zero failures, against the current branch history. Log: `/private/tmp/warline-release-final.log`.
- Final English and Farsi live runs verified two different rendered chevron positions, a visible arrow pointing toward Continue, and a successful Continue-to-production transition. Log: `/private/tmp/warline-planb-final-fa2.log`; frames: `/private/tmp/warline-m02-placement/planb-pointer-a-fa-IR.png` and `planb-pointer-b-fa-IR.png`.

This is a focused regression pass for the three reported issues, not a claim that every mission has been played to completion again. Android validation was not requested.
