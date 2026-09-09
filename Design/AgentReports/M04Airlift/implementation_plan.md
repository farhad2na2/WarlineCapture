# M04 implementation sequence and acceptance

The production plan is `Design/M04_Airlift_Production_Plan.md`; the ownership document is `Design/Architecture/m04_airlift_technical_architecture.md`. All implementation packages below are authored. Final core acceptance passed, including visible results, replay, retry and natural idle timeout. The last vehicle-panel localization repair passed a fresh real rescue. See `qa_report.md` for evidence and remaining limits.

| Package | Implemented outcome | Verification |
|---|---|---|
| HUD and Persian | Live compact numeric binding; نفت / بنزین; correct currency/fuel icons; connected ARIA actions; safe UTF-8 transport reasons | 11 header checks, three locale transitions, M2 Credits, 48 ARIA layouts; live bilingual captures |
| Transport gate | Canonical APC pickup, real ground exit, helicopter boarding and airborne departure | All 15 transport scenario-lab checks in each language; complete live rescue |
| Authoring | Separate M4 mission, scenario and logical map; immutable extraction projection; validated roles/anchors; 18 attempt-owned actors | Production builder and integration validation |
| Extraction runtime | Four-person manifest, required APC history, clear LZ hold, real aircraft departure, failure precedence, timer and stars | 15 rules; actual movement and passenger components |
| Tutorial and guide | Twelve fact-driven lessons, contextual ARIA, shared 57-class guide, pause-safe actions, M4 availability copy | Every topic and class loaded in English/Persian; one resident class; clock freezes and resumes |
| Cinematic and comic | RTS overview → pickup → LZ → captured RTS pose; seven M3-style panels; Laila portrait; three narrative sequences | Normal camera ~14.2 seconds without watchdog; 56 caption/layout captures; 15 stable Sprite IDs |
| Result and save | Typed extraction result; visible modal after guide closes; exact delivered count; first-clear unlock transaction; campaign return | Live first clear; save-failure rollback; idempotent retry; bilingual result captures |
| Recovery | Include disabled boarded passengers in cleanup; replay and retry identity reset; failure cannot pay rescue grants | Eight integration checks plus live replay carrier loss and clean retry |
| Architecture | Narrow same-owner extraction helpers; altitude and transport result contracts separated from large owners | Unchanged repository guards; final audit attribution records pre-existing failures |

The first final probe exposed an invisible result after closing the guide: the result binder's region reference was configured in the Editor but not serialized. The fix serializes it and resolves the existing bounded parent for older Menu scenes. Final live acceptance now checks ancestor CanvasGroup alpha and interaction, plus the result and region scale before inspecting results or invoking their buttons. A data-only result read is no longer accepted as visual proof. A subsequent visible-result capture exposed overlapping objective columns and reward overflow; the result now reapplies its layout after the responsive owner and uses compact bilingual objective labels. The focused result renderer passes eight combinations of language, aspect and outcome.

No Android/device lane. M4 uses bilingual captions without recorded voice; no further external voice submission was attempted. The pre-M4 worktree snapshot and old-art hashes preserve the uncommitted M3/art work. After Editor QA, the user authorized committing and pushing all pending M3/M4 implementation, artwork, plans and evidence.
