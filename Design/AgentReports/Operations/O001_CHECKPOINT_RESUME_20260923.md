# O001 checkpoint resume, Editor candidate

Date: 2026-09-23. Status: **partial R4 evidence; not a player-ready sign-off**.

The shipping Operations presenter now captures the versioned 36-actor shared-world image at launch, every 30 mission seconds, at scan/evidence milestones and at terminal transition. Save & Exit writes the image before returning to the Operations menu. A reserved attempt with a checkpoint offers Resume; restore maps stable actor IDs into a fresh finite roster and retains the original session/AP reservation. An invalid current image falls back to the preceding image. An incompatible archive follows the existing technical-failure refund path and retains its journal bytes. A checkpoint cannot be bypassed through Restart.

Evidence on Unity 6000.5.2f1 macOS GUI wrapper:

- `/private/tmp/o001-checkpoint-resume-smoke-3.log`: `result=Passed journey=save-exit-resume`, visible button-event path, same session, elapsed 5.396064 → 5.405107, AP 2.
- `/private/tmp/o001-process-save.log` and `/private/tmp/o001-process-resume.log`: both `result=Passed`; the second run used a **fresh Editor process** and the same disk profile. Session `session.operations.8306241c`, elapsed 5.31347 → 5.322447, AP 2.
- `/private/tmp/o001-resume-editmode-2.xml`: four checkpoint codec tests passed, including carrier/channel remapping, casualty/order/clock/random-state round trip, invalid references and corrupted/incompatible images.
- `/private/tmp/o001-shared-regressions-2.xml`: 93 affected Operations, Campaign and Skirmish EditMode tests passed.

The smoke uses button events rather than a human mouse journey; it establishes durability and routing, not tactical play quality. The earlier `-quit` validation attempts exited before Play Mode/tests ran and have no pass status. Still needed: process restarts during an active scan, after evidence pickup/drop, and around terminal settlement; exactly-once cost/reward checks at those boundaries; cross-mode cleanup; ARIA and unfamiliar-player acceptance. The established English/Farsi full manual victories remain separate evidence.
