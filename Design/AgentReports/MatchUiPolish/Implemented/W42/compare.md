# W42 ARIA panel heading: before and after

Native captures of the shipped ARIA panel (`AriaAssistantButton` in `SCN08_MatchHudContent`) in Play Mode at 1920×1080, EN and FA. They were made by `MatchHudAriaHeadingCapture` (Tests/Editor) on the Windows UI lab checkout.

**How the heading is produced.** The fixture does not choose the heading. For each mission it loads the real resolved setup with `SkirmishSetupBriefing.TryLoad(<id>, 130365)` and puts it on an isolated playing Skirmish session. It then reads the panel model from the shipped `UiShellEcsGateway.TryReadMatchHudAssistantPanel`, applies it to the view, and shows the panel in ARIA Play observing state. Each PNG is the ARIA panel plus a 24 px margin. Each folder has a `geometry.md` with the measured pixels and the rule check.

- **Before:** `before/`, unmodified UI code at eaf0d0381 (fixture only).
  - Log `w42-capture-before-v2.log:1186`: `result=Passed phase=before captures=8 ruleFailures=12 enforced=False`.
  - The 12 failures are 2 rules × 6 cases: FC, BT and CE in EN and FA. The read model and the panel both show BASE ASSAULT / "حمله به پایگاه".
  - BA passes in both languages.
- **After:** `after/`, W42 code (b95b3c28f, fixture 97985991e).
  - Log `w42-capture-after.log:964`: `result=Passed phase=after captures=8 ruleFailures=0 enforced=True`.

These are fixture renders over real mission setups. They are not live mission frames and do not claim mission readiness.

| Mission | Objective | Before EN | After EN | Before FA | After FA |
|---|---|---|---|---|---|
| S002 | Base Assault | [BASE ASSAULT](before/s002-ba-en-1920.png) | [BASE ASSAULT](after/s002-ba-en-1920.png) | [حمله به پایگاه](before/s002-ba-fa-IR-1920.png) | [حمله به پایگاه](after/s002-ba-fa-IR-1920.png) |
| S031 | Frontline Control | [BASE ASSAULT](before/s031-fc-en-1920.png) | [FRONTLINE CONTROL](after/s031-fc-en-1920.png) | [حمله به پایگاه](before/s031-fc-fa-IR-1920.png) | [کنترل خط](after/s031-fc-fa-IR-1920.png) |
| S037 | Breakthrough | [BASE ASSAULT](before/s037-bt-en-1920.png) | [BREAKTHROUGH](after/s037-bt-en-1920.png) | [حمله به پایگاه](before/s037-bt-fa-IR-1920.png) | [شکستن خط](after/s037-bt-fa-IR-1920.png) |
| S020 | Convoy Escort | [BASE ASSAULT](before/s020-ce-en-1920.png) | [CONVOY ESCORT](after/s020-ce-en-1920.png) | [حمله به پایگاه](before/s020-ce-fa-IR-1920.png) | [اسکورت کاروان](after/s020-ce-fa-IR-1920.png) |

## Fit (after, from `after/geometry.md`)

- **Heading frame:** x 1459.7–1886.8 (427 px wide), one line, size 26, not truncated, in every case.
- **EN heading right edge:**
  - BA 1700.6;
  - FC 1808.0 (the longest);
  - BT 1727.1;
  - CE 1731.7.
- **FA headings** are drawn right-to-left and right-aligned inside the frame, on one line. Their height (y 172.7–235.7) matches BA's.
- **Panel size:** the ARIA panel and the Stop ARIA control keep the same rectangles as before.
  - The Stop ARIA control (`WatchAriaPlay`) is 427 × 82.6 px in EN and FA.
  - The panel is inside the 1920×1080 safe area.

## Copy

- **No new strings.**
  - FC, BT and CE reuse the Battle Library family names, the same EN/FA copy the W40 objective card uses (`SkirmishObjectiveCardLayout.Purpose`).
  - BA keeps its catalog title `ui.skirmish.base_assault`.
- **Not changed (outside W42's scope):** the panel body under the heading is still the Base Assault match help ("Destroy the original enemy Barracks…") for FC, BT and CE. It comes from `SkirmishSetupBriefing.MatchHelp`. This is pre-existing and recorded in the results note.
