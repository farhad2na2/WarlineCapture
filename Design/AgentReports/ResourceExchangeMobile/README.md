# Resource exchange mobile redesign — 2026-09-18

## Changes

- One visible confirmation button. All five configured exchanges are in one list; fuel recovery no longer hides behind a misleading tab.
- Clear give/receive values, large +/- controls, real available balances, duration, and actionable validation. The decorative slider, duplicate sample wallet, false capacity panel, and duplicate confirmation controls are hidden.
- Physical resources committed to active exchanges are deducted from spendable balances. Pending outputs reserve space in the form, preventing misleading confirmations.
- Active exchanges precede completed history. The queue scrolls, reports its real capacity, shows a proportional progress bar and countdown, and offers clearing only when there are completed jobs.
- Cancellation is offered only before presentation starts, while reserved inputs are refundable. This preserves existing refund rules without offering a silently costly cancellation.
- English and conversational Farsi labels, shorter route names, and fixed readable Farsi font sizes. No change to recipe rates, minimum batches, or durations.

## Verification

- 38 focused Editor tests passed: prefab bindings and action wiring, both route categories in one list, unavailable confirmation, active queue visibility, no-refund cancellation boundary, reservations, insufficient inputs, capacity, queue limits, cancellation/refunds, and exactly-once completion.
- Real Skirmish, separate QA save: clicked the fifth recipe and the visible confirmation through EventSystem hit tests. Fuel available dropped from 149 to 48 (100 reserved plus ongoing consumption). After the natural 90-second exchange, materials rose from 260 to 440, exactly +180. No simulation fast-forward or injected resources. See `live-completion.txt`.
- Hit-tested phone-sized recipe selection and amount increase/decrease. 844x390 captures in both languages; 2400x1080 Farsi capture. Main touch controls at phone scale: close/confirm 45.5px high, +/- 47.7px, recipe rows 54.2px.
- Visually checked final text placement, route visibility, and wide layout. Farsi no longer auto-shrinks to half-sized text.
- Restored the normal Editor through the existing isolated-save QA restoration flow.

This is Editor gameplay and phone-resolution layout verification, not a physical Android device test.
