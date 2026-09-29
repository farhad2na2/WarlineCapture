# Split Front readiness — approved normal Attack revision

The owner approved native-reference ImageGen gameplay HUD v03 on 2026-09-29 with “ok fix it then”. The center launcher panel and separate Confirm/Cancel flow are removed. Select → Attack → target now issues the normal launcher order. Hold stops preparation without recalling a launched missile. The existing unit card, command controls, portraits, minimap and ARIA Play/Stop remain. Bilingual ARIA, briefing, narrative and field guide now describe normal Attack/Hold.

## Separate gates

- **Owner visual direction:** approved v03; [review](Mockups/review.md), [mockup](Mockups/gameplay-hud-v03.png). v02 was superseded; v01's center-panel deviation was rejected.
- **Automated checks:** revised wrapper build02 passed with the new native marker `launcherPanel=removed attack=existing hold=existing` and rules marker `attack=direct retained-after-shot=true hold=stops-preparation noRecall=in-flight`. Minimum/maximum range, civilian safety, normal hostile targets, durable real deaths, base protection and deadline remain checked. Complete log: `Evidence/Logs/split-front-normal-attack-build-02.log`.
- **Native screen review:** revised English 16:9 native HUD, Attack preparation, Hold status, ARIA Stop and result inspected. The center panel is absent; status is in the original unit card and colorful commands remain. Persian 20:9 briefing, HUD, selected-launcher preparation, visible ARIA Stop, result and return were also inspected; RTL copy fits and the center remains clear. English screens: `Evidence/20260929-073731-split-front-en-smoke/`.
- **Normal-input journeys:** revised English Smoke EN01 passed with native touch Attack → target, Hold stopping preparation, optional production Smoke, ARIA two touch actions, real missile impact, all five hostiles defeated, eight comics, victory, first-clear settlement, duplicate rejection and Campaign return. Wrapper exit 0; all seven required markers verified in the full log. Gameplay source/assets remained unchanged from the pinned candidate. Persian FA01 without Smoke passed: one normal Attack order launched one real missile, ARIA seven touch actions, five hostiles defeated, eight exact 20:9 comic crops, victory/result/settlement/duplicate rejection and Campaign return. Wrapper exit 0. Player hashes remained identical across both final journeys. Persian screens: `Evidence/20260929-074356-split-front-fa-IR/`. Old confirmation-based wins do not certify this change.
- **Human packaged-player/device acceptance:** pending. Automated Editor touch input is separate from real player/device acceptance.
- **Voices:** 16 captions remain available. The revised ARIA briefing is included in the local dry-run payload. No voice upload was retried; exact payload/destination approval remains pending after the earlier automatic review rejection, voices=0.

## Evidence integrity

Revised player source/assets are pinned in `Evidence/normal-attack-candidate-identity.json`, SHA-256 `8012692ac1ed83a458a6ddcebaf2c64e93bd78fbc8293c8a2071d0cd75545325`. All 18 required pass markers across build02, English Smoke EN01 and Persian FA01 were checked against complete logs and zero exit receipts; see `Evidence/normal-attack-validation-summary.json`. Source whitespace checks passed. Historical evidence remains in `readiness-confirmation-candidate-history.md` and `Evidence/Logs/`.

The first new build invocation was discarded: an Editor probe CS0136 naming error left old compiled assemblies loaded. Its successful wrapper receipt had old `launcher=explicit-consent` and `launcherPanel=authored` markers, so it is not revised-candidate evidence. The probe error was corrected, code was reimported, and build02 passed with the required new markers. Full logs and an invalidation note are retained.

Normal-input defeat and dedicated replay/resume/access cases remain separate release checks. No source commit or PR was requested. Unity Editor and Hub remain open.
