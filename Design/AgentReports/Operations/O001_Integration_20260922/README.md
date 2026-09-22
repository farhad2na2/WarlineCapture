# O001 shared-world integration evidence

This is button-event integration smoke, not a manual or ARIA win. The test starts from the real Menu scene and uses the Operations briefing and HUD buttons. It verifies launch, five seconds of live simulation, starting-squad visibility, withdrawal confirmation, durable settlement, actor cleanup, menu return and paid redeployment.

- `lifecycle-smoke.log`: wrapper exit 0; deploy/withdraw/save/return/redeploy passed.
- `focused-editmode.xml` and `.log`: 79 passed, zero failed; wrapper exit 0.
- `normal-launch.png`: capture during the first deployment of this journey.

Published logs redact authentication and machine metadata. Original full logs remain at `/private/tmp/o001-lifecycle-smoke-4.log` and `/private/tmp/o001-integration-editmode-5.log` on the validation host. Known HUD-prefab-save warnings during Editor teardown are retained. The actual prefab passes a pre-Play missing-script check; the teardown cause remains unresolved.

Mission content, English/Farsi layout, full outcomes, active-world checkpoints, normal-input ARIA, device and unfamiliar-player gates are not certified by these files. See the parent progress report and P4R plan.
